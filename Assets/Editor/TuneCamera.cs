using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneCamera
{
    [MenuItem("Tools/TuneCamera")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            c.nearClipPlane = 0.3f;
            EditorUtility.SetDirty(c);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TuneCamera done");
    }
}