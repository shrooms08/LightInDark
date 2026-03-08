using UnityEngine;
using UnityEngine.SceneManagement;

// Controls game flow: start, death, pause, restart, level transitions.
// Submits times to Solana on level complete.
// Death = full level restart.

public class GameManager : MonoBehaviour
{
    // === SINGLETON ===
    public static GameManager Instance { get; private set; }

    // === SETTINGS ===

    [Header("References")]
    [SerializeField] private RunTimer runTimer;
    [SerializeField] private HUD hud;
    [SerializeField] private GameObject pauseScreen;

    [Header("Level Flow")]
    [SerializeField] private string nextLevelName;

    // === STATE ===

    public enum GameState { Playing, Dead, Paused, LevelComplete }
    public GameState CurrentState { get; private set; }

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

    private void Start()
    {
        StartLevel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    // === PUBLIC METHODS ===

    public void StartLevel()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (runTimer != null)
        {
            runTimer.ResetTimer();
            runTimer.StartTimer();
        }

        if (hud != null)
        {
            hud.ResetDeaths();
        }

        SetScreenActive(pauseScreen, false);
    }

    public void OnPlayerDied()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        CurrentState = GameState.Dead;

        if (hud != null)
        {
            hud.AddDeath();
        }

        RestartLevel();
    }

    public void OnPlayerRespawned()
    {
        CurrentState = GameState.Playing;
    }

    public async void OnLevelComplete()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        CurrentState = GameState.LevelComplete;

        if (runTimer != null)
        {
            runTimer.StopTimer();
        }

        // Submit time to Solana if wallet is connected.
        float completionTime = runTimer != null ? runTimer.CurrentTime : 0f;
        int levelId = MainMenu.SelectedLevel;

        if (SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected())
        {
            if (MainMenu.IsCompetitiveMode)
            {
                await SolanaManager.Instance.SubmitCompetitiveTime(levelId, completionTime);
                Debug.Log("Competitive time submitted: " + completionTime);
            }
            else
            {
                await SolanaManager.Instance.SubmitTime(levelId, completionTime);
                Debug.Log("Free play time submitted: " + completionTime);
            }
        }

        LoadNextLevel();
    }

    public void PauseGame()
    {
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;

        if (runTimer != null)
        {
            runTimer.PauseTimer();
        }

        SetScreenActive(pauseScreen, true);
    }

    public void ResumeGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (runTimer != null)
        {
            runTimer.ResumeTimer();
        }

        SetScreenActive(pauseScreen, false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextLevelName);
        }
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    // === PRIVATE METHODS ===

    private void SetScreenActive(GameObject screen, bool active)
    {
        if (screen != null)
        {
            screen.SetActive(active);
        }
    }
}