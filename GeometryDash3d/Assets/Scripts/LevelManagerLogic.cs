using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManagerLogic : MonoBehaviour
{
    public bool IsLevelFinished { get; private set; } = false;

    [Header("UI")]
    [SerializeField] private GameObject levelCompleteUI; // panneau avec boutons Restart / Menu

    [Header("SFX (optionnel)")]
    [SerializeField] private AudioSource sfxSource;   // Output = SFX (2D)
    [SerializeField] private AudioClip restartSfx;
    [Range(0f, 1f)] public float restartVolume = 0.8f;

    private bool isTransitioning = false; // anti double-clic

    public void FinishRun()
    {
        if (IsLevelFinished) return;
        IsLevelFinished = true;

        // 🎵 Stop musique à la fin du niveau
        if (SimpleAudioManager.Instance)
            SimpleAudioManager.Instance.StopMusic();

        if (levelCompleteUI) levelCompleteUI.SetActive(true);

        // >>> Cacher le bouton Pause à l’écran de fin
        var smc = FindObjectOfType<SimpleMenuController>();
        if (smc) smc.HidePauseButtonOnLevelFinish();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 0f;
    }

    // --- Bouton RESTART (depuis l’écran de fin) ---
    public void Btn_RestartLevel()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (restartSfx && sfxSource)
        {
            sfxSource.ignoreListenerPause = true;
            sfxSource.PlayOneShot(restartSfx, restartVolume);
            StartCoroutine(ReloadCurrentSceneRealtime(restartSfx.length, autoPlayAfterReload: true));
        }
        else
        {
            StartCoroutine(ReloadCurrentSceneRealtime(0f, autoPlayAfterReload: true));
        }
    }

    // --- Bouton MENU (depuis l’écran de fin) ---
    public void Btn_BackToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // On relance la scène SANS auto-play pour retomber sur le menu
        StartCoroutine(ReloadCurrentSceneRealtime(0f, autoPlayAfterReload: false));
    }

    private IEnumerator ReloadCurrentSceneRealtime(float waitSec, bool autoPlayAfterReload)
    {
        // Attente en temps réel (la scène est en pause)
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, waitSec));

        // Normalise l’état avant reload
        Time.timeScale = 1f;
        IsLevelFinished = false;

        // Curseur visible car on peut arriver au menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 🎵 Stop musique avant reload
        if (SimpleAudioManager.Instance)
            SimpleAudioManager.Instance.StopMusic();

        // >>> Flag pour SimpleMenuController : auto-play une seule fois si Restart
        PlayerPrefs.SetInt("auto_play_once", autoPlayAfterReload ? 1 : 0);
        PlayerPrefs.Save();

        // Reload de la scène courante (reset total)
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // relance la musique quand la nouvelle scène est prête
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.RestartMusic();
    }
}
