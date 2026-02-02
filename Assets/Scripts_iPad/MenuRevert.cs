using UnityEngine;
using UnityEngine.SceneManagement;  // Required for scene management

public class MenuRevert : MonoBehaviour
{
    public void LoadMenuScene()
    {
        // Load the Experience scene
        SceneManager.LoadScene("MenuScene");
    }
}
