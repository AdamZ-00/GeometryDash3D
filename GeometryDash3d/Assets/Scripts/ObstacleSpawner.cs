using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Références")]
    public GameObject obstaclePrefab;
    public GameObject jumpPadPrefab;
    public GameObject platformPrefab;

    [Header("Plage de spawn")]
    public int numberOfElements = 50;
    public float startZ = 15f;
    public float endZ   = 300f;

    // X aléatoire (ou lanes, voir option plus bas)
    public Vector2 xRange = new Vector2(-2f, 2f);

    [Header("Espacement / anti-chevauchement")]
    public float minSpacingZ = 2f;   // progression mini en Z entre deux essais
    public float maxSpacingZ = 10f;  // progression maxi en Z
    public float margin = 0.15f;     // marge d’air entre footprints

    // "Taille au sol" de chaque type (largeur en X, longueur en Z)
    // Ajuste en fonction de tes prefabs
    public Vector2 obstacleSize = new Vector2(1.0f, 1.0f);
    public Vector2 padSize      = new Vector2(1.2f, 1.0f);
    public Vector2 platformSize = new Vector2(2.0f, 2.0f);

    [Header("Hauteurs Y")]
    public float obstacleY  = 0.25f;
    public float padY       = 0.10f;
    public float platformY  = 1.5f;

    [Header("Options")]
    public int maxAttemptsPerElement = 20;
    public bool snapToLanes = false;
    public float[] lanesX = new float[] { -2f, 0f, 2f }; // utilisé si snapToLanes = true

    // --- interne ---
    struct Footprint
    {
        public Vector2 centerXZ;  // (x,z)
        public Vector2 halfSize;  // demi-largeur (x) / demi-longueur (z)
        public int type;          // 0=obstacle, 1=pad, 2=plateforme (si tu veux des règles spécifiques)
    }
    private List<Footprint> placed = new List<Footprint>();

    void Start()
    {
        float currentZ = startZ;

        for (int i = 0; i < numberOfElements; i++)
        {
            int attempts = 0;
            bool placedOk = false;

            while (attempts < maxAttemptsPerElement && !placedOk)
            {
                attempts++;

                // Avance le Z un peu à chaque essai pour éviter de rester bloqué
                currentZ += Random.Range(minSpacingZ, maxSpacingZ);
                if (currentZ > endZ) return;

                float x = snapToLanes ? lanesX[Random.Range(0, lanesX.Length)]
                                      : Random.Range(xRange.x, xRange.y);

                // 0 = obstacle, 1 = jump pad, 2 = plateforme
                int type = Random.Range(0, 3);
                GameObject prefab = obstaclePrefab;
                Vector2 half;
                float y;

                if (type == 1 && jumpPadPrefab != null)
                {
                    prefab = jumpPadPrefab;
                    half = padSize * 0.5f;
                    y = padY;
                }
                else if (type == 2 && platformPrefab != null)
                {
                    prefab = platformPrefab;
                    half = platformSize * 0.5f;
                    y = platformY;
                }
                else
                {
                    prefab = obstaclePrefab;
                    half = obstacleSize * 0.5f;
                    y = obstacleY;
                    type = 0;
                }

                Footprint candidate = new Footprint
                {
                    centerXZ = new Vector2(x, currentZ),
                    halfSize = half,
                    type = type
                };

                if (!OverlapsAnything(candidate, margin))
                {
                    // Pas de chevauchement : on instancie
                    Vector3 pos = new Vector3(x, y, currentZ);
                    Instantiate(prefab, pos, Quaternion.identity);
                    placed.Add(candidate);
                    placedOk = true;
                }
                // sinon : on boucle, on retente avec un nouveau Z/X
            }
        }
    }

    bool OverlapsAnything(Footprint a, float pad)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if (RectOverlap(a, placed[i], pad))
                return true;
        }
        return false;
    }

    // Test d’intersection de deux rectangles axis-aligned sur X/Z
    bool RectOverlap(Footprint a, Footprint b, float pad)
    {
        float dx = Mathf.Abs(a.centerXZ.x - b.centerXZ.x);
        float dz = Mathf.Abs(a.centerXZ.y - b.centerXZ.y);

        float allowX = a.halfSize.x + b.halfSize.x + pad;
        float allowZ = a.halfSize.y + b.halfSize.y + pad;

        return (dx < allowX) && (dz < allowZ);
    }
}