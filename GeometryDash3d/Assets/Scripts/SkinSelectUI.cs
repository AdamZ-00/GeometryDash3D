using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinSelectUI : MonoBehaviour
{
    [Header("UI")]
    public Image previewImage;
    public TMP_Text nameLabel;
    public Button prevButton, nextButton, applyButton, backButton;

    [Header("Data (for UI only)")]
    [Tooltip("Optional icon per skin for the preview carousel.")]
    public Sprite[] skinIcons;
    [Tooltip("Optional display name per skin.")]
    public string[] skinNames;

    [Header("State")]
    public int currentIndex = 0;

    public const string KEY = "skin_index";

    void Awake()
    {
        if (prevButton) prevButton.onClick.AddListener(Prev);
        if (nextButton) nextButton.onClick.AddListener(Next);
        if (applyButton) applyButton.onClick.AddListener(Apply);
        if (backButton) backButton.onClick.AddListener(BackToMenu);
    }

    void OnEnable()
    {
        currentIndex = PlayerPrefs.GetInt(KEY, 0);
        Refresh();
    }

    public void Prev()
    {
        int total = Total();
        if (total == 0) return;
        currentIndex = (currentIndex - 1 + total) % total;
        Refresh();
    }

    public void Next()
    {
        int total = Total();
        if (total == 0) return;
        currentIndex = (currentIndex + 1) % total;
        Refresh();
    }

    public void Apply()
    {
        PlayerPrefs.SetInt(KEY, currentIndex);
        PlayerPrefs.Save();

        var applier = FindObjectOfType<ModelSwapSkinApplier>(true);
        if (applier) applier.ApplyIndex(currentIndex);
    }

    public void BackToMenu()
    {
        var smc = FindObjectOfType<SimpleMenuController>(true);
        if (smc) smc.Back();
    }

    void Refresh()
    {
        // Read count from the Applier (source of truth)
        var applier = FindObjectOfType<ModelSwapSkinApplier>(true);
        int total = applier ? applier.GetSkinCount() : 0;

        // label
        string label = $"Skin {currentIndex + 1}";
        if (skinNames != null && currentIndex < skinNames.Length && !string.IsNullOrEmpty(skinNames[currentIndex]))
            label = skinNames[currentIndex];
        if (nameLabel) nameLabel.text = label;

        // icon
        Sprite sp = null;
        if (skinIcons != null && currentIndex < skinIcons.Length)
            sp = skinIcons[currentIndex];
        if (previewImage)
        {
            previewImage.sprite = sp;
            previewImage.color = sp ? Color.white : new Color(1, 1, 1, 0.25f);
        }

        bool many = total > 1;
        if (prevButton) prevButton.interactable = many;
        if (nextButton) nextButton.interactable = many;
        if (applyButton) applyButton.interactable = total > 0;
    }

    int Total()
    {
        var applier = FindObjectOfType<ModelSwapSkinApplier>(true);
        return applier ? applier.GetSkinCount() : 0;
    }
}
