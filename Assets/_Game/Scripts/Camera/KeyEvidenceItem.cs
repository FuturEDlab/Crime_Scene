using UnityEngine;

// Marks a scene object as a "key item" the player is expected to photograph.
// The iPad camera looks for any active KeyEvidenceItem inside its view when a
// photo is taken, and the EvidenceGradingManager records it as found evidence
// that counts towards the player's pass/fail grade.
//
// Put this on the root of the object (or anywhere the bounds are correct) and
// give it a unique evidenceId so the same item is never double-counted.
public class KeyEvidenceItem : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique id for this piece of evidence. Used so the same item is only counted once.")]
    public string evidenceId = "";

    [Tooltip("Friendly name shown in logs / future UI.")]
    public string displayName = "Evidence";

    [Header("Detection")]
    [Tooltip("Renderer used to test whether the item is inside the camera view. " +
             "If left empty it is auto-found in children on Awake.")]
    [SerializeField] private Renderer detectionRenderer;

    public Renderer DetectionRenderer => detectionRenderer;

    private void Awake()
    {
        if (string.IsNullOrEmpty(evidenceId))
        {
            // Fall back to a stable id so the item still works if the field is left blank.
            evidenceId = displayName + "_" + GetInstanceID();
        }

        if (detectionRenderer == null)
            detectionRenderer = GetComponentInChildren<Renderer>();
    }

    private void OnEnable()
    {
        if (EvidenceGradingManager.Instance != null)
            EvidenceGradingManager.Instance.Register(this);
    }

    // Returns the world-space bounds used for the in-view test.
    public bool TryGetBounds(out Bounds bounds)
    {
        if (detectionRenderer != null)
        {
            bounds = detectionRenderer.bounds;
            return true;
        }

        bounds = new Bounds(transform.position, Vector3.one * 0.25f);
        return false;
    }
}
