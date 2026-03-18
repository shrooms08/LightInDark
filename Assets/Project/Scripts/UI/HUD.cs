using UnityEngine;
using TMPro;

// Displays the speedrun timer and death count on screen.

public class HUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI deathCountText;

    [Header("References")]
    [SerializeField] private RunTimer runTimer;

    private void Start()
    {
        SetDeathCount(GameManager.DeathCount);
    }

    private void Update()
    {
        if (runTimer != null && timerText != null)
        {
            timerText.text = runTimer.GetFormattedTime();
        }
    }

    public void SetDeathCount(int count)
    {
        if (deathCountText != null)
        {
            deathCountText.text = "Deaths: " + count;
        }
    }
}