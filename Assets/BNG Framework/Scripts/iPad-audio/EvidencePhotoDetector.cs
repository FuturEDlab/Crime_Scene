using UnityEngine;

public class EvidencePhotoDetector : MonoBehaviour
{
    [Header("Evidence Settings")]
    public float maxDistance = 50f;
    public LayerMask evidenceLayer = ~0; // everything by default

    [Header("Optional: Debug")]
    public bool drawDebugRay = true;

    // Example: store a flag
    public bool evidenceFound;

    public void CheckEvidenceHit(Camera captureCam)
    {
        if (captureCam == null) return;

        Ray ray = new Ray(captureCam.transform.position, captureCam.transform.forward);

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.green, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, evidenceLayer))
        {
            // Mark evidence if object is tagged Evidence
            if (hit.collider.CompareTag("Evidence"))
            {
                evidenceFound = true;
                Debug.Log("✅ Evidence photographed: " + hit.collider.name);

                // TODO: hook into your real backend / grading system here
            }
        }
    }
}