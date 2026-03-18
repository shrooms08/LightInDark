using UnityEngine;
using System.Collections;

// A floating enemy that idles until the player gets close, then chases them.
// In darkness: floats and chases.
// In light: gains gravity and falls.
// Only Hazards kill it.

public class Chaser : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 2f;
    [SerializeField] private ParticleSystem deathParticles;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D enemyCollider;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;

    private bool isChasing = false;
    private bool isDead = false;
    private bool deathStarted = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        if (rb.gravityScale == 0f)
        {
            if (PlayerInRange())
            {
                ChasePlayer();
                isChasing = true;
            }
            else
            {
                if (isChasing)
                {
                    rb.linearVelocity = Vector2.zero;
                    isChasing = false;
                }
            }
        }
        else
        {
            if (isChasing)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                isChasing = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

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
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out PlayerHealth health))
            {
                health.Die();
            }
        }
    }

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

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0f;
        }
    }

    public void Die()
    {
        if (deathStarted) return;

        deathStarted = true;
        isDead = true;
        isChasing = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyKill();
        }

        if (animator != null)
        {
            animator.SetBool("Dead", true);
        }

        if (deathParticles != null)
        {
            deathParticles.transform.parent = null;
            deathParticles.Play();
            Destroy(deathParticles.gameObject, 2f);
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }
}