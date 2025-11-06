using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private LevelManagerLogic level;

    [Header("Vitesses")]
    public float forwardSpeed = 8f;
    public float jumpForce = 4f;

    [Header("Jump (maintien)")]
    [Tooltip("Si true: maintenir Espace fait resauter dès qu'on retouche le sol, façon Geometry Dash.")]
    public bool holdToAutoJump = true;

    // états internes du saut
    private bool wasGrounded = false;       // état sol au frame précédent
    private bool jumpedThisGround = false;  // déjà sauté pendant ce contact au sol

    [Header("3 Voies (Runner)")]
    [Tooltip("Bord gauche et droit de ta zone jouable (pics à ±18 => bornes -18 et +18).")]
    public float leftBoundary = -18f;
    public float rightBoundary = 18f;

    [Tooltip("Si > 0, force un espacement fixe entre voies (sinon auto depuis les bornes).")]
    public float laneSpacingOverride = 0f;

    [Tooltip("Vitesse de changement de voie (plus haut = plus réactif).")]
    public float laneChangeSpeed = 14f;

    [Tooltip("Tolérance de snap sur la voie (évite le drift).")]
    public float laneSnapEpsilon = 0.02f;

    private const int LANE_COUNT = 3;
    private int currentLane = 1;    // 0:gauche, 1:milieu, 2:droite
    private float targetLaneX = 0f;

    [Header("Portails de vitesse")]
    [Tooltip("Vitesse neutre (mémorisée au Start si < 0). Les portails 'Neutral' reviennent à cette valeur.")]
    [SerializeField] private float baseForwardSpeed = -1f;
    private Coroutine speedLerpCo;

    [Header("Détection sol")]
    public float groundCheckDistance = 0.55f;
    public LayerMask groundMask;

    [Header("Flip en l'air")]
    public float airRotationDuration = 0.35f;
    public Vector3 flipAxis = Vector3.right;

    [Header("Respawn")]
    [Tooltip("Remettre la vitesse à la valeur de base quand on respawn ?")]
    public bool resetSpeedOnRespawn = true;
    [Tooltip("0 = instantané. >0 = transition douce vers la vitesse de base.")]
    public float respawnSpeedTransition = 0f;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;   // Output = Mixer/SFX (2D)
    [SerializeField] private AudioClip deathSfx;
    [Range(0f, 1f)] public float deathVolume = 0.8f;

    private Rigidbody rb;
    private bool isFlipping = false;
    private Quaternion rotStart, rotEnd;
    private float rotT = 0f;

    private Vector3 spawnPoint;

    // ===== Helper voies =====
    private float ComputeLaneX(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, LANE_COUNT - 1);
        if (laneSpacingOverride > 0f)
        {
            float d = laneSpacingOverride;
            return (laneIndex - 1) * d; // [-d, 0, +d]
        }
        else
        {
            float center = 0f;
            float gap = (rightBoundary - leftBoundary) / 4f; // 3 voies -> gap = largeur/4
            if (laneIndex == 0) return center - gap;
            if (laneIndex == 2) return center + gap;
            return center;
        }
    }

    // Force la voie à partir d'une position X monde (ex: X du portail OUT).
    public void ForceLaneByWorldX(float worldX, bool snapPosition = true)
    {
        int bestLane = 1;
        float bestDist = Mathf.Infinity;
        for (int i = 0; i < 3; i++)
        {
            float lx = ComputeLaneX(i);
            float d = Mathf.Abs(worldX - lx);
            if (d < bestDist) { bestDist = d; bestLane = i; }
        }

        currentLane = bestLane;
        targetLaneX = ComputeLaneX(currentLane);

        if (snapPosition)
        {
            var p = transform.position;
            p.x = targetLaneX;
            transform.position = p;
        }
    }

    private void StepLane(int dir)
    {
        int newLane = Mathf.Clamp(currentLane + dir, 0, LANE_COUNT - 1);
        if (newLane != currentLane)
        {
            currentLane = newLane;
            targetLaneX = ComputeLaneX(currentLane);
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // SFX de secours si rien n'est assigné
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f; // 2D
        }
    }

    void Start()
    {
        spawnPoint = transform.position;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        level = FindObjectOfType<LevelManagerLogic>();

        if (baseForwardSpeed < 0f) baseForwardSpeed = forwardSpeed;

        // voie initiale au milieu
        currentLane = 1;
        targetLaneX = ComputeLaneX(currentLane);

        var p = transform.position;
        p.x = targetLaneX;
        transform.position = p;

        wasGrounded = IsGrounded();
        jumpedThisGround = false;
    }

    void Update()
    {
        if (level != null && level.IsLevelFinished) return;

        // --- gestion 3 voies (tap) ---
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) StepLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) StepLane(+1);

        // --- logique de saut façon GD ---
        bool grounded = IsGrounded();

        // reset du flag quand on vient d'atterrir
        if (grounded && !wasGrounded)
            jumpedThisGround = false;

        // condition de déclenchement : au sol + (appui ou maintien) + pas encore sauté pendant ce contact
        bool wantJumpNow =
            grounded &&
            !jumpedThisGround &&
            (
                Input.GetButtonDown("Jump") ||
                (holdToAutoJump && Input.GetButton("Jump"))
            );

        if (wantJumpNow)
        {
            DoJump(); // saut de hauteur constante
        }

        wasGrounded = grounded;
    }

    void FixedUpdate()
    {
        if (level != null && level.IsLevelFinished) return;

        // avance constante
        Vector3 v = rb.linearVelocity;
        v.z = forwardSpeed;

        // décalage vers la voie cible
        float deltaX = targetLaneX - rb.position.x;
        float vx = Mathf.Clamp(deltaX * laneChangeSpeed, -laneChangeSpeed * 2f, laneChangeSpeed * 2f);

        if (Mathf.Abs(deltaX) <= laneSnapEpsilon)
        {
            rb.position = new Vector3(targetLaneX, rb.position.y, rb.position.z);
            vx = 0f;
        }

        v.x = vx;
        rb.linearVelocity = v;

        // flip en l’air
        if (isFlipping)
        {
            rotT += Time.fixedDeltaTime / airRotationDuration;
            rb.MoveRotation(Quaternion.Slerp(rotStart, rotEnd, rotT));
            if (rotT >= 1f)
            {
                rb.MoveRotation(rotEnd);
                isFlipping = false;
            }
        }
    }

    // --- Saut unifié (utilisé par Update et ForceJump) ---
    private void DoJump(float forceOverride = -1f)
    {
        float f = (forceOverride > 0f) ? forceOverride : jumpForce;

        // hauteur constante : on remet VY à 0 avant l'impulsion
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * f, ForceMode.Impulse);

        // démarre le quart de tour
        isFlipping = true;
        rotT = 0f;
        rotStart = transform.rotation;
        rotEnd = transform.rotation * Quaternion.AngleAxis(90f, flipAxis.normalized);

        // on a "consommé" le saut pour ce contact au sol
        jumpedThisGround = true;
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);
    }

    public void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // remet la voie au centre
        currentLane = 1;
        targetLaneX = ComputeLaneX(currentLane);
        transform.position = new Vector3(targetLaneX, spawnPoint.y, spawnPoint.z);

        // reset vitesse si demandé
        if (resetSpeedOnRespawn)
        {
            if (speedLerpCo != null) StopCoroutine(speedLerpCo);
            SetForwardSpeed(baseForwardSpeed, respawnSpeedTransition);
        }

        // redémarre la musique principale (si AudioManager présent)
        if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.RestartMusic();

        // on repart propre : on recalera jumpedThisGround au prochain atterrissage
        wasGrounded = IsGrounded();
        if (wasGrounded) jumpedThisGround = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Obstacle") || other.collider.CompareTag("Kill"))
        {
            if (deathSfx && sfxSource) sfxSource.PlayOneShot(deathSfx, deathVolume);
            if (DeathCounter.Instance) DeathCounter.Instance.AddDeath();
            if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.StopMusic();
            Respawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("Kill"))
        {
            if (deathSfx && sfxSource) sfxSource.PlayOneShot(deathSfx, deathVolume);
            if (DeathCounter.Instance) DeathCounter.Instance.AddDeath();
            if (SimpleAudioManager.Instance) SimpleAudioManager.Instance.StopMusic();
            Respawn();
            return;
        }
    }

    public void ForceJump(float customJumpForce)
    {
        DoJump(customJumpForce);
    }

    // ===================== API Portails =====================
    public void SetForwardSpeed(float targetSpeed, float transitionDuration = 0f)
    {
        if (speedLerpCo != null) StopCoroutine(speedLerpCo);
        if (transitionDuration <= 0f) forwardSpeed = targetSpeed;
        else speedLerpCo = StartCoroutine(LerpForwardSpeed(targetSpeed, transitionDuration));
    }

    public void SetSpeedMultiplier(float multiplier, float transitionDuration = 0f)
    {
        SetForwardSpeed(baseForwardSpeed * multiplier, transitionDuration);
    }

    public void ResetSpeedToBase(float transitionDuration = 0f)
    {
        SetForwardSpeed(baseForwardSpeed, transitionDuration);
    }

    public void SetBaseForwardSpeed(float newBase, bool alsoApplyNow = false, float transitionDuration = 0f)
    {
        baseForwardSpeed = newBase;
        if (alsoApplyNow) SetForwardSpeed(baseForwardSpeed, transitionDuration);
    }

    private System.Collections.IEnumerator LerpForwardSpeed(float target, float dur)
    {
        float start = forwardSpeed;
        float t = 0f;
        dur = Mathf.Max(0.0001f, dur);
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            forwardSpeed = Mathf.Lerp(start, target, t);
            yield return null;
        }
        speedLerpCo = null;
    }
}
