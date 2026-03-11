using UnityEngine;

// The basic enemy of LightInDark.
// In DARKNESS: floats and patrols back and forth horizontally.
// In LIGHT: gains gravity and falls — the player's weapon against them.
// Kills the player on contact regardless of state.
//
// Requires on this object: Rigidbody2D, Collider2D, SpriteRenderer, LightAffected.
// The LightAffected component handles the gravity switching.
// This script handles patrol movement and player damage.

public class Darkling : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 1.5f;      // How fast it moves while floating
    [SerializeField] private float patrolRange = 3f;         // How far it patrols from its start position

    [Header("Behavior")]
    [SerializeField] private bool destroyOnFall = true;      // If true, destroy after falling a certain distance
    [SerializeField] private float fallDestroyDistance = 15f; // How far below start before it's destroyed

    // === INTERNAL ===

    private Vector3 startPosition;       // Where this enemy was placed in the editor
    private float patrolDirection = 1f;  // 1 = right, -1 = left
    private LightAffected lightAffected;
    private Rigidbody2D rb;
    private bool isFalling = false;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        startPosition = transform.position;
        lightAffected = GetComponent<LightAffected>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Check if we're currently lit (has gravity) or in darkness (floating).
        // We check gravity scale directly — if it's 0, we're in darkness.
        if (rb.gravityScale == 0f)
        {
            Patrol();
            isFalling = false;
        }
        else
        {
            StopPatrol();

            if (!isFalling)
            {
                isFalling = true;
            }

            // Clean up if we've fallen too far below our start.
            if (destroyOnFall && transform.position.y < startPosition.y - fallDestroyDistance)
            {
                // Play kill sound right before destruction
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayEnemyKill();
                }

                Destroy(gameObject);
            }
        }
    }

    // Kills the player on contact.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.Die();
            }
        }
    }

    // Also check for non-trigger collisions (in case collider isn't a trigger).
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out PlayerHealth health))
            {
                health.Die();
            }
        }
    }

    // === PRIVATE METHODS ===

    // Moves back and forth horizontally within patrol range.
    private void Patrol()
    {
        // Move in the current direction.
        Vector3 movement = Vector3.right * patrolDirection * patrolSpeed * Time.deltaTime;
        transform.position += movement;

        // If we've gone too far from start, reverse direction.
        float distanceFromStart = transform.position.x - startPosition.x;

        if (distanceFromStart > patrolRange)
        {
            patrolDirection = -1f;   // Turn left
        }
        else if (distanceFromStart < -patrolRange)
        {
            patrolDirection = 1f;    // Turn right
        }
    }

    // Stop horizontal movement when falling.
    private void StopPatrol()
    {
        // We don't zero out velocity here because Rigidbody2D controls the fall.
        // We just stop manually moving in Patrol().
    }
}