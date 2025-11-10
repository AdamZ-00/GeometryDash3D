using UnityEngine;
using UnityEngine.Audio;

public class SimpleAudioManager : MonoBehaviour
{
    public static SimpleAudioManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private AudioSource musicSource;     // 2D, Loop, Output=Music
    [SerializeField] private AudioMixer mainMixer;        // ton AudioMixer
    [SerializeField] private string exposedMasterParam = "MasterVolume";
    [SerializeField] private string exposedMusicParam = "MusicVolume";
    [SerializeField] private string exposedSfxParam = "SfxVolume";

    [Header("Linear volumes (0..1)")]
    [Range(0, 1)] public float masterVolume01 = 1.0f;
    [Range(0, 1)] public float musicVolume01 = 0.8f;
    [Range(0, 1)] public float sfxVolume01 = 0.9f;

    [Header("Persist Keys")]
    const string KEY_MASTER = "audio_master_v01";
    const string KEY_MUSIC = "audio_music_v01";
    const string KEY_SFX = "audio_sfx_v01";
    const string KEY_MUTE = "audio_mute";

    public AudioClip CurrentClip { get; private set; }

    // --- état interne pause ---
    bool _pauseLock = false;              // jeu en pause → ne jamais (re)jouer
    bool _pendingPlay = false;            // play demandé pendant la pause
    bool _wasPlayingBeforePause = false;  // jouait au moment de la pause

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!musicSource) musicSource = GetComponent<AudioSource>();

        // Charger les prefs sauvegardées
        masterVolume01 = PlayerPrefs.GetFloat(KEY_MASTER, masterVolume01);
        musicVolume01 = PlayerPrefs.GetFloat(KEY_MUSIC, musicVolume01);
        sfxVolume01 = PlayerPrefs.GetFloat(KEY_SFX, sfxVolume01);

        bool muted = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

        // Appliquer au mixer
        ApplyMixerVolume(exposedMasterParam, muted ? 0f : masterVolume01);
        ApplyMixerVolume(exposedMusicParam, muted ? 0f : musicVolume01);
        ApplyMixerVolume(exposedSfxParam, muted ? 0f : sfxVolume01);

        // Ajuster la source musique
        if (musicSource)
        {
            musicSource.playOnAwake = false;  // IMPORTANT
            musicSource.loop = true;
            musicSource.volume = musicVolume01;
            musicSource.spatialBlend = 0f;
        }
    }

    // ===== Pause globale “logique” (on garde aussi AudioListener.pause côté PauseController) =====
    public void OnGamePauseChanged(bool paused)
    {
        _pauseLock = paused;

        if (paused)
        {
            _wasPlayingBeforePause = musicSource && musicSource.isPlaying;
            if (musicSource && musicSource.isPlaying) musicSource.Pause();
        }
        else
        {
            if (_pendingPlay && CurrentClip != null)
            {
                _pendingPlay = false;
                ForcePlay(CurrentClip);
                return;
            }
            if (_wasPlayingBeforePause && musicSource && musicSource.clip && !musicSource.isPlaying)
                musicSource.UnPause();
        }
    }

    // ===== Musique =====
    public void PlayMusic(AudioClip clip)
    {
        if (!clip || musicSource == null) return;

        if (_pauseLock)
        {
            CurrentClip = clip;
            musicSource.clip = clip;
            _pendingPlay = true;    // on jouera à la sortie de pause
            return;
        }

        ForcePlay(clip);
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
        _pendingPlay = false;
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

    // ===== Volumes (0..1 linéaires) =====
    public void SetMasterVolume01(float v)
    {
        masterVolume01 = Mathf.Clamp01(v);
        ApplyMixerVolume(exposedMasterParam, masterVolume01);
        PlayerPrefs.SetFloat(KEY_MASTER, masterVolume01);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume01(float v)
    {
        musicVolume01 = Mathf.Clamp01(v);
        ApplyMixerVolume(exposedMusicParam, musicVolume01);
        if (musicSource) musicSource.volume = musicVolume01;
        PlayerPrefs.SetFloat(KEY_MUSIC, musicVolume01);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume01(float v)
    {
        sfxVolume01 = Mathf.Clamp01(v);
        ApplyMixerVolume(exposedSfxParam, sfxVolume01);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume01);
        PlayerPrefs.Save();
    }

    public void MuteAll(bool mute)
    {
        PlayerPrefs.SetInt(KEY_MUTE, mute ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMixerVolume(exposedMasterParam, mute ? 0f : masterVolume01);
        ApplyMixerVolume(exposedMusicParam, mute ? 0f : musicVolume01);
        ApplyMixerVolume(exposedSfxParam, mute ? 0f : sfxVolume01);

        if (musicSource && !mute)
            musicSource.volume = musicVolume01;
    }

    // ===== internes =====
    static float LinearToDb(float v01) => (v01 <= 0.0001f) ? -80f : Mathf.Log10(Mathf.Clamp01(v01)) * 20f;

    void ApplyMixerVolume(string exposedParam, float v01)
    {
        if (!mainMixer || string.IsNullOrEmpty(exposedParam)) return;
        mainMixer.SetFloat(exposedParam, LinearToDb(v01));
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
