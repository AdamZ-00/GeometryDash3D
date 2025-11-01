using UnityEngine;

public class JumpPadInt : MonoBehaviour
{
    [Header("Réglages du saut")]
    public float jumpForce = 10f;        // Puissance du saut
    public KeyCode jumpKey = KeyCode.Space; // Touche à appuyer
    private bool playerOnPad = false;    // Est-ce que le joueur est sur le pad ?
    private Rigidbody playerRb;

    void Update()
    {
        if (playerOnPad && Input.GetKeyDown(jumpKey))
        {
            // On réinitialise la vitesse verticale avant de sauter
            playerRb.linearVelocity = new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z);
            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            playerOnPad = false; // Empêche de spammer le saut
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnPad = true;
            playerRb = other.GetComponent<Rigidbody>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnPad = false;
            playerRb = null;
        }
    }
}

public class FloatEffect : MonoBehaviour
{
    public float amplitude = 0.2f;  // Amplitude du mouvement vertical
    public float frequency = 2f;    // Fréquence du mouvement

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * frequency) * amplitude;
    }
}
