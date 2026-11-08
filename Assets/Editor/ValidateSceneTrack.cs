using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ValidateSceneTrack
{
    [MenuItem("Tools/ValidateSceneTrack")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        var sections = Object.FindObjectsByType<RunnerTrackSection>(FindObjectsSortMode.None);
        Debug.Log("Found scene sections: " + sections.Length);
        foreach (var s in sections)
        {
            if (s.Validate(2.5f, out string err))
                Debug.Log("VALID: " + s.name + " slot=" + s.sequenceSlot);
            else
                Debug.LogError("INVALID: " + s.name + " - " + err);
        }
        Debug.Log("ValidateSceneTrack done");
    }
}