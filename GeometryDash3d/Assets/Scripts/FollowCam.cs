using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Placement (relatif au joueur, en repère MONDE)")]
    [Tooltip("Décalage X = suivi latéral ; Y = hauteur au-dessus du joueur ; Z = distance derrière (valeur POSITIVE).")]
    public float followX = 1f;     // 0 = ne suit pas en X, 1 = suit exactement le X du joueur
    public float height = 3f;     // hauteur au-dessus du joueur (Y_cam = Y_player + height)
    public float distance = 6f;    // derrière le joueur le long de l'axe Z du monde (pos_cam.z = z_player - distance)

    [Header("Lissage")]
    [Tooltip("Temps de convergence ~ 1/smooth (en s). Plus grand = plus réactif.")]
    public float smooth = 8f;

    [Header("Rotation")]
    [Tooltip("Si vrai : la caméra conserve des angles fixes et NE TOURNE PAS avec le joueur.")]
    public bool lockRotation = true;
    [Tooltip("Angles fixes quand lockRotation est vrai : (pitch, yaw). Roll = 0.")]
    public Vector2 fixedAngles = new Vector2(10f, 0f); // pitch, yaw

    private Vector3 vel; // pour SmoothDamp

    void LateUpdate()
    {
        if (!target) return;

        // --- Position désirée (monde) ---
        // X: suit plus ou moins le joueur
        float desiredX = Mathf.Lerp(transform.position.x, target.position.x, Mathf.Clamp01(followX));

        // Y: garde une hauteur CONSTANTE relative au joueur
        float desiredY = target.position.y + height;

        // Z: garde une distance CONSTANTE derrière le joueur le long de +Z monde
        float desiredZ = target.position.z - Mathf.Abs(distance);

        Vector3 desiredPos = new Vector3(desiredX, desiredY, desiredZ);

        // --- Lissage position ---
        float followTime = 1f / Mathf.Max(0.01f, smooth); // durée de convergence
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref vel, followTime);

        // --- Rotation verrouillée (ne PAS regarder le joueur) ---
        if (lockRotation)
        {
            transform.rotation = Quaternion.Euler(fixedAngles.x, fixedAngles.y, 0f);
        }
        // Sinon, si tu veux une rotation fixe différente à chaud, tu peux la changer ailleurs,
        // mais on n'oriente JAMAIS vers le joueur ici.
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (distance < 0f) distance = -distance; // distance toujours positive
        if (smooth < 0.01f) smooth = 0.01f;
    }
#endif
}
