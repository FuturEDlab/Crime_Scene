using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class RecordingListItem : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dateTimeText;
    public Button playPauseButton;
    public TextMeshProUGUI playPauseLabel;
    public Slider progressSlider;

    private RecordingData recordingData;
    private AudioSource audioSource;

    private bool isPlaying;
    private bool isScrubbing;

    // prevents OnValueChanged from firing when we update slider by code
    private bool ignoreSliderEvent;

    // Called when this item starts playing so UI manager can stop others
    private Action<RecordingListItem> onWantsToPlay;

    public void Setup(RecordingData data, AudioSource sharedSource, Action<RecordingListItem> wantsToPlayCallback)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("RecordingListItem.Setup: data or clip is null.");
            return;
        }

        recordingData = data;
        audioSource = sharedSource;
        onWantsToPlay = wantsToPlayCallback;

        if (speakerNameText) speakerNameText.text = data.speakerName;
        if (dateTimeText) dateTimeText.text = data.displayDateTime;

        // Button
        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveAllListeners();
            playPauseButton.onClick.AddListener(TogglePlayPause);
        }

        // Slider
        if (progressSlider != null)
        {
            progressSlider.onValueChanged.RemoveAllListeners();
            progressSlider.wholeNumbers = false;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = data.clip.length;

            SetSliderValue(0f);

            progressSlider.onValueChanged.AddListener(Scrub);

            // ✅ Add drag begin/end detection so Update() doesn't fight the user
            AddScrubEventsIfMissing(progressSlider);
        }

        isPlaying = false;
        isScrubbing = false;
        SetPlayLabel(false);
    }

    private void TogglePlayPause()
    {
        if (recordingData == null || audioSource == null || recordingData.clip == null)
            return;

        // If currently NOT playing, start
        if (!isPlaying)
        {
            // Tell UI manager we want to play (so it resets others)
            onWantsToPlay?.Invoke(this);

            audioSource.Stop();
            audioSource.clip = recordingData.clip;

            // Start from slider position if user moved it before pressing play
            float startTime = progressSlider != null ? progressSlider.value : 0f;
            audioSource.time = Mathf.Clamp(startTime, 0f, recordingData.clip.length);

            audioSource.Play();

            isPlaying = true;
            SetPlayLabel(true);
        }
        else
        {
            // Pause only
            audioSource.Pause();
            isPlaying = false;
            SetPlayLabel(false);
        }
    }

    void Update()
    {
        if (audioSource == null || recordingData == null) return;

        // If another item started playback, shared audioSource will have a different clip
        if (isPlaying && audioSource.clip != recordingData.clip)
        {
            ForceStopUIOnly();
            return;
        }

        // If audio ended naturally
        if (isPlaying && audioSource.clip == recordingData.clip && !audioSource.isPlaying)
        {
            // If near end, treat as finished
            if (audioSource.time >= audioSource.clip.length - 0.05f)
            {
                isPlaying = false;
                SetPlayLabel(false);
                SetSliderValue(0f);
                audioSource.time = 0f;
            }
            else
            {
                // paused manually or interrupted
                isPlaying = false;
                SetPlayLabel(false);
            }
        }

        // ✅ Update slider while playing (but don't fight user scrubbing)
        if (audioSource.clip == recordingData.clip && audioSource.isPlaying && !isScrubbing && progressSlider != null)
        {
            SetSliderValue(audioSource.time);
        }
    }

    private void Scrub(float value)
    {
        if (ignoreSliderEvent) return;
        if (audioSource == null || audioSource.clip == null || recordingData == null) return;

        // ✅ Only scrub audio if this item is currently active clip
        if (audioSource.clip == recordingData.clip)
        {
            // If user is dragging, skim should jump
            if (isScrubbing)
            {
                audioSource.time = Mathf.Clamp(value, 0f, audioSource.clip.length);
            }
            // If not scrubbing, this could be programmatic or a click on bar.
            // We'll still allow seek when playing:
            else if (audioSource.isPlaying)
            {
                audioSource.time = Mathf.Clamp(value, 0f, audioSource.clip.length);
            }
        }
        else
        {
            // Not active clip: allow UI slider movement but no audio seek.
        }
    }

    // Called by AudioRecordingsUI when another item starts playing
    public void ForceStopUIOnly()
    {
        isPlaying = false;
        SetPlayLabel(false);

        if (progressSlider != null && !isScrubbing)
        {
            SetSliderValue(0f);
        }
    }

    private void SetPlayLabel(bool playing)
    {
        if (playPauseLabel != null)
            playPauseLabel.text = playing ? "Pause" : "Play";
    }

    // -----------------------------
    // Drag detection (adds automatically)
    // -----------------------------
    private void AddScrubEventsIfMissing(Slider slider)
    {
        if (slider == null) return;

        var trigger = slider.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = slider.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

        // Pointer Down
        if (!HasTrigger(trigger, EventTriggerType.PointerDown))
        {
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener((data) => OnSliderPointerDown((BaseEventData)data));
            trigger.triggers.Add(entry);
        }

        // Pointer Up
        if (!HasTrigger(trigger, EventTriggerType.PointerUp))
        {
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entry.callback.AddListener((data) => OnSliderPointerUp((BaseEventData)data));
            trigger.triggers.Add(entry);
        }
    }

    private bool HasTrigger(EventTrigger trigger, EventTriggerType type)
    {
        for (int i = 0; i < trigger.triggers.Count; i++)
        {
            if (trigger.triggers[i].eventID == type) return true;
        }
        return false;
    }

    public void OnSliderPointerDown(BaseEventData data)
    {
        isScrubbing = true;
    }

    public void OnSliderPointerUp(BaseEventData data)
    {
        isScrubbing = false;

        // ✅ Seek once more on release (very reliable)
        if (audioSource != null && audioSource.clip != null && recordingData != null &&
            audioSource.clip == recordingData.clip && progressSlider != null)
        {
            audioSource.time = Mathf.Clamp(progressSlider.value, 0f, audioSource.clip.length);
        }
    }

    // Safe setter to avoid feedback loop
    private void SetSliderValue(float v)
    {
        if (progressSlider == null) return;
        ignoreSliderEvent = true;
        progressSlider.value = v;
        ignoreSliderEvent = false;
    }

    // Optional external control (if you ever want it)
    public void SetScrubbing(bool scrubbing) => isScrubbing = scrubbing;
}
