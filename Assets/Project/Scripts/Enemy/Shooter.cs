using UnityEngine;

// A floating enemy that shoots bullets toward the player at intervals.
// In darkness: floats in place, fires bullets.
// In light: gains gravity and falls (stops shooting).
//
// Requires: Rigidbody2D, Collider2D, SpriteRenderer, LightAffected.

public class Shooter : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;      // The bullet to spawn
    [SerializeField] private float fireRate = 2f;          // Seconds between shots
    [SerializeField] private float detectionRange = 10f;   // Only shoot when player is within range

    [Header("Behavior")]
    [SerializeField] private bool destroyOnFall = true;
    [SerializeField] private float fallDestroyDistance = 15f;

    // === INTERNAL ===

    private Vector3 startPosition;
    private Rigidbody2D rb;
    private float fireTimer;
    private Transform playerTransform;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        fireTimer = fireRate;    // Ready to fire immediately after first interval.

        // Find the player in the scene.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        // Only shoot when in darkness (floating, no gravity).
        if (rb.gravityScale == 0f)
        {
            fireTimer -= Time.deltaTime;

            if (fireTimer <= 0f && PlayerInRange())
            {
                Fire();
                fireTimer = fireRate;
            }
        }

        // Clean up if fallen too far.
        if (destroyOnFall && transform.position.y < startPosition.y - fallDestroyDistance)
        {
            Destroy(gameObject);
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

    private bool PlayerInRange()
    {
        if (playerTransform == null)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        return distance <= detectionRange;
    }

    private void Fire()
    {
        if (bulletPrefab == null || playerTransform == null)
        {
            return;
        }

        // Calculate direction toward the player.
        Vector2 direction = (playerTransform.position - transform.position).normalized;

        // Spawn the bullet slightly in front of the shooter.
        Vector3 spawnPosition = transform.position + (Vector3)(direction * 0.5f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        // Initialize the bullet with its travel direction.
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBulletFire();
    }
}