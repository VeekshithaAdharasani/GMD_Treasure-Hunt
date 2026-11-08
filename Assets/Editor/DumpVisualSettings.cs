using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class DumpVisualSettings
{
    [MenuItem("Tools/DumpVisualSettings")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        Debug.Log("=== LIGHTS ===");
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            Debug.Log("Light: " + l.name + " type=" + l.type + " intensity=" + l.intensity + " color=" + l.color + " range=" + l.range + " shadows=" + l.shadows);
        }
        Debug.Log("=== CAMERAS ===");
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            Debug.Log("Camera: " + c.name + " hdr=" + c.allowHDR + " pp=" + (c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderPostProcessing) + " near=" + c.nearClipPlane + " far=" + c.farClipPlane + " fov=" + c.fieldOfView);
        }
        Debug.Log("=== VOLUMES ===");
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            Debug.Log("Volume: " + v.name + " weight=" + v.weight + " priority=" + v.priority + " isGlobal=" + v.isGlobal + " profile=" + (v.sharedProfile != null ? v.sharedProfile.name : "null"));
            if (v.sharedProfile != null)
            {
                foreach (var c in v.sharedProfile.components)
                {
                    Debug.Log("   Component: " + c.GetType().Name + " active=" + c.active);
                }
            }
        }
        Debug.Log("=== RENDER SETTINGS ===");
        Debug.Log("fog=" + RenderSettings.fog + " mode=" + RenderSettings.fogMode + " color=" + RenderSettings.fogColor + " density=" + RenderSettings.fogDensity);
        Debug.Log("ambientMode=" + RenderSettings.ambientMode + " sky=" + RenderSettings.ambientSkyColor + " equator=" + RenderSettings.ambientEquatorColor + " ground=" + RenderSettings.ambientGroundColor + " intensity=" + RenderSettings.ambientIntensity);
        Debug.Log("skybox=" + (RenderSettings.skybox != null ? RenderSettings.skybox.name : "null"));
        Debug.Log("reflectionIntensity=" + RenderSettings.reflectionIntensity);
        Debug.Log("=== DONE ===");
    }
}