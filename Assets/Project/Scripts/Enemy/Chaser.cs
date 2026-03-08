using UnityEngine;

// A floating enemy that idles until the player gets close, then chases them.
// In darkness: floats and chases (or idles if player is far).
// In light: gains gravity and falls (stops chasing).
//
// Requires: Rigidbody2D, Collider2D, SpriteRenderer, LightAffected.

public class Chaser : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;    // Start chasing when player is this close
    [SerializeField] private float chaseSpeed = 3f;        // How fast it chases

    [Header("Behavior")]
    [SerializeField] private bool destroyOnFall = true;
    [SerializeField] private float fallDestroyDistance = 15f;

    // === INTERNAL ===

    private Vector3 startPosition;
    private Rigidbody2D rb;
    private Transform playerTransform;
    private bool isChasing = false;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        // Only chase when in darkness (floating).
        if (rb.gravityScale == 0f)
        {
            if (PlayerInRange())
            {
                ChasePlayer();
                isChasing = true;
            }
            else
            {
                // Stop moving if player is out of range.
                if (isChasing)
                {
                    rb.linearVelocity = Vector2.zero;
                    isChasing = false;
                }
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

    private void ChasePlayer()
    {
        if (playerTransform == null)
        {
            return;
        }

        // Move toward the player.
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;
    }
}