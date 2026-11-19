#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

[Serializable] public sealed class ArenaPromoConfig
{
    public string scenario,folder,ffmpeg;
    public int size=960,fps=60;
    public float seconds=12;
}

/// <summary>Editor-only staged gameplay capture. Never included in a player build.</summary>
public sealed class ArenaPromoRecorder : MonoBehaviour
{
    [Serializable] sealed class Cue {public string kind;public float time;}
    [Serializable] sealed class CaptureReport
    {
        public string status,scenario,error,music,punch,spell;
        public int frames,width,height,fps,contacts,arrowHits;
        public bool casterAlive;
        public List<Cue> cues=new List<Cue>();
    }
    ArenaPromoConfig config;
    CaptureReport report;
    BattleArenaManager arena;
    ArenaFighter hero;
    Process encoder;
    RenderTexture target;
    Texture2D pixels;
    byte[] buffer;
    bool completed;
    public void Begin(ArenaPromoConfig value){config=value;StartCoroutine(Record());}
    IEnumerator Record()
    {
        Application.runInBackground=true;Time.timeScale=1;Time.captureFramerate=config.fps;
        while((arena=UnityEngine.Object.FindAnyObjectByType<BattleArenaManager>())==null||arena.Fighters.Count<2)yield return null;
        arena.AutomaticReinforcements=false;
        foreach(var receiver in UnityEngine.Object.FindObjectsByType<ArenaLiveBridge>()){receiver.Disconnect();receiver.enabled=false;}
        foreach(var actor in arena.Fighters){actor.enabled=false;actor.GiftIncreaseMaxHealth(config.scenario=="fire"&&actor!=arena.PlayerFighter?100:1500);}
        var hud=GameObject.Find("Broadcast HUD");if(hud!=null)hud.SetActive(false);
        var labels=arena.GetComponent<ArenaNameplateLayout>();if(labels!=null)labels.enabled=false;
        foreach(var actor in arena.Fighters)actor.SetNameplateVisible(false);
        report=new CaptureReport{status="RECORDING",scenario=config.scenario,fps=config.fps,width=config.size,height=config.size};
        var content=ArenaContent.Load();
        if(content!=null)
        {
            report.music=AssetDatabase.GetAssetPath(content.battleMusic);
            if(content.punches!=null&&content.punches.Length>0)report.punch=AssetDatabase.GetAssetPath(content.punches[0]);
        }
        if(config.scenario=="classes")
        {
            foreach(var actor in arena.Fighters)actor.gameObject.SetActive(false);
            var archer=arena.SpawnClassFighter("promo-archer","Okcu",null,ArenaFighterRole.Archer);
            var guardian=arena.SpawnClassFighter("promo-guardian","Muhafiz",null,ArenaFighterRole.Guardian);
            while(archer.IsEntering||guardian.IsEntering)yield return null;
            archer.enabled=false;guardian.enabled=false;archer.GiftIncreaseMaxHealth(350);guardian.GiftIncreaseMaxHealth(350);
            Position(archer,new Vector3(-2.6f,1,0));Position(guardian,new Vector3(2.6f,1,0));
            archer.transform.rotation=Quaternion.LookRotation(Vector3.right);guardian.transform.rotation=Quaternion.LookRotation(Vector3.left);
            hero=archer;
        }
        else
        {
            hero=arena.PlayerFighter;
            Position(hero,new Vector3(0,1,-1));
            int index=0;
            foreach(var actor in arena.Fighters)
            {
                if(actor==hero)continue;
                if(config.scenario=="fire"&&index>=4){actor.gameObject.SetActive(false);continue;}
                float angle=index*Mathf.PI*2/(config.scenario=="fire"?4:9);
                float radius=config.scenario=="fire"?2.1f:4.8f;
                Position(actor,new Vector3(Mathf.Cos(angle)*radius,1,Mathf.Sin(angle)*radius));index++;
            }
        }
        var presentation=arena.GetComponent<ArenaPresentationDirector>();
        if(presentation!=null)presentation.StopAllCoroutines();
        var camera=Camera.main;var brain=camera.GetComponent<CinemachineBrain>();if(brain!=null)brain.enabled=false;
        camera.rect=new Rect(0,0,1,1);camera.fieldOfView=config.scenario=="meteor"?48:45;
        camera.transform.position=config.scenario=="meteor"?new Vector3(0,12,-12):config.scenario=="fire"?new Vector3(0,6.8f,-8):new Vector3(0,7.5f,-9.5f);
        camera.transform.LookAt(new Vector3(0,.8f,0));
        if(config.scenario!="meteor")typeof(BattleArenaManager).GetField("arenaRadius",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(arena,5f);
        foreach(var actor in arena.Fighters)if(actor!=null&&actor.gameObject.activeSelf){actor.SetNameplateVisible(false);actor.enabled=true;}
        for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();
        if(Screen.width!=config.size||Screen.height!=config.size){Fail("Unexpected Game View size: "+Screen.width+"x"+Screen.height);yield break;}
        if(!OpenEncoder()){yield break;}
        int total=Mathf.RoundToInt(config.seconds*config.fps),lastContacts=0,lastArrows=0;
        var drivers=UnityEngine.Object.FindObjectsByType<KayKitFighterDriver>();
        for(int frame=0;frame<total;frame++)
        {
            if(config.scenario=="meteor"&&frame==config.fps*3)
            {hero.PowerUps.Activate(ArenaPower.Meteor);report.cues.Add(new Cue{kind="meteor-cast",time=3});report.cues.Add(new Cue{kind="meteor-impact",time=4});}
            if(config.scenario=="fire"&&frame==config.fps/2)
            {hero.PowerUps.Activate(ArenaPower.Fire);report.cues.Add(new Cue{kind="fire-cast",time=.5f});}
            yield return new WaitForEndOfFrame();
            string failure=null;
            try
            {
                ScreenCapture.CaptureScreenshotIntoRenderTexture(target);
                var previous=RenderTexture.active;RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,config.size,config.size),0,0,false);RenderTexture.active=previous;
                pixels.GetRawTextureData<byte>().CopyTo(buffer);encoder.StandardInput.BaseStream.Write(buffer,0,buffer.Length);
                report.frames++;
                int contacts=0;foreach(var driver in drivers)if(driver!=null)contacts+=driver.ContactHitCount;
                if(contacts>lastContacts){report.cues.Add(new Cue{kind="punch",time=(float)frame/config.fps});lastContacts=contacts;}
                var arrows=arena.GetComponent<ArenaArrowPool>();int arrowHits=arrows!=null?arrows.HitCount:0;
                if(arrowHits>lastArrows){report.cues.Add(new Cue{kind="arrow",time=(float)frame/config.fps});lastArrows=arrowHits;}
                if(frame%config.fps==0)File.WriteAllText(Path.Combine(config.folder,"progress.txt"),config.scenario+" "+frame+"/"+total);
            }
            catch(Exception exception){failure=exception.ToString();}
            if(failure!=null){Fail(failure);yield break;}
        }
        report.contacts=lastContacts;report.arrowHits=lastArrows;report.casterAlive=hero!=null&&hero.IsAlive;
        encoder.StandardInput.Close();encoder.WaitForExit();
        report.status=encoder.ExitCode==0?"PASS":"FAILED";completed=true;
        WriteReport();Cleanup();EditorApplication.isPlaying=false;
    }
    void Position(ArenaFighter actor,Vector3 position)
    {actor.transform.position=position;var body=actor.GetComponent<Rigidbody>();body.position=position;body.linearVelocity=Vector3.zero;}
    bool OpenEncoder()
    {
        try
        {
            Directory.CreateDirectory(config.folder);
            target=new RenderTexture(config.size,config.size,0,RenderTextureFormat.ARGB32);target.Create();
            pixels=new Texture2D(config.size,config.size,TextureFormat.RGB24,false);buffer=new byte[config.size*config.size*3];
            var output=Path.Combine(config.folder,config.scenario+"_raw.mp4");
            var start=new ProcessStartInfo(config.ffmpeg,"-hide_banner -loglevel warning -y -f rawvideo -pixel_format rgb24 -video_size "+config.size+"x"+config.size+" -framerate "+config.fps+" -i pipe:0 -an -c:v libx264 -preset veryfast -crf 17 -pix_fmt yuv420p -movflags +faststart \""+output+"\"");
            start.UseShellExecute=false;start.CreateNoWindow=true;start.RedirectStandardInput=true;start.RedirectStandardError=true;
            encoder=new Process{StartInfo=start};encoder.ErrorDataReceived+=(sender,args)=>{if(args.Data!=null)File.AppendAllText(Path.Combine(config.folder,config.scenario+"_encoder.log"),args.Data+"\n");};
            encoder.Start();encoder.BeginErrorReadLine();return true;
        }
        catch(Exception exception){Fail(exception.ToString());return false;}
    }
    void Fail(string message){if(report==null)report=new CaptureReport{scenario=config.scenario};report.status="FAILED";report.error=message;WriteReport();Cleanup();EditorApplication.isPlaying=false;}
    void WriteReport(){Directory.CreateDirectory(config.folder);File.WriteAllText(Path.Combine(config.folder,config.scenario+"_capture.json"),JsonUtility.ToJson(report,true));}
    void Cleanup()
    {
        if(encoder!=null){try{encoder.StandardInput.Close();if(!encoder.HasExited)encoder.WaitForExit(10000);}catch{}encoder.Dispose();encoder=null;}
        if(target!=null){target.Release();Destroy(target);target=null;}if(pixels!=null){Destroy(pixels);pixels=null;}
        Time.captureFramerate=0;
    }
    void OnDestroy(){if(!completed)Cleanup();}
}
#endif
