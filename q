[1mdiff --git a/.DS_Store b/.DS_Store[m
[1mdeleted file mode 100644[m
[1mindex 7419db7..0000000[m
Binary files a/.DS_Store and /dev/null differ
[1mdiff --git a/.github/PULL_REQUEST_TEMPLATE.md b/.github/PULL_REQUEST_TEMPLATE.md[m
[1mdeleted file mode 100644[m
[1mindex 86135e0..0000000[m
[1m--- a/.github/PULL_REQUEST_TEMPLATE.md[m
[1m+++ /dev/null[m
[36m@@ -1,7 +0,0 @@[m
[31m-⚠️ Before submitting a pull request, please follow these instructions![m
[31m-[m
[31m-1. Click "Preview" above this template.[m
[31m-2. Then, please select the appropriate pull request template and fill it out:[m
[31m-[m
[31m-- [Feature Template](?expand=1&template=feature.md)[m
[31m-- [Bug Fix Template](?expand=1&template=bugfix.md)[m
[1mdiff --git a/.github/PULL_REQUEST_TEMPLATE/bugfix.md b/.github/PULL_REQUEST_TEMPLATE/bugfix.md[m
[1mdeleted file mode 100644[m
[1mindex e8dff3d..0000000[m
[1m--- a/.github/PULL_REQUEST_TEMPLATE/bugfix.md[m
[1m+++ /dev/null[m
[36m@@ -1,40 +0,0 @@[m
[31m-# 🐞 Bug Fix Pull Request[m
[31m-[m
[31m-## What bug did you fix?[m
[31m-[m
[31m-[Insert a short summary of the bug and how you fixed it]  [m
[31m-Example: Fixed a bug where players couldn’t grab certain objects due to a missing collider tag.[m
[31m-[m
[31m----[m
[31m-[m
[31m-## How did you test it?[m
[31m-[m
[31m-- How did you initally produce/reproduce the bug? [Insert steps here][m
[31m-- How did you test the fix? [Insert steps here][m
[31m-- Platform(s) tested on: [e.g., Unity Editor, Meta Quest, etc.][m
[31m-- Test results: [Insert what happened after the fix][m
[31m-[m
[31m-Example:  [m
[31m-Tested in GrabScene on Meta Quest 2. Grabbing works now across all objects. No new issues noticed.[m
[31m-[m
[31m----[m
[31m-[m
[31m-## What did this change affect?[m
[31m-[m
[31m-- Scripts changed: [Insert script names here][m
[31m-- Prefabs changed: [Insert prefab names here][m
[31m-- Other systems affected: [Insert if applicable][m
[31m-[m
[31m-Could this cause other problems?[m
[31m-- [ ] No[m
[31m-- [ ] Yes — [Briefly explain any risks here][m
[31m-[m
[31m----[m
[31m-[m
[31m-## Media (optional)[m
[31m-[m
[31m-[Insert screenshots, GIFs, or video links showing the fix if applicable][m
[31m-[m
[31m----[m
[31m-[m
[31m-> ✅ I confirm that I tested this bug fix and verified that it works as expected without breaking other systems.[m
[1mdiff --git a/.github/PULL_REQUEST_TEMPLATE/feature.md b/.github/PULL_REQUEST_TEMPLATE/feature.md[m
[1mdeleted file mode 100644[m
[1mindex 45fb7a4..0000000[m
[1m--- a/.github/PULL_REQUEST_TEMPLATE/feature.md[m
[1m+++ /dev/null[m
[36m@@ -1,39 +0,0 @@[m
[31m-# ✨ Feature Pull Request[m
[31m-[m
[31m-## What does this feature add?[m
[31m-[m
[31m-[Insert a short description of the new feature here]  [m
[31m-Example: Adds teleportation to the main menu so users can move more easily.[m
[31m-[m
[31m----[m
[31m-[m
[31m-## How did you test it?[m
[31m-[m
[31m-- Scenes tested: [Insert scene name(s) here][m
[31m-- Platform(s) tested on: [e.g., Unity Editor, Meta Quest 2, etc.][m
[31m-- Test results: [Insert what you observed – did it work? Any issues?][m
[31m-[m
[31m-Example:  [m
[31m-Tested in MainMenuScene on Meta Quest 3. Teleportation worked in all intended areas. No errors found.[m
[31m-[m
[31m----[m
[31m-[m
[31m-## What did this change affect?[m
[31m-[m
[31m-- Scripts changed: [Insert script names here][m
[31m-- Prefabs changed: [Insert prefab names here][m
[31m-- Other affected systems: [Insert any other relevant systems here][m
[31m-[m
[31m-Does this affect existing features?[m
[31m-- [ ] No[m
[31m-- [ ] Yes — [Briefly explain how here][m
[31m-[m
[31m----[m
[31m-[m
[31m-## Media (optional)[m
[31m-[m
[31m-[Insert screenshots, GIFs, or video links here if your feature has a visual component][m
[31m-[m
[31m----[m
[31m-[m
[31m-> ✅ I confirm that I have personally tested this feature and checked that it doesn’t break anything else.[m
[1mdiff --git a/.vscode/extensions.json b/.vscode/extensions.json[m
[1mdeleted file mode 100644[m
[1mindex ddb6ff8..0000000[m
[1m--- a/.vscode/extensions.json[m
[1m+++ /dev/null[m
[36m@@ -1,5 +0,0 @@[m
[31m-{[m
[31m-    "recommendations": [[m
[31m-      "visualstudiotoolsforunity.vstuc"[m
[31m-    ][m
[31m-}[m
[1mdiff --git a/.vscode/launch.json b/.vscode/launch.json[m
[1mdeleted file mode 100644[m
[1mindex da60e25..0000000[m
[1m--- a/.vscode/launch.json[m
[1m+++ /dev/null[m
[36m@@ -1,10 +0,0 @@[m
[31m-{[m
[31m-    "version": "0.2.0",[m
[31m-    "configurations": [[m
[31m-        {[m
[31m-            "name": "Attach to Unity",[m
[31m-            "type": "vstuc",[m
[31m-            "request": "attach"[m
[31m-        }[m
[31m-     ][m
[31m-}[m
\ No newline at end of file[m
[1mdiff --git a/.vscode/settings.json b/.vscode/settings.json[m
[1mdeleted file mode 100644[m
[1mindex de3d53c..0000000[m
[1m--- a/.vscode/settings.json[m
[1m+++ /dev/null[m
[36m@@ -1,60 +0,0 @@[m
[31m-{[m
[31m-    "files.exclude": {[m
[31m-        "**/.DS_Store": true,[m
[31m-        "**/.git": true,[m
[31m-        "**/.vs": true,[m
[31m-        "**/.gitmodules": true,[m
[31m-        "**/.vsconfig": true,[m
[31m-        "**/*.booproj": true,[m
[31m-        "**/*.pidb": true,[m
[31m-        "**/*.suo": true,[m
[31m-        "**/*.user": true,[m
[31m-        "**/*.userprefs": true,[m
[31m-        "**/*.unityproj": true,[m
[31m-        "**/*.dll": true,[m
[31m-        "**/*.exe": true,[m
[31m-        "**/*.pdf": true,[m
[31m-        "**/*.mid": true,[m
[31m-        "**/*.midi": true,[m
[31m-        "**/*.wav": true,[m
[31m-        "**/*.gif": true,[m
[31m-        "**/*.ico": true,[m
[31m-        "**/*.jpg": true,[m
[31m-        "**/*.jpeg": true,[m
[31m-        "**/*.png": true,[m
[31m-        "**/*.psd": true,[m
[31m-        "**/*.tga": true,[m
[31m-        "**/*.tif": true,[m
[31m-        "**/*.tiff": true,[m
[31m-        "**/*.3ds": true,[m
[31m-        "**/*.3DS": true,[m
[31m-        "**/*.fbx": true,[m
[31m-        "**/*.FBX": true,[m
[31m-        "**/*.lxo": true,[m
[31m-        "**/*.LXO": true,[m
[31m-        "**/*.ma": true,[m
[31m-        "**/*.MA": true,[m
[31m-        "**/*.obj": true,[m
[31m-        "**/*.OBJ": true,[m
[31m-        "**/*.asset": true,[m
[31m-        "**/*.cubemap": true,[m
[31m-        "**/*.flare": true,[m
[31m-        "**/*.mat": true,[m
[31m-        "**/*.meta": true,[m
[31m-        "**/*.prefab": true,[m
[31m-        "**/*.unity": true,[m
[31m-        "build/": true,[m
[31m-        "Build/": true,[m
[31m-        "Library/": true,[m
[31m-        "library/": true,[m
[31m-        "obj/": true,[m
[31m-        "Obj/": true,[m
[31m-        "Logs/": true,[m
[31m-        "logs/": true,[m
[31m-        "ProjectSettings/": true,[m
[31m-        "UserSettings/": true,[m
[31m-        "temp/": true,[m
[31m-        "Temp/": true[m
[31m-    },[m
[31m-    "dotnet.defaultSolution": "Crime_Scene.sln"[m
[31m-}[m
\ No newline at end of file[m
[1mdiff --git a/Assets/BreakIn_Skip_Button b/Assets/BreakIn_Skip_Button[m
[1mdeleted file mode 100644[m
[1mindex a069ca7..0000000[m
[1m--- a/Assets/BreakIn_Skip_Button[m
[1m+++ /dev/null[m
[36m@@ -1,36 +0,0 @@[m
[31m-using UnityEngine;[m
[31m-using UnityEngine.SceneManagement;[m
[31m-using UnityEngine.UI;[m
[31m-[m
[31m-public class SkipHandler : MonoBehaviour[m
[31m-{[m
[31m-    [SerializeField] private GameObject confirmationPopup;[m
[31m-    [SerializeField] private Button skipButton;[m
[31m-    [SerializeField] private Button yesButton;[m
[31m-    [SerializeField] private Button noButton;[m
[31m-    [SerializeField] private string startMenuSceneName = "StartMenu"; // change to your actual scene name[m
[31m-[m
[31m-    void Start()[m
[31m-    {[m
[31m-        confirmationPopup.SetActive(false);[m
[31m-[m
[31m-        skipButton.onClick.AddListener(ShowConfirmation);[m
[31m-        yesButton.onClick.AddListener(SkipToStartMenu);[m
[31m-        noButton.onClick.AddListener(HideConfirmation);[m
[31m-    }[m
[31m-[m
[31m-    void ShowConfirmation()[m
[31m-    {[m
[31m-        confirmationPopup.SetActive(true);[m
[31m-    }[m
[31m-[m
[31m-    void HideConfirmation()[m
[31m-    {[m
[31m-        confirmationPopup.SetActive(false);[m
[31m-    }[m
[31m-[m
[31m-    void SkipToStartMenu()[m
[31m-    {[m
[31m-        SceneManager.LoadScene(startMenuSceneName);[m
[31m-    }[m
[31m-}[m
[1mdiff --git a/Assets/BreakIn_Skip_Button.meta b/Assets/BreakIn_Skip_Button.meta[m
[1mdeleted file mode 100644[m
[1mindex f6ec3f6..0000000[m
[1m--- a/Assets/BreakIn_Skip_Button.meta[m
[1m+++ /dev/null[m
[36m@@ -1,7 +0,0 @@[m
[31m-fileFormatVersion: 2[m
[31m-guid: f982130e02dcc4a99ac23830939468c0[m
[31m-DefaultImporter:[m
[31m-  externalObjects: {}[m
[31m-  userData: [m
[31m-  assetBundleName: [m
[31m-  assetBundleVariant: [m
[1mdiff --git a/ProjectSettings/GraphicsSettings.asset b/ProjectSettings/GraphicsSettings.asset[m
[1mindex d5698ba..ea0fb10 100644[m
[1m--- a/ProjectSettings/GraphicsSettings.asset[m
[1m+++ b/ProjectSettings/GraphicsSettings.asset[m
[36m@@ -3,7 +3,7 @@[m
 --- !u!30 &1[m
 GraphicsSettings:[m
   m_ObjectHideFlags: 0[m
[31m-  serializedVersion: 16[m
[32m+[m[32m  serializedVersion: 13[m
   m_Deferred:[m
     m_Mode: 1[m
     m_Shader: {fileID: 69, guid: 0000000000000000f000000000000000, type: 0}[m
[36m@@ -13,6 +13,9 @@[m [mGraphicsSettings:[m
   m_ScreenSpaceShadows:[m
     m_Mode: 1[m
     m_Shader: {fileID: 64, guid: 0000000000000000f000000000000000, type: 0}[m
[32m+[m[32m  m_LegacyDeferred:[m
[32m+[m[32m    m_Mode: 1[m
[32m+[m[32m    m_Shader: {fileID: 63, guid: 0000000000000000f000000000000000, type: 0}[m
   m_DepthNormals:[m
     m_Mode: 1[m
     m_Shader: {fileID: 62, guid: 0000000000000000f000000000000000, type: 0}[m
[36m@@ -25,7 +28,6 @@[m [mGraphicsSettings:[m
   m_LensFlare:[m
     m_Mode: 1[m
     m_Shader: {fileID: 102, guid: 0000000000000000f000000000000000, type: 0}[m
[31m-  m_VideoShadersIncludeMode: 2[m
   m_AlwaysIncludedShaders:[m
   - {fileID: 7, guid: 0000000000000000f000000000000000, type: 0}[m
   - {fileID: 15104, guid: 0000000000000000f000000000000000, type: 0}[m
[36m@@ -39,7 +41,6 @@[m [mGraphicsSettings:[m
   - {fileID: 17000, guid: 0000000000000000f000000000000000, type: 0}[m
   - {fileID: 16003, guid: 0000000000000000f000000000000000, type: 0}[m
   m_PreloadedShaders: [][m
[31m-  m_PreloadShadersBatchTimeLimit: -1[m
   m_SpritesDefaultMaterial: {fileID: 10754, guid: 0000000000000000f000000000000000,[m
     type: 0}[m
   m_CustomRenderPipeline: {fileID: 0}[m
[36m@@ -51,7 +52,6 @@[m [mGraphicsSettings:[m
   m_LightmapStripping: 0[m
   m_FogStripping: 0[m
   m_InstancingStripping: 0[m
[31m-  m_BrgStripping: 0[m
   m_LightmapKeepPlain: 1[m
   m_LightmapKeepDirCombined: 1[m
   m_LightmapKeepDynamicPlain: 1[m
[36m@@ -62,12 +62,7 @@[m [mGraphicsSettings:[m
   m_FogKeepExp: 1[m
   m_FogKeepExp2: 1[m
   m_AlbedoSwatchInfos: [][m
[31m-  m_RenderPipelineGlobalSettingsMap:[m
[31m-    UnityEngine.Rendering.Universal.UniversalRenderPipeline: {fileID: 11400000, guid: 18dc0cd2c080841dea60987a38ce93fa,[m
[31m-      type: 2}[m
   m_LightsUseLinearIntensity: 0[m
   m_LightsUseColorTemperature: 0[m
   m_LogWhenShaderIsCompiled: 0[m
[31m-  m_LightProbeOutsideHullStrategy: 0[m
[31m-  m_CameraRelativeLightCulling: 0[m
[31m-  m_CameraRelativeShadowCulling: 0[m
[32m+[m[32m  m_AllowEnlightenSupportForUpgradedProject: 1[m
[1mdiff --git a/ProjectSettings/ProjectSettings.asset b/ProjectSettings/ProjectSettings.asset[m
[1mindex 69a1db3..cceb7b8 100644[m
[1m--- a/ProjectSettings/ProjectSettings.asset[m
[1m+++ b/ProjectSettings/ProjectSettings.asset[m
[36m@@ -1072,7 +1072,7 @@[m [mPlayerSettings:[m
   cloudProjectId: bf3860ef-0517-4d5d-973f-b694a9d066df[m
   framebufferDepthMemorylessMode: 0[m
   qualitySettingsNames: [][m
[31m-  projectName: My project 2025-03-02_09-33-32[m
[32m+[m[32m  projectName:[m[41m [m
   organizationId: gvsu-technology-showcase[m
   cloudEnabled: 0[m
   legacyClampBlendShapeWeights: 1[m
[1mdiff --git a/SkipButtonCode b/SkipButtonCode[m
[1mdeleted file mode 100644[m
[1mindex e9bb605..0000000[m
[1m--- a/SkipButtonCode[m
[1m+++ /dev/null[m
[36m@@ -1,36 +0,0 @@[m
[31m-using UnityEngine;[m
[31m-using UnityEngine.UI;[m
[31m-using UnityEngine.SceneManagement;[m
[31m-[m
[31m-public class SkipHandler : MonoBehaviour[m
[31m-{[m
[31m-    [SerializeField] private GameObject confirmationPopup;[m
[31m-    [SerializeField] private Button skipButton;[m
[31m-    [SerializeField] private Button yesButton;[m
[31m-    [SerializeField] private Button noButton;[m
[31m-    [SerializeField] private string startMenuSceneName = "StartMenu"; // Change to your actual scene name[m
[31m-[m
[31m-    void Start()[m
[31m-    {[m
[31m-        confirmationPopup.SetActive(false); // Hide popup initially[m
[31m-[m
[31m-        skipButton.onClick.AddListener(ShowConfirmation);[m
[31m-        yesButton.onClick.AddListener(SkipToStartMenu);[m
[31m-        noButton.onClick.AddListener(HideConfirmation);[m
[31m-    }[m
[31m-[m
[31m-    void ShowConfirmation()[m
[31m-    {[m
[31m-        confirmationPopup.SetActive(true);[m
[31m-    }[m
[31m-[m
[31m-    void HideConfirmation()[m
[31m-    {[m
[31m-        confirmationPopup.SetActive(false);[m
[31m-    }[m
[31m-[m
[31m-    void SkipToStartMenu()[m
[31m-    {[m
[31m-        SceneManager.LoadScene(startMenuSceneName);[m
[31m-    }[m
[31m-}[m
