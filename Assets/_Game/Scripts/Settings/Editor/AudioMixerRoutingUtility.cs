// Editor-only helper that routes every AudioSource in the open scene(s) to a
// group on the settings AudioMixer. Without a group assignment an AudioSource
// bypasses the mixer entirely, which is why volume sliders can look correctly
// wired and still change nothing.
//
//   Tools > Crime Scene > Audio > List Scene AudioSources     (report only)
//   Tools > Crime Scene > Audio > Route Scene AudioSources     (assigns groups)
//
// Routing is a guess based on names, so it always prints what it did and
// registers an Undo — check the report and fix any source it misfiled by hand.
// It never overwrites a source that already has a group.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

public static class AudioMixerRoutingUtility
{
    // Group names expected on the mixer. These are the GROUP names, which are
    // separate from the exposed PARAMETER names SettingsApplier writes to.
    private const string BackgroundGroup = "Background";
    private const string NarrationGroup = "Narration";
    private const string SoundFXGroup = "SFX";

    private static readonly string[] NarrationHints =
        { "statement", "narration", "narrator", "voice", "dialog", "dialogue", "_vo" };

    private static readonly string[] SoundFXHints =
        { "shutter", "camera", "click", "ui_", "sfx", "footstep", "door", "pickup", "beep" };

    [MenuItem("Tools/Crime Scene/Audio/List Scene AudioSources")]
    private static void ListSources()
    {
        List<AudioSource> sources = FindSceneAudioSources();
        if (sources.Count == 0)
        {
            Debug.Log("[AudioRouting] No AudioSources in the open scene(s).");
            return;
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine($"[AudioRouting] {sources.Count} AudioSource(s) in the open scene(s):");
        foreach (AudioSource source in sources)
        {
            string group = source.outputAudioMixerGroup != null
                ? source.outputAudioMixerGroup.name
                : "<none — bypasses the mixer>";
            string clip = source.clip != null ? source.clip.name : "<no clip>";
            report.AppendLine($"  • {GetPath(source.gameObject)}  clip='{clip}'  group={group}");
        }
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Crime Scene/Audio/Route Scene AudioSources")]
    private static void RouteSources()
    {
        AudioMixer mixer = FindSettingsMixer();
        if (mixer == null)
        {
            EditorUtility.DisplayDialog("No AudioMixer found",
                "Create an AudioMixer first (Assets > Create > Audio Mixer), with groups named " +
                $"'{BackgroundGroup}', '{NarrationGroup}' and '{SoundFXGroup}'.", "OK");
            return;
        }

        AudioMixerGroup background = FindGroup(mixer, BackgroundGroup);
        AudioMixerGroup narration = FindGroup(mixer, NarrationGroup);
        AudioMixerGroup soundFX = FindGroup(mixer, SoundFXGroup);

        if (background == null)
        {
            EditorUtility.DisplayDialog("Missing group",
                $"Mixer '{mixer.name}' has no group named '{BackgroundGroup}'. " +
                "Add the three groups before routing.", "OK");
            return;
        }

        List<AudioSource> sources = FindSceneAudioSources();
        int assigned = 0, skipped = 0;
        var report = new System.Text.StringBuilder();
        report.AppendLine($"[AudioRouting] Routing to mixer '{mixer.name}':");

        foreach (AudioSource source in sources)
        {
            if (source.outputAudioMixerGroup != null)
            {
                skipped++;
                report.AppendLine($"  – kept   {GetPath(source.gameObject)} " +
                                  $"(already on '{source.outputAudioMixerGroup.name}')");
                continue;
            }

            AudioMixerGroup target = Classify(source, background, narration, soundFX);

            Undo.RecordObject(source, "Route AudioSource To Mixer");
            source.outputAudioMixerGroup = target;
            EditorUtility.SetDirty(source);
            assigned++;
            report.AppendLine($"  ✓ {target.name,-11} {GetPath(source.gameObject)}");
        }

        // Persist the change: without marking the scene dirty the assignment is
        // lost the moment the scene is closed without an explicit save.
        foreach (UnityEngine.SceneManagement.Scene scene in GetOpenScenes())
            EditorSceneManager.MarkSceneDirty(scene);

        report.AppendLine($"[AudioRouting] {assigned} assigned, {skipped} already routed. " +
                          $"Save the scene to keep this.");
        Debug.Log(report.ToString());
    }

    // Name-based guess. Ambient beds are by far the most common case here, so
    // anything that doesn't look like speech or a one-shot effect is Background.
    private static AudioMixerGroup Classify(AudioSource source, AudioMixerGroup background,
                                            AudioMixerGroup narration, AudioMixerGroup soundFX)
    {
        string haystack = (source.gameObject.name + " " +
                           (source.clip != null ? source.clip.name : "")).ToLowerInvariant();

        if (narration != null && NarrationHints.Any(haystack.Contains))
            return narration;

        // A looping source is an ambient bed, not a one-shot effect, whatever it
        // is called — that check beats the keyword list.
        if (soundFX != null && !source.loop && SoundFXHints.Any(haystack.Contains))
            return soundFX;

        return background;
    }

    private static AudioMixer FindSettingsMixer()
    {
        // Prefer a mixer inside _Game; fall back to any in the project.
        string[] guids = AssetDatabase.FindAssets("t:AudioMixer");
        AudioMixer fallback = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (mixer == null)
                continue;
            if (path.StartsWith("Assets/_Game/"))
                return mixer;
            fallback ??= mixer;
        }
        return fallback;
    }

    private static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
    {
        // Matches by exact group name, case-insensitively.
        AudioMixerGroup[] groups = mixer.FindMatchingGroups(string.Empty);
        return groups.FirstOrDefault(g =>
            string.Equals(g.name, groupName, System.StringComparison.OrdinalIgnoreCase));
    }

    private static List<AudioSource> FindSceneAudioSources()
    {
        // Include inactive: an ambient source parented under a disabled root is
        // still shipped with the scene and still needs routing.
        return Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include,
                                                     FindObjectsSortMode.None)
            .Where(s => s.gameObject.scene.IsValid())
            .OrderBy(s => GetPath(s.gameObject))
            .ToList();
    }

    private static IEnumerable<UnityEngine.SceneManagement.Scene> GetOpenScenes()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            yield return UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    }

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform current = go.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
