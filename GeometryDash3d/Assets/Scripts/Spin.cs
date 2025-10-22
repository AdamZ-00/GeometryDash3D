using UnityEngine;

public class Spin : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 30f; // degrés/seconde

    void Update()
    {
        transform.Rotate(axis, speed * Time.deltaTime, Space.World);
    }
}
