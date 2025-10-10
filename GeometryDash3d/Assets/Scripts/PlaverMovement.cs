using UnityEngine;

public class PlaverMovement : MonoBehaviour
{

    public float playerSpeed = 2; // Player speed

    void Update()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed, Space.World);
    }
}
