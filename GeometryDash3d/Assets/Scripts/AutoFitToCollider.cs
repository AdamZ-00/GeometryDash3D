using UnityEngine;

[ExecuteAlways]
public class AutoFitToCollider : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Références")]
    public Collider targetCollider;         // Le BoxCollider du Player (ou autre collider de référence)
    public Renderer targetRenderer;         // Laisser vide => cherche le 1er Renderer enfant

    [Header("Options de fit")]
    public bool fitOnStart = true;
    public bool uniformScale = true;        // conserve les proportions
    public Axis matchAxis = Axis.X;         // axe de référence pour le scale uniforme
    public bool centerToCollider = true;    // recentre le visuel sur le collider

    [Header("Debug")]
    public bool logInfo = false;

    void Reset()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (!targetCollider) targetCollider = GetComponentInParent<Collider>();
    }

    void Start()
    {
        if (fitOnStart) FitNow();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying && fitOnStart)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this) FitNow();
            };
        }
    }
#endif

    [ContextMenu("Fit Now")]
    public void FitNow()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (!targetRenderer || !targetCollider) return;

        // 1) SCALE
        // Mesures en monde (bounds) — simple et robuste
        var rendBounds = targetRenderer.bounds;
        var colBounds = targetCollider.bounds;

        Vector3 sizeR = SafeSize(rendBounds.size);
        Vector3 sizeC = SafeSize(colBounds.size);

        Vector3 scaleMul;

        if (uniformScale)
        {
            float ratio = 1f;
            switch (matchAxis)
            {
                case Axis.X: ratio = sizeC.x / sizeR.x; break;
                case Axis.Y: ratio = sizeC.y / sizeR.y; break;
                case Axis.Z: ratio = sizeC.z / sizeR.z; break;
            }
            scaleMul = new Vector3(ratio, ratio, ratio);
        }
        else
        {
            scaleMul = new Vector3(
                sizeC.x / sizeR.x,
                sizeC.y / sizeR.y,
                sizeC.z / sizeR.z
            );
        }

        // Applique le scale sur le root visuel (ce GameObject)
        transform.localScale = MultiplyPerAxis(transform.localScale, scaleMul);

        // 2) CENTER
        if (centerToCollider)
        {
            // Recalcule après scale
            rendBounds = targetRenderer.bounds;
            Vector3 worldDelta = colBounds.center - rendBounds.center;
            transform.position += worldDelta;
        }

        if (logInfo)
        {
            Debug.Log($"[AutoFitToCollider] Fit done. ScaleMul={scaleMul}, Centered={centerToCollider}", this);
        }
    }

    static Vector3 SafeSize(Vector3 v)
    {
        return new Vector3(
            Mathf.Max(1e-5f, v.x),
            Mathf.Max(1e-5f, v.y),
            Mathf.Max(1e-5f, v.z)
        );
    }

    static Vector3 MultiplyPerAxis(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}
