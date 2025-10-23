using UnityEngine;

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

    [Tooltip("Transition lissée (0 = instant)")]
    public float transitionDuration = 0.2f;

    [Header("Options")]
    public bool oneShot = false;
    public bool destroyOnUse = false;
    public string playerTag = "Player";

    private bool used = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used && oneShot) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        switch (kind)
        {
            case PortalKind.Accelerate:
                {
                    // sécurité : si mal réglé, on force > 1
                    float mult = (multiplier <= 1f) ? 1.25f : multiplier;
                    ApplyCumulative(pc, mult, transitionDuration);
                    break;
                }
            case PortalKind.Slow:
                {
                    // sécurité : si mal réglé, on force < 1
                    float mult = (multiplier >= 1f) ? 0.75f : multiplier;
                    ApplyCumulative(pc, mult, transitionDuration);
                    break;
                }
            case PortalKind.Neutral:
                {
                    // remet la vitesse d'origine (base) comme avant
                    pc.ResetSpeedToBase(transitionDuration);
                    break;
                }
            case PortalKind.SetExactValue:
                {
                    // fixe une valeur absolue
                    float target = Mathf.Max(0.01f, setValue);
                    pc.SetForwardSpeed(target, transitionDuration);
                    break;
                }
            case PortalKind.MultiplyCurrent:
                {
                    // mode explicite "cumulatif"
                    ApplyCumulative(pc, multiplier, transitionDuration);
                    break;
                }
        }

        if (oneShot) used = true;
        if (oneShot && destroyOnUse) Destroy(gameObject);
    }

    /// <summary>
    /// Multiplie la vitesse ACTUELLE du joueur, avec transition.
    /// </summary>
    private void ApplyCumulative(PlayerController pc, float mult, float dur)
    {
        mult = Mathf.Max(0.01f, mult); // pas de 0 ou négatif
        float current = pc.forwardSpeed;
        float target = Mathf.Max(0.01f, current * mult);
        pc.SetForwardSpeed(target, dur);
    }
}
