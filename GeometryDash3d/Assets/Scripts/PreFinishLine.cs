using System.Collections;
using UnityEngine;

public class PreFinishLine : MonoBehaviour
{
    [Header("Références")]
    public Transform fireworksRig;         // Référence vers le GameObject contenant les feux d’artifice
    public AudioSource winAudio;           // Son de pré-victoire (facultatif)

    [Header("Paramètres")]
    public float fireworksDuration = 5f;   // Durée d'affichage
    public bool oneShot = true;            // Ne se déclenche qu’une fois

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (oneShot && triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        PlayFireworks();
    }

    void PlayFireworks()
    {
        if (fireworksRig == null) return;

        // Démarre tous les systèmes de particules enfants
        var systems = fireworksRig.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
            ps.Play(true);

        // Joue le son si dispo
        if (winAudio != null)
            winAudio.Play();

        // Stoppe après X secondes
        if (fireworksDuration > 0f)
            StartCoroutine(StopAfter(fireworksDuration));
    }

    IEnumerator StopAfter(float t)
    {
        yield return new WaitForSeconds(t);

        var systems = fireworksRig.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (winAudio != null)
            winAudio.Stop();
    }
}