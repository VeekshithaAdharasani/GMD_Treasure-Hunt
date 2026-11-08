using UnityEditor;
using UnityEngine;

public static class WidenRoad
{
    [MenuItem("Tools/WidenRoad")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/Prefabs/Runner/Sections"});
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var c = PrefabUtility.LoadPrefabContents(path);
            var floor = c.transform.Find("TRACK FLOOR");
            if (floor != null)
            {
                var ls = floor.localScale;
                ls.x = 11f;
                floor.localScale = ls;
            }
            PrefabUtility.SaveAsPrefabAsset(c, path);
            PrefabUtility.UnloadPrefabContents(c);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("WidenRoad done");
    }
}