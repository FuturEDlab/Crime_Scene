using UnityEngine;
using UnityEngine.InputSystem;

public class TabletSpawnerToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform headCamera;          // Main Camera transform
    [SerializeField] private GameObject tabletPrefab;       // Tablet prefab (your iPad)
    
    [Header("Input (A button)")]
    [SerializeField] private InputActionProperty toggleAction; 
    // For Quest right controller A: usually "XRI RightHand/PrimaryButton"

    [Header("Spawn offset relative to camera")]
    [SerializeField] private float forwardDistance = 0.6f;
    [SerializeField] private float downOffset = 0.15f;
    [SerializeField] private float rightOffset = 0.15f;
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(0f, 90f, 0f);

    

    private GameObject tabletInstance;
    private bool isVisible;

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
        ToggleTablet();
    }

    private void ToggleTablet()
    {
        // Create if needed
        if (tabletInstance == null)
        {
            tabletInstance = Instantiate(tabletPrefab);
            tabletInstance.SetActive(false);
        }

        isVisible = !isVisible;
        tabletInstance.SetActive(isVisible);

        if (isVisible)
            MoveTabletInFrontOfPlayer();
    }

    private void MoveTabletInFrontOfPlayer()
    {
        if (headCamera == null) return;

        // Position in front of the camera with offsets
        Vector3 pos =
            headCamera.position
            + headCamera.forward * forwardDistance
            - headCamera.up * downOffset
            + headCamera.right * rightOffset;

        tabletInstance.transform.position = pos;

        // Rotate tablet to face the player (only yaw, so it doesn't tilt weirdly)
        Vector3 lookDir = headCamera.position - tabletInstance.transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            tabletInstance.transform.rotation =
                Quaternion.LookRotation(-lookDir.normalized, Vector3.up) * Quaternion.Euler(rotationOffsetEuler);
        }

        
    }
}
