using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    public float interactRange = 3f;
    public LayerMask interactableLayer;

    [Header("Hold Settings")]
    public Transform holdPoint;

    private Camera cam;
    private InteractableItem heldItem;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System (Recommended)")]
    [Tooltip("Optional: Drag an InputActionReference here (ex: XRI RightHand Interaction/Select)")]
    public InputActionReference interactAction;

    [Tooltip("Optional: Drag an InputActionReference here (ex: XRI RightHand Interaction/Activate or a custom Drop action)")]
    public InputActionReference dropAction;

    // Fallback keyboard actions if you don't assign references:
    private InputAction fallbackInteract;
    private InputAction fallbackDrop;
#endif

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        // Enable inspector-assigned actions if present
        if (interactAction != null) interactAction.action.Enable();
        if (dropAction != null) dropAction.action.Enable();

        // Create fallback keyboard bindings (PC testing) if not assigned
        if (interactAction == null)
        {
            fallbackInteract = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            fallbackInteract.Enable();
        }

        if (dropAction == null)
        {
            fallbackDrop = new InputAction("Drop", InputActionType.Button, "<Keyboard>/q");
            fallbackDrop.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null) interactAction.action.Disable();
        if (dropAction != null) dropAction.action.Disable();

        fallbackInteract?.Disable();
        fallbackInteract?.Dispose();
        fallbackInteract = null;

        fallbackDrop?.Disable();
        fallbackDrop?.Dispose();
        fallbackDrop = null;
    }
#endif

    private void Start()
    {
        if (cam == null)
            Debug.LogError("[PlayerInteraction] No camera found on this object and no Main Camera in scene.");

        if (holdPoint == null)
            Debug.LogWarning("[PlayerInteraction] HoldPoint is NOT set. Create one and drag it into the script.");
    }

    private void Update()
    {
        if (cam == null) return;

        // Drop
        if (WasDropPressed() && heldItem != null)
        {
            heldItem.OnDrop();
            heldItem = null;
            return;
        }

        // Interact / Pickup
        if (WasInteractPressed())
        {
            TryInteractOrPickup();
        }
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (interactAction != null) return interactAction.action.WasPressedThisFrame();
        return fallbackInteract != null && fallbackInteract.WasPressedThisFrame();
#else
        return false;
#endif
    }

    private bool WasDropPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (dropAction != null) return dropAction.action.WasPressedThisFrame();
        return fallbackDrop != null && fallbackDrop.WasPressedThisFrame();
#else
        return false;
#endif
    }

    private void TryInteractOrPickup()
    {
        // Ray from center of view
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item != null)
            {
                if (item.isPickupable && heldItem == null)
                {
                    if (holdPoint == null)
                    {
                        Debug.LogWarning("[PlayerInteraction] Can't pick up: HoldPoint not assigned.");
                        return;
                    }

                    item.OnPickup(holdPoint);
                    heldItem = item;
                    return;
                }

                item.Interact();
                return;
            }

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null) interactable.Interact();
            else Debug.Log("[PlayerInteraction] Hit something on Interactable layer but it has no interact script.");
        }
        else
        {
            Debug.Log("[PlayerInteraction] No interactable object detected in range.");
        }
    }
}
