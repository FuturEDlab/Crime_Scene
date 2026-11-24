// Lighting helpers for the additive crime scene.
//
//   Tools/Crime Scene/Lighting/Bake Crime Scene Lighting
//       Bakes the house lighting. Re-run after changing the house, the room
//       layout, or CrimeScene_Main's lights / sky / ambient (see below).
//   Tools/Crime Scene/Lighting/Render Lighting Previews
//       Renders the open crime scene from eye height (outside, inside and each
//       room) to Temp/CrimeScenePreviews/*.png.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class CrimeSceneLightingTools
{
    private const string MenuRoot = "Tools/Crime Scene/Lighting/";
    private const string PreviewFolder = "Temp/CrimeScenePreviews";
    private const float EyeHeight = 1.6f;

    // Generates only the environment lighting (ambient probe + default
    // reflection cubemap) for one scene: baked and realtime GI are off, so it
    // takes seconds and writes no lightmaps. Without it, a scene uses Unity's
    // built-in default lighting data, and in a player build the ambient and
    // reflections can be left over from the previously loaded scene.
    // Only `scene` may be open, or every open scene shares the result.
    public static bool GenerateEnvironmentLighting(Scene scene)
    {
        if (SceneManager.sceneCount != 1)
        {
            Debug.LogError($"[Crime Scene Lighting] Only {scene.name} may be open while its environment lighting is generated.");
            return false;
        }

        string folder = Path.GetDirectoryName(scene.path).Replace('\\', '/');
        string settingsPath = $"{folder}/{scene.name}_LightingSettings.lighting";
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
        if (settings == null)
        {
            settings = new LightingSettings { name = scene.name + "_LightingSettings" };
            AssetDatabase.CreateAsset(settings, settingsPath);
        }
        settings.bakedGI = false;
        settings.realtimeGI = false;
        EditorUtility.SetDirty(settings);

        SceneManager.SetActiveScene(scene);
        Lightmapping.lightingSettings = settings;

        if (!Lightmapping.Bake())
        {
            Debug.LogError($"[Crime Scene Lighting] Generating lighting for {scene.name} failed.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Crime Scene Lighting] Generated environment lighting for {scene.name}.");
        return true;
    }

    // ---------------------------------------------------------- baked lighting

    // Room centres from the house floor plan (world metres, house centred on
    // the origin; ground floor at 0, upper floor at 2.48). Each room gets a
    // baked ceiling light, and the previews look around from each centre.
    private static readonly (string name, float x, float z, bool upper, float intensity, float range)[] Rooms =
    {
        ("Entryway", -2.25f, 3.00f, false, 1.5f, 3.5f),
        ("LivingRoom", 1.55f, 2.35f, false, 2.1f, 5.0f),
        ("StairsHall", -2.20f, 1.00f, false, 1.1f, 3.0f),
        ("Kitchen", 1.50f, -2.25f, false, 2.1f, 5.0f),
        ("DownstairsHallway", -2.30f, -1.50f, false, 1.2f, 3.0f),
        ("DownstairsBathroom", -2.30f, -3.75f, false, 1.1f, 2.8f),
        ("UpstairsWalkInCloset", -2.40f, 3.30f, true, 1.1f, 3.0f),
        ("MainBedroom", 1.50f, 2.90f, true, 1.8f, 4.5f),
        ("UpstairsHallway", -0.15f, -0.90f, true, 1.1f, 3.0f),
        ("UpstairsBathroom", 2.25f, 0.00f, true, 1.3f, 3.2f),
        ("Bedroom2", -2.40f, -2.30f, true, 1.8f, 4.5f),
        ("UpstairsCloset", -0.15f, -3.30f, true, 0.9f, 2.6f),
        ("UpstairsOffice", 2.25f, -2.90f, true, 1.6f, 4.0f),
    };

    private const float UpperFloor = 2.48f;
    private const float GroundCeilingLight = 2.0f;
    private const float UpperCeilingLight = 4.35f;
    private static readonly Color RoomLightColor = new Color(1f, 0.95f, 0.88f); // soft warm white

    private const string BakedGroupName = "Baked Lighting";
    private const string HouseShellScene = "CrimeScene_HouseShell";

    // Bakes the crime scene's lighting: sunlight and sky bouncing in through
    // the windows plus a soft light per room, into lightmaps on the house
    // (CrimeScene_HouseShell). Loose props are lit by a light-probe grid and
    // reflect two box-projected reflection probes, one per floor.
    //
    // The sun stays a Mixed light (Baked Indirect): its direct light and
    // shadows are realtime, only its bounce is baked. Everything lives in
    // CrimeScene_Main > Lighting > Baked Lighting and is rebuilt on every run.
    //
    // Re-run whenever the house, the room layout or the lighting changes. All
    // crime scene scenes must be open (they are baked together and share one
    // LightingData asset in Scenes/Production/CrimeScene/CrimeScene_Main/).
    [MenuItem(MenuRoot + "Bake Crime Scene Lighting", false, 0)]
    public static void BakeCrimeSceneLighting()
    {
        Scene main = SceneManager.GetSceneByPath(CrimeSceneEditorTools.MainScenePath);
        Scene shell = SceneManager.GetSceneByName(HouseShellScene);
        if (!main.isLoaded || !shell.isLoaded)
        {
            Debug.LogWarning("[Crime Scene Lighting] Open CrimeScene_Main with all its additive scenes first " +
                             "(Tools > Crime Scene > Additive Scenes > Open All For Editing).");
            return;
        }

        Transform lighting = FindInScene(main, "Lighting");
        if (lighting == null)
        {
            Debug.LogError("[Crime Scene Lighting] No \"Lighting\" object in CrimeScene_Main.");
            return;
        }
        SceneManager.SetActiveScene(main);

        Light sun = RenderSettings.sun;
        if (sun != null)
            sun.lightmapBakeType = LightmapBakeType.Mixed;

        Transform group = BuildBakedLightingObjects(lighting, out var ownLights);
        int staticCount = PrepareStaticHouse(shell);
        ApplyBakeSettings(main);

        // Any other baked/mixed light (e.g. ones inside player or prop
        // prefabs) would be burnt into the walls at its editor position.
        var muted = new List<Light>();
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light == sun || ownLights.Contains(light) || light.lightmapBakeType == LightmapBakeType.Realtime || !light.enabled)
                continue;
            light.enabled = false;
            muted.Add(light);
        }

        bool ok;
        try
        {
            ok = Lightmapping.Bake();
        }
        finally
        {
            foreach (Light light in muted)
                light.enabled = true;
        }

        if (!ok)
        {
            Debug.LogError("[Crime Scene Lighting] Bake failed or was cancelled.");
            return;
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Crime Scene Lighting] Baked: {staticCount} static house renderers, {ownLights.Count} room lights, " +
                  $"{group.GetComponentInChildren<LightProbeGroup>().probePositions.Length} light probes, " +
                  $"{muted.Count} other baked lights left out.");
    }

    private static Transform BuildBakedLightingObjects(Transform lighting, out List<Light> lights)
    {
        foreach (string oldName in new[] { BakedGroupName, "Interior", "Interior Reflection Probes" })
        {
            Transform old = lighting.Find(oldName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }
        var group = new GameObject(BakedGroupName).transform;
        group.SetParent(lighting, false);

        // Room lights: baked only, so they cost nothing at runtime and have
        // no per-object light limit.
        var lightRoot = new GameObject("Room Lights").transform;
        lightRoot.SetParent(group, false);
        lights = new List<Light>();
        foreach (var room in Rooms)
        {
            var light = new GameObject(room.name + " Light").AddComponent<Light>();
            light.transform.SetParent(lightRoot, false);
            light.transform.position = new Vector3(room.x, room.upper ? UpperCeilingLight : GroundCeilingLight, room.z);
            light.type = LightType.Point;
            light.lightmapBakeType = LightmapBakeType.Baked;
            light.color = RoomLightColor;
            light.intensity = room.intensity;
            light.range = room.range;
            light.shadows = LightShadows.Soft;
            light.shapeRadius = 0.15f;
            lights.Add(light);
        }

        // Reflection probes: walls and props inside reflect the room, not the
        // outdoor sky.
        var probeRoot = new GameObject("Reflection Probes").transform;
        probeRoot.SetParent(group, false);
        CreateProbe(probeRoot, "Ground Floor Probe", new Vector3(0f, 1.19f, 0f), new Vector3(7.7f, 2.38f, 9.2f));
        CreateProbe(probeRoot, "Upper Floor Probe", new Vector3(0f, 3.6f, 0f), new Vector3(7.8f, 2.25f, 9.2f));

        // Light probes: a fine grid inside the house, a ring just outside it
        // (doorways and windows) and a coarse grid over the neighbourhood, so
        // dynamic objects outdoors don't pick up the darker indoor probes.
        var probeGroup = new GameObject("Light Probes").AddComponent<LightProbeGroup>();
        probeGroup.transform.SetParent(group, false);
        var positions = new List<Vector3>();
        float[] heights = { 0.4f, 1.3f, 2.1f, UpperFloor + 0.4f, UpperFloor + 1.2f, UpperFloor + 2.0f };
        for (float x = -3.4f; x <= 3.41f; x += 0.85f)
            for (float z = -4.1f; z <= 4.11f; z += 1.025f)
                foreach (float y in heights)
                    positions.Add(new Vector3(x, y, z));
        for (float t = -5.5f; t <= 5.51f; t += 1f)
            foreach (float y in new[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f })
            {
                if (Mathf.Abs(t) <= 4.6f)
                {
                    positions.Add(new Vector3(t, y, -5.3f));
                    positions.Add(new Vector3(t, y, 5.3f));
                }
                positions.Add(new Vector3(-4.6f, y, t));
                positions.Add(new Vector3(4.6f, y, t));
            }
        for (float x = -64f; x <= 64f; x += 8f)
            for (float z = -64f; z <= 64f; z += 8f)
            {
                if (Mathf.Abs(x) < 8f && Mathf.Abs(z) < 8f)
                    continue;
                positions.Add(new Vector3(x, 0.6f, z));
                positions.Add(new Vector3(x, 4f, z));
            }
        probeGroup.probePositions = positions.ToArray(); // local = world (group at origin)

        return group;
    }

    private static void CreateProbe(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var probe = new GameObject(name).AddComponent<ReflectionProbe>();
        probe.transform.SetParent(parent, false);
        probe.transform.position = center;
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
        probe.size = size;
        probe.boxProjection = true;
        probe.blendDistance = 0.3f;
        probe.resolution = 128;
        probe.hdr = true;
        probe.importance = 1;
        probe.nearClipPlane = 0.05f;
    }

    // Marks the house geometry static for GI (doors excluded: they open) and
    // makes sure its models have lightmap UVs.
    private static int PrepareStaticHouse(Scene shell)
    {
        const StaticEditorFlags flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic |
                                        StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                                        StaticEditorFlags.OccludeeStatic;
        var models = new HashSet<string>();
        int count = 0;
        foreach (GameObject root in shell.GetRootGameObjects())
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.GetComponentInParent<Rigidbody>(true) != null || HasAncestor(renderer.transform, "SharedDoors"))
                    continue;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
                renderer.receiveGI = ReceiveGI.Lightmaps;
                count++;

                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    models.Add(AssetDatabase.GetAssetPath(filter.sharedMesh));
            }

        foreach (string path in models)
        {
            if (AssetImporter.GetAtPath(path) is ModelImporter importer && !importer.generateSecondaryUV)
            {
                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
                Debug.Log("[Crime Scene Lighting] Generated lightmap UVs for " + path);
            }
        }

        EditorSceneManager.MarkSceneDirty(shell);
        return count;
    }

    private static void ApplyBakeSettings(Scene main)
    {
        string folder = Path.GetDirectoryName(main.path).Replace('\\', '/');
        string settingsPath = $"{folder}/{main.name}_LightingSettings.lighting";
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
        if (settings == null)
        {
            settings = new LightingSettings { name = main.name + "_LightingSettings" };
            AssetDatabase.CreateAsset(settings, settingsPath);
        }

        settings.bakedGI = true;
        settings.realtimeGI = false;
        settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        settings.lightmapResolution = 12f; // texels per metre: enough for soft indoor light on Quest
        settings.lightmapPadding = 2;
        settings.lightmapMaxSize = 1024;
        settings.directionalityMode = LightmapsMode.NonDirectional;
        settings.lightmapCompression = LightmapCompression.NormalQuality;
        settings.ao = true;
        settings.aoMaxDistance = 0.6f;
        settings.maxBounces = 2;
        settings.directSampleCount = 32;
        settings.indirectSampleCount = 256;
        settings.environmentSampleCount = 256;
        EditorUtility.SetDirty(settings);

        Lightmapping.lightingSettings = settings;
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
        return null;
    }

    private static bool HasAncestor(Transform t, string name)
    {
        for (; t != null; t = t.parent)
            if (t.name == name)
                return true;
        return false;
    }

    [MenuItem(MenuRoot + "Render Lighting Previews", false, 20)]
    public static void RenderPreviews()
    {
        Scene main = SceneManager.GetSceneByPath(CrimeSceneEditorTools.MainScenePath);
        if (!main.isLoaded)
        {
            Debug.LogWarning("[Crime Scene Lighting] Open CrimeScene_Main (with its additive scenes) first.");
            return;
        }

        Directory.CreateDirectory(PreviewFolder);
        var shots = new List<(string name, Vector3 position, Quaternion rotation)>();

        foreach (string startName in new[] { "PlayerStart_Outside", "PlayerStart_Inside" })
        {
            GameObject start = FindInLoadedScenes(startName);
            if (start == null)
                continue;
            Vector3 eye = start.transform.position + Vector3.up * EyeHeight;
            Quaternion facing = Quaternion.Euler(0f, start.transform.eulerAngles.y, 0f);
            shots.Add((startName, eye, facing));
            shots.Add((startName + "_back", eye, facing * Quaternion.Euler(0f, 180f, 0f)));
        }

        foreach (var room in Rooms)
        {
            float floor = room.upper ? UpperFloor : 0f;
            Vector3 eye = new Vector3(room.x, floor + EyeHeight, room.z);
            // Look from the room centre towards the house's outer corners.
            float yaw = Mathf.Atan2(room.x, room.z) * Mathf.Rad2Deg;
            shots.Add((room.name, eye, Quaternion.Euler(10f, yaw, 0f)));
            shots.Add((room.name + "_back", eye, Quaternion.Euler(10f, yaw + 180f, 0f)));
        }

        var cameraGo = new GameObject("Lighting Preview Camera") { hideFlags = HideFlags.HideAndDontSave };
        var cam = cameraGo.AddComponent<Camera>();
        var data = cameraGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 80f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 1000f;
        cam.aspect = 4f / 3f;
        var rt = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        try
        {
            foreach (var shot in shots)
            {
                cam.transform.SetPositionAndRotation(shot.position, shot.rotation);
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

        Debug.Log($"[Crime Scene Lighting] {shots.Count} previews written to {Path.GetFullPath(PreviewFolder)}");
    }

    private static GameObject FindInLoadedScenes(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t.gameObject;
        }
        return null;
    }
}
