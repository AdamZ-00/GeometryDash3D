using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("R�glages du saut")]
    public float jumpForce = 10f;

    private void OnTriggerEnter(Collider other)
    {
        // V�rifie si le joueur touche le pad
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ForceJump(jumpForce);
        }
    }
}
