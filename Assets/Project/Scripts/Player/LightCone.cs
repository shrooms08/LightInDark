using UnityEngine;
using System.Collections.Generic;

// The player's light cone. Only active when RotationPad is used or Q/E held.
// Objects behind walls/platforms are NOT lit (line-of-sight check).
//
// Attach this to the LightCone child object under Player.

public class LightCone : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Cone Shape")]
    [SerializeField] private float coneRange = 5f;
    [SerializeField] private float coneAngle = 30f;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float padSmoothing = 10f;

    [Header("Occlusion")]
    [SerializeField] private LayerMask blockingLayers;   // Same as LightConeVisual

    // === INTERNAL ===

    private PolygonCollider2D coneCollider;
    private MeshRenderer meshRenderer;
    private float currentAngle = 0f;
    private bool isActive = false;
    private HashSet<LightAffected> litObjects = new HashSet<LightAffected>();
    private HashSet<LightAffected> objectsInCone = new HashSet<LightAffected>();
    private bool hasInitialized = false;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        coneCollider = GetComponent<PolygonCollider2D>();
        coneCollider.isTrigger = true;
        meshRenderer = GetComponent<MeshRenderer>();

        UpdateConeShape();
        SetLightActive(false);
        hasInitialized = true;
    }

    private void Update()
    {
        bool padActive = false;
        bool keyboardActive = false;

        if (RotationPad.Instance != null)
        {
            padActive = RotationPad.Instance.IsDragging;
        }

        keyboardActive = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);

        bool shouldBeActive = padActive || keyboardActive;

        if (shouldBeActive && !isActive)
        {
            SetLightActive(true);
        }
        else if (!shouldBeActive && isActive)
        {
            SetLightActive(false);
        }

        if (isActive)
        {
            ReadRotationInput();
            ApplyRotation();
            UpdateOcclusion();
        }
    }

    // === TRIGGER CALLBACKS ===

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out LightAffected affected))
        {
            objectsInCone.Add(affected);
            // Don't call EnterLight yet — UpdateOcclusion will check line of sight.
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out LightAffected affected))
        {
            objectsInCone.Remove(affected);

            // If it was lit, unlight it.
            if (litObjects.Contains(affected))
            {
                litObjects.Remove(affected);
                affected.ExitLight();
            }
        }
    }

    // === PRIVATE METHODS ===

    // ... (using statements same)

    private void SetLightActive(bool active)
    {
        isActive = active;
        coneCollider.enabled = active;

        if (meshRenderer != null)
        {
            meshRenderer.enabled = active;
        }

        if (active)
        {
            if (hasInitialized && AudioManager.Instance != null) AudioManager.Instance.PlayLightOn();
        }
        else
        {
            // NEW: Stop ongoing SFX (cuts LightOn instantly)
            if (hasInitialized && AudioManager.Instance != null) AudioManager.Instance.StopSFX();

            // Release everything.
            foreach (LightAffected affected in litObjects)
            {
                if (affected != null)
                {
                    affected.ExitLight();
                }
            }
            litObjects.Clear();
            objectsInCone.Clear();

            // Play Off if assigned (null = silence)
            if (hasInitialized && AudioManager.Instance != null) AudioManager.Instance.PlayLightOff();
        }
    }

    private void UpdateOcclusion()
    {
        // For each object inside the cone trigger, check if there's a clear
        // line of sight from the player to the object.

        // We need a temp list because we might modify litObjects during iteration.
        List<LightAffected> toRemove = new List<LightAffected>();

        foreach (LightAffected affected in objectsInCone)
        {
            if (affected == null)
            {
                continue;
            }

            bool canSee = HasLineOfSight(affected.transform.position);

            if (canSee && !litObjects.Contains(affected))
            {
                // Newly visible — light it up.
                litObjects.Add(affected);
                affected.EnterLight();
            }
            else if (!canSee && litObjects.Contains(affected))
            {
                // Lost line of sight — unlight it.
                toRemove.Add(affected);
            }
        }

        foreach (LightAffected affected in toRemove)
        {
            litObjects.Remove(affected);
            if (affected != null)
            {
                affected.ExitLight();
            }
        }
    }

    private bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector2 origin = transform.position;
        Vector2 direction = (Vector2)targetPosition - origin;
        float distance = direction.magnitude;

        // Cast a ray toward the target. If it hits a blocking layer
        // before reaching the target, line of sight is blocked.
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction.normalized,
            distance,
            blockingLayers
        );

        // If the ray didn't hit anything, or hit something farther than the target,
        // we have line of sight.
        if (hit.collider == null)
        {
            return true;
        }

        // Hit something closer than the target — blocked.
        return false;
    }

    private void ReadRotationInput()
    {
        if (RotationPad.Instance != null && RotationPad.Instance.IsDragging)
        {
            float targetAngle = RotationPad.Instance.CurrentAngle;
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, padSmoothing * Time.deltaTime);
            return;
        }

        float keyboardRotate = 0f;
        if (Input.GetKey(KeyCode.Q)) keyboardRotate = 1f;
        if (Input.GetKey(KeyCode.E)) keyboardRotate = -1f;

        currentAngle += keyboardRotate * rotateSpeed * Time.deltaTime;
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    private void UpdateConeShape()
    {
        float angleRad = coneAngle * Mathf.Deg2Rad;

        Vector2 tip = Vector2.zero;
        Vector2 topEdge = new Vector2(
            Mathf.Cos(angleRad) * coneRange,
            Mathf.Sin(angleRad) * coneRange
        );
        Vector2 bottomEdge = new Vector2(
            Mathf.Cos(-angleRad) * coneRange,
            Mathf.Sin(-angleRad) * coneRange
        );

        coneCollider.SetPath(0, new Vector2[] { tip, topEdge, bottomEdge });
    }
}