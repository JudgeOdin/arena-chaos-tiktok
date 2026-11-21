#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ArenaModePlayTest : MonoBehaviour
{
    const string Active = "ArenaModes.Test";
    const string Report = "Tools/TestResults/ArenaModes.txt";
    static readonly string[] Settings = { "ArenaActiveMode", "ArenaNextMode", "ArenaDuelTournament", "ArenaAnnouncerEnabled" };
    readonly StringBuilder report = new StringBuilder();
    string store;
    string testId;
    string failure;
    int errors;

    [InitializeOnLoadMethod]
    static void Configure()
    {
        EditorApplication.playModeStateChanged -= StateChanged;
        EditorApplication.playModeStateChanged += StateChanged;
    }

    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play Mode already active");
        foreach (string key in Settings)
        {
            SessionState.SetBool(key+".Present",PlayerPrefs.HasKey(key));
            SessionState.SetInt(key+".Backup",PlayerPrefs.GetInt(key));
        }
        PlayerPrefs.SetInt("ArenaActiveMode",0);
        PlayerPrefs.DeleteKey("ArenaNextMode");
        PlayerPrefs.SetInt("ArenaAnnouncerEnabled",0);
        SessionState.SetBool(Active,true);
        EditorApplication.isPaused=false;
        EditorApplication.isPlaying=true;
    }

    static void StateChanged(PlayModeStateChange change)
    {
        if (change!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Active,false)) return;
        SessionState.SetBool(Active,false);
        foreach (string key in Settings)
        {
            if (SessionState.GetBool(key+".Present",false)) PlayerPrefs.SetInt(key,SessionState.GetInt(key+".Backup",0));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if (!SessionState.GetBool(Active,false)) return;
        var obj=new GameObject("Arena Mode Play Test");
        DontDestroyOnLoad(obj);
        obj.AddComponent<ArenaModePlayTest>();
    }

    IEnumerator Start()
    {
        testId="mode-test-"+Guid.NewGuid().ToString("N");
        store=Path.Combine(Path.GetTempPath(),testId+".json");
        ArenaGiftInbox.Instance.UseTestStore(store);
        Application.logMessageReceived+=Capture;
        var run=Run();
        while (true)
        {
            object item;
            try
            {
                if (!run.MoveNext()) break;
                item=run.Current;
            }
            catch (Exception e) { failure=e.ToString(); break; }
            yield return item;
        }
        Application.logMessageReceived-=Capture;
        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report,(failure==null&&errors==0?"PASSED":"FAILED")+"\n"+report+failure);
        foreach(var file in new[]{store,store+".bak"})
            if (File.Exists(file)) File.Delete(file);
        EditorApplication.isPlaying=false;
    }

    void Capture(string condition,string trace,LogType type)
    {
        if (type==LogType.Error||type==LogType.Exception||type==LogType.Assert)
        { errors++; report.AppendLine(condition); }
    }

    IEnumerator Run()
    {
        string deferred=testId+"-deferred";
        for(int i=0;i<=10;i++)
        {
            yield return null;
            var arena=FindAnyObjectByType<BattleArenaManager>();
            Require(arena!=null && arena.Mode==(ArenaMode)i,"Scene did not open requested mode "+i);
            Require(FindAnyObjectByType<ArenaBroadcastHUD>()!=null,"HUD missing");
            string id=testId+"-"+i;
            if (i == 0)
            {
                var live = ArenaGiftInbox.Instance;
                var local = ArenaGiftInbox.Local;
                int baseline = live.PendingUnits;
                Require(local.Submit(new ArenaGiftEvent
                {
                    eventId=testId+"-local", viewerId="arena-test:operator",
                    displayName="Local probe", type=GiftEventType.Health
                }),"Local queue did not accept probe");
                Require(local.PendingUnits==1 && live.PendingUnits==baseline,"Local gift entered LIVE queue");
                local.ClearLocal();
                Require(local.PendingUnits==0 && live.PendingUnits==baseline,"Clearing test queue changed LIVE queue");
                Require(ArenaLiveBridge.IsOffline,"Automated tests should be offline");

                var caster=arena.Fighters[0];
                var target=arena.Fighters[1];
                Vector3 at=target.transform.position;
                var marker=arena.MarkMeteor(at,caster);
                Require(arena.ModeSteering(target,caster,out var escape) && escape.sqrMagnitude>.5f,"Marked meteor did not trigger dodge");
                Destroy(marker);
            }
            if (arena.IsTournament)
            {
                Require(!arena.ProcessChat(id,"Test",null,"!katıl"),"Tournament accepted chat entry");
                for(int j=0;j<8;j++) Require(arena.TryJoinFromLike(id+j,"Test "+j,null),"Tournament entry failed");
                yield return null;
                for(int j=0;j<7;j++)
                {
                    var left=(ArenaFighter)Get(arena,"leftDuelist");
                    var right=(ArenaFighter)Get(arena,"rightDuelist");
                    Require(left!=null && right!=null && left.IsEntering && right.IsEntering,"Both gate entrances must start");
                    Require(left.transform.position.x<-arena.ArenaRadius && right.transform.position.x>arena.ArenaRadius,"Duelists must start behind opposite gates");
                    Require(!arena.DuelCombatStarted && !arena.CanAttack(left,right),"Combat opened during gate entrance");
                    float limit=Time.realtimeSinceStartup+15f;
                    while (!arena.DuelCombatStarted && Time.realtimeSinceStartup<limit) yield return null;
                    Require(arena.DuelCombatStarted && !left.IsEntering && !right.IsEntering,"Duel did not start after both arrivals");
                    Require(arena.RemainingSeconds>57,"Entrance consumed duel clock");
                    Set(arena,"roundEndsAt",Time.time-1);
                    Call(arena,"UpdateTournament");
                    if (j<6) { Set(arena,"nextDuelAt",Time.time-1); Call(arena,"UpdateTournament"); }
                }
                Require(arena.IsFinished && !string.IsNullOrEmpty(arena.ChampionName),"Bracket did not produce champion");
            }
            else
            {
                string json=JsonConvert.SerializeObject(new { @event="chat",data=new { userId=id, nickname="Test "+i,msgId=id,comment=i==8?"!okçu":"!katıl" }});
                Require(ArenaLiveBridge.Current.Process(json),"Local chat event declined");
                Require(ArenaLiveBridge.Current.Process(json),"Duplicate event not acknowledged");
                if (arena.Mode==ArenaMode.Classes) Set(arena,"classUntil",Time.time-1);
                Call(arena,"DeliverEntries");
                var actor=arena.FindViewerFighter(id);
                Require(actor!=null && arena.Fighters.Count(x=>x!=null && x.ViewerId==id)==1,"Duplicate join or missing viewer");
                if (arena.Mode==ArenaMode.Zombies || arena.Mode==ArenaMode.Giant || arena.Mode==ArenaMode.GuestChampion)
                {
                    Call(arena,"TickMode");
                    Require(!arena.IsFinished,"Entering players counted as eliminated");
                }
                actor.gameObject.SetActive(true);
                actor.IsEntering=false;
                if (arena.Mode==ArenaMode.Classes) Require(actor.Role==ArenaFighterRole.Archer,"Class choice lost");
                if (arena.Mode==ArenaMode.Bridge)
                {
                    Call(arena,"TickMode");
                    Require(arena.ModeScore.Contains("1/3"),"Single-player defense did not start");
                }
                if (arena.Mode==ArenaMode.King)
                {
                    Require(arena.ModeScore.Contains("100"),"King health missing");
                    var kings=(ArenaFighter[]) Get(arena,"kings");
                    Require(kings.All(x=>x.IsRoyal && x.GetComponentInChildren<ArenaAnimatedCharacter>()?.HasArenaMaterial==true),
                        "Crowned models with arena materials required");
                    Require(kings[0].NameplatePosition.y-kings[0].transform.position.y>1.85f,"King nameplate did not clear crown");
                    if (SessionState.GetBool("ArenaModes.RoyalCapture",false))
                    {
                        yield return new WaitForSeconds(.35f);
                        ScreenCapture.CaptureScreenshot("Tools/TestResults/Royal-Portrait.png");
                        yield return new WaitForEndOfFrame();
                        var preview=ArenaPhonePreviewOverlay.Create();
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot("Tools/TestResults/PhoneLivePreview.png");
                        yield return new WaitForEndOfFrame();
                        Destroy(preview);
                        yield return null;
                    }
                    int own=arena.CanAttack(actor,kings[0])?1:0;
                    Require(!arena.CanAttack(actor,kings[own]) && arena.CanAttack(actor,kings[1-own]),"Team target filter");
                    int castSide=arena.MeteorSide(actor);
                    Require(!arena.MeteorTarget(null,kings[own],castSide) && arena.MeteorTarget(null,kings[1-own],castSide),
                        "Delayed meteor changed target team after caster departure");
                    Require(arena.ProcessChat(id,"Test",null,"!hedef kral"),"Shield target command");
                    Require(arena.TryApplyGiftToViewer(GiftEventType.TeamShield,id,"Test",null),"King shield not applied");
                    kings[own].TakeDamage(30,kings[1-own]);
                    Require(kings[own].ShieldHealth>0 && kings[own].Health==kings[own].MaxHealth,"Shield did not absorb");
                    kings[1-own].TakeDamage(10000,kings[own]);
                }
                if (arena.Mode==ArenaMode.Zombies)
                {
                    Require(arena.ModeScore.Contains("3"),"Three starting zombies missing");
                    Require(arena.Fighters.Count(x=>x.IsZombie && x.GetComponentInChildren<ArenaAnimatedCharacter>()?.HasArenaMaterial==true)==3,"Distinct starting zombie models missing");
                    var zombie=arena.Fighters.First(x=>x.IsZombie);
                    actor.IsEntering=false;
                    if (SessionState.GetBool("ArenaModes.InfectionCapture",false))
                    {
                        actor.transform.SetPositionAndRotation(Vector3.up,Quaternion.identity);
                        var combatBody=actor.GetComponent<Rigidbody>();
                        combatBody.position=Vector3.up;
                        combatBody.rotation=Quaternion.identity;
                    }
                    actor.TakeDamage(10000,zombie);
                    Call(arena,"TickMode");
                    var infected=arena.FindViewerFighter(id);
                    Require(infected!=null && infected!=actor && infected.IsZombie && infected.GetComponentInChildren<ArenaAnimatedCharacter>()!=null && infected.DisplayName==actor.DisplayName,"Same-name zombie model missing");
                    if (SessionState.GetBool("ArenaModes.InfectionCapture",false))
                    {
                        var camera=arena.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>().transform;
                        var previousPosition=camera.position;
                        var previousRotation=camera.rotation;
                        var center=infected.transform.position+Vector3.up*.55f;
                        camera.position=center+new Vector3(2.1f,2.6f,4.5f);
                        camera.LookAt(center);
                        yield return new WaitForSeconds(.28f);
                        ScreenCapture.CaptureScreenshot("Tools/TestResults/Infection-Turn.png");
                        yield return new WaitForSeconds(.54f);
                        ScreenCapture.CaptureScreenshot("Tools/TestResults/Infection-Character.png");
                        yield return new WaitForEndOfFrame();
                        camera.SetPositionAndRotation(previousPosition,previousRotation);
                    }
                }
                if (arena.Mode==ArenaMode.Gold)
                {
                    Require(arena.ModeScore.Contains("0 : 0"),"Delivery score missing");
                    for (int delivery=0;delivery<3;delivery++)
                    {
                        Set(arena,"carrier",actor);
                        Require(arena.ModeSpeed(actor)<1,"Carrier not slowed");
                        int side=(int)typeof(BattleArenaManager).GetMethod("SideOf",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(arena,new object[]{actor});
                        actor.transform.position=new Vector3((side==0?-1:1)*arena.ArenaRadius*.6f,1,0);
                        Call(arena,"TickMode");
                    }
                    Require(arena.IsFinished && arena.ModeResult.Contains("kazandı"),"Three delivered gold did not finish");
                }
                if (arena.Mode==ArenaMode.Giant || arena.Mode==ArenaMode.GuestChampion)
                {
                    Require(arena.ModeScore.Contains("100"),"Boss health missing");
                    var giant=(ArenaFighter)Get(arena,"boss");
                    Require(Mathf.Abs(giant.transform.position.x)<.1f && Mathf.Abs(giant.transform.position.z)<.1f,"Giant did not spawn in arena center");
                    Require(giant.GetComponentInChildren<ArenaModeEmblem>()!=null,"Boss emblem missing");
                    Set(arena,"strikeAt",Time.time-1);
                    Call(arena,"TickMode");
                    Require((GameObject)Get(arena,"warning")!=null,"Boss telegraph missing");
                    ((ArenaFighter)Get(arena,"boss")).TakeDamage(10000,actor);
                }
                if (arena.Mode==ArenaMode.Bounty)
                {
                    var demoTarget=arena.Fighters.First(x=>x!=null && x.IsBot && x.IsAlive);
                    Set(arena,"marked",demoTarget);
                    Call(arena,"TickMode");
                    var emblem=(ArenaModeEmblem)Get(arena,"specialMarker");
                    Require(emblem!=null && emblem.transform.Find("Ivory bounty skull")!=null,"Bounty skull asset missing");
                    if (SessionState.GetBool("ArenaModes.RoyalCapture",false))
                    {
                        var before=emblem.transform.rotation;
                        yield return new WaitForSeconds(.30f);
                        Require(Quaternion.Angle(before,emblem.transform.rotation)>4f,"Skull did not rotate");
                        ScreenCapture.CaptureScreenshot("Tools/TestResults/Bounty-Portrait.png");
                        yield return new WaitForEndOfFrame();
                    }
                    Set(arena,"marked",actor);
                    actor.gameObject.SetActive(false);
                    Call(arena,"TickMode");
                    Require((ArenaFighter)Get(arena,"marked")!=actor,"Mark did not transfer on departure");
                }
                if (arena.Mode==ArenaMode.Survival)
                    Require(ArenaGiftInbox.Instance.Submit(new ArenaGiftEvent { eventId=deferred,viewerId=deferred,displayName="Pending",type=GiftEventType.Health }),"Pending gift submit failed");
                if (arena.Mode==ArenaMode.King)
                    Require(ArenaGiftInbox.Instance.PendingUnits==1,"Pending gift disappeared during scene transition");
                Set(arena,"roundEndsAt",Time.time-1);
                Call(arena,"Update");
                Require(arena.IsFinished,"Timeout did not complete "+arena.Mode);
                Require(arena.ProcessChat(id,"Test",null,"!hazır"),"Ready registration declined");
            }
            report.AppendLine($"{arena.Mode}: start, end, and scene change requested");
            arena.RequestMode((ArenaMode)((i+1)%11));
            arena.AdvanceMode();
            yield return null;
        }
        yield return null;
        Require(FindAnyObjectByType<BattleArenaManager>()?.Mode==ArenaMode.Survival,"Final scene did not rotate back");
    }

    static void Require(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
    static object Get(BattleArenaManager arena,string field) =>
        typeof(BattleArenaManager).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(arena);
    static void Set(BattleArenaManager arena,string field,object value) =>
        typeof(BattleArenaManager).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(arena,value);
    static void Call(BattleArenaManager arena,string method) =>
        typeof(BattleArenaManager).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(arena,null);
}
#endif

