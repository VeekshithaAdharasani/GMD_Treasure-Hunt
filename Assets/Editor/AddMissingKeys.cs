using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddMissingKeys
{
    [MenuItem("Tools/AddMissingKeys")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        Debug.Log("ACTIVE SCENE: " + scene.name + " path=" + scene.path);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Runner/Pickups/Runner_TreasureKey.prefab");
        if (prefab == null) { Debug.LogError("Key prefab missing"); return; }

        Vector3[] positions = new Vector3[] {
            new Vector3(0f, 1.2f, 60f),
            new Vector3(0f, 1.2f, 200f),
        };
        int[] ids = new int[] { 0, 1 };

        for (int i = 0; i < positions.Length; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.position = positions[i];
            var tk = go.GetComponent<TreasureKey>();
            if (tk != null) tk.keyId = ids[i];
            go.name = "TreasureKey_" + ids[i];
            Debug.Log("Created " + go.name + " in scene " + go.scene.name + " id=" + (tk!=null?tk.keyId.ToString():"none"));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("SaveScene returned " + saved);
    }
}