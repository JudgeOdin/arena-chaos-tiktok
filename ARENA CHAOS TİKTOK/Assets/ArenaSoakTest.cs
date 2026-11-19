using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>Explicit opt-in real-time mixed-load test spanning full rounds and scene reloads.</summary>
public sealed class ArenaSoakTest : MonoBehaviour
{
    int errors,frames,rounds,maxActors;
    float sampled,begin,until,nextGift,nextPower,nextReport;
    long firstMemory,maxMemory;
    BattleArenaManager previous;
    ArenaGiftInbox inbox;
    string folder,run;
    readonly StringBuilder errorLog=new StringBuilder();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-arenaSoakTest")<0||UnityEngine.Object.FindAnyObjectByType<ArenaSoakTest>()!=null)return;
        var obj=new GameObject("Long-running standalone validation");DontDestroyOnLoad(obj);obj.AddComponent<ArenaSoakTest>();
    }
    void Log(string text,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;if(errors<=20)errorLog.AppendLine(text+"\n"+stack);}}
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.logMessageReceived+=Log;
        folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../TestResults"));Directory.CreateDirectory(folder);
        run=Guid.NewGuid().ToString("N");inbox=ArenaGiftInbox.CreateStore("soak-"+run+".json",true);
        int duration=1800;var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="-arenaSoakSeconds"&&int.TryParse(args[i+1],out int requested))duration=Mathf.Clamp(requested,60,7200);
        yield return new WaitForSecondsRealtime(3);
        begin=Time.realtimeSinceStartup;until=begin+duration;firstMemory=Profiler.GetTotalAllocatedMemoryLong();
        while(Time.realtimeSinceStartup<until)
        {
            var arena=UnityEngine.Object.FindAnyObjectByType<BattleArenaManager>();
            if(arena!=null&&arena.Fighters.Count>0)
            {
                if(previous!=arena){rounds++;previous=arena;}
                maxActors=Mathf.Max(maxActors,arena.LivingCount);
                if(Time.realtimeSinceStartup>nextGift)
                {
                    nextGift=Time.realtimeSinceStartup+6;
                    int index=frames%8;string viewer="soak-viewer-"+index;
                    inbox.Submit(new ArenaGiftEvent{eventId=run+"-"+frames,viewerId=viewer,displayName="LOAD TEST "+index,type=index%3==0?GiftEventType.ArcherEntry:index%3==1?GiftEventType.GuardianEntry:GiftEventType.Rose});
                }
                if(!arena.IsFinished&&Time.realtimeSinceStartup>nextPower)
                {
                    nextPower=Time.realtimeSinceStartup+2;
                    int n=0;foreach(var actor in arena.Fighters)
                    {
                        if(actor==null||!actor.IsAlive||actor.IsEntering)continue;
                        ArenaPower power=(ArenaPower)((frames+n)%12);
                        if(power==ArenaPower.Meteor&&((int)(Time.realtimeSinceStartup-begin))%60>2)power=ArenaPower.Fire;
                        actor.PowerUps.Activate(power);if(++n>=6)break;
                    }
                }
            }
            maxMemory=Math.Max(maxMemory,Profiler.GetTotalAllocatedMemoryLong());frames++;sampled+=Time.unscaledDeltaTime;
            if(Time.realtimeSinceStartup>nextReport){nextReport=Time.realtimeSinceStartup+30;Write("RUNNING");}
            yield return null;
        }
        Write(errors==0&&maxActors<=50&&rounds>=2?"PASS":"FAILED");
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"SoakFinal.png"));yield return new WaitForEndOfFrame();
        Application.logMessageReceived-=Log;Application.Quit(errors==0&&maxActors<=50&&rounds>=2?0:1);
    }
    void Write(string result)
    {
        File.WriteAllText(Path.Combine(folder,"SoakTest.txt"),result+"\nElapsedSeconds="+(Time.realtimeSinceStartup-begin).ToString("F1")+"\nRoundsObserved="+rounds+"\nErrors="+errors+"\nAverageFPS="+(frames/Mathf.Max(.01f,sampled)).ToString("F1")+"\nMaxAlive="+maxActors+"\nFirstAllocatedMB="+firstMemory/1048576+"\nPeakAllocatedMB="+maxMemory/1048576+"\nCurrentAllocatedMB="+Profiler.GetTotalAllocatedMemoryLong()/1048576+"\nPendingGiftUnits="+(inbox!=null?inbox.PendingUnits:0)+"\n"+errorLog);
    }
}
