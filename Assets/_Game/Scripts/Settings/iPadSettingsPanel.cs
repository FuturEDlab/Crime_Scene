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
using UnityEngine.UI;
using TMPro;

public class iPadSettingsPanel : MonoBehaviour
{
    [Header("Panel (optional — leave empty if ScreenManager owns visibility)")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;

    [Header("Movement Type")]
    [SerializeField] private Toggle teleportToggle;

    [Header("Movement Speed")]
    [SerializeField] private Button speed075Button;
    [SerializeField] private Button speed100Button;
    [SerializeField] private Button speed125Button;

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
        if (speed075Button != null) speed075Button.onClick.AddListener(() => OnSpeedSelected(0.75f));
        if (speed100Button != null) speed100Button.onClick.AddListener(() => OnSpeedSelected(1.0f));
        if (speed125Button != null) speed125Button.onClick.AddListener(() => OnSpeedSelected(1.25f));
        if (dayButton != null) dayButton.onClick.AddListener(() => OnTimeOfDaySelected(true));
        if (nightButton != null) nightButton.onClick.AddListener(() => OnTimeOfDaySelected(false));

        SetupSlider(backgroundSlider, OnBackgroundVolumeChanged);
        SetupSlider(narrationSlider, OnNarrationVolumeChanged);
        SetupSlider(soundFXSlider, OnSoundFXVolumeChanged);

        // Only self-manage visibility when acting as a standalone pop-up.
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

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

    private void OnNarrationVolumeChanged(float value)
    {
        SettingsManager.Instance.SetNarrationVolume(value);
        UpdateVolumeLabel(narrationValueText, value);
    }

    private void OnSoundFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSoundFXVolume(value);
        UpdateVolumeLabel(soundFXValueText, value);
    }

    private void HighlightSpeedButton(float speed)
    {
        SetButtonColor(speed075Button, Mathf.Approximately(speed, 0.75f));
        SetButtonColor(speed100Button, Mathf.Approximately(speed, 1.0f));
        SetButtonColor(speed125Button, Mathf.Approximately(speed, 1.25f));
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
