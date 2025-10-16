using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; 
    public Vector3 offset = new Vector3(0, 5, -10); // Position relative de la caméra

    void LateUpdate()
    {
        if (player != null)
        {
            // Suivi de la position seulement
            transform.position = player.position + offset;

            // Optionnel : regarde toujours vers le joueur
            transform.LookAt(player.position);
        }
    }
}
