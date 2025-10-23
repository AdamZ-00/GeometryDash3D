using UnityEngine;
using System.Collections;

public class Portals : MonoBehaviour
{
    [Header("Lien vers le portail de sortie")]
    public Transform targetPortals;

    [Header("Réglages")]
    public bool isEntryPortals = true;
    public float teleportCooldown = 0.5f;
    public float exitOffsetY = 0.5f;

    private bool canTeleport = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!isEntryPortals || !canTeleport) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && targetPortals != null)
        {
            Debug.Log($"Téléportation en cours depuis {gameObject.name} vers {targetPortals.name}");
            StartCoroutine(TeleportPlayer(player));
        }
        else
        {
            Debug.LogWarning("Téléportation annulée : Player ou TargetPortals manquant !");
        }
    }

    private IEnumerator TeleportPlayer(PlayerController player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Aucun Rigidbody sur le Player !");
            yield break;
        }

        // Sécurise l’accès au script Portals sur la cible
        Portals targetPortalsScript = targetPortals.GetComponent<Portals>();
        if (targetPortalsScript == null)
        {
            Debug.LogError("Le portail de sortie n’a pas le script Portals.cs !");
            yield break;
        }

        // Bloque les portails temporairement
        canTeleport = false;
        targetPortalsScript.canTeleport = false;

        // Sauvegarde la vélocité pour ne pas freeze le joueur
        Vector3 savedVelocity = rb.linearVelocity;

        // Déplace le joueur au portail de sortie
        player.transform.position = targetPortals.position + Vector3.up * exitOffsetY;
        rb.linearVelocity = savedVelocity;

        Debug.Log("Joueur téléporté avec succès");

        // Délai pour éviter double trigger
        yield return new WaitForSeconds(teleportCooldown);

        canTeleport = true;
        targetPortalsScript.canTeleport = true;
    }
}
