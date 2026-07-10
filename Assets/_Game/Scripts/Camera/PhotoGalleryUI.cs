using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Photo library viewer for the iPad.
//
// Shows one photo at a time (single-item pagination). The player can move
// between photos with the on-screen arrow buttons OR by swiping left/right on
// the photo. When the library is empty it shows the placeholder message.
public class PhotoGalleryUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Display")]
    [Tooltip("RawImage that shows the currently selected photo.")]
    [SerializeField] private RawImage displayImage;

    [Tooltip("Container shown only when there are photos (image + arrows + counter).")]
    [SerializeField] private GameObject contentRoot;

    [Header("Empty State")]
    [Tooltip("Object containing the 'No photos available...' message. Shown when empty.")]
    [SerializeField] private GameObject emptyState;

    [Tooltip("Optional label whose text is set to the empty-state message on start.")]
    [SerializeField] private TMP_Text emptyStateLabel;

    private const string EmptyMessage = "No photos available. Use the camera app to take a photo.";

    [Header("Pagination")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Tooltip("Optional label, e.g. \"2 / 5\".")]
    [SerializeField] private TMP_Text counterText;

    [Tooltip("Optional badge enabled when the shown photo contains key evidence.")]
    [SerializeField] private GameObject evidenceBadge;

    [Tooltip("Colour forced onto the badge text so it reads on ANY photo. The " +
             "prefab's authored colour was black, which disappears on photos " +
             "taken at night.")]
    [SerializeField] private Color evidenceBadgeColor = new Color(1f, 0.72f, 0f); // amber

    [Header("Swipe")]
    [Tooltip("Horizontal drag distance (pixels) required to flip to another photo.")]
    [SerializeField] private float swipeThreshold = 60f;

    private int _currentIndex;
    private Vector2 _dragStart;

    private void Awake()
    {
        if (emptyStateLabel != null)
            emptyStateLabel.text = EmptyMessage;

        if (evidenceBadge != null)
        {
            // Readable on any photo: bold amber instead of the authored black
            // (invisible on night shots), and drawn above its siblings so the
            // photo can never cover it.
            TMP_Text label = evidenceBadge.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = evidenceBadgeColor;
                label.fontStyle = FontStyles.Bold;
            }
            evidenceBadge.transform.SetAsLastSibling();
        }
    }

    private void OnEnable()
    {
        if (previousButton != null) previousButton.onClick.AddListener(ShowPrevious);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNext);

        if (PhotoLibrary.Instance != null)
            PhotoLibrary.Instance.OnLibraryChanged += OnLibraryChanged;

        // Jump to the newest photo whenever the gallery is opened.
        _currentIndex = Mathf.Max(0, PhotoCount - 1);
        Refresh();
    }

    private void OnDisable()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(ShowPrevious);
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNext);

        if (PhotoLibrary.Instance != null)
            PhotoLibrary.Instance.OnLibraryChanged -= OnLibraryChanged;
    }

    private int PhotoCount => PhotoLibrary.Instance != null ? PhotoLibrary.Instance.Count : 0;

    private void OnLibraryChanged()
    {
        // A brand new photo was added => show it.
        _currentIndex = Mathf.Max(0, PhotoCount - 1);
        Refresh();
    }

    public void ShowNext()
    {
        if (PhotoCount == 0) return;
        _currentIndex = (_currentIndex + 1) % PhotoCount;
        Refresh();
    }

    public void ShowPrevious()
    {
        if (PhotoCount == 0) return;
        _currentIndex = (_currentIndex - 1 + PhotoCount) % PhotoCount;
        Refresh();
    }

    private void Refresh()
    {
        int count = PhotoCount;
        bool hasPhotos = count > 0;

        if (emptyState != null) emptyState.SetActive(!hasPhotos);
        if (contentRoot != null) contentRoot.SetActive(hasPhotos);

        if (!hasPhotos)
        {
            if (displayImage != null) displayImage.texture = null;
            if (counterText != null) counterText.text = string.Empty;
            if (evidenceBadge != null) evidenceBadge.SetActive(false);
            SetArrowsInteractable(false);
            return;
        }

        _currentIndex = Mathf.Clamp(_currentIndex, 0, count - 1);
        EvidencePhoto photo = PhotoLibrary.Instance.GetPhoto(_currentIndex);

        if (displayImage != null)
            displayImage.texture = photo != null ? photo.Image : null;

        if (counterText != null)
            counterText.text = $"{_currentIndex + 1} / {count}";

        if (evidenceBadge != null)
            evidenceBadge.SetActive(photo != null && photo.ContainsEvidence);

        // Only one photo => no point flipping.
        SetArrowsInteractable(count > 1);
    }

    private void SetArrowsInteractable(bool interactable)
    {
        if (previousButton != null) previousButton.interactable = interactable;
        if (nextButton != null) nextButton.interactable = interactable;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragStart = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float deltaX = eventData.position.x - _dragStart.x;
        if (Mathf.Abs(deltaX) < swipeThreshold)
            return;

        // Swipe left => next, swipe right => previous (standard gallery feel).
        if (deltaX < 0) ShowNext();
        else ShowPrevious();
    }
}
