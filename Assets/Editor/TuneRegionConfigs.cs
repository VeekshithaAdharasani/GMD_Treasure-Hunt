using UnityEditor;
using UnityEngine;

public static class TuneRegionConfigs
{
    [MenuItem("Tools/TuneRegionConfigs")]
    public static void Run()
    {
        string[] paths = new string[] {
            "Assets/VisualPolish/EnvironmentFramework/RunnerRegion_ForgottenEntrance.asset",
            "Assets/VisualPolish/EnvironmentFramework/RunnerRegion_CrystalCaverns.asset",
            "Assets/VisualPolish/EnvironmentFramework/RunnerRegion_AncientTreasureVault.asset",
        };
        int[][] slots = new int[][] {
            new int[]{0,8,8},
            new int[]{9,17,17},
            new int[]{18,24,-1},
        };
        float[] centers = new float[]{170f, 350f, 0f};
        for (int i=0;i<paths.Length;i++)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>(paths[i]);
            if (cfg == null) { Debug.LogError("Missing " + paths[i]); continue; }
            cfg.firstVisualSlot = slots[i][0];
            cfg.lastVisualSlot = slots[i][1];
            cfg.outgoingTransitionSlot = slots[i][2];
            if (i < 2) cfg.transitionCenterZ = centers[i];
            cfg.fogDensity = Mathf.Max(0.004f, cfg.fogDensity * 0.85f);
            cfg.ambientIntensity = Mathf.Clamp(cfg.ambientIntensity, 0.6f, 0.9f);
            EditorUtility.SetDirty(cfg);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("TuneRegionConfigs done");
    }
}