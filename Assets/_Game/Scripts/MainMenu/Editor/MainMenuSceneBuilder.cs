// Builds the Main_Menu scene from the "Crime Scene Figma Board":
//   - Main Menu  (page "Main Menu",  frame "Horizon Component Menu Panel / Computer Screen")
//   - Tutorial   (page "Tutorial",   frames "Start", "1" ... "13")
//   - Controls   (section "Controls", frame 260:886)
// Every panel is a 2000x2000 Figma frame laid out in Figma pixels on one
// world-space canvas (1 Figma px = 1 mm), in front of a locked player.
//
// Tools > Crime Scene > Build Main Menu Scene. Safe to re-run: the scene is
// rebuilt from scratch, then preview PNGs of every panel are written to
// Temp/MainMenuPreviews so they can be compared with Figma.

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TabletFigmaUI;

public static class MainMenuSceneBuilder
{
    private const string Title = "Build Main Menu Scene";
    public const string ScenePath = "Assets/_Game/Scenes/Flow/Main_Menu.unity";
    private const string RigPrefabGuid = "6b815e14ccbb2d7469b339871bdbe1cf"; // XR Rig Rigidbody.prefab

    private const string ArtFolder = "Assets/_Game/Art/UI/MainMenu";
    private const string MaterialFolder = "Assets/_Game/Art/Materials/MainMenu";
    private const string TileFillPath = "Assets/_Game/Art/UI/TabletHome/TileFill.png"; // white 9-slice, 80px corner

    // Panel placement: 2000 px * 0.001 = a 2 m square, centred at eye height in
    // front of the player, who starts at the origin facing +Z.
    private const float PanelSize = 2000f;
    private const float PanelScale = 0.001f;
    private const float PanelDistance = 2.5f;
    private const float PanelCenterHeight = 1.6f;
    private const float IndicatorSize = 0.5f;

    private const string PreviewFolder = "Temp/MainMenuPreviews";

    // ------------------------------------------------------------ shared art

    private class Art
    {
        public Sprite fill, border1, border3, border9, soundMax, returnArrow;
        public Sprite[] pageNav; // Default, Hover, Selected, Hover + Selected
        public TMP_FontAsset semiBold, bold, medium, italic;
        public float ppu;
    }

    private static Art art;

    // ------------------------------------------------------------- the build

    [MenuItem("Tools/Crime Scene/Build Main Menu Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        LoadArt();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildEnvironment();
        BuildPlayer();

        Canvas canvas = CreateCanvas();
        art.ppu = canvas.GetComponent<CanvasScaler>().referencePixelsPerUnit;

        var controller = new GameObject("Main Menu Controller").AddComponent<MainMenuController>();
        GameObject mainMenu = BuildMainMenuPanel(canvas.transform, controller);
        GameObject tutorialPanel = BuildTutorialPanel(canvas.transform, controller, out MainMenuTutorial tutorial);
        GameObject controls = BuildControlsPanel(canvas.transform, controller);

        var so = new SerializedObject(controller);
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
        so.FindProperty("tutorialPanel").objectReferenceValue = tutorialPanel;
        so.FindProperty("controlsPanel").objectReferenceValue = controls;
        so.FindProperty("tutorial").objectReferenceValue = tutorial;
        so.ApplyModifiedPropertiesWithoutUndo();

        tutorialPanel.SetActive(false);
        controls.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        CrimeSceneLightingTools.GenerateEnvironmentLighting(scene);
        AddSceneToBuildSettings();

        string previews = RenderPreviews();
        RenderEnvironmentPreviews();
        Debug.Log($"[{Title}] Saved {ScenePath} (first scene in Build Profiles). Previews: {previews}");
    }

    private static void LoadArt()
    {
        art = new Art
        {
            fill = ImportArt(TileFillPath, SliceBorderPx),
            border1 = ImportArt(ArtFolder + "/Common/Border_1of20.png", SliceBorderPx),
            border3 = ImportArt(ArtFolder + "/Common/Border_3of85.png", SliceBorderPx),
            border9 = ImportArt(ArtFolder + "/Common/Border_9of50.png", SliceBorderPx),
            soundMax = ImportArt(ArtFolder + "/Common/SoundMax.png"),
            returnArrow = ImportSvg(ArtFolder + "/Common/ReturnArrow.svg", 512),
            pageNav = new[]
            {
                ImportSvg(ArtFolder + "/Tutorial/PageNav_Default.svg", 512),
                ImportSvg(ArtFolder + "/Tutorial/PageNav_Hover.svg", 512),
                ImportSvg(ArtFolder + "/Tutorial/PageNav_Selected.svg", 512),
                ImportSvg(ArtFolder + "/Tutorial/PageNav_HoverSelected.svg", 512),
            },
            semiBold = GetOrCreateFontAsset("Inter-SemiBold"),
            bold = GetOrCreateFontAsset("Inter-Bold"),
            medium = GetOrCreateFontAsset("Inter-Medium"),
            // Figma uses Inter Light Italic / Inter Black, which aren't in the
            // project: Inter Italic and bold-styled Inter Bold stand in.
            italic = GetOrCreateFontAsset("Inter-Italic"),
        };
    }

    // Like TabletFigmaUI.ImportPng, but with mipmaps: the panel is seen from a
    // couple of metres away, so the 2x Figma exports are shown minified.
    private static Sprite ImportArt(string path, float border = 0f)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.Exception("Missing texture: " + path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = Vector4.one * border;
        importer.mipmapEnabled = border == 0f;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return LoadSprite(path);
    }

    private static Sprite LoadTutorialArt(string name) => ImportArt($"{ArtFolder}/Tutorial/{name}.png");
    private static Sprite LoadControlsArt(string name) => ImportArt($"{ArtFolder}/Controls/{name}.png");

    // ----------------------------------------------------- scene environment

    // "Soft white void" look (task #172): a near-white gradient sky whose
    // horizon colour is also the fog colour, a light grid floor (1 m cells)
    // that fades into that white with distance, and a soft pool of light
    // around the player. Everything is realtime and unshadowed (cheap on
    // Quest); only the environment (ambient and reflections) is generated, so
    // the scene doesn't depend on whatever lighting the previous scene left.
    private static readonly Color VoidTop = Hex("E2E4E8");
    private static readonly Color VoidHorizon = Hex("F6F7F9");
    private static readonly Color VoidBottom = Hex("F6F7F9");
    private const float FogDensity = 0.07f;
    private const string FloorGridPath = "Assets/_Game/Art/Materials/MainMenu/MainMenu_FloorGrid.png";
    private const float FloorSize = 200f; // metres; its edge is lost in the fog

    private static void BuildEnvironment()
    {
        var sky = GetOrCreateMaterial("MainMenu_VoidSkybox", "CrimeScene/MainMenuVoidSkybox", Color.white);
        sky.SetColor("_TopColor", VoidTop);
        sky.SetColor("_HorizonColor", VoidHorizon);
        sky.SetColor("_BottomColor", VoidBottom);
        RenderSettings.skybox = sky;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // A little below the sky's brightness, so the light pool still reads
        // on the white floor and the floor brightens into the fog far away.
        RenderSettings.ambientSkyColor = Hex("D4D6DA");
        RenderSettings.ambientEquatorColor = Hex("C6C8CC");
        RenderSettings.ambientGroundColor = Hex("A9ABB0");
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 1f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = FogDensity;
        RenderSettings.fogColor = VoidHorizon;

        // Soft pool of light on the floor around the player.
        var spot = new GameObject("Spot Light").AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = Hex("FFF6EA");
        spot.intensity = 9f;
        spot.range = 9f;
        spot.spotAngle = 110f;
        spot.innerSpotAngle = 20f;
        spot.shadows = LightShadows.None;
        spot.transform.SetPositionAndRotation(new Vector3(0f, 4.5f, 0.6f), Quaternion.Euler(90f, 0f, 0f));

        // Dim fill so hands and the floor keep some shape.
        var fill = new GameObject("Directional Light").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = Hex("F2F4FF");
        fill.intensity = 0.2f;
        fill.shadows = LightShadows.None;
        fill.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        // The rigidbody player needs ground. Unity's plane is 10 m at scale 1.
        floor.transform.localScale = new Vector3(FloorSize / 10f, 1f, FloorSize / 10f);
        var floorMaterial = GetOrCreateMaterial("MainMenu_Floor", "Universal Render Pipeline/Lit", Color.white);
        floorMaterial.SetTexture("_BaseMap", ImportFloorGrid());
        floorMaterial.SetTextureScale("_BaseMap", Vector2.one * FloorSize); // one texture tile = 1 m
        floorMaterial.SetFloat("_Smoothness", 0.1f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

        // Placeholder for the star placement indicator (to be swapped later).
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "Placement Indicator (Placeholder)";
        Object.DestroyImmediate(indicator.GetComponent<Collider>());
        indicator.transform.position = new Vector3(0f, 0.005f, 0f);
        indicator.transform.localScale = new Vector3(IndicatorSize, 0.01f, IndicatorSize);
        indicator.GetComponent<MeshRenderer>().sharedMaterial =
            GetOrCreateMaterial("MainMenu_PlacementIndicator", "Universal Render Pipeline/Unlit", Hex("EED12D"));
    }

    // Tiling grid with trilinear + anisotropic filtering, so the lines stay
    // clean (no shimmer) where the floor is seen at a grazing angle.
    private static Texture2D ImportFloorGrid()
    {
        var importer = AssetImporter.GetAtPath(FloorGridPath) as TextureImporter;
        if (importer == null)
            throw new System.Exception("Missing texture: " + FloorGridPath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 8;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(FloorGridPath);
    }

    private static Material GetOrCreateMaterial(string name, string shaderName, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.Exception("Shader not found: " + shaderName);
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Materials", "MainMenu");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildPlayer()
    {
        string rigPath = AssetDatabase.GUIDToAssetPath(RigPrefabGuid);
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
        if (rigPrefab == null)
            throw new System.Exception("XR Rig Rigidbody prefab not found (GUID " + RigPrefabGuid + ").");

        var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        new GameObject("Player Lock").AddComponent<MainMenuPlayerLock>();
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(PanelSize, PanelSize);
        rect.localScale = Vector3.one * PanelScale;
        rect.position = new Vector3(0f, PanelCenterHeight, PanelDistance);

        go.GetComponent<CanvasScaler>().referencePixelsPerUnit = 100f;
        go.AddComponent<BNG.VRCanvas>(); // registers the canvas with the BNG laser pointer
        return canvas;
    }

    // ------------------------------------------------------------ main menu

    private static GameObject BuildMainMenuPanel(Transform canvas, MainMenuController controller)
    {
        RectTransform panel = Stretch("Main Menu", canvas);

        Image background = Stretch("Background", panel).gameObject.AddComponent<Image>();
        SetSliced(background, art.fill, 20f, art.ppu);
        background.color = Hex("0B0B0F");
        background.raycastTarget = false;

        Image outline = Stretch("Outline", panel).gameObject.AddComponent<Image>();
        SetSliced(outline, art.border1, 20f, art.ppu);
        // rgba(133,199,219,0.42) over #0B0B0F, pre-blended: the project renders in
        // Linear colour space, where alpha blends differently from Figma (sRGB).
        outline.color = new Color32(62, 90, 101, 255);
        outline.raycastTarget = false;

        RectTransform header = Place("Menu Header", panel, 160f, 326.5f, 1680f, 107f);
        CreateText("Menu Title", header, "STING Crime Scene", art.semiBold, 88f, Hex("F1F4F7"),
            TextAlignmentOptions.Top, 0f, 0f, 1680f, 107f);

        // Primary / Neutral / Destructive Button states from the component library:
        // fills Default / Hover / Pressed, then the outline for Hover (35% white)
        // and Pressed (10% white), pre-blended over that state's fill (Linear colour space).
        Color[] primary = { Hex("0032A0"), Hex("0F4ABF"), Hex("002470"), Hex("6389D5"), Hex("1A3A7E") };
        Color[] neutral = { Hex("2C2C2C"), Hex("333B47"), Hex("171A1F"), Hex("7A8088"), Hex("2E3135") };
        Color[] destructive = { Hex("AA0A1E"), Hex("C71A30"), Hex("7A0514"), Hex("DB6A78"), Hex("871E2C") };

        Wire(CreateMenuButton(panel, "Start", art.semiBold, 56f, Hex("F1F4F7"), 505.5f, 200f, primary), controller.StartGame);
        Wire(CreateMenuButton(panel, "Tutorial", art.bold, 48f, Color.white, 777.5f, 170f, neutral), controller.ShowTutorial);
        Wire(CreateMenuButton(panel, "Controls", art.semiBold, 48f, Color.white, 1019.5f, 170f, neutral), controller.ShowControls);
        Wire(CreateMenuButton(panel, "Settings", art.semiBold, 48f, Color.white, 1261.5f, 170f, neutral), controller.ShowSettings);
        Wire(CreateMenuButton(panel, "Exit", art.semiBold, 48f, Color.white, 1503.5f, 170f, destructive), controller.ExitGame);

        return panel.gameObject;
    }

    private static Button CreateMenuButton(Transform parent, string label, TMP_FontAsset font, float fontSize,
        Color textColor, float y, float height, Color[] fills)
    {
        RectTransform rect = Place(label, parent, 160f, y, 1680f, height);
        PivotToCenter(rect); // so the hover/press scale grows from the middle
        float radius = height / 2f;

        Image fill = Stretch("Fill", rect).gameObject.AddComponent<Image>();
        SetSliced(fill, art.fill, radius, art.ppu);
        fill.color = fills[0]; // also the button's hit area

        Image border = Stretch("Border", rect).gameObject.AddComponent<Image>();
        SetSliced(border, art.border3, radius, art.ppu);
        border.color = Color.clear;
        border.raycastTarget = false;
        border.enabled = false;

        CreateText("Label", rect, label, font, fontSize, textColor, TextAlignmentOptions.Midline, 0f, 0f, 1680f, height);

        Button button = AddButton(rect.gameObject, fill);
        var look = new SerializedObject(rect.gameObject.AddComponent<MainMenuButton>());
        look.FindProperty("fill").objectReferenceValue = fill;
        look.FindProperty("border").objectReferenceValue = border;
        look.FindProperty("defaultColor").colorValue = fills[0];
        look.FindProperty("hoverColor").colorValue = fills[1];
        look.FindProperty("pressedColor").colorValue = fills[2];
        look.FindProperty("borderHover").colorValue = fills[3];
        look.FindProperty("borderPressed").colorValue = fills[4];
        look.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // -------------------------------------------------------------- tutorial

    private struct Deco
    {
        public string file; public float x, y;
        public Deco(string file, float x, float y) { this.file = file; this.x = x; this.y = y; }
    }

    private struct Label
    {
        public string text; public float x, y, size;
        public Label(string text, float x, float y, float size) { this.text = text; this.x = x; this.y = y; this.size = size; }
    }

    private class Page
    {
        public string name, screen, body;
        public float bodySize = 64f;
        public Deco[] art = new Deco[0];      // opaque art first, then keyed (transparent) art on top
        public Label[] inlineLabels = new Label[0];
    }

    // Figma "Tutorial" page, frames in reading order. Text is the Figma copy,
    // spaces included: the blank runs leave room for the inline icons, which
    // are placed at their Figma positions (found by matching the exports to
    // the frame renders).
    private static Page[] TutorialPages() => new[]
    {
        new Page { name = "Start", screen = "Home Screen",
            body = "During your investigation, the main tool you’ll have is the iPad.\n\nEach app has it’s own function to use throughout the investigation." },
        new Page { name = "1", screen = "Home Screen",
            body = "First, we’ll start with the camera app.\n\nThe camera app is accessible through the red app icon.",
            art = new[] { new Deco("Arrow_01", 542, 856) } },
        new Page { name = "2", screen = "Camera",
            body = "When the camera app is open, you’ll have 3 options.\n\n           will take you back to the main screen.\n\n           will snap a photo.\n\n\n           will take you to the photo library.",
            art = new[] { new Deco("Icon_Back", 183, 743), new Deco("Icon_Shutter", 173, 949), new Deco("Icon_PhotoLibrary", 164, 1146) } },
        new Page { name = "3", screen = "Camera2",
            body = "When you have an object in view, an indicator will appear over the object.\n\nIf the object isn’t evidence, the indicator will be <u>white</u>.\n\nIf the object is evidence, the indicator will be <u><color=#EED12D>yellow</color></u> and say Evidence Detected." },
        new Page { name = "4", screen = "Home Screen",
            body = "Next, we’ll cover the photo library app.\n\nThe photo library is accessible through the green app icon or through the camera app.",
            art = new[] { new Deco("Arrow_04", 690, 926) } },
        new Page { name = "5", screen = "Photos From Home Screen",
            body = "All the pictures you’ve taken will be in the photo library. Select any to view.\n\nEvidence will be labeled as such with a yellow outline and a      . \n\nUse the                buttons to flip through the pages.\n\n         will take you to the home page.",
            art = new[] { new Deco("Icon_Magnifier", 319, 961), new Deco("Icon_NextPage", 523, 1115), new Deco("Icon_PrevPage", 413, 1115), new Deco("Icon_Back", 168, 1360) } },
        new Page { name = "6", screen = "Camera Photo 26",
            body = "When you select a picture, it will open a new window and show you the selected picture as well as any relevant details about it.\n\n        will take you back to the photo library.",
            art = new[] { new Deco("Icon_Back", 160, 972) } },
        new Page { name = "7", screen = "Home Screen",
            body = "Next, we’ll cover the doorbell camera footage app.\n\nAll doorbell camera footage is accessible through the blue app icon.",
            art = new[] { new Deco("Arrow_07", 498, 1101), new Deco("Line_07", 497, 1074) } },
        new Page { name = "8", screen = "Ring Footage",
            body = "All the footage from the doorbell camera will be in this app.\n\nSelect any of the videos to watch it and review the details.\n\n        will take you to the home page.",
            art = new[] { new Deco("Icon_Back", 159, 1128) } },
        new Page { name = "9", screen = "Ring Footage Clicked1",
            body = "When viewing a video, you’ll be able to play, pause, rewind, and fast-forward the video at any time using the scrub bar.\n\n       will play and pause the video.\n\n       will take you back to the doorbell footage.",
            art = new[] { new Deco("Icon_Play", 141, 961), new Deco("Icon_Back", 150, 1205) } },
        new Page { name = "10", screen = "Home Screen",
            body = "Next, audio recording app.\n\nAll audio recordings are accessible through the yellow app icon.",
            art = new[] { new Deco("Arrow_10", 498, 1101), new Deco("Line_10", 497, 983) } },
        new Page { name = "11", screen = "Audio Recordings",
            body = "All audio recordings will be in this app.\n\nSelect any of the recordings to listen to and review the details.\n\n       will take you to the home page.",
            art = new[] { new Deco("Icon_Back", 140, 1056) } },
        new Page { name = "12", screen = "Audio 1 Clicked", bodySize = 60f,
            body = "When a reviewing an audio recording, you’ll be able to play and pause the recording at any time using the      .\n\nYou’ll also be able to view relevant details.\n\nYou can view the transcript for the audio recording by using the Transcript      .\n\n       will take you back to the audio recordings.",
            art = new[] { new Deco("Icon_PlayAudio", 752, 716), new Deco("Icon_Back", 150, 1390), new Deco("Icon_Transcript", 740, 1234) } },
        new Page { name = "13", screen = "Audio 1 Clicked", bodySize = 60f,
            body = "When viewing a transcript, the                                     will take you back to the audio recordings page.\n\n                         will take you back to that specific audio recording (the page before this).",
            art = new[] { new Deco("Icon_Back", 148, 872), new Deco("Icon_Back", 266, 585) },
            inlineLabels = new[] { new Label("<u>Audio Recording 1</u>", 376, 578, 45f), new Label("Transcript", 242, 866, 50f) } },
    };

    private static GameObject BuildTutorialPanel(Transform canvas, MainMenuController controller, out MainMenuTutorial tutorial)
    {
        RectTransform panel = Stretch("Tutorial", canvas);
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = Hex("242424");
        background.raycastTarget = false;

        tutorial = panel.gameObject.AddComponent<MainMenuTutorial>();

        Page[] pages = TutorialPages();
        var pageObjects = new GameObject[pages.Length];
        for (int i = 0; i < pages.Length; i++)
        {
            bool first = i == 0, last = i == pages.Length - 1;
            Page page = pages[i];
            RectTransform root = Stretch(page.name, panel);

            CreateImage(page.screen, root, LoadTutorialArt($"iPad_{i:00}"), 1107f, 488f, 768f, 1024f);
            foreach (Deco deco in page.art)
                CreateArt(deco.file, root, LoadTutorialArt(deco.file), deco.x, deco.y);

            CreateImage("sound_max", root, art.soundMax, 141f, 127f, 142f, 142f);
            CreateText("Read Aloud", root, "Read Aloud", art.italic, 64f, Color.white,
                TextAlignmentOptions.TopLeft, 309f, 159f, 400f, 77f);

            // Figma box is 844 wide; the project's Inter TTF measures up to ~1%
            // wider than Figma's, so allow a little slack to keep Figma's line breaks.
            TextMeshProUGUI body = CreateText("Body", root, page.body, art.semiBold, page.bodySize, Color.white,
                TextAlignmentOptions.TopLeft, 141f, 497f, 857f, 1150f);
            body.textWrappingMode = TextWrappingModes.Normal;
            foreach (Label label in page.inlineLabels)
                CreateText(StripTags(label.text), root, label.text, art.medium, label.size, Color.white,
                    TextAlignmentOptions.TopLeft, label.x, label.y, 450f, label.size * 1.25f);

            if (!first)
                Wire(CreateReturnButton(root, 82f, 308f, Color.clear), controller.ShowMainMenu);

            // Page navigation ("Group 23" = Back, "Group 24" = Next / Finish, "Group 20" = Next on Start).
            if (first)
                Wire(CreatePageNavButton(root, "Group 20", "Next", 818f, 364f, 818f, 1063f, false), tutorial.NextPage);
            else
            {
                Wire(CreatePageNavButton(root, "Group 23", "Back", 571f, 371f, 713f, 571f, true), tutorial.PreviousPage);
                if (last)
                    Wire(CreatePageNavButton(root, "Group 24", "Finish", 1013f, 417f, 1013f, 1311f, false), tutorial.Finish);
                else
                    Wire(CreatePageNavButton(root, "Group 24", "Next", 1069f, 361f, 1069f, 1311f, false), tutorial.NextPage);
            }

            root.gameObject.SetActive(first);
            pageObjects[i] = root.gameObject;
        }

        var so = new SerializedObject(tutorial);
        so.FindProperty("menu").objectReferenceValue = controller;
        var list = so.FindProperty("pages");
        list.arraySize = pageObjects.Length;
        for (int i = 0; i < pageObjects.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = pageObjects[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        return panel.gameObject;
    }

    // "Next"/"Back"/"Finish" text with the circled arrow, four states
    // (Figma "Page Navigation": Default, Hover, Selected, Hover + Selected).
    private static Button CreatePageNavButton(Transform parent, string name, string label,
        float groupX, float groupWidth, float textX, float iconX, bool pointsBack)
    {
        RectTransform rect = Place(name, parent, groupX, 1695f, groupWidth, 119f);
        Image hitArea = AddHitArea(rect.gameObject);

        CreateText(label, rect, label, art.semiBold, 96f, Color.white, TextAlignmentOptions.TopLeft,
            textX - groupX, 2f, 300f, 116f);

        RectTransform icon = Place("Vector", rect, iconX - groupX, 0f, 119f, 119f);
        if (pointsBack)
        {
            PivotToCenter(icon);
            icon.localScale = new Vector3(-1f, 1f, 1f);
        }

        string[] stateNames = { "Default", "Hover", "Selected", "Selected + Hover" };
        // The state SVGs lose the Figma layer opacity, so it is applied as an opaque
        // tint sampled from the Figma renders on #242424 (hover #BBBBBB, selected #5F5F5F).
        Color[] tints = { Color.white, Hex("BBBBBB"), Hex("5F5F5F"), Color.white };
        Sprite[] sprites = { art.pageNav[0], art.pageNav[0], art.pageNav[0], art.pageNav[3] };
        var states = new GameObject[4];
        for (int s = 0; s < 4; s++)
        {
            float pad = s == 3 ? 2f : 0f; // Hover + Selected adds a 2px outer stroke
            Image image = CreateImage(stateNames[s], icon, sprites[s], -pad, -pad, 119f + 2f * pad, 119f + 2f * pad);
            image.color = tints[s];
            states[s] = image.gameObject;
            states[s].SetActive(s == 0);
        }

        Button button = AddButton(rect.gameObject, hitArea);
        SetStateObjects(rect.gameObject.AddComponent<TabletAppIcon>(), states);
        return button;
    }

    // ------------------------------------------------------------- controls

    private static GameObject BuildControlsPanel(Transform canvas, MainMenuController controller)
    {
        RectTransform panel = Stretch("Controls", canvas);
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = false;

        // Opaque art first, then the transparent (white) art and arrows on top.
        var decos = new[]
        {
            new Deco("SideController3", 515, 1342),
            new Deco("Left", 709, 546),
            new Deco("Right", 1110, 546),
            new Deco("SideController2", 505, 926),
            new Deco("Teleport", 937, 1296),
            new Deco("Arrow11", 615, 499),
            new Deco("Arrow14", 1246, 499),
            new Deco("Arrow13", 799, 440),
            new Deco("Arrow15", 1148, 440),
            new Deco("Arrow12", 675, 644),
            new Deco("Arrow16", 1204, 644),
            new Deco("Arrow2", 1032, 1145),
        };
        foreach (Deco deco in decos)
            CreateArt(deco.file, panel, LoadControlsArt(deco.file), deco.x, deco.y);

        // Title is Inter Black in Figma; bold-styled Inter Bold stands in.
        TextMeshProUGUI title = CreateText("Controls Title", panel, "Controls", art.bold, 150f, Color.white,
            TextAlignmentOptions.Top, 0f, 128f, 2000f, 182f);
        title.fontStyle = FontStyles.Bold;

        CreateText("Turn", panel, "Turn", art.semiBold, 50f, Color.white, TextAlignmentOptions.TopLeft, 517f, 461f, 200f, 61f);
        CreateText("Move/ Teleport", panel, "Move/\nTeleport", art.semiBold, 50f, Color.white, TextAlignmentOptions.TopLeft, 1410f, 473f, 300f, 122f);
        CreateText("Interact (Left)", panel, "Interact", art.semiBold, 50f, Color.white, TextAlignmentOptions.TopLeft, 745f, 397f, 250f, 61f);
        CreateText("Interact (Right)", panel, "Interact", art.semiBold, 50f, Color.white, TextAlignmentOptions.TopLeft, 1118f, 397f, 250f, 61f);
        CreateText("Fetch iPad (Left)", panel, "Fetch iPad", art.semiBold, 50f, Color.white, TextAlignmentOptions.TopLeft, 439f, 744f, 300f, 61f);
        CreateText("Fetch iPad (Right)", panel, "Fetch iPad", art.semiBold, 50f, Color.white, TextAlignmentOptions.TopLeft, 1360f, 744f, 300f, 61f);
        CreateText("Select", panel, "Select", art.medium, 100f, Color.white, TextAlignmentOptions.TopLeft, 1336f, 1136f, 400f, 121f);

        Wire(CreateReturnButton(panel, 505f, 1788f, Hex("1C1C1E")), controller.ShowMainMenu);
        return panel.gameObject;
    }

    // ------------------------------------------------------- shared widgets

    // Figma "Return" (991x150, radius 50). States: Default (given fill),
    // Hover #3C3C3C + 9px #717171 outline, Selected #191919,
    // Selected + Hover #191919 + outline. Text and icon don't move.
    private static Button CreateReturnButton(Transform parent, float x, float y, Color defaultFill)
    {
        RectTransform rect = Place("Return", parent, x, y, 991f, 150f);

        Image tile = Stretch("Background", rect).gameObject.AddComponent<Image>();
        SetSliced(tile, art.fill, 50f, art.ppu);
        tile.color = defaultFill; // hit area, even when transparent

        Image border = Stretch("Border", rect).gameObject.AddComponent<Image>();
        SetSliced(border, art.border9, 50f, art.ppu);
        border.raycastTarget = false;
        border.enabled = false;

        // Text centre is 64px right of the frame centre.
        CreateText("Return to Main Menu", rect, "Return to Main Menu", art.medium, 75f, Color.white,
            TextAlignmentOptions.Top, 991f / 2f + 64f - 400f, 29f, 800f, 91f);
        // SVG viewBox includes the 0.75px stroke: inset -0.52% / -0.52%.
        CreateImage("Vector", rect, art.returnArrow, 59f - 0.53f, 39f - 0.37f, 102.906f, 73.278f);

        Button button = AddButton(rect.gameObject, tile);
        var icon = new SerializedObject(rect.gameObject.AddComponent<TabletAppIcon>());
        icon.FindProperty("tile").objectReferenceValue = tile;
        icon.FindProperty("border").objectReferenceValue = border;
        icon.FindProperty("defaultColor").colorValue = defaultFill;
        icon.FindProperty("hoverColor").colorValue = Hex("3C3C3C");
        icon.FindProperty("selectedColor").colorValue = Hex("191919");
        icon.FindProperty("hoverSelectedColor").colorValue = Hex("191919");
        Color outline = Hex("717171");
        icon.FindProperty("borderDefault").colorValue = Color.clear;
        icon.FindProperty("borderHover").colorValue = outline;
        icon.FindProperty("borderSelected").colorValue = Color.clear;
        icon.FindProperty("borderHoverSelected").colorValue = outline;
        icon.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // Exported art is 2x the Figma size.
    private static Image CreateArt(string name, Transform parent, Sprite sprite, float x, float y)
    {
        return CreateImage(name, parent, sprite, x, y, sprite.rect.width / 2f, sprite.rect.height / 2f);
    }

    private static void SetStateObjects(TabletAppIcon icon, GameObject[] states)
    {
        var so = new SerializedObject(icon);
        so.FindProperty("defaultObject").objectReferenceValue = states[0];
        so.FindProperty("hoverObject").objectReferenceValue = states[1];
        so.FindProperty("selectedObject").objectReferenceValue = states[2];
        so.FindProperty("hoverSelectedObject").objectReferenceValue = states[3];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PivotToCenter(RectTransform rect)
    {
        Vector2 size = rect.sizeDelta;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition += new Vector2(size.x / 2f, -size.y / 2f);
    }

    private static void Wire(Button button, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    private static string StripTags(string text) => System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", "");

    // --------------------------------------------------------- build profile

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ------------------------------------------------------------- previews

    // Renders each panel (and each tutorial page) of the open Main_Menu scene
    // to Temp/MainMenuPreviews/*.png for a side-by-side check with Figma.
    [MenuItem("Tools/Crime Scene/Render Main Menu Previews")]
    public static string RenderPreviews()
    {
        var controller = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogWarning("[Main Menu Previews] Open the Main_Menu scene first.");
            return null;
        }

        var so = new SerializedObject(controller);
        var panels = new[]
        {
            so.FindProperty("mainMenuPanel").objectReferenceValue as GameObject,
            so.FindProperty("tutorialPanel").objectReferenceValue as GameObject,
            so.FindProperty("controlsPanel").objectReferenceValue as GameObject,
        };
        var tutorial = so.FindProperty("tutorial").objectReferenceValue as MainMenuTutorial;
        var pagesProp = new SerializedObject(tutorial).FindProperty("pages");
        var pages = new GameObject[pagesProp.arraySize];
        for (int i = 0; i < pages.Length; i++)
            pages[i] = pagesProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;

        var wasActive = new bool[panels.Length];
        for (int i = 0; i < panels.Length; i++)
            wasActive[i] = panels[i].activeSelf;
        var pageWasActive = new bool[pages.Length];
        for (int i = 0; i < pages.Length; i++)
            pageWasActive[i] = pages[i].activeSelf;

        Directory.CreateDirectory(PreviewFolder);
        RectTransform canvasRect = panels[0].transform.parent as RectTransform;

        var cameraGo = new GameObject("Preview Camera") { hideFlags = HideFlags.HideAndDontSave };
        var cam = cameraGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = canvasRect.rect.height * canvasRect.lossyScale.y / 2f;
        cam.aspect = 1f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.magenta;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1f;
        cam.transform.SetPositionAndRotation(canvasRect.position - canvasRect.forward * 0.5f, canvasRect.rotation);
        var rt = new RenderTexture(1000, 1000, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        try
        {
            void Shot(int panel, int page, string file)
            {
                for (int i = 0; i < panels.Length; i++)
                    panels[i].SetActive(i == panel);
                for (int i = 0; i < pages.Length; i++)
                    pages[i].SetActive(i == page);
                Canvas.ForceUpdateCanvases();
                foreach (var text in panels[panel].GetComponentsInChildren<TMP_Text>())
                    text.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();

                cam.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(PreviewFolder, file), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            Shot(0, 0, "main_menu.png");
            Shot(2, 0, "controls.png");
            for (int i = 0; i < pages.Length; i++)
                Shot(1, i, $"tutorial_{i:00}.png");
        }
        finally
        {
            for (int i = 0; i < panels.Length; i++)
                panels[i].SetActive(wasActive[i]);
            for (int i = 0; i < pages.Length; i++)
                pages[i].SetActive(pageWasActive[i]);
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cameraGo);
        }

        string folder = Path.GetFullPath(PreviewFolder);
        Debug.Log("[Main Menu Previews] Written to " + folder);
        return folder;
    }

    // Eye-level views of the open Main_Menu scene's environment (sky, fog,
    // floor and lights), to Temp/MainMenuPreviews/environment_*.png.
    [MenuItem("Tools/Crime Scene/Render Main Menu Environment Previews")]
    public static void RenderEnvironmentPreviews()
    {
        Directory.CreateDirectory(PreviewFolder);
        var cameraGo = new GameObject("Environment Preview Camera") { hideFlags = HideFlags.HideAndDontSave };
        var cam = cameraGo.AddComponent<Camera>();
        cameraGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = false;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 80f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 1000f;
        cam.aspect = 4f / 3f;
        var rt = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        var shots = new (string name, Vector3 position, Vector3 euler)[]
        {
            ("environment_front", new Vector3(0f, 1.6f, 0f), new Vector3(0f, 0f, 0f)),
            ("environment_down", new Vector3(0f, 1.6f, 0f), new Vector3(45f, 0f, 0f)),
            ("environment_back", new Vector3(0f, 1.6f, 0f), new Vector3(10f, 180f, 0f)),
            ("environment_side", new Vector3(0f, 1.6f, 0f), new Vector3(0f, 90f, 0f)),
            ("environment_overview", new Vector3(3.5f, 2.6f, -3.5f), new Vector3(15f, -35f, 0f)),
        };

        try
        {
            foreach (var shot in shots)
            {
                cam.transform.SetPositionAndRotation(shot.position, Quaternion.Euler(shot.euler));
                cam.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(PreviewFolder, shot.name + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }
        finally
        {
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cameraGo);
        }
    }
}
