using System.Collections.Generic;
using UnityEngine;

/// <summary>Distinct, reusable visual signatures, in world units even on imported FBX bones.</summary>
public sealed class ArenaPowerVisual : MonoBehaviour
{
    ArenaPowerUps owner;
    ArenaPower power;
    readonly List<LineRenderer> rings=new List<LineRenderer>();
    readonly List<Vector3> ringAxes=new List<Vector3>();
    readonly List<float> ringRadii=new List<float>();
    readonly List<Renderer> iceRenderers=new List<Renderer>();
    readonly List<MaterialPropertyBlock> oldBlocks=new List<MaterialPropertyBlock>();
    ParticleSystem[] particles;
    Light glow;
    bool emitting;
    float fade;
    static int glowCount;
    MaterialPropertyBlock iceTint;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetGlows(){glowCount=0;}
    public static ArenaPowerVisual Create(ArenaPowerUps owner,ArenaPower power)
    {
        var obj=new GameObject(ArenaPowerUps.Title(power)+" Signature");obj.transform.SetParent(owner.transform,false);
        var visual=obj.AddComponent<ArenaPowerVisual>();visual.owner=owner;visual.power=power;visual.Build();return visual;
    }
    ParticleSystem Plume(string name,Vector3 offset,Material material,Color color,float size,float rate,float speed,float life,float radius)
    {
        var obj=new GameObject(name);obj.transform.SetParent(transform,false);obj.transform.localPosition=offset;
        var p=obj.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=p.main;main.loop=true;main.playOnAwake=false;main.scalingMode=ParticleSystemScalingMode.Local;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.startLifetime=life;main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.5f,speed);main.startSize=new ParticleSystem.MinMaxCurve(size*.55f,size);main.startColor=color;main.maxParticles=Mathf.CeilToInt(rate*life)+12;
        var emission=p.emission;emission.rateOverTime=rate;
        var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=radius;
        var velocity=p.velocityOverLifetime;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(0);velocity.y=new ParticleSystem.MinMaxCurve(speed*.7f);velocity.z=new ParticleSystem.MinMaxCurve(0);velocity.enabled=true;
        var fade=p.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var scale=p.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
        var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.sortingFudge=1;
        return p;
    }
    void Ring(float radius,Vector3 axis,Color color,float width=.035f)
    {
        var obj=new GameObject("Energy orbit");obj.transform.SetParent(transform,false);
        var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=65;line.widthMultiplier=width;line.sharedMaterial=ArenaContent.Load().sparkMaterial;
        line.startColor=line.endColor=color;line.numCornerVertices=2;
        rings.Add(line);ringAxes.Add(axis);ringRadii.Add(radius);
    }
    void Build()
    {
        var c=ArenaContent.Load();if(c==null)return;
        switch(power)
        {
            case ArenaPower.Fire:
                foreach(Transform bone in owner.GetComponentsInChildren<Transform>(true))
                if(bone.name=="hand.l"||bone.name=="hand.r")
                {
                    var fire=Plume("Flaming "+bone.name,Vector3.zero,c.flameMaterial,new Color(1,.35f,.015f),.65f,55,1.35f,.55f,.10f);
                    fire.gameObject.AddComponent<ArenaEffectAnchor>().target=bone;
                    var core=Plume("White hot core "+bone.name,Vector3.zero,c.flameMaterial,new Color(1,.85f,.22f),.28f,25,.5f,.24f,.07f);
                    core.gameObject.AddComponent<ArenaEffectAnchor>().target=bone;
                    var embers=Plume("Flying hand embers",Vector3.zero,c.sparkMaterial,new Color(1,.55f,.08f),.10f,18,2,.4f,.08f);
                    embers.gameObject.AddComponent<ArenaEffectAnchor>().target=bone;
                }
                AddGlow(new Color(1,.3f,.025f),2.2f,3);break;
            case ArenaPower.Lightning:
                Ring(.85f,Vector3.up,new Color(.3f,.65f,1),.07f);Ring(.65f,Vector3.forward,Color.cyan,.04f);
                Plume("Electric afterimage",Vector3.up*.3f,c.sparkMaterial,new Color(.4f,.75f,1),.22f,50,.7f,.35f,.5f);break;
            case ArenaPower.Giant:
                Ring(.95f,Vector3.up,new Color(1,.6f,.15f),.075f);
                Plume("Heavy foot dust",Vector3.down*.85f,c.smokeMaterial,new Color(.65f,.48f,.27f,.6f),.55f,24,.7f,.65f,.55f);break;
            case ArenaPower.Ice:
                Ring(.8f,Vector3.up,Color.cyan,.045f);Ring(.85f,Vector3.right,new Color(.65f,.9f,1),.03f);
                Plume("Ice crystal spray",Vector3.up*.25f,c.sparkMaterial,new Color(.55f,.9f,1),.28f,32,.6f,.8f,.65f);
                var driver=owner.GetComponentInChildren<KayKitFighterDriver>();
                if(driver!=null)foreach(var r in driver.GetComponentsInChildren<SkinnedMeshRenderer>()){iceRenderers.Add(r);var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);oldBlocks.Add(block);}break;
            case ArenaPower.Explosive:
                Ring(.7f,Vector3.forward,new Color(1,.35f,.02f),.08f);
                Plume("Volatile sparks",Vector3.up*.4f,c.sparkMaterial,new Color(1,.25f,.03f),.22f,42,2,.4f,.6f);break;
            case ArenaPower.Vampire:
                Ring(.9f,Vector3.up,new Color(1,.025f,.15f),.055f);Ring(.65f,Vector3.forward,new Color(.7f,.04f,.25f),.05f);
                Plume("Blood energy",Vector3.zero,c.smokeMaterial,new Color(.8f,.015f,.06f,.65f),.45f,30,.7f,.65f,.55f);break;
            case ArenaPower.Shadow:
                Plume("Shadow afterimage",Vector3.zero,c.smokeMaterial,new Color(.22f,.06f,.35f,.7f),.75f,32,.15f,.75f,.5f);break;
            case ArenaPower.Meteor: break; // The falling projectile and arena detonation own this effect.
            case ArenaPower.HolyShield:
                Ring(1.05f,Vector3.up,new Color(1,.82f,.25f),.06f);Ring(1.05f,Vector3.right,new Color(1,.94f,.65f),.04f);Ring(1.05f,Vector3.forward,new Color(1,.8f,.2f),.04f);
                Plume("Shield motes",Vector3.zero,c.sparkMaterial,new Color(1,.9f,.45f),.16f,22,.2f,1,.9f);AddGlow(new Color(1,.8f,.3f),1,3);break;
            case ArenaPower.Poison:
                Plume("Toxic fumes",Vector3.down*.25f,c.smokeMaterial,new Color(.25f,.85f,.025f,.65f),.65f,32,.6f,1,.5f);
                Plume("Poison bubbles",Vector3.zero,c.sparkMaterial,new Color(.65f,1,.05f),.22f,16,.8f,.8f,.55f);break;
            case ArenaPower.Firestorm:
                for(int i=0;i<3;i++){float angle=i*Mathf.PI*2/3;var p=Plume("Orbiting fire",new Vector3(Mathf.Cos(angle)*1.3f,-.4f,Mathf.Sin(angle)*1.3f),c.flameMaterial,new Color(1,.3f,.015f),.85f,32,1,.5f,.2f);p.gameObject.AddComponent<ArenaEffectOrbit>().phase=angle;}
                Ring(1.5f,Vector3.up,new Color(1,.45f,.025f),.09f);break;
            case ArenaPower.FrozenTime:
                Ring(1.5f,Vector3.up,new Color(.4f,.8f,1),.065f);Ring(2.5f,Vector3.up,new Color(.65f,.9f,1),.035f);
                Plume("Frozen time fragments",Vector3.zero,c.sparkMaterial,new Color(.55f,.85f,1),.20f,32,.06f,1.4f,1.3f);break;
        }
        particles=GetComponentsInChildren<ParticleSystem>();
    }
    void AddGlow(Color color,float intensity,float range){if(glowCount>=6)return;glowCount++;glow=gameObject.AddComponent<Light>();glow.type=LightType.Point;glow.color=color;glow.intensity=intensity;glow.range=range;glow.shadows=LightShadows.None;}
    void OnDestroy(){if(glow!=null)glowCount=Mathf.Max(0,glowCount-1);}
    void LateUpdate()
    {
        bool active=owner!=null&&owner.GetComponent<ArenaFighter>().IsAlive&&owner.Has(power);
        fade=Mathf.MoveTowards(fade,active?1:0,Time.deltaTime*5);
        if(emitting!=active){emitting=active;if(particles!=null)foreach(var particle in particles){if(active)particle.Play();else particle.Stop(false,ParticleSystemStopBehavior.StopEmitting);}}
        if(glow!=null)glow.enabled=active;
        for(int r=0;r<rings.Count;r++)
        {
            var line=rings[r];line.enabled=fade>0;
            float radius=ringRadii[r];if(power==ArenaPower.FrozenTime)radius*=.5f+Mathf.Repeat(Time.time*.5f+r*.3f,1);
            var rotation=Quaternion.FromToRotation(Vector3.up,ringAxes[r])*Quaternion.Euler(0,Time.time*(power==ArenaPower.Lightning?180:45),0);
            for(int i=0;i<65;i++)
            {
                float angle=i*Mathf.PI*2/64;float jitter=power==ArenaPower.Lightning?Mathf.Sin(i*8+Mathf.Floor(Time.time*16))*.12f:0;
                var point=rotation*new Vector3(Mathf.Cos(angle)*(radius+jitter),0,Mathf.Sin(angle)*(radius+jitter));
                line.SetPosition(i,point+Vector3.up*(power==ArenaPower.Giant||power==ArenaPower.Firestorm||power==ArenaPower.FrozenTime?-.8f:.25f));
            }
            line.widthMultiplier=(power==ArenaPower.Lightning?.06f:.04f)*fade;
        }
        for(int i=0;i<iceRenderers.Count;i++)if(iceRenderers[i]!=null)
        {
            if(!active)iceRenderers[i].SetPropertyBlock(oldBlocks[i]);
            else {if(iceTint==null)iceTint=new MaterialPropertyBlock();iceTint.SetColor("_BaseColor",new Color(.45f,.8f,1,owner.Has(ArenaPower.Shadow)?.35f:1));iceRenderers[i].SetPropertyBlock(iceTint);}
        }
    }
    void OnDisable(){for(int i=0;i<iceRenderers.Count;i++)if(iceRenderers[i]!=null)iceRenderers[i].SetPropertyBlock(oldBlocks[i]);}
}
public sealed class ArenaEffectAnchor : MonoBehaviour
{
    public Transform target;
    void LateUpdate(){if(target!=null)transform.SetPositionAndRotation(target.position,Quaternion.identity);}
}
public sealed class ArenaEffectOrbit : MonoBehaviour
{
    public float phase;
    void LateUpdate(){float angle=Time.time*3+phase;transform.localPosition=new Vector3(Mathf.Cos(angle)*1.3f,-.4f,Mathf.Sin(angle)*1.3f);}
}
