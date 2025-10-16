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
    public float margin      = 0.15f;                 // marge d’air entre footprints

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

    // --- interne ---
    struct Footprint
    {
        public Vector2 centerXZ;   // (x,z)
        public Vector2 halfSize;   // demi largeur/longueur
        public int type;           // 0=obstacle, 1=pad, 2=plateforme
    }
    private readonly List<Footprint> placed = new();

    // lanes calculées dynamiquement si useLanes = true
    private float[] lanesX;

    void Start()
    {
        // Nettoie un éventuel ancien spawn (pratique en Play/Stop)
        if (parentForSpawned != null)
        {
            for (int i = parentForSpawned.childCount - 1; i >= 0; i--)
                Destroy(parentForSpawned.GetChild(i).gameObject);
        }
        placed.Clear();

        // 1) Détermine la plage X en lisant la largeur du Ground (si fournie)
        Vector2 usableX = xRange;
        if (ground != null)
        {
            // On suppose une piste centrée en X=0 : largeur ≈ scale.x
            float half = ground.localScale.x * 0.5f;
            usableX = new Vector2(-half + edgePadding, half - edgePadding);
        }

        // 2) Construit les lanes si demandé
        if (useLanes)
            lanesX = BuildLanes(usableX, laneCount);

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
                if (currentZ > endZ) return;

                // choisi X
                float x = useLanes
                    ? lanesX[Random.Range(0, lanesX.Length)]
                    : Random.Range(usableX.x, usableX.y);

                // choisi type + taille + Y
                int type = Random.Range(0, 3);
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
                    prefab = obstaclePrefab; half = obstacleSize * 0.5f; y = obstacleY; type = 0;
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
                    placedOk = true;
                }
                // sinon : retente avec un autre Z/X
            }
        }
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
}