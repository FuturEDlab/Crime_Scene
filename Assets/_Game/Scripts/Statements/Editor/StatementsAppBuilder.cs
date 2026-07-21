// Installs the Statements app into Tablet_manager.prefab.
//
// Run it from  Tools > Crime Scene > Build iPad Statements App.  Safe to re-run:
// the screen and filler data are rebuilt from scratch each time.
//
// What it does, all inside the prefab asset:
//   1. Creates a "StatementsScreen" sibling of the other app screens (same size
//      as SettingsScreen, dark background) with the StatementsAppUI component —
//      the list itself is generated at runtime from StatementLibrary data.
//   2. Adds StatementLibrary to the prefab root and fills it with FILLER
//      entries pointing at the generated sweep-tone clips in
//      Assets/_Game/Audio/Statements (replace with real recordings later).
//   3. Wires ScreenManager.statementsScreen.
//   4. Adds a "Statements >" link button to the NOTEBOOK screen (Statements is
//      a section inside the notebook, not a separate home-screen app) and
//      removes any home-screen icon left over from earlier builds.

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class StatementsAppBuilder
{
    private const string PrefabPath = "Assets/BNG Framework/Prefabs/iPad/Tablet_manager.prefab";
    private const string ScreenName = "StatementsScreen";
    private const string HomeButtonName = "StatementsButton";
    private const string AudioFolder = "Assets/_Game/Audio/Statements";

    [MenuItem("Tools/Crime Scene/Build iPad Statements App")]
    public static void Build()
    {
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath == PrefabPath)
        {
            EditorUtility.DisplayDialog("Build iPad Statements App",
                "Tablet_manager.prefab is open in Prefab Mode. Close Prefab Mode (the '<' arrow) and run this again.", "OK");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            BuildInto(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[StatementsAppBuilder] Statements app built and saved into Tablet_manager.prefab.");
            EditorUtility.DisplayDialog("Build iPad Statements App",
                "Done. Statements is installed as a section of the Notebook app.\n\n" +
                "Play CSHouse, open the Notebook on the tablet and tap 'Statements >'.", "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildInto(GameObject root)
    {
        ScreenManager screenManager = root.GetComponentInChildren<ScreenManager>(true);
        if (screenManager == null)
            throw new System.Exception("No ScreenManager found in Tablet_manager.prefab.");

        var smSo = new SerializedObject(screenManager);
        var settingsScreenGo = smSo.FindProperty("settingsScreen").objectReferenceValue as GameObject;
        var homeScreenGo = smSo.FindProperty("homeScreen").objectReferenceValue as GameObject;
        var notebookScreenGo = smSo.FindProperty("notebookScreen").objectReferenceValue as GameObject;
        if (settingsScreenGo == null || homeScreenGo == null || notebookScreenGo == null)
            throw new System.Exception("ScreenManager home/settings/notebook screens are not assigned.");

        // --- 1. StatementsScreen, sized exactly like the settings screen.
        RectTransform template = settingsScreenGo.GetComponent<RectTransform>();
        Transform screensParent = template.parent;

        Transform old = screensParent.Find(ScreenName);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var screenGo = new GameObject(ScreenName, typeof(RectTransform));
        screenGo.layer = settingsScreenGo.layer;
        RectTransform screenRect = screenGo.GetComponent<RectTransform>();
        screenRect.SetParent(screensParent, false);
        screenRect.anchorMin = template.anchorMin;
        screenRect.anchorMax = template.anchorMax;
        screenRect.pivot = template.pivot;
        screenRect.anchoredPosition = template.anchoredPosition;
        screenRect.sizeDelta = template.sizeDelta;

        Image bg = screenGo.AddComponent<Image>();
        bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;
        bg.color = TabletTheme.Background;

        screenGo.AddComponent<StatementsAppUI>();
        screenGo.SetActive(false); // ScreenManager decides visibility

        // --- 2. StatementLibrary on the root + filler data. Refresh first so
        // freshly created audio/video files are imported before we load them —
        // a builder run before import silently produced entries with no media.
        AssetDatabase.Refresh();

        StatementLibrary library = root.GetComponent<StatementLibrary>();
        if (library == null)
            library = root.AddComponent<StatementLibrary>();

        List<StatementLibrary.StatementEntry> entries = BuildFillerEntries();
        int missing = entries.FindAll(e => e.clip == null && e.video == null).Count;
        if (missing > 0)
            Debug.LogError($"[StatementsAppBuilder] {missing}/{entries.Count} filler entries have NO media — " +
                           "check the warnings above for the missing asset paths.");
        library.SetStatements(entries);
        EditorUtility.SetDirty(library);
        Debug.Log($"[StatementsAppBuilder] StatementLibrary filled with {entries.Count} entries ({entries.Count - missing} with media).");

        // --- 3. Wire ScreenManager.statementsScreen.
        smSo.FindProperty("statementsScreen").objectReferenceValue = screenGo;
        smSo.ApplyModifiedPropertiesWithoutUndo();

        // --- 4. Statements lives INSIDE the Notebook app: no home-screen icon
        // (any icon from an earlier build is removed), just a link button on the
        // notebook screen.
        Transform oldHomeIcon = FindDeep(homeScreenGo.transform, HomeButtonName);
        if (oldHomeIcon != null)
            Object.DestroyImmediate(oldHomeIcon.gameObject);

        BuildNotebookLink(notebookScreenGo, screenManager);
    }

    private static List<StatementLibrary.StatementEntry> BuildFillerEntries()
    {
        // Deliberately listed OUT of date order — the app must sort ascending.
        var entries = new List<StatementLibrary.StatementEntry>
        {
            AudioEntry("Daniel Reeves — Neighbor",            "2026-03-15 18:45", "statement_reeves"),
            VideoEntry("Security Camera — Front Porch",       "2026-03-15 23:12", "CSFakeFootage1"),
            AudioEntry("Margaret Holloway — Homeowner",       "2026-03-14 09:30", "statement_holloway"),
            AudioEntry("Officer T. Bradley — First Responder","2026-03-16 08:05", "statement_bradley"),
            VideoEntry("Security Camera — Back Yard",         "2026-03-13 22:47", "CSFakeFootage2"),
            AudioEntry("Priya Natarajan — Neighbor",          "2026-03-15 07:20", "statement_natarajan"),
        };
        return entries;
    }

    private static StatementLibrary.StatementEntry AudioEntry(string person, string date, string clipName)
    {
        string path = $"{AudioFolder}/{clipName}.wav";
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
            Debug.LogWarning($"[StatementsAppBuilder] Filler audio clip missing: {path}");

        return new StatementLibrary.StatementEntry
        {
            personName = person,
            dateRecorded = date,
            clip = clip
        };
    }

    private static StatementLibrary.StatementEntry VideoEntry(string person, string date, string clipName)
    {
        string path = $"Assets/_Game/Art/Videos/{clipName}.mov";
        var video = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path);
        if (video == null)
            Debug.LogWarning($"[StatementsAppBuilder] Filler video clip missing: {path}");

        return new StatementLibrary.StatementEntry
        {
            personName = person,
            dateRecorded = date,
            video = video
        };
    }

    // A "Statements >" link button on the Notebook screen — Statements is a
    // section of the notebook, not a separate home-screen app.
    private static void BuildNotebookLink(GameObject notebookScreen, ScreenManager screenManager)
    {
        const string LinkName = "StatementsLink";

        // Re-runnable: remove a previously generated link first.
        Transform existing = FindDeep(notebookScreen.transform, LinkName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var linkGo = new GameObject(LinkName, typeof(RectTransform));
        linkGo.layer = notebookScreen.layer;
        RectTransform linkRect = linkGo.GetComponent<RectTransform>();
        linkRect.SetParent(notebookScreen.transform, false);

        // Centred near the top of the notebook and generously sized, as a
        // fraction of the screen so it works regardless of the canvas's
        // world-unit scale. Nudge in the editor if it overlaps notebook content.
        linkRect.anchorMin = new Vector2(0.22f, 0.85f);
        linkRect.anchorMax = new Vector2(0.78f, 0.96f);
        linkRect.offsetMin = linkRect.offsetMax = Vector2.zero;

        Image background = linkGo.AddComponent<Image>();
        background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        background.type = Image.Type.Sliced;
        background.color = TabletTheme.SurfaceRaised;

        Button linkButton = linkGo.AddComponent<Button>();
        linkButton.targetGraphic = background;
        UnityEventTools.AddPersistentListener(linkButton.onClick, new UnityAction(screenManager.ShowStatements));

        // Auto-sized label (the tablet canvas uses tiny world units, so a fixed
        // font size cannot be trusted here).
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.layer = linkGo.layer;
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(linkGo.transform, false);
        labelRect.anchorMin = new Vector2(0.06f, 0.10f);
        labelRect.anchorMax = new Vector2(0.94f, 0.90f);
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "Recorded Statements";
        label.alignment = TextAlignmentOptions.Center;
        label.color = TabletTheme.TextPrimary;
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 0.01f;
        label.fontSizeMax = 100f;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
