using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class M6PlayModeVerifier
{
    private const string StartKey = "M6_PLAYMODE_VERIFIER_START";
    private const string ResultKey = "M6_PLAYMODE_VERIFIER_RESULT";

    static M6PlayModeVerifier()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= PollResult;
        EditorApplication.update += PollResult;
    }

    [MenuItem("Tools/Runner Adventure/Run Key and Vault Play Mode Verification")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("M6 Play Mode verification refused because Unity is already entering or in Play Mode.");
            return;
        }
        if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/LostCave_Main.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/LostCave_Main.unity", OpenSceneMode.Single);
        if (EditorSceneManager.GetActiveScene().isDirty && !EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()))
        {
            Debug.LogError("M6 Play Mode verification could not save LostCave_Main.");
            return;
        }
        SessionState.SetBool(StartKey, true);
        SessionState.SetString(ResultKey, string.Empty);
        PlayerPrefs.DeleteKey(ResultKey);
        PlayerPrefs.Save();
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(StartKey, false))
        {
            SessionState.SetBool(StartKey, false);
            new GameObject("M6 Play Mode Verification Runner").AddComponent<M6PlayModeVerifierRunner>();
        }
    }

    public static void Report(string result)
    {
        SessionState.SetString(ResultKey, result);
        EditorApplication.isPlaying = false;
    }

    private static void PollResult()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string result = PlayerPrefs.GetString(ResultKey, string.Empty);
        if (string.IsNullOrEmpty(result)) return;
        PlayerPrefs.DeleteKey(ResultKey);
        PlayerPrefs.Save();
        Debug.Log("M6_PLAYMODE_VERIFICATION_" + result);
        EditorApplication.delayCall += () => EditorApplication.Exit(result == "PASS" ? 0 : 2);
    }
}
