using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public TMP_Text scoreText;
    public TMP_Text healthText;
    public TMP_Text treasureKeysText;
    public TMP_Text crystalCountText;
    public TMP_Text stageText;
    public TMP_Text stageProgressText;
    public Slider stageProgressSlider;
    public GameObject winPanel;
    public GameObject gameOverPanel;
    public GameObject lockedTreasurePanel;
    public TMP_Text finalStatsText;
    public TMP_Text lockedTreasureText;
    public TMP_Text interactionHintText;
    public GameObject mainMenuPanel, instructionsPanel, pausePanel, introPanel;
    public Button startAdventureButton;
    public Button playButton, instructionsButton, exitButton, instructionsBackButton;
    public Button resumeButton, pauseRestartButton, pauseMainMenuButton;
    public Button gameOverRestartButton, victoryRestartButton;
    public Button gameOverMainMenuButton, victoryMainMenuButton;
    [Min(1)] public int startingHealth = 3;
    [Min(0f)] public float damageCooldown = 1f;
    private float nextDamageAllowedTime;
    [Header("Checkpoints")]
    public float lastCheckpointZ = 0f;
    public bool hasCheckpoint = true;
    [Min(0)] public int maxCheckpointRespawns = 2;
    private int checkpointRespawnsRemaining;
    private static readonly float[] CheckpointZs = new float[] { 0f, 80f, 180f, 250f, 360f, 420f };
    [Min(1)] public int requiredTreasureKeys = 2;
    [Header("Runner Power-ups")]
    [Min(0.1f)] public float coinMagnetDuration = 10f;
    [Min(1f)] public float coinMagnetRadius = 12f;
    [Min(1f)] public float coinMagnetSpeed = 16f;
    [Min(0.1f)] public float shieldDuration = 12f;
    [Min(0.1f)] public float speedBoostDuration = 7f;
    [Min(1f)] public float speedBoostMultiplier = 1.5f;
    [Min(0.1f)] public float scoreMultiplierDuration = 12f;
    [Min(1)] public int scoreMultiplierValue = 2;
    [Min(0)] public int treasureBonusPoints = 250;
    private float coinMagnetExpiresAt, shieldExpiresAt, speedBoostExpiresAt, scoreMultiplierExpiresAt;
    private Transform cachedPlayerTransform;
    public bool CoinMagnetActive => Time.time < coinMagnetExpiresAt;
    public bool ShieldActive => Time.time < shieldExpiresAt;
    public float SpeedBoostMultiplier => Time.time < speedBoostExpiresAt ? speedBoostMultiplier : 1f;
    public int ActiveScoreMultiplier => Time.time < scoreMultiplierExpiresAt ? scoreMultiplierValue : 1;
    public float CoinMagnetRadius => coinMagnetRadius;
    public float CoinMagnetSpeed => coinMagnetSpeed;
    public Transform PlayerTransform
    {
        get
        {
            if (cachedPlayerTransform == null)
            {
                PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
                if (player != null) cachedPlayerTransform = player.transform;
            }
            return cachedPlayerTransform;
        }
    }
    public int Score { get; private set; }
    public int Crystals { get; private set; }
    public int Health { get; private set; }
    public int TreasureKeys { get; private set; }
    public bool HasAllTreasureKeys => TreasureKeys >= requiredTreasureKeys;
    public bool IsFinished { get; private set; }
    private readonly HashSet<int> collectedTreasureKeyIds = new HashSet<int>();
    private enum FlowState { Intro, Menu, Playing, Instructions, Paused, GameOver, Victory }
    private FlowState flowState;
    private static bool skipMenuOnNextLoad;
    private static bool skipIntroOnNextLoad;
    public bool IsInputLocked => flowState != FlowState.Playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; skipMenuOnNextLoad = false; skipIntroOnNextLoad = false; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Health = startingHealth;
        checkpointRespawnsRemaining = Mathf.Max(0, maxCheckpointRespawns);
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (lockedTreasurePanel != null) lockedTreasurePanel.SetActive(false);
        if (skipMenuOnNextLoad) { skipMenuOnNextLoad = false; skipIntroOnNextLoad = true; EnterPlayingState(); }
        else if (!skipIntroOnNextLoad) { skipIntroOnNextLoad = true; EnterIntroState(); }
        else EnterMenuState();
        WireButtons();
        RefreshUI();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (flowState == FlowState.Playing && Keyboard.current.escapeKey.wasPressedThisFrame) EnterPauseState();
        else if (flowState == FlowState.Paused && Keyboard.current.escapeKey.wasPressedThisFrame) EnterPlayingState();
        if (flowState == FlowState.Playing) UpdateCheckpoint();
    }

    private void UpdateCheckpoint()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;
        float z = player.transform.position.z;
        for (int i = 0; i < CheckpointZs.Length; i++)
        {
            if (z >= CheckpointZs[i] && CheckpointZs[i] > lastCheckpointZ)
            {
                lastCheckpointZ = CheckpointZs[i];
                hasCheckpoint = true;
                AudioManager.Play("sfx_checkpoint");
            }
        }
    }

    private void RespawnAtCheckpoint()
    {
        Health = startingHealth;
        nextDamageAllowedTime = Time.time + Mathf.Max(0f, damageCooldown);
        ResetPowerUps();
        RunnerTrackManager track = FindFirstObjectByType<RunnerTrackManager>();
        if (track != null) track.ResetToCheckpoint(lastCheckpointZ);
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) pm.ResetToCentreLane();
        IsFinished = false;
        flowState = FlowState.Playing;
        Time.timeScale = 1f;
        SetPanels(null);
        RefreshUI();
    }

    private void WireButtons()
    {
        Bind(playButton, StartFreshGame); Bind(instructionsButton, ShowInstructions); Bind(exitButton, ExitGame);
        Bind(startAdventureButton, StartFreshGame);
        Bind(instructionsBackButton, EnterMenuState); Bind(resumeButton, EnterPlayingState);
        Bind(pauseRestartButton, RestartGame); Bind(pauseMainMenuButton, ReturnToMainMenu);
        Bind(gameOverRestartButton, RestartGame); Bind(victoryRestartButton, RestartGame);
        Bind(gameOverMainMenuButton, ReturnToMainMenu); Bind(victoryMainMenuButton, ReturnToMainMenu);
    }
    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    { if (button != null) { button.onClick.RemoveListener(action); button.onClick.AddListener(action); } }

    public void StartFreshGame()
    {
        Time.timeScale = 1f; Score = 0; Crystals = 0; Health = startingHealth; TreasureKeys = 0;
        collectedTreasureKeyIds.Clear(); nextDamageAllowedTime = 0f; IsFinished = false;
        lastCheckpointZ = 0f; hasCheckpoint = true; checkpointRespawnsRemaining = Mathf.Max(0, maxCheckpointRespawns);
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            Vector3 p = pm.transform.position; p.x = 0f; pm.transform.position = p;
        }
        ResetPowerUps();
        if (winPanel != null) winPanel.SetActive(false); if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (lockedTreasurePanel != null) lockedTreasurePanel.SetActive(false);
        RefreshUI(); SetRunnerProgress(1, "Forgotten Entrance", 0f); EnterPlayingState();
    }
    public void ShowInstructions() { SetFlowPanel(FlowState.Instructions, instructionsPanel); }
    private void EnterMenuState() { SetFlowPanel(FlowState.Menu, mainMenuPanel); Time.timeScale = 0f; }
    private void EnterIntroState() { SetFlowPanel(FlowState.Intro, introPanel); Time.timeScale = 0f; }
    public void ShowMainMenu() { EnterMenuState(); }
    private void EnterPlayingState() { flowState = FlowState.Playing; SetPanels(null); Time.timeScale = 1f; }
    public void ResumeGame() { if (flowState == FlowState.Paused) EnterPlayingState(); }
    private void EnterPauseState() { SetFlowPanel(FlowState.Paused, pausePanel); Time.timeScale = 0f; }
    private void SetFlowPanel(FlowState state, GameObject panel) { flowState = state; SetPanels(panel); }
    private void SetPanels(GameObject active)
    {
        if (introPanel != null) introPanel.SetActive(introPanel == active);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenuPanel == active);
        if (instructionsPanel != null) instructionsPanel.SetActive(instructionsPanel == active);
        if (pausePanel != null) pausePanel.SetActive(pausePanel == active);
    }
    public void ReturnToMainMenu() { Time.timeScale = 1f; skipMenuOnNextLoad = false; skipIntroOnNextLoad = false; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
    public void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void SetInteractionHint(string message) { if (interactionHintText != null) interactionHintText.text = message; }

    public void AddScore(int amount)
    {
        if (IsFinished) return;
        Score += Mathf.Max(0, amount) * ActiveScoreMultiplier;
        RefreshUI();
    }

    public void AddCollectible(Collectible.TreasureType type, int points)
    {
        if (IsFinished) return;
        if (type == Collectible.TreasureType.BlueCrystal || type == Collectible.TreasureType.GoldCrystal)
            Crystals++;
        Score += Mathf.Max(0, points) * ActiveScoreMultiplier;
        RefreshUI();
    }

    public void ActivatePowerUp(Collectible.PowerUpType type)
    {
        if (IsFinished || type == Collectible.PowerUpType.None) return;
        float now = Time.time;
        switch (type)
        {
            case Collectible.PowerUpType.CoinMagnet:
                coinMagnetExpiresAt = now + Mathf.Max(0.1f, coinMagnetDuration);
                SetInteractionHint("Coin Magnet active — nearby coins are drawn to you!");
                break;
            case Collectible.PowerUpType.Shield:
                shieldExpiresAt = now + Mathf.Max(0.1f, shieldDuration);
                SetInteractionHint("Shield active — it will block your next obstacle hit.");
                break;
            case Collectible.PowerUpType.SpeedBoost:
                speedBoostExpiresAt = now + Mathf.Max(0.1f, speedBoostDuration);
                SetInteractionHint("Speed Boost active!");
                break;
            case Collectible.PowerUpType.ScoreMultiplier:
                scoreMultiplierExpiresAt = now + Mathf.Max(0.1f, scoreMultiplierDuration);
                SetInteractionHint("Score x" + Mathf.Max(1, scoreMultiplierValue) + " active!");
                break;
            case Collectible.PowerUpType.TreasureBonus:
                AddScore(treasureBonusPoints);
                SetInteractionHint("Treasure bonus: +" + treasureBonusPoints + " points!");
                break;
        }
    }

    public bool TryAbsorbDamage()
    {
        if (!ShieldActive) return false;
        shieldExpiresAt = 0f;
        SetInteractionHint("Your shield absorbed the hit!");
        return true;
    }

    private void ResetPowerUps()
    {
        coinMagnetExpiresAt = shieldExpiresAt = speedBoostExpiresAt = scoreMultiplierExpiresAt = 0f;
        cachedPlayerTransform = null;
    }

    public bool CollectTreasureKey(int keyId)
    {
        string rejectionReason = null;
        if (IsFinished) rejectionReason = "game-finished";
        else if (keyId < 0 || keyId >= requiredTreasureKeys) rejectionReason = "keyId-out-of-range";
        else if (collectedTreasureKeyIds.Contains(keyId)) rejectionReason = "duplicate-keyId";
        if (rejectionReason != null)
        {
            Debug.Log("[KEY-DIAG] key-rejected keyId=" + keyId + " reason=" + rejectionReason + " uniqueCollected=" + collectedTreasureKeyIds.Count, this);
            return false;
        }
        collectedTreasureKeyIds.Add(keyId);
        TreasureKeys = collectedTreasureKeyIds.Count;
        Debug.Log("[KEY-DIAG] key-accepted keyId=" + keyId + " uniqueCollected=" + TreasureKeys, this);
        RefreshUI();
        return true;
    }

    public void SetAdventureStage(int stageNumber, string stageName)
    {
        if (stageText != null) stageText.text = "LEVEL " + Mathf.Clamp(stageNumber, 1, 3) + "/3  •  " + stageName;
    }

    public void SetRunnerProgress(int stageNumber, string stageName, float normalizedProgress)
    {
        SetAdventureStage(stageNumber, stageName);
        normalizedProgress = Mathf.Clamp01(normalizedProgress);
        if (stageProgressSlider != null) stageProgressSlider.value = normalizedProgress;
        if (stageProgressText != null) stageProgressText.text = "STAGE " + Mathf.RoundToInt(normalizedProgress * 100f) + "%";
    }

    public void TakeDamage(int amount = 1)
    {
        if (IsFinished || Time.time < nextDamageAllowedTime) return;
        nextDamageAllowedTime = Time.time + Mathf.Max(0f, damageCooldown);
        Health = Mathf.Max(0, Health - Mathf.Max(1, amount));
        RefreshUI();
        if (Health <= 0)
        {
            if (hasCheckpoint && checkpointRespawnsRemaining > 0) { checkpointRespawnsRemaining--; RespawnAtCheckpoint(); }
            else FinishGame(false);
        }
    }

    public void WinGame() { if (!IsFinished) FinishGame(true); }

    public void ShowLockedTreasure()
    {
        if (IsFinished) return;
        if (interactionHintText != null) { int missing = Mathf.Max(0, requiredTreasureKeys - TreasureKeys); interactionHintText.text = "Collect " + missing + " golden key" + (missing == 1 ? "" : "s") + " (" + TreasureKeys + "/" + requiredTreasureKeys + ") to unlock this treasure."; }
    }

    private void FinishGame(bool won)
    {
        IsFinished = true;
        int bestScore = Mathf.Max(PlayerPrefs.GetInt("LostCave_BestScore", 0), Score);
        PlayerPrefs.SetInt("LostCave_BestScore", bestScore);
        PlayerPrefs.Save();
        flowState = won ? FlowState.Victory : FlowState.GameOver;
        if (won) AudioManager.Play("sfx_victory");
        if (won && winPanel != null) winPanel.SetActive(true);
        if (!won && gameOverPanel != null) gameOverPanel.SetActive(true);
        SetPanels(null);
        if (finalStatsText != null) finalStatsText.text = "FINAL SCORE: " + Score + "\nBEST SCORE: " + bestScore + "\nTREASURE KEYS: " + TreasureKeys + "/" + requiredTreasureKeys;
        if (won)
        {
            if (stageProgressSlider != null) stageProgressSlider.value = 1f;
            if (stageProgressText != null) stageProgressText.text = "STAGE COMPLETE";
        }
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        skipMenuOnNextLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RefreshUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + Score;
        if (crystalCountText != null) crystalCountText.text = "Crystals: " + Crystals;
        if (healthText != null) healthText.text = "Health: " + Health;
        if (treasureKeysText != null) treasureKeysText.text = "TREASURE KEYS: " + TreasureKeys + "/" + requiredTreasureKeys;
    }
}

