using UnityEngine;
public sealed class ArenaAudioEffects : MonoBehaviour
{
    static ArenaAudioEffects instance;
    AudioSource audioSource;
    AudioSource musicSource;
    [SerializeField, Range(0,1)] float effectsVolume=.35f;
    [SerializeField, Range(0,1)] float musicVolume=.18f;
    float nextImpactSound;
    float nextPowerImpact;
    float nextSpellSound;
    readonly AudioClip[] spellClips=new AudioClip[5];
    ArenaContent content;
    Material sparkMaterial;
    AudioClip infectionClip;
    float nextInfection;
    bool ownsMaterial;
    float nextBlast, nextSound;
    Vector3 blastOrigin;
    readonly ParticleSystem[] pool = new ParticleSystem[12];
    readonly ParticleSystem[] burstPool = new ParticleSystem[12];
    int burstCursor;
    int cursor;
    void Awake()
    {
        instance=this;content=ArenaContent.Load();audioSource=gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake=false;audioSource.volume=effectsVolume;
        musicSource=ArenaMusicPlayer.GetOrCreate(content!=null?content.battleMusic:null);
        infectionClip=Resources.Load<AudioClip>("ArenaChaos/Infection/Reanimation");
        string[] names={"Fire","Ice","Lightning","Magic","Meteor"};
        for(int i=0;i<names.Length;i++)spellClips[i]=Resources.Load<AudioClip>("ArenaChaos/SpellAudio/"+names[i]);
        Shader shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if(shader==null) shader=Shader.Find("Particles/Standard Unlit");
        sparkMaterial=content!=null?content.sparkMaterial:null;
        if(sparkMaterial==null){sparkMaterial=new Material(shader);ownsMaterial=true;}
        if(content!=null) sparkMaterial.mainTexture=content.spark;
        sparkMaterial.SetFloat("_Surface",1);sparkMaterial.SetFloat("_Blend",0);
        sparkMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        sparkMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        sparkMaterial.SetInt("_ZWrite",0);sparkMaterial.renderQueue=3000;
        for(int i=0;i<pool.Length;i++)
        {
            var obj=new GameObject("Pooled Contact Sparks");obj.transform.SetParent(transform,false);
            var p=obj.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main;main.loop=false;main.playOnAwake=false;main.duration=.25f;main.startLifetime=.22f;
            main.startSpeed=3f;main.startSize=.13f;main.maxParticles=20;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=p.emission;emission.rateOverTime=0;
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.08f;
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial=sparkMaterial;pool[i]=p;
        }
    }
    public static void Impact(Vector3 position,bool heavy)
    {
        if(instance==null)return;
        var p=instance.pool[instance.cursor++%instance.pool.Length];p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        p.transform.position=position;var main=p.main;main.startColor=heavy?new Color(1,.45f,.08f):new Color(1,.85f,.3f);
        p.Emit(heavy?16:10);
        var clips=instance.content!=null?instance.content.punches:null;
        if(clips!=null&&clips.Length>0&&Time.time>=instance.nextImpactSound)
        {instance.nextImpactSound=Time.time+.035f;instance.audioSource.PlayOneShot(clips[Random.Range(0,clips.Length)],heavy?.65f:.4f);}
    }
    public static void Cheer()
    {if(instance!=null&&instance.content!=null&&instance.content.cheer!=null)instance.audioSource.PlayOneShot(instance.content.cheer,.6f);}
    public static void Spell(ArenaPower power,bool impact=false)
    {
        if(instance==null||Time.time<instance.nextSpellSound)return;
        instance.nextSpellSound=Time.time+(impact?.12f:.18f);
        int index=power==ArenaPower.Meteor?4:power==ArenaPower.Fire||power==ArenaPower.Firestorm||power==ArenaPower.Explosive?0:power==ArenaPower.Ice||power==ArenaPower.FrozenTime?1:power==ArenaPower.Lightning?2:3;
        var clip=instance.spellClips[index];if(clip!=null)instance.audioSource.PlayOneShot(clip,impact?.20f:.4f);
    }
    public static void PowerHit(Vector3 position,ArenaPower power)
    {
        if(instance==null||instance.content==null||Time.time<instance.nextPowerImpact)return;
        instance.nextPowerImpact=Time.time+.035f;instance.blastOrigin=position;
        Spell(power,true);
        var color=power==ArenaPower.Ice||power==ArenaPower.Lightning?Color.cyan:power==ArenaPower.Poison?Color.green:power==ArenaPower.Vampire?Color.red:power==ArenaPower.HolyShield?Color.yellow:new Color(1,.32f,.015f);
        bool fire=power==ArenaPower.Fire||power==ArenaPower.Explosive;
        instance.Burst("Power impact",fire?instance.content.flameMaterial:instance.sparkMaterial,color,fire?24:20,.45f,fire?.65f:.25f,.15f,fire?3:4);
    }
    public static void Infection(Vector3 position)
    {
        if (instance == null) return;
        instance.blastOrigin = position + Vector3.down*.7f;
        instance.Burst("Infection spores",instance.sparkMaterial,new Color(.61f,.85f,.31f),22,.72f,.14f,.24f,2.1f);
        instance.Burst("Infection dust",instance.sparkMaterial,new Color(.25f,.36f,.17f),13,.63f,.45f,.23f,1.1f);
        if (instance.infectionClip != null && Time.time >= instance.nextInfection)
        {
            instance.nextInfection = Time.time+.25f;
            instance.audioSource.PlayOneShot(instance.infectionClip,.72f);
        }
    }

    public static void Step()
    {
        if(instance==null||instance.content==null||Time.time<instance.nextSound)return;
        instance.nextSound=Time.time+.06f;
        var clips=instance.content.footsteps;
        if(clips!=null&&clips.Length>0)instance.audioSource.PlayOneShot(clips[Random.Range(0,clips.Length)],.10f);
    }
    public static void Door()
    {
        if(instance==null||instance.content==null)return;
        var clips=instance.content.doors;
        if(clips!=null&&clips.Length>0)instance.audioSource.PlayOneShot(clips[Random.Range(0,clips.Length)],.4f);
    }
    public static void Blast()
    {BlastAt(Vector3.zero);}
    public static void BlastAt(Vector3 position)
    {
        if(instance==null||Time.time<instance.nextBlast)return;
        instance.blastOrigin=position;
        instance.nextBlast=Time.time+1f;instance.StartCoroutine(instance.BlastSequence());
    }
    public static void MeteorExplosion(Vector3 position)
    {if(instance!=null)instance.StartCoroutine(instance.MeteorSequence(position));}
    System.Collections.IEnumerator MeteorSequence(Vector3 position)
    {
        Spell(ArenaPower.Meteor);
        blastOrigin=position;
        Burst("Meteor White Hot Core",content.flameMaterial,new Color(1,.9f,.55f),16,.7f,5.5f,.5f,3);
        Burst("Meteor Fireball",content.flameMaterial,new Color(1,.3f,.025f),96,1.1f,2.4f,1,8);
        Burst("Meteor Shock Front",sparkMaterial,new Color(1,.8f,.35f),128,.8f,.65f,.5f,18);
        var waveObject=new GameObject("Meteor expanding shock ring");waveObject.transform.position=position+Vector3.up*.12f;
        var wave=waveObject.AddComponent<LineRenderer>();wave.useWorldSpace=false;wave.loop=true;wave.positionCount=97;wave.widthMultiplier=.2f;wave.sharedMaterial=sparkMaterial;
        wave.startColor=wave.endColor=new Color(1,.8f,.4f);
        var flash=waveObject.AddComponent<Light>();flash.type=LightType.Point;flash.range=24;flash.intensity=12;flash.color=new Color(1,.6f,.2f);flash.shadows=LightShadows.None;
        Door();Impact(position,true);Cheer();
        float elapsed=0;
        while(elapsed<.85f)
        {
            elapsed+=Time.deltaTime;float t=elapsed/.85f;float radius=Mathf.Lerp(.3f,16,1-Mathf.Pow(1-t,2));
            for(int i=0;i<97;i++){float angle=i*Mathf.PI*2/96;wave.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));}
            wave.widthMultiplier=Mathf.Lerp(.28f,.015f,t);flash.intensity=12*(1-t)*(1-t);
            if(elapsed>.12f&&flash.range>20)
            {
                flash.range=20;blastOrigin=position;
                Burst("Meteor Rising Smoke",content.smokeMaterial,new Color(.2f,.15f,.12f,.8f),64,2.3f,3.5f,2,4);
                Burst("Meteor embers",sparkMaterial,new Color(1,.4f,.04f),96,1.4f,.18f,.7f,10);
            }
            yield return null;
        }
        Destroy(waveObject);
    }
    System.Collections.IEnumerator BlastSequence()
    {
        Burst("Blast Warning",sparkMaterial,new Color(1,.65f,.1f),22,.25f,.4f,2.2f,0f);
        yield return new WaitForSeconds(.22f);
        Burst("Radial Shockwave",sparkMaterial,new Color(1,.8f,.3f),64,.55f,.6f,.3f,11f);
        Burst("Fire Burst",content.flameMaterial,new Color(1,.38f,.1f),28,.65f,1.2f,.5f,4.5f);
        Burst("Dust Cloud",content.smokeMaterial,new Color(.55f,.46f,.35f,.55f),20,1.2f,1.7f,.8f,3f);
        Door();Cheer();
    }
    void Burst(string name,Material material,Color color,int count,float lifetime,float size,float radius,float speed)
    {
        int slot=burstCursor++%burstPool.Length;
        var p=burstPool[slot];
        if(p==null){var obj=new GameObject("Pooled "+name);obj.transform.SetParent(transform,false);p=obj.AddComponent<ParticleSystem>();burstPool[slot]=p;}
        p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.transform.position=blastOrigin+Vector3.up*.3f;
        var main=p.main;main.loop=false;main.playOnAwake=false;main.duration=lifetime;main.startLifetime=lifetime;
        main.startColor=color;main.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);main.startSpeed=speed;main.maxParticles=count;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        var shape=p.shape;shape.shapeType=name.Contains("Core")||name.Contains("Fireball")||name.Contains("Smoke")?ParticleSystemShapeType.Sphere:ParticleSystemShapeType.Circle;shape.radius=radius;shape.rotation=new Vector3(90,0,0);
        var velocity=p.velocityOverLifetime;velocity.enabled=name.Contains("Smoke");velocity.space=ParticleSystemSimulationSpace.World;velocity.x=0;velocity.y=2;velocity.z=0;
        var emission=p.emission;emission.rateOverTime=0;
        var fade=p.colorOverLifetime;fade.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
        p.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
        p.Emit(count);
    }
    void OnDestroy(){if(instance==this)instance=null;if(ownsMaterial&&sparkMaterial!=null)Destroy(sparkMaterial);}
    void Update()
    {
        float announcerGain = ArenaAnnouncer.BackgroundGain;
        audioSource.volume = effectsVolume * Mathf.Lerp(.55f, 1f, announcerGain);
        var arena=GetComponent<BattleArenaManager>();
        float intensity=arena==null?1:Mathf.Lerp(.55f,1,Mathf.Clamp01(arena.LivingCount/12f));
        float duckRate = announcerGain < 1f ? 4f : .24f;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume,
            musicVolume * intensity * announcerGain, Time.unscaledDeltaTime * duckRate);
        if(Time.timeScale==0){if(musicSource.isPlaying)musicSource.Pause();}
        else if(musicSource.clip!=null&&!musicSource.isPlaying)musicSource.UnPause();
    }
}
