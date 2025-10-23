using UnityEngine;
using UnityEngine.Audio;

public class SimpleAudioManager : MonoBehaviour
{
    public static SimpleAudioManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private AudioSource musicSource;  // assigne l'AudioSource musique (2D, Loop)
    [SerializeField] private AudioMixer mainMixer;     // (optionnel) ton MainMixer
    [SerializeField] private string exposedMusicParam = "MusicVolume";

    [Header("Volume (0..1)")]
    [Range(0, 1)] public float musicVolume01 = 0.8f;

    public AudioClip CurrentClip { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!musicSource) musicSource = GetComponent<AudioSource>();
        ApplyMixerVolume(exposedMusicParam, musicVolume01);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        CurrentClip = clip;
        musicSource.clip = clip;
        musicSource.volume = musicVolume01; // direct, pas de fade ici
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void RestartMusic() // relance la dernière musique connue
    {
        if (CurrentClip) PlayMusic(CurrentClip);
    }

    public void SetMusicVolume01(float v)
    {
        musicVolume01 = Mathf.Clamp01(v);
        ApplyMixerVolume(exposedMusicParam, musicVolume01);
        musicSource.volume = musicVolume01;
    }

    private void ApplyMixerVolume(string exposedParam, float v01)
    {
        if (!mainMixer || string.IsNullOrEmpty(exposedParam)) return;
        float dB = (v01 <= 0.0001f) ? -80f : Mathf.Log10(v01) * 20f;
        mainMixer.SetFloat(exposedParam, dB);
    }
}
