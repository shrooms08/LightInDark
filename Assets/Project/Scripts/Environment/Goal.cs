using UnityEngine;

// The finish line of a level. When the player touches this, the run is complete.
//
// Attach this to an empty object with a Collider2D set as Trigger.

public class Goal : MonoBehaviour
{
    private bool hasBeenReached = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenReached)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            hasBeenReached = true;

            // Tell the GameManager the level is done.
            // It handles stopping the timer and showing the complete screen.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelComplete();
            }
        }
    }
}