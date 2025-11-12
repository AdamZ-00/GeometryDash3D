using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Mouvement")]
    public Vector3 moveAxis = Vector3.right; // direction du mouvement
    public float moveDistance = 6f;          // amplitude du déplacement total
    public float moveSpeed = 3f;             // vitesse du mouvement

    private Vector3 startPos;
    private Rigidbody rb;
    private Vector3 lastPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // on contrôle le mouvement manuellement
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        startPos = transform.position;
        lastPosition = startPos;
    }

    void FixedUpdate()
    {
        // Mouvement sinusoïdal fluide
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance * 0.5f;
        Vector3 newPos = startPos + moveAxis.normalized * offset;

        // Déplacement physique
        rb.MovePosition(newPos);

        lastPosition = newPos;
    }

    void OnCollisionStay(Collision other)
    {
        if (other.collider.CompareTag("Player"))
        {
            Rigidbody playerRb = other.rigidbody;
            if (playerRb != null)
            {
                // Déplacement de la plateforme entre deux frames
                Vector3 platformDelta = transform.position - lastPosition;
                // On ajoute ce déplacement à la vitesse du joueur (pour le "transporter")
                playerRb.position += platformDelta;
            }
        }
    }
}
