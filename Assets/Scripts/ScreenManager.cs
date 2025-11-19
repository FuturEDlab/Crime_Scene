using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    // Drag your panels into these slots in the Inspector
    public GameObject homeScreen;
    public GameObject settingsScreen;
    public GameObject evidenceScreen;
    public GameObject notebookScreen;
    public GameObject cameraScreen;

    void Start()
    {
        ShowHome();   // start with Home screen visible
    }

    void HideAll()
    {
        homeScreen.SetActive(false);
        settingsScreen.SetActive(false);
        evidenceScreen.SetActive(false);
        notebookScreen.SetActive(false);
        cameraScreen.SetActive(false);
    }

    public void ShowHome()
    {
        HideAll();
        homeScreen.SetActive(true);
    }

    public void ShowSettings()
    {
        HideAll();
        settingsScreen.SetActive(true);
    }

    public void ShowEvidence()
    {
        HideAll();
        evidenceScreen.SetActive(true);
    }

    public void ShowNotebook()
    {
        HideAll();
        notebookScreen.SetActive(true);
    }

    public void ShowCamera()
    {
        HideAll();
        cameraScreen.SetActive(true);
    }
}