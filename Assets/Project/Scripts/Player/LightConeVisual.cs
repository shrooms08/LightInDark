using UnityEngine;

// Draws the visible light cone mesh. Uses raycasting to stop the light
// at walls, platforms, and other solid objects, creating realistic shadows.
//
// How it works:
// - Casts rays in a fan shape across the cone angle
// - Each ray stops at the first solid object it hits
// - The mesh is built from the hit points, so the light shape
//   wraps around obstacles naturally
//
// Attach this to the LightCone child object under Player.
// Requires: MeshFilter, MeshRenderer on the same object.

public class LightConeVisual : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Appearance")]
    [SerializeField] private float coneRange = 5f;
    [SerializeField] private float coneAngle = 30f;
    [SerializeField] private int segments = 30;          // More segments = smoother edges
    [SerializeField] private Color coneColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Occlusion")]
    [SerializeField] private LayerMask blockingLayers;   // What blocks the light (Ground, etc.)

    // === INTERNAL ===

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        // Create a simple unlit material.
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = coneColor;
        meshRenderer.material = mat;
        meshRenderer.sortingOrder = -5;
    }

    private void LateUpdate()
    {
        // Only build the mesh if the renderer is enabled (light is on).
        if (!meshRenderer.enabled)
        {
            return;
        }

        BuildOccludedMesh();
    }

    // === PRIVATE METHODS ===

    private void BuildOccludedMesh()
    {
        // We build a fan of triangles from the origin (player position).
        // Each triangle edge is determined by a raycast.
        // If the ray hits something, the triangle stops at the hit point.
        // If it doesn't hit, the triangle extends to full cone range.

        // +1 for the center vertex, +1 because segments need segment+1 edge points.
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        // Vertex 0 is always the origin (tip of the cone).
        vertices[0] = Vector3.zero;

        // Calculate the angle step between each ray.
        float totalAngle = coneAngle * 2f;    // coneAngle is half-angle
        float angleStep = totalAngle / segments;
        float startAngle = -coneAngle;

        for (int i = 0; i <= segments; i++)
        {
            // Calculate ray direction in local space.
            float currentAngle = startAngle + (angleStep * i);
            float angleRad = currentAngle * Mathf.Deg2Rad;

            // Direction the ray should go (local to the LightCone object).
            Vector2 localDirection = new Vector2(
                Mathf.Cos(angleRad),
                Mathf.Sin(angleRad)
            );

            // Convert to world direction for raycasting.
            // transform.TransformDirection converts from local to world space.
            Vector2 worldDirection = transform.TransformDirection(localDirection);

            // Cast the ray from the player's world position.
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                worldDirection,
                coneRange,
                blockingLayers
            );

            // If the ray hit something, use the hit distance.
            // Otherwise, use full cone range.
            float distance;
            if (hit.collider != null)
            {
                distance = hit.distance;
            }
            else
            {
                distance = coneRange;
            }

            // Set the vertex position in LOCAL space.
            // (The mesh is in local coordinates of the LightCone object.)
            vertices[i + 1] = localDirection * distance;
        }

        // Build triangles. Each triangle connects:
        // vertex 0 (center) → vertex i+1 → vertex i+2
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;           // Center
            triangles[i * 3 + 1] = i + 1;   // Current edge point
            triangles[i * 3 + 2] = i + 2;   // Next edge point
        }

        // Apply to mesh.
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }
}