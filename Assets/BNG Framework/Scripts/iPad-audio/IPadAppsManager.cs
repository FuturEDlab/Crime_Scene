using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IPadAppsManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;
    public GameObject cameraPanel;
    public GameObject photosPanel;

    [Header("Home Buttons")]
    public Button cameraIconButton;
    public Button photosIconButton;

    [Header("Camera UI")]
    public RawImage cameraPreviewRawImage;
    public Button captureButton;
    public Button cameraBackButton;

    [Header("Photos UI")]
    public RawImage photoDisplayRawImage;
    public Button leftButton;
    public Button rightButton;
    public Button photosBackButton;
    public GameObject noPhotosTextObject;

    [Header("Capture Source")]
    public Camera iPadCaptureCamera;
    public RenderTexture iPadCameraRT;

    private readonly List<Texture2D> photos = new List<Texture2D>();
    private int currentPhotoIndex = -1;

    void Awake()
    {
        ShowHome();

        // Hook button events (NULL SAFE)
        if (cameraIconButton) cameraIconButton.onClick.AddListener(ShowCamera);
        if (photosIconButton) photosIconButton.onClick.AddListener(ShowPhotos);

        if (captureButton) captureButton.onClick.AddListener(CapturePhoto);
        if (cameraBackButton) cameraBackButton.onClick.AddListener(ShowHome);

        // ✅ FIXED: Left = Prev, Right = Next
        if (leftButton) leftButton.onClick.AddListener(PrevPhoto);
        if (rightButton) rightButton.onClick.AddListener(NextPhoto);

        if (photosBackButton) photosBackButton.onClick.AddListener(ShowHome);

        // Preview wiring
        if (cameraPreviewRawImage != null && iPadCameraRT != null)
            cameraPreviewRawImage.texture = iPadCameraRT;

        RefreshPhotosUI();
    }

    void ShowHome()
    {
        if (homePanel) homePanel.SetActive(true);
        if (cameraPanel) cameraPanel.SetActive(false);
        if (photosPanel) photosPanel.SetActive(false);
    }

    void ShowCamera()
    {
        if (homePanel) homePanel.SetActive(false);
        if (cameraPanel) cameraPanel.SetActive(true);
        if (photosPanel) photosPanel.SetActive(false);

        if (iPadCaptureCamera != null) iPadCaptureCamera.enabled = true;
    }

    void ShowPhotos()
    {
        if (homePanel) homePanel.SetActive(false);
        if (cameraPanel) cameraPanel.SetActive(false);
        if (photosPanel) photosPanel.SetActive(true);

        RefreshPhotosUI();
    }

    void CapturePhoto()
    {
        if (iPadCaptureCamera == null)
        {
            Debug.LogError("iPadCaptureCamera is missing.");
            return;
        }
        if (iPadCameraRT == null)
        {
            Debug.LogError("iPadCameraRT is missing.");
            return;
        }

        // ✅ Force the camera to render into the RT RIGHT NOW
        iPadCaptureCamera.targetTexture = iPadCameraRT;
        iPadCaptureCamera.Render();

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = iPadCameraRT;

        Texture2D tex = new Texture2D(iPadCameraRT.width, iPadCameraRT.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, iPadCameraRT.width, iPadCameraRT.height), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;

        photos.Add(tex);
        currentPhotoIndex = photos.Count - 1;

        // Evidence check (optional)
        var detector = GetComponent<EvidencePhotoDetector>();
        if (detector != null)
            detector.CheckEvidenceHit(iPadCaptureCamera);

        Debug.Log($"Photo captured. Total photos: {photos.Count}");

        // ✅ Refresh UI immediately
        RefreshPhotosUI();

        // Optional: auto-open Photos after capture
        // ShowPhotos();
    }

    void RefreshPhotosUI()
    {
        bool hasPhotos = photos.Count > 0;

        if (noPhotosTextObject != null)
            noPhotosTextObject.SetActive(!hasPhotos);

        if (photoDisplayRawImage != null)
        {
            photoDisplayRawImage.gameObject.SetActive(hasPhotos);

            if (hasPhotos)
            {
                if (currentPhotoIndex < 0) currentPhotoIndex = 0;
                currentPhotoIndex = Mathf.Clamp(currentPhotoIndex, 0, photos.Count - 1);
                photoDisplayRawImage.texture = photos[currentPhotoIndex];
            }
            else
            {
                photoDisplayRawImage.texture = null;
            }
        }

        if (leftButton) leftButton.interactable = hasPhotos && currentPhotoIndex > 0;
        if (rightButton) rightButton.interactable = hasPhotos && currentPhotoIndex < photos.Count - 1;
    }

    void PrevPhoto()
    {
        if (photos.Count == 0) return;
        currentPhotoIndex = Mathf.Max(0, currentPhotoIndex - 1);
        RefreshPhotosUI();
    }

    void NextPhoto()
    {
        if (photos.Count == 0) return;
        currentPhotoIndex = Mathf.Min(photos.Count - 1, currentPhotoIndex + 1);
        RefreshPhotosUI();
    }
}
