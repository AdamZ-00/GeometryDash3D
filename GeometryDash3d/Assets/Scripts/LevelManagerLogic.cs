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

        // Pause totale du jeu (la musique est déjà stoppée, le SFX de restart jouera en temps réel)
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        // Joue le SFX et attend en temps réel pendant la pause
        if (restartSfx && sfxSource)
        {
            sfxSource.ignoreListenerPause = true;          // pour être sûr d’entendre le son même si tu pauses globalement l'audio
            sfxSource.PlayOneShot(restartSfx, restartVolume);
            StartCoroutine(RestartAfterSoundRealtime(restartSfx.length));
        }
        else
        {
            DoRestart();
        }
    }

    private IEnumerator RestartAfterSoundRealtime(float clipLen)
    {
        // On reste en pause : le cube n’avance pas pendant que le son joue
        float wait = Mathf.Max(0.1f, clipLen);
        yield return new WaitForSecondsRealtime(wait);

        DoRestart();
    }

    private void DoRestart()
    {
        Time.timeScale = 1f;
        IsLevelFinished = false;

        if (SimpleAudioManager.Instance)
            SimpleAudioManager.Instance.StopMusic();

        int idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
        // La musique sera relancée automatiquement par ton LevelMusic/AudioManager au Start
    }
}
