using UnityEngine;

public enum RunnerRegionId
{
    ForgottenEntrance = 0,
    CrystalCaverns = 1,
    AncientTreasureVault = 2
}

/// <summary>Shared visual, route-slot, and atmosphere data for one runner region.</summary>
[CreateAssetMenu(menuName = "Lost Cave/Runner Region Configuration", fileName = "RunnerRegionConfig")]
public sealed class RunnerRegionConfig : ScriptableObject
{
    [Header("Route mapping")]
    public RunnerRegionId regionId;
    public string displayName;
    [Min(0)] public int firstVisualSlot;
    [Min(0)] public int lastVisualSlot;
    [Tooltip("The transition section slot that leads out of this region. Use -1 for the final region.")]
    public int outgoingTransitionSlot = -1;

    [Header("Reusable modular section visuals")]
    public Material floorMaterial;
    public GameObject baseDecoration;
    [Tooltip("Optional alternatives. The route variation system chooses deterministically and avoids adjacent repeats.")]
    public GameObject[] decorationVariants;
    [Tooltip("Small uniform scale variants for a single decoration prefab. Keep close to 1 to preserve lane clearance.")]
    public float[] decorationScaleVariants = { 1f };

    [Header("Atmosphere profile")]
    public Color fogColor = Color.gray;
    [Min(0f)] public float fogDensity = 0.007f;
    public Color ambientSky = Color.gray;
    public Color ambientEquator = Color.gray;
    public Color ambientGround = Color.gray;
    [Min(0f)] public float ambientIntensity = 0.9f;

    [Header("Outgoing transition")]
    [Min(0f)] public float transitionCenterZ;
    [Min(1f)] public float transitionBlendWidth = 24f;
    public AnimationCurve transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public bool ContainsVisualSlot(int slot)
    {
        return slot >= firstVisualSlot && slot <= lastVisualSlot;
    }

    public GameObject GetDecoration(int sequenceSlot, int seed)
    {
        if (decorationVariants != null && decorationVariants.Length > 0)
        {
            int index = RunnerEnvironmentVariation.SelectIndex((int)regionId, sequenceSlot, decorationVariants.Length, seed);
            if (decorationVariants[index] != null) return decorationVariants[index];
        }

        return baseDecoration;
    }

    public float GetDecorationScale(int sequenceSlot, int seed)
    {
        if (decorationScaleVariants == null || decorationScaleVariants.Length == 0) return 1f;
        int index = RunnerEnvironmentVariation.SelectIndex((int)regionId, sequenceSlot, decorationScaleVariants.Length, seed ^ 0x31A7);
        return Mathf.Clamp(decorationScaleVariants[index], 0.9f, 1.1f);
    }
}
