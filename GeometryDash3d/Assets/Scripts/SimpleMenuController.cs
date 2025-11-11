using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject skinsPanel;

    [Header("NEW: Levels UI Panel")]
    public GameObject levelSelectPanel;

    [Header("Options")]
    public bool openMenuOnStart = true;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject pauseButton;

    [Header("Audio")]
    [SerializeField] private AudioClip menuMusic;   // musique d’ambiance menu

    void Start()
    {
        if (PlayerPrefs.GetInt("auto_play_once", 0) == 1)
        {
            PlayerPrefs.SetInt("auto_play_once", 0);
            PlayerPrefs.Save();
            Play();
            return;
        }

        if (openMenuOnStart) OpenMenu();
        else HideAllPanels();
    }

    public void Play()
    {
        // Ferme les panneaux
        HideAllPanels();

        // Ferme un éventuel PausePanel actif
        var pause = FindObjectOfType<PauseController>(true);
        if (pause != null) pause.Resume();
        else
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        SetPauseButtonVisible(true);

        // Crossfade vers la musique du niveau courant
        var loader = FindObjectOfType<LevelLoader>(true);
        if (loader) loader.PlayCurrentLevelMusic(0.6f);   // <-- crossfade propre

        // “Assureur” fin de frame
        if (loader) loader.PlayCurrentLevelMusicEndOfFrame(this, 0.6f);
    }

    public void OpenMenu()
    {
        ShowMain();
        Time.timeScale = 0f;
        SetPauseButtonVisible(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Crossfade vers la musique du MENU
        if (SimpleAudioManager.Instance && menuMusic)
            SimpleAudioManager.Instance.CrossfadeTo(menuMusic, 0.6f);
    }

    public void ShowMain()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
    }

    public void ShowSkins()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(true);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
    }

    public void ShowLevels()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        var ui = levelSelectPanel ? levelSelectPanel.GetComponentInChildren<LevelSelectUI>(true) : null;
        if (ui) ui.RefreshFromPrefs();
    }

    public void Back() => ShowMain();

    public void Quit()
    {
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.FadeOutMusic(0.4f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void HideAllPanels()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
    }

    public void SetPauseButtonVisible(bool visible)
    {
        if (pauseButton) pauseButton.SetActive(visible);
    }

    public void HidePauseButtonOnLevelFinish() => SetPauseButtonVisible(false);

    public void CloseAllPanels() => HideAllPanels();
}
