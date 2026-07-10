// Gaze + button doors.
//
// Design: physics doors are awkward in VR (must be grabbed, swing shut on their
// own, and grabbing the handle triggers a hand-scaling bug). Instead, every BNG
// door starts CLOSED and non-grabbable. When the player looks at a door within
// range, a small world-space prompt appears on it ("Press A to open/close");
// pressing the tablet-recall button toggles the door: closed doors swing open
// (away from the player), open doors swing shut. While a door is targeted the
// tablet recall is suppressed so the same press doesn't also summon the iPad.
//
// Everything here is a pure code bootstrap — no scene setup or prefab wiring.
// Per-door tweaks: DoorPropAngleOverride (angle/direction), GazeDoorExclude
// (keep the original BNG door).

using System.Collections;
using BNG;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ---------------------------------------------------------------- bootstrap

public static class GazeDoorBootstrap
{
    private const float DefaultOpenAngle = 90f;

    private static bool _hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        Convert(); // first scene is already loaded when this runs

        // Guarded so "Enter Play Mode without domain reload" doesn't stack a new
        // subscription on every play session.
        if (!_hooked)
        {
            _hooked = true;
            SceneManager.sceneLoaded += (_, _) => Convert();
        }
    }

    private static void Convert()
    {
        DoorHelper[] doors = Object.FindObjectsByType<DoorHelper>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (DoorHelper door in doors)
            ConvertDoor(door);

        if (doors.Length > 0)
        {
            DoorGazeManager.EnsureExists();
            Debug.Log($"[GazeDoors] Converted {doors.Length} door(s): closed, ungrabbable, gaze+button to open/close.");
        }
    }

    private static void ConvertDoor(DoorHelper helper)
    {
        Transform doorTransform = helper.transform;

        // The whole DoorWithHandle assembly (frame, door, handle objects).
        Transform assembly = doorTransform.parent != null ? doorTransform.parent : doorTransform;

        // Per-door opt-out: leave this door as an original BNG physics door.
        if (assembly.GetComponentInChildren<GazeDoorExclude>(true) != null)
        {
            Debug.Log($"[GazeDoors] '{assembly.name}' excluded — keeping original BNG door behaviour.");
            return;
        }

        // An explicit per-door override fixes both magnitude AND direction;
        // otherwise the swing direction is chosen at open time (away from the
        // player) and only the magnitude comes from the default.
        float openAngle = DefaultOpenAngle;
        DoorPropAngleOverride angleOverride = assembly.GetComponentInChildren<DoorPropAngleOverride>(true);
        bool hasForcedAngle = angleOverride != null;
        if (hasForcedAngle)
            openAngle = angleOverride.openAngle;

        // Stop DoorHelper.Update immediately — it fights kinematic state.
        helper.enabled = false;

        // Capture the real hinge before removing it so the swing animation
        // rotates around the correct edge.
        Vector3 localAnchor = Vector3.zero;
        Vector3 localAxis = Vector3.up;
        HingeJoint hinge = helper.GetComponent<HingeJoint>();
        if (hinge != null)
        {
            localAnchor = hinge.anchor;
            localAxis = hinge.axis;
        }

        // Freeze the door and handle in place. Speculative detection first:
        // Unity warns if a kinematic body keeps continuous mode.
        foreach (Rigidbody rb in assembly.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;

            // CRITICAL: interpolation makes the renderer follow the PHYSICS pose,
            // which stomps direct transform rotation every frame — the door only
            // shows a fraction of its swing. The swing drives the transform, so
            // interpolation must be off.
            rb.interpolation = RigidbodyInterpolation.None;
        }

        // Strip the ENTIRE old grab system: every joint (hinge, the handle's
        // fixed joint) and every BNG behaviour on the door assembly — Grabbable,
        // HandleHelper, DoorHelper, grip points, handle GFX/follow helpers, etc.
        // Meshes and colliders stay (the door must still block and be gazeable);
        // our own GazeDoor is the only behaviour left. Disable before Destroy so
        // no Update slips in during the remainder of this frame.
        foreach (Joint joint in assembly.GetComponentsInChildren<Joint>(true))
            Object.Destroy(joint);

        foreach (MonoBehaviour behaviour in assembly.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour is GazeDoor)
                continue;

            if (behaviour.GetType().Namespace == "BNG")
            {
                behaviour.enabled = false;
                Object.Destroy(behaviour);
            }
        }

        // GazeDoor sits on the assembly root so a raycast hitting ANY collider
        // of the door (panel, frame, handle) resolves to it via GetComponentInParent.
        if (assembly.GetComponent<GazeDoor>() == null)
        {
            GazeDoor gazeDoor = assembly.gameObject.AddComponent<GazeDoor>();
            gazeDoor.Configure(doorTransform, localAnchor, localAxis, openAngle, hasForcedAngle);
        }
    }
}

// ---------------------------------------------------------- per-door swing

public class GazeDoor : MonoBehaviour
{
    private const float SwingSeconds = 1.1f; // full 90° swing duration

    private Transform _door;
    private float _openAngle;
    private bool _forcedDirection;

    // Closed pose + hinge line captured ONCE while the door is closed; every
    // swing pose is computed absolutely from these, so opening and closing are
    // exact inverses and nothing can accumulate drift.
    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private Vector3 _worldAnchor;
    private Vector3 _worldAxis;
    private Vector3 _promptPosition;

    private float _currentAngle;
    private Coroutine _swing;

    public bool IsOpen { get; private set; }
    public bool IsAnimating => _swing != null;

    // World position the "press to open/close" prompt should hover at.
    public Vector3 PromptPosition => _promptPosition;

    public void Configure(Transform door, Vector3 localAnchor, Vector3 localAxis, float openAngle, bool forcedDirection)
    {
        _door = door;
        _openAngle = openAngle;
        _forcedDirection = forcedDirection;

        _closedPosition = door.position;
        _closedRotation = door.rotation;
        _worldAnchor = door.TransformPoint(localAnchor);
        _worldAxis = door.TransformDirection(localAxis).normalized;

        // Centre of the door panel's renderers = a natural anchor for the prompt.
        Renderer[] renderers = door.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            _promptPosition = bounds.center;
        }
        else
        {
            _promptPosition = door.position;
        }
    }

    // Open a closed door (swinging away from the opener) or close an open one.
    public void Toggle(Vector3 openerPosition)
    {
        if (_door == null || IsAnimating)
            return;

        if (IsOpen)
        {
            IsOpen = false;
            _swing = StartCoroutine(SwingTo(0f));
        }
        else
        {
            if (!_forcedDirection)
                _openAngle = Mathf.Abs(_openAngle) * PickAwaySign(openerPosition);
            IsOpen = true;
            _swing = StartCoroutine(SwingTo(_openAngle));
        }
    }

    // Simulates the door centre's final position for both swing directions and
    // returns the sign that ends up FARTHER from the player.
    private float PickAwaySign(Vector3 openerPosition)
    {
        Vector3 fromHinge = _promptPosition - _worldAnchor;
        float magnitude = Mathf.Abs(_openAngle);

        Vector3 endPositive = _worldAnchor + Quaternion.AngleAxis(magnitude, _worldAxis) * fromHinge;
        Vector3 endNegative = _worldAnchor + Quaternion.AngleAxis(-magnitude, _worldAxis) * fromHinge;

        float distPositive = (endPositive - openerPosition).sqrMagnitude;
        float distNegative = (endNegative - openerPosition).sqrMagnitude;
        return distPositive >= distNegative ? 1f : -1f;
    }

    private IEnumerator SwingTo(float targetAngle)
    {
        float startAngle = _currentAngle;
        float span = Mathf.Abs(targetAngle - startAngle);
        float duration = Mathf.Max(0.25f, SwingSeconds * span / 90f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t); // ease-out
            SetSwing(Mathf.Lerp(startAngle, targetAngle, eased));
            yield return null;
        }

        SetSwing(targetAngle);
        Physics.SyncTransforms();
        _swing = null;
    }

    // Places the door at exactly `angle` degrees around the hinge, measured from
    // its closed pose.
    private void SetSwing(float angle)
    {
        Quaternion delta = Quaternion.AngleAxis(angle, _worldAxis);
        _door.SetPositionAndRotation(
            _worldAnchor + delta * (_closedPosition - _worldAnchor),
            delta * _closedRotation);
        _currentAngle = angle;
    }
}

// -------------------------------------------- gaze detection + prompt + input

public class DoorGazeManager : MonoBehaviour
{
    private const float MaxGazeDistance = 3f;

    private static DoorGazeManager _instance;

    private GazeDoor _target;
    private Transform _prompt;
    private TextMeshProUGUI _promptText;
    private string _buttonName;
    private readonly RaycastHit[] _hits = new RaycastHit[16];

    public static void EnsureExists()
    {
        if (_instance != null)
            return;

        var go = new GameObject("DoorGazeManager");
        _instance = go.AddComponent<DoorGazeManager>();
        Object.DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        BuildPrompt();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        TabletRecallInScene recall = TabletRecallInScene.Instance;
        GazeDoor door = FindGazedDoor();

        // Claim or release the shared "bring iPad" button.
        if (recall != null)
            recall.SuppressRecall = door != null;

        SetTarget(door);

        if (door != null && recall != null && recall.ToggleAction != null &&
            recall.ToggleAction.WasPerformedThisFrame())
        {
            Camera cam = Camera.main;
            door.Toggle(cam != null ? cam.transform.position : door.PromptPosition);
        }
    }

    // Raycast from the centre of the player's view. Uses RaycastAll and skips
    // the player's own body/hands and the held tablet — those colliders sit
    // right at the camera and made the prompt FLICKER by intermittently
    // stealing the single closest-hit raycast.
    private GazeDoor FindGazedDoor()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return null;

        int count = Physics.RaycastNonAlloc(cam.transform.position, cam.transform.forward,
            _hits, MaxGazeDistance, ~0, QueryTriggerInteraction.Ignore);

        // Nearest legitimate hit decides (sorting the few hits by distance).
        GazeDoor best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];
            if (hit.distance >= bestDistance)
                continue;

            // Ignore the player rig (body, hands, controllers) and the tablet.
            if (hit.collider.GetComponentInParent<BNGPlayerController>() != null)
                continue;
            if (hit.collider.GetComponentInParent<TabletPersist>() != null)
                continue;

            bestDistance = hit.distance;
            GazeDoor door = hit.collider.GetComponentInParent<GazeDoor>();
            best = (door != null && !door.IsAnimating) ? door : null; // walls block gaze
        }

        return best;
    }

    private void SetTarget(GazeDoor door)
    {
        _target = door;

        if (_prompt == null)
            return;

        bool show = _target != null;
        if (_prompt.gameObject.activeSelf != show)
            _prompt.gameObject.SetActive(show);

        if (!show)
            return;

        // The prompt reads open/close from the door's current state.
        _promptText.text = $"Press {GetButtonName()} to {(_target.IsOpen ? "close" : "open")}";

        // The door centre sits INSIDE the panel mesh, so pull the prompt toward
        // the player — it always floats in front of the door, never clipping
        // through the wood, and stays fully visible while billboarding.
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 toPlayer = (cam.transform.position - _target.PromptPosition).normalized;
            _prompt.position = _target.PromptPosition + toPlayer * 0.35f;
            _prompt.rotation = Quaternion.LookRotation(_prompt.position - cam.transform.position);
        }
        else
        {
            _prompt.position = _target.PromptPosition;
        }
    }

    // Builds a small world-space "Press A to open" label entirely from code.
    private void BuildPrompt()
    {
        int uiLayer = LayerMask.NameToLayer("UI"); // excluded from iPad photos

        var root = new GameObject("DoorPrompt", typeof(RectTransform), typeof(Canvas));
        root.layer = uiLayer >= 0 ? uiLayer : 0;
        root.transform.SetParent(transform, false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(340f, 90f);
        rootRect.localScale = Vector3.one * 0.0012f; // ~0.4m wide in the world

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.layer = root.layer;
        bg.transform.SetParent(root.transform, false);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = root.layer;
        textGo.transform.SetParent(root.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        _promptText = textGo.AddComponent<TextMeshProUGUI>();
        _promptText.fontSize = 42f;
        _promptText.color = Color.white;
        _promptText.alignment = TextAlignmentOptions.Center;
        _promptText.text = $"Press {GetButtonName()} to open";

        _prompt = root.transform;
        _prompt.gameObject.SetActive(false);
    }

    // Human-readable name of the tablet-recall binding ("A" on the headset).
    private string GetButtonName()
    {
        if (!string.IsNullOrEmpty(_buttonName))
            return _buttonName;

        _buttonName = "A";
        TabletRecallInScene recall = TabletRecallInScene.Instance;
        if (recall != null && recall.ToggleAction != null)
        {
            try
            {
                string display = recall.ToggleAction.GetBindingDisplayString();
                if (!string.IsNullOrEmpty(display))
                    _buttonName = display;
            }
            catch { /* keep fallback */ }
        }
        return _buttonName;
    }
}
