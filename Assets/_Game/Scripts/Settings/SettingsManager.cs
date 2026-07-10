// This script manages all game settings by reading and writing to settings.json.
// It is shared between the main menu (Issue #12) and the iPad panel (Issue #13)
// so that any changes made in either place are saved and carried over between sessions.
// Some functions are stubbed out with TODO comments because the required assets,
// such as audio and locomotion components, are not yet in the project.

using System;
using System.IO;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy ONLY this component, never the whole GameObject. This
            // manager rides on the Tablet_manager prefab root (next to
            // PhotoLibrary / EvidenceGradingManager); if a SettingsManager from a
            // previous scene (e.g. the main menu) already persists, destroying
            // the GameObject here would delete the entire tablet.
            Destroy(this);
            return;
        }
        Instance = this;

        // DontDestroyOnLoad only works on root objects. On the tablet the root
        // already persists via TabletPersist, so this is just for standalone use
        // (e.g. a SettingsManager object in the main menu scene).
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private static string SettingsFilePath =>
        Path.Combine(Application.persistentDataPath, "settings.json");

    [Serializable]
    public class MovementSettings
    {
        public string type = "continuous";
        public float speed = 1.0f;
    }

    [Serializable]
    public class EnvironmentSettings
    {
        public string timeOfDay = "day";
    }

    [Serializable]
    public class AudioSettings
    {
        public float backgroundVolume = 1.0f;
        public float narrationVolume = 1.0f;
        public float soundFXVolume = 1.0f;
    }

    [Serializable]
    public class AccessibilitySettings
    {
        public bool subtitles = false;
        public string colorblindMode = "none";
    }

    [Serializable]
    public class SettingsData
    {
        public MovementSettings movement = new MovementSettings();
        public EnvironmentSettings environment = new EnvironmentSettings();
        public AudioSettings audio = new AudioSettings();
        public AccessibilitySettings accessibility = new AccessibilitySettings();
    }

    [Serializable]
    private class SettingsWrapper
    {
        public SettingsData settings = new SettingsData();
        public MetaData meta = new MetaData();
    }

    [Serializable]
    private class MetaData
    {
        public string lastUpdatedFrom = "mainMenu";
        public string lastUpdated = "";
    }

    public SettingsData CurrentSettings => _wrapper.settings;
    private SettingsWrapper _wrapper = new SettingsWrapper();

    // Raised after settings are loaded OR changed. Scene-side components
    // (SettingsApplier) subscribe to this to push the new values onto the actual
    // world objects — lights, locomotion, audio — that this persistent singleton
    // cannot hold references to across scene loads.
    public event Action OnSettingsChanged;

    public void LoadSettings()
    {
        if (File.Exists(SettingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                _wrapper = JsonUtility.FromJson<SettingsWrapper>(json);
                Debug.Log($"[SettingsManager] Settings loaded from {SettingsFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsManager] Failed to parse settings file, using defaults. Error: {e.Message}");
                _wrapper = new SettingsWrapper();
                SaveSettings("system");
            }
        }
        else
        {
            Debug.Log("[SettingsManager] No settings file found — writing defaults.");
            SaveSettings("system");
        }

        OnSettingsChanged?.Invoke();
    }

    public void SaveSettings(string source = "iPad")
    {
        _wrapper.meta.lastUpdatedFrom = source;
        _wrapper.meta.lastUpdated = DateTime.UtcNow.ToString("o");
        try
        {
            string json = JsonUtility.ToJson(_wrapper, prettyPrint: true);
            File.WriteAllText(SettingsFilePath, json);
            Debug.Log($"[SettingsManager] Settings saved ({source}).");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsManager] Could not save settings: {e.Message}");
        }

        // Whoever changed a value (main menu or iPad) writes the shared file, so
        // notify listeners in the current scene to re-apply immediately.
        OnSettingsChanged?.Invoke();
    }

    // ----- Setters -----------------------------------------------------------
    // Each setter only edits the shared data model and persists it. The actual
    // in-world effect is applied by SettingsApplier, which listens to
    // OnSettingsChanged. This keeps SettingsManager scene-independent so the same
    // instance survives from the main menu into the experience.

    public void SetMovementType(bool useTeleport)
    {
        CurrentSettings.movement.type = useTeleport ? "teleport" : "continuous";
        SaveSettings("iPad");
    }

    public void SetMovementSpeed(float speedMultiplier)
    {
        float[] validSpeeds = { 0.75f, 1.0f, 1.25f };
        bool isValid = Array.Exists(validSpeeds, s => Mathf.Approximately(s, speedMultiplier));
        if (!isValid)
        {
            Debug.LogWarning($"[SettingsManager] Invalid speed {speedMultiplier}.");
            return;
        }
        CurrentSettings.movement.speed = speedMultiplier;
        SaveSettings("iPad");
    }

    public void SetTimeOfDay(bool isDay)
    {
        CurrentSettings.environment.timeOfDay = isDay ? "day" : "night";
        SaveSettings("iPad");
    }

    public void SetBackgroundVolume(float volume)
    {
        CurrentSettings.audio.backgroundVolume = Mathf.Clamp01(volume);
        SaveSettings("iPad");
    }

    public void SetNarrationVolume(float volume)
    {
        CurrentSettings.audio.narrationVolume = Mathf.Clamp01(volume);
        SaveSettings("iPad");
    }

    public void SetSoundFXVolume(float volume)
    {
        CurrentSettings.audio.soundFXVolume = Mathf.Clamp01(volume);
        SaveSettings("iPad");
    }

    public void SetSubtitles(bool enabled)
    {
        CurrentSettings.accessibility.subtitles = enabled;
        // TODO: Subtitles feature coming in a separate issue.
        SaveSettings("iPad");
    }

    public void SetColorblindMode(string mode)
    {
        CurrentSettings.accessibility.colorblindMode = mode;
        // TODO: Colorblind mode coming in a separate issue.
        SaveSettings("iPad");
    }
}