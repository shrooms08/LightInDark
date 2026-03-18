using UnityEngine;
using System.Collections;

// A floating enemy that shoots bullets toward the player.
// In darkness: floats and shoots.
// In light: gains gravity and falls.
// Only Hazards kill it.

public class Shooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float detectionRange = 10f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 2f;
    [SerializeField] private ParticleSystem deathParticles;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D enemyCollider;
    private Transform playerTransform;

    private float fireTimer;
    private bool isDead = false;
    private bool deathStarted = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();

        fireTimer = fireRate;

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
            fireTimer -= Time.deltaTime;

            if (fireTimer <= 0f && PlayerInRange())
            {
                Fire();
                fireTimer = fireRate;
            }
        }
        else
        {
            // Light = fall only
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

    private void Fire()
    {
        if (bulletPrefab == null || playerTransform == null)
        {
            return;
        }

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        Vector3 spawnPosition = transform.position + (Vector3)(direction * 0.5f);

        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBulletFire();
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