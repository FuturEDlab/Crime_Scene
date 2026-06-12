using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadExperienceScene()
    {
        // Load the Experience scene
        SceneManager.LoadScene("ExperienceScene");
    }

    public void LoadTutorialScene()
    {
        // Load the tutorial scene
        SceneManager.LoadScene("TutorialScene");
    }

    public void LoadSettings()
    {
        // Just print a message for now since there's no functionality for settings
        Debug.Log("Settings Button Clicked");
    }
}
