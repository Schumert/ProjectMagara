using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Libraries (drag & drop)")]
    [SerializeField] private SoundDef[] sfxLibrary;
    [SerializeField] private SoundDef[] musicLibrary;

    [Header("Music")]
    [SerializeField] private float musicCrossfadeTime = 0.6f;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 6; // aynı anda birden fazla SFX
    [SerializeField] private bool sfx3DSpatialized = false; // jam için genelde kapalı

    private Dictionary<string, AudioClip> _sfx;
    private Dictionary<string, AudioClip> _music;

    private AudioSource _musicA;
    private AudioSource _musicB;
    private bool _usingA;

    private List<AudioSource> _sfxPool;
    private int _sfxIndex;

    [Serializable]
    public class SfxGroup
    {
        public string key;              // örn: "possession"
        public AudioClip[] variations;  // whoosh1, whoosh2, whoosh3...
    }

    [Serializable]
    public class SoundDef
    {
        public string key;
        public AudioClip clip;
    }

    [SerializeField] private SfxGroup[] sfxGroups;
    private Dictionary<string, AudioClip[]> _groupDict;


    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Kütüphaneleri hazırla
        _sfx = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sfxLibrary) if (s != null && s.clip) _sfx[s.key] = s.clip;

        _music = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in musicLibrary) if (m != null && m.clip) _music[m.key] = m.clip;

        // Müzik kaynakları (crossfade için iki kaynak)
        _musicA = gameObject.AddComponent<AudioSource>();
        _musicB = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { _musicA, _musicB })
        {
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D
            src.volume = 0f;
        }
        _usingA = true;

        // SFX pool
        _sfxPool = new List<AudioSource>(sfxPoolSize);
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = sfx3DSpatialized ? 1f : 0f;
            src.volume = sfxVolume;
            _sfxPool.Add(src);
        }

        SceneManager.activeSceneChanged += (_, __) => CleanupOneShots();


        _groupDict = new Dictionary<string, AudioClip[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in sfxGroups)
            if (g != null && g.variations.Length > 0)
                _groupDict[g.key] = g.variations;

    }

    private void OnDestroy()
    {
        if (I == this) I = null;
        SceneManager.activeSceneChanged -= (_, __) => CleanupOneShots();
    }

    // ---------- MUSIC ----------
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
        _usingA = !_usingA;

        to.clip = clip;
        to.loop = loop;
        to.volume = 0f;
        to.Play();
        StopAllCoroutines();
        StartCoroutine(FadeMusic(from, to, musicCrossfadeTime));
    }

    public void StopMusic(float fadeOut = 0.3f)
    {
        var active = _usingA ? _musicB : _musicA; // son çalan
        StopAllCoroutines();
        StartCoroutine(FadeOutThenStop(active, fadeOut));
    }

    System.Collections.IEnumerator FadeMusic(AudioSource from, AudioSource to, float t)
    {
        float time = 0f;
        while (time < t)
        {
            time += Time.unscaledDeltaTime;
            float k = time / Mathf.Max(0.0001f, t);
            from.volume = Mathf.Lerp(musicVolume, 0f, k);
            to.volume = Mathf.Lerp(0f, musicVolume, k);
            yield return null;
        }
        from.Stop();
        from.volume = 0f;
        to.volume = musicVolume;
    }

    System.Collections.IEnumerator FadeOutThenStop(AudioSource src, float t)
    {
        float start = src.volume;
        float time = 0f;
        while (time < t)
        {
            time += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, time / Mathf.Max(0.0001f, t));
            yield return null;
        }
        src.Stop();
        src.volume = 0f;
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        (_usingA ? _musicB : _musicA).volume = musicVolume; // çalan kaynak
    }

    // ---------- SFX ----------
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
        src.volume = sfxVolume * Mathf.Clamp01(volume);
        src.transform.position = Vector3.zero;
        src.spatialBlend = sfx3DSpatialized ? 1f : 0f;
        src.PlayOneShot(clip);
    }

    public void PlaySFXAtPosition(string key, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (!_sfx.TryGetValue(key, out var clip) || clip == null) return;
        PlaySFXAtPosition(clip, position, volume, pitch);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        // tek seferlik 3D one-shot objesi
        var go = new GameObject($"SFX_{clip.name}");
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.clip = clip;
        src.pitch = pitch;
        src.volume = sfxVolume * Mathf.Clamp01(volume);
        src.spatialBlend = 1f;
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
        foreach (var s in _sfxPool) s.volume = sfxVolume;
    }

    public void MuteAll(bool mute)
    {
        _musicA.mute = _musicB.mute = mute;
        foreach (var s in _sfxPool) s.mute = mute;
    }

    private void CleanupOneShots()
    {
        // sahne değişiminde havada kalan geçici SFX objelerini temizlemek istersen buraya ekleyebilirsin
    }


    public void PlayRandomFromGroup(string groupKey, float volume = 1f, float pitch = 1f)
    {
        if (_groupDict == null || !_groupDict.TryGetValue(groupKey, out var clips)) return;
        if (clips == null || clips.Length == 0) return;

        int idx = UnityEngine.Random.Range(0, clips.Length);
        PlaySFX(clips[idx], volume, pitch);
    }

}
