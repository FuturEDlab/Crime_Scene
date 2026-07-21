using BNG;
using UnityEngine;

// Shared helpers for turning any scene object into a BNG VRIF grabbable.
// Used by the editor menu, the GrabbableObjectSetup component, and validation.
public static class GrabbableSetupUtility
{
    public const string GrabbableLayerName = "Grabbable";

    // Hand trigger radius on each Grabber (default BNG prefab is ~0.05).
    public const float HandGrabRadius = 0.15f;

    // Point-and-grab fallback when the hand bubble is not overlapping yet.
    public const float RemoteGrabDistance = 1.5f;

    private static bool _handGrabRangeBoosted;

    /// <summary>
    /// Ensures the object has everything BNG needs to be picked up by a Grabber:
    /// Grabbable layer, collider, kinematic rigidbody (no physics simulation),
    /// and a Grabbable component with a larger effective grab range.
    /// </summary>
    public static bool Apply(GameObject target, bool logResult = true)
    {
        if (target == null)
            return false;

        SetLayer(target, GrabbableLayerName);

        Collider col = EnsureCollider(target);
        EnsureKinematicRigidbody(target);
        Grabbable grabbable = EnsureGrabbable(target);
        ConfigureGrabbable(grabbable);

        if (Application.isPlaying)
            BoostHandGrabRangeOnce();

        if (logResult)
            Debug.Log($"[GrabbableSetup] '{target.name}' is ready to grab. " +
                      $"Layer={GrabbableLayerName}, Collider={col.GetType().Name}, " +
                      $"kinematic=true, remoteGrab={grabbable.RemoteGrabDistance}m.");

        return Validate(target, logWarnings: false);
    }

    /// <summary>
    /// Slightly enlarges the hand Grabber trigger spheres once per session so
    /// objects are easier to pick up without changing closest-object priority.
    /// </summary>
    public static void BoostHandGrabRangeOnce()
    {
        if (_handGrabRangeBoosted)
            return;

        Grabber[] grabbers =
#if UNITY_2023_1_OR_NEWER
            Object.FindObjectsByType<Grabber>(FindObjectsSortMode.None);
#else
            Object.FindObjectsOfType<Grabber>();
#endif

        foreach (Grabber grabber in grabbers)
        {
            SphereCollider sphere = grabber.GetComponent<SphereCollider>();
            if (sphere != null && sphere.isTrigger)
                sphere.radius = HandGrabRadius;
        }

        _handGrabRangeBoosted = true;
    }

    /// <summary>
    /// Logs warnings when required grabbable pieces are missing or misconfigured.
    /// </summary>
    public static bool Validate(GameObject target, bool logWarnings = true)
    {
        if (target == null)
            return false;

        bool ok = true;
        int grabbableLayer = LayerMask.NameToLayer(GrabbableLayerName);

        if (grabbableLayer >= 0 && target.layer != grabbableLayer)
        {
            if (logWarnings)
                Debug.LogWarning($"[GrabbableSetup] '{target.name}' is not on the " +
                                 $"{GrabbableLayerName} layer (current: {LayerMask.LayerToName(target.layer)}).");
            ok = false;
        }

        Collider col = target.GetComponent<Collider>();
        if (col == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[GrabbableSetup] '{target.name}' needs a Collider on the same " +
                                 "object as the Grabbable component.");
            ok = false;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[GrabbableSetup] '{target.name}' needs a Rigidbody.");
            ok = false;
        }
        else if (!rb.isKinematic && rb.useGravity)
        {
            if (logWarnings)
                Debug.LogWarning($"[GrabbableSetup] '{target.name}' Rigidbody should be kinematic " +
                                 "with gravity off for this project's grabbable setup.");
        }

        if (target.GetComponent<Grabbable>() == null)
        {
            if (logWarnings)
                Debug.LogWarning($"[GrabbableSetup] '{target.name}' needs a BNG Grabbable component.");
            ok = false;
        }

        if (target.transform.localScale != Vector3.one)
        {
            if (logWarnings)
                Debug.LogWarning($"[GrabbableSetup] '{target.name}' has non-uniform scale " +
                                 $"{target.transform.localScale}. BNG recommends scale 1,1,1 on the " +
                                 "root; scale a child mesh instead.");
        }

        return ok;
    }

    private static void SetLayer(GameObject target, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[GrabbableSetup] Layer '{layerName}' does not exist. " +
                             "Add it in Tags & Layers (layer 10 in this project).");
            return;
        }

        target.layer = layer;
    }

    private static Collider EnsureCollider(GameObject target)
    {
        Collider col = target.GetComponent<Collider>();
        if (col != null)
            return col;

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        BoxCollider box = target.AddComponent<BoxCollider>();

        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            box.center = target.transform.InverseTransformPoint(bounds.center);

            Vector3 lossy = target.transform.lossyScale;
            box.size = new Vector3(
                bounds.size.x / Mathf.Max(Mathf.Abs(lossy.x), 0.001f),
                bounds.size.y / Mathf.Max(Mathf.Abs(lossy.y), 0.001f),
                bounds.size.z / Mathf.Max(Mathf.Abs(lossy.z), 0.001f));
        }

        return box;
    }

    // BNG still expects a Rigidbody, but we keep it kinematic with no gravity so
    // the object does not tumble or simulate physics on its own.
    private static Rigidbody EnsureKinematicRigidbody(GameObject target)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
            rb = target.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        return rb;
    }

    private static Grabbable EnsureGrabbable(GameObject target)
    {
        Grabbable grabbable = target.GetComponent<Grabbable>();
        if (grabbable != null)
            return grabbable;

        return target.AddComponent<Grabbable>();
    }

    private static void ConfigureGrabbable(Grabbable grabbable)
    {
        if (grabbable == null)
            return;

        // No velocity/joint physics — object follows the hand kinematically.
        grabbable.GrabPhysics = GrabPhysics.Kinematic;

        // Extra reach via the controller remote-grab ray; closest hand grab still
        // wins when multiple objects are nearby (BNG default behaviour).
        grabbable.RemoteGrabbable = true;
        grabbable.RemoteGrabDistance = RemoteGrabDistance;
    }
}
