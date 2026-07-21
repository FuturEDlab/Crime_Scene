// Controls the iPad's Settings screen. Every control writes through
// SettingsManager, which persists to the shared settings.json (the same file the
// main menu writes), so a change made here overrides what was set in the menu.
//
// Screen visibility is normally owned by the tablet's ScreenManager (ShowSettings()
// activates this screen, like the Camera/Evidence/Notebook apps). In that setup
// leave 'Settings Panel' and 'Settings Button' unassigned — this component just
// wires the controls and refreshes their values from the saved settings each time
// the screen is shown (OnEnable). The optional Panel/Button fields let it also run
// as a self-contained pop-up if ever needed outside the ScreenManager flow.

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class iPadSettingsPanel : MonoBehaviour
{
    [Header("Panel (optional — leave empty if ScreenManager owns visibility)")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;

    [Header("Movement Type")]
    [SerializeField] private Toggle teleportToggle;

    // Three speed buttons in slow / normal / fast order. Named after their role,
    // not a fixed number, so changing SettingsManager.SpeedOptions relabels them
    // without renaming anything. FormerlySerializedAs keeps the prefab's existing
    // references (from when these were 0.75/1.0/1.25) wired after the rename.
    [Header("Movement Speed (slow / normal / fast)")]
    [FormerlySerializedAs("speed075Button")]
    [SerializeField] private Button speedSlowButton;
    [FormerlySerializedAs("speed100Button")]
    [SerializeField] private Button speedNormalButton;
    [FormerlySerializedAs("speed125Button")]
    [SerializeField] private Button speedFastButton;

    [Header("Environment")]
    [SerializeField] private Button dayButton;
    [SerializeField] private Button nightButton;

    [Header("Audio Sliders")]
    [SerializeField] private Slider backgroundSlider;
    [SerializeField] private Slider narrationSlider;
    [SerializeField] private Slider soundFXSlider;

    [Header("Audio Value Labels")]
    [SerializeField] private TextMeshProUGUI backgroundValueText;
    [SerializeField] private TextMeshProUGUI narrationValueText;
    [SerializeField] private TextMeshProUGUI soundFXValueText;

    [Header("Button State Colors")]
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color defaultColor = new Color(0.85f, 0.85f, 0.85f);

    private bool _wired;

    private void Awake()
    {
        WireControls();
    }

    private void OnEnable()
    {
        // The screen just became visible (ScreenManager.ShowSettings, or the
        // optional pop-up) — refresh every control from the saved settings.
        PopulateUIFromSettings();
    }

    // Hooks up all control callbacks exactly once.
    private void WireControls()
    {
        if (_wired)
            return;
        _wired = true;

        if (settingsButton != null) settingsButton.onClick.AddListener(OpenPanel);
        if (teleportToggle != null) teleportToggle.onValueChanged.AddListener(OnMovementTypeChanged);
        WireSpeedButtons();
        if (dayButton != null) dayButton.onClick.AddListener(() => OnTimeOfDaySelected(true));
        if (nightButton != null) nightButton.onClick.AddListener(() => OnTimeOfDaySelected(false));

        SetupSlider(backgroundSlider, OnBackgroundVolumeChanged);
        SetupSlider(narrationSlider, OnNarrationVolumeChanged);
        SetupSlider(soundFXSlider, OnSoundFXVolumeChanged);

        // Only self-manage visibility when acting as a standalone pop-up.
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // The three buttons in slow/normal/fast order, matching SettingsManager.SpeedOptions.
    private Button[] SpeedButtons => new[] { speedSlowButton, speedNormalButton, speedFastButton };

    // Points each button at its speed and rewrites its caption to match. The
    // caption is set from code (not the prefab) so the numbers on screen can
    // never drift out of sync with the values the buttons actually apply.
    private void WireSpeedButtons()
    {
        Button[] buttons = SpeedButtons;
        float[] speeds = SettingsManager.SpeedOptions;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            if (i >= speeds.Length)
            {
                // Fewer speeds than buttons — hide the spare rather than leave a
                // dead button on the screen.
                buttons[i].gameObject.SetActive(false);
                continue;
            }

            float speed = speeds[i]; // copied per iteration: the closure must not capture i
            buttons[i].onClick.AddListener(() => OnSpeedSelected(speed));

            var label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = SpeedLabel(speed);
        }
    }

    // 0.5 -> "0.5x", 1 -> "1.0x", 1.5 -> "1.5x". Invariant culture so the decimal
    // is always a dot, never a comma, regardless of the device locale.
    private static string SpeedLabel(float speed) =>
        speed.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "x";

    private void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null)
            return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(callback);
    }

    private void OpenPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        PopulateUIFromSettings();
    }

    public void ClosePanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void PopulateUIFromSettings()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("[iPadSettingsPanel] No SettingsManager in scene — controls not populated.");
            return;
        }

        var s = SettingsManager.Instance.CurrentSettings;

        if (teleportToggle != null)
        {
            teleportToggle.onValueChanged.RemoveListener(OnMovementTypeChanged);
            teleportToggle.isOn = s.movement.type == "teleport";
            teleportToggle.onValueChanged.AddListener(OnMovementTypeChanged);
        }

        HighlightSpeedButton(s.movement.speed);
        HighlightTimeButton(s.environment.timeOfDay == "day");

        if (backgroundSlider != null) backgroundSlider.SetValueWithoutNotify(s.audio.backgroundVolume);
        if (narrationSlider != null) narrationSlider.SetValueWithoutNotify(s.audio.narrationVolume);
        if (soundFXSlider != null) soundFXSlider.SetValueWithoutNotify(s.audio.soundFXVolume);

        UpdateVolumeLabel(backgroundValueText, s.audio.backgroundVolume);
        UpdateVolumeLabel(narrationValueText, s.audio.narrationVolume);
        UpdateVolumeLabel(soundFXValueText, s.audio.soundFXVolume);
    }

    private void OnMovementTypeChanged(bool isTeleport)
    {
        SettingsManager.Instance.SetMovementType(isTeleport);
    }

    private void OnSpeedSelected(float speed)
    {
        SettingsManager.Instance.SetMovementSpeed(speed);
        HighlightSpeedButton(speed);
    }

    private void OnTimeOfDaySelected(bool isDay)
    {
        SettingsManager.Instance.SetTimeOfDay(isDay);
        HighlightTimeButton(isDay);
    }

    private void OnBackgroundVolumeChanged(float value)
    {
        SettingsManager.Instance.SetBackgroundVolume(value);
        UpdateVolumeLabel(backgroundValueText, value);
    }

    // Narration and SFX have nothing playing in the world most of the time, so
    // without a test sound these sliders would feel dead while being dragged.
    // Background needs no preview — the scenes' ambient audio is already audible.
    private void OnNarrationVolumeChanged(float value)
    {
        SettingsManager.Instance.SetNarrationVolume(value);
        UpdateVolumeLabel(narrationValueText, value);
        if (SettingsAudioPreview.Instance != null)
            SettingsAudioPreview.Instance.PreviewNarration();
    }

    private void OnSoundFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSoundFXVolume(value);
        UpdateVolumeLabel(soundFXValueText, value);
        if (SettingsAudioPreview.Instance != null)
            SettingsAudioPreview.Instance.PreviewSoundFX();
    }

    private void HighlightSpeedButton(float speed)
    {
        Button[] buttons = SpeedButtons;
        float[] speeds = SettingsManager.SpeedOptions;
        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = i < speeds.Length && Mathf.Approximately(speed, speeds[i]);
            SetButtonColor(buttons[i], selected);
        }
    }

    private void HighlightTimeButton(bool isDay)
    {
        SetButtonColor(dayButton, isDay);
        SetButtonColor(nightButton, !isDay);
    }

    private void SetButtonColor(Button button, bool isSelected)
    {
        if (button == null)
            return;
        var img = button.GetComponent<Image>();
        if (img != null)
            img.color = isSelected ? selectedColor : defaultColor;
    }

    private void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(value * 100) + "%";
    }
}
