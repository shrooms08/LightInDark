using UnityEngine;

// A projectile fired by Shooter enemies.
// Travels in a straight line. Kills the player on contact.
// Has LightAffected so the player's light gives it gravity (it drops).
// Destroys itself after a time limit or when hitting something solid.
//
// Requires: Rigidbody2D, Collider2D (trigger), LightAffected.

public class EnemyBullet : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Bullet")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifetime = 5f;    // Auto-destroy after this many seconds

    // === INTERNAL ===

    private Vector2 direction;
    private Rigidbody2D rb;
    private bool initialized = false;

    // === PUBLIC METHODS ===

    // Called by the Shooter when it creates this bullet.
    // Sets the direction the bullet should travel.
    public void Initialize(Vector2 fireDirection)
    {
        direction = fireDirection.normalized;
        initialized = true;
    }

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;    // Starts with no gravity (in darkness).

        // Auto-destroy after lifetime expires.
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            return;
        }

        // Only move in our direction if we have no gravity (in darkness).
        // When lit, gravity takes over and the bullet falls.
        if (rb.gravityScale == 0f)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    // === TRIGGER CALLBACKS ===

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kill the player.
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.Die();
            }
            Destroy(gameObject);
        }

        // Destroy on hitting solid ground (not other enemies or triggers).
        // We check for the Ground layer.
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}