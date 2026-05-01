// This script applies a color filter to the camera to simulate colorblind modes.
// It uses OnRenderImage to apply a shader effect at the camera level.
// Modes: None, Red-Green (Deuteranopia), Red-Green (Protanopia), Blue-Yellow (Tritanopia), Grayscale.

using UnityEngine;

public class ColorblindManager : MonoBehaviour
{
    public enum ColorblindMode
    {
        None,
        Deuteranopia,
        Protanopia,
        Tritanopia,
        Grayscale
    }

    public static ColorblindManager Instance { get; private set; }

    [SerializeField] private ColorblindMode currentMode = ColorblindMode.None;
    [SerializeField] private Material colorblindMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ApplyMode(currentMode);
    }

    public void SetMode(ColorblindMode mode)
    {
        currentMode = mode;
        ApplyMode(mode);
        SettingsManager.Instance.SetColorblindMode(mode.ToString());
    }

    private void ApplyMode(ColorblindMode mode)
    {
        if (colorblindMaterial == null) return;

        colorblindMaterial.SetInt("_Mode", (int)mode);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (currentMode == ColorblindMode.None || colorblindMaterial == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        Graphics.Blit(src, dest, colorblindMaterial);
    }
}