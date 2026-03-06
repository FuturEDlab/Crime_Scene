using UnityEngine;

public class SceneTeleportTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointName;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.Go(targetSceneName, targetSpawnPointName);
        }
    }
}