using UnityEngine;

public class ModelSwapSkinApplier : MonoBehaviour
{
    [Header("Mount for visual FBX")]
    public Transform visualMount;               // empty under Player where the FBX will be instantiated

    [Header("Model Prefabs (one per skin)")]
    public GameObject[] skinModelPrefabs;       // drag your FBX prefabs here

    [Header("Per-skin local overrides (optional)")]
    public bool useOverrides = false;
    public Vector3[] overrideLocalPositions;
    public Vector3[] overrideLocalEuler;
    public Vector3[] overrideLocalScales = null; // if empty/null -> keep prefab's scale

    public const string KEY = "skin_index";

    Transform _currentInstance;

    void Reset()
    {
        // Auto-create/find a "VisualMount"
        var t = transform.Find("VisualMount");
        if (!t)
        {
            var go = new GameObject("VisualMount");
            go.transform.SetParent(transform, false);
            t = go.transform;
        }
        visualMount = t;
    }

    void Awake()
    {
        if (!visualMount) Reset();
        // sécurité: garde le mount à l'identité
        visualMount.localPosition = Vector3.zero;
        visualMount.localRotation = Quaternion.identity;
        visualMount.localScale = Vector3.one;
    }

    void Start()
    {
        ApplySaved();
    }

    public void ApplySaved()
    {
        int idx = PlayerPrefs.GetInt(KEY, 0);
        ApplyIndex(idx);
    }

    public void ApplyIndex(int index)
    {
        if (!visualMount || skinModelPrefabs == null || skinModelPrefabs.Length == 0) return;

        index = Mathf.Clamp(index, 0, skinModelPrefabs.Length - 1);

        // Destroy previous instance
        if (_currentInstance)
        {
            if (Application.isPlaying) Destroy(_currentInstance.gameObject);
            else DestroyImmediate(_currentInstance.gameObject);
            _currentInstance = null;
        }

        var prefab = skinModelPrefabs[index];
        if (!prefab)
        {
            Debug.LogWarning($"[ModelSwapSkinApplier] Skin index {index} est NULL dans skinModelPrefabs.");
            return;
        }

        var inst = Instantiate(prefab, visualMount); // parented under the mount

        // Par défaut : reprendre la TRS locale du prefab
        inst.transform.localPosition = prefab.transform.localPosition;
        inst.transform.localRotation = prefab.transform.localRotation;
        inst.transform.localScale = prefab.transform.localScale;

        // Overrides optionnels
        if (useOverrides)
        {
            if (overrideLocalPositions != null && index < overrideLocalPositions.Length)
                inst.transform.localPosition = overrideLocalPositions[index];

            if (overrideLocalEuler != null && index < overrideLocalEuler.Length)
                inst.transform.localRotation = Quaternion.Euler(overrideLocalEuler[index]);

            if (overrideLocalScales != null && index < overrideLocalScales.Length && overrideLocalScales[index] != Vector3.zero)
                inst.transform.localScale = overrideLocalScales[index];
        }

        // Dernières sécurités : évite les NaN ou scale nulle
        if (inst.transform.localScale == Vector3.zero)
            inst.transform.localScale = Vector3.one;

        _currentInstance = inst.transform;
    }

    public int GetSkinCount() => skinModelPrefabs != null ? skinModelPrefabs.Length : 0;
}
