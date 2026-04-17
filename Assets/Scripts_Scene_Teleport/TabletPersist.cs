// Script is for Tablet not to be destroyed when teleporting between scenes

using UnityEngine;

public class TabletPersist : MonoBehaviour
{
    private static TabletPersist instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}