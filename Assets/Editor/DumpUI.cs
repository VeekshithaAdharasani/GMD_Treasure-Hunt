using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DumpUI
{
    [MenuItem("Tools/DumpUI")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        Debug.Log("Total Buttons: " + buttons.Length);
        foreach (var b in buttons) Debug.Log("Button: " + GetPath(b.transform));
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log("Total Canvases: " + canvases.Length);
        foreach (var cv in canvases) Debug.Log("Canvas: " + GetPath(cv.transform));
        var panels = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        int p=0; foreach (var r in panels) if (r.name.ToLower().Contains("panel")) { p++; Debug.Log("Panel: " + GetPath(r)); }
        Debug.Log("Panel count: " + p);
    }
    static string GetPath(Transform t){ string p=t.name; while(t.parent!=null){t=t.parent; p=t.name+"/"+p;} return p; }
}