using System.Collections.Generic;
using System;
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
    [Header("Random power-up placements")]
    public GameObject[] powerUpPrefabs;
    public int powerUpSeed = 84271;

    private const float LaneSpacing = 3.5f;
    private const int TotalSequenceSlots = 60;
    private const float LevelOneEndZ = 400f;
    private const float LevelTwoEndZ = 800f;
    private const float TrackEndZ = 1200f;
    private readonly List<RunnerTrackSection> activeSections = new List<RunnerTrackSection>();
    private bool initialized;
    private float lastPowerUpZ = -1000f;
    private static readonly Material[] RuntimePowerUpMaterials = new Material[5];

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
            player.autoRunSpeed = Mathf.Clamp(8f + playerZ / 500f, 8f, 10.4f);
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
        TrySpawnPowerUp(instance, nextSequenceSlot);
        activeSections.Add(instance);
        nextStartZ += instance.sectionLength;
        nextSequenceSlot++;
    }

    private RunnerTrackSection GetNextPrefab(int slot)
    {
        if (slot >= 0 && slot <= 18) return forgottenEntranceSections[slot % forgottenEntranceSections.Length];
        if (slot == 19) return transitionToCrystalCaverns;
        if (slot >= 20 && slot <= 39) return crystalCavernsSections[(slot - 20) % crystalCavernsSections.Length];
        if (slot == 40) return transitionToFinalTreasureVault;
        if (slot >= 41 && slot <= 59) return finalTreasureVaultSections[(slot - 41) % finalTreasureVaultSections.Length];
        return null;
    }

    private void TrySpawnPowerUp(RunnerTrackSection section, int slot)
    {
        if (section == null) return;
        System.Random random = new System.Random(powerUpSeed ^ (slot * 7919));
        float centerZ = section.StartZ + section.sectionLength * 0.5f;
        if (random.NextDouble() > 0.43 || centerZ - lastPowerUpZ < 70f) return;

        int[] lanes = { -1, 0, 1 };
        int laneStart = random.Next(lanes.Length);
        int selectedLane = 99;
        for (int i = 0; i < lanes.Length; i++)
        {
            int lane = lanes[(laneStart + i) % lanes.Length];
            bool blocked = false;
            foreach (RunnerLaneHazard hazard in section.GetComponentsInChildren<RunnerLaneHazard>(true))
            {
                if (hazard.laneIndex == lane && Mathf.Abs(hazard.transform.position.z - centerZ) < 7.5f) { blocked = true; break; }
            }
            if (!blocked) { selectedLane = lane; break; }
        }
        if (selectedLane == 99) return;

        int powerUpIndex = random.Next(5);
        Collectible.PowerUpType type = (Collectible.PowerUpType)(powerUpIndex + 1);
        GameObject pickup;
        if (powerUpPrefabs != null && powerUpPrefabs.Length == 5 && powerUpPrefabs[powerUpIndex] != null)
            pickup = Instantiate(powerUpPrefabs[powerUpIndex], section.transform);
        else
            pickup = CreatePowerUpPickup(type, section.transform);
        if (pickup == null) return;
        pickup.name = "PowerUp_" + type + "_Slot_" + slot;
        pickup.transform.localPosition = new Vector3(selectedLane * LaneSpacing, 1f, section.sectionLength * 0.5f);
        lastPowerUpZ = centerZ;
    }

    private static GameObject CreatePowerUpPickup(Collectible.PowerUpType type, Transform parent)
    {
        PrimitiveType shape;
        Color color;
        switch (type)
        {
            case Collectible.PowerUpType.CoinMagnet: shape = PrimitiveType.Cylinder; color = new Color(1f, 0.55f, 0.08f); break;
            case Collectible.PowerUpType.Shield: shape = PrimitiveType.Sphere; color = new Color(0.1f, 0.8f, 1f); break;
            case Collectible.PowerUpType.SpeedBoost: shape = PrimitiveType.Capsule; color = new Color(1f, 0.25f, 0.06f); break;
            case Collectible.PowerUpType.ScoreMultiplier: shape = PrimitiveType.Cube; color = new Color(0.7f, 0.2f, 1f); break;
            case Collectible.PowerUpType.TreasureBonus: shape = PrimitiveType.Cylinder; color = new Color(1f, 0.82f, 0.08f); break;
            default: shape = PrimitiveType.Sphere; color = Color.white; break;
        }
        GameObject pickup = GameObject.CreatePrimitive(shape);
        pickup.transform.SetParent(parent, false);
        pickup.transform.localScale = shape == PrimitiveType.Cylinder ? new Vector3(0.85f, 0.18f, 0.85f) : Vector3.one * 0.72f;
        Collider collider = pickup.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;
        Renderer renderer = pickup.GetComponent<Renderer>();
        if (renderer != null)
        {
            int index = (int)type - 1;
            if (RuntimePowerUpMaterials[index] == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                RuntimePowerUpMaterials[index] = new Material(shader) { color = color };
            }
            renderer.sharedMaterial = RuntimePowerUpMaterials[index];
        }
        Collectible collectible = pickup.AddComponent<Collectible>();
        collectible.powerUpType = type;
        collectible.treasureType = Collectible.TreasureType.Coin;
        collectible.points = 0;
        return pickup;
    }

    public void ResetToCheckpoint(float checkpointZ)
    {
        lastPowerUpZ = checkpointZ - 1000f;
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
