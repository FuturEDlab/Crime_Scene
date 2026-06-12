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
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
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
    }

    public void SetMovementType(bool useTeleport)
    {
        CurrentSettings.movement.type = useTeleport ? "teleport" : "continuous";
        // TODO: Swap active locomotion provider components.
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
        // TODO: Apply multiplier to the ContinuousMoveProvider.
        SaveSettings("iPad");
    }

    public void SetTimeOfDay(bool isDay)
    {
        CurrentSettings.environment.timeOfDay = isDay ? "day" : "night";
        // TODO: Swap skybox and adjust directional light.
        SaveSettings("iPad");
    }

    public void SetBackgroundVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        CurrentSettings.audio.backgroundVolume = volume;
        // TODO: No background audio assets exist yet.
        SaveSettings("iPad");
    }

    public void SetNarrationVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        CurrentSettings.audio.narrationVolume = volume;
        // TODO: No narration audio assets exist yet.
        SaveSettings("iPad");
    }

    public void SetSoundFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        CurrentSettings.audio.soundFXVolume = volume;
        // TODO: No SFX audio assets exist yet.
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