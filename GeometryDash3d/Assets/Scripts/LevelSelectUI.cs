using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UI de sélection de niveaux : image + nom + flèches + Play.
/// S’appuie sur LevelLoader.levelPrefabs.Length pour le nombre total.
/// Les sprites/noms sont fournis ici (1 par index).
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image previewImage;       // l’image d’aperçu (Sprite)
    public TMP_Text nameLabel;         // nom (ou TMP_Text si tu utilises TMP)
    public Button prevButton;
    public Button nextButton;
    public Button playButton;
    public Button backButton;

    [Header("Data")]
    [Tooltip("Sprites d’aperçu par index de niveau (peuvent contenir des null, c’est ok).")]
    public Sprite[] levelSprites;
    [Tooltip("Noms affichés par index de niveau (si vide ou pas assez long -> 'Level X').")]
    public string[] levelNames;

    [Header("Options")]
    [Tooltip("Index actuel du carrousel (0-based). Sera initialisé depuis PlayerPrefs.")]
    public int currentIndex = 0;

    const string KEY_SELECTED = "selected_level_index";

    void Awake()
    {
        // Hook des boutons si non branchés dans l’Inspector
        if (prevButton) prevButton.onClick.AddListener(Prev);
        if (nextButton) nextButton.onClick.AddListener(Next);
        if (playButton) playButton.onClick.AddListener(PlaySelected);
        if (backButton) backButton.onClick.AddListener(BackToMenu);
    }

    void Start()
    {
        RefreshFromPrefs(); // initialise currentIndex + UI
    }

    public void RefreshFromPrefs()
    {
        int saved = PlayerPrefs.GetInt(KEY_SELECTED, 0);
        SetIndex(saved);
    }

    public void Prev()
    {
        int total = GetTotalLevels();
        if (total <= 0) return;

        int i = (currentIndex - 1 + total) % total;
        SetIndex(i);
    }

    public void Next()
    {
        int total = GetTotalLevels();
        if (total <= 0) return;

        int i = (currentIndex + 1) % total;
        SetIndex(i);
    }

    public void SetIndex(int i)
    {
        int total = GetTotalLevels();
        if (total <= 0) { currentIndex = 0; UpdatePreview(); return; }

        currentIndex = Mathf.Clamp(i, 0, total - 1);
        PlayerPrefs.SetInt(KEY_SELECTED, currentIndex);
        PlayerPrefs.Save();

        UpdatePreview();
    }

    void UpdatePreview()
    {
        // Nom
        string label = $"Level {currentIndex + 1}";
        if (levelNames != null && currentIndex < levelNames.Length && !string.IsNullOrEmpty(levelNames[currentIndex]))
            label = levelNames[currentIndex];
        if (nameLabel) nameLabel.text = label;

        // Image
        Sprite sp = null;
        if (levelSprites != null && currentIndex < levelSprites.Length)
            sp = levelSprites[currentIndex];
        if (previewImage)
        {
            previewImage.sprite = sp;
            previewImage.color = sp ? Color.white : new Color(1, 1, 1, 0.2f); // léger fade si pas d'image
        }

        // (option) désactiver flèches si 1 seul niveau
        int total = GetTotalLevels();
        bool many = total > 1;
        if (prevButton) prevButton.interactable = many;
        if (nextButton) nextButton.interactable = many;
    }

    public void PlaySelected()
    {
        // On garde l’index choisi dans PlayerPrefs
        PlayerPrefs.SetInt(KEY_SELECTED, currentIndex);
        PlayerPrefs.SetInt("auto_play_once", 1); // on veut démarrer direct en jeu
        PlayerPrefs.Save();

        // Stop la musique du menu si besoin
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.StopMusic();

        // Recharge la même scène (le LevelLoader lira l’index et instanciera le bon prefab)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        var smc = FindObjectOfType<SimpleMenuController>(true);
        if (smc) smc.Back();
    }

    int GetTotalLevels()
    {
        var loader = FindObjectOfType<LevelLoader>(true);
        if (loader == null || loader.levelPrefabs == null) return 0;
        return loader.levelPrefabs.Length;
    }
}
