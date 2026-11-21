using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>Opt-in standalone test: -arenaSmokeTest -arenaFighters 10|20|50.</summary>
public sealed class ArenaRuntimeSmokeTest : MonoBehaviour
{
    int errors;
    float seconds;
    int frames;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-arenaSmokeTest")<0)return;
        var obj=new GameObject("Standalone validation");obj.AddComponent<ArenaRuntimeSmokeTest>();
    }
    void Log(string message,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.logMessageReceived+=Log;
        var arena=UnityEngine.Object.FindAnyObjectByType<BattleArenaManager>();
        yield return null;
        int initial=arena!=null?arena.Fighters.Count:0;
        if(arena==null){Application.Quit(1);yield break;}
        foreach(var fighter in arena.Fighters)fighter.GiftIncreaseMaxHealth(5000);
        yield return new WaitForSeconds(3);
        float until=Time.realtimeSinceStartup+10;
        while(Time.realtimeSinceStartup<until)
        {seconds+=Time.unscaledDeltaTime;frames++;yield return null;}
        int hits=0;float minY=float.MaxValue,maxY=float.MinValue;bool materials=true;
        foreach(var driver in UnityEngine.Object.FindObjectsByType<KayKitFighterDriver>())hits+=driver.ContactHitCount;
        foreach(var fighter in arena.Fighters)
        {
            minY=Mathf.Min(minY,fighter.transform.position.y);maxY=Mathf.Max(maxY,fighter.transform.position.y);
            foreach(var renderer in fighter.GetComponentsInChildren<Renderer>())
                if(renderer.enabled)foreach(var material in renderer.sharedMaterials)if(material==null||!material.shader.isSupported)materials=false;
        }
        string folder=Path.Combine(Application.dataPath,"../../TestResults");Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Standalone_"+initial+".png"));
        yield return new WaitForEndOfFrame();
        bool success=errors==0&&hits>0&&initial>=10&&materials;
        string report=(success?"PASS":"FAILED")+"\nFighters="+initial+"\nErrors="+errors+"\nLimbContacts="+hits+
            "\nAverageFPS="+(frames/Mathf.Max(.001f,seconds)).ToString("F1")+"\nSampleSeconds="+seconds.ToString("F1")+
            "\nGPU="+SystemInfo.graphicsDeviceName+"\nResolution="+Screen.width+"x"+Screen.height+
            "\nRenderPercent="+ArenaRenderQuality.Percent+"\nAllocatedMB="+(Profiler.GetTotalAllocatedMemoryLong()/1048576)+"\nRootY="+minY+".."+maxY+"\nMaterialsValid="+materials;
        File.WriteAllText(Path.Combine(folder,"Standalone_"+initial+".txt"),report);
        Application.logMessageReceived-=Log;Application.Quit(success?0:1);
    }
}
