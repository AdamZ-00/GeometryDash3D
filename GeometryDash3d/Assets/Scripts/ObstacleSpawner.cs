using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Sol/piste. Sert à lire la largeur pour adapter le spawn.")]
    public Transform ground;                         // mets ton Ground ici (ou laisse vide si tu préfères xRange manuel)
    public Transform parentForSpawned;               // optionnel : parent pour ranger les spawns

    public GameObject obstaclePrefab;
    public GameObject jumpPadPrefab;
    public GameObject platformPrefab;

    [Tooltip("Obstacle plus petit (tiny). Laisser vide si non utilisé.")]
    public GameObject tinyObstaclePrefab;
    [Range(0f, 1f)] public float tinyChance = 0.5f;


    [Header("Z (avant/arrière)")]
    public int   numberOfElements = 50;
    public float startZ = 15f;
    public float endZ   = 300f;

    [Header("X (gauche/droite)")]
    [Tooltip("Laisser vide -> auto depuis le Ground. Sinon utilisé en fallback.")]
    public Vector2 xRange = new Vector2(-2f, 2f);

    [Tooltip("Marge depuis les bords de la piste")]
    public float edgePadding = 0.4f;

    [Header("Lanes (optionnel)")]
    public bool  useLanes = true;
    [Range(2, 7)] public int laneCount = 3;          // nb de voies si useLanes = true

    [Header("Espacement / anti-chevauchement")]
    public float minSpacingZ = 2f;
    public float maxSpacingZ = 10f;
    public float margin      = 0.15f;                // marge d’air entre footprints

    [Header("Taille au sol (X,Z)")]
    public Vector2 obstacleSize = new Vector2(1.0f, 1.0f);
    public Vector2 padSize      = new Vector2(1.2f, 1.0f);
    public Vector2 platformSize = new Vector2(2.0f, 2.0f);

    [Header("Hauteurs Y")]
    public float obstacleY  = 0.25f;
    public float padY       = 0.10f;
    public float platformY  = 1.5f;

    [Header("Essais")]
    [Tooltip("Nb max d'essais pour placer un élément sans chevauchement.")]
    public int maxAttemptsPerElement = 20;

    [Header("Probabilités")]
    [Range(0f, 1f)] public float padChance = 0.33f;        // probabilité brute pad
    [Range(0f, 1f)] public float platformChance = 0.33f;   // probabilité brute plateforme (le reste = obstacles)

    [Header("Contraintes minimales")]
    [Tooltip("Force au moins un JumpPad tous les N éléments (0 = désactivé).")]
    public int forcePadEveryN = 0;

    // --- interne ---
    struct Footprint
    {
        public Vector2 centerXZ;   // (x,z)
        public Vector2 halfSize;   // demi largeur/longueur
        public int type;           // 0=obstacle, 1=pad, 2=plateforme
    }

    private readonly List<Footprint> placed = new List<Footprint>();
    private float[] lanesX;

    // Compteurs debug
    private int _countObs, _countPads, _countPlatforms;

    void Start()
    {
        // Nettoyage éventuels anciens spawns (pratique en Play/Stop)
        if (parentForSpawned != null)
        {
            for (int i = parentForSpawned.childCount - 1; i >= 0; i--)
                Destroy(parentForSpawned.GetChild(i).gameObject);
        }
        placed.Clear();
        _countObs = _countPads = _countPlatforms = 0;

        // Warnings si prefabs non assignés
        if (jumpPadPrefab == null) Debug.LogWarning("[ObstacleSpawner] jumpPadPrefab n'est pas assigné !");
        if (platformPrefab == null) Debug.LogWarning("[ObstacleSpawner] platformPrefab n'est pas assigné !");
        if (obstaclePrefab == null) Debug.LogError("[ObstacleSpawner] obstaclePrefab est manquant !");

        // 1) Détermine la plage X en lisant la largeur du Ground (si fournie)
        Vector2 usableX = xRange;
        if (ground != null)
        {
            // Hypothèse : piste centrée en X=0, largeur ≈ scale.x
            float half = ground.localScale.x * 0.5f;
            usableX = new Vector2(-half + edgePadding, half - edgePadding);
        }

        // 2) Construit les lanes si demandé
        if (useLanes)
            lanesX = BuildLanes(usableX, laneCount);
        else
            lanesX = null;

        // 3) Spawn
        float currentZ = startZ;

        for (int i = 0; i < numberOfElements; i++)
        {
            int attempts = 0;
            bool placedOk = false;

            while (attempts < maxAttemptsPerElement && !placedOk)
            {
                attempts++;

                // avance en Z
                currentZ += Random.Range(minSpacingZ, maxSpacingZ);
                if (currentZ > endZ)
                {
                    // Fin de piste
                    Debug.Log($"[ObstacleSpawner] Piste terminée à Z={currentZ:F2}. Spawns -> Obstacles: {_countObs}, Pads: {_countPads}, Plateformes: {_countPlatforms}");
                    return;
                }

                // choisi X
                float x = useLanes
                    ? lanesX[Random.Range(0, lanesX.Length)]
                    : Random.Range(usableX.x, usableX.y);

                // choisi type + taille + Y
                int type = PickTypeNormalized();

                // Option de forçage d'un pad
                if (forcePadEveryN > 0 && (i + 1) % forcePadEveryN == 0)
                    type = 1; // pad

                GameObject prefab;
                Vector2 half;
                float y;

                if (type == 1 && jumpPadPrefab != null)
                {
                    prefab = jumpPadPrefab; half = padSize * 0.5f; y = padY;
                }
                else if (type == 2 && platformPrefab != null)
                {
                    prefab = platformPrefab; half = platformSize * 0.5f; y = platformY;
                }
                else
                {
                    // 50% de chance de tiny si dispo
                    if (tinyObstaclePrefab != null && Random.value < tinyChance)
                        prefab = tinyObstaclePrefab;
                    else
                        prefab = obstaclePrefab;

                        half = obstacleSize * 0.5f; y = obstacleY; type = 0;
                }


                var candidate = new Footprint
                {
                    centerXZ = new Vector2(x, currentZ),
                    halfSize = half,
                    type     = type
                };

                if (!OverlapsAnything(candidate, margin))
                {
                    // instancie
                    Vector3 pos = new Vector3(x, y, currentZ);
                    var go = Instantiate(prefab, pos, Quaternion.identity);
                    if (parentForSpawned) go.transform.SetParent(parentForSpawned, true);

                    placed.Add(candidate);

                    // compteurs
                    if (type == 1) _countPads++;
                    else if (type == 2) _countPlatforms++;
                    else _countObs++;

                    placedOk = true;
                }
                // sinon : retente avec un autre Z/X
            }
        }

        Debug.Log($"[ObstacleSpawner] Spawns -> Obstacles: {_countObs}, Pads: {_countPads}, Plateformes: {_countPlatforms}");
    }

    /// <summary>
    /// Retourne 0=obstacle, 1=pad, 2=plateforme, avec normalisation des chances.
    /// Si padChance + platformChance > 1, on tronque à 1. Le reste = obstacles.
    /// </summary>
    int PickTypeNormalized()
    {
        float pPad = Mathf.Clamp01(padChance);
        float pPlat = Mathf.Clamp01(platformChance);
        float total = Mathf.Min(1f, pPad + pPlat);

        float r = Random.value;
        if (r < pPad) return 1; // pad
        else if (r < total) return 2; // plateforme
        return 0; // obstacle
    }

    // Construit des lanes également espacées entre usableX.x et usableX.y
    float[] BuildLanes(Vector2 usableX, int count)
    {
        count = Mathf.Max(2, count);
        float[] lanes = new float[count];
        float span = usableX.y - usableX.x;

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (i / (float)(count - 1));   // 0..1
            lanes[i] = usableX.x + t * span;
        }
        return lanes;
    }

    bool OverlapsAnything(Footprint a, float pad)
    {
        for (int i = 0; i < placed.Count; i++)
            if (RectOverlap(a, placed[i], pad)) return true;
        return false;
    }

    // test intersection rectangles axis-aligned (X/Z)
    bool RectOverlap(Footprint a, Footprint b, float pad)
    {
        float dx = Mathf.Abs(a.centerXZ.x - b.centerXZ.x);
        float dz = Mathf.Abs(a.centerXZ.y - b.centerXZ.y);

        float allowX = a.halfSize.x + b.halfSize.x + pad;
        float allowZ = a.halfSize.y + b.halfSize.y + pad;

        return (dx < allowX) && (dz < allowZ);
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG/Spawn 1 JumpPad ici")]
    void DebugSpawnOnePad()
    {
        if (jumpPadPrefab == null) { Debug.LogWarning("[ObstacleSpawner] Aucun jumpPadPrefab assigné."); return; }

        // X = voie centrale si lanes dispo, sinon 0
        float x;
        if (useLanes && lanesX != null && lanesX.Length > 0)
            x = lanesX[Mathf.Clamp(lanesX.Length / 2, 0, Mathf.Max(0, lanesX.Length - 1))];
        else
            x = 0f;

        float z = Mathf.Clamp((startZ + endZ) * 0.5f, startZ, endZ - 1f);
        var go = Instantiate(jumpPadPrefab, new Vector3(x, padY, z), Quaternion.identity);
        if (parentForSpawned) go.transform.SetParent(parentForSpawned, true);
        Debug.Log("[ObstacleSpawner] JumpPad de test instancié.");
    }
#endif

    // Optionnel : assurer des valeurs cohérentes dès l'Inspector
    void OnValidate()
    {
        if (maxAttemptsPerElement < 1) maxAttemptsPerElement = 1;
        if (numberOfElements < 0) numberOfElements = 0;
        if (endZ < startZ) endZ = startZ;
        if (laneCount < 2) laneCount = 2;
        if (minSpacingZ < 0.01f) minSpacingZ = 0.01f;
        if (maxSpacingZ < minSpacingZ) maxSpacingZ = minSpacingZ;
        if (forcePadEveryN < 0) forcePadEveryN = 0;
    }
}