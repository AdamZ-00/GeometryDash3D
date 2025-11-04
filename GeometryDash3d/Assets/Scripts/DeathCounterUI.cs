using UnityEngine;
using TMPro;   // nécessite TextMeshPro (Window > Package Manager > TMP)

public class DeathCounterUI : MonoBehaviour
{
    [Header("Références")]
    public TextMeshProUGUI label;

    [Header("Affichage")]
    [Tooltip("Texte affiché. {0}=nombre de morts")]
    public string format = "ATTEMPTS: {0}";
    public bool uppercase = true;

    void Start()
    {
        if (!label) label = GetComponent<TextMeshProUGUI>();

        if (DeathCounter.Instance)
        {
            DeathCounter.Instance.OnDeathsChanged += UpdateLabel;
            UpdateLabel(DeathCounter.Instance.Deaths);
        }
        else
        {
            Debug.LogWarning("[DeathCounterUI] Aucune instance DeathCounter dans la scène.");
            UpdateLabel(0);
        }
    }

    void OnDestroy()
    {
        if (DeathCounter.Instance)
            DeathCounter.Instance.OnDeathsChanged -= UpdateLabel;
    }

    void UpdateLabel(int deaths)
    {
        var text = string.Format(format, deaths);
        label.text = uppercase ? text.ToUpperInvariant() : text;
    }
}
