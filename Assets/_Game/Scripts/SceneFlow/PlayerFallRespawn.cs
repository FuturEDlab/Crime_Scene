// Puts the player back on the map when they fall off it or end up past the
// map border (MapBoundary).
//
// BNG's own MinElevation reset only fires far below the map and sends the
// player all the way back to the starting position. This catches them much
// sooner — below fallHeight, or as soon as the head/body is outside the border
// (taking the headset off for a while can make tracking jump the player across
// or off the map) — and returns them to the last spot they were standing on,
// kept a little inside the border. With nothing recorded yet, the nearest
// SpawnPoint is used, which keeps the player on the side of the front door
// (inside / outside) they were on.

using System.Collections;
using BNG;
using UnityEngine;

public class PlayerFallRespawn : MonoBehaviour
{
    [Header("Player (found at runtime if left empty)")]
    [SerializeField] private BNGPlayerController player;
    [SerializeField] private PlayerTeleport teleport;

    [Header("Triggers")]
    [Tooltip("Respawn when the player drops below this world height.")]
    [SerializeField] private float fallHeight = -10f;

    [Tooltip("Respawn when the head or body is this far outside the map border (m). Small values allow leaning into the wall.")]
    [SerializeField] private float outOfBoundsTolerance = 0.75f;

    [Header("Respawn point")]
    [Tooltip("How far inside the border the player is placed when respawning near the edge (m).")]
    [SerializeField] private float edgeInset = 1f;

    [Tooltip("How often the last safe standing position is recorded (s).")]
    [SerializeField] private float recordInterval = 0.25f;

    [Tooltip("Player counts as standing when there is floor at most this far below their feet (m).")]
    [SerializeField] private float groundedDistance = 0.3f;

    [Tooltip("Used only when no safe position has been recorded yet; the nearest one is chosen.")]
    [SerializeField] private string[] fallbackSpawnPoints = { "PlayerStart_Outside", "PlayerStart_Inside" };

    private Vector3 lastSafePosition;
    private bool hasSafePosition;
    private float nextRecordTime;
    private bool respawning;

    private void Update()
    {
        if (!FindPlayer() || respawning)
            return;

        Vector3 body = player.transform.position;
        Vector3 head = Camera.main != null ? Camera.main.transform.position : body;

        if (body.y < fallHeight || IsOutOfBounds(body) || IsOutOfBounds(head))
        {
            StartCoroutine(Respawn(body, head));
            return;
        }

        if (Time.time >= nextRecordTime)
        {
            nextRecordTime = Time.time + recordInterval;
            RecordSafePosition(body);
        }
    }

    private bool FindPlayer()
    {
        if (player == null)
            player = FindAnyObjectByType<BNGPlayerController>();
        if (teleport == null && player != null)
            teleport = player.GetComponentInChildren<PlayerTeleport>();
        if (teleport == null)
            teleport = FindAnyObjectByType<PlayerTeleport>();
        return player != null && teleport != null;
    }

    private static MapBoundary ActiveBoundary()
    {
        MapBoundary boundary = MapBoundary.Instance;
        return boundary != null && boundary.isActiveAndEnabled ? boundary : null;
    }

    private bool IsOutOfBounds(Vector3 point)
    {
        MapBoundary boundary = ActiveBoundary();
        return boundary != null && !boundary.Contains(point) && boundary.DistanceToBorder(point) > outOfBoundsTolerance;
    }

    private void RecordSafePosition(Vector3 body)
    {
        // BNG's DistanceFromGround is skewed by this rig's capsule offsets
        // (~0.25 while standing), so check for floor under the feet directly.
        const float rayStart = 0.5f;
        int notPlayer = Physics.DefaultRaycastLayers & ~(1 << player.gameObject.layer);
        bool grounded = Physics.Raycast(body + Vector3.up * rayStart, Vector3.down,
            rayStart + groundedDistance, notPlayer, QueryTriggerInteraction.Ignore);
        bool justTeleported = Time.time - player.LastTeleportTime < 0.5f;
        MapBoundary boundary = ActiveBoundary();
        bool insideBorder = boundary == null || boundary.Contains(body);

        if (grounded && !justTeleported && insideBorder && body.y >= fallHeight)
        {
            lastSafePosition = body;
            hasSafePosition = true;
        }
    }

    private IEnumerator Respawn(Vector3 body, Vector3 head)
    {
        respawning = true;

        Vector3 destination = hasSafePosition ? lastSafePosition : NearestFallbackSpawn(body);

        MapBoundary boundary = ActiveBoundary();
        if (boundary != null)
            destination = boundary.ClosestPointInside(destination, edgeInset);

        Debug.Log($"[PlayerFallRespawn] Player left the map at {body}; respawning at {destination}.");

        // A tracking jump leaves the head far from the body. Pull the camera rig
        // back over the body first, or the head would land outside the border
        // again after the teleport.
        if (player.CameraRig != null)
        {
            Vector3 headOffset = head - body;
            headOffset.y = 0f;
            player.CameraRig.position -= headOffset;
        }

        teleport.TeleportPlayer(destination, player.transform.rotation);

        // Wait out the teleport (delay + fade) before checking again.
        yield return new WaitForSeconds(teleport.TeleportDelay + 0.5f);
        respawning = false;
    }

    private Vector3 NearestFallbackSpawn(Vector3 from)
    {
        SpawnPoint nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (string id in fallbackSpawnPoints)
        {
            SpawnPoint point = SpawnPoint.Find(id);
            if (point == null)
                continue;

            float d = Vector2.Distance(new Vector2(from.x, from.z),
                new Vector2(point.transform.position.x, point.transform.position.z));
            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearest = point;
            }
        }

        if (nearest != null)
            return nearest.transform.position;

        Debug.LogWarning("[PlayerFallRespawn] No safe position or fallback SpawnPoint; respawning at the world origin.");
        return Vector3.zero;
    }
}
