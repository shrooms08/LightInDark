using UnityEngine;

// Controls player movement and jumping.
// Reads from keyboard (editor) and TouchControls (mobile).
// Attach this to the Player object.

public class PlayerController : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    // === INTERNAL ===

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool jumpRequested;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        ReadInput();
    }

    private void FixedUpdate()
    {
        CheckGround();
        Move();
        Jump();
    }

    // === PUBLIC METHODS ===

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    // === PRIVATE METHODS ===

    private void ReadInput()
    {
        // Read keyboard input.
        moveInput = Input.GetAxisRaw("Horizontal");

        bool keyboardJump = Input.GetButtonDown("Jump");

        // If touch controls exist, layer them on top.
        // Touch overrides keyboard if touch is giving input.
        if (TouchControls.Instance != null)
        {
            float touchMove = TouchControls.Instance.MoveInput;
            bool touchJump = TouchControls.Instance.JumpPressed;

            // Use touch input if it's active, otherwise keep keyboard.
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

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
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
        }

        jumpRequested = false;
    }
}