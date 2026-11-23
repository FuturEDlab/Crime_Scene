// Main_Menu scene: shows one 2000x2000 panel at a time (Main Menu, Tutorial,
// Controls) and runs the main menu buttons. Built and wired by
// Tools > Crime Scene > Build Main Menu Scene (MainMenuSceneBuilder).

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private MainMenuTutorial tutorial;

    [Tooltip("Scene loaded by Start. Must be in the Build Profiles scene list.")]
    [SerializeField] private string crimeSceneName = "CrimeScene_Main";

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu() => Show(mainMenuPanel);

    public void ShowTutorial()
    {
        if (tutorial != null)
            tutorial.ShowFirstPage();
        Show(tutorialPanel);
    }

    public void ShowControls() => Show(controlsPanel);

    // Placeholder: the settings options are still being decided.
    public void ShowSettings()
    {
        Debug.Log("[MainMenu] Settings is not available yet.");
    }

    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(crimeSceneName))
        {
            Debug.LogError($"[MainMenu] Scene \"{crimeSceneName}\" is not in the Build Profiles scene list.");
            return;
        }
        SceneManager.LoadScene(crimeSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Show(GameObject panel)
    {
        SetActive(mainMenuPanel, panel == mainMenuPanel);
        SetActive(tutorialPanel, panel == tutorialPanel);
        SetActive(controlsPanel, panel == controlsPanel);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
