using UnityEngine;

public class KillZone : MonoBehaviour
{
    private LevelManagerLogic level;

    private void Awake()
    {
        level = FindObjectOfType<LevelManagerLogic>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Si niveau terminé, on ne fait plus rien
        if (level != null && level.IsLevelFinished) return;

        // Demande au contrôleur joueur de se respawn
        var ctrl = other.GetComponent<PlayerController>();
        if (ctrl != null)
        {
            ctrl.Respawn();
            return;
        }

        // Fallback si pas de PlayerController (au cas où)
        if (other.attachedRigidbody != null)
        {
            other.attachedRigidbody.linearVelocity = Vector3.zero;
            other.attachedRigidbody.angularVelocity = Vector3.zero;
        }
        other.transform.position = Vector3.zero; // remplace au besoin
    }
}