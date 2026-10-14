using System.Collections;
using BNG;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Teleports the player when they press the confirm button inside this trigger.
// In the additive crime scene everything is loaded at once, so the trigger moves
// the player to a SpawnPoint (looked up by id). targetScene is the legacy
// whole-scene switch, used only when no spawn point id is set.
public class SceneTeleportTrigger : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Id of a SpawnPoint to move the player to (additive scenes). Takes priority over Target Scene.")]
    public string targetSpawnPoint;

    [Tooltip("Legacy: scene to load (single mode) when Target Spawn Point is empty.")]
    public string targetScene;

    [Header("Grabbers (found on the player at runtime if left empty)")]
    [SerializeField] private Grabber leftGrabber;
    [SerializeField] private Grabber rightGrabber;

    [Header("Optional UI Prompt")]
    [SerializeField] private GameObject promptText; // Can drag a text to put in front of player for UExperience so they know can teleport

    [Tooltip("If Prompt Text is empty (the player rig lives in another scene), the prompt object is found by this name at runtime.")]
    [SerializeField] private string promptTextName = "";

    [Tooltip("Optional text written into the prompt when it is shown, so several triggers can share one prompt object.")]
    [SerializeField] private string promptMessage = "";

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
        SetPromptVisible(false);
    }

    private void Start()
    {
        if (promptText == null && !string.IsNullOrEmpty(promptTextName))
            promptText = FindSceneObject(promptTextName);

        // Make sure prompt starts hidden by default
        SetPromptVisible(false);

        // The player rig lives in another scene (CrimeScene_Main), so the
        // grabbers can't be wired in the Inspector — find them on the rig.
        if (leftGrabber == null || rightGrabber == null)
        {
            foreach (Grabber grabber in FindObjectsByType<Grabber>(FindObjectsSortMode.None))
            {
                if (grabber.HandSide == ControllerHand.Left && leftGrabber == null)
                    leftGrabber = grabber;
                else if (grabber.HandSide == ControllerHand.Right && rightGrabber == null)
                    rightGrabber = grabber;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) // Only if Player collides "if" block gets skipped
            return; // Player has to be tagged as "Player" in inspector

        playerInside = true;

        // Show prompt when player enters trigger
        SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) // Ignore anything that is not tagged Player
            return;

        playerInside = false;

        // Hide prompt when player leaves trigger
        SetPromptVisible(false);
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
        SetPromptVisible(false);

        if (!string.IsNullOrEmpty(targetSpawnPoint))
        {
            SpawnPoint spawn = SpawnPoint.Find(targetSpawnPoint);
            if (spawn == null)
                Debug.LogWarning($"[SceneTeleportTrigger] SpawnPoint '{targetSpawnPoint}' not found in the loaded scenes.");
            else
                spawn.TeleportPlayerHere();

            // The player leaves the trigger without OnTriggerExit firing reliably
            // (it's a teleport, not a walk) — reset so the next visit works.
            playerInside = false;
            isTeleporting = false;
            yield break;
        }

        SceneManager.LoadScene(targetScene); /* Target scene is the exact name of the scene in "Scene List",
                                                that has to be on in Build Profiles menu File -> Build Profiles */
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptText == null)
            return;

        if (visible && !string.IsNullOrEmpty(promptMessage))
        {
            TMP_Text label = promptText.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = promptMessage;
        }

        promptText.SetActive(visible);
    }

    // Finds an object by name across all loaded scenes, including inactive ones.
    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == objectName)
                return t.gameObject;
        }
        return null;
    }
}
