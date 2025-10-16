using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{

    private LevelManagerLogic level;

    [Header("Vitesses")]
    public float forwardSpeed = 8f;
    public float strafeSpeed = 6f;
    public float jumpForce = 4f;

    [Header("Détection sol")]
    public float groundCheckDistance = 0.55f;   // distance du rayon vers le bas
    public LayerMask groundMask;

    [Header("Bords")]
    public bool clampX = true;
    public float minX = -2.5f;
    public float maxX = 2.5f;

    [Header("Flip en l'air")]
    public float airRotationDuration = 0.35f;
    public Vector3 flipAxis = Vector3.right;

    private Rigidbody rb;
    private bool isFlipping = false;
    private Quaternion rotStart, rotEnd;
    private float rotT = 0f;

    private Vector3 spawnPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        spawnPoint = transform.position;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        level = FindObjectOfType<LevelManagerLogic>();
    }

    void Update()
    {
        if (level != null && level.IsLevelFinished) return;
        
        // saut si au sol
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // démarrage du quart de tour
            isFlipping = true;
            rotT = 0f;
            rotStart = transform.rotation;
            rotEnd = transform.rotation * Quaternion.AngleAxis(90f, flipAxis.normalized);
        }

        if (clampX)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            transform.position = p;
        }
    }

    void FixedUpdate()
    {
        if (level != null && level.IsLevelFinished) return;
        
        // déplacement
        float h = Input.GetAxisRaw("Horizontal");
        Vector3 v = rb.linearVelocity;
        v.x = h * strafeSpeed;
        v.z = forwardSpeed;
        rb.linearVelocity = v;

        // rotation en l’air
        if (isFlipping)
        {
            rotT += Time.fixedDeltaTime / airRotationDuration;
            rb.MoveRotation(Quaternion.Slerp(rotStart, rotEnd, rotT));

            // stop du flip
            if (rotT >= 1f)
            {
                rb.MoveRotation(rotEnd);
                isFlipping = false;
            }
        }
    }

    bool IsGrounded()
    {
        // tire un rayon vers le bas dans le monde
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);
    }

    public void Respawn()
    {
        // coupe toute vitesse
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // remet à la position de départ
        transform.position = spawnPoint;

        // optionnel : remettre l’orientation/états
        // transform.rotation = Quaternion.identity;
        // isFlipping = false;
    }


    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Obstacle") || other.collider.CompareTag("Kill")) 
            Respawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("Kill"))
            Respawn();
    }

    public void ForceJump(float customJumpForce)
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // reset du Y pour un saut net
        rb.AddForce(Vector3.up * customJumpForce, ForceMode.Impulse);

        // petit flip visuel
        isFlipping = true;
        rotT = 0f;
        rotStart = transform.rotation;
        rotEnd = transform.rotation * Quaternion.AngleAxis(90f, flipAxis.normalized);
    }

}
