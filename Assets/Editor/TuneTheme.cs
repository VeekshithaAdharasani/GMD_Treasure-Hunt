using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneTheme
{
    [MenuItem("Tools/TuneTheme")]
    public static void Run()
    {
        // Region configs
        var fe = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>("Assets/VisualPolish/EnvironmentFramework/RunnerRegion_ForgottenEntrance.asset");
        var cc = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>("Assets/VisualPolish/EnvironmentFramework/RunnerRegion_CrystalCaverns.asset");
        var fv = AssetDatabase.LoadAssetAtPath<RunnerRegionConfig>("Assets/VisualPolish/EnvironmentFramework/RunnerRegion_AncientTreasureVault.asset");
        if (fe != null) { fe.fogColor = new Color(0.35f,0.42f,0.32f); fe.fogDensity=0.006f; fe.ambientSky=new Color(0.45f,0.5f,0.38f); fe.ambientEquator=new Color(0.25f,0.32f,0.2f); fe.ambientGround=new Color(0.12f,0.15f,0.1f); fe.ambientIntensity=0.8f; EditorUtility.SetDirty(fe); }
        if (cc != null) { cc.fogColor = new Color(0.08f,0.14f,0.22f); cc.fogDensity=0.01f; cc.ambientSky=new Color(0.12f,0.2f,0.32f); cc.ambientEquator=new Color(0.08f,0.14f,0.22f); cc.ambientGround=new Color(0.05f,0.08f,0.12f); cc.ambientIntensity=0.7f; EditorUtility.SetDirty(cc); }
        if (fv != null) { fv.fogColor = new Color(0.25f,0.18f,0.08f); fv.fogDensity=0.007f; fv.ambientSky=new Color(0.45f,0.3f,0.15f); fv.ambientEquator=new Color(0.3f,0.2f,0.1f); fv.ambientGround=new Color(0.15f,0.1f,0.05f); fv.ambientIntensity=0.8f; EditorUtility.SetDirty(fv); }
        AssetDatabase.SaveAssets();

        // Atmosphere component arrays
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        foreach (var a in Object.FindObjectsByType<RunnerWorldAtmosphere>(FindObjectsSortMode.None))
        {
            a.fogColors = new Color[]{ new Color(0.35f,0.42f,0.32f), new Color(0.08f,0.14f,0.22f), new Color(0.25f,0.18f,0.08f) };
            a.skyAmbient = new Color[]{ new Color(0.45f,0.5f,0.38f), new Color(0.12f,0.2f,0.32f), new Color(0.45f,0.3f,0.15f) };
            a.equatorAmbient = new Color[]{ new Color(0.25f,0.32f,0.2f), new Color(0.08f,0.14f,0.22f), new Color(0.3f,0.2f,0.1f) };
            a.groundAmbient = new Color[]{ new Color(0.12f,0.15f,0.1f), new Color(0.05f,0.08f,0.12f), new Color(0.15f,0.1f,0.05f) };
            a.fogDensities = new float[]{ 0.006f, 0.01f, 0.007f };
            EditorUtility.SetDirty(a);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TuneTheme done");
    }
}