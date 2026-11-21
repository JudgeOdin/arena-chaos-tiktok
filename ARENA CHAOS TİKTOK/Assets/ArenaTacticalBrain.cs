using UnityEngine;

/// <summary>Local combat decisions: retreat, flank, keep range, avoid crowds and recover.</summary>
public sealed class ArenaTacticalBrain : MonoBehaviour
{
    public enum Tactic { Approach, Flank, Retreat, Recover, KeepRange, Dodge }
    public Tactic CurrentTactic { get; private set; }
    public int RetreatDecisions { get; private set; }
    public int FlankDecisions { get; private set; }
    public int DodgeDecisions { get; private set; }
    ArenaFighter fighter;
    float decideAt,retreatUntil,restUntil,lastRetreat=-10,nextDodge,dodgeUntil;
    Vector3 dodgeDirection;
    readonly RaycastHit[] obstacles=new RaycastHit[16];
    float preferredSide,aggression;
    void Awake(){fighter=GetComponent<ArenaFighter>();preferredSide=Random.value<.5f?-1:1;aggression=Random.Range(.3f,.85f);}
    public ArenaFighter SelectTarget()
    {
        var duel=fighter.Arena.DuelOpponent(fighter);
        if (duel!=null) return duel;
        ArenaFighter best=null;float bestScore=float.MaxValue;
        foreach(var other in fighter.Arena.Fighters)
        {
            if(other==null||!fighter.Arena.CanAttack(fighter,other)||!other.IsAlive||other.IsEntering||!other.isActiveAndEnabled)continue;
            float distance=Vector3.Distance(transform.position,other.transform.position);
            float score=distance*(.7f+.3f*other.Health/other.MaxHealth);
            if(other.PowerUps.Has(ArenaPower.Shadow))score*=1.8f;
            if(other.PowerUps.Has(ArenaPower.Giant)&&fighter.Health<fighter.MaxHealth*.5f)score*=1.5f;
            if(other.Role==ArenaFighterRole.Archer&&fighter.Role!=ArenaFighterRole.Archer)score*=.85f;
            if(score<bestScore){bestScore=score;best=other;}
        }
        return best;
    }
    public Vector3 Decide(ArenaFighter target,float range,out bool attack)
    {
        attack=false;if(target==null)return Vector3.zero;
        Vector3 direction=target.transform.position-transform.position;direction.y=0;
        float distance=direction.magnitude;var toward=direction.normalized;
        var settings=fighter.Arena.MatchSettings;
        float low=settings!=null?settings.retreatHealthFraction:.35f;
        if(Time.time>=decideAt)
        {
            decideAt=Time.time+Random.Range(.6f,1.1f);
            if(fighter.Health/fighter.MaxHealth<low&&Time.time-lastRetreat>4)
            {retreatUntil=Time.time+Random.Range(1.8f,3);lastRetreat=Time.time;RetreatDecisions++;}
            if(Time.time>=retreatUntil)
            {
                CurrentTactic=Random.value>aggression?Tactic.Flank:Tactic.Approach;
                if(CurrentTactic==Tactic.Flank){FlankDecisions++;preferredSide=-preferredSide;}
                restUntil=Random.value<.18f?Time.time+.3f:Time.time;
            }
        }
        Vector3 tangent=new Vector3(-toward.z,0,toward.x)*preferredSide;
        if(Time.time>nextDodge&&target.IsPunching&&distance<2.4f&&CurrentTactic!=Tactic.Retreat)
        {
            nextDodge=Time.time+(settings!=null?settings.dodgeCooldown:2.5f);
            if(Random.value<(settings!=null?settings.dodgeChance:.45f))
            {dodgeUntil=Time.time+.32f;dodgeDirection=-toward*.5f+tangent;DodgeDecisions++;}
        }
        Vector3 movement;
        if(Time.time<dodgeUntil)
        {CurrentTactic=Tactic.Dodge;movement=dodgeDirection;}
        else if(Time.time<retreatUntil)
        {CurrentTactic=Tactic.Retreat;movement=-toward+tangent*.6f;}
        else if(Time.time<restUntil)
        {CurrentTactic=Tactic.Recover;movement=Vector3.zero;}
        else if(fighter.Role==ArenaFighterRole.Archer&&distance<range*.65f)
        {CurrentTactic=Tactic.KeepRange;movement=-toward+tangent*.25f;attack=distance>2;}
        else
        {
            movement=distance>range?toward+(CurrentTactic==Tactic.Flank?tangent*.45f:Vector3.zero):CurrentTactic==Tactic.Flank?tangent*.55f:Vector3.zero;
            attack=distance<=range;
        }
        Vector3 separation=Vector3.zero;
        foreach(var other in fighter.Arena.Fighters)
        {
            if(other==null||other==fighter||!other.IsAlive||other.IsEntering)continue;
            var away=transform.position-other.transform.position;away.y=0;float sqr=away.sqrMagnitude;
            if(sqr>.01f&&sqr<1.7f)separation+=away.normalized*(1-sqr/1.7f);
        }
        movement+=separation*.7f;
        if(movement.sqrMagnitude>.01f)
        {
            int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.45f,.22f,movement.normalized,obstacles,.8f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                if(obstacles[i].collider.GetComponentInParent<ArenaFighter>()!=null)continue;
                var normal=obstacles[i].normal;normal.y=0;
                if(normal.sqrMagnitude>.2f)movement=Vector3.ProjectOnPlane(movement,normal)+normal*.25f+tangent*.35f;
            }
        }
        float edge=fighter.Arena.ArenaRadius-1.2f;
        if(Mathf.Abs(transform.position.x)>edge)movement.x-=Mathf.Sign(transform.position.x)*1.5f;
        if(Mathf.Abs(transform.position.z)>edge)movement.z-=Mathf.Sign(transform.position.z)*1.5f;
        if(CurrentTactic==Tactic.Retreat&&Time.time-fighter.LastDamageTime>3&&fighter.Health/fighter.MaxHealth<.45f)
            fighter.Heal((settings!=null?settings.recoveryPerSecond:2)*Time.fixedDeltaTime);
        return Vector3.ClampMagnitude(movement,1);
    }
}
