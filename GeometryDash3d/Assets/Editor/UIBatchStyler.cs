#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIBatchStyler : EditorWindow
{
    // Ciblage
    Transform root;

    // Couleurs Button
    Color normalCol = new Color32(224, 224, 224, 255); // #E0E0E0
    Color highlightedCol = new Color32(255, 255, 255, 255); // #FFFFFF
    Color pressedCol = new Color32(128, 207, 255, 255); // #80CFFF
    Color disabledCol = new Color32(85, 85, 85, 255);    // #555555
    float fadeDuration = 0.15f;

    // Image de fond (arrondis)
    Sprite roundedSprite;   // Sprite 9-sliced recommandé
    bool setSliced = true;

    // Texte TMP effets
    bool addShadow = true;
    Vector2 shadowDistance = new Vector2(2f, -2f);
    Color shadowColor = new Color(0, 0, 0, 0.25f); // #00000040 ~ 0.25 alpha

    bool addOutline = false;
    Color outlineColor = new Color(0, 0, 0, 0.6f);
    float outlineWidth = 0.2f; // TMP Outline width 0..1

    // Hover scale
    bool addHover = true;
    float hoverScale = 1.1f;
    float hoverSpeed = 10f;

    // SFX
    bool addClickSfx = true;
    AudioClip clickClip;
    float clickVolume = 0.8f;

    [MenuItem("Tools/UI/Style Buttons...")]
    public static void Open()
    {
        var w = GetWindow<UIBatchStyler>("UI Batch Styler");
        w.minSize = new Vector2(360, 520);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        root = (Transform)EditorGUILayout.ObjectField("Root (Canvas/Parent)", root, typeof(Transform), true);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Button Colors (ColorTint)", EditorStyles.boldLabel);
        normalCol = EditorGUILayout.ColorField("Normal", normalCol);
        highlightedCol = EditorGUILayout.ColorField("Highlighted", highlightedCol);
        pressedCol = EditorGUILayout.ColorField("Pressed", pressedCol);
        disabledCol = EditorGUILayout.ColorField("Disabled", disabledCol);
        fadeDuration = EditorGUILayout.FloatField("Fade Duration", fadeDuration);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Rounded Background", EditorStyles.boldLabel);
        roundedSprite = (Sprite)EditorGUILayout.ObjectField("Rounded Sprite", roundedSprite, typeof(Sprite), false);
        setSliced = EditorGUILayout.Toggle("Set Image Type = Sliced", setSliced);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("TMP Text Effects", EditorStyles.boldLabel);
        addShadow = EditorGUILayout.Toggle("Add Shadow", addShadow);
        if (addShadow)
        {
            shadowDistance = EditorGUILayout.Vector2Field("Shadow Distance", shadowDistance);
            shadowColor = EditorGUILayout.ColorField("Shadow Color", shadowColor);
        }
        addOutline = EditorGUILayout.Toggle("Add Outline", addOutline);
        if (addOutline)
        {
            outlineColor = EditorGUILayout.ColorField("Outline Color", outlineColor);
            outlineWidth = EditorGUILayout.Slider("Outline Width", outlineWidth, 0f, 1f);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Hover Scale", EditorStyles.boldLabel);
        addHover = EditorGUILayout.Toggle("Add UIButtonHover", addHover);
        if (addHover)
        {
            hoverScale = EditorGUILayout.Slider("Scale Factor", hoverScale, 1.0f, 1.5f);
            hoverSpeed = EditorGUILayout.Slider("Lerp Speed", hoverSpeed, 5f, 20f);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Click SFX", EditorStyles.boldLabel);
        addClickSfx = EditorGUILayout.Toggle("Add UIButtonSfx", addClickSfx);
        if (addClickSfx)
        {
            clickClip = (AudioClip)EditorGUILayout.ObjectField("Click Clip", clickClip, typeof(AudioClip), false);
            clickVolume = EditorGUILayout.Slider("Volume", clickVolume, 0f, 1f);
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("APPLY TO ALL BUTTONS UNDER ROOT", GUILayout.Height(36)))
        {
            if (!root)
            {
                EditorUtility.DisplayDialog("UI Batch Styler", "Assigne un Root (Canvas ou parent).", "OK");
            }
            else
            {
                Apply();
            }
        }
    }

    void Apply()
    {
        var buttons = root.GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length == 0)
        {
            EditorUtility.DisplayDialog("UI Batch Styler", "Aucun Button trouvé sous le Root.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int ug = Undo.GetCurrentGroup();

        foreach (var btn in buttons)
        {
            Undo.RecordObject(btn, "Style Button");

            // Transition colors
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = normalCol;
            colors.highlightedColor = highlightedCol;
            colors.pressedColor = pressedCol;
            colors.disabledColor = disabledCol;
            colors.fadeDuration = fadeDuration;
            btn.colors = colors;

            // Background Image arrondi
            var img = btn.GetComponent<Image>();
            if (img)
            {
                Undo.RecordObject(img, "Style Button Image");
                if (roundedSprite) img.sprite = roundedSprite;
                if (setSliced && img.sprite && img.type != Image.Type.Sliced)
                    img.type = Image.Type.Sliced;
            }

            // TMP effects (sur TextMeshProUGUI enfants)
            var tmps = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                Undo.RecordObject(tmp, "Style TMP");
                if (addShadow)
                {
                    var sh = tmp.GetComponent<Shadow>();
                    if (!sh) sh = Undo.AddComponent<Shadow>(tmp.gameObject);
                    sh.effectDistance = shadowDistance;
                    sh.effectColor = shadowColor;
                }
                if (addOutline)
                {
                    // TMP a son propre Outline via material (Outline Width/Color)
                    tmp.outlineWidth = outlineWidth;
                    tmp.outlineColor = outlineColor;
                }
            }

            // Hover
            if (addHover)
            {
                var hover = btn.GetComponent<UIButtonHover>();
                if (!hover) hover = Undo.AddComponent<UIButtonHover>(btn.gameObject);
                hover.scaleFactor = hoverScale;
                hover.speed = hoverSpeed;
            }

            // Click SFX
            if (addClickSfx)
            {
                var sfx = btn.GetComponent<UIButtonSfx>();
                if (!sfx) sfx = Undo.AddComponent<UIButtonSfx>(btn.gameObject);
                sfx.clickClip = clickClip;
                sfx.volume = clickVolume;
            }

            EditorUtility.SetDirty(btn);
        }

        Undo.CollapseUndoOperations(ug);
        Debug.Log($"[UIBatchStyler] Stylé {buttons.Length} Button(s) sous '{root.name}'.");
    }
}
#endif
