using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    private SceneDestination pending;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Go(SceneDestination destination)
    {
        if (destination == null) return;
        pending = destination;
        SceneManager.LoadScene(destination.sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pending == null) return;

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.SetPositionAndRotation(
                pending.position,
                Quaternion.Euler(pending.eulerRotation)
            );
        }

        pending = null;
    }
}