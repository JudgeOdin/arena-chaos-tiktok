using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Growing ground fractures followed by the boss's actual impact.</summary>
public sealed class ArenaBossSlamVisual : MonoBehaviour
{
    Material energy;
    Mesh fractures;
    ParticleSystem dust, embers, impactDust, impactSparks;
    LineRenderer wave;
    float began, duration, radius, struckAt;
    bool struck;
    static readonly int Reveal=Shader.PropertyToID("_Reveal"), Intensity=Shader.PropertyToID("_Intensity");

    public static GameObject Begin(Transform parent,Vector3 position,float radius,float duration)
    {
        var root=new GameObject("Giant ground fracture warning");
        root.transform.SetParent(parent);
        root.transform.position=new Vector3(position.x,.105f,position.z);
        var effect=root.AddComponent<ArenaBossSlamVisual>();
        effect.radius=radius;effect.duration=duration;effect.began=Time.time;
        effect.Build();return root;
    }

    void Build()
    {
        var content=ArenaContent.Load();
        energy=new Material(Resources.Load<Shader>("ArenaChaos/GroundFracture"));
        energy.SetFloat(Reveal,.08f);
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
        var random=new System.Random(173);
        for(int arm=0;arm<11;arm++)
        {
            float angle=arm*Mathf.PI*2/11+(float)random.NextDouble()*.17f;
            Vector3 previous=Vector3.zero;
            for(int step=1;step<=7;step++)
            {
                float r=radius*step/7;
                float bend=angle+((float)random.NextDouble()-.5f)*.32f;
                var next=new Vector3(Mathf.Cos(bend)*r,0,Mathf.Sin(bend)*r);
                Ribbon(previous,next,.16f*(1-step*.065f),vertices,uvs,colors,triangles);
                if(step==3||step==5)
                {
                    float side=bend+(arm%2==0?1:-1)*.64f;
                    var tip=next+new Vector3(Mathf.Cos(side),0,Mathf.Sin(side))*.55f;
                    if(tip.magnitude<radius)Ribbon(next,tip,.065f,vertices,uvs,colors,triangles);
                }
                previous=next;
            }
        }
        // Short rim accents mark the exact damage boundary without covering the floor.
        for(int arc=0;arc<16;arc++)
            for(int i=0;i<3;i++)
            {
                float a=arc*Mathf.PI*2/16+i*.035f;
                Ribbon(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius,
                    new Vector3(Mathf.Cos(a+.035f),0,Mathf.Sin(a+.035f))*radius,.055f,vertices,uvs,colors,triangles);
            }
        fractures=new Mesh{name="Ground fracture ribbons"};
        fractures.SetVertices(vertices);fractures.SetUVs(0,uvs);fractures.SetColors(colors);fractures.SetTriangles(triangles,0);fractures.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh=fractures;
        var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=energy;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        if(content!=null)
        {
            dust=Particles("Dust escaping fissures",content.smokeMaterial,new Color(.72f,.55f,.33f,.38f),.65f,.9f,.25f,radius*.8f,true);
            embers=Particles("Rising charged grit",content.sparkMaterial,new Color(1,.66f,.16f,.8f),.065f,.75f,.9f,radius*.88f,true);
            impactDust=Particles("Ground slam dust front",content.smokeMaterial,new Color(.65f,.48f,.29f,.42f),1.1f,1.15f,3.2f,radius*.65f,false);
            impactSparks=Particles("Ground slam hot fragments",content.sparkMaterial,new Color(1,.78f,.37f),.12f,.65f,5,radius*.3f,false);
        }
        var waveObject=new GameObject("Expanding impact front");waveObject.transform.SetParent(transform,false);
        wave=waveObject.AddComponent<LineRenderer>();wave.useWorldSpace=false;wave.loop=true;
        wave.positionCount=97;wave.sharedMaterial=energy;wave.enabled=false;
        wave.startColor=wave.endColor=new Color(1,.85f,.6f,.65f);
        wave.shadowCastingMode=ShadowCastingMode.Off;wave.receiveShadows=false;
    }

    void Ribbon(Vector3 a,Vector3 b,float width,List<Vector3> vertices,List<Vector2> uvs,List<Color> colors,List<int> triangles)
    {
        var direction=(b-a).normalized;var across=new Vector3(-direction.z,0,direction.x)*width*.5f;
        int n=vertices.Count;
        vertices.Add(a-across);vertices.Add(a+across);vertices.Add(b-across);vertices.Add(b+across);
        float start=a.magnitude/radius*.88f,end=b.magnitude/radius*.88f;
        uvs.Add(new Vector2(start,0));uvs.Add(new Vector2(start,1));uvs.Add(new Vector2(end,0));uvs.Add(new Vector2(end,1));
        for(int i=0;i<4;i++)colors.Add(Color.white);
        triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n+2);triangles.Add(n+1);triangles.Add(n+3);
    }

    ParticleSystem Particles(string label,Material material,Color color,float size,float life,float speed,float spread,bool loop)
    {
        var root=new GameObject(label);root.transform.SetParent(transform,false);
        var p=root.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=p.main;main.playOnAwake=false;main.loop=loop;main.startLifetime=life;
        main.startColor=color;main.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);
        main.startSpeed=speed;main.maxParticles=100;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.gravityModifier=loop?0:.28f;
        var emission=p.emission;emission.rateOverTime=loop?12:0;
        var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=spread;shape.rotation=new Vector3(-90,0,0);
        main.startSpeed=loop?speed:0;
        var velocity=p.velocityOverLifetime;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=0;velocity.y=loop?.45f:2.2f;velocity.z=0;velocity.radial=loop?.15f:speed;velocity.enabled=true;
        var fade=p.colorOverLifetime;fade.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;
        var scale=p.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,1.8f));
        p.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
        if(loop)p.Play();return p;
    }

    public void Strike()
    {
        if(struck)return;
        struck=true;struckAt=Time.time;
        if(dust!=null)dust.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        if(embers!=null)embers.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        if(impactDust!=null){impactDust.Play();impactDust.Emit(48);}
        if(impactSparks!=null){impactSparks.Play();impactSparks.Emit(65);}
        energy.SetFloat(Reveal,2);wave.enabled=true;
        ArenaAudioEffects.BossSlam(transform.position);
        Destroy(gameObject,1.65f);
    }

    void Update()
    {
        if(!struck)
        {
            float t=Mathf.Clamp01((Time.time-began)/duration);
            energy.SetFloat(Reveal,.12f+t);
            energy.SetFloat(Intensity,.55f+t*.85f+Mathf.Sin(Time.time*(7+t*12))*.13f*t);
            if(embers!=null){var emission=embers.emission;emission.rateOverTime=12+t*24;}
            return;
        }
        float age=Time.time-struckAt;
        energy.SetFloat(Intensity,Mathf.Max(0,1-age/1.25f)*(age<.1f?2.5f:1.4f));
        float progress=Mathf.Clamp01(age/.65f);
        float distance=radius*Mathf.Lerp(.12f,1.32f,1-Mathf.Pow(1-progress,2));
        for(int i=0;i<97;i++){float angle=i*Mathf.PI*2/96;wave.SetPosition(i,new Vector3(Mathf.Cos(angle)*distance,.035f,Mathf.Sin(angle)*distance));}
        wave.widthMultiplier=Mathf.Lerp(.24f,.015f,progress);
        wave.startColor=wave.endColor=new Color(1,.85f,.6f,(1-progress)*.8f);
        wave.enabled=age<.65f;
    }

    void OnDestroy(){if(energy!=null)Destroy(energy);if(fractures!=null)Destroy(fractures);}
}
