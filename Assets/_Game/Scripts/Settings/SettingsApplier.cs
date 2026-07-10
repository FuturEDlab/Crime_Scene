// Applies the shared settings (see SettingsManager) to the world: locomotion,
// time-of-day lighting and audio.
//
// Lives on the Tablet_manager prefab root, next to SettingsManager /
// PhotoLibrary / EvidenceGradingManager, so the whole settings app ships inside
// the one tablet prefab with zero per-scene setup.
//
// A prefab cannot hold Inspector references to scene objects (the directional
// light, the player rig's locomotion) — and the tablet persists across scene
// loads while those objects are recreated per scene. So this component
// auto-finds its targets at runtime and re-resolves + re-applies after every
// scene load (CSHouse <-> CS_Outside) and whenever a setting changes.

using System.Collections;
using BNG;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SettingsApplier : MonoBehaviour
{
    // DAY is simply each scene's own authored lighting, captured on scene load
    // and restored. Only NIGHT needs a preset. This works even in CSHouse, which
    // has no lights at all and is lit purely by skybox ambient.
    [Header("Night preset")]
    [Tooltip("Flat ambient colour used at night (the main night effect — it works " +
             "in scenes with no lights, like the house interior).")]
    [SerializeField] private Color nightAmbientColor = new Color(0.05f, 0.07f, 0.13f);

    [Tooltip("If the scene has a directional light, dim/cool it to this at night.")]
    [SerializeField] private Color nightSunColor = new Color(0.55f, 0.65f, 0.95f);
    [SerializeField] private float nightSunIntensity = 0.2f;

    [Tooltip("Optional night skybox material (an asset — safe to assign on the " +
             "prefab). Leave empty to clear the skybox at night, which renders a " +
             "dark background.")]
    [SerializeField] private Material nightSkybox;

    [Header("Audio (optional — asset, safe to assign on the prefab)")]
    [Tooltip("AudioMixer with exposed float params (in dB) named to match the " +
             "fields below. Leave empty until audio assets exist in the project.")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string backgroundVolumeParam = "BackgroundVolume";
    [SerializeField] private string narrationVolumeParam = "NarrationVolume";
    [SerializeField] private string soundFXVolumeParam = "SFXVolume";

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // Scene objects, re-found after every scene load (never serialized —
    // a persistent prefab cannot keep references into scenes).
    private LocomotionManager _locomotionManager;
    private SmoothLocomotion _smoothLocomotion;
    private Light _directionalLight;

    // Base MovementSpeed captured from the rig when it is resolved, so the
    // multiplier stays relative to the designer's value, not hard-coded.
    private float _baseMovementSpeed = 1.25f;

    // The scene's own authored lighting, captured ONCE per scene load and
    // restored when the player selects "day". Guarded by the scene handle so a
    // second resolve in the same scene can't accidentally capture night values
    // we applied ourselves.
    private int _capturedSceneHandle = -1;
    private UnityEngine.Rendering.AmbientMode _dayAmbientMode;
    private Color _dayAmbientColor;
    private float _dayAmbientIntensity;
    private Material _daySkybox;
    private Quaternion _daySunRotation;
    private Color _daySunColor;
    private float _daySunIntensity;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged += ApplyAll;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= ApplyAll;
    }

    private void Start()
    {
        // SettingsManager on the same object may have Awoken after our OnEnable —
        // make sure we are subscribed exactly once.
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= ApplyAll;
            SettingsManager.Instance.OnSettingsChanged += ApplyAll;
        }

        ResolveSceneObjects();
        ApplyAll();

        // Re-apply at end of frame to win any Start()-order race with BNG's
        // LocomotionManager, which also sets a default locomotion in its Start().
        StartCoroutine(ApplyEndOfFrame());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The tablet persists but the light and player rig are new objects in the
        // freshly loaded scene — find them again, then re-apply the settings.
        ResolveSceneObjects();
        ApplyAll();
        StartCoroutine(ApplyEndOfFrame());
    }

    private IEnumerator ApplyEndOfFrame()
    {
        yield return null;
        ApplyAll();
    }

    // Finds this scene's locomotion components and main directional light, and
    // captures the scene's authored ("day") lighting exactly once per scene.
    private void ResolveSceneObjects()
    {
        _locomotionManager = FindFirstObjectByType<LocomotionManager>();
        _smoothLocomotion = FindFirstObjectByType<SmoothLocomotion>();

        // settings.json is the single source of truth for locomotion — stop BNG
        // from also loading its own PlayerPrefs value over ours.
        if (_locomotionManager != null)
            _locomotionManager.LoadLocomotionFromPrefs = false;

        // Prefer the light Unity itself considers the sun; fall back to the
        // first directional light in the scene (CSHouse has none — that's fine,
        // night is driven through ambient light there).
        _directionalLight = RenderSettings.sun;
        if (_directionalLight == null)
        {
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    _directionalLight = light;
                    break;
                }
            }
        }

        // Capture the untouched scene lighting ONCE per scene. Resolve can run
        // again in the same scene (Start after sceneLoaded); re-capturing then
        // would record values we already modified.
        int sceneHandle = SceneManager.GetActiveScene().handle;
        if (sceneHandle != _capturedSceneHandle)
        {
            _capturedSceneHandle = sceneHandle;

            if (_smoothLocomotion != null)
                _baseMovementSpeed = _smoothLocomotion.MovementSpeed;

            _dayAmbientMode = RenderSettings.ambientMode;
            _dayAmbientColor = RenderSettings.ambientLight;
            _dayAmbientIntensity = RenderSettings.ambientIntensity;
            _daySkybox = RenderSettings.skybox;

            if (_directionalLight != null)
            {
                _daySunRotation = _directionalLight.transform.rotation;
                _daySunColor = _directionalLight.color;
                _daySunIntensity = _directionalLight.intensity;
            }
        }

        if (verboseLogging)
            Debug.Log($"[SettingsApplier] Scene objects resolved. " +
                      $"locomotion={( _locomotionManager != null )}, " +
                      $"smooth={( _smoothLocomotion != null )}, " +
                      $"sun={( _directionalLight != null ? _directionalLight.name : "none" )}.");
    }

    public void ApplyAll()
    {
        if (SettingsManager.Instance == null)
            return;

        var s = SettingsManager.Instance.CurrentSettings;
        ApplyMovementType(s.movement.type);
        ApplyMovementSpeed(s.movement.speed);
        ApplyTimeOfDay(s.environment.timeOfDay);
        ApplyAudio(s.audio);
    }

    // ----- Movement ----------------------------------------------------------

    private void ApplyMovementType(string type)
    {
        if (_locomotionManager == null)
            return; // no rig in this scene (e.g. main menu)

        bool useTeleport = type == "teleport";
        _locomotionManager.ChangeLocomotion(
            useTeleport ? LocomotionType.Teleport : LocomotionType.SmoothLocomotion,
            save: false);
    }

    private void ApplyMovementSpeed(float multiplier)
    {
        if (_smoothLocomotion == null)
            return;

        _smoothLocomotion.MovementSpeed = _baseMovementSpeed * multiplier;
    }

    // ----- Time of day -------------------------------------------------------

    private void ApplyTimeOfDay(string timeOfDay)
    {
        bool isDay = timeOfDay != "night";

        if (isDay)
        {
            // Day = the scene exactly as the designer authored it.
            RenderSettings.ambientMode = _dayAmbientMode;
            RenderSettings.ambientLight = _dayAmbientColor;
            RenderSettings.ambientIntensity = _dayAmbientIntensity;
            RenderSettings.skybox = _daySkybox;

            if (_directionalLight != null)
            {
                _directionalLight.transform.rotation = _daySunRotation;
                _directionalLight.color = _daySunColor;
                _directionalLight.intensity = _daySunIntensity;
            }
        }
        else
        {
            // Night: flat dark ambient is the workhorse — it darkens every scene,
            // including CSHouse, which has no lights and is lit by ambient only.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = nightAmbientColor;
            RenderSettings.ambientIntensity = 1f;

            // No night skybox assigned -> clear it, which renders a dark sky.
            RenderSettings.skybox = nightSkybox;

            if (_directionalLight != null)
            {
                _directionalLight.color = nightSunColor;
                _directionalLight.intensity = nightSunIntensity;
            }
        }

        // Ambient/skybox changed — rebuild the environment lighting probe.
        DynamicGI.UpdateEnvironment();
    }

    // ----- Audio -------------------------------------------------------------

    private void ApplyAudio(SettingsManager.AudioSettings audio)
    {
        if (audioMixer == null)
        {
            // TODO: No AudioMixer exists yet because the project has no
            // background/narration/SFX audio assets. Once audio is added, create
            // an AudioMixer with three exposed volume params (names above),
            // assign it on the Tablet_manager prefab, and route the scene's
            // AudioSources through its groups. Volumes are already being saved
            // to settings.json in the meantime.
            return;
        }

        SetMixerVolume(backgroundVolumeParam, audio.backgroundVolume);
        SetMixerVolume(narrationVolumeParam, audio.narrationVolume);
        SetMixerVolume(soundFXVolumeParam, audio.soundFXVolume);
    }

    // Converts a linear 0..1 slider value to decibels for an AudioMixer param.
    private void SetMixerVolume(string param, float linear01)
    {
        if (string.IsNullOrEmpty(param))
            return;

        // -80 dB is Unity's mixer floor (effectively muted); log curve above that.
        float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear01)) * 20f;
        audioMixer.SetFloat(param, dB);
    }
}
