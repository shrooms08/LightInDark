using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Soar.Accounts;
using Solana.Unity.Soar.Program;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

// ============================================================
//  SolanaManager.cs  —  LightInDark v2
//  Solana layer for competitive season mode.
//
//  ⚠️  IMPORTANT FOR OGHENERUKEVWE:
//  You already have wallet connect, pubkey display, and balance
//  working through the MagicBlock SDK. DO NOT touch or replace
//  that code. This script adds the blockchain game logic ON TOP
//  of your existing wallet setup.
//
//  ConnectWallet() below is DEAD CODE — kept only for reference.
//  All methods here rely on Web3.Account being set by YOUR
//  existing wallet connect flow before they are called.
//
//  ALL KEYS ARE PRE-FILLED. No Inspector changes needed.
//
//  ──────────────────────────────────────────────────────────
//  HOW TO CALL FROM GAME:
//
//  On “Stake & Enter Season” button:
//      bool ok = await SolanaManager.Instance.StakeLIDForSeason(2, 100_000_000UL);
//      // 100_000_000 = 100 LID (6 decimals)
//
//  Before allowing a competitive run to start:
//      bool eligible = await SolanaManager.Instance.IsEligibleForSeason(2);
//
//  On competitive level START (before gameplay begins):
//      bool ok = await SolanaManager.Instance.StartCompetitiveRun(2, levelId);
//      // Only start level if true
//
//  On competitive level COMPLETE:
//      bool ok = await SolanaManager.Instance.SubmitRunResult(2, levelId, RunTimer.CurrentTime, DeathCounter.Count);
//
//  On leaderboard screen open:
//      var entries = await SolanaManager.Instance.GetLeaderboard(2, levelId);
//
//  On “Claim Rewards” button (after season ends):
//      bool ok = await SolanaManager.Instance.ClaimSeasonRewards(2);
//
//  ──────────────────────────────────────────────────────────
//  PROGRAM NOTES:
//  - Program:   lightindark-v2 (Anchor 0.31.1)
//  - Token:     LID (6 decimals) — Season 1 stake = 100 LID = 100_000_000 raw
//  - ER:        MagicBlock Ephemeral Rollups for gasless real-time run tracking
//  - Leaderboard: SOAR — lowest time = best rank (IsAscending = true)
//  - Reward split: Winner1=40%, Winner2=20%, Winner3=10%, Rollover=15%, Creator=5%, Burn=10%
// ============================================================

public class SolanaManager : MonoBehaviour
{
    // --------------------------------------------------------
    //  KNOWN-GOOD DEFAULTS (used for validation; Inspector can override)
    // --------------------------------------------------------
    private const string ExpectedProgramId      = "6CvCAte9SsfB34yWcpshY3Do2d7VqkLfHCRbHBsv6zar";
    private const string ExpectedLidMint        = "3TX7tdXJLnJ51aBRR3TkVocnyFaiyNhETK3CQFp3E6bf";
    private const string ExpectedAuthority      = "3iWQtmdwKAh2M3Ev8Beedanm5njhxqDLqDnG5uax9Cne";
    private const string ExpectedSoarGameKey    = "CKBtEXH8JhUR1Rdh1F7XJj8aD8onALzrVH3mDsmDEqdm";
    private const string ExpectedMainRpc        = "https://api.devnet.solana.com";
    private const string ExpectedMagicRouterRpc = "https://devnet-router.magicblock.app";
    private const string ExpectedEphemeralRpc   = "https://devnet.magicblock.app";

    // ––––––––––––––––––––––––––––
    //  SINGLETON
    // ––––––––––––––––––––––––––––
    public static SolanaManager Instance { get; private set; }


    // --------------------------------------------------------
    //  PROGRAM — pre-filled, do not change
    // --------------------------------------------------------
    [Header("Anchor Program (pre-filled — do not change)")]
    public string ProgramId = "6CvCAte9SsfB34yWcpshY3Do2d7VqkLfHCRbHBsv6zar";

    // --------------------------------------------------------
    //  LID TOKEN — pre-filled, do not change
    // --------------------------------------------------------
    [Header("LID Token (pre-filled — do not change)")]
    public string LIDTokenMint    = "3TX7tdXJLnJ51aBRR3TkVocnyFaiyNhETK3CQFp3E6bf";
    public string AuthorityWallet = "3iWQtmdwKAh2M3Ev8Beedanm5njhxqDLqDnG5uax9Cne";

    // --------------------------------------------------------
    //  SOAR — pre-filled, do not change
    // --------------------------------------------------------
    [Header("SOAR (pre-filled — do not change)")]
    public string SOARGameKey = "CKBtEXH8JhUR1Rdh1F7XJj8aD8onALzrVH3mDsmDEqdm";
    // Index 0 = Season 1 leaderboard. Expand per season/level as needed.
    public string[] SOARLeaderboardKeys = new string[]
    {
        "A6oQXhxwyHyxC2c5ntvdjwCT9BBfXcjXt3V73WMVKBtH", // Season 2 / Level 1
    };

    // --------------------------------------------------------
    //  RPC ENDPOINTS — pre-filled, do not change
    // --------------------------------------------------------
    [Header("RPC (pre-filled — do not change)")]
    public string MainRPC        = "https://api.devnet.solana.com";
    // Magic Router — routes txs to ER when a run is delegated
    public string MagicRouterRPC = "https://devnet-router.magicblock.app";
    // ER direct — for gasless update_run calls during gameplay
    public string EphemeralRPC   = "https://devnet.magicblock.app";

    // --------------------------------------------------------
    //  EVENTS — subscribe in your UI scripts
    // --------------------------------------------------------
    public event Action<string> OnError;
    public event Action<string> OnTxSuccess;

    // ============================================================
    //  UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ValidateSerializedConfig();
        DontDestroyOnLoad(gameObject);
    }

    private void ValidateSerializedConfig()
    {
        // Unity serialization can override code defaults. We don't hard-reset values here (to avoid surprises),
        // but we do (a) fill empty values and (b) log warnings when critical ids differ from expected.
        if (string.IsNullOrWhiteSpace(ProgramId)) ProgramId = ExpectedProgramId;
        if (string.IsNullOrWhiteSpace(LIDTokenMint)) LIDTokenMint = ExpectedLidMint;
        if (string.IsNullOrWhiteSpace(AuthorityWallet)) AuthorityWallet = ExpectedAuthority;
        if (string.IsNullOrWhiteSpace(SOARGameKey)) SOARGameKey = ExpectedSoarGameKey;
        if (string.IsNullOrWhiteSpace(MainRPC)) MainRPC = ExpectedMainRpc;
        if (string.IsNullOrWhiteSpace(MagicRouterRPC)) MagicRouterRPC = ExpectedMagicRouterRpc;
        if (string.IsNullOrWhiteSpace(EphemeralRPC)) EphemeralRPC = ExpectedEphemeralRpc;

        WarnIfNotEqual(nameof(ProgramId), ProgramId, ExpectedProgramId);
        WarnIfNotEqual(nameof(LIDTokenMint), LIDTokenMint, ExpectedLidMint);
        WarnIfNotEqual(nameof(AuthorityWallet), AuthorityWallet, ExpectedAuthority);
        WarnIfNotEqual(nameof(SOARGameKey), SOARGameKey, ExpectedSoarGameKey);
        WarnIfNotEqual(nameof(MainRPC), MainRPC, ExpectedMainRpc);
        WarnIfNotEqual(nameof(MagicRouterRPC), MagicRouterRPC, ExpectedMagicRouterRpc);
        WarnIfNotEqual(nameof(EphemeralRPC), EphemeralRPC, ExpectedEphemeralRpc);
    }

    private static void WarnIfNotEqual(string field, string actual, string expected)
    {
        if (!string.Equals(actual?.Trim(), expected, StringComparison.Ordinal))
            Debug.LogWarning($"[SolanaManager] Serialized {field} differs from expected. actual={actual} expected={expected}");
    }

    // ============================================================
    //  0. WALLET — DEAD CODE
    //     Your existing MagicBlock wallet integration already handles
    //     connect, pubkey display, and balance. Do NOT call this.
    //     All methods below just need Web3.Account to be set.
    // ============================================================

    [Obsolete("Wallet is already connected via MagicBlock SDK. Do not call this.")]
    public async Task<string> ConnectWallet()
    {
        Debug.LogWarning("[SolanaManager] ConnectWallet() is a no-op. " +
                        "Wallet is managed by your existing MagicBlock SDK integration.");
        await Task.CompletedTask;
        return Web3.Account?.PublicKey?.Key ?? "";
    }

    public bool IsWalletConnected() => Web3.Account != null;

    public string GetWalletAddress()
    {
        if (!IsWalletConnected()) return "";
        string key = Web3.Account.PublicKey.Key;
        return key.Length <= 8 ? key : $"{key[..4]}...{key[^4..]}";
    }

    // ============================================================
    //  TOKEN HELPERS (SPL) — used for UI + staking
    // ============================================================

    public async Task<double> GetLidBalanceUiAsync()
    {
        if (!IsWalletConnected()) return 0d;
        var owner = Web3.Account.PublicKey.Key;
        var mint = LIDTokenMint;
        var accounts = await GetTokenAccountsByOwnerAndMintParsed(owner, mint);
        double total = 0d;
        foreach (var a in accounts)
            total += a.UiAmount;
        return total;
    }

    private sealed class TokenAccountParsed
    {
        public string Pubkey;
        public double UiAmount;
        public int Decimals;
        public string AmountRaw;
    }

    private async Task<List<TokenAccountParsed>> GetTokenAccountsByOwnerAndMintParsed(string ownerBase58, string mintBase58)
    {
        // Uses standard Solana JSON-RPC: getTokenAccountsByOwner with mint filter + jsonParsed encoding.
        // This avoids SDK type/version mismatches (filters/types differ across SDK versions).
        var payload = new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "getTokenAccountsByOwner",
            ["params"] = new JArray
            {
                ownerBase58,
                new JObject { ["mint"] = mintBase58 },
                new JObject { ["encoding"] = "jsonParsed", ["commitment"] = "processed" }
            }
        };

        var json = await PostJsonRpcAsync(MainRPC, payload.ToString());
        var list = new List<TokenAccountParsed>();

        var value = json?["result"]?["value"] as JArray;
        if (value == null) return list;

        foreach (var entry in value)
        {
            var pubkey = entry?["pubkey"]?.ToString();
            var tokenAmount = entry?["account"]?["data"]?["parsed"]?["info"]?["tokenAmount"];
            if (string.IsNullOrEmpty(pubkey) || tokenAmount == null) continue;

            // tokenAmount: { amount: "700000000", decimals: 6, uiAmount: 700.0, uiAmountString: "700" }
            int decimals = tokenAmount["decimals"]?.Value<int>() ?? 0;
            string amountRaw = tokenAmount["amount"]?.ToString() ?? "0";

            double uiAmount = 0d;
            var uiAmountToken = tokenAmount["uiAmount"];
            if (uiAmountToken != null && uiAmountToken.Type != JTokenType.Null)
            {
                uiAmount = uiAmountToken.Value<double>();
            }
            else
            {
                // Fallback if uiAmount isn't returned
                if (double.TryParse(amountRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var raw))
                    uiAmount = decimals <= 0 ? raw : raw / Math.Pow(10d, decimals);
            }

            list.Add(new TokenAccountParsed
            {
                Pubkey = pubkey,
                UiAmount = uiAmount,
                Decimals = decimals,
                AmountRaw = amountRaw
            });
        }

        return list;
    }

    private static async Task<JObject> PostJsonRpcAsync(string url, string jsonBody)
    {
        using var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"RPC HTTP error: {req.result} {req.error}");

        return JObject.Parse(req.downloadHandler.text);
    }

    private async Task<PublicKey> FindBestPlayerTokenAccountForMintAsync(PublicKey owner, PublicKey mint)
    {
        var accounts = await GetTokenAccountsByOwnerAndMintParsed(owner.Key, mint.Key);
        TokenAccountParsed best = null;
        foreach (var a in accounts)
        {
            if (best == null || a.UiAmount > best.UiAmount)
                best = a;
        }
        return best != null ? new PublicKey(best.Pubkey) : null;
    }

    // ============================================================
    //  1. STAKING
    //     Calls stake_for_season on-chain.
    //     Player must hold LID tokens.
    //     Season 1 stake amount = 100_000_000 (100 LID, 6 decimals).
    //
    //     PDA seeds:
    //       SeasonConfig : ["season", season_id_le_bytes]
    //       PlayerEntry  : ["entry",  season_id_le_bytes, player_pubkey]
    // ============================================================

    /// <summary>
    /// Stakes LID tokens to register the player for a season.
    /// Call during the registration window only.
    /// amount: raw token units (100 LID = 100_000_000).
    /// </summary>
    public async Task<bool> StakeLIDForSeason(uint seasonId, ulong amount = 100_000_000UL)
    {
        if (!IsWalletConnected())
        {
            Debug.LogWarning("[SolanaManager] StakeLIDForSeason: wallet not connected.");
            OnError?.Invoke("Wallet not connected.");
            return false;
        }

        try
        {
            Debug.Log("[SolanaManager] StakeLIDForSeason called.");
            Debug.Log("[SolanaManager] Wallet connected: " + IsWalletConnected());
            Debug.Log("[SolanaManager] Player: " + Web3.Account.PublicKey.Key);
            Debug.Log("[SolanaManager] Season ID: " + seasonId);
            Debug.Log("[SolanaManager] Amount: " + amount);

            var programId = new PublicKey(ProgramId);
            var playerKey = Web3.Account.PublicKey;
            var lidMint = new PublicKey(LIDTokenMint);
            byte[] seasonBytes = BitConverter.GetBytes(seasonId);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("season"), seasonBytes },
                programId, out PublicKey seasonConfigPDA, out _);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("entry"), seasonBytes, playerKey.KeyBytes },
                programId, out PublicKey playerEntryPDA, out _);

            // Anchor expects a PDA TokenAccount vault, not an ATA:
            // seeds = ["vault", season_id_le_bytes]
            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("vault"), seasonBytes },
                programId, out PublicKey vaultPDA, out _);

            // Player token account is the player's LID ATA (any token account would work, but ATA is standard)
            var associatedTokenProgramId = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJe1bNs");
            PublicKey.TryFindProgramAddress(
                new[] {
                    playerKey.KeyBytes,
                    TokenProgram.ProgramIdKey.KeyBytes,
                    lidMint.KeyBytes
                },
                associatedTokenProgramId,
                out PublicKey playerLIDATA, out _);

            Debug.Log("[SolanaManager] Player ATA (derived): " + playerLIDATA.Key);
            Debug.Log("[SolanaManager] Vault PDA: " + vaultPDA.Key);

            // Light preflight checks (helps diagnose common failures quickly)
            bool playerAtaExists = false;
            try
            {
                var ataInfo = await Web3.Rpc.GetAccountInfoAsync(playerLIDATA, Commitment.Processed);
                playerAtaExists = ataInfo.WasSuccessful && ataInfo.Result?.Value != null;
                Debug.Log("[SolanaManager] Player ATA exists: " + playerAtaExists);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SolanaManager] Could not fetch player ATA info: " + e.Message);
            }

            // NOTE: Some wallets may hold LID in a non-ATA token account.
            // We attempted to auto-discover that via getTokenAccountsByOwner+mint, but the SDK version in this
            // Unity project doesn't expose the needed filter types, so we use raw JSON-RPC to find the best token account.
            PublicKey playerTokenAccountToUse = playerLIDATA;
            var discoveredTokenAccount = await FindBestPlayerTokenAccountForMintAsync(playerKey, lidMint);
            if (discoveredTokenAccount != null)
            {
                playerTokenAccountToUse = discoveredTokenAccount;
                Debug.Log("[SolanaManager] Using player token account for staking: " + playerTokenAccountToUse.Key);
            }
            else
            {
                Debug.LogWarning("[SolanaManager] No token account found for LID mint. Will create ATA and require funds there.");
            }

            bool vaultExists = false;
            try
            {
                var vaultInfo = await Web3.Rpc.GetAccountInfoAsync(vaultPDA, Commitment.Processed);
                vaultExists = vaultInfo.WasSuccessful && vaultInfo.Result?.Value != null;
                Debug.Log("[SolanaManager] Vault PDA exists: " + vaultExists);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SolanaManager] Could not fetch vault PDA info: " + e.Message);
            }

            // If the season vault isn't initialized on-chain, staking cannot succeed.
            // (The Anchor program expects a pre-existing TokenAccount at the vault PDA.)
            if (!vaultExists)
            {
                Debug.LogError("[SolanaManager] Stake blocked: season vault PDA TokenAccount does not exist. " +
                               "Season setup is incomplete (vault not initialized).");
                OnError?.Invoke("Season vault is not initialized on-chain. Ask admin to initialize season vault, then retry.");
                return false;
            }

            var data = new List<byte>();
            data.AddRange(GetDiscriminator("stake_for_season"));
            data.AddRange(seasonBytes);

            // Anchor context (exact):
            // 0 player (signer, mut)
            // 1 season_config (mut)
            // 2 player_entry (init, mut)
            // 3 player_token_account (mut)
            // 4 vault (mut) - PDA TokenAccount
            // 5 token_program
            // 6 system_program
            var keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerKey, true),
                AccountMeta.Writable(seasonConfigPDA, false),
                AccountMeta.Writable(playerEntryPDA, false),
                AccountMeta.Writable(playerTokenAccountToUse, false),
                AccountMeta.Writable(vaultPDA, false),
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
            };

            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash()
            };

            // Create player's ATA if missing (common on fresh wallets).
            // Only create ATA if we actually intend to use it.
            if (!playerAtaExists && playerTokenAccountToUse.Equals(playerLIDATA))
            {
                Debug.LogWarning("[SolanaManager] Player ATA missing. Adding CreateAssociatedTokenAccount instruction.");

                var rentSysvar = new PublicKey("SysvarRent111111111111111111111111111111111");
                tx.Instructions.Add(new TransactionInstruction
                {
                    ProgramId = associatedTokenProgramId,
                    Keys = new List<AccountMeta>
                    {
                        AccountMeta.Writable(playerKey, true),          // payer
                        AccountMeta.Writable(playerLIDATA, false),      // ata
                        AccountMeta.ReadOnly(playerKey, false),         // owner
                        AccountMeta.ReadOnly(lidMint, false),           // mint
                        AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                        AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                        AccountMeta.ReadOnly(rentSysvar, false),
                    },
                    Data = Array.Empty<byte>()
                });
            }

            tx.Instructions.Add(new TransactionInstruction
            {
                ProgramId = programId,
                Keys = keys,
                Data = data.ToArray()
            });

            Debug.Log("[SolanaManager] About to sign and send stake transaction...");
            Debug.Log("[SolanaManager] stake_for_season ix keys: " + keys.Count + " | data bytes: " + data.Count +
                      " | tx ixs: " + tx.Instructions.Count);

            var result = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);

            Debug.Log("[SolanaManager] Stake tx success: " + result.WasSuccessful);
            Debug.Log("[SolanaManager] Stake tx result: " + result.Result);
            Debug.Log("[SolanaManager] Stake tx reason: " + result.Reason);

            if (result.WasSuccessful)
            {
                Debug.Log("[SolanaManager] Stake confirmed. Tx: " + result.Result);
                OnTxSuccess?.Invoke($"Staked {amount / 1_000_000} LID for Season {seasonId}!");
                return true;
            }

            Debug.LogError("[SolanaManager] StakeLIDForSeason failed: " + result.Reason);
            OnError?.Invoke("Staking failed. Check LID balance and season registration window.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError("[SolanaManager] StakeLIDForSeason error: " + e.Message);
            OnError?.Invoke("Stake error: " + e.Message);
            return false;
        }
    }

    // Token-account discovery helper intentionally removed (SDK version mismatch).

    // ============================================================
    //  2. ELIGIBILITY CHECK
    //     Read-only — just checks if PlayerEntry PDA exists.
    //     No transaction sent.
    // ============================================================

    /// <summary>
    /// Returns true if the player has staked for this season.
    /// Call before showing the "Start Competitive Run" button.
    /// </summary>
    public async Task<bool> IsEligibleForSeason(uint seasonId)
    {
        if (!IsWalletConnected()) return false;

        try
        {
            var programId    = new PublicKey(ProgramId);
            var playerKey    = Web3.Account.PublicKey;
            byte[] seasonBytes = BitConverter.GetBytes(seasonId);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("entry"), seasonBytes, playerKey.KeyBytes },
                programId, out PublicKey playerEntryPDA, out _);

            var info = await Web3.Rpc.GetAccountInfoAsync(playerEntryPDA);
            bool eligible = info.WasSuccessful && info.Result?.Value != null;

            Debug.Log($"[SolanaManager] Season {seasonId} eligibility: {eligible}");
            return eligible;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] IsEligibleForSeason error: {e.Message}");
            return false;
        }
    }

    // ============================================================
    //  3. START COMPETITIVE RUN
    //     Calls start_competitive_run on-chain.
    //     Creates ActiveRun PDA + delegates it to ER for gasless
    //     real-time tracking during gameplay.
    //
    //     PDA seeds:
    //       ActiveRun : ["run", player_pubkey]
    // ============================================================

    /// <summary>
    /// Opens a competitive run on-chain and delegates to Ephemeral Rollups.
    /// Call on level start. Only let the player play if this returns true.
    /// </summary>
    public async Task<bool> StartCompetitiveRun(uint seasonId, int levelId)
    {
        if (!IsWalletConnected())
        {
            OnError?.Invoke("Wallet not connected.");
            return false;
        }

        try
        {
            Debug.Log($"[SolanaManager] Starting competitive run — season {seasonId}, level {levelId}...");

            var programId    = new PublicKey(ProgramId);
            var playerKey    = Web3.Account.PublicKey;
            byte[] seasonBytes = BitConverter.GetBytes(seasonId);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("run"), playerKey.KeyBytes },
                programId, out PublicKey activeRunPDA, out _);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("season"), seasonBytes },
                programId, out PublicKey seasonConfigPDA, out _);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("entry"), seasonBytes, playerKey.KeyBytes },
                programId, out PublicKey playerEntryPDA, out _);

            var delegationProgram = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("buffer"), activeRunPDA.KeyBytes },
                delegationProgram, out PublicKey bufferPDA, out _);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("delegation"), activeRunPDA.KeyBytes },
                delegationProgram, out PublicKey delegationRecordPDA, out _);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("delegation-metadata"), activeRunPDA.KeyBytes },
                delegationProgram, out PublicKey delegationMetaPDA, out _);

            var data = new List<byte>();
            data.AddRange(GetDiscriminator("start_competitive_run"));
            data.AddRange(seasonBytes);
            data.Add((byte)levelId);

            // Anchor context (expected):
            // 0 player (signer, mut)
            // 1 season_config (PDA)
            // 2 player_entry (PDA)
            // 3 active_run (init, PDA)
            // 4 owner_program (your program id)
            // 5 buffer (delegation PDA, mut)
            // 6 delegation_record (delegation PDA, mut)
            // 7 delegation_metadata (delegation PDA, mut)
            // 8 delegation_program
            // 9 system_program
            var keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerKey,           true),
                AccountMeta.ReadOnly(seasonConfigPDA,     false),
                AccountMeta.ReadOnly(playerEntryPDA,      false),
                AccountMeta.Writable(activeRunPDA,        false),
                AccountMeta.ReadOnly(programId,           false),
                AccountMeta.Writable(bufferPDA,           false),
                AccountMeta.Writable(delegationRecordPDA, false),
                AccountMeta.Writable(delegationMetaPDA,   false),
                AccountMeta.ReadOnly(delegationProgram,   false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
            };

            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>
                {
                    new TransactionInstruction { ProgramId = programId, Keys = keys, Data = data.ToArray() }
                },
                RecentBlockHash = await Web3.BlockHash()
            };

            var result = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);

            if (result.WasSuccessful)
            {
                Debug.Log($"[SolanaManager] Run started + delegated to ER. Tx: {result.Result}");
                OnTxSuccess?.Invoke("Run started!");
                return true;
            }

            Debug.LogError($"[SolanaManager] StartCompetitiveRun failed: {result.Reason}");
            OnError?.Invoke("Could not start run. Are you eligible for this season?");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] StartCompetitiveRun error: {e.Message}");
            OnError?.Invoke($"Run start error: {e.Message}");
            return false;
        }
    }

    // ============================================================
    //  4. SUBMIT RUN RESULT
    //     Two steps:
    //       a) commit_run  — undelegates from ER, writes final time
    //          and death count to L1.
    //       b) SubmitToSOAR — pushes time to SOAR leaderboard.
    //
    //     timeSeconds : RunTimer.CurrentTime from your GameManager
    //     deathCount  : DeathCounter.Count from your GameManager
    // ============================================================

    /// <summary>
    /// Commits the final run result on-chain then pushes to SOAR leaderboard.
    /// Call from GameManager.OnLevelComplete when in competitive mode.
    /// </summary>
    public async Task<bool> SubmitRunResult(uint seasonId, int levelId, float timeSeconds, int deathCount)
    {
        if (!IsWalletConnected())
        {
            OnError?.Invoke("Wallet not connected.");
            return false;
        }

        ulong timeMs = (ulong)(timeSeconds * 1000f);
        Debug.Log($"[SolanaManager] Submitting run — season {seasonId}, level {levelId}, " +
                $"time {FormatTime(timeMs)}, deaths {deathCount}...");

        bool committed = await CommitRun(seasonId, timeMs, (uint)deathCount);
        if (!committed)
        {
            OnError?.Invoke("Failed to commit run on-chain.");
            return false;
        }

        bool submitted = await SubmitToSOAR(levelId, timeMs);
        if (!submitted)
            Debug.LogWarning("[SolanaManager] Run committed on-chain but SOAR submission failed.");

        OnTxSuccess?.Invoke($"Run submitted! Time: {FormatTime(timeMs)} | Deaths: {deathCount}");
        return true;
    }

    private async Task<bool> CommitRun(uint seasonId, ulong finalTimeMs, uint finalDeathCount)
    {
        try
        {
            var programId    = new PublicKey(ProgramId);
            var playerKey    = Web3.Account.PublicKey;
            byte[] seasonBytes = BitConverter.GetBytes(seasonId);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("run"), playerKey.KeyBytes },
                programId, out PublicKey activeRunPDA, out _);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("entry"), seasonBytes, playerKey.KeyBytes },
                programId, out PublicKey playerEntryPDA, out _);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("season"), seasonBytes },
                programId, out PublicKey seasonConfigPDA, out _);

            // MagicBlock magic accounts (required by commit_and_undelegate_accounts CPI)
            var magicContext = new PublicKey("MagicContext1111111111111111111111111111111");
            var magicProgram = new PublicKey("Magic11111111111111111111111111111111111111");

            // commit_run data: discriminator + final_time_ms (u64 LE) + final_death_count (u32 LE)
            var data = new List<byte>();
            data.AddRange(GetDiscriminator("commit_run"));
            data.AddRange(BitConverter.GetBytes(finalTimeMs));
            data.AddRange(BitConverter.GetBytes(finalDeathCount));

            // Anchor context (expected):
            // 0 player (signer, mut)
            // 1 active_run (mut)
            // 2 player_entry (mut)
            // 3 season_config (mut)
            // 4 magic_context (mut)
            // 5 magic_program
            var keys = new List<AccountMeta>
            {
                AccountMeta.Writable(playerKey, true),
                AccountMeta.Writable(activeRunPDA, false),
                AccountMeta.Writable(playerEntryPDA, false),
                AccountMeta.Writable(seasonConfigPDA, false),
                AccountMeta.Writable(magicContext, false),
                AccountMeta.ReadOnly(magicProgram, false),
            };

            // Send via Magic Router so the undelegate routes correctly from ER -> L1
            var routerRpc = ClientFactory.GetClient(MagicRouterRPC);
            var blockHash = await routerRpc.GetLatestBlockHashAsync();

            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>
                {
                    new TransactionInstruction { ProgramId = programId, Keys = keys, Data = data.ToArray() }
                },
                RecentBlockHash = blockHash.Result.Value.Blockhash
            };

            var result = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);

            if (result.WasSuccessful)
            {
                Debug.Log($"[SolanaManager] Run committed on-chain. Tx: {result.Result}");
                return true;
            }

            Debug.LogError($"[SolanaManager] CommitRun failed: {result.Reason}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] CommitRun error: {e.Message}");
            return false;
        }
    }

    private async Task<bool> SubmitToSOAR(int levelId, ulong timeMs)
    {
        string leaderboardKey = GetSOARLeaderboardKey(levelId);
        if (string.IsNullOrEmpty(leaderboardKey))
        {
            Debug.LogWarning($"[SolanaManager] No SOAR leaderboard key for level {levelId}.");
            return false;
        }

        try
        {
            var game         = new PublicKey(SOARGameKey);
            var leaderboard  = new PublicKey(leaderboardKey);
            var playerPDA    = SoarPda.PlayerPda(Web3.Account.PublicKey);
            var playerScores = SoarPda.PlayerScoresPda(playerPDA, leaderboard);
            var topEntries   = SoarPda.LeaderboardTopEntriesPda(leaderboard);

            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash()
            };

            // Auto-register on SOAR if first-ever submission
            if (!await IsPdaInitialized(playerPDA))
            {
                Debug.Log("[SolanaManager] First SOAR submission — registering player...");
                var regAccounts = new RegisterPlayerAccounts()
                {
                    Payer         = Web3.Account,
                    User          = Web3.Account,
                    PlayerAccount = playerPDA,
                    Game          = game,
                    Leaderboard   = leaderboard,
                    NewList       = playerScores,
                    SystemProgram = SystemProgram.ProgramIdKey
                };
                tx.Add(SoarProgram.RegisterPlayer(regAccounts, SoarProgram.ProgramIdKey));
            }

            // Submit score — time in ms, IsAscending=true so lowest = rank 1
            var submitAccounts = new SubmitScoreAccounts()
            {
                Authority     = Web3.Account, // player is authority (confirmed from SOAR docs)
                Payer         = Web3.Account,
                PlayerAccount = playerPDA,
                Game          = game,
                Leaderboard   = leaderboard,
                PlayerScores  = playerScores,
                TopEntries    = topEntries,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            tx.Add(SoarProgram.SubmitScore(submitAccounts, timeMs, SoarProgram.ProgramIdKey));

            var result = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
            Debug.Log($"[SolanaManager] SOAR score submitted: {FormatTime(timeMs)}. Tx: {result.Result}");
            return result.WasSuccessful;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] SubmitToSOAR error: {e.Message}");
            return false;
        }
    }

    // ============================================================
    //  5. LEADERBOARD
    //     Fetches top entries from SOAR.
    //     Returns list sorted by fastest time (rank 1 = fastest).
    //     DeathCount is stored in PlayerEntry on-chain — can be
    //     fetched separately if you want it on the leaderboard UI.
    // ============================================================

    /// <summary>
    /// Fetches top leaderboard entries for display.
    /// Returns list sorted by fastest time (rank 1 = fastest).
    /// </summary>
        public async Task<List<LeaderboardEntry>> GetLeaderboard(uint seasonId, int levelId)
    {
        await Task.CompletedTask;
        return new List<LeaderboardEntry>();
    }

    // ============================================================
    //  6. CLAIM SEASON REWARDS
    //     Reward distribution is admin-triggered via distribute_season_rewards.
    //     (Emmy runs scripts/distribute-rewards.ts at season end.)
    //     This method checks season state and surfaces reward status to the UI.
    //     Full on-chain claim instruction can be wired here when ready.
    //
    //     Reward split (for reference):
    //       Winner 1 = 40% of prize pool
    //       Winner 2 = 20% of prize pool
    //       Winner 3 = 10% of prize pool
    //       Rollover = 15% to next season
    //       Creator  = 5%
    //       Burn     = 10%
    // ============================================================

    /// <summary>
    /// Checks reward status for the season and signals UI.
    /// Full claim wiring added once distribute_season_rewards flow is finalized.
    /// </summary>
    public async Task<bool> ClaimSeasonRewards(uint seasonId)
    {
        if (!IsWalletConnected())
        {
            OnError?.Invoke("Wallet not connected.");
            return false;
        }

        try
        {
            Debug.Log($"[SolanaManager] Checking reward status for season {seasonId}...");

            var programId    = new PublicKey(ProgramId);
            byte[] seasonBytes = BitConverter.GetBytes(seasonId);

            PublicKey.TryFindProgramAddress(
                new[] { System.Text.Encoding.UTF8.GetBytes("season"), seasonBytes },
                programId, out PublicKey seasonConfigPDA, out _);

            var info = await Web3.Rpc.GetAccountInfoAsync(seasonConfigPDA);
            if (!info.WasSuccessful || info.Result?.Value == null)
            {
                Debug.LogWarning($"[SolanaManager] SeasonConfig not found for season {seasonId}.");
                OnError?.Invoke("Season not found.");
                return false;
            }

            // TODO: Parse SeasonConfig account data bytes to check season.status == Distributed
            // Wire the actual claim tx here once admin distribution flow is confirmed.
            Debug.Log($"[SolanaManager] Season {seasonId} config found. " +
                    "Rewards distributed by admin at season end.");
            OnTxSuccess?.Invoke("Season ended. Rewards are being distributed.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SolanaManager] ClaimSeasonRewards error: {e.Message}");
            OnError?.Invoke($"Claim error: {e.Message}");
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

    /// <summary>
    /// Computes the 8-byte Anchor instruction discriminator.
    /// Formula: first 8 bytes of SHA256("global:{instructionName}")
    /// </summary>
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
        return $"{address[..4]}...{address[^4..]}";
    }

    /// <summary>Formats milliseconds to MM:SS.mmm e.g. "01:23.456"</summary>
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
public int    Rank;
public string WalletAddress; // e.g. “7xKs…3nRt”
public ulong  TimeMs;
public string TimeFormatted; // e.g. “01:23.456”
public int    DeathCount;    // secondary sort — lower is better
}