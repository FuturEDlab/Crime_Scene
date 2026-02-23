using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuRevert : MonoBehaviour
{
    // Existing function you already use for Back:
    public void LoadMenuScene()
    {
        SceneManager.LoadScene("MenuScene");
    }

    // NEW: for your Skip button in TutorialScene
    public void LoadExperienceScene()
    {
        SceneManager.LoadScene("ExperienceScene");
    }

    // (Optional) if you also want to go back to tutorial sometimes:
    public void LoadTutorialScene()
    {
        SceneManager.LoadScene("TutorialScene");
    }
}
