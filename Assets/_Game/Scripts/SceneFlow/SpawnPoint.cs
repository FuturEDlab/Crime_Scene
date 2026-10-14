// A named place the player can be moved to. Lives in CrimeScene_Gameplay and
// is found at runtime by id, so triggers in other scenes never need a direct
// (cross-scene) Inspector reference to it.

using BNG;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Unique id other systems use to find this spawn point, e.g. PlayerStart_Outside.")]
    public string id = "";

    public static SpawnPoint Find(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
            return null;

        foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (point.id == spawnId)
                return point;
        }
        return null;
    }

    // Moves the player rig here using BNG's own teleport (handles rigidbody and
    // CharacterController rigs, and resets velocity).
    public bool TeleportPlayerHere()
    {
        PlayerTeleport teleport = FindFirstObjectByType<PlayerTeleport>();
        if (teleport == null)
        {
            Debug.LogWarning($"[SpawnPoint] No PlayerTeleport found — cannot move player to '{id}'.");
            return false;
        }

        teleport.TeleportPlayer(transform.position, transform.rotation);
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
    }
}
