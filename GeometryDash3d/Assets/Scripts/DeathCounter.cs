using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathCounter : MonoBehaviour
{
    // === Singleton ===
    public static DeathCounter Instance { get; private set; }

    // Clés PlayerPrefs
    private string _levelKey;   // ex: "Deaths:Level:SampleScene"
    private const string TOTAL_KEY = "Deaths:TOTAL";

    // Valeurs courantes (du niveau en cours)
    public int Deaths { get; private set; } = 0;

    // Compteur global (lecture simple)
    public int TotalDeaths => PlayerPrefs.GetInt(TOTAL_KEY, 0);

    public System.Action<int> OnDeathsChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // On garde l'objet si tu charges plusieurs scènes (optionnel, utile si hub/menu)
        DontDestroyOnLoad(gameObject);

        // Abonne pour recharger à chaque scène
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Init pour la scène courante
        BuildKeysForActiveScene();
        LoadLevelDeaths();
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildKeysForActiveScene();
        LoadLevelDeaths();
    }

    private void BuildKeysForActiveScene()
    {
        // Clé par NIVEAU (par nom de scène)
        string sceneName = SceneManager.GetActiveScene().name;
        _levelKey = $"Deaths:Level:{sceneName}";
    }

    private void LoadLevelDeaths()
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

        PlayerPrefs.Save(); // on force l’écriture disque
        OnDeathsChanged?.Invoke(Deaths);
    }

    /// <summary>Remet à zéro UNIQUEMENT le niveau courant (si tu veux un bouton "Reset attempts").</summary>
    public void ResetLevelDeaths()
    {
        Deaths = 0;
        PlayerPrefs.SetInt(_levelKey, 0);
        PlayerPrefs.Save();
        OnDeathsChanged?.Invoke(Deaths);
    }

    /// <summary>Réinitialise le compteur global (rarement utilisé, à réserver à un menu d’options).</summary>
    public void ResetTotalDeaths()
    {
        PlayerPrefs.SetInt(TOTAL_KEY, 0);
        PlayerPrefs.Save();
    }
}
