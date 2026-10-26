// Loads the crime scene's additive scenes on top of CrimeScene_Main.
//
// CrimeScene_Main only holds global systems (this loader, lighting, the XR rig
// and the tablet). The house shell, every room, the outside and the gameplay
// layer are separate scenes so the modeler and engineers can each own and save
// their own scene without merge conflicts. Every listed scene must be in the
// Build Profiles scene list.
//
// Scenes that are already open (e.g. you opened Main + Kitchen in the editor
// before pressing Play) are skipped, so editing workflows never double-load.

using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class CrimeSceneLoader : MonoBehaviour
{
    [Tooltip("Scene names (not paths) loaded additively when CrimeScene_Main starts.")]
    [SerializeField] private string[] additiveScenes =
    {
        "CrimeScene_HouseShell",
        "CrimeScene_Outside",
        "CrimeScene_Entryway",
        "CrimeScene_LivingRoom",
        "CrimeScene_Kitchen",
        "CrimeScene_DownstairsHallway",
        "CrimeScene_DownstairsCoatCloset",
        "CrimeScene_UnderStairsCloset",
        "CrimeScene_DownstairsBathroom",
        "CrimeScene_StairsUpstairsHallway",
        "CrimeScene_MainBedroom",
        "CrimeScene_UpstairsWalkInCloset",
        "CrimeScene_Bedroom2",
        "CrimeScene_UpstairsBathroom",
        "CrimeScene_UpstairsCloset",
        "CrimeScene_UpstairsOffice",
        "CrimeScene_Gameplay",
    };

    public string[] AdditiveScenes => additiveScenes;

    private void Awake()
    {
        // Main owns lighting/RenderSettings, so it must stay the active scene
        // (the editor may have another open scene marked active).
        Scene main = gameObject.scene;
        if (SceneManager.GetActiveScene() != main)
            SceneManager.SetActiveScene(main);

        foreach (string sceneName in additiveScenes)
        {
            if (string.IsNullOrEmpty(sceneName) || SceneManager.GetSceneByName(sceneName).isLoaded)
                continue;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[CrimeSceneLoader] Scene \"{sceneName}\" is not in the Build Profiles scene list.");
                continue;
            }

            // Synchronous additive load: the scenes are in place by the next
            // frame, before the rigidbody player can fall through missing floors.
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}
