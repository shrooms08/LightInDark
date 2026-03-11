using UnityEngine;

// Central audio manager. Plays all game sound effects.
// Singleton — access via AudioManager.Instance.PlaySound()
//
// Setup:
// 1. Create empty GameObject "AudioManager" in every level scene
// 2. Add this script
// 3. Add an AudioSource component
// 4. Drag your audio clips into the Inspector fields

public class AudioManager : MonoBehaviour
{
    // === SINGLETON ===
    public static AudioManager Instance { get; private set; }

    // === AUDIO CLIPS ===

    [Header("Player")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip landSound;

    [Header("Light")]
    [SerializeField] private AudioClip lightOnSound;
    [SerializeField] private AudioClip lightOffSound;

    [Header("Enemies")]
    [SerializeField] private AudioClip enemyFallSound;
    [SerializeField] private AudioClip enemyKillSound;
    [SerializeField] private AudioClip bulletFireSound;

    [Header("Environment")]
    [SerializeField] private AudioClip spikeFallSound;
    [SerializeField] private AudioClip platformFallSound;
    [SerializeField] private AudioClip platformPushSound;

    [Header("Game")]
    [SerializeField] private AudioClip goalSound;
    [SerializeField] private AudioClip countdownTickSound;
    [SerializeField] private AudioClip goSound;

    [Header("Settings")]
    [SerializeField] private float sfxVolume = 1f;

    // === INTERNAL ===

    private AudioSource audioSource;

    // === UNITY LIFECYCLE ===

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // === PUBLIC METHODS ===

    // Player
    public void PlayJump() { Play(jumpSound); }
    public void PlayDeath() { Play(deathSound); }
    public void PlayLand() { Play(landSound); }

    // Light
    public void PlayLightOn() { Play(lightOnSound); }
    public void PlayLightOff() { Play(lightOffSound); }

    // Enemies
    public void PlayEnemyFall() { Play(enemyFallSound); }
    public void PlayEnemyKill() { Play(enemyKillSound); }
    public void PlayBulletFire() { Play(bulletFireSound); }

    // Environment
    public void PlaySpikeFall() { Play(spikeFallSound); }
    public void PlayPlatformFall() { Play(platformFallSound); }
    public void PlayPlatformPush() { Play(platformPushSound); }

    // Game
    public void PlayGoal() { Play(goalSound); }
    public void PlayCountdownTick() { Play(countdownTickSound); }
    public void PlayGo() { Play(goSound); }

    // NEW: Stop all current SFX (cuts off ongoing sounds instantly)
    public void StopSFX()
    {
        if (audioSource != null) audioSource.Stop();
    }

    // === PRIVATE METHODS ===

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }
}