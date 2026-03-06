using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private string pendingSpawnPointName;

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
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Go(string targetSceneName, string targetSpawnPointName)
    {
        pendingSpawnPointName = targetSpawnPointName;
        SceneManager.LoadScene(targetSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(MovePlayerToSpawn());
    }

    private IEnumerator MovePlayerToSpawn()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);

        GameObject player = GameObject.FindWithTag("Player");
        GameObject spawnPoint = GameObject.Find(pendingSpawnPointName);

        if (player == null)
        {
            Debug.LogWarning("Player with tag 'Player' not found.");
            yield break;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("Spawn point not found: " + pendingSpawnPointName);
            yield break;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.SetPositionAndRotation(
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );

        Physics.SyncTransforms();
    }
}