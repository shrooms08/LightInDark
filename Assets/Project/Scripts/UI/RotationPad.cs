using UnityEngine;
using UnityEngine.EventSystems;

// A circular touch pad that controls the light cone rotation.
// The player drags their finger and the light cone points in that direction.
//
// How it works:
// - Player touches the pad
// - Drag direction from pad center = light cone angle
// - Release = light stays at last angle
//
// Attach this to a UI Image (the visible pad).
// Implements drag interfaces so Unity sends touch/mouse events to it.

public class RotationPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // === SETTINGS ===

    [Header("Visual")]
    [SerializeField] private RectTransform knob;           // Optional: a small dot showing drag direction
    [SerializeField] private float knobRadius = 50f;       // How far the knob can move from center

    // === STATE ===

    // The current angle in degrees. LightCone reads this.
    public float CurrentAngle { get; private set; } = 0f;
    public bool IsDragging { get; private set; } = false;

    // Singleton so LightCone can find it.
    public static RotationPad Instance { get; private set; }

    // === INTERNAL ===

    private RectTransform padRect;

    // === UNITY LIFECYCLE ===

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        padRect = GetComponent<RectTransform>();
    }

    // === POINTER EVENTS ===
    // Unity calls these automatically because we implement the interfaces.

    // Finger/mouse touches the pad.
    public void OnPointerDown(PointerEventData eventData)
    {
        IsDragging = true;
        UpdateAngle(eventData);
    }

    // Finger/mouse moves while touching.
    public void OnDrag(PointerEventData eventData)
    {
        UpdateAngle(eventData);
    }

    // Finger/mouse lifts off.
    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;

        // Reset knob to center.
        if (knob != null)
        {
            knob.anchoredPosition = Vector2.zero;
        }
    }

    // === PRIVATE METHODS ===

    private void UpdateAngle(PointerEventData eventData)
    {
        // Convert the screen touch position to a local position inside the pad.
        // RectTransformUtility does the math for different screen sizes and canvas scaling.
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            padRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // Calculate the angle from the center of the pad to the touch point.
        // Atan2 gives us the angle in radians, Rad2Deg converts to degrees.
        // This gives: 0° = right, 90° = up, 180° = left, -90° = down.
        CurrentAngle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;

        // Move the knob visual to show where the player is pointing.
        if (knob != null)
        {
            // Clamp the knob position to the radius so it stays inside the pad.
            Vector2 direction = localPoint.normalized;
            float distance = Mathf.Min(localPoint.magnitude, knobRadius);
            knob.anchoredPosition = direction * distance;
        }
    }
}