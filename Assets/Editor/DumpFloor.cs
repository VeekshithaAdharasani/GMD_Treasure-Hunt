using UnityEditor;
using UnityEngine;

public static class DumpFloor
{
    [MenuItem("Tools/DumpFloor")]
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
                var bc = floor.GetComponent<BoxCollider>();
                if (bc != null)
                    Debug.Log(path + " floor size=" + bc.size + " scale=" + floor.localScale);
            }
            PrefabUtility.UnloadPrefabContents(c);
        }
    }
}