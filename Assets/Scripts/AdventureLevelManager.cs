using UnityEngine;

/// <summary>
/// AdventureLevelManager — Tracks which of the 3 adventure levels the player is currently in
/// and updates the GameManager stage HUD.  Lightweight; no RunnerTrackManager dependency.
///
/// Level 1 – The Forgotten Depths  (Regions 1 & 2: Forgotten Entrance + Crystal Caverns)
/// Level 2 – The Ancient Mystery   (Regions 3 & 4: Ancient Ruins + Lost Temple)
/// Level 3 – The Legendary Treasure(Region 5: Final Treasure Vault)
///
/// Usage:
///  - Attach this component to a persistent GameObject in the scene (e.g. "AdventureManager").
///  - Create trigger volumes at each region boundary and call SetRegion() from them,
///    OR let each section prefab's trigger child call it when the player enters.
/// </summary>
[DisallowMultipleComponent]
public sealed class AdventureLevelManager : MonoBehaviour
{
    public static AdventureLevelManager Instance { get; private set; }

    // ------------------------------------------------------------------ regions
    public enum Region
    {
        ForgottenEntrance  = 1,   // Level 1
        CrystalCaverns     = 2,   // Level 1
        AncientRuins       = 3,   // Level 2
        LostTemple         = 4,   // Level 2
        TreasureVault      = 5    // Level 3
    }

    private static readonly string[] RegionNames =
    {
        "",                        // index 0 unused
        "Forgotten Entrance",
        "Crystal Caverns",
        "Ancient Ruins",
        "Lost Temple",
        "Treasure Vault"
    };

    // ------------------------------------------------------------------ state
    public Region CurrentRegion { get; private set; } = Region.ForgottenEntrance;
    public int    CurrentLevel  { get; private set; } = 1;

    // ------------------------------------------------------------------ respawn checkpoints (set by Inspector or by script)
    [Header("Respawn Checkpoints (optional — assign in Inspector)")]
    [Tooltip("Checkpoint positions for each region (index 0–4 → regions 1–5).")]
    public Transform[] regionCheckpoints = new Transform[5];

    // ------------------------------------------------------------------ Unity
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Announce starting region in HUD
        AnnounceRegion(CurrentRegion);
    }

    // ------------------------------------------------------------------ public API
    /// <summary>Call this when the player enters a new environment region.</summary>
    public void SetRegion(Region region)
    {
        if (region == CurrentRegion) return;
        CurrentRegion = region;
        int regionIndex = (int)region;
        CurrentLevel   = regionIndex <= 2 ? 1 : (regionIndex <= 4 ? 2 : 3);
        AnnounceRegion(region);
    }

    /// <summary>Convenience overload — accepts the region number directly (1–5).</summary>
    public void SetRegion(int regionNumber)
    {
        if (regionNumber < 1 || regionNumber > 5) return;
        SetRegion((Region)regionNumber);
    }

    /// <summary>Returns the checkpoint Transform for the given region, or null.</summary>
    public Transform GetCheckpoint(Region region)
    {
        int idx = (int)region - 1;
        if (idx < 0 || idx >= regionCheckpoints.Length) return null;
        return regionCheckpoints[idx];
    }

    // ------------------------------------------------------------------ private
    private void AnnounceRegion(Region region)
    {
        int regionIndex = (int)region;
        int stageNumber = regionIndex;                       // stages 1–5 for the 5 regions
        string stageName = RegionNames[regionIndex];

        if (GameManager.Instance != null)
            GameManager.Instance.SetAdventureStage(stageNumber, stageName);

        Debug.Log($"[AdventureLevelManager] Entered Region {regionIndex}: {stageName}  (Level {CurrentLevel})");
    }
}
