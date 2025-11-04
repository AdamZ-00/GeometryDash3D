using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class NeonGridController : MonoBehaviour
{
    public PlayerController player;          // assigne ton Player
    public float scrollFactorX = 0.02f;      // influence de la vitesse sur le scroll X
    public float scrollFactorY = -0.005f;    // influence sur Y
    public float baseEmission = 2f;
    public float pulseAmount = 0.5f;         // intensité de la pulsation
    public float pulseSpeed = 1.5f;          // vitesse de la pulsation

    private Material _mat;
    private int _ScrollXID, _ScrollYID, _EmissionID;

    void Awake()
    {
        _mat = GetComponent<Renderer>().material; // instance
        _ScrollXID = Shader.PropertyToID("_ScrollX");
        _ScrollYID = Shader.PropertyToID("_ScrollY");
        _EmissionID = Shader.PropertyToID("_Emission");
    }

    void Update()
    {
        float fwd = player ? player.forwardSpeed : 8f;
        // on mappe la vitesse à un petit scroll subtil
        _mat.SetFloat(_ScrollXID, scrollFactorX * fwd);
        _mat.SetFloat(_ScrollYID, scrollFactorY * fwd);

        // petite pulsation de l'émission (sinus lente)
        float pulse = baseEmission + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        _mat.SetFloat(_EmissionID, Mathf.Max(0f, pulse));
    }
}
