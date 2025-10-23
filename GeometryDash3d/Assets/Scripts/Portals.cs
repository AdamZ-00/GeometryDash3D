using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Portals : MonoBehaviour
{
    [Header("Lien vers le portail de sortie")]
    public Transform targetPortals;

    [Header("Réglages")]
    public bool isEntryPortals = true;
    public float teleportCooldown = 0.5f;
    public float exitOffsetY = 0.5f;

    private bool canTeleport = true;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isEntryPortals || !canTeleport) return;

        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player != null && targetPortals != null)
        {
            StartCoroutine(TeleportPlayer(player));
        }
        else
        {
            Debug.LogWarning("Téléportation annulée : Player ou targetPortals manquant !");
        }
    }

    private IEnumerator TeleportPlayer(PlayerController player)
    {
        var rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Aucun Rigidbody sur le Player !");
            yield break;
        }

        // Sécurise l’accès au script du portail cible
        var targetScript = targetPortals.GetComponent<Portals>();
        if (targetScript == null)
        {
            Debug.LogError("Le portail de sortie n’a pas le script Portals.cs !");
            yield break;
        }

        // Bloque l’entrée/sortie pendant le transfert
        canTeleport = false;
        targetScript.canTeleport = false;

        // Sauvegarde la vélocité pour une transition propre
        Vector3 savedVelocity = rb.linearVelocity;

        // Téléporte à la sortie (avec léger offset Y pour éviter recollision)
        Vector3 outPos = targetPortals.position + Vector3.up * exitOffsetY;
        player.transform.position = outPos;

        // >>> Aligne la VOIE sur la position du portail OUT <<<
        // (empêche le PlayerController de te ramener sur l'ancienne voie)
        player.ForceLaneByWorldX(targetPortals.position.x, snapPosition: true);

        // Restaure la vélocité (optionnel: tu peux aussi tourner la vitesse selon l’orientation du portail)
        rb.linearVelocity = savedVelocity;

        // Anti double-trigger
        yield return new WaitForSeconds(teleportCooldown);
        canTeleport = true;
        targetScript.canTeleport = true;
    }
}
