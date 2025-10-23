using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManagerLogic : MonoBehaviour
{
    public bool IsLevelFinished { get; private set; } = false;

    [Header("UI")]
    [SerializeField] private GameObject levelCompleteUI;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;   // Output = SFX (2D)
    [SerializeField] private AudioClip restartSfx;
    [Range(0f, 1f)] public float restartVolume = 0.8f;

    public void FinishRun()
    {
        if (IsLevelFinished) return;
        IsLevelFinished = true;

        // Stop musique à la fin du niveau
        if (SimpleAudioManager.Instance)
            SimpleAudioManager.Instance.StopMusic();

        if (levelCompleteUI) levelCompleteUI.SetActive(true);

        // Met le jeu en pause : la physique et Update/FixedUpdate sont figés
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        // Joue le SFX tout de suite (il joue même en pause si AudioSource est normal)
        if (restartSfx && sfxSource)
        {
            sfxSource.PlayOneShot(restartSfx, restartVolume);
            // IMPORTANT : on n'enlève PAS la pause ici
            StartCoroutine(RestartAfterSoundRealtime(restartSfx.length));
        }
        else
        {
            // Pas de son -> restart immédiat
            DoRestart();
        }
    }

    private IEnumerator RestartAfterSoundRealtime(float clipLen)
    {
        // Reste en pause (Time.timeScale == 0) pour que le cube n'avance pas
        // On attend en temps réel, donc le son peut jouer jusqu'au bout
        float wait = Mathf.Max(0.1f, clipLen);
        yield return new WaitForSecondsRealtime(wait);

        DoRestart();
    }

    private void DoRestart()
    {
        // On sort de la pause juste au moment du reload
        Time.timeScale = 1f;
        IsLevelFinished = false;

        if (SimpleAudioManager.Instance)
            SimpleAudioManager.Instance.StopMusic();

        int idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
        // La musique sera relancée automatiquement par LevelMusic.Start()
    }
}
