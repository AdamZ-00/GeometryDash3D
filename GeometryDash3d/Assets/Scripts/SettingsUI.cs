using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI")]
    public Slider masterSlider;  // 0..1
    public Slider musicSlider;   // 0..1
    public Slider sfxSlider;     // 0..1
    public Toggle muteToggle;

    // Clés UI (on garde ton espace de clés)
    const string KEY_MASTER = "ui_master_v01";
    const string KEY_MUSIC = "ui_music_v01";
    const string KEY_SFX = "ui_sfx_v01";
    const string KEY_MUTE = "ui_mute";

    // Valeurs par défaut affichées si aucune pref
    float _defaultMaster = 1.0f;
    float _defaultMusic = 0.8f;
    float _defaultSfx = 0.9f;

    bool _loading = false;

    void OnEnable()
    {
        _loading = true;

        // 1) Charger les valeurs persistées (sinon reprendre celles de l’AudioManager, sinon défauts)
        float vMaster = PlayerPrefs.GetFloat(KEY_MASTER,
            SimpleAudioManager.Instance ? SimpleAudioManager.Instance.masterVolume01 : _defaultMaster);
        float vMusic = PlayerPrefs.GetFloat(KEY_MUSIC,
            SimpleAudioManager.Instance ? SimpleAudioManager.Instance.musicVolume01 : _defaultMusic);
        float vSfx = PlayerPrefs.GetFloat(KEY_SFX,
            SimpleAudioManager.Instance ? SimpleAudioManager.Instance.sfxVolume01 : _defaultSfx);
        bool muted = PlayerPrefs.GetInt(KEY_MUTE, 0) == 1;

        // 2) Appliquer à l’audio (mute via Master, sans écraser les sliders)
        if (SimpleAudioManager.Instance)
        {
            SimpleAudioManager.Instance.MuteAll(muted);          // coupe/remet le Master dans le Mixer
            SimpleAudioManager.Instance.SetMasterVolume01(vMaster);
            SimpleAudioManager.Instance.SetMusicVolume01(vMusic);
            SimpleAudioManager.Instance.SetSfxVolume01(vSfx);
        }

        // 3) Configurer l’UI sans déclencher d’événements
        if (masterSlider) { masterSlider.minValue = 0f; masterSlider.maxValue = 1f; masterSlider.SetValueWithoutNotify(vMaster); }
        if (musicSlider) { musicSlider.minValue = 0f; musicSlider.maxValue = 1f; musicSlider.SetValueWithoutNotify(vMusic); }
        if (sfxSlider) { sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f; sfxSlider.SetValueWithoutNotify(vSfx); }
        if (muteToggle) { muteToggle.SetIsOnWithoutNotify(muted); }

        // 4) (Ré)abonner proprement
        RemoveAllListeners();
        if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (muteToggle) muteToggle.onValueChanged.AddListener(OnMuteToggled);

        _loading = false;
    }

    void OnDisable() => RemoveAllListeners();

    void RemoveAllListeners()
    {
        if (masterSlider) masterSlider.onValueChanged.RemoveAllListeners();
        if (musicSlider) musicSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider) sfxSlider.onValueChanged.RemoveAllListeners();
        if (muteToggle) muteToggle.onValueChanged.RemoveAllListeners();
    }

    // --- Callbacks ---
    public void OnMasterChanged(float v)
    {
        if (_loading) return;
        SimpleAudioManager.Instance?.SetMasterVolume01(v);
        PlayerPrefs.SetFloat(KEY_MASTER, Mathf.Clamp01(v));
        PlayerPrefs.Save();

        // Petit confort : si l’utilisateur bouge un slider alors que Mute est ON, on dé-mute
        if (muteToggle && muteToggle.isOn && v > 0.001f)
            muteToggle.SetIsOnWithoutNotify(false); OnMuteToggled(false);
    }

    public void OnMusicChanged(float v)
    {
        if (_loading) return;
        SimpleAudioManager.Instance?.SetMusicVolume01(v);
        PlayerPrefs.SetFloat(KEY_MUSIC, Mathf.Clamp01(v));
        PlayerPrefs.Save();

        if (muteToggle && muteToggle.isOn && v > 0.001f)
            muteToggle.SetIsOnWithoutNotify(false); OnMuteToggled(false);
    }

    public void OnSfxChanged(float v)
    {
        if (_loading) return;
        SimpleAudioManager.Instance?.SetSfxVolume01(v);
        PlayerPrefs.SetFloat(KEY_SFX, Mathf.Clamp01(v));
        PlayerPrefs.Save();

        if (muteToggle && muteToggle.isOn && v > 0.001f)
            muteToggle.SetIsOnWithoutNotify(false); OnMuteToggled(false);
    }

    public void OnMuteToggled(bool mute)
    {
        if (_loading) return;

        // On ne touche PAS aux sliders visuels : mute agit via le Master dans le Mixer
        SimpleAudioManager.Instance?.MuteAll(mute);
        PlayerPrefs.SetInt(KEY_MUTE, mute ? 1 : 0);
        PlayerPrefs.Save();

        // (Option) Tu peux griser les sliders si mute :
        // if (masterSlider) masterSlider.interactable = !mute;
        // if (musicSlider)  musicSlider.interactable  = !mute;
        // if (sfxSlider)    sfxSlider.interactable    = !mute;
    }
}
