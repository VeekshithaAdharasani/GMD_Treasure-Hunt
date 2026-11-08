using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddRegionAudio
{
    [MenuItem("Tools/AddRegionAudio")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        GameObject go = GameObject.Find("RegionAudio");
        if (go == null) go = new GameObject("RegionAudio");
        if (go.GetComponent<RunnerRegionAudio>() == null) go.AddComponent<RunnerRegionAudio>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AddRegionAudio done");
    }
}