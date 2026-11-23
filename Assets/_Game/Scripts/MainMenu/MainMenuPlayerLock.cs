// Keeps the player in place while in the Main_Menu scene: turns off the
// rig's locomotion (smooth movement, teleport, stick turning, climbing and the
// locomotion toggle) so only the UI pointer is usable. The tablet is not in
// this scene at all, so there are no iPad controls to block.
//
// LocomotionManager re-enables movement in its own Start, so this runs late
// and again one frame later.

using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class MainMenuPlayerLock : MonoBehaviour
{
    private IEnumerator Start()
    {
        Lock();
        yield return null;
        Lock();
    }

    private static void Lock()
    {
        // Manager first, so it can't switch the others back on.
        Disable<BNG.LocomotionManager>();
        Disable<BNG.SmoothLocomotion>();
        Disable<BNG.PlayerTeleport>();
        Disable<BNG.PlayerRotation>();
        Disable<BNG.PlayerClimbing>();
    }

    private static void Disable<T>() where T : Behaviour
    {
        foreach (T component in FindObjectsByType<T>(FindObjectsInactive.Include))
            component.enabled = false;
    }
}
