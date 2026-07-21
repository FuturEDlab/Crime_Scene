// Script is for Tablet not to be destroyed when teleporting between scenes

using UnityEngine;

public class TabletPersist : MonoBehaviour
{
    public static TabletPersist Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
