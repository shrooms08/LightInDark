using UnityEngine;
using UnityEngine.SceneManagement;

// Main menu with two paths:
// 1. PLAY — goes straight to Level_01 (free play, no wallet needed)
// 2. SPEEDRUN — opens the speedrun screen with instructions, wallet connect, stake, countdown
//
// Attach to MainMenu object in Menu scene.

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject speedrunPanel;

    public static bool IsCompetitiveMode { get; set; } = false;
    public static int SelectedLevel { get; set; } = 1;

    private void Start()
    {
        ShowTitlePanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnPlayButton();
        }
    }

    public void OnPlayButton()
    {
        IsCompetitiveMode = false;
        SelectedLevel = 1;
        GameManager.DeathCount = 0;
        SceneManager.LoadScene("Level_01");
    }

    public void OnSpeedrunButton()
    {
        IsCompetitiveMode = true;
        SelectedLevel = 1;
        GameManager.DeathCount = 0;
        ShowSpeedrunPanel();
    }

    public void OnBackButton()
    {
        IsCompetitiveMode = false;
        ShowTitlePanel();
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

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