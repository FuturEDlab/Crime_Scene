
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Settings Data")]
    private SettingsData settings;
    private string settingsFilePath;

    [Header("UI References")]
    public GameObject settingsPanel;
    public Button settingsButton;
    public Button closeSettingsButton;

    [Header("Movement Type Buttons")]
    public Button teleportingButton;
    public Button continuousButton;

    [Header("Movement Speed Buttons")]
    public Button speed075Button;
    public Button speed100Button;
    public Button speed125Button;

    [Header("Time of Day Buttons")]
    public Button dayButton;
    public Button nightButton;

    [Header("Volume Sliders")]
    public Slider backgroundVolumeSlider;
    public Slider narrationVolumeSlider;
    public Slider soundFXVolumeSlider;

    [Header("Volume Text")]
    public TextMeshProUGUI backgroundVolumeText;
    public TextMeshProUGUI narrationVolumeText;
    public TextMeshProUGUI soundFXVolumeText;

    [Header("Scene References")]
    public Light directionalLight;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        settingsFilePath = Path.Combine(Application.persistentDataPath, "vr_settings.json");
        LoadSettings();
    }

    private void Start()
    {
        SetupUIListeners();
        ApplyAllSettings();
        UpdateUI();
        
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void SetupUIListeners()
    {
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        if (teleportingButton != null)
            teleportingButton.onClick.AddListener(() => UpdateMovementType("teleporting"));
        
        if (continuousButton != null)
            continuousButton.onClick.AddListener(() => UpdateMovementType("continuous"));

        if (speed075Button != null)
            speed075Button.onClick.AddListener(() => UpdateMovementSpeed(0.75f));
        
        if (speed100Button != null)
            speed100Button.onClick.AddListener(() => UpdateMovementSpeed(1.0f));
        
        if (speed125Button != null)
            speed125Button.onClick.AddListener(() => UpdateMovementSpeed(1.25f));

        if (dayButton != null)
            dayButton.onClick.AddListener(() => UpdateTimeOfDay("day"));
        
        if (nightButton != null)
            nightButton.onClick.AddListener(() => UpdateTimeOfDay("night"));

        if (backgroundVolumeSlider != null)
            backgroundVolumeSlider.onValueChanged.AddListener(UpdateBackgroundVolume);
        
        if (narrationVolumeSlider != null)
            narrationVolumeSlider.onValueChanged.AddListener(UpdateNarrationVolume);
        
        if (soundFXVolumeSlider != null)
            soundFXVolumeSlider.onValueChanged.AddListener(UpdateSoundFXVolume);
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(settingsFilePath, json);
            Debug.Log($"Settings saved to: {settingsFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving settings: {e.Message}");
        }
    }

    public void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                settings = JsonUtility.FromJson<SettingsData>(json);
                Debug.Log("Settings loaded successfully");
            }
            else
            {
                Debug.Log("No settings file found, using defaults");
                settings = new SettingsData();
                SaveSettings();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading settings: {e.Message}");
            settings = new SettingsData();
        }
    }

    public void UpdateMovementType(string movementType)
    {
        settings.movementType = movementType;
        ApplyMovementType();
        SaveSettings();
        UpdateUI();
    }

    private void ApplyMovementType()
    {
        Debug.Log($"Movement type: {settings.movementType}");
        // TODO: Connect to VR locomotion
    }

    public void UpdateMovementSpeed(float speed)
    {
        settings.movementSpeed = speed;
        ApplyMovementSpeed();
        SaveSettings();
        UpdateUI();
    }

    private void ApplyMovementSpeed()
    {
        Debug.Log($"Movement speed: {settings.movementSpeed}x");
        // TODO: Connect to movement controller
    }

    public void UpdateTimeOfDay(string timeOfDay)
    {
        settings.timeOfDay = timeOfDay;
        ApplyTimeOfDay();
        SaveSettings();
        UpdateUI();
    }

    private void ApplyTimeOfDay()
    {
        if (settings.timeOfDay == "night")
        {
            if (directionalLight != null)
            {
                directionalLight.intensity = 0.3f;
                directionalLight.color = new Color(0.6f, 0.7f, 1f);
            }
            RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.2f);
        }
        else
        {
            if (directionalLight != null)
            {
                directionalLight.intensity = 1.0f;
                directionalLight.color = Color.white;
            }
            RenderSettings.fogColor = new Color(0.5f, 0.6f, 0.7f);
        }
    }

    public void UpdateBackgroundVolume(float volume)
    {
        settings.soundVolume.background = volume;
        ApplyBackgroundVolume();
        SaveSettings();
        
        if (backgroundVolumeText != null)
            backgroundVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
    }

    private void ApplyBackgroundVolume()
    {
        Debug.Log($"Background volume: {settings.soundVolume.background * 100}%");
        // TODO: Connect to audio
    }

    public void UpdateNarrationVolume(float volume)
    {
        settings.soundVolume.narration = volume;
        ApplyNarrationVolume();
        SaveSettings();
        
        if (narrationVolumeText != null)
            narrationVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
    }

    private void ApplyNarrationVolume()
    {
        Debug.Log($"Narration volume: {settings.soundVolume.narration * 100}%");
        // TODO: Connect to audio
    }

    public void UpdateSoundFXVolume(float volume)
    {
        settings.soundVolume.soundFX = volume;
        ApplySoundFXVolume();
        SaveSettings();
        
        if (soundFXVolumeText != null)
            soundFXVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
    }

    private void ApplySoundFXVolume()
    {
        Debug.Log($"Sound FX volume: {settings.soundVolume.soundFX * 100}%");
        // TODO: Connect to audio
    }

    private void ApplyAllSettings()
    {
        ApplyMovementType();
        ApplyMovementSpeed();
        ApplyTimeOfDay();
        ApplyBackgroundVolume();
        ApplyNarrationVolume();
        ApplySoundFXVolume();
    }

    private void UpdateUI()
    {
        UpdateButtonHighlight(teleportingButton, settings.movementType == "teleporting");
        UpdateButtonHighlight(continuousButton, settings.movementType == "continuous");

        UpdateButtonHighlight(speed075Button, Mathf.Approximately(settings.movementSpeed, 0.75f));
        UpdateButtonHighlight(speed100Button, Mathf.Approximately(settings.movementSpeed, 1.0f));
        UpdateButtonHighlight(speed125Button, Mathf.Approximately(settings.movementSpeed, 1.25f));

        UpdateButtonHighlight(dayButton, settings.timeOfDay == "day");
        UpdateButtonHighlight(nightButton, settings.timeOfDay == "night");

        if (backgroundVolumeSlider != null)
        {
            backgroundVolumeSlider.value = settings.soundVolume.background;
            if (backgroundVolumeText != null)
                backgroundVolumeText.text = $"{Mathf.RoundToInt(settings.soundVolume.background * 100)}%";
        }

        if (narrationVolumeSlider != null)
        {
            narrationVolumeSlider.value = settings.soundVolume.narration;
            if (narrationVolumeText != null)
                narrationVolumeText.text = $"{Mathf.RoundToInt(settings.soundVolume.narration * 100)}%";
        }

        if (soundFXVolumeSlider != null)
        {
            soundFXVolumeSlider.value = settings.soundVolume.soundFX;
            if (soundFXVolumeText != null)
                soundFXVolumeText.text = $"{Mathf.RoundToInt(settings.soundVolume.soundFX * 100)}%";
        }
    }

    private void UpdateButtonHighlight(Button button, bool isActive)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = isActive ? new Color(0.2f, 0.5f, 1f) : new Color(0.3f, 0.3f, 0.3f);
        button.colors = colors;
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public SettingsData GetSettings()
    {
        return settings;
    }
}