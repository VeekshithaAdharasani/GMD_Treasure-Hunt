using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneAtmosphere
{
    [MenuItem("Tools/TuneAtmosphere")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        var atms = Object.FindObjectsByType<RunnerWorldAtmosphere>(FindObjectsSortMode.None);
        foreach (var a in atms)
        {
            a.firstStageEndZ = 160f;
            a.secondStageEndZ = 340f;
            a.blendWidth = 40f;
            EditorUtility.SetDirty(a);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TuneAtmosphere done: " + atms.Length);
    }
}