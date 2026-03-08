using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Types;

// ============================================================
//  SolanaManager.cs
//
//  SETUP:
//  1. Install Solana Unity SDK via Package Manager
//  2. Import Samples from Package Manager inspector
//  3. Drag the Web3 prefab from Samples into your scene
//  4. Add THIS script to the SAME GameObject as Web3
//  5. Copy AndroidManifest.xml for deep links
//
//  ALL KEYS ARE PRE-FILLED — no Inspector changes needed.
// ============================================================

public class SolanaManager : MonoBehaviour
{
    // --------------------------------------------------------
    //  SINGLETON
    // --------------------------------------------------------
    public static SolanaManager Instance { get; private set; }

    // --------------------------------------------------------
    //  ANCHOR PROGRAM — pre-filled, do not change
    // --------------------------------------------------------
    [Header("Anchor Program (pre-filled — do not change)")]
    public string ProgramId = "DSAwJkMCpdabrQS5zxDfAk3oxznKCLS5iABoc7GXcnnr";

    public string[] PoolPDAs = new string[]
    {
        "BQQ1E2AVuVyN78NaridUMKyjVPJX5TUxdWAZGbDykgj8", // Level 1
        "4dxAqjrr18aYAUgRQzTZFr6QTqobVZbL1DWgAPX9zdbQ", // Level 2
        "DHsBdHskDrjx2bYy4VL5YBVDBxn4vkKqovRJ2RUsKDBF"  // Level 3
    };

    // --------------------------------------------------------
    //  SOAR — pre-filled, do not change
    // --------------------------------------------------------
    [Header("SOAR (pre-filled — do not change)")]
    public string SOARGameKey = "F17zjfekRWEqdEkTVpQi6AZa1WCmXa4ZXtdmmSXFp3tf";

    public string[] SOARLeaderboardKeys = new string[]
    {
        "EZ36jCLw2aXTNE3DnfJZYomwbA1d6WAsM9AvjHthXtiS", // Level 1
        "CC8HsJVsCnFiQus9YmhdLheMd6DeYLsfzKP9sz7pVgmT", // Level 2
        "Dh38PNQvu4FGakqjg121ybZqvq3zfvpDmjFHtPndcR3e"  // Level 3
    };

    // --------------------------------------------------------
    //  SKR TOKEN — mainnet mint, pre-filled
    // --------------------------------------------------------
    [Header("SKR Token (pre-filled — mainnet only)")]
    public string SKRTokenMint = "SKRbvo6Gf7GondiT3BbTfuRDPqLWei4j2Qy2NPGZhW3";

    // --------------------------------------------------------
    //  EVENTS
    // --------------------------------------------------------
    public event Action<string> OnWalletConnected;
    public event Action OnWalletDisconnected;
    public event Action<string> OnError;

    // ============================================================
    //  UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================================================
    //  1. WALLET CONNECTION
    // ============================================================

    public async Task<string> ConnectWallet()
    {
        try
        {
            Debug.Log("[SolanaManager] Connecting wallet via MWA...");

            var account = await Web3.Instance.LoginWalletAdapter();

            if (account == null)
            {
                Debug.LogWarning("[SolanaManager] Login returned null — user may have cancelled.");
                return null;
            }

            string pubkey = Web3.Account.PublicKey.Key;
            Debug.Log($"[SolanaManager] Wallet connected: {pubkey}");
            OnWalletConnected?.Invoke(GetWalletAddress());

            bool isSKRHolder = await CheckSKRHolder();
            if (isSKRHolder)
            {
                Debug.Log("[SolanaManager] SKR holder detected — trigger cosmetic unlock.");
            }

            return pubkey;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] ConnectWallet error: {e.Message}");
            OnError?.Invoke("Wallet connection failed.");
            return null;
        }
    }

    public void DisconnectWallet()
    {
        Web3.Instance.Logout();
        OnWalletDisconnected?.Invoke();
        Debug.Log("[SolanaManager] Wallet disconnected.");
    }

    public bool IsWalletConnected()
    {
        return Web3.Account != null;
    }

    public string GetWalletAddress()
    {
        if (!IsWalletConnected()) return "";
        string key = Web3.Account.PublicKey.Key;
        if (key.Length <= 8) return key;
        return $"{key.Substring(0, 4)}...{key.Substring(key.Length - 4)}";
    }

    // ============================================================
    //  2. SOAR LEADERBOARD
    //  NOTE: SOAR client library not yet imported.
    //  SubmitTime logs locally. GetLeaderboard returns empty.
    //  Emmy can re-enable once Solana.Unity.Soar DLL is resolved.
    // ============================================================

    public async Task<bool> SubmitTime(int levelId, float timeSeconds)
    {
        if (!IsWalletConnected())
        {
            Debug.LogWarning("[SolanaManager] SubmitTime: wallet not connected.");
            return false;
        }

        ulong scoreMs = (ulong)(timeSeconds * 1000f);
        Debug.Log($"[SolanaManager] Time recorded: {scoreMs}ms for level {levelId}. SOAR submission pending.");
        await Task.CompletedTask;
        return true;
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboard(int levelId)
    {
        var entries = new List<LeaderboardEntry>();
        Debug.Log($"[SolanaManager] Leaderboard fetch for level {levelId} — SOAR client pending.");
        await Task.CompletedTask;
        return entries;
    }

    // ============================================================
    //  3. COMPETITIVE RUN (Anchor program) — fully functional
    // ============================================================

    public async Task<bool> EnterCompetitiveRun(int levelId)
    {
        if (!IsWalletConnected())
        {
            Debug.LogWarning("[SolanaManager] EnterCompetitiveRun: wallet not connected.");
            return false;
        }

        try
        {
            Debug.Log($"[SolanaManager] Entering competitive run for level {levelId}...");

            var programId = new PublicKey(ProgramId);
            var playerKey = Web3.Account.PublicKey;
            var poolKey = new PublicKey(PoolPDAs[levelId - 1]);

            byte[] levelSeed = new byte[] { (byte)levelId };
            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("run"), levelSeed, playerKey.KeyBytes },
                programId,
                out PublicKey playerRunPDA,
                out _
            );

            var data = new List<byte>();
            data.AddRange(GetDiscriminator("enter_run"));
            data.Add((byte)levelId);

            var keys = new List<AccountMeta>
            {
                AccountMeta.Writable(poolKey, false),
                AccountMeta.Writable(playerRunPDA, false),
                AccountMeta.Writable(playerKey, true),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false)
            };

            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>()
                {
                    new TransactionInstruction { ProgramId = programId, Keys = keys, Data = data.ToArray() }
                },
                RecentBlockHash = await Web3.BlockHash()
            };

            var result = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);

            if (result.WasSuccessful)
            {
                Debug.Log($"[SolanaManager] Competitive run entered. Tx: {result.Result}");
                return true;
            }
            else
            {
                Debug.LogError($"[SolanaManager] EnterCompetitiveRun failed: {result.Reason}");
                OnError?.Invoke("Failed to enter run. Check your SOL balance.");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] EnterCompetitiveRun error: {e.Message}");
            OnError?.Invoke($"Error entering run: {e.Message}");
            return false;
        }
    }

    public async Task<bool> SubmitCompetitiveTime(int levelId, float timeSeconds)
    {
        if (!IsWalletConnected())
        {
            Debug.LogWarning("[SolanaManager] SubmitCompetitiveTime: wallet not connected.");
            return false;
        }

        try
        {
            ulong timeMs = (ulong)(timeSeconds * 1000f);
            Debug.Log($"[SolanaManager] Submitting competitive time {timeMs}ms for level {levelId}...");

            var programId = new PublicKey(ProgramId);
            var playerKey = Web3.Account.PublicKey;

            byte[] levelSeed = new byte[] { (byte)levelId };
            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("run"), levelSeed, playerKey.KeyBytes },
                programId,
                out PublicKey playerRunPDA,
                out _
            );

            var data = new List<byte>();
            data.AddRange(GetDiscriminator("submit_time"));
            data.Add((byte)levelId);
            data.AddRange(BitConverter.GetBytes(timeMs));

            var keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerRunPDA, false),
                AccountMeta.ReadOnly(playerKey, true)
            };

            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>()
                {
                    new TransactionInstruction { ProgramId = programId, Keys = keys, Data = data.ToArray() }
                },
                RecentBlockHash = await Web3.BlockHash()
            };

            var result = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);

            if (result.WasSuccessful)
            {
                Debug.Log($"[SolanaManager] Competitive time submitted: {timeMs}ms. Tx: {result.Result}");
                await SubmitTime(levelId, timeSeconds);
                return true;
            }
            else
            {
                Debug.LogError($"[SolanaManager] SubmitCompetitiveTime failed: {result.Reason}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] SubmitCompetitiveTime error: {e.Message}");
            return false;
        }
    }

    // ============================================================
    //  4. SKR TOKEN CHECK — fully functional
    // ============================================================

    public async Task<bool> CheckSKRHolder()
    {
        if (!IsWalletConnected() || string.IsNullOrEmpty(SKRTokenMint))
            return false;

        try
        {
            var skrMint = new PublicKey(SKRTokenMint);

            var tokenAccounts = await Web3.Rpc.GetTokenAccountsByOwnerAsync(
                Web3.Account.PublicKey,
                null,
                skrMint
            );

            if (tokenAccounts.WasSuccessful && tokenAccounts.Result?.Value != null)
            {
                foreach (var account in tokenAccounts.Result.Value)
                {
                    var balance = account.Account.Data.Parsed.Info.TokenAmount.AmountUlong;
                    if (balance > 0)
                    {
                        Debug.Log($"[SolanaManager] SKR holder confirmed. Balance: {balance}");
                        return true;
                    }
                }
            }

            Debug.Log("[SolanaManager] No SKR tokens found.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] CheckSKRHolder error: {e.Message}");
            return false;
        }
    }

    // ============================================================
    //  HELPERS
    // ============================================================

    private string GetSOARLeaderboardKey(int levelId)
    {
        int idx = levelId - 1;
        if (idx < 0 || idx >= SOARLeaderboardKeys.Length) return "";
        return SOARLeaderboardKeys[idx];
    }

    private static byte[] GetDiscriminator(string instructionName)
    {
        var hash = System.Security.Cryptography.SHA256.Create()
            .ComputeHash(System.Text.Encoding.UTF8.GetBytes($"global:{instructionName}"));
        var disc = new byte[8];
        Array.Copy(hash, disc, 8);
        return disc;
    }

    private static async Task<bool> IsPdaInitialized(PublicKey pda)
    {
        var info = await Web3.Rpc.GetAccountInfoAsync(pda);
        return info.WasSuccessful && info.Result?.Value != null;
    }

    private static string AbbreviateAddress(string address)
    {
        if (string.IsNullOrEmpty(address) || address.Length <= 8) return address;
        return $"{address.Substring(0, 4)}...{address.Substring(address.Length - 4)}";
    }

    public static string FormatTime(ulong timeMs)
    {
        var ts = TimeSpan.FromMilliseconds(timeMs);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }
}

// ============================================================
//  DATA MODELS
// ============================================================

[Serializable]
public class LeaderboardEntry
{
    public int Rank;
    public string WalletAddress;
    public ulong TimeMs;
    public string TimeFormatted;
}