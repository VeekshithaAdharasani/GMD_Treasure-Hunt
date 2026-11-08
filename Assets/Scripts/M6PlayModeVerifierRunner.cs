using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class M6PlayModeVerifierRunner : MonoBehaviour
{
    private enum TestPhase { Initial, LockedRestart, VictoryRestart }
    private TestPhase phase;
    private bool sceneReloaded;
    private string failure;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) player.autoRunSpeed = 0f;
    }

    private void Start() { StartCoroutine(Run()); }

    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(1f);
        if (!FindActors(out PlayerMovement player, out GameManager manager, out RunnerTrackManager track))
        { Fail("Player, GameManager, or RunnerTrackManager did not initialize."); yield break; }
        player.autoRunSpeed = 0f;
        CharacterController controller = player.GetComponent<CharacterController>();

        GameObject chestObject = GameObject.Find("FINAL VAULT TREASURE CHEST");
        TreasureChest chest = chestObject != null ? chestObject.GetComponent<TreasureChest>() : null;
        if (chest == null) { Fail("Final vault chest component is missing."); yield break; }
        chestObject.SendMessage("OnTriggerEnter", controller, SendMessageOptions.DontRequireReceiver);
        if (!manager.IsFinished || manager.lockedTreasurePanel == null || !manager.lockedTreasurePanel.activeInHierarchy ||
            manager.lockedTreasureText == null || !manager.lockedTreasureText.text.Contains("2 more keys"))
        { Fail("Chest did not show the correct locked message before key collection."); yield break; }

        phase = TestPhase.LockedRestart;
        Button lockedRestart = FindRestart(manager.lockedTreasurePanel.transform);
        if (lockedRestart == null || lockedRestart.onClick.GetPersistentEventCount() == 0)
        { Fail("Locked treasure panel has no persistent Restart Adventure button."); yield break; }
        Time.timeScale = 1f;
        sceneReloaded = false;
        lockedRestart.onClick.Invoke();
        yield return WaitForReload();
        if (failure != null) yield break;

        if (!FindActors(out player, out manager, out track)) { Fail("Restart from locked chest did not reload the scene."); yield break; }
        player.autoRunSpeed = 0f;
        controller = player.GetComponent<CharacterController>();
        if (manager.Score != 0 || manager.Health != manager.startingHealth || manager.TreasureKeys != 0 || manager.IsFinished)
        {
            Fail("Restart from locked chest state was score=" + manager.Score + ", health=" + manager.Health + "/" + manager.startingHealth +
                 ", keys=" + manager.TreasureKeys + ", finished=" + manager.IsFinished + ".");
            yield break;
        }

        TreasureKey key0 = FindKey(0); TreasureKey key1 = FindKey(1);
        if (key0 == null || key1 == null) { Fail("Stage 1 or 2 key is missing from the preview track."); yield break; }
        string collectError = CollectAndCheck(key0, controller, manager, 1);
        if (collectError != null) { Fail(collectError); yield break; }
        if (manager.treasureKeysText == null || !manager.treasureKeysText.text.Contains("1/2"))
        { Fail("HUD did not immediately show 1/2 keys."); yield break; }
        collectError = CollectAndCheck(key1, controller, manager, 2);
        if (collectError != null) { Fail(collectError); yield break; }

        Teleport(player, controller, 0f, 81f);
        yield return new WaitForSecondsRealtime(0.25f);
        if (manager.stageText == null || !manager.stageText.text.Contains("STAGE 2/5"))
        { Fail("Stage transition from Forgotten Entrance to Crystal Caverns did not update the stage HUD."); yield break; }
        if (manager.TreasureKeys != 2) { Fail("Key progress did not persist across the first stage transition."); yield break; }
        TreasureKey key2 = FindKey(2);
        if (key2 == null) { Fail("Ancient Ruins key section did not spawn ahead of the runner."); yield break; }
        collectError = CollectAndCheck(key2, controller, manager, 3);
        if (collectError != null) { Fail(collectError); yield break; }

        Teleport(player, controller, 0f, 161f);
        yield return new WaitForSecondsRealtime(0.25f);
        if (manager.stageText == null || !manager.stageText.text.Contains("STAGE 3/5") || manager.TreasureKeys != 3)
        { Fail("Progression to Ancient Ruins did not preserve key progress."); yield break; }
        TreasureKey key3 = FindKey(3);
        if (key3 == null) { Fail("Lost Temple key section did not spawn ahead of the runner."); yield break; }
        collectError = CollectAndCheck(key3, controller, manager, 4);
        if (collectError != null) { Fail(collectError); yield break; }

        Teleport(player, controller, 0f, 241f);
        yield return new WaitForSecondsRealtime(0.25f);
        if (manager.stageText == null || !manager.stageText.text.Contains("STAGE 4/5") || manager.TreasureKeys != 4)
        { Fail("Progression to Lost Temple did not preserve both keys."); yield break; }
        Teleport(player, controller, 0f, 321f);
        yield return new WaitForSecondsRealtime(0.25f);
        if (manager.stageText == null || !manager.stageText.text.Contains("STAGE 5/5"))
        { Fail("Final Treasure Vault stage did not activate."); yield break; }

        chestObject = GameObject.Find("FINAL VAULT TREASURE CHEST");
        if (chestObject == null) { Fail("Chest was not preserved at the final vault endpoint."); yield break; }
        chestObject.SendMessage("OnTriggerEnter", controller, SendMessageOptions.DontRequireReceiver);
        TreasureChest finalChest = chestObject.GetComponent<TreasureChest>();
        if (finalChest == null || finalChest.treasureLight == null || !finalChest.treasureLight.enabled ||
            finalChest.openingSparkles == null || !finalChest.openingSparkles.isPlaying)
        { Fail("Unlocked chest did not start its golden light and sparkle effects."); yield break; }
        yield return new WaitForSecondsRealtime(1.8f);
        if (!manager.IsFinished || manager.winPanel == null || !manager.winPanel.activeInHierarchy ||
            manager.finalStatsText == null || !manager.finalStatsText.text.Contains("2/2"))
        { Fail("Four-key chest unlock did not show the victory screen and final key count."); yield break; }

        phase = TestPhase.VictoryRestart;
        Button victoryRestart = FindRestart(manager.winPanel.transform);
        if (victoryRestart == null || victoryRestart.onClick.GetPersistentEventCount() == 0)
        { Fail("Victory panel has no persistent Restart Adventure button."); yield break; }
        Time.timeScale = 1f;
        sceneReloaded = false;
        victoryRestart.onClick.Invoke();
        yield return WaitForReload();
        if (failure != null) yield break;
        if (!FindActors(out player, out manager, out track) || manager.Score != 0 || manager.Health != manager.startingHealth ||
            manager.TreasureKeys != 0 || manager.IsFinished)
        { Fail("Restart from victory did not return to a fresh adventure."); yield break; }

        Debug.Log("M6_PLAYMODE_CASES_PASS | lockedChest=true | 4UniqueKeys=true | duplicatePrevention=true | keyHud=true | stagePersistence=true | fiveStageProgression=true | victory=true | restart=true");
#if UNITY_EDITOR
        PlayerPrefs.SetString("M6_PLAYMODE_VERIFIER_RESULT", "PASS");
        PlayerPrefs.Save();
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator WaitForReload()
    {
        float elapsed = 0f;
        while (!sceneReloaded && elapsed < 5f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!sceneReloaded) Fail("Restart button did not reload LostCave_Main.");
        else yield return new WaitForSecondsRealtime(0.35f);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "LostCave_Main") return;
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) player.autoRunSpeed = 0f;
        sceneReloaded = true;
    }

    private static bool FindActors(out PlayerMovement player, out GameManager manager, out RunnerTrackManager track)
    {
        player = FindFirstObjectByType<PlayerMovement>();
        manager = GameManager.Instance;
        track = FindFirstObjectByType<RunnerTrackManager>();
        return player != null && manager != null && track != null;
    }

    private static TreasureKey FindKey(int id)
    {
        foreach (TreasureKey key in FindObjectsByType<TreasureKey>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (key.keyId == id) return key;
        return null;
    }

    private static string CollectAndCheck(TreasureKey key, CharacterController controller, GameManager manager, int expectedCount)
    {
        key.gameObject.SendMessage("OnTriggerEnter", controller, SendMessageOptions.DontRequireReceiver);
        if (manager.TreasureKeys != expectedCount)
            return "Key " + key.keyId + " collection did not increment progress to " + expectedCount + ".";
        key.gameObject.SendMessage("OnTriggerEnter", controller, SendMessageOptions.DontRequireReceiver);
        if (manager.TreasureKeys != expectedCount)
            return "Key " + key.keyId + " was counted more than once.";
        return null;
    }

    private static void Teleport(PlayerMovement player, CharacterController controller, float x, float z)
    {
        controller.enabled = false;
        player.transform.position = new Vector3(x, 1f, z);
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    private static Button FindRestart(Transform parent)
    {
        foreach (Button button in parent.GetComponentsInChildren<Button>(true))
            if (button.gameObject.name == "RestartAdventureButton") return button;
        return null;
    }

    private void Fail(string message)
    {
        failure = message;
        Debug.LogError("M6_PLAYMODE_CASE_FAIL | " + message + " | phase=" + phase);
#if UNITY_EDITOR
        Time.timeScale = 1f;
        PlayerPrefs.SetString("M6_PLAYMODE_VERIFIER_RESULT", "FAIL");
        PlayerPrefs.Save();
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
