using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Compteur de morts PERSISTANT par niveau (index de LevelLoader).
/// Clés PlayerPrefs:
///  - Deaths:LevelIndex:{i}
///  - Deaths:TOTAL
/// </summary>
public class DeathCounter : MonoBehaviour
{
    public static DeathCounter Instance { get; private set; }

    private string _levelKey;                   // ex: "Deaths:LevelIndex:0"
    private const string TOTAL_KEY = "Deaths:TOTAL";

    public int Deaths { get; private set; } = 0;
    public int TotalDeaths => PlayerPrefs.GetInt(TOTAL_KEY, 0);

    public System.Action<int> OnDeathsChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Écoute les reloads de scène (au cas où) et surtout les changements de niveau
        SceneManager.sceneLoaded += OnSceneLoaded;
        LevelLoader.OnLevelInstantiated += OnLevelInstantiated;

        // Init clé pour le niveau courant (si LevelLoader n'a pas encore instancié)
        BuildKeyFromPrefsIndex();
        LoadLevelDeaths();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            LevelLoader.OnLevelInstantiated -= OnLevelInstantiated;
        }
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // En single-scene, ça peut suffire si LevelLoader n'a pas encore émis son event
        BuildKeyFromPrefsIndex();
        LoadLevelDeaths();
    }

    void OnLevelInstantiated(int index, GameObject _)
    {
        BuildKeyForIndex(index);
        LoadLevelDeaths();
    }

    void BuildKeyFromPrefsIndex()
    {
        int idx = PlayerPrefs.GetInt(LevelLoader.KEY_SELECTED_LEVEL, 0);
        BuildKeyForIndex(idx);
    }

    void BuildKeyForIndex(int idx)
    {
        _levelKey = $"Deaths:LevelIndex:{Mathf.Max(0, idx)}";
    }

    void LoadLevelDeaths()
    {
        Deaths = PlayerPrefs.GetInt(_levelKey, 0);
        OnDeathsChanged?.Invoke(Deaths);
    }

    /// <summary>Incrémente le compteur du niveau + le global, sauvegarde immédiate.</summary>
    public void AddDeath()
    {
        Deaths++;
        PlayerPrefs.SetInt(_levelKey, Deaths);

        int total = PlayerPrefs.GetInt(TOTAL_KEY, 0) + 1;
        PlayerPrefs.SetInt(TOTAL_KEY, total);

        PlayerPrefs.Save();
        OnDeathsChanged?.Invoke(Deaths);
    }

    public void ResetLevelDeaths()
    {
        Deaths = 0;
        PlayerPrefs.SetInt(_levelKey, 0);
        PlayerPrefs.Save();
        OnDeathsChanged?.Invoke(Deaths);
    }

    public void ResetTotalDeaths()
    {
        PlayerPrefs.SetInt(TOTAL_KEY, 0);
        PlayerPrefs.Save();
    }
}
