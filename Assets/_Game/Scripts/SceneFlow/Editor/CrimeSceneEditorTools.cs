// Editor helpers for the additive crime scene (CrimeScene_Main + its additive
// scenes). The scene list comes from the CrimeSceneLoader in CrimeScene_Main,
// so it is defined in exactly one place.
//
// Opening CrimeScene_Main on its own (double-click, File > Open) also opens
// every additive scene, like "Open All For Editing". Turn that off with the
// "Auto-Open All Scenes With Main" toggle (per user, stored in EditorPrefs).
//
//   Tools/Crime Scene/Additive Scenes/Open All For Editing
//   Tools/Crime Scene/Additive Scenes/Open Main Only
//   Tools/Crime Scene/Additive Scenes/Auto-Open All Scenes With Main
//   Tools/Crime Scene/Additive Scenes/Validate

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CrimeSceneEditorTools
{
    public const string MainScenePath = "Assets/_Game/Scenes/Production/CrimeScene/CrimeScene_Main.unity";
    private const string MenuRoot = "Tools/Crime Scene/Additive Scenes/";
    private const string AutoOpenMenu = MenuRoot + "Auto-Open All Scenes With Main";
    private const string AutoOpenPrefKey = "CrimeScene.AutoOpenAllWithMain";

    // Set while a menu deliberately opens Main without its additive scenes.
    private static bool _suppressAutoOpen;

    static CrimeSceneEditorTools()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static bool AutoOpenEnabled
    {
        get => EditorPrefs.GetBool(AutoOpenPrefKey, true);
        set => EditorPrefs.SetBool(AutoOpenPrefKey, value);
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (mode != OpenSceneMode.Single || scene.path != MainScenePath || _suppressAutoOpen || !AutoOpenEnabled)
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Opening more scenes from inside the sceneOpened callback is not safe;
        // do it right after the current open finishes.
        EditorApplication.delayCall += () =>
        {
            Scene main = SceneManager.GetSceneByPath(MainScenePath);
            if (main.isLoaded)
                OpenAdditiveScenes(main);
        };
    }

    [MenuItem(MenuRoot + "Open All For Editing", false, 0)]
    public static void OpenAllForEditing()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene main;
        _suppressAutoOpen = true;
        try { main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single); }
        finally { _suppressAutoOpen = false; }

        OpenAdditiveScenes(main);
    }

    [MenuItem(MenuRoot + "Open Main Only", false, 1)]
    public static void OpenMainOnly()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        _suppressAutoOpen = true;
        try { EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single); }
        finally { _suppressAutoOpen = false; }
    }

    [MenuItem(AutoOpenMenu, false, 2)]
    private static void ToggleAutoOpen() => AutoOpenEnabled = !AutoOpenEnabled;

    [MenuItem(AutoOpenMenu, true)]
    private static bool ToggleAutoOpenValidate()
    {
        Menu.SetChecked(AutoOpenMenu, AutoOpenEnabled);
        return true;
    }

    private static void OpenAdditiveScenes(Scene main)
    {
        foreach (string path in AdditiveScenePaths(main))
        {
            if (!SceneManager.GetSceneByPath(path).isLoaded)
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        // Main owns lighting/RenderSettings; keep it active while editing too.
        SceneManager.SetActiveScene(main);
    }

    [MenuItem(MenuRoot + "Validate", false, 20)]
    public static void ValidateMenu()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        bool ok = Validate(out string report);
        if (ok) Debug.Log(report); else Debug.LogError(report);
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Validate Additive Scenes", ok ? "All checks passed." : "Problems found — see the Console.", "OK");
    }

    // Opens every crime-scene scene and checks build settings, missing scripts
    // and cross-scene references (which Unity cannot serialize).
    public static bool Validate(out string report)
    {
        var sb = new StringBuilder("[CrimeScene] Additive scene validation\n");
        int problems = 0;

        OpenAllForEditing();
        Scene main = SceneManager.GetSceneByPath(MainScenePath);
        List<string> paths = AdditiveScenePaths(main);

        // Build settings: Main first, every additive scene present and enabled.
        EditorBuildSettingsScene[] build = EditorBuildSettings.scenes;
        if (build.Length == 0 || build[0].path != MainScenePath || !build[0].enabled)
        {
            sb.AppendLine($"  BUILD: {MainScenePath} must be the first enabled scene in Build Profiles.");
            problems++;
        }
        foreach (string path in paths)
        {
            if (!build.Any(s => s.path == path && s.enabled))
            {
                sb.AppendLine($"  BUILD: {path} is missing or disabled in Build Profiles.");
                problems++;
            }
        }

        var allScenes = new List<Scene> { main };
        allScenes.AddRange(paths.Select(SceneManager.GetSceneByPath));
        foreach (Scene scene in allScenes)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                sb.AppendLine($"  LOAD: could not open {scene.path}.");
                problems++;
                continue;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (missing > 0)
                    {
                        sb.AppendLine($"  MISSING SCRIPT: {scene.name}/{PathOf(t)} ({missing})");
                        problems++;
                    }

                    foreach (Component component in t.GetComponents<Component>())
                    {
                        if (component == null)
                            continue;
                        problems += CheckCrossSceneReferences(component, scene, sb);
                    }
                }
            }
        }

        sb.AppendLine(problems == 0
            ? $"  OK: {allScenes.Count} scenes, no missing scripts, no cross-scene references."
            : $"  {problems} problem(s).");
        report = sb.ToString();
        return problems == 0;
    }

    private static int CheckCrossSceneReferences(Component component, Scene owner, StringBuilder sb)
    {
        int problems = 0;
        var so = new SerializedObject(component);
        SerializedProperty prop = so.GetIterator();
        while (prop.NextVisible(true))
        {
            if (prop.propertyType != SerializedPropertyType.ObjectReference || prop.objectReferenceValue == null)
                continue;

            GameObject target = prop.objectReferenceValue switch
            {
                GameObject go => go,
                Component c => c.gameObject,
                _ => null,
            };
            if (target == null || EditorUtility.IsPersistent(target) || target.scene == owner)
                continue;

            sb.AppendLine($"  CROSS-SCENE: {owner.name}/{PathOf(component.transform)} {component.GetType().Name}.{prop.propertyPath} -> {target.scene.name}/{PathOf(target.transform)}");
            problems++;
        }
        return problems;
    }

    // Additive scene paths, resolved from the loader's scene names via Build Profiles
    // (falling back to a project search for scenes not yet in the build list).
    public static List<string> AdditiveScenePaths(Scene main)
    {
        var loader = main.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<CrimeSceneLoader>(true))
            .FirstOrDefault();
        var result = new List<string>();
        if (loader == null)
        {
            Debug.LogError("[CrimeScene] No CrimeSceneLoader found in CrimeScene_Main.");
            return result;
        }

        foreach (string sceneName in loader.AdditiveScenes)
        {
            string path = EditorBuildSettings.scenes
                .Select(s => s.path)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == sceneName);
            if (path == null)
            {
                path = AssetDatabase.FindAssets($"t:Scene {sceneName}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == sceneName);
            }

            if (path == null)
                Debug.LogError($"[CrimeScene] Scene \"{sceneName}\" listed in CrimeSceneLoader was not found.");
            else
                result.Add(path);
        }
        return result;
    }

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
