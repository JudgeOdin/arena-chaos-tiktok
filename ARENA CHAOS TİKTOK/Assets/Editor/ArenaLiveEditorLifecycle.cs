#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Cancel async receivers before Editor reload replaces nonserialized task/token fields.</summary>
[InitializeOnLoad]
public static class ArenaLiveEditorLifecycle
{
    static ArenaLiveEditorLifecycle()
    {
        AssemblyReloadEvents.beforeAssemblyReload+=Disconnect;
        EditorApplication.quitting+=Disconnect;
        EditorApplication.playModeStateChanged+=state=>
        {if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.ExitingEditMode)Disconnect();};
    }
    static void Disconnect()
    {
        foreach(var receiver in Resources.FindObjectsOfTypeAll<ArenaLiveBridge>())
            if(receiver!=null)receiver.Disconnect();
    }
}
#endif
