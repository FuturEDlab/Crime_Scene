// Builds the "Photo Library" app (Figma section 25:114) on the Evidence screen
// of Tablet_manager.prefab, replacing the old one-photo PhotoGalleryUI.
//
// Run it from  Tools > Crime Scene > Build iPad Photo Library.  Safe to re-run.
//
// Two views, named after their Figma frames, driven by PhotoLibraryScreen:
//   Photos From Home Screen   title, Page Back (-> Home), an inactive styled
//                             "Picture 1" template (cloned 3x3 per page) and
//                             the Page Navigation arrows.
//   Camera Photo              the tapped photo large ("Picture 11"), DETAILS
//                             heading and the "Details" card.
//
// The DETAILS rows are placeholders in Figma ("Text / Sub-Text"); they show
// Evidence / Taken / Photo here.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static TabletFigmaUI;

public static class TabletPhotoLibraryBuilder
{
    private const string Title = "Build iPad Photo Library";
    private const string ArtFolder = "Assets/_Game/Art/UI/TabletPhotos";
    private const string MediaArtFolder = "Assets/_Game/Art/UI/TabletMedia";
    private const string HomeArtFolder = "Assets/_Game/Art/UI/TabletHome";

    private const float Radius = 20f;
    private static readonly Color EvidenceYellow = Hex("CEB006");
    private static readonly Color DetailsHeading = Hex("8F8F94");
    private static readonly Color CardFill = Hex("1C1C1E");
    private static readonly Color CardBorder = Hex("595959");

    [MenuItem("Tools/Crime Scene/Build iPad Photo Library")]
    public static void Build()
    {
        var assets = ImportAssets();
        if (EditPrefab(TabletManagerPrefabGuid, Title, root => BuildInto(root, assets)))
            EditorUtility.DisplayDialog(Title,
                "Done. The Photo Library is on the Evidence screen of Tablet_manager.prefab.\n\n" +
                "Play CrimeScene_Main, take a few photos (some of key evidence) and open Photos.", "OK");
    }

    private class PhotoAssets
    {
        public Sprite roundedFill, border5, magnifier, line;
        public Sprite[] back, next, prev;
        public TMP_FontAsset bold, medium;
    }

    private static PhotoAssets ImportAssets()
    {
        return new PhotoAssets
        {
            roundedFill = ImportSlicedPng(HomeArtFolder + "/TileFill.png"),
            border5 = ImportSlicedPng(MediaArtFolder + "/FootageBorder.png"), // 5px border, radius 20
            magnifier = ImportSvg(ArtFolder + "/MagnifyingGlass.svg", 192),
            line = ImportSvg(ArtFolder + "/DetailsLine.svg", 1024),
            back = ImportBackArrow(),
            next = new[]
            {
                ImportSvg(ArtFolder + "/Next_Default.svg", 240),
                ImportSvg(ArtFolder + "/Next_Hover.svg", 240),
                ImportSvg(ArtFolder + "/Next_Selected.svg", 240),
                ImportSvg(ArtFolder + "/Next_HoverSelected.svg", 240),
            },
            prev = new[]
            {
                ImportSvg(ArtFolder + "/Prev_Default.svg", 240),
                ImportSvg(ArtFolder + "/Prev_Hover.svg", 240),
                ImportSvg(ArtFolder + "/Prev_Selected.svg", 240),
                ImportSvg(ArtFolder + "/Prev_HoverSelected.svg", 240),
            },
            bold = GetOrCreateFontAsset("Inter-Bold"),
            medium = GetOrCreateFontAsset("Inter-Medium"),
        };
    }

    private static void BuildInto(GameObject root, PhotoAssets a)
    {
        ScreenManager screenManager = FindScreenManager(root);
        GameObject screenGo = GetScreen(screenManager, "evidenceScreen");
        RectTransform canvasRect = screenManager.GetComponent<RectTransform>();
        float referencePpu = ReferencePpu(canvasRect);
        Transform screen = screenGo.transform;

        // Replace the old single-photo gallery.
        foreach (PhotoGalleryUI old in screenGo.GetComponents<PhotoGalleryUI>())
            Object.DestroyImmediate(old);
        foreach (string oldChild in new[] { "Content", "EmptyState", "DisplayImage" })
            DestroyChild(screen, oldChild);
        foreach (PhotoLibraryScreen old in screenGo.GetComponents<PhotoLibraryScreen>())
            Object.DestroyImmediate(old);

        if (screenGo.TryGetComponent(out Image screenImage))
            screenImage.color = Color.black;

        RectTransform grid = CreateFrame("Photos From Home Screen", screen, canvasRect);
        RectTransform detail = CreateFrame("Camera Photo", screen, canvasRect);

        // ------------------------------------------------------------ grid
        // Page 1 layout: 165x154 photos at x 106 / 301 / 496, y 278 / 479 / 680.
        RectTransform photo = CreateRoundedPhoto("Picture 1", grid, 106f, 278f, 165f, 154f,
            a.roundedFill, a.border5, Radius, referencePpu, Color.white,
            "Photo", out RawImage photoImage, out Image photoBorder);

        // Evidence strip sits under the border; the tile's rounded mask gives
        // "Rectangle 16" its rounded bottom corners.
        RectTransform tag = Stretch("Evidence Tag", photo);
        tag.SetSiblingIndex(photoBorder.transform.GetSiblingIndex());
        Image strip = Place("Rectangle 16", tag, 0f, 120.135f, 165f, 33.865f).gameObject.AddComponent<Image>();
        strip.color = new Color(0f, 0f, 0f, 0.65f);
        strip.raycastTarget = false;
        CreateText("Evidence", tag, "Evidence", a.medium, 20f, new Color(1f, 1f, 1f, 0.75f),
            TextAlignmentOptions.TopLeft, 25f, 125f, 88f, 24f);
        // Magnifier node 27x27 at (122, 124); the SVG adds its shadow padding.
        CreateImage("Vector", tag, a.magnifier, 112f, 118f, 47f, 47f);

        var refs = photo.gameObject.AddComponent<PhotoTileRefs>();
        refs.photo = photoImage;
        refs.border = photoBorder;
        refs.evidenceTag = tag.gameObject;
        AddButton(photo.gameObject, photo.GetComponent<Image>());
        photo.gameObject.SetActive(false);

        CreateTitle(grid, "Photo Library", a.medium, 45f, 239f, 160f, 290f, 54f);
        Button gridBack = CreateBackButton(grid, a.back, 85f, 167f);
        UnityEventTools.AddPersistentListener(gridBack.onClick, new UnityAction(screenManager.ShowHome));

        // Page Navigation: 56x56; Selected + Hover SVGs carry 2px of padding.
        Button next = CreateStateButton("Vector", grid, 496f, 918f, 56f, 56f, NavStates(a.next));
        Button prev = CreateStateButton("Vector", grid, 215f, 918f, 56f, 56f, NavStates(a.prev));
        next.name = "Next Page";
        prev.name = "Previous Page";

        // ---------------------------------------------------------- detail
        CreateTitle(detail, "Photo Library", a.medium, 45f, 239f, 108f, 290f, 54f);
        Button detailBack = CreateBackButton(detail, a.back, 85f, 115f);

        CreateRoundedPhoto("Picture 11", detail, 121f, 204f, 512f, 364f,
            a.roundedFill, a.border5, Radius, referencePpu, EvidenceYellow,
            "Photo", out RawImage detailPhoto, out Image detailBorder);

        CreateText("DETAILS", detail, "DETAILS", a.bold, 40f, DetailsHeading,
            TextAlignmentOptions.TopLeft, 118f, 606f, 167f, 48f);

        RectTransform card = Place("Details", detail, 118f, 672f, 516f, 278f);
        Image cardFill = card.gameObject.AddComponent<Image>();
        SetSliced(cardFill, a.roundedFill, Radius, referencePpu);
        cardFill.color = CardFill;
        cardFill.raycastTarget = false;
        Image cardBorder = Stretch("Border", card).gameObject.AddComponent<Image>();
        SetSliced(cardBorder, a.border5, Radius, referencePpu);
        cardBorder.color = CardBorder;
        cardBorder.raycastTarget = false;

        TMP_Text evidence = CreateDetailRow(card, a, "Evidence", 39f, 30f, 478f);
        TMP_Text taken = CreateDetailRow(card, a, "Taken", 39f, 112.29f, 478f);
        TMP_Text number = CreateDetailRow(card, a, "Photo", 36.67f, 196.22f, 476f);
        // Line 1 nodes at y 101 / 185 (476 wide); the SVG adds the 3.5px round caps.
        CreateImage("Line 1", card, a.line, 16.5f, 97.5f, 483f, 7f);
        CreateImage("Line 1", card, a.line, 15.5f, 181.5f, 483f, 7f);

        detail.gameObject.SetActive(false);

        // --------------------------------------------------------- wiring
        var library = new SerializedObject(screenGo.AddComponent<PhotoLibraryScreen>());
        library.FindProperty("gridView").objectReferenceValue = grid.gameObject;
        library.FindProperty("photoTemplate").objectReferenceValue = photo;
        library.FindProperty("photoPitch").vector2Value = new Vector2(195f, 201f);
        library.FindProperty("nextPageButton").objectReferenceValue = next;
        library.FindProperty("previousPageButton").objectReferenceValue = prev;
        library.FindProperty("detailView").objectReferenceValue = detail.gameObject;
        library.FindProperty("detailPhoto").objectReferenceValue = detailPhoto;
        library.FindProperty("detailBorder").objectReferenceValue = detailBorder;
        library.FindProperty("detailBackButton").objectReferenceValue = detailBack;
        library.FindProperty("evidenceValue").objectReferenceValue = evidence;
        library.FindProperty("timeValue").objectReferenceValue = taken;
        library.FindProperty("numberValue").objectReferenceValue = number;
        library.FindProperty("evidenceBorder").colorValue = EvidenceYellow;
        library.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StateArt[] NavStates(Sprite[] s)
    {
        return new[]
        {
            new StateArt(s[0],  0f,  0f, 56f, 56f),
            new StateArt(s[1],  0f,  0f, 56f, 55f),
            new StateArt(s[2],  0f,  0f, 56f, 55f),
            new StateArt(s[3], -2f, -2f, 60f, 59f),
        };
    }

    // "Detail Text" row: white label left, 50% white value right-aligned to `valueRight`.
    private static TMP_Text CreateDetailRow(RectTransform card, PhotoAssets a, string label,
        float x, float y, float valueRight)
    {
        RectTransform row = Place("Detail Text", card, 0f, 0f, 516f, 278f);
        CreateText("Text", row, label, a.medium, 40f, Color.white, TextAlignmentOptions.TopLeft, x, y, 200f, 48f);
        // Long values (several evidence names) shrink instead of running into the label.
        TextMeshProUGUI value = CreateText("Sub-Text", row, "", a.medium, 40f, new Color(1f, 1f, 1f, 0.5f),
            TextAlignmentOptions.TopRight, valueRight - 260f, y, 260f, 48f);
        value.enableAutoSizing = true;
        value.fontSizeMin = 18f;
        value.fontSizeMax = 40f;
        value.overflowMode = TextOverflowModes.Ellipsis;
        return value;
    }
}
