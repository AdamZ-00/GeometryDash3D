using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class FitQuadToCamera : MonoBehaviour
{
    public Camera targetCamera;      // auto = Camera.main
    public float distance = 5f;      // Z local devant la caméra
    public bool keepUpright = true;

    [Range(0.5f, 1.5f)]
    public float externalScaleFactor = 1f;  // <-- multiplicateur appliqué par d'autres scripts (bounce)

    float _baseW, _baseH;

    void LateUpdate()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera) return;

        transform.localPosition = new Vector3(0f, 0f, Mathf.Max(0.01f, distance));
        if (keepUpright) transform.localRotation = Quaternion.identity;

        // taille “écran” à cette distance
        float h = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * targetCamera.aspect;

        _baseW = w;
        _baseH = h;

        // on applique le multiplicateur externe (bounce)
        transform.localScale = new Vector3(_baseW * externalScaleFactor, _baseH * externalScaleFactor, 1f);
    }
}
