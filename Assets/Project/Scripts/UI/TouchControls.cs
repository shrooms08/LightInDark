using UnityEngine;

// Central input hub for mobile touch controls.
// Handles: movement (left/right buttons) and jump (button).
// Rotation is handled separately by RotationPad.
//
// Attach this to an empty object in the scene.

public class TouchControls : MonoBehaviour
{
    // === SINGLETON ===
    public static TouchControls Instance { get; private set; }

    // === INPUT STATE ===
    public float MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }

    // Flags set by UI button events.
    private bool holdingLeft = false;
    private bool holdingRight = false;
    private bool jumpQueued = false;

    // === UNITY LIFECYCLE ===

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // --- Movement: combine touch + keyboard ---
        float touchMove = 0f;
        if (holdingLeft) touchMove = -1f;
        if (holdingRight) touchMove = 1f;

        float keyboardMove = Input.GetAxisRaw("Horizontal");
        MoveInput = (keyboardMove != 0f) ? keyboardMove : touchMove;

        // --- Jump: one-frame flag ---
        JumpPressed = jumpQueued || Input.GetButtonDown("Jump");
        jumpQueued = false;
    }

    // === BUTTON CALLBACKS ===

    public void OnLeftPressed() { holdingLeft = true; }
    public void OnLeftReleased() { holdingLeft = false; }
    public void OnRightPressed() { holdingRight = true; }
    public void OnRightReleased() { holdingRight = false; }
    public void OnJumpPressed() { jumpQueued = true; }
}