using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TabletSceneRebind : MonoBehaviour
{
    [SerializeField] private MonoBehaviour tabletRecallComponent;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(RebindNextFrame());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindNextFrame());
    }

    private IEnumerator RebindNextFrame()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);

        if (tabletRecallComponent == null)
        {
            Debug.LogWarning("TabletSceneRebind: tabletRecallComponent is not assigned.");
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("TabletSceneRebind: Camera.main not found.");
            yield break;
        }

        bool assigned =
            TrySetTransformField(tabletRecallComponent, "headCamera", cam.transform) ||
            TrySetTransformField(tabletRecallComponent, "HeadCamera", cam.transform);

        if (!assigned)
        {
            Debug.LogWarning("TabletSceneRebind: Could not assign Head Camera field.");
        }
    }

    private bool TrySetTransformField(MonoBehaviour target, string fieldName, Transform value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field != null && field.FieldType == typeof(Transform))
        {
            field.SetValue(target, value);
            return true;
        }

        return false;
    }
}