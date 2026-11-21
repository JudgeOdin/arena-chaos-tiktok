using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ArenaRenderQuality
{
    const string Preference = "ArenaWorldRenderPercent";
    public static readonly int[] Choices = { 100, 85, 70 };
    static UniversalRenderPipelineAsset working;
    static RenderPipelineAsset originalOverride;
    static float originalScale;
    public static int Percent { get; private set; } = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        working = null;
        originalOverride = null;
        Percent = 100;
        Application.quitting -= Restore;
        Application.quitting += Restore;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        int percent=PlayerPrefs.GetInt(Preference,100);
        string[] args=Environment.GetCommandLineArgs();
        int flag=Array.IndexOf(args,"-arenaRenderPercent");
        if (flag>=0 && flag+1<args.Length && int.TryParse(args[flag+1],out int requested))
            percent=requested;
        Apply(percent);
    }

    public static void Select(int percent)
    {
        if (!Apply(percent)) return;
        PlayerPrefs.SetInt(Preference, percent);
        PlayerPrefs.Save();
    }

    static bool Apply(int percent)
    {
        if (Array.IndexOf(Choices,percent)<0) return false;
        if (percent==Percent) return true;
        if (percent==100)
        {
            Restore();
            return true;
        }
        if (working==null)
        {
            var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (source==null) return false;
            originalOverride=QualitySettings.renderPipeline;
            working=UnityEngine.Object.Instantiate(source);
            working.name=source.name+" (arena render scale)";
            originalScale=source.renderScale;
            working.renderScale=originalScale*percent/100f;
            QualitySettings.renderPipeline=working;
        }
        else working.renderScale=originalScale*percent/100f;
        Percent=percent;
        return true;
    }

    static void Restore()
    {
        if (working!=null)
        {
            QualitySettings.renderPipeline=originalOverride;
            UnityEngine.Object.Destroy(working);
            working=null;
        }
        Percent=100;
    }
}

