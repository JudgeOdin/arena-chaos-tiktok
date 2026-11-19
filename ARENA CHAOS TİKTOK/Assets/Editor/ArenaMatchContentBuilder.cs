#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class ArenaMatchContentBuilder
{
    [MenuItem("Arena Chaos/Prepare Match And Gift Classes")]
    public static void Prepare()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play first");
        const string folder="Assets/Resources/ArenaChaos/";
        if(AssetDatabase.LoadAssetAtPath<ArenaMatchSettings>(folder+"MatchSettings.asset")==null)AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ArenaMatchSettings>(),folder+"MatchSettings.asset");
        if(AssetDatabase.LoadAssetAtPath<ArenaGiftRoster>(folder+"GiftRoster.asset")==null)AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ArenaGiftRoster>(),folder+"GiftRoster.asset");
        if(AssetDatabase.LoadAssetAtPath<ArenaLiveSettings>(folder+"LiveSettings.asset")==null)AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ArenaLiveSettings>(),folder+"LiveSettings.asset");
        var content=ArenaContent.Load();
        content.bowPrefab=Equipment("bow_withString",3);content.arrowPrefab=Equipment("arrow_bow",3);content.shieldPrefab=Equipment("shield_badge",1);
        var controller=(AnimatorController)content.controller;var machine=controller.layers[0].stateMachine;
        AnimationClip source=null;foreach(var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatRanged.fbx"))if(obj is AnimationClip clip&&clip.name=="Ranged_Bow_Release")source=clip;
        if(source==null)throw new System.Exception("Bow release clip missing");
        string path="Assets/ArenaChaos/Animations/Bow.anim";
        var copy=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(copy==null){copy=new AnimationClip();AssetDatabase.CreateAsset(copy,path);}
        EditorUtility.CopySerialized(source,copy);copy.name="Bow";
        foreach(var binding in AnimationUtility.GetCurveBindings(copy))if((binding.path=="Rig_Medium"||binding.path=="Rig_Medium/root")&&binding.propertyName.StartsWith("m_LocalPosition"))AnimationUtility.SetEditorCurve(copy,binding,null);
        AnimatorState state=null;foreach(var candidate in machine.states)if(candidate.state.name=="Bow")state=candidate.state;
        if(state==null)state=machine.AddState("Bow");state.motion=copy;state.writeDefaultValues=true;
        AnimationClip aiming=null;foreach(var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_CombatRanged.fbx"))if(obj is AnimationClip clip&&clip.name=="Ranged_Bow_Aiming_Idle")aiming=clip;
        if(aiming==null)throw new System.Exception("Bow aiming clip missing");
        string aimPath="Assets/ArenaChaos/Animations/BowGuard.anim";var aimCopy=AssetDatabase.LoadAssetAtPath<AnimationClip>(aimPath);if(aimCopy==null){aimCopy=new AnimationClip();AssetDatabase.CreateAsset(aimCopy,aimPath);}
        EditorUtility.CopySerialized(aiming,aimCopy);aimCopy.name="BowGuard";
        foreach(var binding in AnimationUtility.GetCurveBindings(aimCopy))if((binding.path=="Rig_Medium"||binding.path=="Rig_Medium/root")&&binding.propertyName.StartsWith("m_LocalPosition"))AnimationUtility.SetEditorCurve(aimCopy,binding,null);
        var aimSettings=AnimationUtility.GetAnimationClipSettings(aimCopy);aimSettings.loopTime=true;AnimationUtility.SetAnimationClipSettings(aimCopy,aimSettings);
        AnimatorState aimState=null;foreach(var candidate in machine.states)if(candidate.state.name=="BowGuard")aimState=candidate.state;if(aimState==null)aimState=machine.AddState("BowGuard");aimState.motion=aimCopy;
        EditorUtility.SetDirty(aimCopy);
        EditorUtility.SetDirty(copy);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
    }
    static GameObject Equipment(string name,int character)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/"+name+".fbx");
        if(source==null)throw new System.Exception("Equipment missing: "+name);
        var obj=Object.Instantiate(source);var content=ArenaContent.Load();
        var material=content.fighters[character].GetComponentInChildren<Renderer>().sharedMaterial;
        foreach(var renderer in obj.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
        var prefab=PrefabUtility.SaveAsPrefabAsset(obj,"Assets/ArenaChaos/Prefabs/Equipment_"+name+".prefab");Object.DestroyImmediate(obj);return prefab;
    }
}
#endif
