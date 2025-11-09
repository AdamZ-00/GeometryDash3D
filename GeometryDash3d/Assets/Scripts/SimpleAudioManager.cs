using UnityEngine;
using UnityEngine.Audio;

public class SimpleAudioManager : MonoBehaviour
{
    public static SimpleAudioManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private AudioSource musicSource;  // 2D, Loop
    [SerializeField] private AudioMixer mainMixer;     // optionnel
    [SerializeField] private string exposedMusicParam = "MusicVolume";

    [Header("Volume (0..1)")]
    [Range(0, 1)] public float musicVolume01 = 0.8f;

    public AudioClip CurrentClip { get; private set; }

    // --- état interne lié à la pause ---
    bool _pauseLock = false;              // vrai = jeu en pause, bloque toute (re)lecture
    bool _pendingPlay = false;            // un PlayMusic a été demandé pendant la pause
    bool _wasPlayingBeforePause = false;  // la musique jouait au moment d’entrer en pause

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!musicSource) musicSource = GetComponent<AudioSource>();
        ApplyMixerVolume(exposedMusicParam, musicVolume01);
    }

    // ===== API PUBLIQUE =====

    // Appelée par PauseController
    public void OnGamePauseChanged(bool paused)
    {
        _pauseLock = paused;

        if (paused)
        {
            _wasPlayingBeforePause = musicSource && musicSource.isPlaying;
            if (musicSource && musicSource.isPlaying) musicSource.Pause();
            // On ne joue rien en pause, même si un PlayMusic arrive -> _pendingPlay passera à true
        }
        else
        {
            // On sort de pause
            if (_pendingPlay && CurrentClip != null)
            {
                _pendingPlay = false;
                ForcePlay(CurrentClip);   // on joue le clip qui était demandé pendant la pause
                return;
            }

            // Sinon, si juste mis en pause, on reprend là où on en était
            if (_wasPlayingBeforePause && musicSource && musicSource.clip && !musicSource.isPlaying)
                musicSource.UnPause();
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (!clip || musicSource == null) return;

        // Si on est en pause, on mémorise la demande, SANS jouer
        if (_pauseLock)
        {
            CurrentClip = clip;
            musicSource.clip = clip;
            _pendingPlay = true;      // sera exécuté en sortie de pause
            return;
        }

        // Sinon on joue immédiatement
        ForcePlay(clip);
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
        _pendingPlay = false; // on annule toute lecture différée
    }

    public void PauseMusic()
    {
        if (musicSource && musicSource.isPlaying) musicSource.Pause();
        _wasPlayingBeforePause = true;
    }

    public void ResumeMusic()
    {
        if (musicSource && musicSource.clip && !musicSource.isPlaying) musicSource.UnPause();
        _wasPlayingBeforePause = false;
    }

    public void RestartMusic()
    {
        if (CurrentClip) PlayMusic(CurrentClip);
    }

    public void SetMusicVolume01(float v)
    {
        musicVolume01 = Mathf.Clamp01(v);
        ApplyMixerVolume(exposedMusicParam, musicVolume01);
        if (musicSource) musicSource.volume = musicVolume01;
    }

    void OnApplicationQuit() { StopMusic(); }

    // ===== internes =====
    void ApplyMixerVolume(string exposedParam, float v01)
    {
        if (!mainMixer || string.IsNullOrEmpty(exposedParam)) return;
        float dB = (v01 <= 0.0001f) ? -80f : Mathf.Log10(v01) * 20f;
        mainMixer.SetFloat(exposedParam, dB);
    }

    void ForcePlay(AudioClip clip)
    {
        musicSource.Stop();
        CurrentClip = clip;
        musicSource.clip = clip;
        musicSource.volume = musicVolume01;
        musicSource.loop = true;
        musicSource.Play();
    }
}
