using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI")]
    public Slider masterSlider;  // 0..1
    public Slider musicSlider;   // 0..1
    public Slider sfxSlider;     // 0..1
    public Toggle muteToggle;

    // Clés UI (on sépare de SimpleAudioManager si tu veux des valeurs par défaut différentes)
    const string KEY_MASTER = "ui_master_v01";
    const string KEY_MUSIC = "ui_music_v01";
    const string KEY_SFX = "ui_sfx_v01";
    const string KEY_MUTE = "ui_mute";

    float _lastMaster = 1.0f;
    float _lastMusic = 0.8f;
    float _lastSfx = 0.9f;

    void Start()
    {
        // Récupère valeurs sauvegardées sinon celles de l'AudioManager
        float m = PlayerPrefs.GetFloat(KEY_MASTER, SimpleAudioManager.Instance ? SimpleAudioManager.Instance.masterVolume01 : 1.0f);
        float mu = PlayerPrefs.GetFloat(KEY_MUSIC, SimpleAudioManager.Instance ? SimpleAudioManager.Instance.musicVolume01 : 0.8f);
        float sx = PlayerPrefs.GetFloat(KEY_SFX, SimpleAudioManager.Instance ? SimpleAudioManager.Instance.sfxVolume01 : 0.9f);
        bool mt = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

        // Applique au système
        if (SimpleAudioManager.Instance)
        {
            SimpleAudioManager.Instance.SetMasterVolume01(mt ? 0f : m);
            SimpleAudioManager.Instance.SetMusicVolume01(mt ? 0f : mu);
            SimpleAudioManager.Instance.SetSfxVolume01(mt ? 0f : sx);
            SimpleAudioManager.Instance.MuteAll(mt);
        }

        // UI
        SetupSlider(masterSlider, m);
        SetupSlider(musicSlider, mu);
        SetupSlider(sfxSlider, sx);
        muteToggle.isOn = mt;

        if (m > 0.001f) _lastMaster = m;
        if (mu > 0.001f) _lastMusic = mu;
        if (sx > 0.001f) _lastSfx = sx;

        // Abonnements
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        muteToggle.onValueChanged.AddListener(OnMuteToggled);
    }

    void SetupSlider(Slider s, float v) { s.minValue = 0f; s.maxValue = 1f; s.value = v; }

    // --- Callbacks Sliders ---
    public void OnMasterChanged(float v)
    {
        if (v > 0.001f) _lastMaster = v;
        if (muteToggle.isOn && v > 0.001f) muteToggle.isOn = false;

        SimpleAudioManager.Instance?.SetMasterVolume01(v);
        PlayerPrefs.SetFloat(KEY_MASTER, v); PlayerPrefs.Save();
    }

    public void OnMusicChanged(float v)
    {
        if (v > 0.001f) _lastMusic = v;
        if (muteToggle.isOn && v > 0.001f) muteToggle.isOn = false;

        SimpleAudioManager.Instance?.SetMusicVolume01(v);
        PlayerPrefs.SetFloat(KEY_MUSIC, v); PlayerPrefs.Save();
    }

    public void OnSfxChanged(float v)
    {
        if (v > 0.001f) _lastSfx = v;
        if (muteToggle.isOn && v > 0.001f) muteToggle.isOn = false;

        SimpleAudioManager.Instance?.SetSfxVolume01(v);
        PlayerPrefs.SetFloat(KEY_SFX, v); PlayerPrefs.Save();
    }

    // --- Toggle Mute ---
    public void OnMuteToggled(bool mute)
    {
        if (mute)
        {
            // Sauvegarde les valeurs actuelles et coupe
            PlayerPrefs.SetFloat(KEY_MASTER, masterSlider.value);
            PlayerPrefs.SetFloat(KEY_MUSIC, musicSlider.value);
            PlayerPrefs.SetFloat(KEY_SFX, sfxSlider.value);
            PlayerPrefs.SetInt(KEY_MUTE, 1);
            PlayerPrefs.Save();

            SimpleAudioManager.Instance?.MuteAll(true);

            masterSlider.SetValueWithoutNotify(0f);
            musicSlider.SetValueWithoutNotify(0f);
            sfxSlider.SetValueWithoutNotify(0f);
        }
        else
        {
            // Restaure des niveaux “sains”
            float m = Mathf.Max(PlayerPrefs.GetFloat(KEY_MASTER, _lastMaster), 0.05f);
            float mu = Mathf.Max(PlayerPrefs.GetFloat(KEY_MUSIC, _lastMusic), 0.05f);
            float sx = Mathf.Max(PlayerPrefs.GetFloat(KEY_SFX, _lastSfx), 0.05f);

            SimpleAudioManager.Instance?.MuteAll(false);
            SimpleAudioManager.Instance?.SetMasterVolume01(m);
            SimpleAudioManager.Instance?.SetMusicVolume01(mu);
            SimpleAudioManager.Instance?.SetSfxVolume01(sx);

            masterSlider.SetValueWithoutNotify(m);
            musicSlider.SetValueWithoutNotify(mu);
            sfxSlider.SetValueWithoutNotify(sx);

            PlayerPrefs.SetInt(KEY_MUTE, 0); PlayerPrefs.Save();
        }
    }
}
