using UnityEngine;

public class SideGroundTrails : MonoBehaviour
{
    [Header("Cible & Sol")]
    public Transform target;                 // ton cube (Player)
    public LayerMask groundMask;             // UNIQUEMENT le layer du sol

    [Header("Cast vers le bas")]
    public float castHeight = 0.6f;
    public float castRadius = 0.3f;
    public float extraDown = 0.6f;

    [Header("Position des traînées / sparks")]
    public float sideOffset = 0.55f;         // demi-largeur cube + marge
    public float lift = 0.02f;         // léger décalage au-dessus du sol

    [Header("Trails")]
    public TrailRenderer leftTrail;
    public TrailRenderer rightTrail;

    [Header("Sparks (Particle Systems)")]
    public ParticleSystem leftSparks;
    public ParticleSystem rightSparks;

    [Tooltip("Nombre de particules émises d'un coup quand on atterrit.")]
    public int landBurst = 15;

    [Tooltip("Débit max (particules/s) atteint à 'speedForMaxSparks'.")]
    public float sparksRateAtMax = 80f;

    [Tooltip("Vitesse avant (Z) à partir de laquelle on atteint le débit max.")]
    public float speedForMaxSparks = 12f;

    [Header("Emission conditions")]
    public float minForwardSpeed = 0.1f;     // coupe à l'arrêt
    public bool clearOnAir = true;

    private Rigidbody _rb;
    private bool _wasGrounded = false;
    private static readonly RaycastHit[] _hits = new RaycastHit[6];

    // caches d'emission
    private ParticleSystem.EmissionModule _emitL, _emitR;

    void Awake()
    {
        if (!target) target = transform;
        _rb = target.GetComponent<Rigidbody>();

        // Trails: pas enfants du player
        if (leftTrail) leftTrail.transform.SetParent(null, true);
        if (rightTrail) rightTrail.transform.SetParent(null, true);

        // Sparks: pas enfants non plus (optionnel)
        if (leftSparks) leftSparks.transform.SetParent(null, true);
        if (rightSparks) rightSparks.transform.SetParent(null, true);

        // Cache les modules d'émission
        if (leftSparks) _emitL = leftSparks.emission;
        if (rightSparks) _emitR = rightSparks.emission;

        SetTrailEmitting(false);
        SetSparksRate(0f);
        ClearAll();
    }

    void LateUpdate()
    {
        if (!target) return;

        // ---- Ground check robuste (SphereCast + ignore le player) ----
        Vector3 origin = target.position + Vector3.up * castHeight;
        float distance = castHeight + extraDown;

        bool grounded = false;
        Vector3 groundPoint = default;
        Vector3 groundNormal = Vector3.up;

        int count = Physics.SphereCastNonAlloc(origin, castRadius, Vector3.down, _hits, distance, groundMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (h.collider == null) continue;
            if (h.collider.transform.root == target.root) continue; // ignore soi-même
            grounded = true;
            groundPoint = h.point;
            groundNormal = h.normal;
            break;
        }

        float vz = (_rb ? _rb.linearVelocity.z : 0f);
        bool speedOK = Mathf.Abs(vz) > minForwardSpeed;

        bool shouldEmit = grounded && speedOK;

        // Transition sol -> air : nettoie et coupe
        if (_wasGrounded && !grounded)
        {
            if (clearOnAir) ClearAll();
            SetSparksRate(0f);
            SetTrailEmitting(false);
        }

        // Transition air -> sol : petit burst d'atterrissage
        if (!_wasGrounded && grounded)
        {
            if (landBurst > 0)
            {
                if (leftSparks) leftSparks.Emit(landBurst);
                if (rightSparks) rightSparks.Emit(landBurst);
            }
        }

        _wasGrounded = grounded;

        // Place et émet
        if (shouldEmit)
        {
            Vector3 basePoint = groundPoint + groundNormal * lift;
            Vector3 leftPos = basePoint + Vector3.left * sideOffset;
            Vector3 rightPos = basePoint + Vector3.right * sideOffset;

            if (leftTrail) { leftTrail.transform.position = leftPos; leftTrail.transform.forward = Vector3.forward; }
            if (rightTrail) { rightTrail.transform.position = rightPos; rightTrail.transform.forward = Vector3.forward; }

            if (leftSparks) leftSparks.transform.position = leftPos;
            if (rightSparks) rightSparks.transform.position = rightPos;

            SetTrailEmitting(true);

            // Débit continu en fonction de la vitesse avant (0..max)
            float t = Mathf.InverseLerp(0f, speedForMaxSparks, Mathf.Abs(vz));
            SetSparksRate(Mathf.Lerp(0f, sparksRateAtMax, t));
        }
        else
        {
            SetTrailEmitting(false);
            SetSparksRate(0f);
        }
    }

    private void SetTrailEmitting(bool on)
    {
        if (leftTrail) leftTrail.emitting = on;
        if (rightTrail) rightTrail.emitting = on;
    }

    private void SetSparksRate(float rate)
    {
        if (leftSparks)
        {
            var e = _emitL;
            e.rateOverTime = rate;
        }
        if (rightSparks)
        {
            var e = _emitR;
            e.rateOverTime = rate;
        }
    }

    public void ClearAll()
    {
        if (leftTrail) leftTrail.Clear();
        if (rightTrail) rightTrail.Clear();
        // Pas nécessaire de Clear() les particules : elles meurent avec leur lifetime
    }
}
