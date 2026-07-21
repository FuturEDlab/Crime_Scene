using System.Collections;
using BNG;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TabletRecallInScene : MonoBehaviour
{
    private static TabletRecallInScene _instance;

    // Shared with the gaze-door system (GazeDoorSystem.cs): doors reuse the same
    // "bring iPad" button, and set SuppressRecall while the player is looking at
    // a door so pressing it opens the door WITHOUT also summoning the tablet.
    public static TabletRecallInScene Instance => _instance;
    public InputAction ToggleAction => toggleAction.action;
    public bool SuppressRecall { get; set; }

    [Header("References")]
    [SerializeField] private Transform headCamera;          // VR head (CenterEyeAnchor)
    [SerializeField] private GameObject tabletRoot;         // Tablet_manager already in the scene

    [Header("Input (a button)")]
    [SerializeField] private InputActionProperty toggleAction;  // For tests in Unity editor and VR. Ability to set up keys for recall
                                                                // For this project "A" button on VR was chosen
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
        // Survive scene loads (outside <-> inside) so recall still works everywhere.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshTabletCache();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void Start()
    {
        StartCoroutine(RebindAfterSceneLoad());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindAfterSceneLoad());
    }

    private IEnumerator RebindAfterSceneLoad()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);
        RebindReferences();
    }

    // Re-links the head camera and persisted tablet after a scene change.
    private void RebindReferences()
    {
        Camera main = Camera.main;
        if (main != null)
        {
            headCamera = main.transform;

            // Depth-buffer precision fix: BNG rigs default to a 0.01 near clip,
            // which starves the depth buffer at range — the tablet's UI then
            // z-fights its screen mesh from a few metres away (flickering
            // triangles of shifting shade). Raising the near plane restores
            // precision without visibly clipping the hands.
            if (main.nearClipPlane < 0.05f)
                main.nearClipPlane = 0.05f;
        }

        if (TabletPersist.Instance != null)
            tabletRoot = TabletPersist.Instance.gameObject;

        RefreshTabletCache();

        if (headCamera == null)
            Debug.LogWarning("[TabletRecall] Head camera not found after scene load.");
        if (tabletRoot == null)
            Debug.LogWarning("[TabletRecall] Tablet not found after scene load.");
    }

    private void RefreshTabletCache()
    {
        if (tabletRoot == null)
            return;

        tabletRb = tabletRoot.GetComponentInChildren<Rigidbody>();

        if (tabletColliders == null || tabletColliders.Length == 0)
            tabletColliders = tabletRoot.GetComponentsInChildren<Collider>(true);
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
    

    private void OnTogglePerformed(InputAction.CallbackContext ctx)
    {
        // A gaze-door is targeted — the press belongs to the door, not the tablet.
        if (SuppressRecall)
            return;

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

    private IEnumerator RecallSafely()
    {
        // Disable interaction so ray/hover/grab systems cannot affect the tablet
        SetInteractionEnabled(false);

        // Compute target position in front of the player's head. The head's
        // forward is FLATTENED to the horizon first: using the raw forward
        // while the player looks up/down shoots the tablet toward the ceiling
        // or the floor ("spawns far away"). The offsets are world-vertical and
        // horizontal-right for the same reason.
        Vector3 flatForward = Vector3.ProjectOnPlane(headCamera.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.001f) // looking straight up/down
            flatForward = Vector3.ProjectOnPlane(headCamera.up, Vector3.up);
        flatForward.Normalize();
        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward);

        // Never spawn the tablet inside geometry: standing close to a wall or a
        // just-opened door and recalling at the fixed distance placed the tablet
        // INSIDE the surface, and physics depenetration then launched it across
        // the room ("spawns far away"). Spherecast toward the spawn point and
        // pull it closer when something is in the way. The tablet's own
        // colliders are already disabled here (SetInteractionEnabled(false))
        // and the player rig is filtered out explicitly.
        float spawnDistance = forwardDistance;
        RaycastHit[] obstructions = Physics.SphereCastAll(
            headCamera.position, 0.15f, flatForward,
            forwardDistance + 0.1f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in obstructions)
        {
            if (hit.collider.GetComponentInParent<BNGPlayerController>() != null)
                continue; // player body / hands
            if (hit.distance <= 0.001f)
                continue; // spherecast started inside this collider — unreliable
            spawnDistance = Mathf.Min(spawnDistance, Mathf.Max(0.3f, hit.distance - 0.12f));
        }

        Vector3 targetPos =
            headCamera.position
            + flatForward * spawnDistance
            - Vector3.up * downOffset
            + flatRight * rightOffset;

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

        // If the placement still overlapped something, the physics step just ran
        // and gave the tablet an ejection velocity — kill it so the tablet stays
        // where it was summoned instead of drifting off.
        if (resetPhysicsOnShow && tabletRb != null && !tabletRb.isKinematic)
        {
            tabletRb.linearVelocity = Vector3.zero;
            tabletRb.angularVelocity = Vector3.zero;
        }

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
