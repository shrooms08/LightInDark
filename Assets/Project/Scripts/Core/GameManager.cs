using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Controls game flow: start, death, pause, restart, level transitions.
// Offline mode = no blockchain.
// Speedrun mode = submits final result on level complete.
// Death = full level restart after a short delay.

public class GameManager : MonoBehaviour
{
    // === SINGLETON ===
    public static GameManager Instance { get; private set; }

    // === GLOBAL RUN DATA ===
    public static int DeathCount = 0;

    // === SETTINGS ===

    [Header("References")]
    [SerializeField] private RunTimer runTimer;
    [SerializeField] private HUD hud;
    [SerializeField] private GameObject pauseScreen;

    [Header("Level Flow")]
    [SerializeField] private string nextLevelName;

    [Header("Death")]
    [SerializeField] private float deathRestartDelay = 3f;

    [Header("Competitive")]
    [SerializeField] private uint seasonId = 0;

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

    // === LEVEL FLOW ===

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
            hud.SetDeathCount(DeathCount);
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

        if (runTimer != null)
        {
            runTimer.PauseTimer();
        }

        DeathCount++;

        if (hud != null)
        {
            hud.SetDeathCount(DeathCount);
        }

        StartCoroutine(RestartLevelAfterDelay());
    }

    public void OnPlayerRespawned()
    {
        CurrentState = GameState.Playing;
    }

    // === LEVEL COMPLETE ===

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

        float completionTime = runTimer != null ? runTimer.CurrentTime : 0f;
        int levelId = MainMenu.SelectedLevel;

        // Only Speedrun mode submits to blockchain.
        if (MainMenu.IsCompetitiveMode)
        {
            if (SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected())
            {
                bool ok = await SolanaManager.Instance.SubmitRunResult(
                    seasonId,
                    levelId,
                    completionTime,
                    DeathCount
                );

                if (ok)
                {
                    Debug.Log($"Competitive run submitted. Time: {completionTime}, Deaths: {DeathCount}");
                }
                else
                {
                    Debug.LogWarning("Competitive run submission failed.");
                }
            }
            else
            {
                Debug.LogWarning("Competitive mode is on, but wallet is not connected or SolanaManager is missing.");
            }
        }
        else
        {
            Debug.Log($"Offline level complete. Time: {completionTime}, Deaths: {DeathCount}");
        }

        LoadNextLevel();
    }

    // === PAUSE ===

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

    // === RESTART ===

    private IEnumerator RestartLevelAfterDelay()
    {
        yield return new WaitForSeconds(deathRestartDelay);
        RestartLevel();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // === LEVEL LOAD ===

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
        DeathCount = 0;
        MainMenu.IsCompetitiveMode = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    // === UTIL ===

    private void SetScreenActive(GameObject screen, bool active)
    {
        if (screen != null)
        {
            screen.SetActive(active);
        }
    }
}