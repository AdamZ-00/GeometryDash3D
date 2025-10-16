using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Placement")]
    public float height   = 3f;   // hauteur constante de la caméra
    public float distance = 6f;   // distance derrière le joueur (le long du +Z)
    [Range(0f,1f)] public float followX = 1f; // 0 = ne suit pas en X, 1 = suit totalement

    [Header("Aiming")]
    public float lookAhead = 10f; // vise un point en avant sur la piste
    public float smooth = 8f;     // lissage du mouvement

    private Vector3 vel; // utilisé par SmoothDamp

    void LateUpdate()
    {
        if (!target) return;

        // position désirée : hauteur FIXE, derrière le joueur, X facultatif
        float desiredX = Mathf.Lerp(transform.position.x, target.position.x, followX);
        Vector3 desiredPos = new Vector3(desiredX, height, target.position.z - distance);

        // lissage du déplacement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref vel, 1f / smooth);

        // vise un point en avant, à HAUTEUR FIXE (ne pointe pas vers le Y du joueur)
        Vector3 lookPoint = new Vector3(target.position.x, height, target.position.z + lookAhead);
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }
}