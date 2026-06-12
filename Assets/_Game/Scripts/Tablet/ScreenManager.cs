using Unity.VisualScripting;
using UnityEngine;  // Gives us access to Unity-specific classes like MonoBehaviour, GameObject, etc.


// This script will live on your iPad Canvas object.
// It controls which screen (panel) is visible at any time.
public class ScreenManager : MonoBehaviour
{
    // These fields are references to each screen (panel).
    // They are marked [SerializeField] so you can assign them in the Inspector,
    // but the variables stay private (hidden from other scripts).
    [SerializeField] private GameObject homeScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private GameObject evidenceScreen;
    [SerializeField] private GameObject notebookScreen;
    [SerializeField] private GameObject cameraScreen;

    // Unity automatically calls Start() once when the object becomes active.
    void Start()
    {
        ShowHome();   // Make the Home screen the first screen the user sees.
    }

    // This function hides ALL screens.
    // We call it before turning any single screen on,
    // because only one screen should be visible at a time.
    void HideAll()
    {
        homeScreen.SetActive(false);
        settingsScreen.SetActive(false);
        evidenceScreen.SetActive(false);
        notebookScreen.SetActive(false);
        cameraScreen.SetActive(false);
    }

    // Show the Home screen
    public void ShowHome()
    {
        //if (homeScreen.activeSelf) // If Home is already visible, do nothing
        //    return;
        
        HideAll();               // Turn everything off...
        homeScreen.SetActive(true);  // ...then turn on only the Home screen
    }

    // Show the Settings screen
    public void ShowSettings()
    {
        //var SettingsButton = new Button(() => { Debug.Log("Button clicked"); }) { text = "Click me" };

        HideAll();
        settingsScreen.SetActive(true);
    }

    // Show the Evidence screen
    public void ShowEvidence()
    {
        HideAll();
        evidenceScreen.SetActive(true);
    }

    // Show the Notebook screen
    public void ShowNotebook()
    {
        HideAll();
        notebookScreen.SetActive(true);
    }

    // Show the Camera screen
    public void ShowCamera()
    {
        HideAll();
        cameraScreen.SetActive(true);
    }
    
    
}
