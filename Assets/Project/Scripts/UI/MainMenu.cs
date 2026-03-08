using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Main menu with wallet connection and level select.
// Handles: Play (free), Connect Wallet, Competitive Run, Leaderboard.
//
// Attach to an empty object in the Menu scene.

public class MainMenu : MonoBehaviour
{
    // === SETTINGS ===

    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;        // Title + Play + Connect Wallet
    [SerializeField] private GameObject levelSelectPanel;  // Level buttons + mode toggle

    [Header("Wallet UI")]
    [SerializeField] private TextMeshProUGUI walletButtonText;   // "Connect Wallet" / "7xKs...3nRt"
    [SerializeField] private GameObject disconnectButton;         // Only visible when connected

    [Header("Mode")]
    [SerializeField] private TextMeshProUGUI modeText;     // "FREE PLAY" or "COMPETITIVE"
    [SerializeField] private GameObject entryFeeText;      // "Entry: 0.01 SOL" — only visible in competitive

    // === STATE ===

    public static bool IsCompetitiveMode { get; private set; } = false;
    public static int SelectedLevel { get; private set; } = 1;

    // === UNITY LIFECYCLE ===

    private void Start()
    {
        ShowTitlePanel();
        UpdateWalletUI();
    }

    private void Update()
    {
        // Keyboard shortcuts for editor testing.
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnPlayButton();
        }
    }

    // === TITLE PANEL BUTTONS ===

    public void OnPlayButton()
    {
        ShowLevelSelectPanel();
    }

    public async void OnConnectWalletButton()
    {
        // Check if SolanaManager exists.
        if (SolanaManager.Instance == null)
        {
            Debug.LogWarning("SolanaManager not found in scene.");
            return;
        }

        if (SolanaManager.Instance.IsWalletConnected())
        {
            // Already connected — disconnect.
            SolanaManager.Instance.DisconnectWallet();
        }
        else
        {
            // Connect.
            string pubkey = await SolanaManager.Instance.ConnectWallet();

            if (pubkey != null)
            {
                Debug.Log("Wallet connected: " + pubkey);
            }
            else
            {
                Debug.LogWarning("Wallet connection failed or cancelled.");
            }
        }

        UpdateWalletUI();
    }

    public void OnDisconnectButton()
    {
        if (SolanaManager.Instance != null)
        {
            SolanaManager.Instance.DisconnectWallet();
        }
        UpdateWalletUI();
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    // === LEVEL SELECT BUTTONS ===

    public void OnLevel1Button()
    {
        StartLevel(1, "Level_01");
    }

    public void OnLevel2Button()
    {
        StartLevel(2, "Level_02");
    }

    public void OnLevel3Button()
    {
        StartLevel(3, "Level_03");
    }

    public void OnToggleModeButton()
    {
        // Only allow competitive mode if wallet is connected.
        if (!IsCompetitiveMode)
        {
            if (SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected())
            {
                IsCompetitiveMode = true;
            }
            else
            {
                Debug.Log("Connect wallet first to play competitive mode.");
                return;
            }
        }
        else
        {
            IsCompetitiveMode = false;
        }

        UpdateModeUI();
    }

    public void OnBackButton()
    {
        ShowTitlePanel();
    }

    // === PRIVATE METHODS ===

    private async void StartLevel(int levelId, string sceneName)
    {
        SelectedLevel = levelId;

        // If competitive mode, pay entry fee first.
        if (IsCompetitiveMode && SolanaManager.Instance != null)
        {
            bool success = await SolanaManager.Instance.EnterCompetitiveRun(levelId);
            if (!success)
            {
                Debug.LogWarning("Failed to enter competitive run. Playing free instead.");
                IsCompetitiveMode = false;
            }
        }

        SceneManager.LoadScene(sceneName);
    }

    private void ShowTitlePanel()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
    }

    private void ShowLevelSelectPanel()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        UpdateModeUI();
    }

    private void UpdateWalletUI()
    {
        bool connected = SolanaManager.Instance != null
                      && SolanaManager.Instance.IsWalletConnected();

        if (walletButtonText != null)
        {
            walletButtonText.text = connected
                ? SolanaManager.Instance.GetWalletAddress()
                : "CONNECT WALLET";
        }

        if (disconnectButton != null)
        {
            disconnectButton.SetActive(connected);
        }
    }

    private void UpdateModeUI()
    {
        if (modeText != null)
        {
            modeText.text = IsCompetitiveMode ? "COMPETITIVE" : "FREE PLAY";
        }

        if (entryFeeText != null)
        {
            entryFeeText.SetActive(IsCompetitiveMode);
        }
    }
}