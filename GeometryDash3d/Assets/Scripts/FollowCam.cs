using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3f, -6f);
    public float smooth = 5f;

    void LateUpdate()
    {
        if (!target) return;
        transform.position = Vector3.Lerp(transform.position, target.position + offset, smooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.forward * 10f);
    }
}
