using UnityEngine;

// Tracks the player's run time with frame-accurate precision.
// Other scripts (HUD, GameManager) can read the current time.
// The timer only runs when the player is actively playing.
//
// Attach this to an empty object in the scene, or on the GameManager object.

public class RunTimer : MonoBehaviour
{
    // === STATE ===

    // Public read, private write — other scripts can check the time but not change it.
    public float CurrentTime { get; private set; }
    public bool IsRunning { get; private set; }

    // === UNITY LIFECYCLE ===


    private void Update()
    {
        if (IsRunning)
        {
            // Time.deltaTime = the time in seconds since the last frame.
            // Adding it each frame gives us a precise running total.
            CurrentTime += Time.deltaTime;
        }
    }

    // === PUBLIC METHODS ===

    // Call this when the player starts a run (level begins or player moves).
    public void StartTimer()
    {
        CurrentTime = 0f;
        IsRunning = true;
    }

    // Call this when the player finishes the level.
    public void StopTimer()
    {
        IsRunning = false;
    }

    // Call this when the player dies — pause the timer.
    // We DON'T reset it because in speedruns, death time counts.
    public void PauseTimer()
    {
        IsRunning = false;
    }

    // Call this when the player respawns — resume counting.
    public void ResumeTimer()
    {
        IsRunning = true;
    }

    // Resets the timer completely. Used when restarting a level from scratch.
    public void ResetTimer()
    {
        CurrentTime = 0f;
        IsRunning = false;
    }

    // Formats the time as a clean string: "00:00.000"
    // Minutes : Seconds . Milliseconds
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(CurrentTime / 60f);
        int seconds = Mathf.FloorToInt(CurrentTime % 60f);
        int milliseconds = Mathf.FloorToInt((CurrentTime * 1000f) % 1000f);

        // The $ before the string lets us embed variables with {}.
        // :00 means "always show 2 digits". :000 means "always show 3 digits".
        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }
}