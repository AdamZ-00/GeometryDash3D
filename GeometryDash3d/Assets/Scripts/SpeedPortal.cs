using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SpeedPortal : MonoBehaviour
{
    public enum PortalKind { Accelerate, Slow, Neutral, SetExactValue, MultiplyCurrent }

    [Header("Type")]
    public PortalKind kind = PortalKind.Accelerate;

    [Header("Paramètres")]
    [Tooltip("Accélérer/Ralentir : 1.25 = +25%, 0.75 = -25% (appliqué SUR la vitesse ACTUELLE)")]
    public float multiplier = 1.25f;

    [Tooltip("Vitesse exacte si SetExactValue")]
    public float setValue = 12f;

    [Tooltip("Transition de vitesse (0 = instant)")]
    public float transitionDuration = 0.2f;

    [Header("Pickup FX")]
    [Tooltip("Jouer une petite anim de disparition quand pris")]
    public bool hideOnUse = true;
    [Tooltip("Durée de l’anim de disparition")]
    public float hideDuration = 0.18f;
    [Tooltip("Tente de baisser l'alpha du matériel (si le shader expose _Color)")]
    public bool fadeColorIfPossible = true;
    [Tooltip("VFX optionnel instancié au pickup")]
    public ParticleSystem pickupVfx;
    [Tooltip("SFX optionnel joué au pickup")]
    public AudioClip pickupSfx;
    [Range(0, 1)] public float pickupSfxVolume = 0.8f;

    [Header("Options")]
    public bool oneShot = true;
    public bool destroyOnUse = true;
    public string playerTag = "Player";

    private bool used = false;
    private Collider col;
    private Renderer[] rends;
    private Vector3 startScale;

    void Awake()
    {
        col = GetComponent<Collider>();
        rends = GetComponentsInChildren<Renderer>(true);
        startScale = transform.localScale;
    }

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used && oneShot) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        // --- Applique l’effet de vitesse ---
        switch (kind)
        {
            case PortalKind.Accelerate:
                {
                    float mult = (multiplier <= 1f) ? 1.25f : multiplier;
                    ApplyCumulative(pc, mult, transitionDuration);
                    break;
                }
            case PortalKind.Slow:
                {
                    float mult = (multiplier >= 1f) ? 0.75f : multiplier;
                    ApplyCumulative(pc, mult, transitionDuration);
                    break;
                }
            case PortalKind.Neutral:
                pc.ResetSpeedToBase(transitionDuration);
                break;

            case PortalKind.SetExactValue:
                pc.SetForwardSpeed(Mathf.Max(0.01f, setValue), transitionDuration);
                break;

            case PortalKind.MultiplyCurrent:
                ApplyCumulative(pc, Mathf.Max(0.01f, multiplier), transitionDuration);
                break;
        }

        // --- Empêche les doubles triggers et lance la disparition ---
        if (oneShot) used = true;
        if (col) col.enabled = false;

        if (pickupSfx)
        {
            // Joue en 2D rapide (source one-shot éphémère)
            var src = new GameObject("PortalPickupSFX").AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.clip = pickupSfx;
            src.volume = pickupSfxVolume;
            src.Play();
            Object.Destroy(src.gameObject, pickupSfx.length + 0.2f);
        }

        if (pickupVfx)
        {
            var vfx = Instantiate(pickupVfx, transform.position, transform.rotation);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax + 0.5f);
        }

        if (hideOnUse)
            StartCoroutine(VanishThenEnd());
        else
        {
            if (destroyOnUse) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }

    /// <summary> Multiplie la vitesse ACTUELLE du joueur avec transition. </summary>
    private void ApplyCumulative(PlayerController pc, float mult, float dur)
    {
        float current = Mathf.Max(0.01f, pc.forwardSpeed);
        float target = Mathf.Max(0.01f, current * mult);
        pc.SetForwardSpeed(target, dur);
    }

    private IEnumerator VanishThenEnd()
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, hideDuration);

        // Prépare MaterialPropertyBlock pour éviter d’instance les materials
        MaterialPropertyBlock mpb = null;
        if (fadeColorIfPossible) mpb = new MaterialPropertyBlock();

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            // Scale vers 0 (ease out quad)
            float k = 1f - t;
            k = k * k; // ease
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, startScale, k);

            if (fadeColorIfPossible && rends != null)
            {
                float a = Mathf.Clamp01(1f - t);
                for (int i = 0; i < rends.Length; i++)
                {
                    var r = rends[i];
                    if (!r) continue;

                    // Si le shader expose _Color, on ajuste l’alpha via MPB
                    // (ne touche pas aux materials partagés)
                    Color c;
                    r.GetPropertyBlock(mpb);
                    if (r.sharedMaterial && r.sharedMaterial.HasProperty("_Color"))
                    {
                        c = r.sharedMaterial.color;
                        c.a = a;
                        mpb.SetColor("_Color", c);
                        r.SetPropertyBlock(mpb);
                    }
                }
            }

            yield return null;
        }

        // Fin
        if (destroyOnUse) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
