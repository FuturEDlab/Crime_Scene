using UnityEngine;

// Optional per-door tweak for the gaze-door system (GazeDoorSystem.cs). Add
// this to a specific DoorWithHandle instance (root or any child) when the
// default 90° swing opens the door into a wall — set a negative angle to flip
// the swing direction, or a smaller value for a partially opened door.
public class DoorPropAngleOverride : MonoBehaviour
{
    [Tooltip("Degrees to rotate this door open around its hinge. " +
             "Negative flips the swing direction. Default used elsewhere is 90.")]
    public float openAngle = -90f;
}
