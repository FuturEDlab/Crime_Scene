using UnityEngine;

// Opts a specific door OUT of the gaze-door system (GazeDoorSystem.cs).
//
// Add this component to a DoorWithHandle instance in the scene (the root or any
// child) and that door is left completely untouched: it keeps its original BNG
// physics behaviour — hinge, grabbable handle, self-closing and all.
//
// Note: the hand-scaling bug that motivated the gaze system lives in the BNG
// handle grab, so an excluded door has that bug again. Use for doors that must
// stay interactive for gameplay reasons.
public class GazeDoorExclude : MonoBehaviour
{
}
