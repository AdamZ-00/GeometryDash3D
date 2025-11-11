using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class SimpleAudioManager : MonoBehaviour
{
    public static SimpleAudioManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private AudioSource musicSource;     // 2D, Loop, Output = Music
    [SerializeField] private AudioMixer mainMixer;
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
    bool _pendingPlay = false;            // play/crossfade demandé pendant la pause
    float _pendingFade = 0.6f;            // durée à utiliser quand on reprendra
    bool _wasPlayingBeforePause = false;

    Coroutine _fadeCo;                    // une seule coroutine de fade à la fois

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!musicSource) musicSource = GetComponent<AudioSource>();

        // Charger préférences
        masterVolume01 = PlayerPrefs.GetFloat(KEY_MASTER, masterVolume01);
        musicVolume01 = PlayerPrefs.GetFloat(KEY_MUSIC, musicVolume01);
        sfxVolume01 = PlayerPrefs.GetFloat(KEY_SFX, sfxVolume01);
        bool muted = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

        // Appliquer au mixer
        ApplyMixerVolume(exposedMasterParam, muted ? 0f : masterVolume01);
        ApplyMixerVolume(exposedMusicParam, muted ? 0f : musicVolume01);
        ApplyMixerVolume(exposedSfxParam, muted ? 0f : sfxVolume01);

        // Source musique
        if (musicSource)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume01;
        }
    }

    // ===== Pause =====
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
            // Si on avait demandé un play/crossfade pendant la pause
            if (_pendingPlay && CurrentClip != null)
            {
                _pendingPlay = false;
                PlayMusicWithFade(CurrentClip, _pendingFade);
                return;
            }

            if (_wasPlayingBeforePause && musicSource && musicSource.clip && !musicSource.isPlaying)
                musicSource.UnPause();
        }
    }

    // ===== API Musique (play direct) =====
    public void PlayMusic(AudioClip clip)
    {
        if (!clip || musicSource == null) return;

        if (_pauseLock)
        {
            CurrentClip = clip;
            _pendingPlay = true;
            _pendingFade = 0f; // pas de fade imposé
            return;
        }

        ForcePlay(clip);
    }

    public void RestartMusic()
    {
        if (CurrentClip) PlayMusic(CurrentClip);
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
        _pendingPlay = false;
        KillFade();
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

    // ===== Fades =====
    /// <summary>Fade-out simple de la musique en cours.</summary>
    public void FadeOutMusic(float duration = 1f)
    {
        if (!musicSource || !musicSource.isPlaying) return;
        StartFade(FadeOutCoroutine(duration));
    }

    /// <summary>Joue un clip avec fade-in (depuis 0 → musicVolume01).</summary>
    public void PlayMusicWithFade(AudioClip clip, float fadeIn = 1f)
    {
        if (!clip || musicSource == null) return;

        if (_pauseLock)
        {
            CurrentClip = clip;
            _pendingPlay = true;
            _pendingFade = Mathf.Max(0f, fadeIn);
            return;
        }

        StartFade(FadeInSwitchCoroutine(clip, Mathf.Max(0f, fadeIn)));
    }

    /// <summary>
    /// “Crossfade” séquentiel : fade-out du clip courant puis fade-in du nouveau clip.
    /// (Implémentation mono-source, donc pas de chevauchement réel.)
    /// </summary>
    public void CrossfadeTo(AudioClip newClip, float duration = 0.6f)
    {
        if (!newClip || musicSource == null) return;

        if (_pauseLock)
        {
            CurrentClip = newClip;
            _pendingPlay = true;
            _pendingFade = Mathf.Max(0f, duration);
            return;
        }

        // si c'est déjà ce clip et qu'il joue, on ne fait rien
        if (musicSource.isPlaying && musicSource.clip == newClip) return;

        StartFade(FadeOutInCoroutine(newClip, Mathf.Max(0f, duration)));
    }

    // ===== Volumes =====
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
        KillFade();
        musicSource.Stop();
        CurrentClip = clip;
        musicSource.clip = clip;
        musicSource.volume = musicVolume01;
        musicSource.loop = true;
        musicSource.Play();
    }

    void StartFade(IEnumerator routine)
    {
        KillFade();
        _fadeCo = StartCoroutine(routine);
    }

    void KillFade()
    {
        if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; }
    }

    // --- coroutines de fade (temps non-scalé pour marcher en pause menu) ---
    IEnumerator FadeOutCoroutine(float duration)
    {
        if (musicSource == null) yield break;
        float startVol = musicSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.volume = musicVolume01; // prêt pour la prochaine lecture
        _fadeCo = null;
    }

    IEnumerator FadeInSwitchCoroutine(AudioClip clip, float fadeIn)
    {
        // prépare et joue à volume 0
        musicSource.Stop();
        CurrentClip = clip;
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        if (fadeIn <= 0f) { musicSource.volume = musicVolume01; _fadeCo = null; yield break; }

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume01, t / fadeIn);
            yield return null;
        }
        musicSource.volume = musicVolume01;
        _fadeCo = null;
    }

    IEnumerator FadeOutInCoroutine(AudioClip next, float dur)
    {
        // 1) fade-out
        float startVol = musicSource.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / dur);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();

        // 2) switch + fade-in
        CurrentClip = next;
        musicSource.clip = next;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume01, t / dur);
            yield return null;
        }
        musicSource.volume = musicVolume01;
        _fadeCo = null;
    }
}
