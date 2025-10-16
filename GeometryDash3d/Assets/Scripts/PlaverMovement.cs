using UnityEngine;

public class PlaverMovement : MonoBehaviour
{

    public float playerSpeed = 2; 
    public float horizontalSpeed = 3;
    public float rightLimit = 5.5f;
    public float leftLimit = -5.5f;
    public float rotationSpeed = 180f; // vitesse en degrés par seconde

    void Update()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed, Space.World);
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime, Space.Self);

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            if (this.gameObject.transform.position.x >= leftLimit)
            {
                transform.Translate(Vector3.left * Time.deltaTime * horizontalSpeed, Space.World);
            }
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            if (this.gameObject.transform.position.x <= rightLimit)
            {

                transform.Translate(Vector3.right * Time.deltaTime * horizontalSpeed, Space.World);
            }
        }
    }
}
