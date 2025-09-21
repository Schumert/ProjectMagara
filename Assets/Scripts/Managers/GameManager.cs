using System;
using System.Collections.Generic;
using DYP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Player Reference")]
    public BasicMovementController2D player;

    [Header("Scene → Music Map")]
    [Tooltip("Sahne adına göre AudioManager key’i ile müzik çal.")]
    public SceneMusic[] sceneMusic;


    [Serializable]
    public struct SceneMusic
    {
        public string sceneName;
        public string musicKey; // AudioManager’daki key
    }

    // Basit durum bayrakları (ör. "HasKey", "BossDefeated" vs.)
    private readonly HashSet<string> _flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // İlk sahne için de çalışsın
        ApplySceneSetup(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.GetComponent<BasicMovementController2D>();
        }

        ApplySceneSetup(scene.name);
    }

    private void ApplySceneSetup(string sceneName)
    {
        //Müzik
        if (sceneMusic != null)
        {
            foreach (var map in sceneMusic)
            {
                if (!string.IsNullOrEmpty(map.sceneName) &&
                    string.Equals(map.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    AudioManager.I?.PlayMusic(map.musicKey);
                    break;
                }
            }
        }

        //Oyuncu değişiklikleri
        ApplyPlayerModifiers();
    }

    public void ApplyPlayerModifiers()
    {
        if (player == null) return;


        //kutu sayısı artar, azalır - double jump açılır kapanır vs


    }

    // --------- Flags (durum) ----------
    public void SetFlag(string key, bool value = true)
    {
        if (value) _flags.Add(key); else _flags.Remove(key);
        // Flag değişince oyuncu statlarını tekrar uygula
        ApplyPlayerModifiers();
    }

    public bool HasFlag(string key) => _flags.Contains(key);


}
