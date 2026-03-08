using UnityEngine;

// Follows the player with smooth movement.
// Camera is clamped to level boundaries so it never shows outside the level.
//
// Attach to Main Camera.

public class CameraFollow : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Limits")]
    [SerializeField] private bool useLimits = true;
    [SerializeField] private float minX = -1f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float maxY = 10f;

    // === UNITY LIFECYCLE ===

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Desired position based on player.
        Vector3 desiredPosition = target.position + offset;

        // Smooth follow.
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // Clamp to level boundaries.
        if (useLimits)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        // Keep the Z offset (camera must be behind the 2D plane).
        smoothedPosition.z = offset.z;

        transform.position = smoothedPosition;
    }
}