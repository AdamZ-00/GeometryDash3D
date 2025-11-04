using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BgBounceOnBeat : MonoBehaviour
{
    [Header("Clock (OBLIGATOIRE)")]
    public BpmConductor conductor;

    [Header("Optional punch")]
    public AudioBassProbe bassProbe;
    [Range(0f, 2f)] public float bassInfluence = 0.6f;

    [Header("Cibles")]
    public Renderer targetRenderer;     // auto si null
    public FitQuadToCamera fitQuad;     // auto si présent

    [Header("Mapping (NeonGridPro / NeonGridUnlit)")]
    public float baseEmission = 3.0f;
    public float emissionOnBeat = 6.0f;
    public bool widenLines = true;
    public float lineWidthOnBeat = 0.035f;
    public bool zoomGrid = true;
    public float gridZoomOnBeat = 0.22f;
    public bool bounceScreen = true;
    public float screenBumpOnBeat = 0.10f;

    [Header("Dynamique")]
    public float pulseKick = 1.0f;
    public float pulseDecay = 3.0f;
    public float visualSmoothing = 12f;
    public float returnSpeed = 9f;

    [Header("DEBUG")]
    public bool logBeatsHere = false;
    public bool allowManualPulse = true;
    public KeyCode manualPulseKey = KeyCode.P;
    [Range(0, 1)] public float debugPulse = 0f;   // slider manuel (0..1)

    // shader property IDs
    Material _mat;                  // référence (pas modifiée directement)
    MaterialPropertyBlock _mpb;     // block pour pousser les valeurs
    int _EmissionID, _WidthAID, _WidthBID, _ScaleAID, _ScaleBID, _PulseAmtID;

    // bases (lues une fois, jamais écrasées)
    float _baseWidthA = 0.025f;
    float _baseWidthB = 0.012f;
    float _baseScaleA = 9f;
    float _baseScaleB = 20f;

    float _pulse;        // énergie instantanée
    float _pVis;         // lissée pour affichage

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        if (!fitQuad) fitQuad = GetComponent<FitQuadToCamera>();

        _mat = targetRenderer.sharedMaterial;      // on lit la base depuis le shared (pas d’instance)
        _mpb = new MaterialPropertyBlock();

        _EmissionID = Shader.PropertyToID("_Emission");
        _WidthAID = Shader.PropertyToID("_WidthA");
        _WidthBID = Shader.PropertyToID("_WidthB");
        _ScaleAID = Shader.PropertyToID("_ScaleA");
        _ScaleBID = Shader.PropertyToID("_ScaleB");
        _PulseAmtID = Shader.PropertyToID("_PulseAmt");

        // Lire les bases si dispo
        if (_mat)
        {
            if (_mat.HasProperty(_WidthAID)) _baseWidthA = _mat.GetFloat(_WidthAID);
            if (_mat.HasProperty(_WidthBID)) _baseWidthB = _mat.GetFloat(_WidthBID);
            if (_mat.HasProperty(_ScaleAID)) _baseScaleA = _mat.GetFloat(_ScaleAID);
            if (_mat.HasProperty(_ScaleBID)) _baseScaleB = _mat.GetFloat(_ScaleBID);
            // Remettre l’éventuel _PulseAmt à 0 (si présent)
            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(_PulseAmtID, 0f);
            targetRenderer.SetPropertyBlock(_mpb);
        }
    }

    void OnEnable()
    {
        if (!conductor) conductor = FindObjectOfType<BpmConductor>();
        if (conductor) conductor.OnBeat += HandleBeat;
    }

    void OnDisable()
    {
        if (conductor) conductor.OnBeat -= HandleBeat;
    }

    void HandleBeat(int idx)
    {
        if (logBeatsHere) Debug.Log($"[BgBounceOnBeat] Beat {idx} reçu.");
        float punch = 1f;
        if (bassProbe && bassInfluence > 0f)
        {
            float env = Mathf.Clamp01(bassProbe.BassEnvelope * 8f);
            punch += env * bassInfluence;
        }
        _pulse += pulseKick * punch;
    }

    void Update()
    {
        // test manuel
        if (allowManualPulse && Input.GetKeyDown(manualPulseKey))
            _pulse += pulseKick;

        // décroissance & lissage
        float dt = Time.unscaledDeltaTime;
        _pulse = Mathf.Max(0f, _pulse - pulseDecay * dt);

        float p = Mathf.Clamp01(_pulse);
        // ajouter éventuellement le slider debug
        p = Mathf.Clamp01(Mathf.Max(p, debugPulse));

        float kVis = 1f - Mathf.Exp(-visualSmoothing * dt);
        _pVis = Mathf.Lerp(_pVis, p, kVis);

        ApplyVisuals(_pVis, dt);
    }

    void ApplyVisuals(float p, float dt)
    {
        float k = 1f - Mathf.Exp(-returnSpeed * dt);

        // On lit le block actuel, on modifie, on repousse.
        targetRenderer.GetPropertyBlock(_mpb);

        // Emission
        if (_mat && _mat.HasProperty(_EmissionID))
        {
            float current = _mpb.GetFloat(_EmissionID);
            float target = baseEmission + p * emissionOnBeat;
            _mpb.SetFloat(_EmissionID, Mathf.Lerp(current, target, k));
        }

        // Lignes
        if (widenLines)
        {
            if (_mat && _mat.HasProperty(_WidthAID))
            {
                float current = _mpb.GetFloat(_WidthAID); // 0 si pas encore écrit → fallback
                if (current <= 0f) current = _baseWidthA;
                float target = _baseWidthA + p * lineWidthOnBeat;
                _mpb.SetFloat(_WidthAID, Mathf.Lerp(current, target, k));
            }
            if (_mat && _mat.HasProperty(_WidthBID))
            {
                float current = _mpb.GetFloat(_WidthBID);
                if (current <= 0f) current = _baseWidthB;
                float target = _baseWidthB + p * lineWidthOnBeat * 0.6f;
                _mpb.SetFloat(_WidthBID, Mathf.Lerp(current, target, k));
            }
        }

        // Zoom de grille
        if (zoomGrid)
        {
            if (_mat && _mat.HasProperty(_ScaleAID))
            {
                float current = _mpb.GetFloat(_ScaleAID);
                if (current <= 0f) current = _baseScaleA;
                float mul = 1f + p * gridZoomOnBeat;
                float target = _baseScaleA * mul;
                _mpb.SetFloat(_ScaleAID, Mathf.Lerp(current, target, k));
            }
            if (_mat && _mat.HasProperty(_ScaleBID))
            {
                float current = _mpb.GetFloat(_ScaleBID);
                if (current <= 0f) current = _baseScaleB;
                float mul = 1f + p * gridZoomOnBeat;
                float target = _baseScaleB * mul;
                _mpb.SetFloat(_ScaleBID, Mathf.Lerp(current, target, k));
            }
        }

        // Push vers le GPU
        targetRenderer.SetPropertyBlock(_mpb);

        // Bump écran (hors material)
        if (bounceScreen && fitQuad)
        {
            float targetMul = 1f + p * screenBumpOnBeat;
            fitQuad.externalScaleFactor = Mathf.Lerp(fitQuad.externalScaleFactor, targetMul, k);
        }
    }
}
