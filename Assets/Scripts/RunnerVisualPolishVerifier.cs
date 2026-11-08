using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class RunnerVisualPolishVerifier : MonoBehaviour
{
    private bool sceneReloaded;
    private const float LaneWidth = 2.5f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LostCave_Main") sceneReloaded = true;
    }

    private IEnumerator Start() { yield return Run(); }

    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        if (!FindActors(out PlayerMovement player, out GameManager manager, out RunnerTrackManager track))
        { Fail("Missing player, GameManager or RunnerTrackManager."); yield break; }

        manager.StartFreshGame();
        float startZ = player.transform.position.z;
        yield return new WaitForSecondsRealtime(1.1f);
        if (manager.Health != 3 || manager.healthText == null || !manager.healthText.text.Contains("3") ||
            player.transform.position.z < startZ + 5f)
        { Fail("Run did not start with three health and advance safely: health=" + manager.Health + ", z=" + player.transform.position.z + "."); yield break; }

        player.autoRunSpeed = 0f;
        player.RequestLaneChange(1);
        yield return new WaitForSecondsRealtime(0.25f);
        if (player.CurrentLane != 1 || Mathf.Abs(player.transform.position.x - LaneWidth) > 0.3f)
        { Fail("Right lane switch did not settle in lane 1."); yield break; }
        player.RequestLaneChange(-1); player.RequestLaneChange(-1);
        yield return new WaitForSecondsRealtime(0.35f);
        if (player.CurrentLane != -1 || Mathf.Abs(player.transform.position.x + LaneWidth) > 0.3f)
        { Fail("Left lane switching did not settle in lane -1."); yield break; }

        CharacterController controller = player.GetComponent<CharacterController>();
        Teleport(player, controller, 0f, 8f);
        float groundY = player.transform.position.y;
        player.RequestJump();
        yield return new WaitForSecondsRealtime(0.18f);
        if (player.transform.position.y < groundY + 0.2f)
        { Fail("Jump request did not raise the player."); yield break; }
        yield return new WaitForSecondsRealtime(0.6f);
        player.RequestSlide();
        yield return new WaitForSecondsRealtime(0.1f);
        if (!player.IsSliding || controller.height >= 2f)
        { Fail("Slide request did not lower the controller."); yield break; }
        yield return new WaitForSecondsRealtime(player.slideDuration + 0.15f);
        if (player.IsSliding || controller.height < 1.9f)
        { Fail("Slide did not restore the standing controller."); yield break; }

        Collectible crystal = FindObjectsByType<Collectible>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();
        if (crystal == null) { Fail("No active crystal pickup was found."); yield break; }
        int scoreBefore = manager.Score;
        int crystalsBefore = manager.Crystals;
        Teleport(player, controller, crystal.transform.position.x, crystal.transform.position.z);
        yield return new WaitForSecondsRealtime(0.25f);
        if (manager.Score <= scoreBefore || manager.Crystals != crystalsBefore + 1)
        { Fail("Crystal trigger did not update score and crystal count."); yield break; }

        Obstacle obstacle = track.sectionsContainer.GetComponentsInChildren<RunnerLaneHazard>(true)
            .Select(h => h.GetComponent<Obstacle>() ?? h.GetComponentInChildren<Obstacle>(true) ?? h.GetComponentInParent<Obstacle>())
            .FirstOrDefault(o => o != null && o.gameObject.activeInHierarchy && o.GetComponentsInChildren<Collider>(true).Any(c => c.enabled));
        Collider obstacleCollider = obstacle != null ? obstacle.GetComponentsInChildren<Collider>(true).FirstOrDefault(c => c.enabled) : null;
        if (obstacle == null || obstacleCollider == null) { Fail("No active obstacle with an enabled collider was found."); yield break; }
        Bounds obstacleBounds = obstacleCollider.bounds;
        int obstacleLane = Mathf.Clamp(Mathf.RoundToInt(obstacleBounds.center.x / LaneWidth), -1, 1);
        float approachZ = obstacleBounds.min.z - controller.radius - 0.12f;
        Teleport(player, controller, obstacleLane * LaneWidth, approachZ);
        int healthBeforeHit = manager.Health;
        controller.Move(Vector3.forward * (obstacleBounds.size.z + controller.radius + 0.5f));
        Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(0.1f);
        if (manager.Health != healthBeforeHit - 1)
        { Fail("Physical obstacle contact did not apply exactly one health hit: before=" + healthBeforeHit + ", after=" + manager.Health + "."); yield break; }
        manager.TakeDamage();
        if (manager.Health != healthBeforeHit - 1)
        { Fail("One-second damage cooldown did not suppress a repeated hit."); yield break; }
        yield return new WaitForSecondsRealtime(manager.damageCooldown + 0.15f);
        manager.TakeDamage();
        if (manager.Health != healthBeforeHit - 2)
        { Fail("Damage was not allowed after the cooldown expired."); yield break; }
        yield return new WaitForSecondsRealtime(manager.damageCooldown + 0.15f);
        manager.TakeDamage(10);
        if (manager.Health != 0 || !manager.IsFinished || manager.gameOverPanel == null || !manager.gameOverPanel.activeInHierarchy)
        { Fail("Health clamp or Game Over at zero did not work."); yield break; }

        sceneReloaded = false;
        manager.RestartGame();
        yield return WaitForSceneReload();
        if (!FindActors(out player, out manager, out track) || manager.Health != 3 || manager.Score != 0 || manager.TreasureKeys != 0 || manager.IsFinished)
        { Fail("Restart did not restore a fresh game with three health."); yield break; }
        player.autoRunSpeed = 0f;
        controller = player.GetComponent<CharacterController>();

        TreasureChest chest = FindFirstObjectByType<TreasureChest>();
        if (chest == null) { Fail("Final chest is missing."); yield break; }
        Teleport(player, controller, chest.transform.position.x, chest.transform.position.z);
        chest.SendMessage("OnTriggerEnter", controller, SendMessageOptions.DontRequireReceiver);
        InvokeChestInteraction(chest);
        if (manager.IsFinished || manager.TreasureKeys != 0 || manager.interactionHintText == null ||
            !manager.interactionHintText.text.ToLowerInvariant().Contains("both"))
        { Fail("Chest did not remain locked without both keys."); yield break; }

        for (int id = 0; id < 2; id++)
        {
            if (id == 2)
            {
                Teleport(player, controller, 0f, 101f);
                yield return null;
                if (manager.stageText == null || !manager.stageText.text.Contains("2/3"))
                { Fail("Stage 2 HUD transition did not activate at the Ancient Ruins boundary."); yield break; }
            }
            TreasureKey key = FindObjectsByType<TreasureKey>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(k => k.keyId == id);
            if (key == null) { Fail("Unique key ID " + id + " is missing or unreachable in the active scene."); yield break; }
            Vector3 keyPosition = key.transform.position;
            Teleport(player, controller, keyPosition.x, keyPosition.z);
            yield return new WaitForSecondsRealtime(0.2f);
            if (manager.TreasureKeys != id + 1)
            { Fail("Key " + id + " trigger did not persistently advance key progress."); yield break; }
            if (id == 0 && manager.treasureKeysText != null && !manager.treasureKeysText.text.Contains("1/2"))
            { Fail("HUD did not show key progress 1/2."); yield break; }
        }
        Teleport(player, controller, 0f, 200.5f);
        yield return null;
        if (manager.stageText == null || !manager.stageText.text.Contains("3/3") || manager.TreasureKeys != 4)
        { Fail("Stage 3 transition or four-key persistence failed."); yield break; }

        Teleport(player, controller, chest.transform.position.x, chest.transform.position.z);
        chest.SendMessage("OnTriggerEnter", controller, SendMessageOptions.DontRequireReceiver);
        InvokeChestInteraction(chest);
        yield return new WaitForSecondsRealtime(chest.openDuration + 0.6f);
        if (!manager.IsFinished || manager.winPanel == null || !manager.winPanel.activeInHierarchy ||
            manager.finalStatsText == null || !manager.finalStatsText.text.Contains("2/2"))
        { Fail("Unlocked chest did not produce the two-key Victory screen."); yield break; }

        sceneReloaded = false;
        manager.RestartGame();
        yield return WaitForSceneReload();
        if (!FindActors(out player, out manager, out track) || manager.Health != 3 || manager.TreasureKeys != 0 || manager.Score != 0)
        { Fail("Restart from Victory did not restore health, score and keys."); yield break; }
        manager.TakeDamage(10);
        if (manager.Health != 0 || manager.gameOverPanel == null || !manager.gameOverPanel.activeInHierarchy)
        { Fail("Game Over screen did not activate after the restart damage check."); yield break; }
        sceneReloaded = false;
        manager.RestartGame();
        yield return WaitForSceneReload();
        if (!FindActors(out player, out manager, out track) || manager.Health != 3 || manager.TreasureKeys != 0 || manager.Score != 0)
        { Fail("Restart from Game Over did not return to three health and a fresh run."); yield break; }

        Finish(true,
            "auto-run; lane-switch; jump; slide; crystal-trigger; physical-obstacle-hit; damage-cooldown; zero-health-GameOver; key-IDs-0-3; 3-stage-HUD; locked-and-unlocked-chest; Victory; restart-from-Victory-and-GameOver");
    }

    private IEnumerator WaitForSceneReload()
    {
        float elapsed = 0f;
        while (!sceneReloaded && elapsed < 6f) { elapsed += Time.unscaledDeltaTime; yield return null; }
        if (!sceneReloaded) yield break;
        yield return new WaitForSecondsRealtime(0.35f);
    }

    private static bool FindActors(out PlayerMovement player, out GameManager manager, out RunnerTrackManager track)
    {
        player = FindFirstObjectByType<PlayerMovement>();
        manager = GameManager.Instance;
        track = FindFirstObjectByType<RunnerTrackManager>();
        return player != null && manager != null && track != null;
    }

    private static void Teleport(PlayerMovement player, CharacterController controller, float x, float z)
    {
        controller.enabled = false;
        player.transform.position = new Vector3(x, 1f, z);
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    private static void InvokeChestInteraction(TreasureChest chest)
    {
        MethodInfo interaction = typeof(TreasureChest).GetMethod("Interact", BindingFlags.Instance | BindingFlags.NonPublic);
        interaction?.Invoke(chest, null);
    }

    private void Fail(string message)
    {
        Finish(false, message);
    }

    private void Finish(bool passed, string details)
    {
        Debug.Log((passed ? "RUNNER_VISUAL_PLAYTEST_PASS | " : "RUNNER_VISUAL_PLAYTEST_FAIL | ") + details);
        if (!passed) Debug.LogError("Runner visual-polish Play Mode verification failed: " + details);
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 2);
#else
        Application.Quit(passed ? 0 : 2);
#endif
    }
}
