using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Drives the iPad's camera app.
//
// A dedicated Camera is mounted on the tablet and points out of the screen, so
// it always frames whatever the iPad is pointed at. Because the tablet is held
// in the player's hands, pivoting the iPad (even to look behind the player)
// changes what gets photographed - no extra code required.
//
// The camera renders continuously into a RenderTexture that is shown in a
// RawImage "viewfinder" on the Camera screen. Pressing the shutter copies the
// current frame into a Texture2D, scans for any key evidence in view, stores
// the result in the PhotoLibrary and marks found evidence in the grading
// backend.
//
// IMPORTANT: the iPad screen is a World-Space UI canvas (UI layer). If the
// capture camera films that layer it only sees the tablet's own screen ->
// blank viewfinder and a white photo. By default we strip the UI layer from
// the camera so it only photographs the actual world.
[RequireComponent(typeof(Camera))]
public class IpadCamera : MonoBehaviour
{
    [Header("Capture")]
    [Tooltip("Camera that frames the shot. Defaults to the Camera on this object.")]
    [SerializeField] private Camera captureCamera;

    [Tooltip("Resolution of the saved photo / live viewfinder.")]
    [SerializeField] private int photoWidth = 1024;
    [SerializeField] private int photoHeight = 1024;

    [Tooltip("Remove the UI layer from the camera so it never films the iPad's " +
             "own screen (the usual cause of a white photo / blank viewfinder).")]
    [SerializeField] private bool excludeUILayerFromCapture = true;

    [Tooltip("Remove the built-in TransparentFX layer so transparent effects " +
             "don't appear in photos or the live viewfinder.")]
    [SerializeField] private bool excludeTransparentFXFromCapture = true;

    [Tooltip("Any extra layers to hide from the camera (custom VFX, highlights, " +
             "outlines, etc.). These are stripped from the capture in addition " +
             "to UI and TransparentFX above.")]
    [SerializeField] private LayerMask additionalExcludedLayers;

    [Header("Follow (recommended)")]
    [Tooltip("Transform the camera should stick to every frame - assign the part " +
             "of the tablet that physically moves when grabbed (the object with " +
             "the Grabbable / Rigidbody, or the iPad screen). Leave empty to just " +
             "use this object's own transform / normal parenting.")]
    [SerializeField] private Transform followTarget;

    [Tooltip("Local position offset from the follow target (e.g. push the lens to the back of the tablet).")]
    [SerializeField] private Vector3 followPositionOffset = Vector3.zero;

    [Tooltip("Local rotation offset from the follow target, in degrees. " +
             "Use this to aim the lens out of the back of the screen.")]
    [SerializeField] private Vector3 followEulerOffset = Vector3.zero;

    [Header("Viewfinder")]
    [Tooltip("RawImage on the Camera screen that shows the live camera feed.")]
    [SerializeField] private RawImage viewfinder;

    [Tooltip("Optional full-screen overlay briefly shown when a photo is taken. " +
             "Add a CanvasGroup to it for a smooth fade; otherwise it just blinks on/off.")]
    [SerializeField] private GameObject shutterFlash;
    [SerializeField] private float shutterFlashSeconds = 0.2f;

    private CanvasGroup _flashGroup;

    [Header("Key Item Detection")]
    [Tooltip("Max distance (metres) at which a key item still counts as photographed.")]
    [SerializeField] private float maxDetectionDistance = 8f;

    [Tooltip("Require a clear line of sight to the item (raycast occlusion check).")]
    [SerializeField] private bool requireLineOfSight = true;

    [Tooltip("Player's head / main camera. Line of sight is tested from HERE, not " +
             "from the lens, so shoving the tablet through a wall can't reveal " +
             "evidence on the other side. Auto-uses Camera.main if left empty.")]
    [SerializeField] private Transform playerHead;

    [Tooltip("Layers that can block line of sight (walls, furniture, etc.). " +
             "Include the layer(s) your key items live on too.")]
    [SerializeField] private LayerMask occlusionMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private RenderTexture _previewTexture;
    private float _flashTimer;

    private void Awake()
    {
        if (captureCamera == null)
            captureCamera = GetComponent<Camera>();

        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;
    }

    private void OnEnable()
    {
        SetupCamera();
    }

    private void OnDisable()
    {
        // Stop rendering into the texture while the app is closed to save perf.
        if (captureCamera != null)
        {
            captureCamera.targetTexture = null;
            captureCamera.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (_previewTexture != null)
        {
            _previewTexture.Release();
            Destroy(_previewTexture);
            _previewTexture = null;
        }
    }

    private void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;

            // Fade the overlay out over its lifetime for a smooth flash.
            if (_flashGroup != null)
                _flashGroup.alpha = Mathf.Clamp01(_flashTimer / Mathf.Max(0.0001f, shutterFlashSeconds));

            if (_flashTimer <= 0f && shutterFlash != null)
                shutterFlash.SetActive(false);
        }
    }

    // LateUpdate runs after grab/physics has moved the tablet for this frame, so
    // snapping the camera here guarantees the viewfinder tracks the tablet even
    // while it is being held and moved around.
    private void LateUpdate()
    {
        if (followTarget == null)
            return;

        Quaternion rot = followTarget.rotation * Quaternion.Euler(followEulerOffset);
        Vector3 pos = followTarget.TransformPoint(followPositionOffset);
        transform.SetPositionAndRotation(pos, rot);
    }

    private void SetupCamera()
    {
        if (captureCamera == null)
        {
            Debug.LogError("[IpadCamera] No capture Camera assigned/found. " +
                           "This script must sit on a GameObject that has a Camera.");
            return;
        }

        if (_previewTexture == null)
        {
            _previewTexture = new RenderTexture(photoWidth, photoHeight, 24)
            {
                name = "iPadCameraPreview"
            };
            _previewTexture.Create();
        }

        // Never film the iPad's own World-Space UI canvas.
        if (excludeUILayerFromCapture)
            ExcludeLayer("UI");

        // Keep transparent effects out of photos / the viewfinder.
        if (excludeTransparentFXFromCapture)
            ExcludeLayer("TransparentFX");

        // Strip any extra layers the designer flagged (custom VFX, etc.).
        captureCamera.cullingMask &= ~additionalExcludedLayers.value;

        // Render monoscopically to the texture. Without this an XR build (Quest 3)
        // may try to render this camera in stereo, which breaks the preview.
        captureCamera.stereoTargetEye = StereoTargetEyeMask.None;

        captureCamera.targetTexture = _previewTexture;
        // Keep the camera enabled so URP renders it into the preview texture
        // every frame automatically (the correct continuous-render method for
        // the Universal Render Pipeline - do NOT call Camera.Render() manually).
        captureCamera.enabled = true;

        if (viewfinder != null)
        {
            viewfinder.texture = _previewTexture;
            viewfinder.color = Color.white; // make sure it isn't tinted transparent
        }
        else
        {
            Debug.LogWarning("[IpadCamera] No viewfinder RawImage assigned - " +
                             "you won't see a live preview, but capture still works.");
        }

        if (verboseLogging)
            Debug.Log($"[IpadCamera] Camera ready. Rendering layers: [{GetIncludedLayerNames()}]. " +
                      $"clearFlags={captureCamera.clearFlags}, fov={captureCamera.fieldOfView}, " +
                      $"pos={captureCamera.transform.position}.");
    }

    // Lists every layer name the capture camera is currently set to render, so
    // you can see which layer an unwanted effect actually lives on.
    private string GetIncludedLayerNames()
    {
        var names = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((captureCamera.cullingMask & (1 << i)) == 0)
                continue;

            string n = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(n))
                names.Add($"{i}:{n}");
        }
        return string.Join(", ", names);
    }

    // Removes a named layer from the capture camera's culling mask (if it exists).
    private void ExcludeLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            captureCamera.cullingMask &= ~(1 << layer);
    }

    // Hook this to the shutter button's OnClick.
    public void CapturePhoto()
    {
        if (captureCamera == null)
        {
            Debug.LogError("[IpadCamera] Cannot capture: no camera.");
            return;
        }

        SetupCamera();

        // URP keeps _previewTexture updated every frame, so we just copy out
        // whatever is currently shown in the viewfinder.
        Texture2D photo = new Texture2D(photoWidth, photoHeight, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = _previewTexture;
        photo.ReadPixels(new Rect(0, 0, photoWidth, photoHeight), 0, 0);
        photo.Apply();
        RenderTexture.active = previousActive;

        List<string> captured = DetectAndMarkEvidence();

        if (PhotoLibrary.Instance != null)
        {
            PhotoLibrary.Instance.AddPhoto(new EvidencePhoto(photo, captured));
            if (verboseLogging)
                Debug.Log($"[IpadCamera] Photo captured ({photoWidth}x{photoHeight}), " +
                          $"evidence in frame: {captured.Count}.");
        }
        else
        {
            Debug.LogWarning("[IpadCamera] No PhotoLibrary in scene - photo discarded.");
        }

        PlayShutterFlash();
    }

    // Finds every key item currently inside the camera frustum (and, optionally,
    // not occluded), records it in the grading backend and returns the ids that
    // were visible in this shot.
    private List<string> DetectAndMarkEvidence()
    {
        var capturedIds = new List<string>();
        EvidenceGradingManager grading = EvidenceGradingManager.Instance;
        if (grading == null)
            return capturedIds;

        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(captureCamera);
        Vector3 camPos = captureCamera.transform.position;

        // Line of sight is checked from the player's eyes so the tablet can't be
        // pushed through a wall to photograph evidence on the other side.
        Vector3 eyePos = playerHead != null ? playerHead.position : camPos;

        foreach (KeyEvidenceItem item in grading.RegisteredItems)
        {
            if (item == null || !item.isActiveAndEnabled)
                continue;

            if (!item.TryGetBounds(out Bounds bounds))
                continue;

            if (!GeometryUtility.TestPlanesAABB(frustum, bounds))
                continue;

            float distance = (bounds.center - camPos).magnitude;
            if (distance > maxDetectionDistance)
                continue;

            if (requireLineOfSight && IsOccluded(item, eyePos, bounds.center))
                continue;

            grading.MarkFound(item.evidenceId);
            capturedIds.Add(item.evidenceId);
        }

        return capturedIds;
    }

    private bool IsOccluded(KeyEvidenceItem item, Vector3 origin, Vector3 targetPoint)
    {
        Vector3 dir = targetPoint - origin;
        float dist = dir.magnitude;
        if (dist <= 0.001f)
            return false;

        if (!Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist + 0.1f, occlusionMask, QueryTriggerInteraction.Ignore))
            return false; // nothing in the way

        // If we hit the item itself (or one of its children), it is visible.
        return !hit.transform.IsChildOf(item.transform);
    }

    private void PlayShutterFlash()
    {
        if (shutterFlash == null)
            return;

        if (_flashGroup == null)
            _flashGroup = shutterFlash.GetComponent<CanvasGroup>();

        shutterFlash.SetActive(true);
        if (_flashGroup != null)
            _flashGroup.alpha = 1f;

        _flashTimer = shutterFlashSeconds;
    }
}
