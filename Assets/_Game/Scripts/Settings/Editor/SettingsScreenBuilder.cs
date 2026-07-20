// Builds the iPad Settings screen UI directly inside Tablet_manager.prefab.
//
// Run it from  Tools > Crime Scene > Build iPad Settings Screen.
// It is safe to re-run: the generated "SettingsControls" container is deleted
// and rebuilt from scratch each time.
//
// What it does, all inside the prefab asset (no scene objects involved):
//   1. Finds the SettingsScreen object via ScreenManager.settingsScreen.
//   2. Creates every control (movement toggle, speed buttons, day/night buttons,
//      volume sliders + % labels, close button) as children of it.
//   3. Adds iPadSettingsPanel to SettingsScreen and wires all its references.
//   4. Wires CloseButton.onClick -> ScreenManager.ShowHome().
//   5. Adds SettingsManager + SettingsApplier to the prefab root (next to
//      PhotoLibrary / EvidenceGradingManager / TabletPersist).
//
// The UI is authored in a comfortable 800x1150 "design pixel" space inside a
// container that is then uniformly scaled down to fit the tablet's world-space
// canvas, so fonts, sliders and buttons keep sane proportions regardless of the
// canvas's actual (tiny, world-unit) dimensions.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class SettingsScreenBuilder
{
    private const string PrefabPath = "Assets/BNG Framework/Prefabs/iPad/Tablet_manager.prefab";
    private const string ContainerName = "SettingsControls";

    // Design-space size the UI is authored in before being scaled to the canvas.
    private const float DesignWidth = 800f;
    private const float DesignHeight = 1150f;

    // Palette comes from TabletTheme so all tablet apps share one look.
    private static readonly Color RowLabelColor = TabletTheme.TextPrimary;
    private static readonly Color ButtonColor = TabletTheme.SurfaceRaised;
    private static readonly Color ButtonTextColor = TabletTheme.TextPrimary;
    private static readonly Color SliderBgColor = TabletTheme.SurfaceRaised;
    private static readonly Color SliderFillColor = TabletTheme.Accent;

    [MenuItem("Tools/Crime Scene/Build iPad Settings Screen")]
    public static void Build()
    {
        // Refuse to fight with an open Prefab Mode session on the same asset.
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == PrefabPath)
        {
            EditorUtility.DisplayDialog("Build iPad Settings Screen",
                "Tablet_manager.prefab is open in Prefab Mode. Close Prefab Mode (the '<' arrow) and run this again.", "OK");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BuildInto(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[SettingsScreenBuilder] Settings screen built and saved into Tablet_manager.prefab.");
            EditorUtility.DisplayDialog("Build iPad Settings Screen",
                "Done. Settings screen built inside Tablet_manager.prefab.\n\n" +
                "Open the prefab to inspect it, then Play CSHouse and tap the Settings icon on the tablet.", "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildInto(GameObject root)
    {
        // --- 1. Locate SettingsScreen through ScreenManager (robust to renames).
        ScreenManager screenManager = root.GetComponentInChildren<ScreenManager>(true);
        if (screenManager == null)
            throw new System.Exception("No ScreenManager found in Tablet_manager.prefab.");

        var smSo = new SerializedObject(screenManager);
        var settingsScreenGo = smSo.FindProperty("settingsScreen").objectReferenceValue as GameObject;
        if (settingsScreenGo == null)
            throw new System.Exception("ScreenManager.settingsScreen is not assigned in the prefab.");

        RectTransform screenRt = settingsScreenGo.GetComponent<RectTransform>();

        // --- 2. Fresh container (delete any previous build or manual attempt).
        Transform old = settingsScreenGo.transform.Find(ContainerName);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        RectTransform container = CreateRect(ContainerName, screenRt);
        container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.sizeDelta = new Vector2(DesignWidth, DesignHeight);
        container.anchoredPosition = Vector2.zero;

        // Scale design space down to the actual screen rect.
        Vector2 screenSize = screenRt.rect.size;
        if (screenSize.x <= 0.001f || screenSize.y <= 0.001f)
        {
            Debug.LogWarning("[SettingsScreenBuilder] Could not read SettingsScreen rect; using fallback 3.2x5.2.");
            screenSize = new Vector2(3.2f, 5.2f);
        }
        float scale = Mathf.Min(screenSize.x / DesignWidth, screenSize.y / DesignHeight);
        container.localScale = new Vector3(scale, scale, 1f);

        var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // --- 3. Header, same pattern as the Statements screen: back button on
        // the left, centred title (trailing spacer keeps the title centred).
        RectTransform header = CreateRow(container, "Header", 100f, out _);
        Button closeButton = CreateButton(header, "HomeButton", "< Home", 190f);
        TextMeshProUGUI title = CreateText(header, "Title", "Settings", 54f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        SetLayout(title.gameObject, flexibleWidth: 1f);
        RectTransform headerSpacer = CreateRect("Spacer", header);
        SetLayout(headerSpacer.gameObject, preferredWidth: 190f);

        // --- 4. Movement type row.
        RectTransform moveRow = CreateRow(container, "MovementRow", 84f, out _);
        AddRowLabel(moveRow, "Movement");
        Toggle teleportToggle = CreateToggle(moveRow, "TeleportToggle", "Teleport");

        // --- 5. Speed row. Labels come from SettingsManager.SpeedOptions so the
        // builder and the runtime panel always agree on the numbers.
        RectTransform speedRow = CreateRow(container, "SpeedRow", 84f, out _);
        AddRowLabel(speedRow, "Speed");
        float[] speeds = SettingsManager.SpeedOptions;
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        Button speedSlow = CreateButton(speedRow, "SpeedSlowButton", speeds[0].ToString("0.0", ic) + "x", 0f, flexibleWidth: 1f);
        Button speedNormal = CreateButton(speedRow, "SpeedNormalButton", speeds[1].ToString("0.0", ic) + "x", 0f, flexibleWidth: 1f);
        Button speedFast = CreateButton(speedRow, "SpeedFastButton", speeds[2].ToString("0.0", ic) + "x", 0f, flexibleWidth: 1f);

        // --- 6. Time of day row.
        RectTransform timeRow = CreateRow(container, "TimeOfDayRow", 84f, out _);
        AddRowLabel(timeRow, "Time of Day");
        Button dayButton = CreateButton(timeRow, "DayButton", "Day", 0f, flexibleWidth: 1f);
        Button nightButton = CreateButton(timeRow, "NightButton", "Night", 0f, flexibleWidth: 1f);

        // --- 7. Volume rows (label + slider + % readout).
        Slider bgSlider = CreateVolumeRow(container, "Background", out TextMeshProUGUI bgValue);
        Slider narSlider = CreateVolumeRow(container, "Narration", out TextMeshProUGUI narValue);
        Slider sfxSlider = CreateVolumeRow(container, "SoundFX", out TextMeshProUGUI sfxValue);

        // --- 8. Add iPadSettingsPanel on SettingsScreen and wire every field.
        iPadSettingsPanel panel = settingsScreenGo.GetComponent<iPadSettingsPanel>();
        if (panel == null)
            panel = settingsScreenGo.AddComponent<iPadSettingsPanel>();

        var so = new SerializedObject(panel);
        so.FindProperty("settingsPanel").objectReferenceValue = null;   // ScreenManager owns visibility
        so.FindProperty("settingsButton").objectReferenceValue = null;
        so.FindProperty("teleportToggle").objectReferenceValue = teleportToggle;
        so.FindProperty("speedSlowButton").objectReferenceValue = speedSlow;
        so.FindProperty("speedNormalButton").objectReferenceValue = speedNormal;
        so.FindProperty("speedFastButton").objectReferenceValue = speedFast;
        so.FindProperty("dayButton").objectReferenceValue = dayButton;
        so.FindProperty("nightButton").objectReferenceValue = nightButton;
        so.FindProperty("backgroundSlider").objectReferenceValue = bgSlider;
        so.FindProperty("narrationSlider").objectReferenceValue = narSlider;
        so.FindProperty("soundFXSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("backgroundValueText").objectReferenceValue = bgValue;
        so.FindProperty("narrationValueText").objectReferenceValue = narValue;
        so.FindProperty("soundFXValueText").objectReferenceValue = sfxValue;
        // Runtime button highlighting must use the same theme colours the
        // buttons were built with, or the first click restyles them.
        so.FindProperty("selectedColor").colorValue = TabletTheme.Accent;
        so.FindProperty("defaultColor").colorValue = TabletTheme.SurfaceRaised;
        so.ApplyModifiedPropertiesWithoutUndo();

        // --- 9. Close button returns to the Home app.
        UnityEventTools.AddPersistentListener(closeButton.onClick, new UnityAction(screenManager.ShowHome));

        // --- 10. Managers on the prefab root (same spot as PhotoLibrary etc.).
        if (root.GetComponent<SettingsManager>() == null)
            root.AddComponent<SettingsManager>();
        if (root.GetComponent<SettingsApplier>() == null)
            root.AddComponent<SettingsApplier>();
    }

    // ---------------------------------------------------------------- helpers

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer; // stay on the tablet's UI layer
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    // A horizontal row inside the vertical stack.
    private static RectTransform CreateRow(RectTransform parent, string name, float height, out HorizontalLayoutGroup group)
    {
        RectTransform row = CreateRect(name, parent);
        SetLayout(row.gameObject, preferredHeight: height);
        group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 18f;
        group.childAlignment = TextAnchor.MiddleLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = true;
        return row;
    }

    private static void AddRowLabel(RectTransform row, string text)
    {
        TextMeshProUGUI label = CreateText(row, text.Replace(" ", "") + "Label", text, 36f, TextAlignmentOptions.Left);
        SetLayout(label.gameObject, preferredWidth: 230f);
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string text, float size, TextAlignmentOptions align)
    {
        RectTransform rt = CreateRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = RowLabelColor;
        tmp.alignment = align;
        return tmp;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, float preferredWidth, float flexibleWidth = 0f)
    {
        RectTransform rt = CreateRect(name, parent);
        SetLayout(rt.gameObject, preferredWidth: preferredWidth, flexibleWidth: flexibleWidth);

        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = ButtonColor;

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateText(rt, "Text", label, 32f, TextAlignmentOptions.Center);
        text.color = ButtonTextColor;
        Stretch(text.rectTransform);
        return button;
    }

    private static Toggle CreateToggle(RectTransform parent, string name, string label)
    {
        RectTransform rt = CreateRect(name, parent);
        SetLayout(rt.gameObject, flexibleWidth: 1f);

        // Checkbox background, fixed square on the left.
        RectTransform box = CreateRect("Background", rt);
        box.anchorMin = new Vector2(0f, 0.5f);
        box.anchorMax = new Vector2(0f, 0.5f);
        box.pivot = new Vector2(0f, 0.5f);
        box.sizeDelta = new Vector2(56f, 56f);
        box.anchoredPosition = Vector2.zero;
        var boxImage = box.gameObject.AddComponent<Image>();
        boxImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        boxImage.type = Image.Type.Sliced;
        boxImage.color = ButtonColor;

        RectTransform check = CreateRect("Checkmark", box);
        Stretch(check, 10f);
        var checkImage = check.gameObject.AddComponent<Image>();
        checkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        checkImage.color = SliderFillColor;

        TextMeshProUGUI text = CreateText(rt, "Label", label, 32f, TextAlignmentOptions.Left);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(72f, 0f); // clear the checkbox

        var toggle = rt.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        return toggle;
    }

    private static Slider CreateVolumeRow(RectTransform container, string channel, out TextMeshProUGUI valueText)
    {
        RectTransform row = CreateRow(container, channel + "Row", 84f, out _);
        AddRowLabel(row, channel == "SoundFX" ? "Sound FX" : channel);

        // Slider (structure mirrors Unity's default slider control).
        RectTransform rt = CreateRect(channel + "Slider", row);
        SetLayout(rt.gameObject, flexibleWidth: 1f, preferredHeight: 48f);

        RectTransform bg = CreateRect("Background", rt);
        bg.anchorMin = new Vector2(0f, 0.3f);
        bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        var bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bgImage.type = Image.Type.Sliced;
        bgImage.color = SliderBgColor;

        RectTransform fillArea = CreateRect("Fill Area", rt);
        fillArea.anchorMin = new Vector2(0f, 0.3f);
        fillArea.anchorMax = new Vector2(1f, 0.7f);
        fillArea.offsetMin = new Vector2(16f, 0f);
        fillArea.offsetMax = new Vector2(-16f, 0f);

        RectTransform fill = CreateRect("Fill", fillArea);
        fill.sizeDelta = new Vector2(16f, 0f);
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.type = Image.Type.Sliced;
        fillImage.color = SliderFillColor;

        RectTransform handleArea = CreateRect("Handle Slide Area", rt);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(18f, 0f);
        handleArea.offsetMax = new Vector2(-18f, 0f);

        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(36f, 0f);
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        handleImage.color = Color.white;

        var slider = rt.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // % readout on the right.
        valueText = CreateText(row, channel + "ValueText", "100%", 30f, TextAlignmentOptions.Right);
        SetLayout(valueText.gameObject, preferredWidth: 110f);

        return slider;
    }

    private static void SetLayout(GameObject go, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null)
            le = go.AddComponent<LayoutElement>();
        if (preferredWidth > 0f) le.preferredWidth = preferredWidth;
        if (preferredHeight > 0f) le.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
    }

    private static void Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }
}
