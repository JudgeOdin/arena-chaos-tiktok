#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ArenaContentBuilder
{
    const string Root = "Assets/ArenaChaos";
    const string Characters = "Assets/KayKit_Adventurers_2.0_FREE/Characters/fbx/";
    const string Animations = "Assets/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";
    const string Dungeon = "Assets/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";

    [MenuItem("Arena Chaos/Build Imported Content")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Animations");
        Directory.CreateDirectory(Root + "/Meshes");
        Directory.CreateDirectory("Assets/Resources/ArenaChaos");
        AssetDatabase.Refresh();
        var content = AssetDatabase.LoadAssetAtPath<ArenaContent>("Assets/Resources/ArenaChaos/Content.asset");
        if (content == null) { content = ScriptableObject.CreateInstance<ArenaContent>(); AssetDatabase.CreateAsset(content, "Assets/Resources/ArenaChaos/Content.asset"); }
        string controllerPath = Root + "/Animations/KayKitArena.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        if (controller.layers.Length > 1) controller.layers = new[] { controller.layers[0] };
        var machine = controller.layers[0].stateMachine;
        foreach (var state in machine.states) machine.RemoveState(state.state);
        AddState(machine, "Guard", "CombatMelee", "Melee_Unarmed_Idle", true);
        AddState(machine, "Walk", "MovementBasic", "Walking_A", true);
        AddState(machine, "Punch", "CombatMelee", "Melee_Unarmed_Attack_Punch_A", false);
        AddState(machine, "Kick", "CombatMelee", "Melee_Unarmed_Attack_Kick", false);
        AddState(machine, "Block", "CombatMelee", "Melee_Blocking", true);
        AddState(machine, "Hit", "General", "Hit_A", false);
        AddState(machine, "Death", "General", "Death_A", false);
        AddState(machine, "Cheer", "Simulation", "Cheering", true);
        AddState(machine, "Sit", "Simulation", "Sit_Chair_Idle", true);
        content.controller = controller;
        var prefabs = new List<GameObject>();
        foreach (string name in new[] { "Barbarian", "Knight", "Mage", "Ranger", "Rogue", "Rogue_Hooded" })
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Characters + name + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing character: " + name);
            GameObject obj = UnityEngine.Object.Instantiate(source);
            obj.name = name;
            var textureName = name.StartsWith("Rogue") ? "rogue" : name.ToLowerInvariant();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/KayKit_Adventurers_2.0_FREE/Textures/" + textureName + "_texture.png");
            var material = MaterialAsset(name, Color.white, texture);
            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }
            var animator = obj.GetComponent<Animator>();
            if (animator == null) animator = obj.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            prefabs.Add(PrefabUtility.SaveAsPrefabAsset(obj, Root + "/Prefabs/" + name + ".prefab"));
            UnityEngine.Object.DestroyImmediate(obj);
        }
        content.fighters = prefabs.ToArray();
        ConfigureGuardLayer(controller, prefabs[0]);
        var spectators = new List<GameObject>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            var actor = UnityEngine.Object.Instantiate(prefabs[i]);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Animations/Cheer.anim");
            clip.SampleAnimation(actor, .35f + i * .11f);
            var combined = new List<CombineInstance>();
            var baked = new List<Mesh>();
            Material material = null;
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skin.gameObject.activeInHierarchy) continue;
                var mesh = new Mesh(); skin.BakeMesh(mesh); baked.Add(mesh);
                for (int s = 0; s < mesh.subMeshCount; s++)
                    combined.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = actor.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix });
                material = skin.sharedMaterial;
            }
            var joined = new Mesh { name = prefabs[i].name + " Crowd" };
            joined.CombineMeshes(combined.ToArray(), true, true);
            string meshPath = Root + "/Meshes/Crowd_" + i + ".asset";
            var previous = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (previous == null) AssetDatabase.CreateAsset(joined, meshPath);
            else { EditorUtility.CopySerialized(joined, previous); UnityEngine.Object.DestroyImmediate(joined); joined = previous; EditorUtility.SetDirty(previous); }
            var crowd = new GameObject("Spectator " + prefabs[i].name, typeof(MeshFilter), typeof(MeshRenderer));
            crowd.GetComponent<MeshFilter>().sharedMesh = joined;
            crowd.GetComponent<MeshRenderer>().sharedMaterial = material;
            crowd.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spectators.Add(PrefabUtility.SaveAsPrefabAsset(crowd, Root + "/Prefabs/Crowd_" + i + ".prefab"));
            UnityEngine.Object.DestroyImmediate(crowd); UnityEngine.Object.DestroyImmediate(actor);
            foreach (var mesh in baked) UnityEngine.Object.DestroyImmediate(mesh);
        }
        content.spectators = spectators.ToArray();
        var environment = new List<ArenaContent.Model>();
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Dungeon + "dungeon_texture.png");
        var dungeonMaterial = MaterialAsset("DungeonAtlas", Color.white, atlas);
        foreach (string name in new[] { "wall", "wall_half", "wall_pillar", "wall_doorway", "wall_gated", "column", "barrier", "floor_tile_large", "floor_dirt_large", "floor_wood_large", "stairs_wide", "stairs_wood", "banner_red", "banner_blue", "torch_mounted" })
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Dungeon + name + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing environment: " + name);
            GameObject obj = UnityEngine.Object.Instantiate(source);
            obj.name = name;
            foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = dungeonMaterial;
            environment.Add(new ArenaContent.Model { key = name, prefab = PrefabUtility.SaveAsPrefabAsset(obj, Root + "/Prefabs/Env_" + name + ".prefab") });
            UnityEngine.Object.DestroyImmediate(obj);
        }
        content.environment = environment.ToArray();
        content.stone = MaterialAsset("DarkStone", new Color(.15f, .19f, .24f));
        content.sand = MaterialAsset("WarmSand", new Color(.55f, .4f, .23f));
        content.timber = MaterialAsset("Timber", new Color(.17f, .09f, .055f));
        content.gold = MaterialAsset("Gold", new Color(.95f, .65f, .22f));
        content.red = MaterialAsset("Crimson", new Color(.55f, .055f, .09f));
        content.blue = MaterialAsset("RoyalBlue", new Color(.045f, .22f, .5f));
        content.ambience = FindAudio("10 - Ambience.wav");
        content.battleMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/the_final_battle.ogg");
        content.cheer = FindAudio("04 - Strong cheering - II - Short.wav");
        Directory.CreateDirectory(Root + "/Audio");
        content.ambience = CopyAudio(content.ambience, "Ambience");
        content.cheer = CopyAudio(content.cheer, "Cheer");
        content.punches = AudioGroup("impactPunch_medium_");
        content.footsteps = AudioGroup("footstep_concrete_");
        content.doors = AudioGroup("impactWood_heavy_");
        content.smoke = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PNG (Transparent)/smoke_01.png");
        content.spark = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PNG (Transparent)/spark_01.png");
        content.flame = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PNG (Transparent)/flame_01.png");
        content.sparkMaterial = ParticleMaterial("ContactSpark",content.spark);
        content.smokeMaterial = ParticleMaterial("BlastSmoke",content.smoke);
        content.flameMaterial = ParticleMaterial("BlastFlame",content.flame);
        // Long recordings stream compressed rather than decompressing the complete archive.
        foreach (var clip in new[] { content.ambience, content.cheer, content.battleMusic }) if (clip != null)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .65f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
        EditorUtility.SetDirty(content);
        ConfigureQuality();
        AssetDatabase.SaveAssets();
        Debug.Log("Arena content prepared: 6 character prefabs, 15 environment prefabs, 9 animation states, audio and VFX textures.");
    }

    static void AddState(AnimatorStateMachine machine, string stateName, string set, string clipName, bool loop)
    {
        AnimationClip source = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Animations + "Rig_Medium_" + set + ".fbx"))
            if (asset is AnimationClip clip && clip.name == clipName) { source = clip; break; }
        if (source == null) throw new InvalidOperationException("Missing animation: " + clipName);
        string path = Root + "/Animations/" + stateName + ".anim";
        var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (copy == null) { copy = new AnimationClip(); AssetDatabase.CreateAsset(copy, path); }
        EditorUtility.CopySerialized(source, copy);
        copy.name = stateName;
        var settings = AnimationUtility.GetAnimationClipSettings(copy);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(copy, settings);
        // Preserve the authored bone rotations and hip bounce, strip global translations.
        foreach (var binding in AnimationUtility.GetCurveBindings(copy))
            if ((binding.path == "Rig_Medium" || binding.path == "Rig_Medium/root") && binding.propertyName.StartsWith("m_LocalPosition"))
                AnimationUtility.SetEditorCurve(copy, binding, null);
        EditorUtility.SetDirty(copy);
        var state = machine.AddState(stateName);
        state.motion = copy;
        state.writeDefaultValues = true;
        if (stateName == "Guard") machine.defaultState = state;
    }

    static Material MaterialAsset(string name, Color color, Texture texture = null)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color;
        mat.mainTexture = texture;
        mat.SetFloat("_Smoothness", .2f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }
    static AudioClip FindAudio(string suffix)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Gregor Quendel - Free Crowd Cheering Sounds" }))
        { string path = AssetDatabase.GUIDToAssetPath(guid); if (path.EndsWith(suffix)) return AssetDatabase.LoadAssetAtPath<AudioClip>(path); }
        return null;
    }
    static Material ParticleMaterial(string name, Texture texture)
    {
        string path=Root+"/Materials/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,path);}
        material.mainTexture=texture;material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if(name=="BlastFlame"){material.SetFloat("_Blend",2);material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.One);material.SetColor("_BaseColor",new Color(4,4,4,1));}
        material.SetInt("_ZWrite",0);material.renderQueue=3000;EditorUtility.SetDirty(material);return material;
    }
    static AudioClip[] AudioGroup(string prefix)
    {
        var result = new List<AudioClip>();
        for (int i = 0; i < 5; i++) { var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + prefix + i.ToString("000") + ".ogg"); if (clip != null) result.Add(clip); }
        return result.ToArray();
    }
    static AudioClip CopyAudio(AudioClip source, string name)
    {
        if (source == null) return null;
        string path = Root + "/Audio/" + name + ".wav";
        if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static void ConfigureGuardLayer(AnimatorController controller, GameObject source)
    {
        string path = Root + "/Animations/UpperBody.mask";
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
        if (mask == null) { mask = new AvatarMask(); AssetDatabase.CreateAsset(mask,path); }
        var bones = source.GetComponentsInChildren<Transform>(true);
        mask.transformCount = bones.Length;
        for(int i=0;i<bones.Length;i++)
        {
            string bonePath = AnimationUtility.CalculateTransformPath(bones[i],source.transform);
            mask.SetTransformPath(i,bonePath);
            mask.SetTransformActive(i,bonePath.Contains("/spine"));
        }
        EditorUtility.SetDirty(mask);
        var machine = new AnimatorStateMachine { name = "Guard Upper Body" };
        AssetDatabase.AddObjectToAsset(machine,controller);
        var guard = machine.AddState("Guard");
        guard.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/Guard.anim");
        machine.defaultState=guard;
        controller.AddLayer(new AnimatorControllerLayer { name="Guard Upper Body",stateMachine=machine,avatarMask=mask,defaultWeight=1f,blendingMode=AnimatorLayerBlendingMode.Override });
        EditorUtility.SetDirty(controller);
    }
    static void ConfigureQuality()
    {
        var original = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if(original != null)
        {
            string path=Root+"/BroadcastQuality.asset";
            var profile=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(path);
            if(profile==null){profile=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(profile,path);}
            profile.renderScale=1f;profile.msaaSampleCount=4;
            profile.shadowDistance=35f;profile.mainLightShadowmapResolution=2048;profile.shadowCascadeCount=2;
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=profile;
            QualitySettings.renderPipeline=profile;
            EditorUtility.SetDirty(profile);
        }
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if(!scenes.Exists(s=>s.path=="Assets/Scenes/SampleScene.unity"))scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",true));
        EditorBuildSettings.scenes=scenes.ToArray();
    }
}
#endif
