using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrabbableObjectSetup))]
public class GrabbableObjectSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GrabbableObjectSetup setup = (GrabbableObjectSetup)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Grabbable Setup"))
            setup.ApplySetup();

        if (GUILayout.Button("Validate Setup"))
            GrabbableSetupUtility.Validate(setup.gameObject);
    }
}
