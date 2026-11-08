using UnityEngine;

/// <summary>Shared easing helper used to blend region atmosphere at route transition seams.</summary>
public static class RunnerRegionTransition
{
    public static float EvaluateWeight(float playerZ, float centerZ, float blendWidth, AnimationCurve curve)
    {
        float halfWidth = Mathf.Max(1f, blendWidth) * 0.5f;
        float t = Mathf.InverseLerp(centerZ - halfWidth, centerZ + halfWidth, playerZ);
        float eased = curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
        return Mathf.Clamp01(eased);
    }
}
