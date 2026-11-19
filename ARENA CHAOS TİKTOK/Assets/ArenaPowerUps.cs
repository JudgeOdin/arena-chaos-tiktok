using System;
using System.Collections.Generic;
using UnityEngine;

public enum ArenaPower { Fire, Lightning, Giant, Ice, Explosive, Vampire, Shadow, Meteor, HolyShield, Poison, Firestorm, FrozenTime }

/// <summary>Timed modifiers compose without changing permanent gift upgrades or the physics root scale.</summary>
public sealed class ArenaPowerUps : MonoBehaviour
{
    [Serializable] public class Settings
    {
        public ArenaPower power;
        [Min(.1f)] public float duration=12;
        [Min(0)] public float cooldown=1;
        [Min(0)] public float strength=8;
    }
    [SerializeField] Settings[] settings;
    ArenaPowerSettings tuning;
    readonly float[] ends=new float[12], nextUse=new float[12];
    readonly ParticleSystem[] effects=new ParticleSystem[12];
    static readonly ArenaPower[] ContactPowers={ArenaPower.Fire,ArenaPower.Lightning,ArenaPower.Ice,ArenaPower.Explosive,ArenaPower.Vampire,ArenaPower.Poison};
    readonly Collider[] overlaps=new Collider[96];
    readonly HashSet<ArenaFighter> victims=new HashSet<ArenaFighter>();
    ArenaFighter fighter, burnSource, poisonSource;
    float burnUntil,poisonUntil,slowedUntil,nextTick;
    Transform model;
    Vector3 modelScale;
    readonly List<Renderer> shadowRenderers=new List<Renderer>();
    readonly List<Material[]> originalMaterials=new List<Material[]>();
    readonly List<Material[]> transparentMaterials=new List<Material[]>();
    readonly List<Material> shadowMaterials=new List<Material>();
    bool shadowVisible;
    public bool Has(ArenaPower power)=>Time.time<ends[(int)power];
    public float MovementMultiplier=>(Has(ArenaPower.Lightning)?1+Config(ArenaPower.Lightning).strength*.0875f:1)*(Has(ArenaPower.Shadow)?1+Config(ArenaPower.Shadow).strength*.0375f:1)*(Has(ArenaPower.Giant)?(tuning!=null?tuning.giantMovementMultiplier:.8f):1)*(Time.time<slowedUntil?(tuning!=null?tuning.slowedMovementMultiplier:.45f):1);
    public float AttackRate=>Has(ArenaPower.Lightning)?1+Config(ArenaPower.Lightning).strength*.05625f:1;
    public float FireDamage=>Has(ArenaPower.Fire)?Config(ArenaPower.Fire).strength:0;
    public float DamageMultiplier=>Has(ArenaPower.Giant)?1+Config(ArenaPower.Giant).strength*.0875f:1;
    public float DamageReduction=>Has(ArenaPower.Ice)?1-Mathf.Clamp(Config(ArenaPower.Ice).strength*.05f,0,.8f):1;
    public string ActiveNames
    {
        get { string result="";for(int i=0;i<12;i++)if(Has((ArenaPower)i))result+=(result.Length>0?" / ":"")+Title((ArenaPower)i);return result; }
    }
    public static string Title(ArenaPower power)
    {
        string[] titles={"ALEV YUMRUĞU","ŞİMŞEK HIZI","DEV GÜÇ","BUZ ZIRHI","PATLAYICI YUMRUK","VAMPİR GÜCÜ","GÖLGE MODU","METEOR","KUTSAL KALKAN","ZEHİRLİ DARBE","ATEŞ FIRTINASI","DONMUŞ ZAMAN"};
        return titles[(int)power];
    }
    void Awake()
    {
        fighter=GetComponent<ArenaFighter>();
        var catalog=Resources.Load<ArenaPowerSettings>("ArenaChaos/PowerUps");
        tuning=catalog;
        if(catalog!=null&&catalog.powers!=null&&catalog.powers.Length==12)settings=catalog.powers;
        if(settings==null||settings.Length!=12)
        {settings=new Settings[12];for(int i=0;i<12;i++)settings[i]=new Settings{power=(ArenaPower)i};}
    }
    Settings Config(ArenaPower power)
    {foreach(var config in settings)if(config.power==power)return config;return new Settings{power=power};}
    public bool Activate(ArenaPower power)
    {
        if(fighter==null||!fighter.IsAlive||Time.time<nextUse[(int)power])return false;
        if(power==ArenaPower.Meteor&&(fighter.Arena==null||!fighter.Arena.TryReserveMeteor()))return false;
        var config=Config(power);nextUse[(int)power]=Time.time+config.cooldown;
        ends[(int)power]=Time.time+config.duration;
        if(power==ArenaPower.HolyShield)fighter.GiftShield(config.strength*5);
        if(power==ArenaPower.Meteor)
        {if(fighter.Arena!=null)fighter.Arena.StartCoroutine(Meteor(config.strength));else StartCoroutine(Meteor(config.strength));}
        if(model==null)
        {
            var driver=GetComponentInChildren<KayKitFighterDriver>(true);
            if(driver!=null){model=driver.transform;modelScale=model.localScale;}
        }
        EnsureEffect(power);
        ArenaAudioEffects.Spell(power);
        ArenaAudioEffects.Impact(transform.position+Vector3.up,true);
        ArenaBroadcastHUD.NotifyGift(fighter.DisplayName,Title(power),config.duration.ToString("0")+" saniye");
        return true;
    }
    static Color Tint(ArenaPower power)
    {
        switch(power){case ArenaPower.Ice:case ArenaPower.FrozenTime:return Color.cyan;case ArenaPower.Lightning:return new Color(.5f,.6f,1);case ArenaPower.Poison:return Color.green;case ArenaPower.Vampire:return Color.red;case ArenaPower.Shadow:return new Color(.3f,.1f,.5f);case ArenaPower.HolyShield:return Color.yellow;default:return new Color(1,.35f,.05f);}
    }
    readonly ArenaPowerVisual[] signatures=new ArenaPowerVisual[12];
    void EnsureEffect(ArenaPower power)
    {
        if(signatures[(int)power]==null)signatures[(int)power]=ArenaPowerVisual.Create(this,power);
    }
    public void Slow(float duration){slowedUntil=Mathf.Max(slowedUntil,Time.time+duration);}
    public void OnContact(ArenaFighter victim,float actualDamage)
    {
        if(victim==null)return;
        var target=victim.PowerUps;
        if(Has(ArenaPower.Fire)){target.burnUntil=Time.time+(tuning!=null?tuning.burnDuration:3);target.burnSource=fighter;}
        if(Has(ArenaPower.Poison)){target.poisonUntil=Time.time+(tuning!=null?tuning.poisonDuration:5);target.poisonSource=fighter;}
        if(Has(ArenaPower.Vampire))fighter.Heal(actualDamage*Mathf.Clamp01(Config(ArenaPower.Vampire).strength*.0375f));
        if(Has(ArenaPower.Ice))target.Slow(tuning!=null?tuning.contactSlowDuration:2);
        if(Has(ArenaPower.Giant)||Has(ArenaPower.Explosive))
        {
            var body=victim.GetComponent<Rigidbody>();var direction=victim.transform.position-transform.position;direction.y=0;
            if(body!=null&&!body.isKinematic)body.AddForce(direction.normalized*(Has(ArenaPower.Giant)?5:3),ForceMode.Impulse);
        }
        if(Has(ArenaPower.Explosive))Area(victim.transform.position,tuning!=null?tuning.explosiveRadius:2.1f,Config(ArenaPower.Explosive).strength,false,victim);
        foreach(var power in ContactPowers)
            if(Has(power)){ArenaAudioEffects.PowerHit(victim.transform.position+Vector3.up*.5f,power);break;}
        if(Has(ArenaPower.Fire)||Has(ArenaPower.Lightning)||Has(ArenaPower.Explosive)||Has(ArenaPower.Vampire)||Has(ArenaPower.Poison))ArenaAudioEffects.Impact(victim.transform.position+Vector3.up,true);
    }
    void Area(Vector3 center,float radius,float damage,bool slow,ArenaFighter excluded=null)
    {
        victims.Clear();int count=Physics.OverlapSphereNonAlloc(center,radius,overlaps);
        for(int i=0;i<count;i++)
        {
            var other=overlaps[i].GetComponentInParent<ArenaFighter>();
            if(other==null||other==fighter||other==excluded||!other.IsAlive||other.IsEntering||!victims.Add(other))continue;
            if(damage>0)other.TakeDamage(damage,fighter);if(slow)other.PowerUps.Slow(tuning!=null?tuning.auraSlowDuration:1.5f);
        }
    }
    System.Collections.IEnumerator Meteor(float strength)
    {
        // The cast survives the sender's death or destruction during the fall.
        var arena=fighter!=null?fighter.Arena:null;
        var caster=fighter;
        Vector3 landing=Vector3.up*.12f;
        var content=ArenaContent.Load();
        var meteor=new GameObject("Falling meteor flame");meteor.transform.position=landing+new Vector3(-5,18,-3);
        var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.name="Meteor core";Destroy(rock.GetComponent<Collider>());rock.transform.SetParent(meteor.transform,false);rock.transform.localScale=new Vector3(1.5f,1.8f,1.35f);
        if(content!=null)rock.GetComponent<Renderer>().sharedMaterial=content.stone;
        var particle=meteor.AddComponent<ParticleSystem>();var main=particle.main;main.startLifetime=.3f;main.startSize=1.8f;main.startSpeed=2;main.startColor=new Color(1,.4f,.04f);main.maxParticles=96;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=particle.emission;emission.rateOverTime=100;
        if(content!=null)particle.GetComponent<ParticleSystemRenderer>().sharedMaterial=content.flameMaterial;
        float elapsed=0;
        while(elapsed<1){elapsed+=Time.deltaTime;meteor.transform.position=Vector3.Lerp(landing+new Vector3(-5,18,-3),landing,elapsed);yield return null;}
        particle.Stop(false,ParticleSystemStopBehavior.StopEmitting);Destroy(meteor,.4f);
        if(arena!=null&&!arena.IsFinished)
        {
            var snapshot=new List<ArenaFighter>(arena.Fighters);
            foreach(var opponent in snapshot)if(opponent!=null&&opponent!=caster&&opponent.IsAlive)opponent.EliminateFromMeteor(caster);
        }
        ArenaAudioEffects.MeteorExplosion(landing);
    }
    void SetShadow(bool active)
    {
        if(shadowVisible==active)return;shadowVisible=active;
        if(active&&shadowRenderers.Count==0&&model!=null)
        foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            shadowRenderers.Add(renderer);originalMaterials.Add(renderer.sharedMaterials);
            var copies=new Material[renderer.sharedMaterials.Length];
            for(int i=0;i<copies.Length;i++)
            {
                var copy=new Material(renderer.sharedMaterials[i]);copy.SetFloat("_Surface",1);copy.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);copy.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);copy.SetInt("_ZWrite",0);copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");copy.renderQueue=3000;
                var color=copy.GetColor("_BaseColor");color.a=.35f;copy.SetColor("_BaseColor",color);copies[i]=copy;shadowMaterials.Add(copy);
            }
            transparentMaterials.Add(copies);
        }
        for(int i=0;i<shadowRenderers.Count;i++)if(shadowRenderers[i]!=null)shadowRenderers[i].sharedMaterials=active?transparentMaterials[i]:originalMaterials[i];
    }
    void OnDisable(){SetShadow(false);}
    void OnDestroy(){foreach(var material in shadowMaterials)if(material!=null)Destroy(material);}
    void Update()
    {
        if(fighter==null)return;
        SetShadow(Has(ArenaPower.Shadow)&&fighter.IsAlive);
        for(int i=0;i<12;i++)if(effects[i]!=null&&!Has((ArenaPower)i)&&effects[i].isEmitting)effects[i].Stop(false,ParticleSystemStopBehavior.StopEmitting);
        if(model!=null)model.localScale=Vector3.Lerp(model.localScale,modelScale*(Has(ArenaPower.Giant)?(tuning!=null?tuning.giantVisualScale:1.4f):1),1-Mathf.Exp(-7*Time.deltaTime));
        if(Time.time<nextTick||!fighter.IsAlive||fighter.IsEntering||fighter.Arena.IsFinished)return;
        nextTick=Time.time+1;
        if(Time.time<burnUntil)fighter.TakeDamage(tuning!=null?tuning.burnDamagePerSecond:3,burnSource);
        if(Time.time<poisonUntil)fighter.TakeDamage(tuning!=null?tuning.poisonDamagePerSecond:2,poisonSource);
        if(Has(ArenaPower.Firestorm))Area(transform.position,tuning!=null?tuning.firestormRadius:2.8f,Config(ArenaPower.Firestorm).strength,false);
        if(Has(ArenaPower.FrozenTime))Area(transform.position,tuning!=null?tuning.frozenTimeRadius:6,0,true);
        if(Has(ArenaPower.HolyShield)&&fighter.ShieldHealth<=0)ends[(int)ArenaPower.HolyShield]=0;
    }
}
public sealed class ArenaPowerFlame : MonoBehaviour
{
    public ArenaPowerUps owner;
    void Update(){var particle=GetComponent<ParticleSystem>();if(owner==null||!owner.Has(ArenaPower.Fire))particle.Stop(false,ParticleSystemStopBehavior.StopEmitting);else if(!particle.isEmitting)particle.Play();}
}
