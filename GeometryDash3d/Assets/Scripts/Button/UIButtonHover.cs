using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Vector3 baseScale;
    public float scaleFactor = 1.1f;
    public float speed = 10f;
    bool hovering = false;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        // reset à l’état normal à chaque réactivation du bouton
        hovering = false;
        transform.localScale = baseScale;
    }

    void OnDisable()
    {
        // au moment où le panel se désactive, on remet tout à plat
        hovering = false;
        transform.localScale = baseScale;
    }

    void Update()
    {
        var target = hovering ? baseScale * scaleFactor : baseScale;
        // Time.unscaledDeltaTime pour que l’anim marche aussi en pause/menu
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData _) => hovering = true;
    public void OnPointerExit(PointerEventData _) => hovering = false;

    // Utilitaire si tu veux forcer le reset depuis un autre script
    public void ResetState()
    {
        hovering = false;
        transform.localScale = baseScale;
    }
}
