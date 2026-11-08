using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TuneLights
{
    [MenuItem("Tools/TuneLights")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        int n=0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) l.intensity = Mathf.Min(l.intensity, 1.0f);
            else if (l.type == LightType.Point) l.intensity = Mathf.Min(l.intensity, 1.2f);
            else if (l.type == LightType.Spot) l.intensity = Mathf.Min(l.intensity, 1.5f);
            EditorUtility.SetDirty(l);
            n++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TuneLights done: " + n);
    }
}