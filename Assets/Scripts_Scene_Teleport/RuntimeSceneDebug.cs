using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeSceneDebug : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(LogAfterLoad());
    }

    private IEnumerator LogAfterLoad()
    {
        // wait a little so runtime-created objects appear
        yield return null;
        yield return new WaitForSeconds(0.2f);

        Debug.Log("===== RUNTIME DEBUG START =====");

        AudioListener[] listeners = Resources.FindObjectsOfTypeAll<AudioListener>();
        Debug.Log("AudioListeners found: " + listeners.Length);
        foreach (var l in listeners)
        {
            if (l == null) continue;
            Debug.Log("[AudioListener] " + GetFullPath(l.gameObject) + " | activeInHierarchy=" + l.gameObject.activeInHierarchy);
        }

        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
        Debug.Log("Cameras found: " + cameras.Length);
        foreach (var c in cameras)
        {
            if (c == null) continue;
            Debug.Log("[Camera] " + GetFullPath(c.gameObject) + " | activeInHierarchy=" + c.gameObject.activeInHierarchy + " | enabled=" + c.enabled);
        }

        CanvasGroup[] groups = Resources.FindObjectsOfTypeAll<CanvasGroup>();
        foreach (var cg in groups)
        {
            if (cg == null) continue;
            if (cg.gameObject.name.Contains("Fader") || cg.gameObject.name.Contains("Fade") || cg.gameObject.name.Contains("Screen"))
            {
                Debug.Log("[CanvasGroup] " + GetFullPath(cg.gameObject) + " | alpha=" + cg.alpha);
            }
        }

        Debug.Log("===== RUNTIME DEBUG END =====");
    }

    private string GetFullPath(GameObject obj)
    {
        if (obj == null) return "(null)";

        StringBuilder sb = new StringBuilder(obj.name);
        Transform current = obj.transform.parent;

        while (current != null)
        {
            sb.Insert(0, current.name + "/");
            current = current.parent;
        }

        return sb.ToString();
    }
}