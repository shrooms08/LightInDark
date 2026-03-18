using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SpeedrunScreen : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject instructionGroup;
    [SerializeField] private GameObject runPanel;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI walletAddressText;
    [SerializeField] private TextMeshProUGUI registrationCountdownText;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Buttons")]
    [SerializeField] private GameObject connectButton;
    [SerializeField] private GameObject checkWalletButton;
    [SerializeField] private GameObject stakeButton;
    [SerializeField] private GameObject startRunButton;
    [SerializeField] private GameObject disconnectWalletButton;

    [Header("Settings")]
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private uint seasonId = 0;
    [SerializeField] private int levelId = 1;
    [SerializeField] private string levelSceneName = "Level_01";
    [SerializeField] private string registrationEndIsoUtc = "2026-03-19T23:59:59Z";

    private bool isProcessing = false;
    private bool isCountingDown = false;
    private float countdownTimer;
    private DateTime registrationEndUtc;

    private void OnEnable()
    {
        registrationEndUtc = DateTime.Parse(
            registrationEndIsoUtc,
            null,
            System.Globalization.DateTimeStyles.AdjustToUniversal |
            System.Globalization.DateTimeStyles.AssumeUniversal
        );

        ShowInstructions();
        RefreshWalletUI();
        ResetButtonsFromWalletState();
    }

    private void Update()
    {
        UpdateRegistrationCountdown();

        if (!isCountingDown) return;

        countdownTimer -= Time.deltaTime;

        if (countdownTimer <= 0f)
        {
            isCountingDown = false;
            LaunchSpeedrun();
        }
        else if (countdownText != null)
        {
            countdownText.text = Mathf.CeilToInt(countdownTimer).ToString();
        }
    }

    public async void OnConnectWalletButton()
    {
        if (isProcessing) return;
        isProcessing = true;

        SetStatus("Connecting wallet...");

        bool connected = await WaitForWalletConnection(6f);

        if (!connected)
        {
            SetStatus("Wallet connection failed.");
            RefreshWalletUI();
            ResetButtonsFromWalletState();
            isProcessing = false;
            return;
        }

        RefreshWalletUI();

        HideAllActionButtons();

        if (connectButton != null) connectButton.SetActive(false);
        if (checkWalletButton != null) checkWalletButton.SetActive(true);
        if (disconnectWalletButton != null) disconnectWalletButton.SetActive(true);

        SetStatus("Wallet connected.");
        isProcessing = false;
    }

    public void OnDisconnectWalletButton()
    {
        Debug.Log("[SpeedrunScreen] Wallet disconnected");

        if (runPanel != null)
            runPanel.SetActive(false);

        if (instructionGroup != null)
            instructionGroup.SetActive(true);

        RefreshWalletUI();
        ResetButtonsFromWalletState();
        SetStatus("Wallet disconnected.");
    }

    public async void OnCheckWalletButton()
    {
        if (isProcessing) return;
        isProcessing = true;

        if (SolanaManager.Instance == null || !SolanaManager.Instance.IsWalletConnected())
        {
            SetStatus("Connect wallet first.");
            RefreshWalletUI();
            ResetButtonsFromWalletState();
            isProcessing = false;
            return;
        }

        SetStatus("Checking wallet...");

        bool alreadyEligible = await SolanaManager.Instance.IsEligibleForSeason(seasonId);

        HideAllActionButtons();

        if (disconnectWalletButton != null)
            disconnectWalletButton.SetActive(true);

        if (alreadyEligible)
        {
            ShowRunPanel();
            SetStatus("Already eligible.");
        }
        else
        {
            if (stakeButton != null) stakeButton.SetActive(true);
            SetStatus("Wallet checked. Stake required to enter.");
        }

        isProcessing = false;
    }

    public async void OnStakeButton()
    {
        if (isProcessing) return;
        isProcessing = true;

        if (SolanaManager.Instance == null || !SolanaManager.Instance.IsWalletConnected())
        {
            SetStatus("Connect wallet first.");
            ResetButtonsFromWalletState();
            isProcessing = false;
            return;
        }

        Debug.Log("[SpeedrunScreen] Stake button pressed.");
        Debug.Log("[SpeedrunScreen] Wallet: " + SolanaManager.Instance.GetWalletAddress());

        SetStatus("Staking 100 LID...");

        bool ok = await SolanaManager.Instance.StakeLIDForSeason(seasonId, 100_000_000UL);

        Debug.Log("[SpeedrunScreen] Stake result: " + ok);

        if (!ok)
        {
            SetStatus("Stake failed.");
            HideAllActionButtons();

            if (checkWalletButton != null) checkWalletButton.SetActive(true);
            if (disconnectWalletButton != null) disconnectWalletButton.SetActive(true);

            isProcessing = false;
            return;
        }

        SetStatus("Stake successful.");
        ShowRunPanel();
        isProcessing = false;
    }

    public async void OnStartRunButton()
    {
        if (isProcessing) return;
        isProcessing = true;

        if (!IsRegistrationClosed())
        {
            SetStatus("Registration is still open.");
            isProcessing = false;
            return;
        }

        SetStatus("Starting run...");

        bool ok = await SolanaManager.Instance.StartCompetitiveRun(seasonId, levelId);

        if (!ok)
        {
            SetStatus("Run start failed.");
            ShowRunPanel();
            isProcessing = false;
            return;
        }

        SetStatus("Run started.");
        StartCountdown();
        isProcessing = false;
    }

    private async Task<bool> WaitForWalletConnection(float timeoutSeconds)
    {
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            if (SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected())
            {
                return true;
            }

            await Task.Delay(250);
            elapsed += 0.25f;
        }

        return false;
    }

    private void RefreshWalletUI()
    {
        bool connected = SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected();

        if (walletAddressText != null)
        {
            walletAddressText.text = connected
                ? SolanaManager.Instance.GetWalletAddress()
                : "Not connected";
        }
    }

    private void ResetButtonsFromWalletState()
    {
        bool connected = SolanaManager.Instance != null && SolanaManager.Instance.IsWalletConnected();

        HideAllActionButtons();

        if (connectButton != null) connectButton.SetActive(!connected);
        if (checkWalletButton != null) checkWalletButton.SetActive(connected);
        if (disconnectWalletButton != null) disconnectWalletButton.SetActive(connected);

        if (!connected)
        {
            if (runPanel != null) runPanel.SetActive(false);
            if (instructionGroup != null) instructionGroup.SetActive(true);
        }
    }

    private void HideAllActionButtons()
    {
        if (connectButton != null) connectButton.SetActive(false);
        if (checkWalletButton != null) checkWalletButton.SetActive(false);
        if (stakeButton != null) stakeButton.SetActive(false);
        if (startRunButton != null) startRunButton.SetActive(false);
        if (disconnectWalletButton != null) disconnectWalletButton.SetActive(false);
    }

    private void ShowInstructions()
    {
        if (instructionGroup != null) instructionGroup.SetActive(true);
        if (runPanel != null) runPanel.SetActive(false);

        if (instructionText != null)
        {
            instructionText.text =
                "SPEEDRUN MODE\n\n" +
                "Connect wallet.\n" +
                "Check wallet.\n" +
                "Stake 100 LID.\n" +
                "After registration closes, start your run.";
        }
    }

    private void ShowRunPanel()
    {
        if (instructionGroup != null) instructionGroup.SetActive(false);
        if (runPanel != null) runPanel.SetActive(true);

        UpdateRunPanelState();
    }

    private void UpdateRunPanelState()
    {
        bool closed = IsRegistrationClosed();

        if (registrationCountdownText != null)
        {
            if (closed)
            {
                registrationCountdownText.text = "Registration closed. You can start your run.";
            }
            else
            {
                TimeSpan remaining = registrationEndUtc - DateTime.UtcNow;
                if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;

                registrationCountdownText.text =
                    $"Registration closes in: {remaining.Days:D2}d {remaining.Hours:D2}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s";
            }
        }

        if (startRunButton != null)
        {
            startRunButton.SetActive(closed);
        }
    }

    private void UpdateRegistrationCountdown()
    {
        if (runPanel != null && runPanel.activeSelf)
        {
            UpdateRunPanelState();
        }
    }

    private bool IsRegistrationClosed()
    {
        return DateTime.UtcNow >= registrationEndUtc;
    }

    private void StartCountdown()
    {
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
        MainMenu.SelectedLevel = levelId;
        GameManager.DeathCount = 0;
        SceneManager.LoadScene(levelSceneName);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = msg;
        }
    }
}