using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;     // Panel Pause (plein écran, inactif au départ)
    public GameObject skinsPanel;     // Sous-panel Skins (optionnel)
    public GameObject settingsPanel;  // Sous-panel Settings (optionnel)
    [SerializeField] GameObject pauseButton; // Bouton Pause (en haut-droite)

    bool isPaused;
    public bool IsPaused => isPaused;

    void Update()
    {
        // Empêche la pause pendant l’écran de fin
        var lvl = FindObjectOfType<LevelManagerLogic>();
        if (lvl != null && lvl.IsLevelFinished) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        // 🔇 PAUSE GLOBALE DE L’AUDIO
        AudioListener.pause = true;

        if (pausePanel) pausePanel.SetActive(true);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseButton) pauseButton.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // 🔊 REPRISE GLOBALE DE L’AUDIO
        AudioListener.pause = false;

        if (pausePanel) pausePanel.SetActive(false);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (pauseButton) pauseButton.SetActive(true);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --- Boutons ---
    public void Btn_Resume() => Resume();

    public void Btn_Restart()
    {
        // On veut entendre le SFX même en pause :
        // pense à cocher ignoreListenerPause = true sur ton sfxSource si ce n’est pas déjà fait.
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // On nettoie proprement
        AudioListener.pause = false;
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.StopMusic();

        PlayerPrefs.SetInt("auto_play_once", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Btn_BackToMenu()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        AudioListener.pause = false;
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.StopMusic();

        PlayerPrefs.SetInt("auto_play_once", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Btn_QuitGame()
    {
        AudioListener.pause = false;
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.StopMusic();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Btn_OpenSkins()
    {
        // On reste en pause → on renforce la pause audio globale
        AudioListener.pause = true;

        if (skinsPanel)
        {
            skinsPanel.SetActive(true);
            if (pausePanel) pausePanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }
    }

    public void Btn_OpenSettings()
    {
        AudioListener.pause = true;

        if (settingsPanel)
        {
            settingsPanel.SetActive(true);
            if (pausePanel) pausePanel.SetActive(false);
            if (skinsPanel) skinsPanel.SetActive(false);
        }
    }

    public void Btn_BackFromSubpanel()
    {
        // On revient au panneau Pause (toujours en pause audio)
        AudioListener.pause = true;

        if (pausePanel) pausePanel.SetActive(true);
        if (skinsPanel) skinsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }
}
