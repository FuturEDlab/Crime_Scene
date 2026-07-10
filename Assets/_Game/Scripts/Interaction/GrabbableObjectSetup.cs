using UnityEngine;

// Drop this on any object (e.g. a cube) and click "Apply Grabbable Setup" in the Inspector,
// or use GameObject > Crime Scene > Make Grabbable on the selection.
//
// Requires the BNG VR Interaction Framework Grabber on the player rig (already in XR Rig Rigidbody).
[DisallowMultipleComponent]
public class GrabbableObjectSetup : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("Run validation on Start and log any remaining issues.")]
    [SerializeField] private bool validateOnStart = true;

    [ContextMenu("Apply Grabbable Setup")]
    public void ApplySetup()
    {
        GrabbableSetupUtility.Apply(gameObject);
    }

    private void Start()
    {
        if (Application.isPlaying)
            GrabbableSetupUtility.BoostHandGrabRangeOnce();

        if (validateOnStart)
            GrabbableSetupUtility.Validate(gameObject);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Auto-configure when the component is first added in the editor.
        GrabbableSetupUtility.Apply(gameObject, logResult: false);
    }
#endif
}
