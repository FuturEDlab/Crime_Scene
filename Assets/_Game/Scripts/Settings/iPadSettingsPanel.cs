// This is a work in progress pushed to the feature/ipad-settings-panel branch for safe keeping.
// The UI has been built but the inspector references and script wiring are not yet complete.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class iPadSettingsPanel : MonoBehaviour
{
    [Header("Panel")]
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

    private void Start()
    {
        settingsButton.onClick.AddListener(OpenPanel);
        teleportToggle.onValueChanged.AddListener(OnMovementTypeChanged);
        speed075Button.onClick.AddListener(() => OnSpeedSelected(0.75f));
        speed100Button.onClick.AddListener(() => OnSpeedSelected(1.0f));
        speed125Button.onClick.AddListener(() => OnSpeedSelected(1.25f));
        dayButton.onClick.AddListener(() => OnTimeOfDaySelected(true));
        nightButton.onClick.AddListener(() => OnTimeOfDaySelected(false));

        backgroundSlider.minValue = 0f;
        backgroundSlider.maxValue = 1f;
        narrationSlider.minValue = 0f;
        narrationSlider.maxValue = 1f;
        soundFXSlider.minValue = 0f;
        soundFXSlider.maxValue = 1f;

        backgroundSlider.onValueChanged.AddListener(OnBackgroundVolumeChanged);
        narrationSlider.onValueChanged.AddListener(OnNarrationVolumeChanged);
        soundFXSlider.onValueChanged.AddListener(OnSoundFXVolumeChanged);

        settingsPanel.SetActive(false);
    }

    private void OpenPanel()
    {
        settingsPanel.SetActive(true);
        PopulateUIFromSettings();
    }

    public void ClosePanel()
    {
        settingsPanel.SetActive(false);
    }

    private void PopulateUIFromSettings()
    {
        var s = SettingsManager.Instance.CurrentSettings;

        teleportToggle.onValueChanged.RemoveAllListeners();
        teleportToggle.isOn = s.movement.type == "teleport";
        teleportToggle.onValueChanged.AddListener(OnMovementTypeChanged);

        HighlightSpeedButton(s.movement.speed);
        HighlightTimeButton(s.environment.timeOfDay == "day");

        backgroundSlider.SetValueWithoutNotify(s.audio.backgroundVolume);
        narrationSlider.SetValueWithoutNotify(s.audio.narrationVolume);
        soundFXSlider.SetValueWithoutNotify(s.audio.soundFXVolume);

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
