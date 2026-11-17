// Props every BNG door (DoorWithHandle) open for good and removes all door
// interaction, so the player simply walks through doorways.
//
// Run it from  Tools > Crime Scene > Prop All Doors Open  (Edit mode). It opens
// CrimeScene_HouseShell (the doors) and CrimeScene_Gameplay (the front-door
// teleports) additively if they aren't open yet.
//
// Per door:
//   - removes the whole BNG grab/hinge system (DoorHelper, Grabbable, handle
//     helpers, grip points, joints) and any GazeDoorExclude; rigidbodies stay
//     but become kinematic, colliders stay so the panel is still solid;
//   - swings the panel 90 degrees around its real hinge, angled into the house
//     (the side whose open panel ends nearer the centre of the house shell).
//     If that side would push the panel into a wall, the other side is used.
// Also deactivates the front door's SceneTeleportTrigger objects: with the
// door open the player walks in and out instead of pressing B to teleport.
//
// The result is baked into the scenes: check it in the Scene view, rotate any
// door by hand if it should sit differently, then save. Ctrl+Z undoes it all.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using BNG;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PropDoorsOpenTool
{
    private const string Title = "Prop All Doors Open";
    private const float OpenAngle = 90f;

    private static readonly string[] ScenePaths =
    {
        "Assets/_Game/Scenes/Production/CrimeScene/CrimeScene_HouseShell.unity",
        "Assets/_Game/Scenes/Production/CrimeScene/CrimeScene_Gameplay.unity",
    };

    [MenuItem("Tools/Crime Scene/Prop All Doors Open")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(Title, "Exit Play mode first — this edits the scenes.", "OK");
            return;
        }

        foreach (string path in ScenePaths)
            if (!SceneManager.GetSceneByPath(path).isLoaded)
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        Physics.SyncTransforms(); // edit-mode overlap tests need current poses
        HouseCentres.Clear();

        Undo.SetCurrentGroupName(Title);
        int undoGroup = Undo.GetCurrentGroup();

        var report = new StringBuilder();
        var touched = new HashSet<Scene>();

        DoorHelper[] doors = Object.FindObjectsByType<DoorHelper>(FindObjectsInactive.Include);
        foreach (DoorHelper door in doors)
        {
            // PropOpen destroys the DoorHelper itself, so read the scene first.
            touched.Add(door.gameObject.scene);
            PropOpen(door, report);
        }

        int teleports = 0;
        foreach (SceneTeleportTrigger trigger in Object.FindObjectsByType<SceneTeleportTrigger>(FindObjectsInactive.Include))
        {
            if (!trigger.gameObject.activeSelf)
                continue;
            Undo.RecordObject(trigger.gameObject, Title);
            trigger.gameObject.SetActive(false);
            touched.Add(trigger.gameObject.scene);
            report.AppendLine($"  teleport '{trigger.name}' deactivated");
            teleports++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        foreach (Scene scene in touched)
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[{Title}] {doors.Length} door(s) propped open, {teleports} teleport trigger(s) deactivated.\n{report}");

        if (doors.Length == 0 && teleports == 0)
        {
            EditorUtility.DisplayDialog(Title, "Nothing to do: no interactive doors or active teleport triggers left.", "OK");
            return;
        }

        EditorUtility.DisplayDialog(Title,
            $"{doors.Length} door(s) propped open, {teleports} teleport trigger(s) deactivated.\n\n" +
            "Check the doors in the Scene view (details in the Console), rotate any by hand if needed, then save.", "OK");
        EditorSceneManager.SaveModifiedScenesIfUserWantsTo(touched.ToArray());
    }

    private static void PropOpen(DoorHelper helper, StringBuilder report)
    {
        Transform door = helper.transform;
        Transform assembly = door.parent != null ? door.parent : door;

        // Real hinge line (captured before the joint is removed).
        HingeJoint hinge = helper.GetComponent<HingeJoint>();
        Vector3 anchor = hinge != null ? door.TransformPoint(hinge.anchor) : door.position;
        Vector3 axis = hinge != null ? door.TransformDirection(hinge.axis).normalized : Vector3.up;

        float sign = ChooseSide(door, assembly, anchor, axis, out string reason);

        StripInteraction(assembly);

        Undo.RecordObject(door, Title);
        Quaternion delta = Quaternion.AngleAxis(sign * OpenAngle, axis);
        door.SetPositionAndRotation(anchor + delta * (door.position - anchor), delta * door.rotation);

        report.AppendLine($"  {door.gameObject.scene.name}/{assembly.name}: opened {sign * OpenAngle:+0;-0}° ({reason})");
    }

    // Into the house = the swing whose open panel ends nearer the house centre,
    // unless only the other swing is clear of walls.
    private static float ChooseSide(Transform door, Transform assembly, Vector3 anchor, Vector3 axis, out string reason)
    {
        Vector3 panelCentre = Bounds(door.GetComponentsInChildren<Renderer>(true), door.position).center;
        Vector3 houseCentre = HouseCentre(door.gameObject.scene);
        Vector3 fromHinge = panelCentre - anchor;

        float DistanceAfter(float s) =>
            ((anchor + Quaternion.AngleAxis(s * OpenAngle, axis) * fromHinge) - houseCentre).sqrMagnitude;

        float preferred = DistanceAfter(1f) <= DistanceAfter(-1f) ? 1f : -1f;
        bool preferredBlocked = HitsWall(door, assembly, anchor, axis, preferred);
        bool otherBlocked = HitsWall(door, assembly, anchor, axis, -preferred);

        if (preferredBlocked && !otherBlocked)
        {
            reason = "into the house would hit a wall, opened the other way";
            return -preferred;
        }

        reason = preferredBlocked ? "into the house; WARNING both sides touch geometry, check it" : "into the house";
        return preferred;
    }

    private static readonly Dictionary<Scene, Vector3> HouseCentres = new();

    private static Vector3 HouseCentre(Scene scene)
    {
        if (HouseCentres.TryGetValue(scene, out Vector3 centre))
            return centre;

        var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true));
        centre = Bounds(renderers, Vector3.zero).center;
        HouseCentres[scene] = centre;
        return centre;
    }

    // Would the door's box colliders overlap anything (walls, furniture) once
    // swung by sign * OpenAngle? Boxes are shrunk a little so touching the
    // frame or floor doesn't count.
    private static bool HitsWall(Transform door, Transform assembly, Vector3 anchor, Vector3 axis, float sign)
    {
        Quaternion delta = Quaternion.AngleAxis(sign * OpenAngle, axis);
        foreach (BoxCollider box in door.GetComponentsInChildren<BoxCollider>(true))
        {
            if (box.isTrigger)
                continue;

            Transform t = box.transform;
            Vector3 centre = anchor + delta * (t.TransformPoint(box.center) - anchor);
            Vector3 half = Vector3.Scale(box.size, t.lossyScale) * 0.5f * 0.85f;
            half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));

            foreach (Collider hit in Physics.OverlapBox(centre, half, delta * t.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(assembly))
                    continue;
                if (hit.GetComponentInParent<BNGPlayerController>() != null)
                    continue;
                return true;
            }
        }
        return false;
    }

    // Removes every BNG behaviour, joint and leftover door opt-out on the
    // assembly. Components another component depends on ([RequireComponent])
    // are removed in a later pass, after their dependents.
    private static void StripInteraction(Transform assembly)
    {
        var doomed = new List<Component>();
        foreach (MonoBehaviour behaviour in assembly.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
                continue;
            if (behaviour.GetType().Namespace == "BNG" || behaviour is GazeDoorExclude)
                doomed.Add(behaviour);
        }
        doomed.AddRange(assembly.GetComponentsInChildren<Joint>(true));

        for (int pass = 0; pass < 8 && doomed.Count > 0; pass++)
        {
            for (int i = doomed.Count - 1; i >= 0; i--)
            {
                Component c = doomed[i];
                if (c != null && IsRequiredByOther(c))
                    continue;
                if (c != null)
                    Undo.DestroyObjectImmediate(c);
                doomed.RemoveAt(i);
            }
        }
        foreach (Component left in doomed)
            Debug.LogWarning($"[{Title}] Could not remove {left.GetType().Name} on '{left.name}'.", left);

        foreach (Rigidbody rb in assembly.GetComponentsInChildren<Rigidbody>(true))
        {
            Undo.RecordObject(rb, Title);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // required before kinematic
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }

    private static bool IsRequiredByOther(Component target)
    {
        System.Type type = target.GetType();
        foreach (Component other in target.GetComponents<Component>())
        {
            if (other == null || other == target)
                continue;
            foreach (RequireComponent req in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
            {
                if ((req.m_Type0 != null && req.m_Type0.IsAssignableFrom(type)) ||
                    (req.m_Type1 != null && req.m_Type1.IsAssignableFrom(type)) ||
                    (req.m_Type2 != null && req.m_Type2.IsAssignableFrom(type)))
                    return true;
            }
        }
        return false;
    }

    private static Bounds Bounds(IEnumerable<Renderer> renderers, Vector3 fallback)
    {
        Bounds? bounds = null;
        foreach (Renderer r in renderers)
        {
            if (bounds == null) bounds = r.bounds;
            else { Bounds b = bounds.Value; b.Encapsulate(r.bounds); bounds = b; }
        }
        return bounds ?? new Bounds(fallback, Vector3.zero);
    }
}
