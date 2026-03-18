using UnityEngine;
using System.Collections;

// The basic enemy of LightInDark.
// In DARKNESS: floats and patrols horizontally.
// In LIGHT: gains gravity and falls, but keeps patrolling on ground.
// Only Hazards kill it.

public class Darkling : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float patrolRange = 3f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 2f;
    [SerializeField] private ParticleSystem deathParticles;

    private Vector3 startPosition;
    private float patrolDirection = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D enemyCollider;
    private SpriteRenderer spriteRenderer;

    private bool isDead = false;
    private bool deathStarted = false;

    private void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        Patrol();
    }

    private void Patrol()
    {
        Vector3 movement = Vector3.right * patrolDirection * patrolSpeed * Time.deltaTime;
        transform.position += movement;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = patrolDirection < 0f;
        }

        float distanceFromStart = transform.position.x - startPosition.x;

        if (distanceFromStart > patrolRange)
        {
            patrolDirection = -1f;
        }
        else if (distanceFromStart < -patrolRange)
        {
            patrolDirection = 1f;
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

    public void Die()
    {
        if (deathStarted) return;

        deathStarted = true;
        isDead = true;

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