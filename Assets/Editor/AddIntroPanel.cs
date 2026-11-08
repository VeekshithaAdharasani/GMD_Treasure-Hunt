using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class AddIntroPanel
{
    [MenuItem("Tools/AddIntroPanel")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null) { Debug.LogError("No GameManager"); return; }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("No Canvas"); return; }

        // Remove existing intro panel if present
        var existing = canvas.transform.Find("IntroPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject panel = new GameObject("IntroPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        Image pimg = panel.GetComponent<Image>();
        pimg.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);

        // Title
        GameObject title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        title.transform.SetParent(panel.transform, false);
        RectTransform trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.85f); trt.anchorMax = new Vector2(0.5f, 0.85f);
        trt.sizeDelta = new Vector2(900, 80);
        var ttmp = title.GetComponent<TextMeshProUGUI>();
        ttmp.text = "TREASURE HUNT: THE LOST CAVE";
        ttmp.fontSize = 48; ttmp.alignment = TextAlignmentOptions.Center;
        ttmp.color = new Color(1f, 0.85f, 0.3f);

        // Story
        GameObject story = new GameObject("Story", typeof(RectTransform), typeof(TextMeshProUGUI));
        story.transform.SetParent(panel.transform, false);
        RectTransform srt = story.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.55f); srt.anchorMax = new Vector2(0.5f, 0.55f);
        srt.sizeDelta = new Vector2(900, 200);
        var stmp = story.GetComponent<TextMeshProUGUI>();
        stmp.text = "Deep inside an ancient forgotten jungle lies a mysterious cave hiding a lost treasure. Become an explorer, collect two golden keys, overcome dangerous obstacles and discover the treasure hidden within.";
        stmp.fontSize = 26; stmp.alignment = TextAlignmentOptions.Center;
        stmp.color = Color.white;

        // Button
        GameObject btn = new GameObject("StartAdventureButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(panel.transform, false);
        RectTransform brt = btn.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.2f); brt.anchorMax = new Vector2(0.5f, 0.2f);
        brt.sizeDelta = new Vector2(260, 60);
        Image bimg = btn.GetComponent<Image>();
        bimg.color = new Color(0.8f, 0.6f, 0.2f);
        Button button = btn.GetComponent<Button>();
        GameObject bt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        bt.transform.SetParent(btn.transform, false);
        RectTransform btrt = bt.GetComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one;
        btrt.offsetMin = Vector2.zero; btrt.offsetMax = Vector2.zero;
        var btmp = bt.GetComponent<TextMeshProUGUI>();
        btmp.text = "Start Adventure";
        btmp.fontSize = 24; btmp.alignment = TextAlignmentOptions.Center;
        btmp.color = Color.black;

        gm.introPanel = panel;
        gm.startAdventureButton = button;

        EditorUtility.SetDirty(gm);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AddIntroPanel done");
    }
}