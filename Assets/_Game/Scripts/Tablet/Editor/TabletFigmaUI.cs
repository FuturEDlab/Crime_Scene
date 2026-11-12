// Shared building blocks for the tablet builders that recreate Figma frames
// ("Crime Scene Figma Board") inside the tablet prefabs: asset import, the
// Inter TMP fonts, and layout helpers that work in Figma's own pixel space.
//
// Every screen is laid out inside a 768x1024 container (the Figma iPad frame)
// that is uniformly scaled onto the tablet canvas (30x40 units, same 3:4
// ratio), so coordinates can be copied straight from Figma: (x, y) is the
// top-left corner of a layer and y grows downwards.

using TMPro;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class TabletFigmaUI
{
    public const string TabletPrefabGuid = "956923e3a5accdb479f7b8960b33cb00";        // Tablet.prefab
    public const string TabletManagerPrefabGuid = "fe42837d5516330439e6d6a0bbc56175"; // Tablet_manager.prefab (variant)

    public const string FontFolder = "Assets/_Game/Art/Fonts/Inter";

    public const float FrameWidth = 768f;
    public const float FrameHeight = 1024f;

    // The generated 9-slice PNGs are drawn at 4x with an 80px corner.
    public const float SliceBorderPx = 80f;

    // ---------------------------------------------------------------- prefabs

    // Loads the prefab by GUID, lets `build` edit it, saves. Returns false if
    // the prefab is open in Prefab Mode (editing it underneath would clash).
    public static bool EditPrefab(string guid, string title, System.Action<GameObject> build)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            throw new System.Exception("Prefab not found (GUID " + guid + ").");

        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == path)
        {
            EditorUtility.DisplayDialog(title,
                System.IO.Path.GetFileName(path) + " is open in Prefab Mode. Close Prefab Mode (the '<' arrow) and run this again.", "OK");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            build(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"[{title}] Built and saved into {path}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return true;
    }

    public static ScreenManager FindScreenManager(GameObject root)
    {
        ScreenManager screenManager = root.GetComponentInChildren<ScreenManager>(true);
        if (screenManager == null)
            throw new System.Exception("No ScreenManager found in " + root.name + ".");
        return screenManager;
    }

    public static GameObject GetScreen(ScreenManager screenManager, string field)
    {
        var screen = new SerializedObject(screenManager).FindProperty(field).objectReferenceValue as GameObject;
        if (screen == null)
            throw new System.Exception("ScreenManager." + field + " is not assigned.");
        return screen;
    }

    public static float ReferencePpu(RectTransform canvasRect)
    {
        var scaler = canvasRect.GetComponent<CanvasScaler>();
        return scaler != null ? scaler.referencePixelsPerUnit : 100f;
    }

    // ----------------------------------------------------------------- assets

    // SVG -> textured sprite usable by a plain UI Image. textureSize ~4x the
    // on-screen size in Figma pixels keeps it crisp on the tablet.
    public static Sprite ImportSvg(string path, int textureSize)
    {
        var importer = AssetImporter.GetAtPath(path) as SVGImporter;
        if (importer == null)
            throw new System.Exception("Missing or non-SVG asset: " + path);

        importer.SvgType = SVGType.TexturedSprite;
        importer.KeepTextureAspectRatio = true;
        importer.TextureSize = textureSize;
        importer.SampleCount = 4;
        importer.ViewportOptions = Unity.VectorGraphics.ViewportOptions.PreserveViewport; // keep the stroke padding baked into the viewBox

        // ScriptedImporter setters don't mark the importer dirty, and
        // SaveAndReimport only writes the .meta when it is.
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        return LoadSprite(path);
    }

    // PNG -> sprite; border > 0 makes it 9-sliced.
    public static Sprite ImportPng(string path, float border = 0f)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new System.Exception("Missing texture: " + path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = Vector4.one * border;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return LoadSprite(path);
    }

    public static Sprite ImportSlicedPng(string path) => ImportPng(path, SliceBorderPx);

    public static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        // Some importers store the sprite as a sub-asset only.
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite subSprite)
                return subSprite;

        throw new System.Exception("No sprite imported from " + path);
    }

    // Dynamic SDF font asset saved next to the TTF, same layout as TMP's own
    // "Create > TextMeshPro > Font Asset" (atlas + material as sub-assets).
    public static TMP_FontAsset GetOrCreateFontAsset(string fontName)
    {
        string assetPath = $"{FontFolder}/{fontName} SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{fontName}.ttf");
        if (font == null)
            throw new System.Exception($"Missing font {FontFolder}/{fontName}.ttf");

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
        fontAsset.name = fontName + " SDF";
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        fontAsset.atlasTextures[0].name = fontName + " SDF Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        fontAsset.material.name = fontName + " SDF Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    // ----------------------------------------------------------------- layout

    // A 768x1024 container covering the whole tablet canvas, so children can be
    // placed with Figma coordinates even though the parent screen is smaller.
    // Any previous container with the same name is replaced.
    public static RectTransform CreateFrame(string name, Transform parent, RectTransform canvasRect)
    {
        Transform old = parent.Find(name);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(FrameWidth, FrameHeight);

        float parentScale = parent.lossyScale.x / canvasRect.lossyScale.x;
        rect.localScale = Vector3.one * (canvasRect.rect.width / FrameWidth / parentScale);

        Vector3 canvasCenter = parent.InverseTransformPoint(canvasRect.TransformPoint(canvasRect.rect.center));
        rect.localPosition = new Vector3(canvasCenter.x, canvasCenter.y, 0f);
        return rect;
    }

    // Figma-style placement: (x, y) is the top-left corner, y grows downwards.
    public static RectTransform Place(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    public static RectTransform Stretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static Image CreateImage(string name, Transform parent, Sprite sprite, float x, float y, float w, float h)
    {
        Image image = Place(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    // 9-sliced rounded sprite whose corners render at `cornerRadius` Figma px.
    public static void SetSliced(Image image, Sprite sprite, float cornerRadius, float referencePpu)
    {
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.fillCenter = true;
        image.pixelsPerUnitMultiplier =
            (SliceBorderPx / cornerRadius) / (sprite.pixelsPerUnit / referencePpu);
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, TMP_FontAsset font,
        float size, Color color, TextAlignmentOptions alignment, float x, float y, float w, float h)
    {
        var label = Place(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    // Button whose look is driven by TabletAppIcon, not Unity's transitions.
    public static Button AddButton(GameObject go, Graphic target)
    {
        Button button = go.AddComponent<Button>();
        button.targetGraphic = target;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    // Invisible raycast target so a button made only of state artwork is clickable.
    public static Image AddHitArea(GameObject go)
    {
        Image hit = go.AddComponent<Image>();
        hit.color = Color.clear;
        return hit;
    }

    // Artwork for one state of a CreateStateButton button.
    public struct StateArt
    {
        public Sprite sprite;
        public float x, y, w, h; // relative to the button's Figma node
        public StateArt(Sprite sprite, float x, float y, float w, float h)
        {
            this.sprite = sprite; this.x = x; this.y = y; this.w = w; this.h = h;
        }
    }

    // Button drawn as one image per state (default, hover, selected,
    // selected + hover); state SVGs may differ in size/padding.
    public static Button CreateStateButton(string name, Transform parent,
        float x, float y, float w, float h, StateArt[] states)
    {
        RectTransform rect = Place(name, parent, x, y, w, h);
        Image hitArea = AddHitArea(rect.gameObject);

        string[] stateNames = { "Default", "Hover", "Selected", "Selected + Hover" };
        var objects = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            StateArt s = states[i];
            objects[i] = CreateImage(stateNames[i], rect, s.sprite, s.x, s.y, s.w, s.h).gameObject;
            objects[i].SetActive(i == 0);
        }

        Button button = AddButton(rect.gameObject, hitArea);
        var icon = new SerializedObject(rect.gameObject.AddComponent<TabletAppIcon>());
        icon.FindProperty("defaultObject").objectReferenceValue = objects[0];
        icon.FindProperty("hoverObject").objectReferenceValue = objects[1];
        icon.FindProperty("selectedObject").objectReferenceValue = objects[2];
        icon.FindProperty("hoverSelectedObject").objectReferenceValue = objects[3];
        icon.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // "Page Back" arrow (Figma layer "Vector", 80x55), identical on every screen.
    private const string BackArtFolder = "Assets/_Game/Art/UI/TabletCamera";

    public static Sprite[] ImportBackArrow()
    {
        return new[]
        {
            ImportSvg(BackArtFolder + "/Back_Default.svg", 336),
            ImportSvg(BackArtFolder + "/Back_Hover.svg", 336),
            ImportSvg(BackArtFolder + "/Back_Selected.svg", 336),
            ImportSvg(BackArtFolder + "/Back_HoverSelected.svg", 336),
        };
    }

    public static Button CreateBackButton(Transform parent, Sprite[] back, float x, float y)
    {
        return CreateStateButton("Vector", parent, x, y, 80f, 55f, new[]
        {
            new StateArt(back[0], -0.536f, -0.374f, 80.9134f, 56.2616f),
            new StateArt(back[1], -1.432f, -1.001f, 82.4358f, 58.3642f),
            new StateArt(back[2],  0f,      0f,     80f,      56f),
            new StateArt(back[3], -1.432f, -1.001f, 82.4358f, 58.3642f),
        });
    }

    // A new app screen next to the existing ones, sized like `template`,
    // black, inactive (ScreenManager decides visibility). Replaces any
    // previous screen with the same name.
    public static GameObject CreateScreen(string name, GameObject template)
    {
        RectTransform templateRect = template.GetComponent<RectTransform>();
        Transform parent = templateRect.parent;
        DestroyChild(parent, name);

        var go = new GameObject(name, typeof(RectTransform));
        go.layer = template.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = templateRect.anchorMin;
        rect.anchorMax = templateRect.anchorMax;
        rect.pivot = templateRect.pivot;
        rect.anchoredPosition = templateRect.anchoredPosition;
        rect.sizeDelta = templateRect.sizeDelta;

        go.AddComponent<Image>().color = Color.black;
        go.SetActive(false);
        return go;
    }

    // Underlined Inter Medium screen title ("Photo Library", "Audio Recordings"...).
    public static TextMeshProUGUI CreateTitle(Transform parent, string text, TMP_FontAsset medium,
        float size, float x, float y, float w, float h)
    {
        TextMeshProUGUI title = CreateText(text, parent, text, medium, size, Color.white,
            TextAlignmentOptions.Top, x, y, w, h);
        title.fontStyle = FontStyles.Underline;
        return title;
    }

    // Rounded photo frame: a 9-sliced mask with `contentName` RawImage inside
    // and a border Image on top. Returns the root (mask) rect.
    public static RectTransform CreateRoundedPhoto(string name, Transform parent, float x, float y, float w, float h,
        Sprite fill, Sprite border, float radius, float referencePpu, Color borderColor,
        string contentName, out RawImage content, out Image borderImage)
    {
        RectTransform rect = Place(name, parent, x, y, w, h);
        Image mask = rect.gameObject.AddComponent<Image>();
        SetSliced(mask, fill, radius, referencePpu);
        rect.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        content = Stretch(contentName, rect).gameObject.AddComponent<RawImage>();
        content.raycastTarget = false;

        borderImage = Stretch("Border", rect).gameObject.AddComponent<Image>();
        SetSliced(borderImage, border, radius, referencePpu);
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;
        return rect;
    }

    public static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }

    public static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        color.a = alpha;
        return color;
    }
}
