using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetLaneWidth
{
    [MenuItem("Tools/SetLaneWidth")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        foreach (var p in Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            p.laneWidth = 3.5f;
            EditorUtility.SetDirty(p);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("SetLaneWidth done");
    }
}