using UnityEngine;

/// <summary>Idempotently attaches a region's authored decoration module to a route section.</summary>
public static class RunnerEnvironmentAssembler
{
    public const string DecorationRootName = "RUNNER_ENVIRONMENT_DECORATION";

    public static Transform EnsureDecoration(RunnerTrackSection section, RunnerRegionConfig region, GameObject fallbackPrefab, int seed)
    {
        if (section == null) return null;

        Transform decoration = section.transform.Find(DecorationRootName);
        if (decoration == null)
        {
            GameObject prefab = region != null ? region.GetDecoration(section.sequenceSlot, seed) : fallbackPrefab;
            if (prefab == null) return null;

            GameObject instance = Object.Instantiate(prefab, section.transform);
            instance.name = DecorationRootName;
            decoration = instance.transform;
        }

        decoration.localPosition = Vector3.zero;
        decoration.localRotation = Quaternion.identity;
        float variationScale = region != null ? region.GetDecorationScale(section.sequenceSlot, seed) : 1f;
        decoration.localScale = Vector3.one * variationScale;
        return decoration;
    }
}
