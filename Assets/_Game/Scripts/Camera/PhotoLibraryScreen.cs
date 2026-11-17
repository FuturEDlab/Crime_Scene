using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The tablet "Photo Library" app (Figma section 25:114): every photo taken with
// the iPad camera this session, from PhotoLibrary.
//
//   Grid view   ("Photos From Home Screen"): 3x3 photos per page, oldest first.
//               Photos containing key evidence get the yellow border and the
//               "Evidence" + magnifier strip. Next / previous page arrows only
//               appear when there is a page to go to.
//   Detail view ("Camera Photo"): tapping a photo shows it large with a
//               DETAILS card (evidence found, time taken, photo number).
//
// The look comes from objects authored by TabletPhotoLibraryBuilder in Figma
// pixel space; this component clones the inactive photo template per photo and
// fills in the detail view. Page Back returns from detail to grid; from the
// grid it is wired to ScreenManager.ShowHome by the builder.
public class PhotoLibraryScreen : MonoBehaviour
{
    [Header("Grid view")]
    [SerializeField] private GameObject gridView;
    [Tooltip("Inactive, fully styled first photo; its position is the grid origin.")]
    [SerializeField] private RectTransform photoTemplate;
    [SerializeField] private Vector2 photoPitch = new Vector2(195f, 201f);
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 3;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button previousPageButton;

    [Header("Detail view")]
    [SerializeField] private GameObject detailView;
    [SerializeField] private RawImage detailPhoto;
    [SerializeField] private Image detailBorder;
    [SerializeField] private Button detailBackButton;
    [SerializeField] private TMP_Text evidenceValue;
    [SerializeField] private TMP_Text timeValue;
    [SerializeField] private TMP_Text numberValue;

    [Header("Colours")]
    [SerializeField] private Color normalBorder = Color.white;
    [SerializeField] private Color evidenceBorder = new Color(0.808f, 0.69f, 0.024f); // #CEB006
    [SerializeField] private Color missingPhoto = new Color(0.769f, 0.769f, 0.769f); // #C4C4C4

    private readonly List<RectTransform> _photos = new();
    private int _page;

    private int PageSize => columns * rows;
    private IReadOnlyList<EvidencePhoto> Library =>
        PhotoLibrary.Instance != null ? PhotoLibrary.Instance.Photos : System.Array.Empty<EvidencePhoto>();

    private void Awake()
    {
        if (nextPageButton != null) nextPageButton.onClick.AddListener(() => ShowPage(_page + 1));
        if (previousPageButton != null) previousPageButton.onClick.AddListener(() => ShowPage(_page - 1));
        if (detailBackButton != null) detailBackButton.onClick.AddListener(ShowGrid);
    }

    private void OnEnable()
    {
        if (PhotoLibrary.Instance != null)
            PhotoLibrary.Instance.OnLibraryChanged += OnLibraryChanged;

        // Open on the page holding the newest photo.
        _page = Mathf.Max(0, (Library.Count - 1) / PageSize);
        ShowGrid();
    }

    private void OnDisable()
    {
        if (PhotoLibrary.Instance != null)
            PhotoLibrary.Instance.OnLibraryChanged -= OnLibraryChanged;
    }

    private void OnLibraryChanged()
    {
        if (gridView != null && gridView.activeSelf)
            ShowPage(_page);
    }

    // ------------------------------------------------------------------ grid

    private void ShowGrid()
    {
        if (detailView != null) detailView.SetActive(false);
        if (gridView != null) gridView.SetActive(true);
        ShowPage(_page);
    }

    private void ShowPage(int page)
    {
        int count = Library.Count;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)PageSize));
        _page = Mathf.Clamp(page, 0, pageCount - 1);

        foreach (RectTransform photo in _photos)
            if (photo != null)
                Destroy(photo.gameObject);
        _photos.Clear();

        if (photoTemplate != null)
        {
            photoTemplate.gameObject.SetActive(false);

            int first = _page * PageSize;
            for (int slot = 0; slot < PageSize && first + slot < count; slot++)
                _photos.Add(CreatePhoto(first + slot, slot));
        }

        if (nextPageButton != null) nextPageButton.gameObject.SetActive(_page < pageCount - 1);
        if (previousPageButton != null) previousPageButton.gameObject.SetActive(_page > 0);
    }

    // Figma names the photos "Picture 1..9" per page.
    private RectTransform CreatePhoto(int index, int slot)
    {
        EvidencePhoto data = Library[index];
        RectTransform photo = Instantiate(photoTemplate, photoTemplate.parent);
        photo.name = $"Picture {slot + 1}";
        photo.anchoredPosition = photoTemplate.anchoredPosition +
            new Vector2(photoPitch.x * (slot % columns), -photoPitch.y * (slot / columns));
        photo.gameObject.SetActive(true);

        var refs = photo.GetComponent<PhotoTileRefs>();
        if (refs != null)
        {
            SetPhoto(refs.photo, data);
            if (refs.border != null)
                refs.border.color = data.ContainsEvidence ? evidenceBorder : normalBorder;
            if (refs.evidenceTag != null)
                refs.evidenceTag.SetActive(data.ContainsEvidence);
        }

        photo.GetComponent<Button>().onClick.AddListener(() => ShowDetail(index));
        return photo;
    }

    // ---------------------------------------------------------------- detail

    private void ShowDetail(int index)
    {
        EvidencePhoto data = index >= 0 && index < Library.Count ? Library[index] : null;
        if (data == null)
            return;

        if (gridView != null) gridView.SetActive(false);
        if (detailView != null) detailView.SetActive(true);

        SetPhoto(detailPhoto, data);
        if (detailBorder != null)
            detailBorder.color = data.ContainsEvidence ? evidenceBorder : normalBorder;

        if (evidenceValue != null) evidenceValue.text = EvidenceNames(data);
        if (timeValue != null) timeValue.text = data.TimeTaken.ToString("h:mm tt");
        if (numberValue != null) numberValue.text = $"{index + 1} of {Library.Count}";
    }

    // Display names of the key items in the shot, or "None".
    private static string EvidenceNames(EvidencePhoto data)
    {
        if (!data.ContainsEvidence)
            return "None";

        EvidenceGradingManager grading = EvidenceGradingManager.Instance;
        IEnumerable<string> names = data.CapturedEvidenceIds.Select(id =>
        {
            KeyEvidenceItem item = grading != null
                ? grading.RegisteredItems.FirstOrDefault(i => i != null && i.evidenceId == id)
                : null;
            return item != null ? item.displayName : id;
        });
        return string.Join(", ", names);
    }

    // Photo cropped to fill its frame; grey placeholder if the texture is gone.
    private void SetPhoto(RawImage target, EvidencePhoto data)
    {
        if (target == null)
            return;

        Texture2D image = data != null ? data.Image : null;
        target.texture = image;
        target.color = image != null ? Color.white : missingPhoto;
        if (image == null)
            return;

        Vector2 box = target.rectTransform.rect.size;
        float texAspect = image.width / (float)image.height;
        float boxAspect = box.y > 0f ? box.x / box.y : 1f;
        target.uvRect = texAspect > boxAspect
            ? new Rect((1f - boxAspect / texAspect) * 0.5f, 0f, boxAspect / texAspect, 1f)
            : new Rect(0f, (1f - texAspect / boxAspect) * 0.5f, 1f, texAspect / boxAspect);
    }
}
