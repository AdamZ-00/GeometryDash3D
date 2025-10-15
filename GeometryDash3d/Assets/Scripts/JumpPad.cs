using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Réglages du saut")]
    public float jumpForce = 10f;

    private void OnTriggerEnter(Collider other)
    {
        // Vérifie si le joueur touche le pad
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ForceJump(jumpForce);
        }
    }
}
