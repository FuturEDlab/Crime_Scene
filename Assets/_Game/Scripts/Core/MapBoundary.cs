// Invisible border around the playable map, so the player can't walk (or be
// thrown by a tracking jump) off the edge of the world.
//
// The border is a polygon of corners in this object's local XZ plane. At
// runtime every edge becomes a tall box collider plus a grid quad on its inner
// face. The grid is hidden until the player gets close; it then fades in
// around the player's head, so running into the border reads as "this is the
// edge of the map" instead of an unexplained invisible stop.
//
// Edit the corners in the Inspector or drag their handles in the Scene view.
// PlayerFallRespawn (CrimeScene_Main) uses Contains / ClosestPointInside to
// catch players who got past the wall and put them back inside.

using BNG;
using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    public static MapBoundary Instance { get; private set; }

    [Tooltip("Border corners in this object's local XZ plane, in order. The last corner connects back to the first.")]
    [SerializeField] private Vector2[] corners =
    {
        new Vector2(-40f, -51.3f),
        new Vector2(-40f, 63f),
        new Vector2(28.2f, 63f),
        new Vector2(28.2f, -51.3f),
    };

    [Tooltip("Wall height above this object's position (m). Tall enough that nothing gets over it.")]
    [SerializeField] private float wallHeight = 10f;

    [Tooltip("Collider thickness, extending outward from the border line (m).")]
    [SerializeField] private float wallThickness = 1f;

    [Header("Feedback")]
    [Tooltip("Grid material shown on the wall when the player is near (MapBoundary shader).")]
    [SerializeField] private Material feedbackMaterial;

    [Tooltip("The grid starts to fade in when the player's head or body is this close to the border (m).")]
    [SerializeField] private float showDistance = 1.2f;

    private static readonly int BoundaryPointId = Shader.PropertyToID("_MapBoundaryPoint");

    // World-space XZ corners and each edge's inward normal, cached at Awake.
    private Vector2[] worldCorners;
    private Vector2[] inwardNormals;
    private Renderer[] wallRenderers;
    private BNGPlayerController player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[MapBoundary] More than one MapBoundary is loaded; using the newest.");
        Instance = this;

        if (corners == null || corners.Length < 3)
        {
            Debug.LogError("[MapBoundary] Needs at least 3 corners.", this);
            enabled = false;
            return;
        }

        CacheWorldPolygon();
        BuildWalls();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Shader.SetGlobalVector(BoundaryPointId, Vector4.zero);
    }

    private void LateUpdate()
    {
        if (player == null)
            player = FindAnyObjectByType<BNGPlayerController>();

        Camera head = Camera.main;
        if (head == null && player == null)
            return;

        // Closest the head or body gets to each wall; a point outside the
        // border counts as touching it.
        Vector3 headPos = head != null ? head.transform.position : player.transform.position;
        float nearest = float.MaxValue;
        for (int i = 0; i < wallRenderers.Length; i++)
        {
            float d = DistanceToEdge(ToXZ(headPos), i);
            if (player != null)
                d = Mathf.Min(d, DistanceToEdge(ToXZ(player.transform.position), i));

            wallRenderers[i].enabled = d < showDistance;
            nearest = Mathf.Min(nearest, d);
        }

        bool outside = !Contains(headPos) || (player != null && !Contains(player.transform.position));
        float strength = outside ? 1f : Mathf.Clamp01(1f - nearest / showDistance);

        // xyz = where the grid is revealed around, w = overall intensity.
        Shader.SetGlobalVector(BoundaryPointId,
            new Vector4(headPos.x, headPos.y, headPos.z, strength));
    }

    // True when the point is inside the border (XZ only, height is ignored).
    public bool Contains(Vector3 worldPoint)
    {
        return ContainsXZ(ToXZ(worldPoint));
    }

    // Distance from the point to the nearest border edge (XZ only).
    public float DistanceToBorder(Vector3 worldPoint)
    {
        Vector2 p = ToXZ(worldPoint);
        float best = float.MaxValue;
        for (int i = 0; i < worldCorners.Length; i++)
            best = Mathf.Min(best, DistanceToEdge(p, i));
        return best;
    }

    // Returns the point unchanged when it is inside the border and at least
    // `inset` metres from every edge; otherwise moves it to the nearest spot
    // that is. Y is kept.
    public Vector3 ClosestPointInside(Vector3 worldPoint, float inset)
    {
        Vector2 p = ToXZ(worldPoint);

        // Each pass snaps to the nearest edge and steps `inset` inward; a second
        // pass fixes corners, where stepping off one edge can land on another.
        for (int pass = 0; pass < 4; pass++)
        {
            int nearestEdge = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < worldCorners.Length; i++)
            {
                float d = DistanceToEdge(p, i);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    nearestEdge = i;
                }
            }

            if (ContainsXZ(p) && nearestDistance >= inset - 0.001f)
                break;

            p = ClosestPointOnEdge(p, nearestEdge) + inwardNormals[nearestEdge] * inset;
        }

        return new Vector3(p.x, worldPoint.y, p.y);
    }

    private void CacheWorldPolygon()
    {
        int count = corners.Length;
        worldCorners = new Vector2[count];
        for (int i = 0; i < count; i++)
            worldCorners[i] = ToXZ(transform.TransformPoint(new Vector3(corners[i].x, 0f, corners[i].y)));

        // Shoelace sign tells the winding, which decides which side is "in".
        float area = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector2 a = worldCorners[i];
            Vector2 b = worldCorners[(i + 1) % count];
            area += a.x * b.y - b.x * a.y;
        }

        inwardNormals = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 edge = (worldCorners[(i + 1) % count] - worldCorners[i]).normalized;
            Vector2 left = new Vector2(-edge.y, edge.x);
            inwardNormals[i] = area > 0f ? left : -left;
        }
    }

    private void BuildWalls()
    {
        int count = worldCorners.Length;
        wallRenderers = new Renderer[count];
        float baseY = transform.position.y;

        for (int i = 0; i < count; i++)
        {
            Vector2 a = worldCorners[i];
            Vector2 b = worldCorners[(i + 1) % count];
            float length = Vector2.Distance(a, b);
            Vector2 mid = (a + b) * 0.5f;
            Vector3 inward = new Vector3(inwardNormals[i].x, 0f, inwardNormals[i].y);

            // Forward points into the map: the inner face sits on the border
            // line (z = 0) and the collider extends outward (negative z).
            GameObject wall = new GameObject($"BorderWall_{i}");
            wall.layer = gameObject.layer;
            wall.transform.SetParent(transform, false);
            wall.transform.SetPositionAndRotation(
                new Vector3(mid.x, baseY, mid.y), Quaternion.LookRotation(inward, Vector3.up));

            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(length, wallHeight, wallThickness);
            box.center = new Vector3(0f, wallHeight * 0.5f, -wallThickness * 0.5f);

            wall.AddComponent<MeshFilter>().sharedMesh = BuildQuad(length, wallHeight);
            MeshRenderer meshRenderer = wall.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = feedbackMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            meshRenderer.enabled = false;
            wallRenderers[i] = meshRenderer;
        }
    }

    // Vertical quad centred on x, standing on y = 0, a hair inside the border.
    // UVs are in metres so the grid keeps square cells on every wall.
    private static Mesh BuildQuad(float length, float height)
    {
        const float faceOffset = 0.01f;
        float half = length * 0.5f;
        Mesh mesh = new Mesh { name = "BorderWallQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-half, 0f, faceOffset),
            new Vector3(half, 0f, faceOffset),
            new Vector3(half, height, faceOffset),
            new Vector3(-half, height, faceOffset),
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(length, 0f),
            new Vector2(length, height),
            new Vector2(0f, height),
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private bool ContainsXZ(Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = worldCorners.Length - 1; i < worldCorners.Length; j = i++)
        {
            Vector2 a = worldCorners[i];
            Vector2 b = worldCorners[j];
            if ((a.y > p.y) != (b.y > p.y) &&
                p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    private float DistanceToEdge(Vector2 p, int edge)
    {
        return Vector2.Distance(p, ClosestPointOnEdge(p, edge));
    }

    private Vector2 ClosestPointOnEdge(Vector2 p, int edge)
    {
        Vector2 a = worldCorners[edge];
        Vector2 b = worldCorners[(edge + 1) % worldCorners.Length];
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return a + ab * t;
    }

    private static Vector2 ToXZ(Vector3 v) => new Vector2(v.x, v.z);

    // Editor access for MapBoundaryEditor (corner handles).
    public Vector2[] Corners
    {
        get => corners;
        set => corners = value;
    }

    private void OnDrawGizmos()
    {
        if (corners == null || corners.Length < 2)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 a = transform.TransformPoint(new Vector3(corners[i].x, 0f, corners[i].y));
            Vector3 b = transform.TransformPoint(new Vector3(corners[(i + 1) % corners.Length].x, 0f,
                corners[(i + 1) % corners.Length].y));
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(a + Vector3.up * wallHeight, b + Vector3.up * wallHeight);
            Gizmos.DrawLine(a, a + Vector3.up * wallHeight);
        }
    }
}
