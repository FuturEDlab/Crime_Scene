// Builds the iPad Camera screen from the Figma "Camera" section (node 25:2,
// file "Crime Scene Figma Board") inside Tablet_manager.prefab, where the
// camera app lives (IpadCameraRig + its CameraScreen controls).
//
// Run it from  Tools > Crime Scene > Build iPad Camera Screen.  Safe to re-run:
// the generated objects are deleted and rebuilt from scratch each time.
//
// Layout (Figma layer names):
//   Rectangle 5      rounded viewfinder; grey (#C4C4C4) until the live feed
//                    arrives, then the IpadCamera RenderTexture, cropped to fill.
//   Rectangle 7      "Picture Indicator Box": white frame + glow, turns yellow
//                    while key evidence is in shot.
//   Evidence Detected  label shown with the yellow frame.
//   Vector           Page Back       -> ScreenManager.ShowHome
//   Button           Capture Button  -> IpadCamera.CapturePhoto
//   Rectangle 11 + Photos  Photo Library (From Camera) -> ScreenManager.ShowEvidence
//
// The old ViewFinder / Shutter Button / Shutter Flash objects are replaced and
// IpadCamera is rewired to the new viewfinder, flash and evidence indicator.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static TabletFigmaUI;

public static class TabletCameraScreenBuilder
{
    private const string Title = "Build iPad Camera Screen";
    private const string ArtFolder = "Assets/_Game/Art/UI/TabletCamera";
    private const string HomeArtFolder = "Assets/_Game/Art/UI/TabletHome";
    private const string FrameName = "Camera";

    // Viewfinder ("Rectangle 5").
    private const float ViewX = 93f, ViewY = 216f, ViewW = 582f, ViewH = 565f, ViewRadius = 30f;
    private static readonly Color ViewPlaceholder = Hex("C4C4C4");

    // Picture Indicator Box ("Rectangle 7"): border 10, radius 20; the glow PNG
    // has 80px of padding around the box.
    private const float BoxX = 128f, BoxY = 249f, BoxW = 513f, BoxH = 341f, BoxRadius = 20f, GlowPadding = 80f;
    private static readonly Color EvidenceYellow = Hex("EED12D");
    private static readonly Color EvidenceTextColor = Hex("CEB006");

    // Photo Library (From Camera) pill: default, hover, selected, hover + selected.
    private static readonly Color[] PillColors = { Hex("363636"), Hex("575757"), Hex("202020"), Hex("202020") };
    private const float PillRadius = 58.5f; // Figma's 65px clamps to half the 117px height

    [MenuItem("Tools/Crime Scene/Build iPad Camera Screen")]
    public static void Build()
    {
        var assets = ImportAssets();
        if (EditPrefab(TabletManagerPrefabGuid, Title, root => BuildInto(root, assets)))
            EditorUtility.DisplayDialog(Title,
                "Done. The new Camera screen is in Tablet_manager.prefab.\n\n" +
                "Play CrimeScene_Main, open Camera on the tablet and point it at a piece of key evidence.", "OK");
    }

    // ------------------------------------------------------------------ assets

    private class CameraAssets
    {
        public Sprite roundedFill, indicatorBorder, indicatorGlow, pillBorder;
        public Sprite[] back, capture; // default, hover, selected, hover + selected
        public Sprite photos;
        public TMP_FontAsset medium;
        public Material mediumShadow;
    }

    private static CameraAssets ImportAssets()
    {
        return new CameraAssets
        {
            roundedFill = ImportSlicedPng(HomeArtFolder + "/TileFill.png"),
            indicatorBorder = ImportSlicedPng(ArtFolder + "/IndicatorBorder.png"),
            pillBorder = ImportSlicedPng(ArtFolder + "/PillBorder.png"),
            indicatorGlow = ImportPng(ArtFolder + "/IndicatorGlow.png"),

            back = ImportBackArrow(),
            capture = new[]
            {
                ImportSvg(ArtFolder + "/Capture_Default.svg", 416),
                ImportSvg(ArtFolder + "/Capture_Hover.svg", 416),
                ImportSvg(ArtFolder + "/Capture_Selected.svg", 416),
                ImportSvg(ArtFolder + "/Capture_HoverSelected.svg", 416),
            },
            photos = ImportSvg(ArtFolder + "/GlyphPhotosSmall.svg", 304),

            medium = GetOrCreateFontAsset("Inter-Medium"),
            mediumShadow = GetOrCreateShadowMaterial("Inter-Medium"),
        };
    }

    // Drop shadow for "Evidence Detected" (Figma: 0 4px 11.9px rgba(0,0,0,.74))
    // as a TMP underlay preset next to the font asset.
    private static Material GetOrCreateShadowMaterial(string fontName)
    {
        string path = $"{FontFolder}/{fontName} SDF Shadow.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        TMP_FontAsset font = GetOrCreateFontAsset(fontName);
        var material = new Material(font.material) { name = fontName + " SDF Shadow" };
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.74f));
        material.SetFloat("_UnderlayOffsetX", 0f);
        material.SetFloat("_UnderlayOffsetY", -0.8f);
        material.SetFloat("_UnderlayDilate", 0f);
        material.SetFloat("_UnderlaySoftness", 0.8f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // ------------------------------------------------------------------ layout

    private static void BuildInto(GameObject root, CameraAssets a)
    {
        ScreenManager screenManager = FindScreenManager(root);
        GameObject cameraScreen = GetScreen(screenManager, "cameraScreen");
        RectTransform canvasRect = screenManager.GetComponent<RectTransform>();
        float referencePpu = ReferencePpu(canvasRect);

        IpadCamera ipadCamera = root.GetComponentInChildren<IpadCamera>(true);
        if (ipadCamera == null)
            throw new System.Exception("No IpadCamera found in Tablet_manager.prefab.");

        // Replace the previous camera UI (hand-made or from an earlier build).
        Transform screen = cameraScreen.transform;
        DestroyChild(screen, "ViewFinder");
        DestroyChild(screen, "Shutter Button");
        DestroyChild(screen, "Shutter Flash");

        if (cameraScreen.TryGetComponent(out Image screenImage))
            screenImage.color = Color.black;

        RectTransform frame = CreateFrame(FrameName, screen, canvasRect);

        // --- Rectangle 5: rounded viewfinder.
        RectTransform viewRect = Place("Rectangle 5", frame, ViewX, ViewY, ViewW, ViewH);
        Image viewMask = viewRect.gameObject.AddComponent<Image>();
        SetSliced(viewMask, a.roundedFill, ViewRadius, referencePpu);
        viewMask.color = ViewPlaceholder;
        viewMask.raycastTarget = false;
        viewRect.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        RawImage viewfinder = Stretch("ViewFinder", viewRect).gameObject.AddComponent<RawImage>();
        viewfinder.color = ViewPlaceholder; // IpadCamera turns it white once the feed is live
        viewfinder.raycastTarget = false;
        viewfinder.uvRect = CoverUv(ipadCamera, ViewW / ViewH);

        GameObject flash = Stretch("Shutter Flash", viewRect).gameObject;
        Image flashImage = flash.AddComponent<Image>();
        flashImage.color = Color.white;
        flashImage.raycastTarget = false;
        flash.AddComponent<CanvasGroup>().blocksRaycasts = false;
        flash.SetActive(false);

        // --- Rectangle 7: Picture Indicator Box (glow behind the frame).
        Image glow = CreateImage("Rectangle 7 Glow", frame, a.indicatorGlow,
            BoxX - GlowPadding, BoxY - GlowPadding, BoxW + 2f * GlowPadding, BoxH + 2f * GlowPadding);
        glow.color = Color.white;

        Image box = Place("Rectangle 7", frame, BoxX, BoxY, BoxW, BoxH).gameObject.AddComponent<Image>();
        SetSliced(box, a.indicatorBorder, BoxRadius, referencePpu);
        box.color = Color.white;
        box.raycastTarget = false;

        // --- Page Back ("Vector"). State SVGs carry different glow padding,
        // so each sits at its own offset inside the 80x55 Figma node.
        Button back = CreateBackButton(frame, a.back, 120f, 851f);

        // --- Capture Button ("Button").
        Button capture = CreateStateButton("Button", frame, 331f, 828f, 102f, 101f, new[]
        {
            new StateArt(a.capture[0],  0f,   0f, 102f, 101f),
            new StateArt(a.capture[1],  0f,   0f, 102f, 101f),
            new StateArt(a.capture[2],  0f,   0f, 102f, 101f),
            new StateArt(a.capture[3], -0.5f, 0f, 103f, 101f),
        });

        // --- Photo Library (From Camera): "Rectangle 11" pill + "Photos" glyph.
        Button library = CreatePillButton(frame, referencePpu, a);

        // --- Evidence Detected (hidden until IpadCamera sees key evidence).
        TextMeshProUGUI evidenceText = CreateText("Evidence Detected", frame, "Evidence Detected", a.medium, 50f,
            EvidenceTextColor, TextAlignmentOptions.Top, 158f, 638f, 453f, 61f);
        evidenceText.fontSharedMaterial = a.mediumShadow;
        evidenceText.gameObject.SetActive(false);

        // --- Wiring.
        UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(screenManager.ShowHome));
        UnityEventTools.AddPersistentListener(capture.onClick, new UnityAction(ipadCamera.CapturePhoto));
        UnityEventTools.AddPersistentListener(library.onClick, new UnityAction(screenManager.ShowEvidence));

        var cam = new SerializedObject(ipadCamera);
        cam.FindProperty("viewfinder").objectReferenceValue = viewfinder;
        cam.FindProperty("shutterFlash").objectReferenceValue = flash;
        cam.FindProperty("evidenceFrame").objectReferenceValue = box;
        cam.FindProperty("noEvidenceColor").colorValue = Color.white;
        cam.FindProperty("evidenceColor").colorValue = EvidenceYellow;
        cam.FindProperty("evidenceLabel").objectReferenceValue = evidenceText.gameObject;
        cam.ApplyModifiedPropertiesWithoutUndo();
    }

    // Crops the camera texture so it fills the viewfinder like Figma's "fill".
    private static Rect CoverUv(IpadCamera ipadCamera, float viewAspect)
    {
        var cam = new SerializedObject(ipadCamera);
        float texAspect = (float)cam.FindProperty("photoWidth").intValue /
                          Mathf.Max(1, cam.FindProperty("photoHeight").intValue);

        if (texAspect > viewAspect)
        {
            float w = viewAspect / texAspect;
            return new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }
        float h = texAspect / viewAspect;
        return new Rect(0f, (1f - h) * 0.5f, 1f, h);
    }

    // Photo Library (From Camera): pill recoloured per state, 50% white border
    // only in Selected + Hover, white "Photos" glyph.
    private static Button CreatePillButton(RectTransform frame, float referencePpu, CameraAssets a)
    {
        RectTransform rect = Place("Rectangle 11", frame, 555f, 820f, 120f, 117f);
        Image pill = rect.gameObject.AddComponent<Image>();
        SetSliced(pill, a.roundedFill, PillRadius, referencePpu);
        pill.color = PillColors[0];

        Image border = Stretch("Rectangle 11 Border", rect).gameObject.AddComponent<Image>();
        SetSliced(border, a.pillBorder, PillRadius, referencePpu);
        border.raycastTarget = false;
        border.enabled = false;

        // Figma "Photos" node at (579, 843); the SVG adds ~2px stroke padding.
        CreateImage("Photos", rect, a.photos, 22.0f, 21.0f, 75.8439f, 74.906f);

        Button button = AddButton(rect.gameObject, pill);
        var icon = new SerializedObject(rect.gameObject.AddComponent<TabletAppIcon>());
        icon.FindProperty("tile").objectReferenceValue = pill;
        icon.FindProperty("border").objectReferenceValue = border;
        icon.FindProperty("defaultColor").colorValue = PillColors[0];
        icon.FindProperty("hoverColor").colorValue = PillColors[1];
        icon.FindProperty("selectedColor").colorValue = PillColors[2];
        icon.FindProperty("hoverSelectedColor").colorValue = PillColors[3];
        icon.FindProperty("borderDefault").colorValue = Color.clear;
        icon.FindProperty("borderHover").colorValue = Color.clear;
        icon.FindProperty("borderSelected").colorValue = Color.clear;
        icon.FindProperty("borderHoverSelected").colorValue = new Color(1f, 1f, 1f, 0.5f);
        icon.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }
}
