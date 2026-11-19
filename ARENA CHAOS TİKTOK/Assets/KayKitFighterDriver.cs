using System.Collections.Generic;
using UnityEngine;

/// <summary>Authored Generic animation on its matching KayKit skeleton. No root motion.</summary>
[DefaultExecutionOrder(110)]
public sealed class KayKitFighterDriver : MonoBehaviour
{
    Animator animator;
    ArenaFighter fighter;
    Transform leftHand, rightHand, leftFoot, rightFoot;
    readonly Collider[] contacts = new Collider[64];
    readonly HashSet<ArenaFighter> struck = new HashSet<ArenaFighter>();
    Vector3 previousLeft, previousRight, lastPosition;
    int cycle, state;
    float started, speed;
    bool attacking, kick, ready;
    float recovering;
    float nextStep;
    public void React(bool death)
    {
        if(animator==null)return;
        if(death)
        {
            attacking=false;animator.speed=1f;
            if(animator.layerCount>1)animator.SetLayerWeight(1,0);
            Change("Death",true);
        }
        else if(!attacking)
        { recovering=Time.time+.2f;Change("Hit",true); }
    }
    public int ContactHitCount { get; private set; }
    public bool UsesLimbContacts => isActiveAndEnabled && animator != null && animator.runtimeAnimatorController != null;
    void Start()
    {
        animator = GetComponent<Animator>();
        fighter = GetComponentInParent<ArenaFighter>();
        if (animator == null || fighter == null) { enabled = false; return; }
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (Transform bone in GetComponentsInChildren<Transform>(true))
        {
            switch (bone.name)
            {
                case "hand.l": leftHand = bone; break;
                case "hand.r": rightHand = bone; break;
                case "foot.l": leftFoot = bone; break;
                case "foot.r": rightFoot = bone; break;
            }
        }
        if (!leftHand || !rightHand || !leftFoot || !rightFoot)
        { Debug.LogError("KayKit skeleton incomplete: contact damage disabled.", this); enabled = false; return; }
        cycle = fighter.AttackCycle;
        lastPosition = fighter.transform.position;
        Change("Guard", true);
    }
    void Update()
    {
        if (fighter == null || !fighter.IsAlive) return;
        if(Time.time<recovering)return;
        Vector3 delta = fighter.transform.position - lastPosition; delta.y = 0;
        lastPosition = fighter.transform.position;
        speed = Mathf.Lerp(speed, delta.magnitude / Mathf.Max(.0001f, Time.deltaTime), 1f - Mathf.Exp(-12f * Time.deltaTime));
        if (cycle != fighter.AttackCycle)
        {
            cycle = fighter.AttackCycle;
            kick = fighter.Role!=ArenaFighterRole.Archer && Random.value < .3f;
            attacking = true; ready = false; struck.Clear(); started = Time.time;
            string name = fighter.Role==ArenaFighterRole.Archer?"Bow":kick?"Kick":"Punch";
            float length = .7f;
            foreach (var clip in animator.runtimeAnimatorController.animationClips) if (clip.name == name) length = clip.length;
            animator.speed = length / .7f * fighter.PowerUps.AttackRate;
            Change(name, true);
        }
        if (attacking && (Time.time - started)*fighter.PowerUps.AttackRate > .74f) attacking = false;
        if (animator.layerCount > 1)
            animator.SetLayerWeight(1, Mathf.MoveTowards(animator.GetLayerWeight(1), attacking||fighter.Role==ArenaFighterRole.Archer ? 0f : 1f, Time.deltaTime * 16f));
        if (!attacking)
        {
            bool moving = speed > .15f && (fighter.IsMoving || fighter.IsEntering);
            animator.speed = moving ? Mathf.Clamp(speed / 2.2f, .65f, 1.7f) : 1f;
            Change(moving ? "Walk" : fighter.Role==ArenaFighterRole.Archer?"BowGuard":"Guard");
            if(moving && Time.time>nextStep){nextStep=Time.time+.34f;ArenaAudioEffects.Step();}
        }
    }
    void Change(string name, bool force = false)
    {
        int hash = Animator.StringToHash("Base Layer." + name);
        if (!force && state == hash) return;
        state = hash;
        animator.CrossFadeInFixedTime(hash, .09f, 0, 0f);
    }
    void LateUpdate()
    {
        if (!attacking || fighter == null || !fighter.IsAlive || fighter.Role==ArenaFighterRole.Archer) return;
        Vector3 a = (kick ? leftFoot : leftHand).position;
        Vector3 b = (kick ? rightFoot : rightHand).position;
        float time = (Time.time - started)*fighter.PowerUps.AttackRate;
        if (ready && time > .12f && time < .61f)
        { Contact(previousLeft, a); Contact(previousRight, b); }
        previousLeft = a; previousRight = b; ready = true;
    }
    void Contact(Vector3 from, Vector3 to)
    {
        int count = Physics.OverlapCapsuleNonAlloc(from, to, kick ? .22f : .18f, contacts, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var victim = contacts[i].GetComponentInParent<ArenaFighter>();
            if (victim == null || victim == fighter || !victim.IsAlive || victim.IsEntering || struck.Contains(victim)) continue;
            if (fighter.ApplyLimbContactDamage(victim))
            {
                struck.Add(victim); ContactHitCount++;
                ArenaBroadcastHUD.RecordHit(fighter, victim);
                ArenaAudioEffects.Impact(to, kick);
            }
        }
    }
}
