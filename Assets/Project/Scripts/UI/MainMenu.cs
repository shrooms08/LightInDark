using UnityEngine;
using UnityEngine.SceneManagement;

// Main menu with two paths:
// 1. PLAY — goes straight to Level_01 (free play, no wallet needed)
// 2. SPEEDRUN — opens the speedrun screen with instructions, wallet connect, stake, countdown
//
// Attach to MainMenu object in Menu scene.

public class MainMenu : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject speedrunPanel;

    // === STATE ===

    public static bool IsCompetitiveMode { get; set; } = false;
    public static int SelectedLevel { get; set; } = 1;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        ShowTitlePanel();
    }

    private void Update()
    {
        // Keyboard shortcut for editor testing.
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnPlayButton();
        }
    }

    // === BUTTON CALLBACKS ===

    public void OnPlayButton()
    {
        // Free play — straight to Level_01, no wallet needed.
        IsCompetitiveMode = false;
        SelectedLevel = 1;
        SceneManager.LoadScene("Level_01");
    }

    public void OnSpeedrunButton()
    {
        // Show the speedrun panel with instructions.
        ShowSpeedrunPanel();
    }

    public void OnBackButton()
    {
        ShowTitlePanel();
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    // === PRIVATE METHODS ===

    private void ShowTitlePanel()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (speedrunPanel != null) speedrunPanel.SetActive(false);
    }

    private void ShowSpeedrunPanel()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (speedrunPanel != null) speedrunPanel.SetActive(true);
    }
}