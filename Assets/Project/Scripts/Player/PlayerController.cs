using UnityEngine;

// Controls player movement and jumping.
// Reads from keyboard (editor) and TouchControls (mobile).

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Particles")]
    [SerializeField] private ParticleSystem landingDust;
    [SerializeField] private ParticleSystem deathBurst;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float moveInput;
    private bool isGrounded;
    private bool jumpRequested;
    private bool wasGrounded;
    private bool isDead;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!isDead)
        {
            ReadInput();
            FlipSprite();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        CheckGround();

        if (!isDead)
        {
            if (!wasGrounded && isGrounded)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayLand();
                }

                if (landingDust != null)
                {
                    landingDust.Play();
                }
            }

            Move();
            Jump();
        }

        wasGrounded = isGrounded;
        UpdateAnimator();
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        moveInput = 0f;
        jumpRequested = false;

        // Stop horizontal movement but allow gravity to keep pulling player down.
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetBool("Dead", true);
        }

        if (deathBurst != null)
        {
            deathBurst.Play();
        }
    }

    private void ReadInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        bool keyboardJump = Input.GetButtonDown("Jump");

        if (TouchControls.Instance != null)
        {
            float touchMove = TouchControls.Instance.MoveInput;
            bool touchJump = TouchControls.Instance.JumpPressed;

            if (touchMove != 0f)
            {
                moveInput = touchMove;
            }

            if (touchJump)
            {
                keyboardJump = true;
            }
        }

        if (keyboardJump)
        {
            jumpRequested = true;
        }
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJump();
            }
        }

        jumpRequested = false;
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void FlipSprite()
    {
        if (spriteRenderer == null) return;

        if (moveInput > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveInput < -0.01f)
            spriteRenderer.flipX = true;
    }

    private void UpdateAnimator()
    {
        if (animator == null || rb == null) return;

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        animator.SetBool("Dead", isDead);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}