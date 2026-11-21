#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad] public static class ArenaPromoJobs
{
    const string Request="Tools/Video/CaptureRequest.json",Key="ArenaPromo.Config";
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static ArenaPromoJobs(){EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=StateChanged;}
    static void Update()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
        string json=File.ReadAllText(Request);File.Delete(Request);var config=JsonUtility.FromJson<ArenaPromoConfig>(json);
        if(config.scenario!="meteor"&&config.scenario!="fire"&&config.scenario!="classes"&&config.scenario!="infection"&&config.scenario!="king")throw new Exception("Unknown capture scenario");
        Directory.CreateDirectory(config.folder);SessionState.SetString(Key,json);SessionState.SetBool("ArenaPromo.Pending",true);
        SessionState.SetBool("ArenaPromo.PreviousModePresent",PlayerPrefs.HasKey("ArenaActiveMode"));
        SessionState.SetInt("ArenaPromo.PreviousMode",PlayerPrefs.GetInt("ArenaActiveMode",0));
        int mode=config.scenario=="infection"?3:config.scenario=="king"?2:0;
        PlayerPrefs.SetInt("ArenaActiveMode",mode);
        SetSize(config.Width,config.Height);Time.timeScale=1;EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("ArenaPromo.Pending",false))
        {
            var config=JsonUtility.FromJson<ArenaPromoConfig>(SessionState.GetString(Key,""));
            var view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
            new GameObject("Editor-only promotional capture").AddComponent<ArenaPromoRecorder>().Begin(config);
        }
        if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("ArenaPromo.Pending",false))
        {SessionState.SetBool("ArenaPromo.Pending",false);Time.captureFramerate=0;Time.timeScale=1;
            if(SessionState.GetBool("ArenaPromo.PreviousModePresent",false))
                PlayerPrefs.SetInt("ArenaActiveMode",SessionState.GetInt("ArenaPromo.PreviousMode",0));
            else PlayerPrefs.DeleteKey("ArenaActiveMode");
            RestoreSize();}
    }
    static object Group(out Type groupType,out object window)
    {
        var assembly=typeof(Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=sizes.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
        var viewType=assembly.GetType("UnityEditor.GameView");window=EditorWindow.GetWindow(viewType);
        var kind=viewType.GetProperty("currentSizeGroupType",Flags).GetValue(window);
        var group=sizes.GetMethod("GetGroup",Flags).Invoke(singleton,new[]{kind});groupType=group.GetType();return group;
    }
    static void SetSize(int width,int height)
    {
        Type groupType;object window;var group=Group(out groupType,out window);var viewType=window.GetType();
        SessionState.SetInt("ArenaPromo.PreviousSize",(int)viewType.GetProperty("selectedSizeIndex",Flags).GetValue(window));
        int count=(int)groupType.GetMethod("GetTotalCount",Flags).Invoke(group,null);
        var assembly=typeof(Editor).Assembly;var sizeType=assembly.GetType("UnityEditor.GameViewSize");var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
        var value=Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.Parse(enumType,"FixedResolution"),width,height,"ARENA PROMO TEMP"},null);
        groupType.GetMethod("AddCustomSize",Flags).Invoke(group,new[]{value});SessionState.SetInt("ArenaPromo.CustomIndex",count);
        viewType.GetProperty("selectedSizeIndex",Flags).SetValue(window,count);
        ((EditorWindow)window).Focus();((EditorWindow)window).Repaint();
    }
    static void RestoreSize()
    {
        Type groupType;object window;var group=Group(out groupType,out window);
        window.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(window,SessionState.GetInt("ArenaPromo.PreviousSize",0));
        groupType.GetMethod("RemoveCustomSize",Flags).Invoke(group,new object[]{SessionState.GetInt("ArenaPromo.CustomIndex",0)});
        ((EditorWindow)window).Repaint();
    }
}
#endif
