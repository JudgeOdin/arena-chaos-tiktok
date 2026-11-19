#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Local editor jobs survive MCP connection loss during a domain reload.</summary>
[InitializeOnLoad]
public static class ArenaEditorJobs
{
    const string Request="Tools/ArenaJob.txt";
    static bool testing;
    static double began;
    static int phase, errors;
    static BattleArenaManager previousArena;
    static bool roundTest;
    static bool powerTest;
    static bool profileTest,classesTest,reliabilityTest;
    static ArenaGiftInbox testInbox;
    static string inboxFile;
    static AudioSource previousMusic;
    static ArenaFighter classArcher,classGuardian;
    static int previousDurationPreference;
    static ArenaFighter powerOwner,powerVictim;
    static float powerHealth;
    static float gameBegan;
    static bool clockStarted;
    static readonly StringBuilder report=new StringBuilder();
    static ArenaEditorJobs()
    {
        EditorApplication.update+=Update;
        if(SessionState.GetBool("ArenaJob.Active",false))
        {
            testing=true;roundTest=SessionState.GetBool("ArenaJob.Round",false);
            powerTest=SessionState.GetBool("ArenaJob.Power",false);
            profileTest=SessionState.GetBool("ArenaJob.Profile",false);classesTest=SessionState.GetBool("ArenaJob.Classes",false);reliabilityTest=SessionState.GetBool("ArenaJob.Reliability",false);
            began=EditorApplication.timeSinceStartup;
            Application.logMessageReceived+=Log;
        }
    }
    static void Update()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(testing){RunTest();return;}
        if(!File.Exists(Request))return;
        string command=File.ReadAllText(Request).Trim();File.Delete(Request);
        try
        {
            switch(command)
            {
                case "refresh":AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);Result("Asset refresh requested");break;
                case "connect-mcp":_ = MCPForUnity.Editor.Services.MCPServiceLocator.Bridge.StartAsync();Result("MCP connection requested");break;
                case "build-content": ArenaContentBuilder.Build();Result("Content built");break;
                case "validate": Result(ArenaValidation.Validate());break;
                case "prepare-match":ArenaMatchContentBuilder.Prepare();Result("Match settings and equipment prepared");break;
                case "round-test":
                case "power-test":
                case "profile-test":
                case "classes-test":
                case "reliability-test":
                case "play-test":
                    if(EditorApplication.isPlaying)throw new Exception("Already playing");
                    testing=true;roundTest=command=="round-test";phase=0;errors=0;clockStarted=false;report.Clear();began=EditorApplication.timeSinceStartup;
                    SessionState.SetBool("ArenaJob.Active",true);SessionState.SetBool("ArenaJob.Round",roundTest);
                    powerTest=command=="power-test";SessionState.SetBool("ArenaJob.Power",powerTest);
                    profileTest=command=="profile-test";SessionState.SetBool("ArenaJob.Profile",profileTest);
                    reliabilityTest=command=="reliability-test";SessionState.SetBool("ArenaJob.Reliability",reliabilityTest);
                    classesTest=command=="classes-test";SessionState.SetBool("ArenaJob.Classes",classesTest);previousDurationPreference=PlayerPrefs.GetInt("ArenaRoundSeconds",180);
                    Time.timeScale=1;Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;break;
                case "build-windows":ArenaValidation.BuildWindows();Result("Windows build queued");break;
                default:throw new Exception("Unknown local editor job");
            }
        }
        catch(Exception e){Result("FAILED: "+e);Debug.LogException(e);}
    }
    static void Log(string condition,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;report.AppendLine(type+": "+condition);}}
    static void RunTest()
    {
        if(!EditorApplication.isPlaying)
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&EditorApplication.timeSinceStartup-began>15)
            {report.AppendLine("Play Mode interrupted before completion");Finish("FAILED");}
            return;
        }
        double wallElapsed=EditorApplication.timeSinceStartup-began;
        try
        {
            var arena=UnityEngine.Object.FindAnyObjectByType<BattleArenaManager>();
            if(arena==null||arena.Fighters.Count==0){if(wallElapsed>120)throw new Exception("Arena startup timed out");return;}
            if(!clockStarted){Time.timeScale=1;gameBegan=Time.time;began=EditorApplication.timeSinceStartup;clockStarted=true;if(!roundTest)arena.AutomaticReinforcements=false;}
            float elapsed=Time.time-gameBegan;
            var presentation=arena.GetComponent<ArenaPresentationDirector>();
            int visibleEntrances=0;
            foreach(var fighter in arena.Fighters)
                if(fighter!=null&&fighter.IsEntering&&fighter.gameObject.activeInHierarchy)visibleEntrances++;
            if(visibleEntrances>1||presentation.ActiveEntrances>1)throw new Exception("Concurrent entrance animations detected");
            if(wallElapsed>120)throw new Exception("Play test timed out; simulated seconds="+elapsed+"; frames="+Time.frameCount);
            if(reliabilityTest)
            {
                if(phase==0&&elapsed>1)
                {
                    foreach(var actor in arena.Fighters){actor.enabled=false;actor.GiftIncreaseMaxHealth(5000);}
                    arena.FinishTimedRound();
                    inboxFile=Path.GetFullPath("Tools/TestResults/Inbox_"+Guid.NewGuid().ToString("N")+".json");
                    testInbox=new GameObject("Isolated inbox test").AddComponent<ArenaGiftInbox>();testInbox.UseTestStore(inboxFile);
                    var parser=new GameObject("Isolated LIVE parser").AddComponent<ArenaLiveBridge>();
                    var liveSettings=ScriptableObject.CreateInstance<ArenaLiveSettings>();
                    liveSettings.gifts=new[]{new ArenaLiveSettings.Binding{giftId="fixture-rose",type=GiftEventType.Rose}};
                    parser.ConfigureForTest(liveSettings);
                    var rawInbox=new GameObject("Raw provider fixture inbox").AddComponent<ArenaGiftInbox>();rawInbox.UseTestStore(inboxFile+"-provider.json");
                    string raw="{\"event\":\"gift\",\"data\":{\"giftId\":\"fixture-rose\",\"userId\":\"fixture-user\",\"msgId\":\"msg-1\",\"groupId\":\"group-1\",\"giftType\":1,\"repeatCount\":1}}";
                    if(!parser.Process(raw,rawInbox)||!parser.Process(raw,rawInbox)||!parser.Process(raw.Replace("msg-1","msg-3").Replace("repeatCount\":1","repeatCount\":3"),rawInbox)||rawInbox.PendingUnits!=3)throw new Exception("Provider payload combo normalization failed");
                    report.AppendLine("TikFinity-shaped raw JSON: repeated message deduplicated, cumulative combo 1 -> 3 delivered as three units");
                    UnityEngine.Object.DestroyImmediate(parser.gameObject);UnityEngine.Object.DestroyImmediate(rawInbox.gameObject);UnityEngine.Object.DestroyImmediate(liveSettings);
                    for(int i=0;i<16;i++)testInbox.Submit(new ArenaGiftEvent{eventId="blocked-"+i,viewerId="missing-"+i,displayName="Missing",type=GiftEventType.FirePunch});
                    var entry=new ArenaGiftEvent{eventId="entry",viewerId="reliable",displayName="InboxViewer",type=GiftEventType.Rose};
                    if(!testInbox.Submit(entry)||!testInbox.Submit(entry))throw new Exception("Inbox acceptance failed");
                    testInbox.Submit(new ArenaGiftEvent{eventId="combo-1",comboId="combo",count=1,viewerId="reliable",displayName="InboxViewer",type=GiftEventType.Health});
                    testInbox.Submit(new ArenaGiftEvent{eventId="combo-3",comboId="combo",count=3,viewerId="reliable",displayName="InboxViewer",type=GiftEventType.Health});
                    testInbox.Submit(new ArenaGiftEvent{eventId="combo-end",comboId="combo",count=3,viewerId="reliable",displayName="InboxViewer",type=GiftEventType.Health});
                    if(testInbox.PendingUnits!=20)throw new Exception("Duplicate/combo counting failed");phase=1;
                }
                if(phase==1&&elapsed>3)
                {
                    if(testInbox.DeliveredUnits!=0)throw new Exception("Result-screen gift was delivered too early");
                    UnityEngine.Object.DestroyImmediate(testInbox.gameObject);
                    testInbox=new GameObject("Reloaded inbox test").AddComponent<ArenaGiftInbox>();testInbox.UseTestStore(inboxFile);
                    if(testInbox.PendingUnits!=20)throw new Exception("Inbox disk restoration failed");
                    typeof(BattleArenaManager).GetField("<IsFinished>k__BackingField",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(arena,false);
                    report.AppendLine("Disk restoration, result-screen deferral, duplicate and combo delta counts verified");phase=2;
                }
                if(phase==2&&elapsed>9)
                {
                    var first=arena.FindViewerFighter("reliable");
                    if(first==null||first.MaxHealth!=145||testInbox.DeliveredUnits!=4)throw new Exception("Blocked gift head prevented delivery or health combo miscounted");
                    var second=arena.SpawnGiftFighter("reliable","SecondLife",null,Color.red);
                    var victim=arena.Fighters[1];victim.TakeDamage(10,first);victim.TakeDamage(20,second);
                    int records=0;foreach(var row in arena.GetRankedStandings())if(row.id=="reliable"){records++;if(row.damage!=30)throw new Exception("Shared damage total incorrect");}
                    if(records!=1)throw new Exception("Viewer appears twice in ranked standings");
                    BattleArenaManager.Standing shared=null;foreach(var row in arena.GetRankedStandings())if(row.id=="reliable")shared=row;
                    float previousPoints=shared.points;int previousKills=shared.kills;
                    arena.RecordRoundDamage(first,100,second);arena.NotifyElimination(second,first);
                    if(shared.damage!=30||shared.points!=previousPoints||shared.kills!=previousKills)throw new Exception("Same viewer farmed damage/KO points from own character");
                    report.AppendLine("Own-viewer damage and elimination cannot inflate shared score");
                    testInbox.Submit(new ArenaGiftEvent{eventId="fire-pair",viewerId="reliable",displayName="SecondLife",type=GiftEventType.FirePunch,count=2});
                    report.AppendLine("Fair queue delivery and one shared viewer score verified");phase=3;
                }
                if(phase==3&&elapsed>17)
                {
                    if(testInbox.DeliveredUnits!=6||testInbox.PendingUnits!=16)throw new Exception("Entering/cooldown gifts were lost");
                    report.AppendLine("Entering and cooldown deferral delivered both gifts");
                    var spawnReserve=typeof(BattleArenaManager).GetMethod("SpawnReserve",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    while(arena.LivingCount<50)spawnReserve.Invoke(arena,new object[]{0,1});
                    foreach(var actor in arena.Fighters)actor.enabled=false;
                    testInbox.Submit(new ArenaGiftEvent{eventId="full-arena-entry",viewerId="full-arena",displayName="WaitingViewer",type=GiftEventType.Rose});phase=4;
                }
                if(phase==4&&elapsed>19)
                {
                    if(testInbox.DeliveredUnits!=6||testInbox.PendingUnits!=17||arena.LivingCount!=50)throw new Exception("Full arena gift dropped or capacity exceeded");
                    arena.Fighters[1].EliminateFromMeteor(arena.FindViewerFighter("reliable"));phase=5;
                }
                if(phase==5&&elapsed>23)
                {
                    if(testInbox.DeliveredUnits!=7||testInbox.PendingUnits!=16||arena.FindViewerFighter("full-arena")==null)throw new Exception("Freed arena slot did not release pending gift");
                    arena.FinishTimedRound();if(arena.ChampionViewerId!=arena.GetRankedStandings()[0].id)throw new Exception("Winner differs from leaderboard");
                    report.AppendLine("Full 50-fighter arena deferred gift, delivered after one slot freed; winner matches HUD ranking; errors="+errors);
                    Finish(errors==0?"PASS":"FAILED");
                }
                return;
            }
            if(profileTest&&elapsed>1)
            {
                var first=arena.SpawnGiftFighter("viewer-A","SharedName",null,Color.red);
                var second=arena.SpawnGiftFighter("viewer-B","SharedName",null,Color.blue);
                float firstMax=first.MaxHealth,secondMax=second.MaxHealth;
                arena.ApplyGiftToViewer(GiftEventType.Health,"viewer-A","SharedName",null);
                if(first.MaxHealth!=firstMax+15||second.MaxHealth!=secondMax||arena.FindViewerFighter("viewer-B")!=second)
                    throw new Exception("Stable viewer identity routing failed");
                report.AppendLine("Two viewers with the same display name remain separate; gift reaches viewer-A only");
                bool musicPlaying=ArenaMusicPlayer.CurrentSource!=null&&ArenaMusicPlayer.CurrentSource.isPlaying&&ArenaMusicPlayer.CurrentSource.clip==ArenaContent.Load().battleMusic;
                if(!musicPlaying)throw new Exception("Battle music not playing");
                report.AppendLine("Imported battle music playback verified; runtime errors="+errors);
                Finish(errors==0?"PASS":"FAILED");return;
            }
            if(powerTest)
            {
                if(phase==0&&elapsed>1)
                {
                    powerOwner=arena.PlayerFighter;powerVictim=arena.Fighters[1];
                    foreach(var actor in arena.Fighters){actor.GiftIncreaseMaxHealth(5000);actor.enabled=false;}
                    powerOwner.TakeDamage(100,powerVictim);
                    powerOwner.PowerUps.Activate(ArenaPower.Vampire);
                    powerOwner.PowerUps.Activate(ArenaPower.Fire);
                    powerOwner.PowerUps.Activate(ArenaPower.Poison);
                    if(powerOwner.PowerUps.Activate(ArenaPower.Fire))throw new Exception("Cooldown failed");
                    float before=powerOwner.Health;
                    powerOwner.ApplyLimbContactDamage(powerVictim);
                    if(powerOwner.Health<=before)throw new Exception("Vampire healing failed");
                    powerHealth=powerVictim.Health;
                    report.AppendLine("Contact damage, vampire healing and cooldown verified");phase=1;
                }
                if(phase==1&&elapsed>4)
                {
                    if(powerVictim.Health>=powerHealth)throw new Exception("Damage over time failed");
                    report.AppendLine("Burn/poison damage observed: "+(powerHealth-powerVictim.Health));
                    powerVictim.PowerUps.Activate(ArenaPower.HolyShield);
                    float before=powerVictim.Health;powerVictim.TakeDamage(10,powerOwner);
                    if(powerVictim.Health!=before)throw new Exception("Shield did not absorb damage");
                    foreach(ArenaPower power in Enum.GetValues(typeof(ArenaPower)))if(power!=ArenaPower.Meteor)powerOwner.PowerUps.Activate(power);
                    if(powerOwner.PowerUps.MovementMultiplier<=0)throw new Exception("Invalid composed movement");
                    report.AppendLine("11 timed effects activated; shield absorption verified");phase=2;
                    ScreenCapture.CaptureScreenshot("Tools/TestResults/PowerShowcase.png");
                }
                if(phase==2&&elapsed>20)
                {
                    foreach(ArenaPower power in Enum.GetValues(typeof(ArenaPower)))if(powerOwner.PowerUps.Has(power))throw new Exception("Effect failed to expire: "+power);
                    if(Mathf.Abs(powerOwner.PowerUps.MovementMultiplier-1)>.001f)throw new Exception("Movement did not restore");
                    report.AppendLine("Timed effects expired; modifiers restored");
                    powerVictim.GiftIncreaseMaxHealth(100000);powerVictim.GiftShield(100000);powerVictim.PowerUps.Activate(ArenaPower.Ice);
                    arena.SpawnGiftFighter("@MeteorQueueTarget",Color.cyan);
                    powerHealth=powerOwner.Health;powerOwner.PowerUps.Activate(ArenaPower.Meteor);phase=3;
                }
                if(phase==3&&elapsed>21.3f)
                {ScreenCapture.CaptureScreenshot("Tools/TestResults/MeteorDetonation.png");phase=4;}
                if(phase==4&&elapsed>23)
                {
                    foreach(var opponent in arena.Fighters)if(opponent!=null&&opponent!=powerOwner&&opponent.IsAlive)throw new Exception("Meteor left a rival alive: "+opponent.DisplayName);
                    if(!powerOwner.IsAlive||powerOwner.Health!=powerHealth)throw new Exception("Meteor damaged its sender");
                    if(arena.IsFinished)throw new Exception("Meteor incorrectly ended the timed round");
                    report.AppendLine("Meteor eliminated all rivals including shielded/armored/queued targets; sender unharmed; errors="+errors);
                    phase=5;
                }
                if(phase==5&&elapsed>36)
                {
                    powerVictim=arena.SpawnGiftFighter("@MeteorAfterSenderDeath",Color.cyan);
                    if(powerVictim==null||!powerOwner.PowerUps.Activate(ArenaPower.Meteor))throw new Exception("Second meteor cast rejected after global interval");
                    powerOwner.EliminateFromMeteor(powerVictim);
                    UnityEngine.Object.Destroy(powerOwner.gameObject);phase=6;
                }
                if(phase==6&&elapsed>38)
                {
                    if(powerVictim.IsAlive||arena.IsFinished)throw new Exception("Meteor failed after sender death/destruction");
                    report.AppendLine("Cast completed after sender died and was destroyed during fall; no premature round end; errors="+errors);
                    Finish(errors==0?"PASS":"FAILED");
                }
                return;
            }
            if(classesTest)
            {
                if(phase==0&&elapsed>1)
                {
                    arena.SelectRoundLength(ArenaRoundLength.FiveMinutes);if(arena.RemainingSeconds<290)throw new Exception("5 minute selection failed");
                    arena.SelectRoundLength(ArenaRoundLength.ThreeMinutes);if(arena.RemainingSeconds<170||arena.RemainingSeconds>180)throw new Exception("3 minute selection failed");
                    int n=0;foreach(var actor in arena.Fighters){actor.enabled=false;actor.GetComponent<Rigidbody>().position=new Vector3(-6+n%7*1.7f,1,5.5f);n++;}
                    classArcher=arena.SpawnClassFighter("test-archer","TestArcher",null,ArenaFighterRole.Archer);
                    classGuardian=arena.SpawnClassFighter("test-guardian","TestGuardian",null,ArenaFighterRole.Guardian);
                    if(classArcher==null||classGuardian==null)throw new Exception("Gift classes missing");
                    report.AppendLine("3/5 minute duration selection and both character gift routes verified");phase=1;
                }
                if(phase==1&&elapsed>9)
                {
                    if(classArcher.IsEntering||classGuardian.IsEntering)throw new Exception("Class entrance timed out");
                    classArcher.GetComponent<Rigidbody>().position=new Vector3(-3,1,0);classGuardian.GetComponent<Rigidbody>().position=new Vector3(3,1,0);
                    classGuardian.transform.rotation=Quaternion.LookRotation(Vector3.left);classGuardian.enabled=false;
                    var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                    typeof(ArenaFighter).GetField("target",flags).SetValue(classArcher,classGuardian);
                    typeof(ArenaFighter).GetField("nextTargetSearch",flags).SetValue(classArcher,Time.time+30);
                    report.AppendLine("Archer/guardian gear and entrances ready; ranged duel started");phase=2;
                }
                if(phase==2&&elapsed>17)
                {
                    var pool=arena.GetComponent<ArenaArrowPool>();if(pool==null||pool.HitCount<1)throw new Exception("No real arrow collision hits");
                    if(classGuardian.ShieldHealth>=80&&classGuardian.Health>=classGuardian.MaxHealth)throw new Exception("Guardian did not take projectile damage");
                    classGuardian.TakeDamage(classGuardian.ShieldHealth+classGuardian.Health-20,null);
                    bool attack;var move=classGuardian.TacticalBrain.Decide(classArcher,1.25f,out attack);
                    if(classGuardian.TacticalBrain.CurrentTactic!=ArenaTacticalBrain.Tactic.Retreat||Vector3.Dot(move,Vector3.right)<=0)throw new Exception("Low health retreat failed");
                    report.AppendLine("Arrow collision hits="+pool.HitCount+"; guardian shield consumption and low health retreat verified; errors="+errors);
                    Finish(errors==0?"PASS":"FAILED");
                }
                return;
            }
            if(roundTest)
            {
                if(phase==0&&elapsed>4)
                {
                    previousArena=arena;previousMusic=ArenaMusicPlayer.CurrentSource;
                    var survivor=arena.PlayerFighter;survivor.GiftIncreaseMaxHealth(5000);
                    foreach(var opponent in arena.Fighters)if(opponent!=survivor)opponent.TakeDamage(10000,survivor);
                    if(arena.IsFinished)throw new Exception("Last survivor ended timed round early");
                    typeof(BattleArenaManager).GetField("roundEndsAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(arena,Time.time+7);
                    report.AppendLine("Round remains active with one survivor");phase=1;
                }
                if(phase==1&&elapsed>8)
                {
                    if(arena.LivingCount<6)throw new Exception("Reserve squad did not replenish combat");
                    report.AppendLine("Reserve squad restored population: "+arena.LivingCount);phase=2;
                }
                if(phase==2&&elapsed>12)
                {
                    if(!arena.IsFinished||arena.ChampionName!=arena.PlayerFighter.DisplayName)throw new Exception("Timed scoring finish failed");
                    report.AppendLine("Time expiry selected score champion: "+arena.ChampionName);phase=3;
                }
                if(phase==3&&arena!=previousArena&&arena.Fighters.Count>=10)
                {
                    if(previousMusic==null||ArenaMusicPlayer.CurrentSource!=previousMusic||!previousMusic.isPlaying)throw new Exception("Music source restarted across round reload");
                    report.AppendLine("Next round rebuilt; same looping music source continues; errors="+errors);Finish(errors==0?"PASS":"FAILED");
                }
                else if(phase==3&&elapsed>60)throw new Exception("Automatic next round timed out");
                return;
            }
            if(phase==0&&elapsed>4)
            {
                Snapshot(arena,"initial");
                if(arena.Fighters.Count!=10)throw new Exception("Initial fighter count != 10");
                for(int i=0;i<3;i++)arena.SpawnGiftFighter("@TestEntry"+i,Color.cyan);
                report.AppendLine("Serialized queue: active="+presentation.ActiveEntrances+", waiting="+presentation.PendingEntrances);
                if(presentation.ActiveEntrances!=1||presentation.PendingEntrances!=2)throw new Exception("Entrance queue did not serialize requests");
                var recipient=arena.FindSenderFighter("TestEntry0");
                float maximum=recipient.MaxHealth, playerMaximum=arena.PlayerFighter.MaxHealth;
                arena.ApplyGiftToPlayer(GiftEventType.Health,"TestEntry0");
                if(recipient.MaxHealth!=maximum+15f||arena.PlayerFighter.MaxHealth!=playerMaximum)throw new Exception("Gift recipient routing failed");
                report.AppendLine("Gift recipient routing verified");
                phase=1;
            }
            if(phase==1&&elapsed>16)
            {
                Snapshot(arena,"after-combat-and-entrances");
                foreach(var fighter in arena.Fighters)
                    if(fighter!=null&&fighter.IsEntering)throw new Exception("Entrance queue did not finish: "+fighter.DisplayName);
                int hits=0;foreach(var driver in UnityEngine.Object.FindObjectsByType<KayKitFighterDriver>())hits+=driver.ContactHitCount;
                report.AppendLine("Limb contacts="+hits);
                if(hits==0)throw new Exception("No animated limb damage contacts observed");
                Directory.CreateDirectory("Tools/TestResults");
                ScreenCapture.CaptureScreenshot("Tools/TestResults/PlayMode.png");
                arena.GetComponent<ArenaPresentationDirector>().PlayMegaBlast();phase=2;
            }
            if(phase==2&&elapsed>22)
            {
                Snapshot(arena,"after-gift");
                report.AppendLine("Runtime errors="+errors);
                Finish(errors==0?"PASS":"FAILED");
            }
        }
        catch(Exception e){report.AppendLine(e.ToString());Finish("FAILED");}
    }
    static void Snapshot(BattleArenaManager arena,string name)
    {
        report.AppendLine(name+": fighters="+arena.Fighters.Count+", alive="+arena.LivingCount);
        foreach(var fighter in arena.Fighters)
        {
            if(fighter==null||!fighter.IsAlive)continue;
            var driver=fighter.GetComponentInChildren<KayKitFighterDriver>();
            var animator=fighter.GetComponentInChildren<Animator>();
            report.AppendLine(fighter.DisplayName+" HP="+fighter.Health+" entering="+fighter.IsEntering+" model="+(driver!=null)+" rootMotion="+(animator!=null&&animator.applyRootMotion)+" pos="+fighter.transform.position);
            if(driver==null||animator==null||animator.applyRootMotion)throw new Exception("Invalid fighter model/animator");
            if(!fighter.IsEntering&&(fighter.transform.position.y<.7f||fighter.transform.position.y>1.35f))throw new Exception("Fighter root lifted or fell through floor");
        }
    }
    static void Finish(string result)
    {
        testing=false;if(classesTest){PlayerPrefs.SetInt("ArenaRoundSeconds",previousDurationPreference);PlayerPrefs.Save();}SessionState.SetBool("ArenaJob.Active",false);Application.logMessageReceived-=Log;
        Directory.CreateDirectory("Tools/TestResults");
        File.WriteAllText("Tools/TestResults/"+(reliabilityTest?"GiftReliability":classesTest?"ClassesAndAI":profileTest?"ViewerProfiles":powerTest?"PowerUps":roundTest?"RoundCycle":"PlayMode")+".txt",result+"\n"+report);
        EditorApplication.isPlaying=false;Result(result+" Play Mode");
    }
    static void Result(string text)
    {Directory.CreateDirectory("Tools/TestResults");File.WriteAllText("Tools/TestResults/EditorJob.txt",DateTime.Now.ToString("s")+"\n"+text);}
}
#endif
