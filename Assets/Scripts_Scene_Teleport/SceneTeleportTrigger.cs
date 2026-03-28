using System.Collections;
using BNG;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneTeleportTrigger : MonoBehaviour
{
    [Header("Scene")]
    public string targetScene;

    [Header("Grabbers")]
    [SerializeField] private Grabber leftGrabber;
    [SerializeField] private Grabber rightGrabber;

    [Header("Optional UI Prompt")]
    [SerializeField] private GameObject promptText; // Can drag a text to put in front of player for UExperience so they know can teleport

    [Header("Button to teleport")]
    [SerializeField] private InputActionProperty confirmAction; // assign keyboard key + Meta Quest button in Inspector

    private bool playerInside = false;
    private bool isTeleporting = false;

    private void OnEnable()
    {
        // Enable the input action when this object/script becomes active
        confirmAction.action?.Enable();
    }

    private void OnDisable()
    {
        // Disable input action when object/script gets disabled or scene unloads
        confirmAction.action?.Disable();

        // Reset state so it does not stay stuck after scene changes
        playerInside = false;
        isTeleporting = false;

        // Hide prompt when object disables
        if (promptText != null)
            promptText.SetActive(false);
    }

    private void Start()
    {
        // Make sure prompt starts hidden by default
        if (promptText != null)
            promptText.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) // Only if Player collides "if" block gets skipped
            return; // Player has to be tagged as "Player" in inspector

        playerInside = true;

        // Show prompt when player enters trigger
        if (promptText != null)
            promptText.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) // Ignore anything that is not tagged Player
            return;

        playerInside = false;

        // Hide prompt when player leaves trigger
        if (promptText != null)
            promptText.SetActive(false);
    }

    private void Update()
    {
        // Do nothing if player is not inside trigger
        // Also do nothing if teleport already started
        if (!playerInside || isTeleporting)
            return;

        // WasPressedThisFrame means the assigned input was pressed this frame only
        // This can be A button on Quest, keyboard key, or both depending on bindings you assign in Inspector
        if (confirmAction.action != null && confirmAction.action.WasPressedThisFrame())
        {
            isTeleporting = true;
            StartCoroutine(ReleaseAndTeleport());
        }
    }

    private IEnumerator ReleaseAndTeleport()
    {
        // Ask both hands to release anything they are holding
        if (leftGrabber != null)
            leftGrabber.ForceRelease = true;

        if (rightGrabber != null)
            rightGrabber.ForceRelease = true;

        yield return null; // wait 1 frame for safety. Coroutine was used for this reason to be able to use yield
                           // VRIF checks ForceRelease in Grabber.Update(), so we give it 1 frame to process release first

        // Hide prompt before switching scenes
        if (promptText != null)
            promptText.SetActive(false);

        SceneManager.LoadScene(targetScene); /* Target scene is the exact name of the scene in "Scene List",
                                                that has to be on in Build Profiles menu File -> Build Profiles */
    }
}