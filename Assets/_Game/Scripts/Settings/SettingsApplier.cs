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
//
// Lighting model: DAY is never *computed* — it is the scene's own lighting,
// snapshotted untouched at scene load and written back verbatim. That includes
// RenderSettings.ambientProbe, the baked spherical-harmonic probe Unity applies
// from the LightingDataAsset. A runtime DynamicGI.UpdateEnvironment() cannot
// reproduce it: the baked probe carries bounce light, a runtime skybox
// projection does not, so recomputing always lands dimmer than the level's
// opening brightness.
//
// TIMING IS THE WHOLE TRICK. Unity applies the baked probe as part of loading
// the scene, and it is not reliably in place during Start()/sceneLoaded. So the
// snapshot is deferred a couple of frames (lightingSettleFrames) and NO
// time-of-day is applied until it has been taken — applying lighting first
// would overwrite the values being snapshotted, which is exactly how day ended
// up permanently darker after a night toggle.
//
// The consequence worth knowing: for those first frames the scene simply shows
// its own authored lighting. If the player starts in night, night lands a frame
// or two in. That is deliberate, and cheaper than the alternative.

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

    [Header("Lighting capture")]
    [Tooltip("Frames to wait after a scene load before snapshotting its lighting, " +
             "so Unity has finished applying the baked ambient probe. Raise this " +
             "if day-after-night still doesn't match the level's opening brightness.")]
    [SerializeField] private int lightingSettleFrames = 2;

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
    // restored verbatim when the player selects "day". Guarded by the scene
    // handle so a second resolve in the same scene can't accidentally capture
    // night values we applied ourselves.
    private int _capturedSceneHandle = -1;
    private UnityEngine.Rendering.AmbientMode _dayAmbientMode;
    private Color _dayAmbientColor;
    private float _dayAmbientIntensity;
    private Material _daySkybox;
    private Quaternion _daySunRotation;
    private Color _daySunColor;
    private float _daySunIntensity;

    // The baked ambient probe and reflection that Unity applies from the scene's
    // LightingDataAsset. This — not ambientMode/colour/intensity — is what makes
    // day look the way it does, because a baked probe carries bounce light that a
    // runtime skybox projection does not. Restoring it verbatim is the only way
    // day-after-night can match the brightness the level opened on.
    private UnityEngine.Rendering.SphericalHarmonicsL2 _dayAmbientProbe;
    private float _dayReflectionIntensity;
    private UnityEngine.Rendering.DefaultReflectionMode _dayReflectionMode;
    private Texture _dayCustomReflection;

    // False until the day snapshot has been taken. Time-of-day is NOT applied
    // before then — writing lighting first would destroy the very values we are
    // trying to snapshot.
    private bool _dayLightingCaptured;

    // Audio misconfiguration is reported once, not every time a slider moves.
    private bool _warnedNoMixer;
    private readonly System.Collections.Generic.HashSet<string> _warnedMissingParams = new();

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

        BeginScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The tablet persists but the light and player rig are new objects in the
        // freshly loaded scene — find them again and re-run the capture.
        BeginScene();
    }

    // Entry point for "a scene just became current". Resolves objects, applies
    // everything that is safe to apply immediately, then hands off to the
    // coroutine that snapshots lighting once it has settled.
    private void BeginScene()
    {
        ResolveSceneObjects();
        ApplyAll(); // movement + audio only — ApplyAll skips lighting until captured
        StopCoroutine(nameof(CaptureThenApplyLighting));
        StartCoroutine(nameof(CaptureThenApplyLighting));
    }

    private IEnumerator CaptureThenApplyLighting()
    {
        // Let the scene settle before reading its lighting. Unity applies the
        // baked ambient probe from the LightingDataAsset as part of the load, and
        // reading it too early yields the pre-bake default — which then becomes a
        // permanently-too-dark "day".
        for (int i = 0; i < Mathf.Max(1, lightingSettleFrames); i++)
            yield return null;

        CaptureDayLighting();

        // Now safe: the snapshot exists, so lighting can be applied. This also
        // re-applies locomotion, winning any Start()-order race with BNG's
        // LocomotionManager, which sets a default locomotion in its own Start().
        ApplyAll();
    }

    // Finds this scene's locomotion components and main directional light. Does
    // NOT touch lighting — that is CaptureDayLighting's job, deliberately
    // deferred (see the header comment).
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

        // A new scene invalidates the previous day snapshot — mark it stale so
        // ApplyAll withholds lighting until the coroutine re-captures.
        int sceneHandle = SceneManager.GetActiveScene().handle;
        if (sceneHandle != _capturedSceneHandle)
        {
            _capturedSceneHandle = sceneHandle;
            _dayLightingCaptured = false;

            if (_smoothLocomotion != null)
                _baseMovementSpeed = _smoothLocomotion.MovementSpeed;
        }

        if (verboseLogging)
            Debug.Log($"[SettingsApplier] Scene objects resolved. " +
                      $"locomotion={( _locomotionManager != null )}, " +
                      $"smooth={( _smoothLocomotion != null )}, " +
                      $"sun={( _directionalLight != null ? _directionalLight.name : "none" )}.");
    }

    // Snapshots the scene's lighting exactly as authored/baked. Must run before
    // ApplyTimeOfDay has written anything, or it records our own night values.
    private void CaptureDayLighting()
    {
        if (_dayLightingCaptured)
            return;

        _dayAmbientMode = RenderSettings.ambientMode;
        _dayAmbientColor = RenderSettings.ambientLight;
        _dayAmbientIntensity = RenderSettings.ambientIntensity;
        _daySkybox = RenderSettings.skybox;

        // The parts that actually carry the brightness.
        _dayAmbientProbe = RenderSettings.ambientProbe;
        _dayReflectionIntensity = RenderSettings.reflectionIntensity;
        _dayReflectionMode = RenderSettings.defaultReflectionMode;
        _dayCustomReflection = RenderSettings.customReflectionTexture;

        if (_directionalLight != null)
        {
            _daySunRotation = _directionalLight.transform.rotation;
            _daySunColor = _directionalLight.color;
            _daySunIntensity = _directionalLight.intensity;
        }

        _dayLightingCaptured = true;

        if (verboseLogging)
            Debug.Log($"[SettingsApplier] Day lighting captured after {lightingSettleFrames} frame(s). " +
                      $"ambientMode={_dayAmbientMode}, intensity={_dayAmbientIntensity}, " +
                      $"probeLuma={ProbeLuma(_dayAmbientProbe):F4}. " +
                      $"Returning to day should report this same probeLuma.");
    }

    // Rough brightness of an ambient probe: the DC (constant) term of the
    // spherical harmonic, which is what a flat "how bright is the ambient"
    // question actually means. Logged so a day/night/day cycle can be compared
    // numerically instead of by eye.
    private static float ProbeLuma(UnityEngine.Rendering.SphericalHarmonicsL2 sh) =>
        0.2126f * sh[0, 0] + 0.7152f * sh[1, 0] + 0.0722f * sh[2, 0];

    public void ApplyAll()
    {
        if (SettingsManager.Instance == null)
            return;

        var s = SettingsManager.Instance.CurrentSettings;
        ApplyMovementType(s.movement.type);
        ApplyMovementSpeed(s.movement.speed);

        // Withhold lighting until the day snapshot exists. Writing it now would
        // clobber the baked probe before CaptureDayLighting can read it, and the
        // scene is already showing correct day lighting in the meantime anyway.
        if (_dayLightingCaptured)
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
            RenderSettings.defaultReflectionMode = _dayReflectionMode;
            RenderSettings.customReflectionTexture = _dayCustomReflection;
            RenderSettings.reflectionIntensity = _dayReflectionIntensity;

            if (_directionalLight != null)
            {
                _directionalLight.transform.rotation = _daySunRotation;
                _directionalLight.color = _daySunColor;
                _directionalLight.intensity = _daySunIntensity;
            }

            // Order matters. UpdateEnvironment regenerates the reflection from the
            // restored skybox, but it ALSO overwrites ambientProbe with a runtime
            // skybox projection that is dimmer than the baked one. So run it first,
            // then put the snapshotted probe back — last write wins, and day ends
            // up bit-for-bit what the level opened on.
            DynamicGI.UpdateEnvironment();
            RenderSettings.ambientProbe = _dayAmbientProbe;

            if (verboseLogging)
                Debug.Log($"[SettingsApplier] Day restored. probeLuma={ProbeLuma(RenderSettings.ambientProbe):F4} " +
                          $"(captured {ProbeLuma(_dayAmbientProbe):F4} — these must match).");
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

            // Ambient/skybox changed — rebuild the environment lighting probe.
            DynamicGI.UpdateEnvironment();
        }
    }

    // ----- Audio -------------------------------------------------------------

    private void ApplyAudio(SettingsManager.AudioSettings audio)
    {
        if (audioMixer == null)
        {
            // Volumes are still saved to settings.json — they just have nothing to
            // act on. Assign the mixer on the Tablet_manager prefab to connect them.
            if (verboseLogging && !_warnedNoMixer)
            {
                _warnedNoMixer = true;
                Debug.LogWarning("[SettingsApplier] No AudioMixer assigned — the volume " +
                                 "sliders will save but change nothing. Assign one to the " +
                                 "'Audio Mixer' field on the Tablet_manager prefab.");
            }
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
        // The log curve is what makes a linear slider *sound* linear — halfway
        // lands at about -6 dB, which the ear reads as "half as loud".
        float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear01)) * 20f;

        // SetFloat returns false when no exposed parameter has that name. This is
        // the single most common way mixer volume "silently does nothing": the
        // group exists but its volume was never exposed, or was exposed under a
        // different name. Say so once, loudly, with the name we actually tried.
        if (!audioMixer.SetFloat(param, dB) && !_warnedMissingParams.Contains(param))
        {
            _warnedMissingParams.Add(param);
            Debug.LogError($"[SettingsApplier] AudioMixer '{audioMixer.name}' has no exposed " +
                           $"parameter named '{param}'. In the Audio Mixer window, right-click " +
                           $"that group's Volume slider > 'Expose ... to script', then rename the " +
                           $"entry under Exposed Parameters to exactly '{param}'.");
        }
    }
}
