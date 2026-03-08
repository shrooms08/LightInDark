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
    private bool hasBeenLit = false;    // Once true, gravity never resets.

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
        hasBeenLit = true;

        // Gravity on permanently.
        rb.gravityScale = litGravity;

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

            // Gravity stays — object keeps falling.
            // Do NOT reset gravityScale.
        }
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