using UnityEngine;

public class SceneRigAutoDestroy : MonoBehaviour
{
    private void Awake()
    {
        PlayerPersist[] rigs = FindObjectsByType<PlayerPersist>(FindObjectsSortMode.None);

        if (rigs.Length > 1)
        {
            foreach (var rig in rigs)
            {
                if (rig.gameObject.scene.IsValid()) // loaded from scene, not DontDestroyOnLoad survivor
                {
                    Destroy(rig.gameObject);
                    return;
                }
            }
        }
    }
}