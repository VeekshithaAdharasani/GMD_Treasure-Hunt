using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class M5PlayModeSmokeRunner : MonoBehaviour
{
    private bool restarted;
    private bool waitingForRestart;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start() { StartCoroutine(Run()); }

    private IEnumerator Run()
    {
        PlayerMovement player=FindFirstObjectByType<PlayerMovement>();GameManager manager=GameManager.Instance;
        if(player==null||manager==null){Fail("Player or GameManager missing at Play Mode start.");yield break;}
        CharacterController cc=player.GetComponent<CharacterController>();float startZ=player.transform.position.z;Debug.Log("M7_AUTORUN_START | speed="+player.autoRunSpeed+" timeScale="+Time.timeScale+" finished="+manager.IsFinished+" z="+startZ);
        yield return new WaitForSeconds(.35f);
        Debug.Log("M7_AUTORUN_END | speed="+player.autoRunSpeed+" timeScale="+Time.timeScale+" finished="+manager.IsFinished+" z="+player.transform.position.z);
        if(player.transform.position.z<startZ+1.5f){Fail("Automatic forward running did not advance.");yield break;}

        int initialScore=manager.Score;player.RequestLaneChange(-1);yield return new WaitForSeconds(.35f);
        string laneError=LaneError(player,-1);if(laneError!=null){Fail(laneError);yield break;}
        yield return new WaitForSeconds(.45f);
        if(manager.Score<=initialScore){Fail("Crystal pickup did not award score while running in its lane.");yield break;}
        player.RequestLaneChange(1);yield return new WaitForSeconds(.3f);laneError=LaneError(player,0);if(laneError!=null){Fail(laneError);yield break;}
        player.RequestLaneChange(1);yield return new WaitForSeconds(.3f);laneError=LaneError(player,1);if(laneError!=null){Fail(laneError);yield break;}
        player.RequestLaneChange(-1);yield return new WaitForSeconds(.3f);laneError=LaneError(player,0);if(laneError!=null){Fail(laneError);yield break;}

        RunnerTrackManager track=FindFirstObjectByType<RunnerTrackManager>();RunnerLaneHazard jumpRock=null,slideBar=null;
        if(track==null){Fail("RunnerTrackManager missing in Play Mode.");yield break;}
        foreach(RunnerLaneHazard h in track.GetComponentsInChildren<RunnerLaneHazard>(true))
        {if(h.name=="FE center rock")jumpRock=h;if(h.name=="FE left low beam")slideBar=h;}
        if(jumpRock==null||slideBar==null){Fail("Expected jump rock or slide bar is missing.");yield break;}
        float healthBefore=manager.Health;SetPlayer(player,cc,0,jumpRock.transform.position.z-3f);float groundY=player.transform.position.y;Debug.Log("M7_JUMP_DEBUG_START | health="+healthBefore+" player="+player.transform.position+" target="+jumpRock.transform.position+" bounds="+jumpRock.GetComponent<Collider>().bounds);
        player.RequestJump();yield return new WaitForSeconds(.32f);
        if(player.transform.position.y<groundY+.5f){Fail("Jump did not clear the player capsule above the track.");yield break;}
        yield return new WaitForSeconds(.5f);
        Debug.Log("M7_JUMP_DEBUG_END | health="+manager.Health+" player="+player.transform.position+" targetBounds="+jumpRock.GetComponent<Collider>().bounds);
        if(manager.Health!=healthBefore){Fail("Jumpable rock damaged the player during the jump.");yield break;}

        SetPlayer(player,cc,-1,slideBar.transform.position.z-1.5f);yield return null;
        if(!cc.isGrounded){cc.Move(Vector3.down*.05f);yield return null;}
        player.RequestSlide();yield return null;
        if(!player.IsSliding||cc.height>=2f){Fail("Slide did not lower the CharacterController.");yield break;}
        yield return new WaitForSeconds(.45f);
        if(manager.Health!=healthBefore){Fail("Slide bar damaged the player while sliding.");yield break;}
        yield return new WaitForSeconds(.55f);
        if(player.IsSliding||Mathf.Abs(cc.height-2f)>.01f){Fail("Slide did not restore the standing collider.");yield break;}

        SetPlayer(player,cc,0,jumpRock.transform.position.z-1.8f);cc.Move(Vector3.forward*2f);yield return null;
        if(manager.Health!=healthBefore-1){Fail("Solid obstacle collision did not apply exactly one health hit.");yield break;}
        yield return new WaitForSeconds(manager.damageCooldown + .1f);
        manager.TakeDamage(manager.Health);
        if(!manager.IsFinished||manager.gameOverPanel==null||!manager.gameOverPanel.activeInHierarchy)
        {Fail("Health depletion did not activate Game Over.");yield break;}
        Time.timeScale=1f;Button restart=null;
        foreach(Button b in FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(b.gameObject.name=="RestartButton"){restart=b;break;}
        if(restart==null||restart.onClick.GetPersistentEventCount()==0){Fail("Existing Restart button callback is missing.");yield break;}
        waitingForRestart=true;
        restart.onClick.Invoke();
    }

    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        if(restarted||!waitingForRestart||scene.name!="LostCave_Main")return;
        restarted=true;StartCoroutine(VerifyRestart());
    }

    private IEnumerator VerifyRestart()
    {
        yield return null;yield return null;
        GameManager manager=GameManager.Instance;PlayerMovement player=FindFirstObjectByType<PlayerMovement>();
        if(manager==null||player==null||manager.Score!=0||manager.Health!=manager.startingHealth||manager.IsFinished)
        {Fail("Restart did not reload a fresh runner state.");yield break;}
        Debug.Log("M5_PLAYMODE_SMOKE_PASS | autoRun=true | threeLanes=true | jumpClearance=true | slideClearance=true | crystalScore=true | obstacleDamage=true | gameOver=true | restart=true");
#if UNITY_EDITOR
        if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(0);
        else UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit(0);
#endif
    }

    private static void SetPlayer(PlayerMovement player,CharacterController cc,int lane,float z)
    {
        while(player.CurrentLane<lane)player.RequestLaneChange(1);
        while(player.CurrentLane>lane)player.RequestLaneChange(-1);
        cc.enabled=false;player.transform.position=new Vector3(lane*player.laneWidth,1f,z);cc.enabled=true;
        Physics.SyncTransforms();
    }
    private static string LaneError(PlayerMovement player,int lane)
    {
        return player.CurrentLane!=lane||Mathf.Abs(player.transform.position.x-lane*player.laneWidth)>.25f
            ?"Lane switch failed. Expected lane "+lane+", got "+player.CurrentLane+" at x="+player.transform.position.x+".":null;
    }
    private static void Fail(string message)
    {
        Debug.LogError("M5_PLAYMODE_SMOKE_FAIL | "+message);
#if UNITY_EDITOR
        if(Application.isBatchMode)UnityEditor.EditorApplication.Exit(2);
        else UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit(2);
#endif
    }
}








