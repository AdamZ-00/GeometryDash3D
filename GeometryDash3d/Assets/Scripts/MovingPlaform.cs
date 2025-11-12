using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Mouvement")]
    public Vector3 moveAxis = Vector3.right; // direction du mouvement (droite/gauche)
    public float moveDistance = 4f;          // distance totale du déplacement
    public float moveSpeed = 2f;             // vitesse du mouvement

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // mouvement sinusoïdal : va-retour fluide entre les deux extrémités
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance * 0.5f;
        transform.position = startPos + moveAxis.normalized * offset;
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Player"))
        {
            other.transform.SetParent(transform); // le joueur devient enfant de la plateforme
        }
    }

    void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Player"))
        {
            other.transform.SetParent(null); // libère le joueur
        }
    }

}

