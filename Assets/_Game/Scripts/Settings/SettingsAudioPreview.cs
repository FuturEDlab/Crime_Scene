// Plays a short test sound when the player drags the Narration or Sound FX
// slider on the iPad Settings screen, so those sliders are audible while being
// adjusted. The Background slider needs no preview — the scenes already play
// ambient audio through that group, so it is audible on its own.
//
// Lives on the Tablet_manager prefab root, next to SettingsManager /
// SettingsApplier. Its references are all ASSETS (mixer groups + clips), never
// scene objects, so they are safe to assign on the prefab and survive the
// scene loads the tablet persists through.
//
// The preview AudioSources are created in code and routed to the mixer groups,
// which means a preview is a genuine end-to-end test of the routing: if the
// test sound does not respond to the slider, the mixer wiring is wrong, not the
// UI. It deliberately does NOT go through SettingsApplier — that applies the
// saved volume to the mixer; this only makes noise for the mixer to act on.

using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsAudioPreview : MonoBehaviour
{
    public static SettingsAudioPreview Instance { get; private set; }

    [Header("Mixer groups (assets — safe on the prefab)")]
    [Tooltip("The Narration group of the settings AudioMixer.")]
    [SerializeField] private AudioMixerGroup narrationGroup;
    [Tooltip("The SFX group of the settings AudioMixer.")]
    [SerializeField] private AudioMixerGroup soundFXGroup;

    [Header("Test clips")]
    [Tooltip("Short spoken clip — a statement recording works well.")]
    [SerializeField] private AudioClip narrationTestClip;
    [Tooltip("Short non-spoken clip — a camera shutter or similar.")]
    [SerializeField] private AudioClip soundFXTestClip;

    [Header("Behaviour")]
    [Tooltip("Seconds of slider silence before the test sound plays. Stops a " +
             "drag from retriggering the clip on every value change.")]
    [SerializeField] private float previewDelay = 0.25f;

    [Tooltip("Seconds of the clip to play before fading out. 0 plays it whole.")]
    [SerializeField] private float previewLength = 1.5f;

    private AudioSource _narrationSource;
    private AudioSource _soundFXSource;
    private Coroutine _pending;

    private void Awake()
    {
        // Same duplicate-destroys-itself rule the other tablet singletons use:
        // a second copy arriving with a newly loaded scene must not replace the
        // original that has been alive since the session started.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        _narrationSource = CreatePreviewSource("PreviewSource_Narration", narrationGroup);
        _soundFXSource = CreatePreviewSource("PreviewSource_SFX", soundFXGroup);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // A 2D, non-looping source routed to the given group. 2D (spatialBlend 0)
    // matters: a preview must be the same volume wherever the player is
    // standing, unlike the world's positional ambient sources.
    private AudioSource CreatePreviewSource(string sourceName, AudioMixerGroup group)
    {
        var host = new GameObject(sourceName);
        host.transform.SetParent(transform, false);

        var source = host.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = group;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        return source;
    }

    public void PreviewNarration() => Preview(_narrationSource, narrationTestClip, "Narration");

    public void PreviewSoundFX() => Preview(_soundFXSource, soundFXTestClip, "Sound FX");

    private void Preview(AudioSource source, AudioClip clip, string label)
    {
        if (source == null)
            return;

        if (clip == null)
        {
            Debug.LogWarning($"[SettingsAudioPreview] No test clip assigned for {label} — " +
                             $"assign one on the Tablet_manager prefab to hear this slider.");
            return;
        }

        if (source.outputAudioMixerGroup == null)
        {
            Debug.LogWarning($"[SettingsAudioPreview] {label} preview has no mixer group — " +
                             $"it will play at full volume and ignore the slider. Assign the " +
                             $"{label} group on the Tablet_manager prefab.");
        }

        // Only the most recent slider movement survives: dragging across several
        // values should end in one preview, not a queue of them.
        if (_pending != null)
            StopCoroutine(_pending);
        _pending = StartCoroutine(PlayAfterDelay(source, clip));
    }

    private IEnumerator PlayAfterDelay(AudioSource source, AudioClip clip)
    {
        // Unscaled: settings stay usable if the game is ever paused.
        yield return new WaitForSecondsRealtime(previewDelay);

        // Silence the other preview so the two can't overlap into mush.
        if (_narrationSource != null && _narrationSource != source) _narrationSource.Stop();
        if (_soundFXSource != null && _soundFXSource != source) _soundFXSource.Stop();

        source.Stop();
        source.clip = clip;
        source.Play();
        _pending = null;

        if (previewLength > 0f && clip.length > previewLength)
        {
            yield return new WaitForSecondsRealtime(previewLength);
            // Only stop if this preview is still the one playing — a newer
            // preview may have taken over the source in the meantime.
            if (source.clip == clip && source.isPlaying)
                source.Stop();
        }
    }
}
