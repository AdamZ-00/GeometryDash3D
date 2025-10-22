using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpeedPortal : MonoBehaviour
{
    public enum PortalKind { Accelerate, Slow, Neutral, SetExactValue, MultiplyCustom }

    [Header("Type")]
    public PortalKind kind = PortalKind.Accelerate;

    [Header("Paramètres")]
    [Tooltip("Accélérer/Ralentir : 1.25 = +25%, 0.75 = -25%")]
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
                if (multiplier < 1f) multiplier = 1.25f;
                pc.SetSpeedMultiplier(multiplier, transitionDuration);
                break;
            case PortalKind.Slow:
                if (multiplier >= 1f) multiplier = 0.75f;
                pc.SetSpeedMultiplier(multiplier, transitionDuration);
                break;
            case PortalKind.Neutral:
                pc.ResetSpeedToBase(transitionDuration);
                break;
            case PortalKind.SetExactValue:
                pc.SetForwardSpeed(setValue, transitionDuration);
                break;
            case PortalKind.MultiplyCustom:
                pc.SetSpeedMultiplier(multiplier, transitionDuration);
                break;
        }

        if (oneShot) used = true;
        if (oneShot && destroyOnUse) Destroy(gameObject);
    }
}
