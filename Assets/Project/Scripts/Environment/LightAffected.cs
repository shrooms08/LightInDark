using UnityEngine;

// Put this on ANY object that should react to the player's light.
// Darkness: floats (no gravity) and appears WHITE.
// Lit: gravity turns on permanently and appears BLACK.
// When light turns off: gravity stays but color returns to WHITE.

public class LightAffected : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float litGravity = 1f;
    [SerializeField] private float darkGravity = 0f;

    [Header("Visual Feedback")]
    [SerializeField] private Color litColor = Color.black;
    [SerializeField] private Color darkColor = Color.white;

    [Header("Particles")]
    [SerializeField] private ParticleSystem lightParticles;

    [Header("Impact")]
    [SerializeField] private float impactSoundThreshold = 3f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private int lightCount = 0;
    private bool hasBeenLit = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        ApplyDarkState();
    }

    public void EnterLight()
    {
        lightCount++;

        if (lightCount == 1)
        {
            if (!hasBeenLit)
            {
                hasBeenLit = true;

                if (rb != null)
                {
                    rb.gravityScale = litGravity;
                }

                if (lightParticles != null)
                {
                    lightParticles.Play();
                }
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = litColor;
            }
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
            if (spriteRenderer != null)
            {
                spriteRenderer.color = darkColor;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasBeenLit)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < impactSoundThreshold)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        if (CompareTag("Enemy"))
        {
            AudioManager.Instance.PlayEnemyFall();
        }
        else if (CompareTag("Platform"))
        {
            AudioManager.Instance.PlayPlatformFall();
        }
        else if (CompareTag("Spike"))
        {
            AudioManager.Instance.PlaySpikeFall();
        }
    }

    private void ApplyDarkState()
    {
        if (rb != null)
        {
            rb.gravityScale = darkGravity;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = darkColor;
        }
    }
}