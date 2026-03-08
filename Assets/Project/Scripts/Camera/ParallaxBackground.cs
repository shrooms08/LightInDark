using UnityEngine;

// Makes the background scroll slower than the camera for a parallax depth effect.
// Attach this to the Background object.

public class ParallaxBackground : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Parallax")]
    [SerializeField] private float parallaxFactor = 0.5f;  // 0 = static, 1 = moves with camera

    // === INTERNAL ===

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        // Move background by a fraction of the camera movement.
        transform.position += new Vector3(
            cameraDelta.x * parallaxFactor,
            cameraDelta.y * parallaxFactor,
            0f
        );

        lastCameraPosition = cameraTransform.position;
    }
}