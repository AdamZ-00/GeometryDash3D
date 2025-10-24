using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Génère une piste de type Geometry Dash 3D en posant aléatoirement :
/// - des obstacles (triangles, etc.)
/// - des JumpPads carrés (saut auto)
/// - des plateformes en hauteur
/// - des JumpPads interactifs "orbe" (JumpPadInt) qui demandent un input
///
/// Le spawner tient compte :
/// - des probabilités par type
/// - des "lanes" (voies X) optionnelles
/// - d'un espacement aléatoire en Z et d'une marge d'air (anti-chevauchement)
/// - d'un offset Y par type (pour faire flotter l'orbe)
///
/// Renseigne ce composant sur un GameObject vide, puis assigne les prefabs
/// dans l'Inspector.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // Références de prefabs et de transforms utiles
    // ---------------------------------------------------------------------
    [Header("Références")]
    [Tooltip("Sol/piste. Sert à lire la largeur pour adapter le spawn (xRange automatique). Si laissé vide, on utilisera xRange manuel.")]
    public Transform ground;                         // optionnel : lit la largeur de la piste
    [Tooltip("Parent pour ranger proprement les éléments instanciés (facultatif).")]
    public Transform parentForSpawned;               // optionnel : range les spawns dans la hiérarchie

    [Tooltip("Obstacle 'par défaut' (ex. pyramide triangle).")]
    public GameObject obstaclePrefab;

    [Tooltip("JumpPad classique (saut automatique).")]
    public GameObject jumpPadPrefab;

    [Tooltip("Plateforme en hauteur sur laquelle on peut atterrir.")]
    public GameObject platformPrefab;

    [Tooltip("JumpPad interactif (orbe). Nécessite un input quand le joueur le traverse.")]
    public GameObject jumpPadIntPrefab;

    [Tooltip("Variante d'obstacle plus petit (tiny). Laisser vide si non utilisée.")]
    public GameObject tinyObstaclePrefab;
    [Range(0f, 1f)] public float tinyChance = 0.5f;

    // ---------------------------------------------------------------------
    // Étendue Z et X de la piste
    // ---------------------------------------------------------------------
    [Header("Z (avant/arrière)")]
    [Tooltip("Nombre d’éléments à tenter de poser entre StartZ et EndZ.")]
    public int numberOfElements = 50;

    [Tooltip("Début de la piste (Z).")]
    public float startZ = 15f;

    [Tooltip("Fin de la piste (Z).")]
    public float endZ = 300f;

    [Header("X (gauche/droite)")]
    [Tooltip("Plage X manuelle si 'ground' est vide. Si 'ground' est renseigné, on déduit la plage depuis sa largeur.")]
    public Vector2 xRange = new Vector2(-2f, 2f);

    [Tooltip("Marge d'air depuis les bords de la piste.")]
    public float edgePadding = 0.4f;

    // ---------------------------------------------------------------------
    // Lanes (voies X) optionnelles
    // ---------------------------------------------------------------------
    [Header("Lanes (optionnel)")]
    [Tooltip("Si activé, on place les éléments sur des voies X fixes (plus 'arcade').")]
    public bool useLanes = true;

    [Tooltip("Nombre de voies quand Lanes est activé.")]
    [Range(2, 7)] public int laneCount = 3;

    // ---------------------------------------------------------------------
    // Gestion des espacements et anti-chevauchements
    // ---------------------------------------------------------------------
    [Header("Espacement / anti-chevauchement")]
    [Tooltip("Espacement minimal aléatoire entre 2 éléments consécutifs (en Z).")]
    public float minSpacingZ = 2f;

    [Tooltip("Espacement maximal aléatoire entre 2 éléments consécutifs (en Z).")]
    public float maxSpacingZ = 10f;

    [Tooltip("Marge d’air supplémentaire entre 'footprints' (rectangles X/Z).")]
    public float margin = 0.15f;

    // ---------------------------------------------------------------------
    // Tailles et hauteurs par type
    // ---------------------------------------------------------------------
    [Header("Taille au sol (X,Z)")]
    [Tooltip("Footprint des obstacles standards (rectangles en X/Z).")]
    public Vector2 obstacleSize = new Vector2(1.0f, 1.0f);

    [Tooltip("Footprint des JumpPads (carrés/orbes).")]
    public Vector2 padSize = new Vector2(1.2f, 1.0f);

    [Tooltip("Footprint des plateformes.")]
    public Vector2 platformSize = new Vector2(2.0f, 2.0f);

    [Header("Hauteurs Y")]
    [Tooltip("Hauteur (Y) des obstacles déposés sur le sol.")]
    public float obstacleY = 0.25f;

    [Tooltip("Hauteur (Y) des JumpPads carrés.")]
    public float padY = 0.10f;

    [Tooltip("Hauteur (Y) des plateformes en hauteur.")]
    public float platformY = 1.5f;

    [Tooltip("Hauteur supplémentaire pour l'orbe (JumpPadInt) afin qu'il flotte un peu.")]
    public float padIntExtraY = 0.5f;

    // ---------------------------------------------------------------------
    // Contrôles supplémentaires
    // ---------------------------------------------------------------------
    [Header("Essais")]
    [Tooltip("Nombre maximum d’essais de placement pour un élément avant d'abandonner (éviter chevauchement).")]
    public int maxAttemptsPerElement = 20;

    [Header("Probabilités")]
    [Tooltip("Probabilité brute d'apparition d'un JumpPad 'carré' (0..1).")]
    [Range(0f, 1f)] public float padChance = 0.25f;

    [Tooltip("Probabilité brute d'apparition d'un JumpPad interactif 'orbe' (0..1).")]
    [Range(0f, 1f)] public float padIntChance = 0.15f;

    [Tooltip("Probabilité brute d'apparition d'une plateforme (0..1). Le reste = obstacles/tiny.")]
    [Range(0f, 1f)] public float platformChance = 0.25f;

    [Header("Contraintes minimales")]
    [Tooltip("Force au moins un JumpPad 'carré' tous les N éléments (0 = désactivé).")]
    public int forcePadEveryN = 0;

    // ---------------------------------------------------------------------
    // Types internes et stockage des footprints
    // ---------------------------------------------------------------------
    struct Footprint
    {
        public Vector2 centerXZ;   // centre (x,z)
        public Vector2 halfSize;   // demi-largeur/demi-longueur (en X/Z)
        public int type;           // 0 = obstacle, 1 = pad, 2 = plateforme, 3 = padInt
    }

    private readonly List<Footprint> placed = new List<Footprint>();
    private float[] lanesX;

    // Compteurs informatifs
    private int _countObs, _countPads, _countPlatforms;

    // =====================================================================
    // Cycle principal
    // =====================================================================
    void Start()
    {
        // Nettoie un parent éventuel (utile quand on Play/Stop souvent)
        if (parentForSpawned != null)
        {
            for (int i = parentForSpawned.childCount - 1; i >= 0; i--)
                Destroy(parentForSpawned.GetChild(i).gameObject);
        }
        placed.Clear();
        _countObs = _countPads = _countPlatforms = 0;

        // Sanity checks
        if (jumpPadPrefab == null) Debug.LogWarning("[ObstacleSpawner] jumpPadPrefab n'est pas assigné !");
        if (platformPrefab == null) Debug.LogWarning("[ObstacleSpawner] platformPrefab n'est pas assigné !");
        if (obstaclePrefab == null) Debug.LogError("[ObstacleSpawner] obstaclePrefab est manquant !");

        // 1) Déduire une plage X depuis le Ground si fourni
        Vector2 usableX = xRange;
        if (ground != null)
        {
            // Hypothèse : la piste est centrée en X=0, et sa "largeur" provient de son scale.x
            float half = ground.localScale.x * 0.5f;
            usableX = new Vector2(-half + edgePadding, half - edgePadding);
        }

        // 2) Construire des lanes si demandé
        lanesX = useLanes ? BuildLanes(usableX, laneCount) : null;

        // 3) Boucle de spawn
        float currentZ = startZ;

        for (int i = 0; i < numberOfElements; i++)
        {
            int attempts = 0;
            bool placedOk = false;

            while (attempts < maxAttemptsPerElement && !placedOk)
            {
                attempts++;

                // avance en Z d'un pas aléatoire
                currentZ += Random.Range(minSpacingZ, maxSpacingZ);
                if (currentZ > endZ)
                {
                    Debug.Log($"[ObstacleSpawner] Piste terminée à Z={currentZ:F2}. Spawns -> Obstacles: {_countObs}, Pads: {_countPads}, Plateformes: {_countPlatforms}");
                    return;
                }

                // choisir X : soit depuis les lanes, soit free-range
                float x = useLanes
                    ? lanesX[Random.Range(0, lanesX.Length)]
                    : Random.Range(usableX.x, usableX.y);

                // choisir type
                int type = PickTypeNormalized();

                // forcer un JumpPad classique tous les N éléments si demandé
                if (forcePadEveryN > 0 && (i + 1) % forcePadEveryN == 0)
                    type = 1;

                // déterminer le prefab, la footprint (halfSize) et la hauteur Y
                GameObject prefab;
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
                else if (type == 3 && jumpPadIntPrefab != null)
                {
                    prefab = jumpPadIntPrefab;
                    half = padSize * 0.5f;
                    y = padY + padIntExtraY; // l'orbe flotte un peu
                }
                else
                {
                    // obstable standard (avec tiny éventuel)
                    prefab = (tinyObstaclePrefab != null && Random.value < tinyChance)
                        ? tinyObstaclePrefab
                        : obstaclePrefab;

                    half = obstacleSize * 0.5f;
                    y = obstacleY;
                    type = 0; // on recadre sur 'obstacle'
                }

                // Footprint candidate pour l'anti-chevauchement
                var candidate = new Footprint
                {
                    centerXZ = new Vector2(x, currentZ),
                    halfSize = half,
                    type = type
                };

                // Test d'intersection avec ce qui a déjà été posé
                if (!OverlapsAnything(candidate, margin))
                {
                    // Instanciation
                    Vector3 pos = new Vector3(x, y, currentZ);
                    var go = Instantiate(prefab, pos, Quaternion.identity);
                    if (parentForSpawned) go.transform.SetParent(parentForSpawned, true);

                    placed.Add(candidate);

                    // Compteurs
                    if (type == 1 || type == 3) _countPads++;
                    else if (type == 2) _countPlatforms++;
                    else _countObs++;

                    placedOk = true; // on sort du while -> élément placé
                }
                // sinon : on boucle, on retentera un autre Z (et potentiellement un autre X)
            }
        }

        Debug.Log($"[ObstacleSpawner] Spawns -> Obstacles: {_countObs}, Pads: {_countPads}, Plateformes: {_countPlatforms}");
    }

    // ---------------------------------------------------------------------
    // Choix du type : 0=obstacle, 1=pad, 2=platforme, 3=padInt
    // La somme des probabilités est plafonnée à 1. Le "reste" = obstacles.
    // ---------------------------------------------------------------------
    int PickTypeNormalized()
    {
        float pPad = Mathf.Clamp01(padChance);
        float pPadInt = Mathf.Clamp01(padIntChance);
        float pPlat = Mathf.Clamp01(platformChance);
        float total = Mathf.Min(1f, pPad + pPadInt + pPlat);

        float r = Random.value;

        if (r < pPad) return 1; // JumpPad carré (auto)
        else if (r < pPad + pPadInt) return 3; // Orbe interactif (input)
        else if (r < total) return 2; // Plateforme
        return 0;                                  // Obstacle
    }

    // ---------------------------------------------------------------------
    // Construit 'count' voies X régulièrement espacées entre usableX.x et usableX.y
    // ---------------------------------------------------------------------
    float[] BuildLanes(Vector2 usableX, int count)
    {
        count = Mathf.Max(2, count);
        float[] lanes = new float[count];

        float span = usableX.y - usableX.x;
        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (i / (float)(count - 1)); // 0..1
            lanes[i] = usableX.x + t * span;
        }
        return lanes;
    }

    // ---------------------------------------------------------------------
    // Détecte si 'a' chevauche un des footprints déjà posés (avec marge)
    // ---------------------------------------------------------------------
    bool OverlapsAnything(Footprint a, float pad)
    {
        for (int i = 0; i < placed.Count; i++)
        {
            if (RectOverlap(a, placed[i], pad))
                return true;
        }
        return false;
    }

    // ---------------------------------------------------------------------
    // Test d'intersection de deux rectangles axis-aligned sur le plan X/Z
    // ---------------------------------------------------------------------
    bool RectOverlap(Footprint a, Footprint b, float pad)
    {
        float dx = Mathf.Abs(a.centerXZ.x - b.centerXZ.x);
        float dz = Mathf.Abs(a.centerXZ.y - b.centerXZ.y);

        float allowX = a.halfSize.x + b.halfSize.x + pad;
        float allowZ = a.halfSize.y + b.halfSize.y + pad;

        return (dx < allowX) && (dz < allowZ);
    }

    // ---------------------------------------------------------------------
    // Utilitaire debug pratique dans l'Inspector (menu contextuel)
    // ---------------------------------------------------------------------
#if UNITY_EDITOR
    [ContextMenu("DEBUG/Spawn 1 JumpPad ici")]
    void DebugSpawnOnePad()
    {
        if (jumpPadPrefab == null)
        {
            Debug.LogWarning("[ObstacleSpawner] Aucun jumpPadPrefab assigné.");
            return;
        }

        // X = voie centrale si lanes dispo, sinon 0
        float x;
        if (useLanes && lanesX != null && lanesX.Length > 0)
            x = lanesX[Mathf.Clamp(lanesX.Length / 2, 0, lanesX.Length - 1)];
        else
            x = 0f;

        // milieu de piste
        float z = Mathf.Clamp((startZ + endZ) * 0.5f, startZ, endZ - 1f);
        var go = Instantiate(jumpPadPrefab, new Vector3(x, padY, z), Quaternion.identity);
        if (parentForSpawned) go.transform.SetParent(parentForSpawned, true);

        Debug.Log("[ObstacleSpawner] JumpPad de test instancié.");
    }
#endif

    // ---------------------------------------------------------------------
    // Garde des valeurs cohérentes dès l'Inspector
    // ---------------------------------------------------------------------
    void OnValidate()
    {
        if (maxAttemptsPerElement < 1) maxAttemptsPerElement = 1;
        if (numberOfElements < 0) numberOfElements = 0;
        if (endZ < startZ) endZ = startZ;

        if (laneCount < 2) laneCount = 2;

        if (minSpacingZ < 0.01f) minSpacingZ = 0.01f;
        if (maxSpacingZ < minSpacingZ) maxSpacingZ = minSpacingZ;

        if (forcePadEveryN < 0) forcePadEveryN = 0;

        // évite valeurs négatives absurdes
        if (obstacleSize.x < 0) obstacleSize.x = 0;
        if (obstacleSize.y < 0) obstacleSize.y = 0;
        if (padSize.x < 0) padSize.x = 0;
        if (padSize.y < 0) padSize.y = 0;
        if (platformSize.x < 0) platformSize.x = 0;
        if (platformSize.y < 0) platformSize.y = 0;
    }
}
