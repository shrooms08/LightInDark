using UnityEngine;

// Kills anything on contact: player, enemies, bullets.
// Does NOT affect LightPlatforms.
// Attach to spikes, kill floors, any lethal surface.
// Requires: Collider2D set as Trigger.

public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kill the player.
        if (other.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerHealth health))
            {
                health.Die();
            }
            return;
        }

        // Skip LightPlatforms.
        if (other.GetComponent<LightPlatform>() != null)
        {
            return;
        }

        // Skip other Hazards.
        if (other.GetComponent<Hazard>() != null)
        {
            return;
        }

        // Destroy any enemy or bullet.
        if (other.TryGetComponent(out LightAffected affected))
        {
            Destroy(other.gameObject);
        }
    }
}