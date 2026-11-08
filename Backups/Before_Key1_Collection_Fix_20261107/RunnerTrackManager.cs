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
    public GameObject coinPickupPrefab;

    private const float LaneSpacing = 3.5f;
    private const int TotalSequenceSlots = 60;
    private const float LevelOneEndZ = 400f;
    private const float LevelTwoEndZ = 800f;
    private const float TrackEndZ = 1200f;
    private readonly List<RunnerTrackSection> activeSections = new List<RunnerTrackSection>();
    private bool initialized;
    private float lastPowerUpZ = -1000f;
    private static readonly Material[] RuntimePowerUpMaterials = new Material[5];
    private static Material runtimeCoinMaterial;

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

        EnsureRequiredKeys();
        for (int i = 0; i < activeSections.Count; i++)
        {
            RunnerTrackSection section = activeSections[i];
            TrySpawnPowerUp(section, section.sequenceSlot);
            TrySpawnCoinTrail(section, section.sequenceSlot);
        }

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
        TrySpawnCoinTrail(instance, nextSequenceSlot);
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

    private void EnsureRequiredKeys()
    {
        TreasureKey[] sceneKeys = FindObjectsByType<TreasureKey>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TreasureKey[] required = new TreasureKey[2];
        List<TreasureKey> routeKeys = new List<TreasureKey>();
        foreach (TreasureKey key in sceneKeys)
        {
            if (key == null || key.transform.IsChildOf(sectionsContainer)) continue;
            routeKeys.Add(key);
            if (key.keyId >= 0 && key.keyId < required.Length && required[key.keyId] == null)
                required[key.keyId] = key;
        }

        // Reuse the two authored scene keys even if an older scene serialized IDs 2/3.
        // Their route order is the stable placement order: Forgotten Entrance, then Crystal Caverns.
        routeKeys.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
        for (int i = 0; i < required.Length; i++)
        {
            if (required[i] != null) continue;
            foreach (TreasureKey candidate in routeKeys)
                if (candidate != required[0] && candidate != required[1]) { required[i] = candidate; break; }
        }

        for (int id = 0; id < required.Length; id++)
        {
            TreasureKey key = required[id];
            if (key == null)
            {
                Debug.LogError("Runner requires exactly one active scene key with ID " + id + ".", this);
                continue;
            }
            key.keyId = id;
            key.gameObject.name = "GoldenKey_ID_" + id;
            key.gameObject.SetActive(true);
            key.transform.position = new Vector3(0f, 1f, id == 0 ? 100f : 600f);
            Collider keyCollider = key.GetComponent<Collider>();
            if (keyCollider != null) { keyCollider.enabled = true; keyCollider.isTrigger = true; }
        }

        foreach (TreasureKey key in sceneKeys)
        {
            if (key == null || key.transform.IsChildOf(sectionsContainer)) continue;
            if (key != required[0] && key != required[1]) key.gameObject.SetActive(false);
        }
    }

    private bool TrySpawnPowerUp(RunnerTrackSection section, int slot)
    {
        if (section == null || !TryGetPowerUpSchedule(slot, out int region, out int ordinal)) return false;
        float centerZ = section.StartZ + section.sectionLength * 0.5f;
        if (centerZ - lastPowerUpZ < 70f) return false;

        Collectible.PowerUpType type = GetRegionPowerUpType(region, ordinal);
        System.Random random = new System.Random(powerUpSeed ^ (slot * 7919));
        int[] lanes = { -1, 0, 1 };
        int laneStart = random.Next(lanes.Length);
        int selectedLane = 99;
        for (int i = 0; i < lanes.Length; i++)
        {
            int lane = lanes[(laneStart + i) % lanes.Length];
            bool blocked = false;
            foreach (RunnerLaneHazard hazard in section.GetComponentsInChildren<RunnerLaneHazard>(true))
            {
                float localZ = section.transform.InverseTransformPoint(hazard.transform.position).z;
                if (hazard.laneIndex == lane && Mathf.Abs(localZ - section.sectionLength * 0.5f) < 4f) { blocked = true; break; }
            }
            foreach (Collectible existing in section.GetComponentsInChildren<Collectible>(true))
            {
                Vector3 local = section.transform.InverseTransformPoint(existing.transform.position);
                int existingLane = Mathf.RoundToInt(local.x / LaneSpacing);
                if (existingLane == lane && Mathf.Abs(local.z - section.sectionLength * 0.5f) < 2f) { blocked = true; break; }
            }
            if (blocked) continue;
            Vector3 candidate = section.transform.TransformPoint(new Vector3(lane * LaneSpacing, 1f, section.sectionLength * 0.5f));
            foreach (TreasureKey key in FindObjectsByType<TreasureKey>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (key != null && Mathf.Abs(key.transform.position.z - candidate.z) < 2f && Mathf.Abs(key.transform.position.x - candidate.x) < 1f) { blocked = true; break; }
            if (!blocked) { selectedLane = lane; break; }
        }
        if (selectedLane == 99) return false;

        int powerUpIndex = (int)type - 1;
        GameObject pickup;
        if (powerUpPrefabs != null && powerUpPrefabs.Length == 5 && powerUpPrefabs[powerUpIndex] != null)
            pickup = Instantiate(powerUpPrefabs[powerUpIndex], section.transform);
        else
            pickup = CreatePowerUpPickup(type, section.transform);
        if (pickup == null) return false;
        pickup.name = "PowerUp_" + type + "_Slot_" + slot;
        pickup.transform.localPosition = new Vector3(selectedLane * LaneSpacing, 1f, section.sectionLength * 0.5f);
        lastPowerUpZ = centerZ;
        return true;
    }

    private bool TryGetPowerUpSchedule(int slot, out int region, out int ordinal)
    {
        int localSlot;
        if (slot >= 0 && slot <= 18) { region = 0; localSlot = slot; }
        else if (slot >= 20 && slot <= 39) { region = 1; localSlot = slot - 20; }
        else if (slot >= 41 && slot <= 59) { region = 2; localSlot = slot - 41; }
        else { region = -1; ordinal = -1; return false; }
        if (localSlot < 2 || (localSlot - 2) % 4 != 0) { ordinal = -1; return false; }
        ordinal = (localSlot - 2) / 4;
        return ordinal >= 0 && ordinal < 5;
    }

    private Collectible.PowerUpType GetRegionPowerUpType(int region, int ordinal)
    {
        int[] types = { 0, 1, 2, 3, 4 };
        System.Random random = new System.Random(powerUpSeed ^ (region * 104729));
        for (int i = types.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int temp = types[i]; types[i] = types[j]; types[j] = temp;
        }
        return (Collectible.PowerUpType)(types[ordinal] + 1);
    }

    private void TrySpawnCoinTrail(RunnerTrackSection section, int slot)
    {
        if (section == null || !IsCoinTrailSlot(slot)) return;
        Collectible[] existing = section.GetComponentsInChildren<Collectible>(true);
        int existingCoins = 0;
        for (int i = 0; i < existing.Length; i++)
            if (existing[i] != null && existing[i].powerUpType == Collectible.PowerUpType.None && existing[i].treasureType == Collectible.TreasureType.Coin) existingCoins++;
        if (existingCoins >= 3) return;

        int ordinal = GetCoinTrailOrdinal(slot);
        int[] lanes = { -1, 0, 1 };
        int preferred = (ordinal + 1) % 3;
        int[] coinZs = { 4, 7, 10, 13, 16 };
        int selectedLane = 99;
        for (int offset = 0; offset < lanes.Length; offset++)
        {
            int lane = lanes[(preferred + offset) % lanes.Length];
            bool blocked = false;
            foreach (RunnerLaneHazard hazard in section.GetComponentsInChildren<RunnerLaneHazard>(true))
            {
                float localZ = section.transform.InverseTransformPoint(hazard.transform.position).z;
                if (hazard.laneIndex == lane && localZ >= 2.5f && localZ <= 17.5f) { blocked = true; break; }
            }
            foreach (Collectible collectible in existing)
            {
                if (collectible == null) continue;
                Vector3 local = section.transform.InverseTransformPoint(collectible.transform.position);
                int pickupLane = Mathf.RoundToInt(local.x / LaneSpacing);
                if (pickupLane == lane && (collectible.powerUpType != Collectible.PowerUpType.None ||
                    (collectible.treasureType == Collectible.TreasureType.Coin && local.z >= 2.5f && local.z <= 17.5f)))
                { blocked = true; break; }
            }
            if (blocked) continue;
            foreach (TreasureKey key in FindObjectsByType<TreasureKey>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (key == null) continue;
                foreach (int z in coinZs)
                {
                    Vector3 candidate = section.transform.TransformPoint(new Vector3(lane * LaneSpacing, 1f, z));
                    if (Mathf.Abs(key.transform.position.z - candidate.z) < 2f && Mathf.Abs(key.transform.position.x - candidate.x) < 1f) { blocked = true; break; }
                }
                if (blocked) break;
            }
            if (!blocked) { selectedLane = lane; break; }
        }
        if (selectedLane == 99) return;

        // Replace crystal pickups in this lane with a readable coin trail rather than stacking both types.
        foreach (Collectible collectible in existing)
        {
            if (collectible == null || collectible.powerUpType != Collectible.PowerUpType.None || collectible.treasureType == Collectible.TreasureType.Coin) continue;
            Vector3 local = section.transform.InverseTransformPoint(collectible.transform.position);
            if (Mathf.RoundToInt(local.x / LaneSpacing) == selectedLane && local.z >= 2.5f && local.z <= 17.5f)
                collectible.gameObject.SetActive(false);
        }

        for (int i = 0; i < coinZs.Length; i++)
        {
            GameObject coin = coinPickupPrefab != null ? Instantiate(coinPickupPrefab, section.transform) : CreateRuntimeCoin(section.transform);
            if (coin == null) continue;
            coin.name = "CoinTrail_Slot_" + slot + "_" + (i + 1);
            coin.transform.localPosition = new Vector3(selectedLane * LaneSpacing, 1f, coinZs[i]);
            Collectible collectible = coin.GetComponent<Collectible>() ?? coin.GetComponentInChildren<Collectible>(true);
            if (collectible != null)
            {
                collectible.treasureType = Collectible.TreasureType.Coin;
                collectible.powerUpType = Collectible.PowerUpType.None;
                collectible.points = 10;
            }
            foreach (Collider trigger in coin.GetComponentsInChildren<Collider>(true)) trigger.isTrigger = true;
        }
    }

    private static bool IsCoinTrailSlot(int slot)
    {
        if (slot >= 0 && slot <= 18) return slot % 2 == 0;
        if (slot >= 20 && slot <= 39) return (slot - 20) % 2 == 0;
        if (slot >= 41 && slot <= 59) return (slot - 41) % 2 == 0;
        return false;
    }

    private static int GetCoinTrailOrdinal(int slot)
    {
        if (slot <= 18) return slot / 2;
        if (slot <= 39) return (slot - 20) / 2;
        return (slot - 41) / 2;
    }

    private static GameObject CreateRuntimeCoin(Transform parent)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "CoinTrail_Pickup";
        coin.transform.SetParent(parent, false);
        coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale = new Vector3(0.72f, 0.12f, 0.72f);
        Collider trigger = coin.GetComponent<Collider>();
        if (trigger != null) trigger.isTrigger = true;
        Renderer renderer = coin.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (runtimeCoinMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                runtimeCoinMaterial = new Material(shader) { color = new Color(1f, 0.72f, 0.12f) };
            }
            renderer.sharedMaterial = runtimeCoinMaterial;
        }
        Collectible collectible = coin.AddComponent<Collectible>();
        collectible.treasureType = Collectible.TreasureType.Coin;
        collectible.points = 10;
        return coin;
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
