using UnityEngine;

// Legacy marker from the removed gaze-door system: it opted a door out of the
// gaze/trigger interaction. Doors are now propped open for good by
// Tools > Crime Scene > Prop All Doors Open, which also removes this component.
// Kept only so scenes that still carry it don't show a missing script; delete
// this file once that tool has been run and the scenes saved.
public class GazeDoorExclude : MonoBehaviour
{
}
