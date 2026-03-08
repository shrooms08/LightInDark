using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Displays the speedrun timer on screen.
// Reads from RunTimer every frame and updates the text.
//
// Attach this to an empty object under the Canvas.
// Requires: a TextMeshProUGUI element for the timer display.

public class HUD : MonoBehaviour
{
    // === REFERENCES ===

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerText;      // The text element showing the time
    [SerializeField] private TextMeshProUGUI deathCountText;  // Optional: shows death count

    [Header("References")]
    [SerializeField] private RunTimer runTimer;               // The timer to read from

    // === INTERNAL ===

    private int deathCount = 0;

    // === UNITY LIFECYCLE ===

    private void Update()
    {
        // Update the timer display every frame.
        if (runTimer != null && timerText != null)
        {
            timerText.text = runTimer.GetFormattedTime();
        }
    }

    // === PUBLIC METHODS ===

    // Called when the player dies. Increases the death counter display.
    public void AddDeath()
    {
        deathCount++;

        if (deathCountText != null)
        {
            deathCountText.text = "Deaths: " + deathCount;
        }
    }

    // Resets the death counter. Used when restarting a level.
    public void ResetDeaths()
    {
        deathCount = 0;

        if (deathCountText != null)
        {
            deathCountText.text = "Deaths: 0";
        }
    }
}