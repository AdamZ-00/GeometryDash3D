using UnityEngine;

/// Sonde basses robuste : AUCUN appel main-thread dans OnAudioFilterRead.
/// -> Pas d'erreur "GetSampleRate can only be called from the main thread".
[DisallowMultipleComponent]
public class AudioBassProbe : MonoBehaviour
{
    [Header("Filtre basses (Hz)")]
    [Range(60f, 400f)] public float lowpassCutoff = 180f;  // fréquence de coupure (kick zone)

    [Header("Enveloppe (secondes)")]
    [Tooltip("Temps pour monter (~90%)")]
    [Range(0.001f, 0.5f)] public float attackTime = 0.04f;
    [Tooltip("Temps pour redescendre (~90%)")]
    [Range(0.02f, 2f)] public float releaseTime = 0.35f;

    // Sorties publiques (lisibles dans Update côté main thread)
    public float BassEnvelope { get; private set; }  // énergie basses lissée
    public float RawRms { get; private set; }  // RMS global (info)

    // --- internals
    float _sr = 48000f;        // sample rate cachée (main thread)
    float _lpL, _lpR;          // états filtre LP
    float _env;                // suiveur d’enveloppe interne

    void Awake()
    {
        CacheSampleRate();
    }

    void OnEnable()
    {
        CacheSampleRate();
        AudioSettings.OnAudioConfigurationChanged += OnAudioCfgChanged;
    }

    void OnDisable()
    {
        AudioSettings.OnAudioConfigurationChanged -= OnAudioCfgChanged;
    }

    void CacheSampleRate()
    {
        // APPEL MAIN THREAD → OK
        var cfg = AudioSettings.GetConfiguration();
        _sr = (cfg.sampleRate > 0) ? cfg.sampleRate : AudioSettings.outputSampleRate;
        if (_sr <= 0) _sr = 48000f; // fallback sûr
    }

    void OnAudioCfgChanged(bool deviceWasChanged)
    {
        // si la sample rate change (cas rare), on la recache sur le main thread
        CacheSampleRate();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (data == null || data.Length == 0) return;

        // Pré-calc pour ce buffer (UNIQUEMENT du math, pas d'API Unity)
        float sr = _sr;
        float dt = 1f / Mathf.Max(1f, sr);

        // LP 1er ordre (bilinéaire simplifié)
        float wc = 2f * Mathf.PI * Mathf.Clamp(lowpassCutoff, 20f, sr * 0.45f);
        float alpha = wc / (wc + sr);              // 0..1

        // Coeffs attack/release per-sample (temps vers ~90%)
        float atk = 1f - Mathf.Exp(-2.2f * dt / Mathf.Max(0.001f, attackTime));
        float rel = 1f - Mathf.Exp(-2.2f * dt / Mathf.Max(0.02f, releaseTime));

        double sumSq = 0.0;

        for (int i = 0; i < data.Length; i += channels)
        {
            float xL = data[i];
            float xR = (channels > 1) ? data[i + 1] : xL;

            // Low-pass sur chaque canal
            _lpL += alpha * (xL - _lpL);
            _lpR += alpha * (xR - _lpR);

            // Basses moyennées
            float bass = 0.5f * (_lpL + _lpR);

            // Rectification (énergie instantanée)
            float rect = bass * bass;

            // Suiveur d'enveloppe attack/release
            float coeff = (rect > _env) ? atk : rel;
            _env += (rect - _env) * coeff;

            // RMS global (info)
            sumSq += 0.5 * (xL * xL + xR * xR);
        }

        RawRms = Mathf.Sqrt((float)(sumSq / (data.Length / channels)));
        BassEnvelope = _env; // valeur stable pour le main thread
    }
}
