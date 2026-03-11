using UnityEngine;

// Put this on ANY object that should react to the player's light.
// When in darkness: no gravity, floats in place, appears WHITE.
// When lit: gravity turns on PERMANENTLY, appears BLACK.
// When light turns off: stays falling (gravity stays), color returns to WHITE.
//
// Once an object has been lit, it never floats again.

public class LightAffected : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Gravity")]
    [SerializeField] private float litGravity = 1f;
    [SerializeField] private float darkGravity = 0f;

    [Header("Visual Feedback")]
    [SerializeField] private Color litColor = Color.black;
    [SerializeField] private Color darkColor = Color.white;

    // === INTERNAL ===

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int lightCount = 0;
    private bool hasBeenLit = false;    // Once true, gravity never resets → we use this to allow impact sounds

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyDarkState();
    }

    // === PUBLIC METHODS ===

    public void EnterLight()
    {
        lightCount++;

        if (lightCount == 1)   // First time lit
        {
            hasBeenLit = true;

            // Gravity on permanently.
            rb.gravityScale = litGravity;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = litColor;
        }
    }

    public void ExitLight()
    {
        lightCount--;

        if (lightCount < 0)
        {
            lightCount = 0;
        }

        if (lightCount == 0)
        {
            // Color reverts to white, but gravity stays on.
            if (spriteRenderer != null)
            {
                spriteRenderer.color = darkColor;
            }
        }
    }

 
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[IMPACT] {gameObject.name} hit {collision.gameObject.name} | Tag: '{tag}' | Lit: {hasBeenLit} | Velocity: {collision.relativeVelocity.magnitude:F1}");

        if (!hasBeenLit) 
        {
            Debug.LogWarning($"[NO SOUND] {gameObject.name} collided but not lit yet!");
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogError("[NO AUDIO] AudioManager missing!");
            return;
        }

        string soundPlayed = "None";
        if (CompareTag("Enemy"))
        {
            AudioManager.Instance.PlayEnemyFall();
            soundPlayed = "EnemyFall";
        }
        else if (CompareTag("Platform"))
        {
            AudioManager.Instance.PlayPlatformFall();
            soundPlayed = "PlatformFall";
        }
        else if (CompareTag("Spike"))
        {
            AudioManager.Instance.PlaySpikeFall();
            soundPlayed = "SpikeFall";
        }
        else
        {
            Debug.LogWarning($"[NO MATCH] Tag '{tag}' on {gameObject.name} - add to script?");
        }

        Debug.Log($"[SOUND] Played {soundPlayed} on {gameObject.name}");
    }

    // === PRIVATE METHODS ===

    private void ApplyDarkState()
    {
        rb.gravityScale = darkGravity;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = darkColor;
        }
    }
}