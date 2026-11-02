// Builds the "Ring Footage" (Figma 1:157) and "Audio Recordings" (Figma 1:174)
// apps inside Tablet_manager.prefab, next to the other app screens, and wires
// them into ScreenManager (ShowRingFootage / ShowAudioRecordings).
//
// Run it from  Tools > Crime Scene > Build iPad Ring Footage + Audio Screens.
// Safe to re-run: both screens are deleted and rebuilt each time.
//
// Each screen holds the static Figma layers (title, Page Back) plus one styled,
// inactive tile template ("Footage 1" / "Group 17"); TabletMediaScreen clones
// it per StatementLibrary entry at runtime. Ring Footage also gets an inline
// "Footage Player" (not in Figma, pending a player design).

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static TabletFigmaUI;

public static class TabletMediaScreensBuilder
{
    private const string Title = "Build iPad Ring Footage + Audio Screens";
    private const string ArtFolder = "Assets/_Game/Art/UI/TabletMedia";
    private const string HomeArtFolder = "Assets/_Game/Art/UI/TabletHome";

    private const float TileRadius = 20f;

    [MenuItem("Tools/Crime Scene/Build iPad Ring Footage + Audio Screens")]
    public static void Build()
    {
        var assets = ImportAssets();
        if (EditPrefab(TabletManagerPrefabGuid, Title, root => BuildInto(root, assets)))
            EditorUtility.DisplayDialog(Title,
                "Done. Ring Footage and Audio Recordings are in Tablet_manager.prefab.\n\n" +
                "Re-run 'Build iPad Home Screen' too so the home icons open them.", "OK");
    }

    private class MediaAssets
    {
        public Sprite roundedFill, footageBorder, audioTile;
        public Sprite[] back;
        public TMP_FontAsset medium;
    }

    private static MediaAssets ImportAssets()
    {
        return new MediaAssets
        {
            roundedFill = ImportSlicedPng(HomeArtFolder + "/TileFill.png"),
            footageBorder = ImportSlicedPng(ArtFolder + "/FootageBorder.png"),
            audioTile = ImportSvg(ArtFolder + "/AudioTile.svg", 616),
            back = ImportBackArrow(),
            medium = GetOrCreateFontAsset("Inter-Medium"),
        };
    }

    private static void BuildInto(GameObject root, MediaAssets a)
    {
        ScreenManager screenManager = FindScreenManager(root);
        GameObject template = GetScreen(screenManager, "settingsScreen");
        RectTransform canvasRect = screenManager.GetComponent<RectTransform>();
        float referencePpu = ReferencePpu(canvasRect);

        GameObject ring = BuildRingFootage(template, canvasRect, screenManager, referencePpu, a);
        GameObject audio = BuildAudioRecordings(template, canvasRect, screenManager, a);

        var sm = new SerializedObject(screenManager);
        sm.FindProperty("ringFootageScreen").objectReferenceValue = ring;
        sm.FindProperty("audioRecordingsScreen").objectReferenceValue = audio;
        sm.ApplyModifiedPropertiesWithoutUndo();
    }

    // ----------------------------------------------------------- Ring Footage

    private static GameObject BuildRingFootage(GameObject template, RectTransform canvasRect,
        ScreenManager screenManager, float referencePpu, MediaAssets a)
    {
        GameObject screen = CreateScreen("RingFootageScreen", template);
        RectTransform frame = CreateFrame("Ring Footage", screen.transform, canvasRect);

        // Figma: 3 tiles 181.25x170.67 at x 62 / 293.25 / 524.49, y 301. Rows
        // repeat with the same 50px gap as the columns.
        RectTransform tile = CreateRoundedPhoto("Footage 1", frame, 62f, 301f, 181.248f, 170.6667f,
            a.roundedFill, a.footageBorder, TileRadius, referencePpu, Color.white,
            "Thumbnail", out _, out _);
        AddButton(tile.gameObject, tile.GetComponent<Image>());
        tile.gameObject.SetActive(false);

        CreateTitle(frame, "Case 24-0917", a.medium, 50f, 215f, 160f, 338f, 61f);
        Button back = CreateBackButton(frame, a.back, 85f, 167f);
        UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(screenManager.ShowHome));

        // Not in Figma: inline player in the same style as the tiles. Position
        // and height are set at runtime from the number of rows.
        RectTransform player = CreateRoundedPhoto("Footage Player", frame, 62f, 521.67f, 643.74f, 362f,
            a.roundedFill, a.footageBorder, TileRadius, referencePpu, Color.white,
            "Video", out RawImage video, out _);
        AddButton(player.gameObject, player.GetComponent<Image>());
        player.gameObject.SetActive(false);

        var media = new SerializedObject(screen.AddComponent<TabletMediaScreen>());
        media.FindProperty("kind").enumValueIndex = (int)TabletMediaScreen.MediaKind.Video;
        media.FindProperty("tileTemplate").objectReferenceValue = tile;
        media.FindProperty("tilePitch").vector2Value = new Vector2(231.246f, 220.6667f);
        media.FindProperty("tileNameFormat").stringValue = "Footage {0}";
        media.FindProperty("nameOffset").intValue = 0;
        media.FindProperty("player").objectReferenceValue = player;
        media.FindProperty("playerImage").objectReferenceValue = video;
        media.ApplyModifiedPropertiesWithoutUndo();
        return screen;
    }

    // ------------------------------------------------------- Audio Recordings

    private static GameObject BuildAudioRecordings(GameObject template, RectTransform canvasRect,
        ScreenManager screenManager, MediaAssets a)
    {
        GameObject screen = CreateScreen("AudioRecordingsScreen", template);
        RectTransform frame = CreateFrame("Audio Recordings", screen.transform, canvasRect);

        CreateTitle(frame, "Audio Recordings", a.medium, 45f, 194f, 160f, 379f, 54f);
        Button back = CreateBackButton(frame, a.back, 85f, 167f);
        UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(screenManager.ShowHome));

        // Figma: "Group 17/18/19", 153.93x154.16 at x 113 / 306.93 / 500.85,
        // y 258 (tile, white border and waveform are one SVG). Rows repeat
        // with the same 40px gap as the columns.
        RectTransform tile = Place("Group 17", frame, 113f, 258f, 153.9253f, 154.1561f);
        Image image = tile.gameObject.AddComponent<Image>();
        image.sprite = a.audioTile;
        AddButton(tile.gameObject, image);
        tile.gameObject.SetActive(false);

        var media = new SerializedObject(screen.AddComponent<TabletMediaScreen>());
        media.FindProperty("kind").enumValueIndex = (int)TabletMediaScreen.MediaKind.Audio;
        media.FindProperty("tileTemplate").objectReferenceValue = tile;
        media.FindProperty("tilePitch").vector2Value = new Vector2(193.926f, 194.1561f);
        media.FindProperty("tileNameFormat").stringValue = "Group {0}";
        media.FindProperty("nameOffset").intValue = 16;
        media.ApplyModifiedPropertiesWithoutUndo();
        return screen;
    }
}
