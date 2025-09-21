using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Libraries (drag & drop)")]
    [SerializeField] private SoundDef[] sfxLibrary;
    [SerializeField] private SoundDef[] musicLibrary;

    [Header("Music")]
    [SerializeField] private float musicCrossfadeTime = 0.6f;

    [Header("Mixer (drag & drop)")]
    [SerializeField] private AudioMixer musicMixer;                 // Exposed param: MusicVolDb
    [SerializeField] private AudioMixerGroup musicGroup;            // Music group
    [SerializeField] private AudioMixerGroup sfxGroup;              // SFX group
    [SerializeField] private string musicVolParam = "MusicVolDb";   // Exposed

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.8f; // user master
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 6;
    [SerializeField] private bool sfx3DSpatialized = false;

    private Dictionary<string, AudioClip> _sfx;
    private Dictionary<string, AudioClip> _music;

    private AudioSource _musicA;
    private AudioSource _musicB;
    private bool _usingA;

    // Crossfade weights
    private float _aWeight = 1f;
    private float _bWeight = 0f;

    private List<AudioSource> _sfxPool;
    private int _sfxIndex;

    [Serializable] public class SfxGroup { public string key; public AudioClip[] variations; }
    [Serializable] public class SoundDef { public string key; public AudioClip clip; }

    [SerializeField] private SfxGroup[] sfxGroups;
    private Dictionary<string, AudioClip[]> _groupDict;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        _sfx = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sfxLibrary) if (s != null && s.clip) _sfx[s.key] = s.clip;

        _music = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in musicLibrary) if (m != null && m.clip) _music[m.key] = m.clip;

        // Music sources
        _musicA = gameObject.AddComponent<AudioSource>();
        _musicB = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { _musicA, _musicB })
        {
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D
            src.volume = 1f;       // Kaynak volumeleri 1'de kalsın; master'ı Mixer ve weight yönetecek
            if (musicGroup) src.outputAudioMixerGroup = musicGroup;
        }
        _usingA = true;
        _aWeight = 1f; _bWeight = 0f;

        ApplyMusicVolumes(); // weights * musicVolume -> Mixer’a yaz

        // SFX pool
        _sfxPool = new List<AudioSource>(sfxPoolSize);
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = sfx3DSpatialized ? 1f : 0f;
            src.volume = 1f; // SFX master’ı Mixer ile, anlık gain’i parametreyle
            if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
            _sfxPool.Add(src);
        }

        _groupDict = new Dictionary<string, AudioClip[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in sfxGroups)
            if (g != null && g.variations != null && g.variations.Length > 0)
                _groupDict[g.key] = g.variations;

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldS, Scene newS) => CleanupOneShots();

    // ===== MUSIC =====
    public void PlayMusic(string key, bool loop = true)
    {
        if (!_music.TryGetValue(key, out var clip) || clip == null)
        {
            Debug.LogWarning($"[AudioManager] Music key not found: {key}");
            return;
        }
        PlayMusic(clip, loop);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        var from = _usingA ? _musicA : _musicB;
        var to = _usingA ? _musicB : _musicA;
        _usingA = !_usingA; // 'to' aktif hedef

        to.clip = clip;
        to.loop = loop;
        to.Play();

        // Debug: çalan tüm AudioSource’ları kontrol et
        DebugActiveMusicSources("PlayMusic");

        StopAllCoroutines();
        StartCoroutine(FadeMusic(fromIsA: from == _musicA));
    }

    public void StopMusic(float fadeOut = 0.3f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutWeights(fadeOut));
    }

    // User master -> dB; weights -> balance; toplam = mixer master * weights
    private void ApplyMusicVolumes()
    {
        // Mixer master: user musicVolume’u dB’ye çevir
        if (musicMixer)
        {
            float dB = (musicVolume <= 0.0001f) ? -80f : Mathf.Log10(musicVolume) * 20f; // 0..1 -> -80..0 dB
            musicMixer.SetFloat(musicVolParam, dB);
        }
        else
        {
            // Mixer yoksa, kaynakları doğrudan ölçekle (fallback)
            _musicA.volume = musicVolume * _aWeight;
            _musicB.volume = musicVolume * _bWeight;
        }
    }

    System.Collections.IEnumerator FadeMusic(bool fromIsA)
    {
        float t = 0f;
        float dur = musicCrossfadeTime;

        float a0 = _aWeight;
        float b0 = _bWeight;

        float aTarget = fromIsA ? 0f : 1f;
        float bTarget = fromIsA ? 1f : 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));
            _aWeight = Mathf.Lerp(a0, aTarget, k);
            _bWeight = Mathf.Lerp(b0, bTarget, k);

            if (!musicMixer) // Mixer varsa master zaten set; sadece fallback’te kaynak volumeleri güncelle
            {
                _musicA.volume = musicVolume * _aWeight;
                _musicB.volume = musicVolume * _bWeight;
            }
            yield return null;
        }

        _aWeight = aTarget;
        _bWeight = bTarget;

        if (!musicMixer)
        {
            _musicA.volume = musicVolume * _aWeight;
            _musicB.volume = musicVolume * _bWeight;
        }

        // Ağırlığı sıfır olan kanalı durdur
        if (Mathf.Approximately(_aWeight, 0f)) _musicA.Stop();
        if (Mathf.Approximately(_bWeight, 0f)) _musicB.Stop();
    }

    System.Collections.IEnumerator FadeOutWeights(float dur)
    {
        float t = 0f;
        float a0 = _aWeight, b0 = _bWeight;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));
            _aWeight = Mathf.Lerp(a0, 0f, k);
            _bWeight = Mathf.Lerp(b0, 0f, k);

            if (!musicMixer)
            {
                _musicA.volume = musicVolume * _aWeight;
                _musicB.volume = musicVolume * _bWeight;
            }
            yield return null;
        }

        _aWeight = 0f; _bWeight = 0f;
        if (!musicMixer)
        {
            _musicA.volume = 0f;
            _musicB.volume = 0f;
        }
        _musicA.Stop();
        _musicB.Stop();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        ApplyMusicVolumes(); // Mixer master’ı (veya fallback) anında güncelle
    }

    // ===== SFX =====
    public void PlaySFX(string key, float volume = 1f, float pitch = 1f)
    {
        if (!_sfx.TryGetValue(key, out var clip) || clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX key not found: {key}");
            return;
        }
        PlaySFX(clip, volume, pitch);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        var src = NextSfxSource();
        src.pitch = pitch;
        // SFX master’ı mixer’dan; burada relatif gain uygula
        float rel = Mathf.Clamp01(volume);
        if (!sfxGroup) src.volume = sfxVolume * rel;
        else src.volume = rel; // mixer tarafı master, burada sadece lokal gain
        src.transform.position = Vector3.zero;
        src.spatialBlend = sfx3DSpatialized ? 1f : 0f;
        src.outputAudioMixerGroup = sfxGroup ? sfxGroup : src.outputAudioMixerGroup;
        src.PlayOneShot(clip);
    }

    public void PlaySFXAtPosition(string key, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (!_sfx.TryGetValue(key, out var clip) || clip == null) return;
        PlaySFXAtPosition(clip, position, volume, pitch);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        var go = new GameObject($"SFX_{clip.name}");
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.clip = clip;
        src.pitch = pitch;
        if (!sfxGroup) src.volume = sfxVolume * Mathf.Clamp01(volume);
        else src.volume = Mathf.Clamp01(volume);
        src.spatialBlend = 1f;
        if (sfxGroup) src.outputAudioMixerGroup = sfxGroup;
        go.transform.position = position;
        src.Play();
        Destroy(go, clip.length + 0.05f);
    }

    private AudioSource NextSfxSource()
    {
        _sfxIndex = (_sfxIndex + 1) % _sfxPool.Count;
        return _sfxPool[_sfxIndex];
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        // Mixer kullanıyorsan burada ayrı bir SFX dB paramı kullanmanı tavsiye ederim (örn. "SfxVolDb")
        if (!sfxGroup)
            foreach (var s in _sfxPool) s.volume = sfxVolume;
    }

    public void MuteAll(bool mute)
    {
        _musicA.mute = _musicB.mute = mute;
        foreach (var s in _sfxPool) s.mute = mute;
    }

    private void CleanupOneShots()
    {
        // ihtiyaca göre sahne değişiminde geçici SFX objelerini temizleyebilirsin
    }

    public void PlayRandomFromGroup(string groupKey, float volume = 1f, float pitch = 1f)
    {
        if (_groupDict == null || !_groupDict.TryGetValue(groupKey, out var clips)) return;
        if (clips == null || clips.Length == 0) return;
        int idx = UnityEngine.Random.Range(0, clips.Length);
        PlaySFX(clips[idx], volume, pitch);
    }

    // ——— Debug yardımcıları ———
    private void DebugActiveMusicSources(string tag)
    {
        var all = FindObjectsOfType<AudioSource>(true);
        int cnt = 0;
        foreach (var a in all)
        {
            if (a.isPlaying && a.clip != null && a.outputAudioMixerGroup == musicGroup)
            {
                cnt++;
                Debug.Log($"[AudioManager:{tag}] Playing: {a.clip.name} vol={a.volume:F2} group={(a.outputAudioMixerGroup ? a.outputAudioMixerGroup.name : "none")}");
            }
        }
        if (cnt > 1) Debug.LogWarning($"[AudioManager:{tag}] WARNING: {cnt} music sources playing simultaneously.");
    }
}
