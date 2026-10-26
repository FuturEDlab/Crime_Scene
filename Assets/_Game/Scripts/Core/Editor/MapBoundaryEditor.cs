// Scene-view handles for MapBoundary: drag a corner to move it along the ground.

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapBoundary))]
public class MapBoundaryEditor : Editor
{
    private void OnSceneGUI()
    {
        MapBoundary boundary = (MapBoundary)target;
        Vector2[] corners = boundary.Corners;
        if (corners == null)
            return;

        Transform t = boundary.transform;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 world = t.TransformPoint(new Vector3(corners[i].x, 0f, corners[i].y));
            float size = HandleUtility.GetHandleSize(world) * 0.12f;

            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.SphereHandleCap);
            Handles.Label(world + Vector3.up * size * 2f, $"Corner {i}");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(boundary, "Move Map Boundary Corner");
                Vector3 local = t.InverseTransformPoint(moved);
                corners[i] = new Vector2(local.x, local.z);
                boundary.Corners = corners;
                EditorUtility.SetDirty(boundary);
            }
        }
    }
}
