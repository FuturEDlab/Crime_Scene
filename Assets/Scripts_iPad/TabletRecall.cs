using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TabletRecallInScene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform headCamera;          // VR head (CenterEyeAnchor)
    [SerializeField] private GameObject tabletRoot;         // Tablet_manager already in the scene

    [Header("Input (A button)")]
    [SerializeField] private InputActionProperty toggleAction;

    [Header("Spawn offset relative to camera")]
    [SerializeField] private float forwardDistance = 0.6f;
    [SerializeField] private float downOffset = 0.15f;
    [SerializeField] private float rightOffset = 0.15f;

    [Header("Rotation fix")]
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(0f, -90f, 0f);

    [Header("Anti Ray / Grab Bug")]
    [Tooltip("Assign grab / interactable components here (BNG Grabbable, XRGrabInteractable, etc.)")]
    [SerializeField] private Behaviour[] disableTheseWhileRecalling;

    [Tooltip("If empty, all colliders under tabletRoot will be collected automatically")]
    [SerializeField] private Collider[] tabletColliders;

    [Header("Physics Stability")]
    [SerializeField] private bool resetPhysicsOnShow = true;

    private Rigidbody tabletRb;
    private bool isVisible;
    private Coroutine recallRoutine;

    private void Awake()
    {
        if (tabletRoot != null)
        {
            tabletRb = tabletRoot.GetComponentInChildren<Rigidbody>();

            // Auto-collect colliders if not manually assigned
            if (tabletColliders == null || tabletColliders.Length == 0)
                tabletColliders = tabletRoot.GetComponentsInChildren<Collider>(true);
        }
    }

    private void OnEnable()
    {
        if (toggleAction.action != null)
            toggleAction.action.performed += OnTogglePerformed;
    
        toggleAction.action?.Enable();
    }
    
    private void OnDisable()
    {
        if (toggleAction.action != null)
            toggleAction.action.performed -= OnTogglePerformed;
    
        toggleAction.action?.Disable();
    }

    // private void OnTogglePerformed(InputAction.CallbackContext ctx)
    // {
    //     ToggleTablet();
    // }

    private void OnTogglePerformed(InputAction.CallbackContext ctx)
    {
        // Always recall/show on press
        RecallNow();
    }

    private void RecallNow()
    {
        if (tabletRoot == null || headCamera == null) return;

        // Always make it visible
        if (!tabletRoot.activeSelf)
            tabletRoot.SetActive(true);

        // Stop any previous recall coroutine, then recall again
        if (recallRoutine != null)
            StopCoroutine(recallRoutine);

        recallRoutine = StartCoroutine(RecallSafely()); // or your RecallSafely() name
    }

    
    
    // private void ToggleTablet()
    // {
    //     if (tabletRoot == null || headCamera == null)
    //         return;
    //
    //     isVisible = !isVisible;
    //
    //     // Hide tablet
    //     if (!isVisible)
    //     {
    //         tabletRoot.SetActive(false);
    //         return;
    //     }
    //
    //     // Show tablet and recall it safely
    //     tabletRoot.SetActive(true);
    //
    //     if (recallRoutine != null)
    //         StopCoroutine(recallRoutine);
    //
    //     recallRoutine = StartCoroutine(RecallSafely());
    // }

    private IEnumerator RecallSafely()
    {
        // Disable interaction so ray/hover/grab systems cannot affect the tablet
        SetInteractionEnabled(false);

        // Compute target position in front of the player's head
        Vector3 targetPos =
            headCamera.position
            + headCamera.forward * forwardDistance
            - headCamera.up * downOffset
            + headCamera.right * rightOffset;

        // Compute rotation so the tablet faces the player (yaw only)
        Vector3 lookDir = headCamera.position - targetPos;
        lookDir.y = 0f;

        Quaternion targetRot = tabletRoot.transform.rotation;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            targetRot =
                Quaternion.LookRotation(-lookDir.normalized, Vector3.up) *
                Quaternion.Euler(rotationOffsetEuler);
        }

        // Reset physics to prevent drifting or flying away
        if (resetPhysicsOnShow && tabletRb != null)
        {
            tabletRb.linearVelocity = Vector3.zero;
            tabletRb.angularVelocity = Vector3.zero;

            bool wasKinematic = tabletRb.isKinematic;
            tabletRb.isKinematic = true;

            tabletRoot.transform.SetPositionAndRotation(targetPos, targetRot);

            tabletRb.isKinematic = wasKinematic;
        }
        else
        {
            tabletRoot.transform.SetPositionAndRotation(targetPos, targetRot);
        }

        Physics.SyncTransforms();

        // Wait one frame so interaction systems update safely
        yield return null;

        // Re-enable interaction after repositioning
        SetInteractionEnabled(true);
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (tabletColliders != null)
        {
            foreach (Collider col in tabletColliders)
            {
                if (col != null)
                    col.enabled = enabled;
            }
        }

        if (disableTheseWhileRecalling != null)
        {
            foreach (Behaviour behaviour in disableTheseWhileRecalling)
            {
                if (behaviour != null)
                    behaviour.enabled = enabled;
            }
        }
    }
}
