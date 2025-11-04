using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Renderer))]
public class MusicBeatBounceFromProbe : MonoBehaviour
{
    [Header("Probe (source d'énergie)")]
    public AudioBassProbe probe;

    [Header("Cibles visuelles")]
    public Renderer targetRenderer;
    public FitQuadToCamera fitQuad;

    [Header("Détection de beat (sur enveloppe)")]
    public int envWindow = 24;
    public float envThresholdMul = 1.3f;
    public float minBeatInterval = 0.12f;
    public float pulseKick = 0.7f;
    public float pulseDecay = 3.5f;

    [Header("Mapping shader (NeonGridPro/Unlit)")]
    public float baseEmission = 2.8f;
    public float emissionOnBeat = 6f;
    public bool widenLines = true;
    public float lineWidthOnBeat = 0.03f;
    public bool zoomGrid = true;
    public float gridZoomOnBeat = 0.18f;
    public bool bounceScreen = true;
    public float screenBumpOnBeat = 0.08f;
    public float returnSpeed = 8f;

    // --- NOUVEAU : drive continu + lissage ---
    [Header("Hybrid Drive (continu + lissage)")]
    [Tooltip("0 = uniquement les beats ; 1 = ajout de drive continu (basse)")]
    [Range(0f, 1f)] public float continuousAmount = 0.5f;
    [Tooltip("Gain sur l'enveloppe basse pour le drive continu")]
    public float driveGain = 2.2f;
    [Tooltip("Courbe (gamma) du drive : >1 = plus de punch")]
    public float driveCurve = 1.6f;
    [Tooltip("Lissage visuel (plus haut = plus fluide)")]
    public float visualSmoothing = 10f;

    // IDs shader
    Material _mat;
    int _EmissionID, _WidthAID, _WidthBID, _ScaleAID, _ScaleBID, _PulseAmtID;
    float _baseWidthA, _baseWidthB, _baseScaleA, _baseScaleB;

    Queue<float> _hist = new Queue<float>();
    float _sum;
    float _sinceLastBeat;
    float _pulse;              // énergie beats
    float _pSmoothed;          // lissé pour affichage

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mat = targetRenderer.material;

        _EmissionID = Shader.PropertyToID("_Emission");
        _WidthAID = Shader.PropertyToID("_WidthA");
        _WidthBID = Shader.PropertyToID("_WidthB");
        _ScaleAID = Shader.PropertyToID("_ScaleA");
        _ScaleBID = Shader.PropertyToID("_ScaleB");
        _PulseAmtID = Shader.PropertyToID("_PulseAmt");

        _baseWidthA = _mat.HasProperty(_WidthAID) ? _mat.GetFloat(_WidthAID) : 0.025f;
        _baseWidthB = _mat.HasProperty(_WidthBID) ? _mat.GetFloat(_WidthBID) : 0.012f;
        _baseScaleA = _mat.HasProperty(_ScaleAID) ? _mat.GetFloat(_ScaleAID) : 9f;
        _baseScaleB = _mat.HasProperty(_ScaleBID) ? _mat.GetFloat(_ScaleBID) : 20f;

        if (_mat.HasProperty(_PulseAmtID)) _mat.SetFloat(_PulseAmtID, 0f);
        if (_mat.HasProperty(_EmissionID)) _mat.SetFloat(_EmissionID, baseEmission);

        if (!fitQuad) fitQuad = GetComponent<FitQuadToCamera>();
    }

    void Update()
    {
        if (!probe) return;

        float dt = Time.unscaledDeltaTime;
        _sinceLastBeat += dt;

        // --- enveloppe basse globale ---
        float env = probe.BassEnvelope;    // ~0.. (déjà lissée et stable)

        // moyenne glissante
        _hist.Enqueue(env);
        _sum += env;
        if (_hist.Count > Mathf.Max(8, envWindow)) _sum -= _hist.Dequeue();
        float avg = _sum / Mathf.Max(1, _hist.Count);

        // seuil dynamique
        float threshold = avg * envThresholdMul;

        // beat net
        if (env > threshold && _sinceLastBeat >= minBeatInterval)
        {
            _pulse += pulseKick;
            _sinceLastBeat = 0f;
        }

        // décroissance des beats
        _pulse = Mathf.Max(0f, _pulse - pulseDecay * dt);

        // --- drive continu (toujours actif, discret) ---
        float continuous = Mathf.Pow(Mathf.Clamp01(env * driveGain), driveCurve) * continuousAmount;

        // énergie totale utilisée pour l'anim (beats + drive)
        float p = Mathf.Clamp01(_pulse + continuous);

        // lissage visuel (évite le côté "glitch")
        float kVis = 1f - Mathf.Exp(-visualSmoothing * dt);
        _pSmoothed = Mathf.Lerp(_pSmoothed, p, kVis);

        ApplyVisuals(_pSmoothed, dt);
    }

    void ApplyVisuals(float p, float dt)
    {
        float k = 1f - Mathf.Exp(-returnSpeed * dt);

        // Emission
        if (_mat.HasProperty(_EmissionID))
        {
            float target = baseEmission + p * emissionOnBeat;
            float cur = _mat.GetFloat(_EmissionID);
            _mat.SetFloat(_EmissionID, Mathf.Lerp(cur, target, k));
        }

        // Lignes
        if (widenLines)
        {
            if (_mat.HasProperty(_WidthAID))
                _mat.SetFloat(_WidthAID, Mathf.Lerp(_mat.GetFloat(_WidthAID), _baseWidthA + p * lineWidthOnBeat, k));

            if (_mat.HasProperty(_WidthBID))
                _mat.SetFloat(_WidthBID, Mathf.Lerp(_mat.GetFloat(_WidthBID), _baseWidthB + p * lineWidthOnBeat * 0.6f, k));
        }

        // Zoom de grille
        if (zoomGrid)
        {
            float mul = 1f + p * gridZoomOnBeat;
            if (_mat.HasProperty(_ScaleAID))
                _mat.SetFloat(_ScaleAID, Mathf.Lerp(_mat.GetFloat(_ScaleAID), _baseScaleA * mul, k));
            if (_mat.HasProperty(_ScaleBID))
                _mat.SetFloat(_ScaleBID, Mathf.Lerp(_mat.GetFloat(_ScaleBID), _baseScaleB * mul, k));
        }

        // Bump écran
        if (bounceScreen && fitQuad)
        {
            float targetMul = 1f + p * screenBumpOnBeat;
            fitQuad.externalScaleFactor = Mathf.Lerp(fitQuad.externalScaleFactor, targetMul, k);
        }
    }
}
