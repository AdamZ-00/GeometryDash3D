using UnityEngine;
using System;

public class BpmConductor : MonoBehaviour
{
    public enum Subdivision { Beat = 1, Half = 2, Quarter = 4, Eighth = 8, Sixteenth = 16 }

    [Header("Music")]
    public AudioSource music;
    public bool autoPlayAndSync = true;
    [Range(0.05f, 1.0f)] public double startDelay = 0.1;

    [Header("Tempo")]
    public double bpm = 140.0;
    public double offsetSeconds = 0.0;
    public Subdivision subdivision = Subdivision.Quarter;

    [Header("Debug")]
    public bool debugLogBeats = true;      // affiche “Beat #x” dans la Console
    public bool showPhaseGizmo = false;    // optionnel

    public event Action<int> OnBeat;
    public int BeatIndex { get; private set; } = 0;

    double _anchorDsp;
    double _nextBeatDsp;
    bool _synced = false;
    double Interval => 60.0 / bpm / (double)subdivision;

    void Start()
    {
        if (!music) music = GetComponent<AudioSource>();
        if (!music)
        {
            Debug.LogError("[BpmConductor] Aucun AudioSource assigné.");
            enabled = false;
            return;
        }

        if (autoPlayAndSync)
        {
            var dspStart = AudioSettings.dspTime + startDelay;
            music.PlayScheduled(dspStart);
            _anchorDsp = dspStart + offsetSeconds;
            _nextBeatDsp = _anchorDsp;
            _synced = true;
        }
    }

    void Update()
    {
        if (!_synced)
        {
            // Si on n'a pas auto-synchronisé : dès que la musique joue, on cale l'ancre
            if (music.isPlaying)
            {
                _anchorDsp = AudioSettings.dspTime + offsetSeconds;
                _nextBeatDsp = _anchorDsp;
                _synced = true;
            }
            else return;
        }

        double now = AudioSettings.dspTime;
        int guard = 0;
        while (now + 1e-6 >= _nextBeatDsp && guard++ < 64)
        {
            if (debugLogBeats) Debug.Log($"[BpmConductor] Beat #{BeatIndex}");
            OnBeat?.Invoke(BeatIndex++);
            _nextBeatDsp += Interval;
        }
    }

    public float GetPhase01()
    {
        double now = AudioSettings.dspTime;
        double t = now - (_nextBeatDsp - Interval);
        return Mathf.Clamp01((float)(t / Interval));
    }

    void OnDrawGizmosSelected()
    {
        if (!showPhaseGizmo) return;
        float p = GetPhase01();
        Gizmos.color = Color.Lerp(Color.blue, Color.red, p);
        Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.1f + 0.1f * p);
    }
}
