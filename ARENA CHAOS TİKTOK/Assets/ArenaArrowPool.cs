using UnityEngine;

/// <summary>Bounded real projectiles: sweep the flight segment, hit the first body/wall, never home.</summary>
public sealed class ArenaArrowPool : MonoBehaviour
{
    sealed class Arrow { public Transform visual;public ArenaFighter owner;public Vector3 velocity;public float expires;public bool active; }
    readonly Arrow[] arrows=new Arrow[64];
    readonly RaycastHit[] hits=new RaycastHit[64];
    public int HitCount { get; private set; }
    public static void Fire(ArenaFighter owner,ArenaFighter target)
    {
        var pool=owner.Arena.GetComponent<ArenaArrowPool>();if(pool==null)pool=owner.Arena.gameObject.AddComponent<ArenaArrowPool>();
        pool.Launch(owner,target);
    }
    void Launch(ArenaFighter owner,ArenaFighter target)
    {
        var content=ArenaContent.Load();if(content==null||content.arrowPrefab==null||target==null)return;
        for(int i=0;i<arrows.Length;i++)
        {
            var arrow=arrows[i];if(arrow!=null&&arrow.active)continue;
            if(arrow==null)
            {
                var obj=Instantiate(content.arrowPrefab,transform);obj.name="Pooled ranger arrow";
                foreach(var collider in obj.GetComponentsInChildren<Collider>())Destroy(collider);
                Bounds bounds=new Bounds();bool found=false;foreach(var r in obj.GetComponentsInChildren<Renderer>()){if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}
                if(found)obj.transform.localScale*=.9f/Mathf.Max(.01f,bounds.size.magnitude);
                arrow=new Arrow{visual=obj.transform};arrows[i]=arrow;
            }
            var equipment=owner.GetComponent<ArenaClassEquipment>();
            var start=equipment!=null&&equipment.ArrowOrigin!=null?equipment.ArrowOrigin.position:owner.transform.position+Vector3.up*.3f+owner.transform.forward*.7f;
            var aim=target.transform.position+Vector3.up*.45f;
            arrow.owner=owner;arrow.velocity=(aim-start).normalized*16;arrow.visual.position=start;arrow.visual.rotation=Quaternion.LookRotation(arrow.velocity);arrow.active=true;arrow.expires=Time.time+1.5f;arrow.visual.gameObject.SetActive(true);return;
        }
    }
    void FixedUpdate()
    {
        foreach(var arrow in arrows)
        {
            if(arrow==null||!arrow.active)continue;
            if(Time.time>=arrow.expires||arrow.owner==null||!arrow.owner.IsAlive||arrow.owner.Arena.IsFinished){Hide(arrow);continue;}
            Vector3 step=arrow.velocity*Time.fixedDeltaTime;
            int count=Physics.SphereCastNonAlloc(arrow.visual.position,.10f,step.normalized,hits,step.magnitude,~0,QueryTriggerInteraction.Ignore);
            float nearest=float.MaxValue;ArenaFighter victim=null;bool blocked=false;Vector3 impact=arrow.visual.position+step;
            for(int i=0;i<count;i++)
            {
                var fighter=hits[i].collider.GetComponentInParent<ArenaFighter>();
                if(fighter==arrow.owner||fighter!=null&&(!fighter.IsAlive||fighter.IsEntering))continue;
                if(hits[i].distance<nearest){nearest=hits[i].distance;victim=fighter;blocked=true;impact=hits[i].point;}
            }
            if(blocked)
            {
                if(victim!=null&&arrow.owner.TryRangedHit(victim)){HitCount++;ArenaAudioEffects.Impact(impact,false);}
                Hide(arrow);
            }
            else arrow.visual.position+=step;
        }
    }
    static void Hide(Arrow arrow){arrow.active=false;arrow.visual.gameObject.SetActive(false);}
}
