#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ArenaValidation
{
    [MenuItem("Arena Chaos/Validate Imported Content")]
    public static string Validate()
    {
        var content=ArenaContent.Load();
        if(content==null)throw new Exception("Content asset missing.");
        var errors=new List<string>();
        int paths=0,materials=0;
        foreach(var prefab in content.fighters)
        {
            var bones=new HashSet<string>();
            foreach(var bone in prefab.GetComponentsInChildren<Transform>(true))bones.Add(AnimationUtility.CalculateTransformPath(bone,prefab.transform));
            foreach(var clip in content.controller.animationClips)
            foreach(var curve in AnimationUtility.GetCurveBindings(clip))
            {paths++;if(!bones.Contains(curve.path))errors.Add(prefab.name+": "+curve.path);}
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            foreach(var material in renderer.sharedMaterials)
            {materials++;if(material==null||material.mainTexture==null||!material.shader.isSupported)errors.Add(prefab.name+": material/texture/shader invalid");}
        }
        if(content.ambience==null||content.cheer==null||content.punches.Length==0)errors.Add("Audio missing");
        if(content.spectators==null||content.spectators.Length!=6)errors.Add("Spectator variants missing");
        string report="Animation bindings checked: "+paths+"; material slots: "+materials+"; errors: "+errors.Count+"\n"+string.Join("\n",errors);
        Directory.CreateDirectory("Tools/TestResults");File.WriteAllText("Tools/TestResults/ContentValidation.txt",report);
        if(errors.Count>0)throw new Exception(report);
        Debug.Log(report);return report;
    }
    [MenuItem("Arena Chaos/Build Windows")]
    public static void BuildWindows()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before build.");
        Directory.CreateDirectory("Builds/Windows");
        EditorApplication.delayCall+=()=>
        {
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/SampleScene.unity"},locationPathName="Builds/Windows/ARENA_CHAOS.exe",
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.CleanBuildCache });
            Directory.CreateDirectory("Tools/TestResults");
            File.WriteAllText("Tools/TestResults/WindowsBuild.txt",report.summary.result+"; errors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings+"; bytes="+report.summary.totalSize);
            var warnings=new SortedDictionary<string,int>();
            foreach(var step in report.steps)foreach(var message in step.messages)
            {
                if(message.type!=LogType.Warning&&message.type!=LogType.Error)continue;
                string text=message.content;if(warnings.ContainsKey(text))warnings[text]++;else warnings[text]=1;
            }
            var warningReport=new System.Text.StringBuilder();foreach(var row in warnings)warningReport.AppendLine(row.Value+"x "+row.Key);
            File.WriteAllText("Tools/TestResults/BuildWarnings.txt",warningReport.ToString());
            if(report.summary.result==BuildResult.Succeeded)File.Copy("THIRD_PARTY_NOTICES.md","Builds/Windows/THIRD_PARTY_NOTICES.md",true);
        };
    }
}
#endif
