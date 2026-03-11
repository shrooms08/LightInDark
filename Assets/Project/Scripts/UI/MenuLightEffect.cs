using UnityEngine;

// Moves a UI circle across the menu screen to create
// a sweeping light effect that inverts colors underneath.
//
// Attach to the LightCircle object.

public class MenuLightEffect : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Movement")]
    [SerializeField] private float speed = 100f;
    [SerializeField] private float amplitude = 200f;       // How far up/down it sways
    [SerializeField] private float horizontalRange = 800f;  // How far left/right

    // === INTERNAL ===

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private float time;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        time += Time.deltaTime;

        // Move in a figure-8 / sweep pattern.
        float x = Mathf.Sin(time * speed * 0.01f) * horizontalRange;
        float y = Mathf.Sin(time * speed * 0.02f) * amplitude;

        rectTransform.anchoredPosition = startPosition + new Vector2(x, y);
    }
}