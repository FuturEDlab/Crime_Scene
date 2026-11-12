// Builds the iPad Home screen from the Figma "Home Page" frame (node 24:2,
// file "Crime Scene Figma Board") directly inside Tablet.prefab. Tablet_manager
// is a variant of Tablet.prefab, so the crime-scene tablet picks it up too.
//
// Run it from  Tools > Crime Scene > Build iPad Home Screen.  Safe to re-run:
// the generated objects are deleted and rebuilt from scratch each time.
//
// What it does:
//   1. Sets import settings for the Figma assets in Assets/_Game/Art/UI/TabletHome
//      (SVGs -> textured sprites, rounded tile PNGs -> 9-sliced sprites) and
//      creates the Inter TMP font assets next to the TTFs if missing.
//   2. Status bar: replaces TopBar/Time with a "Status Bar" (live clock via
//      TopBarClock, battery and wifi icons) plus a small Settings button that
//      is not in the Figma frame, built inactive (hidden until the pause menu
//      decides where Settings lives). The bezel HomeButton is kept.
//   3. Home screen: clears HomeScreen and rebuilds it as the case title plus
//      four app icons (TabletAppIcon) with labels, wired to ScreenManager:
//        Camera -> ShowCamera, Photos -> ShowEvidence (Photo Library),
//        Ring Footage -> ShowRingFootage, Audio Recordings -> ShowAudioRecordings.
//
// Object names follow the Figma layer names (e.g. "Camera" group holding
// "Rectangle 1" and the "Camera" glyph). Layout is in Figma pixels, see
// TabletFigmaUI.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static TabletFigmaUI;

public static class TabletHomeScreenBuilder
{
    private const string Title = "Build iPad Home Screen";
    private const string ArtFolder = "Assets/_Game/Art/UI/TabletHome";

    private const string StatusBarName = "Status Bar";
    private const string HomeFrameName = "Home Screen";

    private const float TileCornerRadius = 20f;

    // Tile colours from the "iPad App Icons" sheet: default, hover, selected,
    // hover + selected.
    private static readonly Color[] CameraColors = { Hex("AF4646"), Hex("BD7070"), Hex("771F1F"), Hex("771F1F") };
    private static readonly Color[] PhotosColors = { Hex("468546"), Hex("42A842"), Hex("346734"), Hex("346734") };
    private static readonly Color[] RingColors   = { Hex("5588B4"), Hex("619CD0"), Hex("335A7D"), Hex("335A7D") };

    [MenuItem("Tools/Crime Scene/Build iPad Home Screen")]
    public static void Build()
    {
        var assets = ImportAssets();
        if (EditPrefab(TabletPrefabGuid, Title, root => BuildInto(root, assets)))
            EditorUtility.DisplayDialog(Title,
                "Done. The new Home screen is in Tablet.prefab (and so in Tablet_manager).\n\n" +
                "Play CrimeScene_Main and look at the tablet.", "OK");
    }

    // ------------------------------------------------------------------ assets

    private class HomeAssets
    {
        public Sprite tileFill, tileBorder;
        public Sprite battery, wifi, settings;
        public Sprite camera, photos, video;
        public Sprite audioDefault, audioHover, audioSelected, audioHoverSelected;
        public TMP_FontAsset bold, semiBold, medium;
    }

    private static HomeAssets ImportAssets()
    {
        return new HomeAssets
        {
            tileFill = ImportSlicedPng(ArtFolder + "/TileFill.png"),
            tileBorder = ImportSlicedPng(ArtFolder + "/TileBorder.png"),

            battery = ImportSvg(ArtFolder + "/StatusBattery.svg", 160),
            wifi = ImportSvg(ArtFolder + "/StatusWifi.svg", 128),
            settings = ImportSvg(ArtFolder + "/StatusSettings.svg", 112),
            camera = ImportSvg(ArtFolder + "/GlyphCamera.svg", 512),
            photos = ImportSvg(ArtFolder + "/GlyphPhotos.svg", 512),
            video = ImportSvg(ArtFolder + "/GlyphVideo.svg", 512),
            audioDefault = ImportSvg(ArtFolder + "/AudioIcon_Default.svg", 704),
            audioHover = ImportSvg(ArtFolder + "/AudioIcon_Hover.svg", 704),
            audioSelected = ImportSvg(ArtFolder + "/AudioIcon_Selected.svg", 704),
            audioHoverSelected = ImportSvg(ArtFolder + "/AudioIcon_HoverSelected.svg", 704),

            bold = GetOrCreateFontAsset("Inter-Bold"),
            semiBold = GetOrCreateFontAsset("Inter-SemiBold"),
            medium = GetOrCreateFontAsset("Inter-Medium"),
        };
    }

    // ------------------------------------------------------------------ layout

    private static void BuildInto(GameObject root, HomeAssets a)
    {
        ScreenManager screenManager = FindScreenManager(root);
        GameObject homeScreenGo = GetScreen(screenManager, "homeScreen");

        // ScreenManager lives on the tablet's world-space canvas.
        RectTransform canvasRect = screenManager.GetComponent<RectTransform>();
        float referencePpu = ReferencePpu(canvasRect);

        // Figma frame background is pure black.
        Transform background = canvasRect.Find("Background");
        if (background != null && background.TryGetComponent(out Image backgroundImage))
            backgroundImage.color = Color.black;

        BuildStatusBar(canvasRect, screenManager, a);
        BuildHome(homeScreenGo, canvasRect, screenManager, referencePpu, a);
    }

    private static void BuildStatusBar(RectTransform canvasRect, ScreenManager screenManager, HomeAssets a)
    {
        Transform topBar = canvasRect.Find("TopBar");
        if (topBar == null)
            throw new System.Exception("TopBar not found under the tablet canvas.");

        // The old clock is replaced rather than moved, so stale scene overrides
        // on it can't drag the new one out of place.
        DestroyChild(topBar, "Time");

        RectTransform frame = CreateFrame(StatusBarName, topBar, canvasRect);

        // "9:41" placeholder in Figma; driven live by TopBarClock.
        TextMeshProUGUI clock = CreateText("9:41", frame, "2:00 PM", a.bold, 40f, Color.white,
            TextAlignmentOptions.TopLeft, 80f, 39f, 300f, 48f);
        var clockSo = new SerializedObject(clock.gameObject.AddComponent<TopBarClock>());
        clockSo.FindProperty("timeText").objectReferenceValue = clock;
        clockSo.ApplyModifiedPropertiesWithoutUndo();

        // Not in the Figma frame: a gear in the status icons' 2px stroke style,
        // left of Battery with the same gap as Battery -> Wifi. The button is a
        // 56px invisible hit area so it is easy to hit with a VR ray.
        RectTransform settingsRect = Place("Settings", frame, 562f, 38f, 56f, 56f);
        AddHitArea(settingsRect.gameObject);
        Image gear = CreateImage("Icon", settingsRect, a.settings, 14f, 14f, 28f, 28f);
        Button settings = settingsRect.gameObject.AddComponent<Button>();
        settings.targetGraphic = gear;
        settings.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = settings.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.7f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.5f);
        colors.selectedColor = Color.white;
        settings.colors = colors;
        UnityEventTools.AddPersistentListener(settings.onClick, new UnityAction(screenManager.ShowSettings));
        // Hidden for now: Settings will move into a pause menu later. Kept (wired,
        // inactive) so it can come back by ticking the object active.
        settingsRect.gameObject.SetActive(false);

        // SVG rects include the 1px stroke padding around the Figma frames.
        CreateImage("Battery", frame, a.battery, 613.4336f, 52.8994f, 38.5609f, 28.1005f);
        CreateImage("Wifi", frame, a.wifi, 661.3906f, 51.7966f, 31.6092f, 27.3653f);
    }

    private static void BuildHome(GameObject homeScreenGo, RectTransform canvasRect,
        ScreenManager screenManager, float referencePpu, HomeAssets a)
    {
        Transform home = homeScreenGo.transform;
        for (int i = home.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(home.GetChild(i).gameObject);

        if (homeScreenGo.TryGetComponent(out Image homeImage))
            homeImage.color = Color.black;

        RectTransform frame = CreateFrame(HomeFrameName, home, canvasRect);

        // Layers in the same order and with the same names as the Figma frame.
        TextMeshProUGUI title = CreateText("Case 24-0917", frame, "Case 24-0917", a.medium, 50f, Color.white,
            TextAlignmentOptions.Top, 215f, 160f, 338f, 61f);
        title.fontStyle = FontStyles.Underline;

        CreateText("Photo Library", frame, "Photo Library", a.semiBold, 30f, Color.white, TextAlignmentOptions.Top, 467f, 497f, 196f, 36f);
        CreateText("Camera", frame, "Camera", a.semiBold, 30f, Color.white, TextAlignmentOptions.Top, 152f, 497f, 114f, 36f);
        CreateText("Ring Footage", frame, "Ring Footage", a.semiBold, 30f, Color.white, TextAlignmentOptions.Top, 115f, 789f, 190f, 36f);
        CreateText("Audio Recordings", frame, "Audio Recordings", a.semiBold, 30f, Color.white, TextAlignmentOptions.Top, 432f, 789f, 257f, 36f);

        // Tile icons: glyph rects are the SVG bounds relative to the tile.
        Button camera = CreateTileIcon("Camera", "Rectangle 1", "Camera", frame, 121f, 305f, 175.6349f, 175.6349f,
            CameraColors, a.camera, 37.32f, 42.63f, 101.4f, 90.9115f, referencePpu, a);
        Button photos = CreateTileIcon("Photos", "Rectangle 2", "Photos", frame, 468f, 305f, 175.6349f, 175.6349f,
            PhotosColors, a.photos, 45.21f, 50.51f, 85.3797f, 85.3642f, referencePpu, a);
        Button ring = CreateTileIcon("Ring Footage", "Rectangle 3", "Video", frame, 121f, 586f, 175.6349f, 175.8982f,
            RingColors, a.video, 40.36f, 40.30f, 95.2155f, 95.2155f, referencePpu, a);
        Button audio = CreateFlatIcon("Audio Recordings", frame, 468f, 586f, 175.6349f, 175.8982f, a);

        UnityEventTools.AddPersistentListener(camera.onClick, new UnityAction(screenManager.ShowCamera));
        UnityEventTools.AddPersistentListener(photos.onClick, new UnityAction(screenManager.ShowEvidence));
        // These two screens only exist in Tablet_manager (TabletMediaScreensBuilder).
        UnityEventTools.AddPersistentListener(ring.onClick, new UnityAction(screenManager.ShowRingFootage));
        UnityEventTools.AddPersistentListener(audio.onClick, new UnityAction(screenManager.ShowAudioRecordings));
    }

    // Figma group: coloured "Rectangle N" + white glyph, plus a hover border
    // (a stroke on the rectangle in Figma, a separate Image here).
    private static Button CreateTileIcon(string name, string rectangleName, string glyphName, RectTransform frame,
        float x, float y, float w, float h, Color[] colors,
        Sprite glyphSprite, float gx, float gy, float gw, float gh,
        float referencePpu, HomeAssets a)
    {
        RectTransform rect = Place(name, frame, x, y, w, h);

        Image tile = Stretch(rectangleName, rect).gameObject.AddComponent<Image>();
        SetSliced(tile, a.tileFill, TileCornerRadius, referencePpu);
        tile.color = colors[0];

        Image border = Stretch(rectangleName + " Border", rect).gameObject.AddComponent<Image>();
        SetSliced(border, a.tileBorder, TileCornerRadius, referencePpu);
        border.raycastTarget = false;
        border.enabled = false;

        CreateImage(glyphName, rect, glyphSprite, gx, gy, gw, gh);

        Button button = AddButton(rect.gameObject, tile);
        var icon = new SerializedObject(rect.gameObject.AddComponent<TabletAppIcon>());
        icon.FindProperty("tile").objectReferenceValue = tile;
        icon.FindProperty("border").objectReferenceValue = border;
        icon.FindProperty("defaultColor").colorValue = colors[0];
        icon.FindProperty("hoverColor").colorValue = colors[1];
        icon.FindProperty("selectedColor").colorValue = colors[2];
        icon.FindProperty("hoverSelectedColor").colorValue = colors[3];
        icon.FindProperty("borderHover").colorValue = Color.white;
        icon.FindProperty("borderHoverSelected").colorValue = new Color(1f, 1f, 1f, 0.5f);
        icon.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // Icon whose artwork is one flattened image per state (Audio Recordings).
    private static Button CreateFlatIcon(string name, RectTransform frame,
        float x, float y, float w, float h, HomeAssets a)
    {
        RectTransform rect = Place(name, frame, x, y, w, h);
        Image tile = rect.gameObject.AddComponent<Image>();
        tile.sprite = a.audioDefault;

        Button button = AddButton(rect.gameObject, tile);
        var icon = new SerializedObject(rect.gameObject.AddComponent<TabletAppIcon>());
        icon.FindProperty("tile").objectReferenceValue = tile;
        icon.FindProperty("defaultSprite").objectReferenceValue = a.audioDefault;
        icon.FindProperty("hoverSprite").objectReferenceValue = a.audioHover;
        icon.FindProperty("selectedSprite").objectReferenceValue = a.audioSelected;
        icon.FindProperty("hoverSelectedSprite").objectReferenceValue = a.audioHoverSelected;
        icon.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }
}
