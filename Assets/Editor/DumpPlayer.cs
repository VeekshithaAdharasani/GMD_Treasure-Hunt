using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DumpPlayer
{
    [MenuItem("Tools/DumpPlayer")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        foreach (var p in Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            Debug.Log("Player pos=" + p.transform.position + " rot=" + p.transform.rotation.eulerAngles + " scale=" + p.transform.localScale);
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) Debug.Log("  CC center=" + cc.center + " height=" + cc.height + " radius=" + cc.radius);
            var model = p.transform.Find("PlayerModel");
            if (model != null) Debug.Log("  PlayerModel localPos=" + model.localPosition + " localScale=" + model.localScale);
        }
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var cf = c.GetComponent<CameraFollow>();
            if (cf != null) Debug.Log("Camera pos=" + c.transform.position + " offset=" + cf.offset + " target=" + (cf.target != null ? cf.target.name : "null"));
        }
        // track floor center
        var sections = Object.FindObjectsByType<RunnerTrackSection>(FindObjectsSortMode.None);
        foreach (var s in sections)
        {
            var floor = s.transform.Find("TRACK FLOOR");
            if (floor != null) Debug.Log("Section " + s.name + " floor worldPos=" + floor.position + " scale=" + floor.lossyScale);
        }
    }
}