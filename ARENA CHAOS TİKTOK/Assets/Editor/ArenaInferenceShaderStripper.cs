#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;

/// <summary>The arena uses local tactical AI, with no neural model. Keep unused model kernels out of this player.</summary>
public sealed class ArenaInferenceShaderStripper : IPreprocessBuildWithReport,IPostprocessBuildWithReport,IPreprocessShaders,IPreprocessComputeShaders
{
    const string Preference="ArenaChaos.IncludeModelInference";
    const string Menu="Arena Chaos/Build/Include neural model inference";
    static bool strip;
    static int strippedVariants;
    public int callbackOrder=>10000;
    [MenuItem(Menu)] static void Toggle(){EditorPrefs.SetBool(Preference,!EditorPrefs.GetBool(Preference,false));}
    [MenuItem(Menu,true)] static bool ValidateMenu(){UnityEditor.Menu.SetChecked(Menu,EditorPrefs.GetBool(Preference,false));return true;}
    public void OnPreprocessBuild(BuildReport report)
    {
        strip=report.summary.platform==BuildTarget.StandaloneWindows64&&!EditorPrefs.GetBool(Preference,false);strippedVariants=0;
        if(strip)foreach(var path in AssetDatabase.GetAllAssetPaths())
        {
            if(path.StartsWith("Assets/")&&(path.EndsWith(".onnx",System.StringComparison.OrdinalIgnoreCase)||path.EndsWith(".sentis",System.StringComparison.OrdinalIgnoreCase)))
            {strip=false;break;}
        }
    }
    static bool Unused(UnityEngine.Object shader)=>strip&&AssetDatabase.GetAssetPath(shader).StartsWith("Packages/com.unity.ai.inference/");
    public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> data)
    {if(Unused(shader)){strippedVariants+=data.Count;data.Clear();}}
    public void OnProcessComputeShader(ComputeShader shader,string kernelName,IList<ShaderCompilerData> data)
    {if(Unused(shader)){strippedVariants+=data.Count;data.Clear();}}
    public void OnPostprocessBuild(BuildReport report)
    {
        System.IO.Directory.CreateDirectory("Tools/TestResults");
        System.IO.File.WriteAllText("Tools/TestResults/InferenceShaderPolicy.txt","UnusedModelKernelStripping="+strip+"\nVariantsRemoved="+strippedVariants+"\nModel assets or the Include neural model inference menu disable stripping.\n");
        strip=false;
    }
}
#endif
