using UnityEngine;

// A hazard that floats on the ceiling and falls when lit.
// Destroys itself the moment it touches anything in the destroy layers.
//
// Uses a SECOND trigger collider (a child object) to detect landing.
// This avoids all the collision callback issues.
//
// SIMPLE SETUP:
// 1. Ceiling spike with: SpriteRenderer, Rigidbody2D (Dynamic, Gravity 0),
//    Polygon Collider 2D (trigger, for killing), LightAffected, Hazard
// 2. Add FallingHazard (this script) to the spike
// 3. Set Destroy Layers in the inspector
// That's it. No extra child objects needed.

public class FallingHazard : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Destroy Settings")]
    [Tooltip("Which layers destroy this hazard when it touches them.")]
    [SerializeField] private LayerMask destroyLayers;

    // === INTERNAL ===

    private Rigidbody2D rb;
    private bool isFalling = false;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        // Detect when light gives it gravity.
        if (rb.gravityScale > 0f)
        {
            isFalling = true;
        }

        // While falling, check every physics frame using OverlapBox.
        if (isFalling && rb.linearVelocity.y < -0.1f)
        {
            CheckLanding();
        }
    }

    // === PRIVATE METHODS ===

    private void CheckLanding()
    {
        // Get the collider bounds to know the spike's size and position.
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            return;
        }

        Bounds bounds = col.bounds;

        // Check a small area at the bottom of the spike.
        Vector2 checkPoint = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 checkSize = new Vector2(bounds.size.x * 0.8f, 0.15f);

        Collider2D hit = Physics2D.OverlapBox(
            checkPoint,
            checkSize,
            0f,
            destroyLayers
        );

        if (hit != null)
        {
            Destroy(gameObject);
        }
    }
}