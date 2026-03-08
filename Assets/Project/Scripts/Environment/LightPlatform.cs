using UnityEngine;

// A platform that reacts to the player's light.
// Always dynamic rigidbody — physics handles everything naturally.
// High mass so the player can push it but not sink it.
// In darkness: gravity 0, floats in place. Player pushes it around.
// In light: LightAffected gives it gravity, it falls.
// Never destroys.
//
// Setup:
// 1. Create a GameObject with a SpriteRenderer
// 2. Add Rigidbody2D (Dynamic, Gravity Scale 0, Mass 50, Freeze Rotation Z)
// 3. Add BoxCollider2D (NOT a trigger)
// 4. Add LightAffected
// 5. Add LightPlatform
// 6. Set Layer to "Ground"
// 7. Add PhysicsMaterial2D with Friction 0.5, Bounciness 0

public class LightPlatform : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Behavior")]
    [SerializeField] private bool returnsWhenDark = false;
    [SerializeField] private float returnSpeed = 2f;

    // === INTERNAL ===

    private Vector3 startPosition;
    private Rigidbody2D rb;
    private bool isFalling = false;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        // Lock rotation only. Keep it dynamic so physics handles collisions.
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // High mass = player can push it slowly but can't sink it by standing on it.
        rb.mass = 5f;

        // High linear drag so it doesn't slide forever when pushed.
        rb.linearDamping = 1f;
    }

    private void FixedUpdate()
    {
        bool isLit = rb.gravityScale > 0f;

        if (isLit)
        {
            isFalling = true;
        }

        // When in darkness and not lit, keep vertical velocity at zero
        // so the player's weight doesn't push it down.
        if (!isLit)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // Also lock vertical position to prevent slow drift.
            if (!isFalling)
            {
                Vector3 pos = transform.position;
                pos.y = startPosition.y;
                transform.position = pos;
            }
        }

        // Return to start when back in darkness.
        if (returnsWhenDark && !isLit && isFalling)
        {
            ReturnToStart();
        }

        // When unlit after falling, lock in place at current Y.
        if (!isLit && isFalling && !returnsWhenDark)
        {
            rb.linearVelocity = Vector2.zero;
            startPosition = transform.position;
            isFalling = false;
        }
    }

    // === PRIVATE METHODS ===

    private void ReturnToStart()
    {
        rb.linearVelocity = Vector2.zero;

        transform.position = Vector3.MoveTowards(
            transform.position,
            startPosition,
            returnSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, startPosition) < 0.05f)
        {
            transform.position = startPosition;
            isFalling = false;
        }
    }
}