using UnityEngine;

// Kills anything on contact: player, enemies, bullets.
// Does NOT affect LightPlatforms.
// Attach to spikes, kill floors, any lethal surface.

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

        // Kill enemies through their own death flow.
        if (other.TryGetComponent(out Darkling darkling))
        {
            darkling.Die();
            return;
        }

        if (other.TryGetComponent(out Shooter shooter))
        {
            shooter.Die();
            return;
        }

        if (other.TryGetComponent(out Chaser chaser))
        {
            chaser.Die();
            return;
        }

        // Destroy bullets or other light-affected objects.
        if (other.TryGetComponent(out LightAffected affected))
        {
            Destroy(other.gameObject);
        }
    }
}