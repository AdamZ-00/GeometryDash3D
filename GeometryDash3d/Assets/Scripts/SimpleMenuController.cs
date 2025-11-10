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
    public GameObject levelSelectPanel;   // <-- assigne ton LevelSelectPanel

    [Header("Options")]
    public bool openMenuOnStart = true;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject pauseButton; // bouton Pause (UI)

    void Start()
    {
        // Auto-play si un Restart vient d’avoir lieu
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
        // 1) Ferme tous les panneaux de MENU
        HideAllPanels();

        // 2) Si un PausePanel traînait actif, on le ferme aussi
        var pause = FindObjectOfType<PauseController>(true);
        if (pause != null)
        {
            pause.Resume(); // remet TimeScale=1, curseur lock, et (si musique en pause) UnPause
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // 3) Bouton pause visible en jeu
        SetPauseButtonVisible(true);

        // 4) Musique du niveau courant (FORCÉE)
        var loader = FindObjectOfType<LevelLoader>(true);
        if (loader) loader.PlayCurrentLevelMusic();

        // 5) Assureur “fin de frame” (optionnel mais je le garde pour être béton)
        if (loader) loader.PlayCurrentLevelMusicEndOfFrame(this);
    }

    IEnumerator EnsureLevelMusicPlaysAgain()
    {
        yield return null; // fin de frame
        var loader = FindObjectOfType<LevelLoader>(true);
        if (loader) loader.PlayCurrentLevelMusic();

        // (option) une seconde tentative après 0.05s unscaled
        float t = 0f;
        while (t < 0.05f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        loader = FindObjectOfType<LevelLoader>(true);
        if (loader) loader.PlayCurrentLevelMusic();
    }

    public void OpenMenu()
    {
        ShowMain();
        Time.timeScale = 0f;

        SetPauseButtonVisible(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowMain()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false); // cache Levels quand on revient au main
    }

    public void ShowSettings()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        // 👉 pas de musique ici (menu navigation only)
    }

    public void ShowSkins()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(true);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
        // 👉 pas de musique ici (menu navigation only)
    }

    // NEW: bouton "Levels" du MainPanel
    public void ShowLevels()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(true);

        // remet le curseur visible (on est en menu)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // notifie le LevelSelectUI qu'on ouvre (pour rafraîchir l'aperçu)
        var ui = levelSelectPanel ? levelSelectPanel.GetComponentInChildren<LevelSelectUI>(true) : null;
        if (ui) ui.RefreshFromPrefs();

        // 👉 pas de musique ici (menu navigation only)
    }

    public void Back() => ShowMain();

    public void Quit()
    {
        // 🎵 stop musique avant de quitter
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.StopMusic();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // quitte le Play Mode
#else
        Application.Quit(); // ferme l’app en build
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

    public void HidePauseButtonOnLevelFinish()
    {
        SetPauseButtonVisible(false);
    }

    // Ferme tous les panneaux de menu (utilisé par PauseController.Resume)
    public void CloseAllPanels()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (levelSelectPanel) levelSelectPanel.SetActive(false);
    }

}
