 using UnityEngine;

// Handles player death and respawning.
//
// Attach this to the Player object.

public class PlayerHealth : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 0.5f;

    [Header("References")]
    [SerializeField] private HUD hud;

    // === INTERNAL ===

    private Vector3 spawnPosition;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private bool isDead = false;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        spawnPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
    }

    // === PUBLIC METHODS ===

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        rb.linearVelocity = Vector2.zero;
        playerController.enabled = false;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeath();

        // Tell the HUD.
        if (hud != null)
        {
            hud.AddDeath();
        }

        // Tell the GameManager.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied();
        }

        Invoke("Respawn", respawnDelay);
    }

    public void SetCheckpoint(Vector3 newSpawnPosition)
    {
        spawnPosition = newSpawnPosition;
    }

    // === PRIVATE METHODS ===

    private void Respawn()
    {
        transform.position = spawnPosition;
        rb.linearVelocity = Vector2.zero;
        playerController.enabled = true;
        isDead = false;

        // Tell the GameManager we're back.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerRespawned();
        }
    }
}