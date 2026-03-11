using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// The Speedrun screen flow:
// 1. Shows instructions on how speedrun works
// 2. Player taps "Connect Wallet" → wallet connects via MWA
// 3. On successful connect → auto-stakes entry fee (0.01 SOL)
// 4. On successful stake → countdown timer starts (5 seconds)
// 5. When countdown hits 0 → loads the speedrun level
//
// Attach to the SpeedrunPanel or a child object inside it.

public class SpeedrunScreen : MonoBehaviour
{
    // === SETTINGS ===

    [Header("UI References")]
    [SerializeField] private GameObject instructionGroup;      // Instructions text + connect button
    [SerializeField] private GameObject stakingGroup;           // "Staking..." status text
    [SerializeField] private GameObject countdownGroup;         // Countdown timer display
    [SerializeField] private TextMeshProUGUI instructionText;   // How speedrun works
    [SerializeField] private TextMeshProUGUI connectButtonText; // "CONNECT WALLET" / wallet address
    [SerializeField] private TextMeshProUGUI statusText;        // "Connecting..." / "Staking..." / "Staked!"
    [SerializeField] private TextMeshProUGUI countdownText;     // "3... 2... 1... GO!"
    [SerializeField] private TextMeshProUGUI walletAddressText; // Shows connected address

    [Header("Settings")]
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private int speedrunLevelId = 1;
    [SerializeField] private string speedrunLevelName = "Level_01";

    // === STATE ===

    private bool isCountingDown = false;
    private float countdownTimer = 0f;
    private bool isProcessing = false;

    // === UNITY LIFECYCLE ===

    private void OnEnable()
    {
        // Reset to instruction state every time panel opens.
        ShowInstructions();
    }

    private void Update()
    {
        if (!isCountingDown)
        {
            return;
        }

        countdownTimer -= Time.deltaTime;

        if (countdownTimer <= 0f)
        {
            // GO! Launch the level.
            isCountingDown = false;
            LaunchSpeedrun();
        }
        else
        {
            // Update countdown display.
            int seconds = Mathf.CeilToInt(countdownTimer);
            if (countdownText != null)
            {
                if (seconds > 0)
                {
                    countdownText.text = seconds.ToString();
                }
                else
                {
                    countdownText.text = "GO!";
                }
            }
        }
    }

    // === BUTTON CALLBACKS ===

    public async void OnConnectWalletButton()
    {
        if (isProcessing)
        {
            return;
        }

        // If already connected, go straight to staking.
        if (SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected())
        {
            await StakeAndStartCountdown();
            return;
        }

        isProcessing = true;

        // Show connecting status.
        if (statusText != null)
        {
            statusText.text = "Connecting wallet...";
            statusText.gameObject.SetActive(true);
        }
        if (connectButtonText != null)
        {
            connectButtonText.text = "CONNECTING...";
        }

        // Attempt wallet connection.
        if (SolanaManager.Instance == null)
        {
            Debug.LogWarning("[SpeedrunScreen] SolanaManager not found.");
            if (statusText != null) statusText.text = "Wallet service not available.";
            if (connectButtonText != null) connectButtonText.text = "CONNECT WALLET";
            isProcessing = false;
            return;
        }

        string pubkey = await SolanaManager.Instance.ConnectWallet();

        if (pubkey == null)
        {
            // Connection failed or cancelled.
            if (statusText != null) statusText.text = "Connection failed. Try again.";
            if (connectButtonText != null) connectButtonText.text = "CONNECT WALLET";
            isProcessing = false;
            return;
        }

        // Connection successful — show address.
        string displayAddress = SolanaManager.Instance.GetWalletAddress();
        if (walletAddressText != null)
        {
            walletAddressText.text = displayAddress;
            walletAddressText.gameObject.SetActive(true);
        }
        if (connectButtonText != null)
        {
            connectButtonText.text = displayAddress;
        }

        // Auto-stake and start countdown.
        await StakeAndStartCountdown();

        isProcessing = false;
    }

    // === PRIVATE METHODS ===

    private async System.Threading.Tasks.Task StakeAndStartCountdown()
    {
        // Show staking status.
        ShowStaking();

        if (statusText != null)
        {
            statusText.text = "Staking 0.01 SOL...";
        }

        // Enter competitive run (pays entry fee).
        bool stakeSuccess = false;

        if (SolanaManager.Instance != null)
        {
            stakeSuccess = await SolanaManager.Instance.EnterCompetitiveRun(speedrunLevelId);
        }

        if (!stakeSuccess)
        {
            if (statusText != null)
            {
                statusText.text = "Staking failed. Check SOL balance.";
            }
            // Go back to instructions after a moment.
            Invoke(nameof(ShowInstructions), 2f);
            return;
        }

        // Stake successful — show countdown.
        if (statusText != null)
        {
            statusText.text = "Staked! Get ready...";
        }

        StartCountdown();
    }

    private void ShowInstructions()
    {
        if (instructionGroup != null) instructionGroup.SetActive(true);
        if (stakingGroup != null) stakingGroup.SetActive(false);
        if (countdownGroup != null) countdownGroup.SetActive(false);

        if (instructionText != null)
        {
            instructionText.text =
                "SPEEDRUN MODE\n\n" +
                "Compete for the fastest time.\n" +
                "Your run is recorded on-chain.\n\n" +
                "Entry fee: 1000 SKR\n" +
                "Top 3 players win the prize pool.\n\n" +
                "Connect your wallet to begin.";
        }

        if (connectButtonText != null)
        {
            if (SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected())
            {
                connectButtonText.text = SolanaManager.Instance.GetWalletAddress();
            }
            else
            {
                connectButtonText.text = "CONNECT WALLET";
            }
        }

        if (statusText != null) statusText.gameObject.SetActive(false);
        if (walletAddressText != null) walletAddressText.gameObject.SetActive(false);

        isCountingDown = false;
        isProcessing = false;
    }

    private void ShowStaking()
    {
        if (instructionGroup != null) instructionGroup.SetActive(false);
        if (stakingGroup != null) stakingGroup.SetActive(true);
        if (countdownGroup != null) countdownGroup.SetActive(false);

        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
        }
    }

    private void StartCountdown()
    {
        if (instructionGroup != null) instructionGroup.SetActive(false);
        if (stakingGroup != null) stakingGroup.SetActive(false);
        if (countdownGroup != null) countdownGroup.SetActive(true);

        countdownTimer = countdownDuration;
        isCountingDown = true;

        if (countdownText != null)
        {
            countdownText.text = Mathf.CeilToInt(countdownDuration).ToString();
        }
    }

    private void LaunchSpeedrun()
    {
        MainMenu.IsCompetitiveMode = true;
        MainMenu.SelectedLevel = speedrunLevelId;
        SceneManager.LoadScene(speedrunLevelName);
    }
}