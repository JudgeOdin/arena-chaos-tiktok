using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class ArenaAnimatedCharacter : MonoBehaviour
{
    const string InfectionPath = "ArenaChaos/Infection/Infected";
    const string RoyalPath = "ArenaChaos/Royal/Monarch";
    public const float RoyalHeight = 2.82f;
    static readonly Dictionary<string, Model> Models = new Dictionary<string, Model>();
    static Material infectedSkin;

    sealed class Model
    {
        public GameObject Prefab;
        public readonly Dictionary<string, AnimationClip> Motions = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
    }

    ArenaFighter fighter;
    Model model;
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    AnimationClipPlayable current, previous;
    AnimationClip selected;
    string selectedMotion;
    float blend = 1f, risingUntil, reactUntil;
    int attackCycle;
    float lastDamage;
    public bool IsRoyal { get; private set; }
    public bool HasArenaMaterial { get; private set; }

    public static bool TryCreate(ArenaFighter owner) => TryCreate(owner, false, Color.white);
    public static bool TryCreateRoyal(ArenaFighter owner, Color team) => TryCreate(owner, true, team);

    static bool TryCreate(ArenaFighter owner, bool royal, Color team)
    {
        string path = royal ? RoyalPath : InfectionPath;
        if (!Models.TryGetValue(path, out var source) || source.Prefab == null)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return false;
            source = new Model { Prefab = prefab };
            foreach (var clip in Resources.LoadAll<AnimationClip>(path))
            {
                if (clip.legacy) continue;
                int separator = clip.name.LastIndexOf('|');
                string name = clip.name.Substring(separator + 1);
                if (!source.Motions.ContainsKey(name) || separator < 0) source.Motions[name] = clip;
            }
            Models[path] = source;
        }
        if (!source.Motions.ContainsKey("Idle") || !source.Motions.ContainsKey("Walk") ||
            !source.Motions.ContainsKey(royal ? "Punch_Right" : "Punch")) return false;

        var instance = Instantiate(source.Prefab, owner.transform, false);
        var animator = instance.GetComponentInChildren<Animator>(true);
        if (animator == null) { Destroy(instance); return false; }
        instance.name = royal ? "Crowned monarch" : "Infected • Quaternius";

        Fit(instance.transform, royal ? RoyalHeight : 1.93f);
        bool materialReady = royal ? ArenaRoyalWardrobe.Dress(owner, instance, team) : ApplyInfectedSkin(instance);
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var driver = instance.AddComponent<ArenaAnimatedCharacter>();
        driver.fighter = owner;
        driver.model = source;
        driver.IsRoyal = royal;
        driver.HasArenaMaterial = materialReady;
        driver.graph = PlayableGraph.Create(royal ? "Monarch motion" : "Infected motion");
        driver.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        driver.mixer = AnimationMixerPlayable.Create(driver.graph, 2);
        AnimationPlayableOutput.Create(driver.graph, "Character skeleton", animator).SetSourcePlayable(driver.mixer);
        driver.Change("Idle");
        driver.graph.Play();
        return true;
    }

    static bool ApplyInfectedSkin(GameObject instance)
    {
        if (infectedSkin == null)
        {
            var atlas = Resources.Load<Texture2D>("ArenaChaos/Infection/ZombieAtlas");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (atlas == null || shader == null) return false;
            infectedSkin = new Material(shader) { name = "Infected atlas • arena lighting", mainTexture = atlas };
            infectedSkin.SetFloat("_Metallic", 0);
            infectedSkin.SetFloat("_Smoothness", .16f);
        }
        foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            renderer.sharedMaterial = infectedSkin;
        return infectedSkin.shader.isSupported;
    }

    static void Fit(Transform model, float height)
    {
        var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (bounds.size.y < .01f) return;
        model.localScale *= height * model.parent.lossyScale.y / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Vector3 feet = model.parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        model.localPosition += new Vector3(-feet.x, -.91f-feet.y, -feet.z);
    }

    public void Rise()
    {
        if (IsRoyal) return;
        risingUntil = Time.time + .68f;
        Change("Crawl");
    }

    void LateUpdate()
    {
        if (!graph.IsValid() || fighter == null) return;
        if (!fighter.IsAlive) Change("Death");
        else if (Time.time < risingUntil) Change("Crawl");
        else if (attackCycle != fighter.AttackCycle)
        {
            attackCycle = fighter.AttackCycle;
            Change(IsRoyal ? "Punch_Right" : "Punch", true);
        }
        else if (!fighter.IsPunching)
        {
            if (lastDamage != fighter.LastDamageTime)
            {
                lastDamage = fighter.LastDamageTime;
                reactUntil = Time.time + .20f;
            }
            string hit = IsRoyal ? "HitRecieve" : "HitReact";
            Change(Time.time < reactUntil ? hit : fighter.IsMoving ? "Walk" : "Idle");
        }

        if (selected != null && selected.length > 0 && current.IsValid() && current.GetTime() >= selected.length)
        {
            if (selectedMotion == "Idle" || selectedMotion == "Walk") current.SetTime(current.GetTime() % selected.length);
            else current.SetTime(Math.Max(0, selected.length - .001f));
        }
        blend = Mathf.MoveTowards(blend, 1f, Time.deltaTime * 9f);
        mixer.SetInputWeight(0, blend);
        mixer.SetInputWeight(1, 1f - blend);
        if (blend >= 1f && previous.IsValid())
        {
            graph.Disconnect(mixer, 1);
            previous.Destroy();
        }
    }

    void Change(string name, bool restart = false)
    {
        if (!model.Motions.TryGetValue(name, out var clip) || clip == selected && !restart) return;
        if (previous.IsValid())
        {
            graph.Disconnect(mixer, 1);
            previous.Destroy();
        }
        if (current.IsValid())
        {
            graph.Disconnect(mixer, 0);
            previous = current;
            graph.Connect(previous, 0, mixer, 1);
        }
        selected = clip;
        selectedMotion = name;
        current = AnimationClipPlayable.Create(graph, clip);
        graph.Connect(current, 0, mixer, 0);
        blend = previous.IsValid() ? 0 : 1;
        mixer.SetInputWeight(0, blend);
        mixer.SetInputWeight(1, 1f - blend);
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}

