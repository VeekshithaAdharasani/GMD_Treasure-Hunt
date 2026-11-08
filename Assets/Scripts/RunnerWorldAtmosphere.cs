using UnityEngine;

/// <summary>Blends the three runner environment lighting profiles at stage seams.</summary>
[DisallowMultipleComponent]
public sealed class RunnerWorldAtmosphere : MonoBehaviour
{
    public Transform player;
    [Min(0f)] public float firstStageEndZ = 80f;
    [Min(0f)] public float secondStageEndZ = 180f;
    [Min(1f)] public float blendWidth = 24f;

    public Color[] fogColors =
    {
        new Color(0.34f, 0.40f, 0.31f),
        new Color(0.12f, 0.22f, 0.33f),
        new Color(0.43f, 0.28f, 0.13f)
    };
    public Color[] skyAmbient =
    {
        new Color(0.52f, 0.48f, 0.35f),
        new Color(0.20f, 0.34f, 0.50f),
        new Color(0.62f, 0.42f, 0.20f)
    };
    public Color[] equatorAmbient =
    {
        new Color(0.30f, 0.35f, 0.24f),
        new Color(0.16f, 0.27f, 0.38f),
        new Color(0.43f, 0.29f, 0.14f)
    };
    public Color[] groundAmbient =
    {
        new Color(0.15f, 0.17f, 0.12f),
        new Color(0.09f, 0.15f, 0.21f),
        new Color(0.22f, 0.14f, 0.08f)
    };
    public float[] fogDensities = { 0.007f, 0.012f, 0.006f };

    [Header("Shared region framework")]
    public RunnerRegionConfig[] regionConfigs;

    private void LateUpdate()
    {
        if (player == null || (!HasRegionProfiles() &&
            (fogColors == null || fogColors.Length < 3 || skyAmbient == null || skyAmbient.Length < 3 ||
             equatorAmbient == null || equatorAmbient.Length < 3 || groundAmbient == null || groundAmbient.Length < 3 ||
             fogDensities == null || fogDensities.Length < 3)))
            return;

        float z = player.position.z;
        Color fog;
        Color sky;
        Color equator;
        Color ground;
        float density;
        float ambientIntensity;
        if (HasRegionProfiles())
        {
            RunnerRegionConfig first = regionConfigs[0];
            RunnerRegionConfig second = regionConfigs[1];
            RunnerRegionConfig third = regionConfigs[2];
            float firstBlend = RunnerRegionTransition.EvaluateWeight(z, first.transitionCenterZ, first.transitionBlendWidth, first.transitionCurve);
            float secondBlend = RunnerRegionTransition.EvaluateWeight(z, second.transitionCenterZ, second.transitionBlendWidth, second.transitionCurve);
            if (z <= second.transitionCenterZ)
                BlendProfile(first, second, firstBlend, out fog, out sky, out equator, out ground, out density, out ambientIntensity);
            else
                BlendProfile(second, third, secondBlend, out fog, out sky, out equator, out ground, out density, out ambientIntensity);
        }
        else
        {
            float halfBlend = blendWidth * 0.5f;
            if (z < firstStageEndZ + halfBlend)
            {
                float t = Mathf.InverseLerp(firstStageEndZ - halfBlend, firstStageEndZ + halfBlend, z);
                BlendProfile(0, 1, t, out fog, out sky, out equator, out ground, out density);
                ambientIntensity = 0.9f;
            }
            else if (z < secondStageEndZ + halfBlend)
            {
                float t = Mathf.InverseLerp(secondStageEndZ - halfBlend, secondStageEndZ + halfBlend, z);
                BlendProfile(1, 2, t, out fog, out sky, out equator, out ground, out density);
                ambientIntensity = 0.9f;
            }
            else
            {
                fog = fogColors[2]; sky = skyAmbient[2]; equator = equatorAmbient[2]; ground = groundAmbient[2]; density = fogDensities[2];
                ambientIntensity = 0.9f;
            }
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = density;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = sky;
        RenderSettings.ambientEquatorColor = equator;
        RenderSettings.ambientGroundColor = ground;
        RenderSettings.ambientIntensity = ambientIntensity;
    }

    private void BlendProfile(int from, int to, float t, out Color fog, out Color sky, out Color equator, out Color ground, out float density)
    {
        fog = Color.Lerp(fogColors[from], fogColors[to], t);
        sky = Color.Lerp(skyAmbient[from], skyAmbient[to], t);
        equator = Color.Lerp(equatorAmbient[from], equatorAmbient[to], t);
        ground = Color.Lerp(groundAmbient[from], groundAmbient[to], t);
        density = Mathf.Lerp(fogDensities[from], fogDensities[to], t);
    }

    private bool HasRegionProfiles()
    {
        return regionConfigs != null && regionConfigs.Length >= 3 &&
               regionConfigs[0] != null && regionConfigs[1] != null && regionConfigs[2] != null;
    }

    private static void BlendProfile(RunnerRegionConfig from, RunnerRegionConfig to, float t,
        out Color fog, out Color sky, out Color equator, out Color ground, out float density, out float ambientIntensity)
    {
        fog = Color.Lerp(from.fogColor, to.fogColor, t);
        sky = Color.Lerp(from.ambientSky, to.ambientSky, t);
        equator = Color.Lerp(from.ambientEquator, to.ambientEquator, t);
        ground = Color.Lerp(from.ambientGround, to.ambientGround, t);
        density = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
        ambientIntensity = Mathf.Lerp(from.ambientIntensity, to.ambientIntensity, t);
    }
}
