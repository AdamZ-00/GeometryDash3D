using UnityEngine;
using UnityEngine.SceneManagement;   // <- IMPORTANT

public class LevelManagerLogic : MonoBehaviour
{
    public bool IsLevelFinished { get; private set; } = false;

    [Header("UI")]
    [SerializeField] private GameObject levelCompleteUI;

    public void FinishRun()
    {
        if (IsLevelFinished) return;
        IsLevelFinished = true;

        if (levelCompleteUI != null) levelCompleteUI.SetActive(true);
        Time.timeScale = 0f;
    }

    // <- Cette méthode doit être PUBLIC, NON statique, SANS paramètre
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        IsLevelFinished = false;

        int idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
    }
}