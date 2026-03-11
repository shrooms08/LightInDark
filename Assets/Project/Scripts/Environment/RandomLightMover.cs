using UnityEngine;
using UnityEngine.UI;

public class RandomLightMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 80f;       // pixels per second
    [SerializeField] private float changeDirectionTime = 3f; // seconds between direction changes
    [SerializeField] private Vector2 minBounds = new Vector2(-400, -300); // relative to canvas center
    [SerializeField] private Vector2 maxBounds = new Vector2(400, 300);

    private Vector2 currentDirection;
    private float timer;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        PickNewDirection();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PickNewDirection();
        }

        // Move
        Vector2 newPos = rect.anchoredPosition + currentDirection * moveSpeed * Time.deltaTime;

        // Clamp to bounds so it doesn't go off-screen forever
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

        rect.anchoredPosition = newPos;

        // If hit edge, pick new direction immediately
        if (newPos.x <= minBounds.x || newPos.x >= maxBounds.x ||
            newPos.y <= minBounds.y || newPos.y >= maxBounds.y)
        {
            PickNewDirection();
        }
    }

    private void PickNewDirection()
    {
        currentDirection = Random.insideUnitCircle.normalized;
        timer = changeDirectionTime + Random.Range(-1f, 1f); // slight variation
    }
}