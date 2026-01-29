using UnityEngine;
using TMPro;

public class DoorScenePortal : MonoBehaviour
{
    [Header("Scene Transition")]
    public string targetSceneName;
    public string targetSpawnPointName;

    [Header("Prompt UI")]
    public Canvas promptCanvas;
    public TMP_Text promptText;
    public string promptMessage = "Go Inside (E)";

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;

    private bool playerInRange;

    private void Start()
    {
        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogError("SceneTransitionManager not found.");
                return;
            }

            SceneTransitionManager.Instance.TransitionTo(targetSceneName, targetSpawnPointName);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(true);

        if (promptText != null)
            promptText.text = promptMessage;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(false);
    }
}