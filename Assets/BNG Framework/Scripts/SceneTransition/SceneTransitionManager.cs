using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    private string pendingSpawnName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void TransitionTo(string sceneName, string spawnPointName)
    {
        pendingSpawnName = spawnPointName;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(pendingSpawnName))
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player with tag 'Player' not found in the loaded scene.");
            pendingSpawnName = null;
            return;
        }

        GameObject spawnObj = GameObject.Find(pendingSpawnName);
        if (spawnObj == null)
        {
            Debug.LogError($"Spawn point '{pendingSpawnName}' not found in scene '{scene.name}'.");
            pendingSpawnName = null;
            return;
        }

        player.transform.SetPositionAndRotation(spawnObj.transform.position, spawnObj.transform.rotation);
        pendingSpawnName = null;
    }
}