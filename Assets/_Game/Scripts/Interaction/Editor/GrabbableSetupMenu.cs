using UnityEditor;
using UnityEngine;

public static class GrabbableSetupMenu
{
    private const string MenuRoot = "GameObject/Crime Scene/";

    [MenuItem(MenuRoot + "Make Grabbable", false, 10)]
    private static void MakeSelectedGrabbable(MenuCommand command)
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("[GrabbableSetup] Select one or more objects first.");
            return;
        }

        foreach (GameObject go in Selection.gameObjects)
        {
            GrabbableSetupUtility.Apply(go);
            EnsureSetupComponent(go);
        }
    }

    [MenuItem(MenuRoot + "Make Grabbable", true)]
    private static bool MakeSelectedGrabbableValidate()
    {
        return Selection.gameObjects.Length > 0;
    }

    [MenuItem(MenuRoot + "Create Grabbable Cube", false, 11)]
    private static void CreateGrabbableCube(MenuCommand command)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Grabbable Cube";
        cube.transform.position = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.pivot
            : Vector3.zero;

        GrabbableSetupUtility.Apply(cube);
        EnsureSetupComponent(cube);

        GameObjectUtility.SetParentAndAlign(cube, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(cube, "Create Grabbable Cube");
        Selection.activeGameObject = cube;
    }

    private static void EnsureSetupComponent(GameObject go)
    {
        if (go.GetComponent<GrabbableObjectSetup>() == null)
            go.AddComponent<GrabbableObjectSetup>();
    }
}
