using UnityEngine;

// Handles player death.
// Attach this to the Player object.

public class PlayerHealth : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerController playerController;
    private bool isDead = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (playerController != null)
        {
            playerController.Die();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDeath();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied();
        }
    }
}