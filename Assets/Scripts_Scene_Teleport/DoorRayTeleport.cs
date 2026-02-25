using UnityEngine;
using UnityEngine.InputSystem;

public class DoorRayTeleport : MonoBehaviour
{
    [Header("Destination Asset")]
    [SerializeField] private SceneDestination destination;

    [Header("Prompt UI (World Space Canvas)")]
    [SerializeField] private GameObject promptUI; // text like "Exit outside"

    [Header("Ray Origin (controller that shoots the laser)")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float rayDistance = 6f;

    [Header("Input (press to teleport)")]
    [SerializeField] private InputActionProperty confirmAction;

    [Header("Proximity Trigger (IsTrigger)")]
    [SerializeField] private Collider proximityTrigger;

    private bool playerNear;
    private bool aiming;

    private void Awake()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            aiming = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerNear || rayOrigin == null) return;

        // Raycast to see if the laser is pointing at THIS door (or its children)
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        bool hitThisDoor =
            Physics.Raycast(ray, out RaycastHit hit, rayDistance) &&
            hit.collider != null &&
            (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform));

        if (hitThisDoor != aiming)
        {
            aiming = hitThisDoor;
            if (promptUI != null) promptUI.SetActive(aiming);
        }

        // Press button to teleport
        if (aiming && confirmAction.action.WasPressedThisFrame())
        {
            if (SceneTransitionManager.Instance != null && destination != null)
                SceneTransitionManager.Instance.Go(destination);
        }
    }
}