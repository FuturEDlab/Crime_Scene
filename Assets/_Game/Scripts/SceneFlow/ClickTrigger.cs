using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Calls SkipButton.Skip() when this 3D object is selected.
// VR: an XRSimpleInteractable is added at runtime, so the XRI ray/poke can select it.
// Desktop: a left mouse click on the object's collider also works (Input System).
// Needs a Collider on this GameObject and an XR Interaction Manager in the scene.
public class ClickTrigger : MonoBehaviour
{
    public SkipButton skipButton;

    private XRSimpleInteractable interactable;
    private Collider ownCollider;

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();

        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame || ownCollider == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (ownCollider.Raycast(ray, out _, 100f))
            Trigger();
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Trigger();
    }

    private void Trigger()
    {
        if (skipButton != null)
            skipButton.Skip();
    }
}
