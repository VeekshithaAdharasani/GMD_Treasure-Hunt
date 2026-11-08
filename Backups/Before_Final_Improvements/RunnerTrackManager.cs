using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunnerTrackManager : MonoBehaviour
{
    [Header("Gameplay")]
    public PlayerMovement player;
    public Transform sectionsContainer;
    public RunnerTrackVisuals sectionVisuals;

    [Header("Level 1 - Forgotten Entrance")]
    public RunnerTrackSection[] forgottenEntranceSections;
    public RunnerTrackSection transitionToCrystalCaverns;
    public RunnerTrackSection transitionToAncientRuins;

    [Header("Level 2 - Crystal Caverns")]
    public RunnerTrackSection[] ancientRuinsSections;
    public RunnerTrackSection transitionToLostTemple;
    public RunnerTrackSection transitionToFinalTreasureVault;

    [Header("Level 3 - Treasure Vault")]
    public RunnerTrackSection[] finalTreasureVaultSections;

    // The current three-stage route uses this saved catalog for level 2.
    [Header("Level 2 - Crystal Caverns")]
    public RunnerTrackSection[] crystalCavernsSections;
    public RunnerTrackSection transitionToForgottenEntrance;
    public RunnerTrackSection[] lostTempleSections;

    [Min(1)] public int sectionsPerBiome = 3;
    [Min(20f)] public float spawnAheadDistance = 80f;
    [Min(0f)] public float recycleBehindDistance = 40f;
    public float nextStartZ;
    public int nextSequenceSlot;

    private const float LaneSpacing = 3.5f;
    private const int TotalSequenceSlots = 25;
    private const float LevelOneEndZ = 180f;
    private const float LevelTwoEndZ = 360f;
    private const float TrackEndZ = 500f;
    private readonly List<RunnerTrackSection> activeSections = new List<RunnerTrackSection>();
    private bool initialized;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerMovement>();
        if (sectionsContainer == null) sectionsContainer = transform;
    }

    private void Start()
    {
        if (player == null || sectionsContainer == null || !CatalogIsComplete())
        {
            Debug.LogError("RunnerTrackManager needs the three-stage track catalogs and a player.", this);
            enabled = false;
            return;
        }

        activeSections.Clear();
        foreach (RunnerTrackSection section in sectionsContainer.GetComponentsInChildren<RunnerTrackSection>(true))
        {
            if (section == null || !section.gameObject.activeInHierarchy) continue;
            activeSections.Add(section);
        }
        activeSections.Sort((a, b) => a.StartZ.CompareTo(b.StartZ));

        if (sectionVisuals == null) sectionVisuals = GetComponent<RunnerTrackVisuals>();

        float lastEnd = 0f;
        for (int i = 0; i < activeSections.Count; i++)
        {
            RunnerTrackSection section = activeSections[i];
            if (!section.Validate(LaneSpacing, out string error))
            {
                Debug.LogError("Runner refused an invalid preview section: " + error, section);
                enabled = false;
                return;
            }
            if (i > 0 && Mathf.Abs(activeSections[i - 1].EndZ - section.StartZ) > 0.01f)
            {
                Debug.LogError("Runner preview track has a gap or overlap before " + section.name + ".", section);
                enabled = false;
                return;
            }
            if (section.sequenceSlot != i)
            {
                Debug.LogError("Runner preview sequence slots must start at zero and be contiguous.", section);
                enabled = false;
                return;
            }
            if (sectionVisuals != null) sectionVisuals.RefreshSection(section);
            lastEnd = section.EndZ;
        }

        // Match the suppression already applied to dynamically spawned sections: explicit scene pickups own key progression.
        foreach (RunnerTrackSection section in activeSections)
            foreach (TreasureKey embeddedKey in section.GetComponentsInChildren<TreasureKey>(true))
                embeddedKey.gameObject.SetActive(false);

        nextSequenceSlot = activeSections.Count;
        nextStartZ = activeSections.Count == 0 ? 0f : lastEnd;
        initialized = true;
        UpdateStageDisplay(player.transform.position.z);
    }

    private void Update()
    {
        if (!initialized || player == null || (GameManager.Instance != null && GameManager.Instance.IsFinished)) return;

        float playerZ = player.transform.position.z;
        UpdateStageDisplay(playerZ);
        // Gentle speed ramp that plateaus at a fair limit.
        if (player != null)
            player.autoRunSpeed = Mathf.Clamp(8f + playerZ / 500f, 8f, 15f);
        int spawnGuard = 0;
        while (nextSequenceSlot < TotalSequenceSlots && nextStartZ < playerZ + spawnAheadDistance && spawnGuard++ < 16 && enabled)
            SpawnNextSection();

        for (int i = activeSections.Count - 1; i >= 0; i--)
        {
            RunnerTrackSection section = activeSections[i];
            if (section == null) { activeSections.RemoveAt(i); continue; }
            if (section.EndZ < playerZ - recycleBehindDistance)
            {
                activeSections.RemoveAt(i);
                Destroy(section.gameObject);
            }
        }
    }

    private bool CatalogIsComplete()
    {
        return Catalog(forgottenEntranceSections) && transitionToCrystalCaverns != null &&
               Catalog(crystalCavernsSections) && transitionToFinalTreasureVault != null &&
               Catalog(finalTreasureVaultSections);
    }

    private static bool Catalog(RunnerTrackSection[] catalog) => catalog != null && catalog.Length == 3;

    private void SpawnNextSection()
    {
        RunnerTrackSection prefab = GetNextPrefab(nextSequenceSlot);
        if (prefab == null) { Debug.LogError("Runner sequence selected a missing track section.", this); enabled = false; return; }
        if (!prefab.Validate(LaneSpacing, out string error)) { Debug.LogError("Runner refused an invalid section: " + error, prefab); enabled = false; return; }

        RunnerTrackSection instance = Instantiate(prefab, sectionsContainer);
        instance.transform.SetPositionAndRotation(new Vector3(0f, 0f, nextStartZ), Quaternion.identity);
        instance.sequenceSlot = nextSequenceSlot;
        // The section prefabs retain legacy key props. The scene setup places the
        // two unique adventure keys, so prevent prefab-contained keys from
        // creating extra IDs as sections are spawned during the run.
        foreach (TreasureKey embeddedKey in instance.GetComponentsInChildren<TreasureKey>(true))
            embeddedKey.gameObject.SetActive(false);
        if (activeSections.Count > 0 && Mathf.Abs(activeSections[activeSections.Count - 1].EndZ - instance.StartZ) > 0.01f)
        {
            Debug.LogError("Runner generated a disconnected track seam at slot " + nextSequenceSlot + ".", instance);
            Destroy(instance.gameObject);
            enabled = false;
            return;
        }
        if (sectionVisuals != null) sectionVisuals.RefreshSection(instance);
        activeSections.Add(instance);
        nextStartZ += instance.sectionLength;
        nextSequenceSlot++;
    }

    private RunnerTrackSection GetNextPrefab(int slot)
    {
        if (slot >= 0 && slot <= 7) return forgottenEntranceSections[slot % forgottenEntranceSections.Length];
        if (slot == 8) return transitionToCrystalCaverns;
        if (slot >= 9 && slot <= 16) return crystalCavernsSections[(slot - 9) % crystalCavernsSections.Length];
        if (slot == 17) return transitionToFinalTreasureVault;
        if (slot >= 18 && slot <= 24) return finalTreasureVaultSections[(slot - 18) % finalTreasureVaultSections.Length];
        return null;
    }

    public void ResetToCheckpoint(float checkpointZ)
    {
        for (int i = activeSections.Count - 1; i >= 0; i--)
        {
            if (activeSections[i] != null) Destroy(activeSections[i].gameObject);
            activeSections.RemoveAt(i);
        }
        int slot = Mathf.Clamp(Mathf.FloorToInt(checkpointZ / 20f), 0, TotalSequenceSlots - 1);
        nextSequenceSlot = slot;
        nextStartZ = slot * 20f;
        if (player != null)
        {
            Vector3 p = player.transform.position;
            player.transform.position = new Vector3(0f, 1.5f, checkpointZ);
        }
        initialized = true;
    }

    private void UpdateStageDisplay(float z)
    {
        if (GameManager.Instance == null) return;
        if (z < LevelOneEndZ)
            GameManager.Instance.SetRunnerProgress(1, "Forgotten Entrance", Mathf.Clamp01(z / LevelOneEndZ));
        else if (z < LevelTwoEndZ)
            GameManager.Instance.SetRunnerProgress(2, "Crystal Caverns", Mathf.Clamp01((z - LevelOneEndZ) / (LevelTwoEndZ - LevelOneEndZ)));
        else
            GameManager.Instance.SetRunnerProgress(3, "Ancient Treasure Vault", Mathf.Clamp01((z - LevelTwoEndZ) / (TrackEndZ - LevelTwoEndZ)));
    }
}
