using System.Collections.Generic;
using UnityEngine;

/// <summary>Animation and swept limb contacts; never moves the fighter root.</summary>
[DefaultExecutionOrder(100)]
public sealed class ImportedFighterAnimationDriver : MonoBehaviour
{
    private static readonly string[] AttackStates =
        { "5 inch punch", "back fist", "elbow chop", "low kick", "back kick", "axe kick", "knee strike" };
    private Animator animator;
    private ArenaFighter fighter;
    private Transform root;
    private Transform leftHand, rightHand, leftFoot, rightFoot, leftKnee, rightKnee;
    private Transform leftThigh, rightThigh;
    private Quaternion leftRest, rightRest, leftShinRest, rightShinRest;
    private Transform leftUpperArm, rightUpperArm, leftForearm, rightForearm, chest, head;
    private Quaternion leftArmRest, rightArmRest, leftForearmRest, rightForearmRest, chestRest;
    private float guardWeight;
    private readonly Collider[] contacts = new Collider[128];
    private readonly HashSet<ArenaFighter> struck = new HashSet<ArenaFighter>();
    private readonly List<int> states = new List<int>();
    private readonly List<float> lengths = new List<float>();
    private readonly List<bool> kicks = new List<bool>();
    private readonly List<bool> knees = new List<bool>();
    private int lastCycle, previous = -1;
    private int idle;
    private bool attacking, kick, kneeStrike, contactReady;
    private float startTime, phase, blend, filteredSpeed;
    private Vector3 lastPosition, previousLeft, previousRight;
    public bool UsesLimbContacts => enabled && animator != null && animator.isActiveAndEnabled && states.Count > 0;
    public int ContactHitCount { get; private set; }
    public float WalkBlend => blend;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
        fighter = GetComponentInParent<ArenaFighter>();
        if (animator == null || !animator.isHuman || animator.avatar == null ||
            !animator.avatar.isValid || animator.runtimeAnimatorController == null || fighter == null)
        {
            Debug.LogWarning("Fighter animation needs a valid Humanoid Avatar and controller.", this);
            enabled = false;
            return;
        }
        animator.applyRootMotion = false;
        root = fighter.transform;
        lastPosition = root.position;
        lastCycle = fighter.AttackCycle;
        idle = Animator.StringToHash("Base Layer.Idle");
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        leftKnee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightKnee = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (leftThigh != null) leftRest = leftThigh.localRotation;
        if (rightThigh != null) rightRest = rightThigh.localRotation;
        if (leftKnee != null) leftShinRest = leftKnee.localRotation;
        if (rightKnee != null) rightShinRest = rightKnee.localRotation;
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (leftUpperArm != null) leftArmRest = leftUpperArm.localRotation;
        if (rightUpperArm != null) rightArmRest = rightUpperArm.localRotation;
        if (leftForearm != null) leftForearmRest = leftForearm.localRotation;
        if (rightForearm != null) rightForearmRest = rightForearm.localRotation;
        if (chest != null) chestRest = chest.localRotation;
        foreach (string name in AttackStates)
        {
            int hash = Animator.StringToHash("Base Layer." + name);
            if (!animator.HasState(0, hash)) continue;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name != name) continue;
                states.Add(hash);
                lengths.Add(clip.length);
                kicks.Add(name.Contains("kick") || name.Contains("knee"));
                knees.Add(name.Contains("knee"));
                break;
            }
        }
        if (animator.HasState(0, idle)) animator.Play(idle, 0, 0f);
    }

    private void Update()
    {
        if (fighter == null || !fighter.IsAlive || animator == null) return;
        if (attacking && Time.time - startTime >= 0.76f)
        {
            attacking = false;
            animator.speed = 1f;
            if (animator.HasState(0, idle)) animator.CrossFadeInFixedTime(idle, 0.08f);
        }
        if (fighter.AttackCycle == lastCycle) return;
        lastCycle = fighter.AttackCycle;
        PlayNextAttack();
    }

    public void PlayNextAttack()
    {
        if (!UsesLimbContacts) return;
        int index = Random.Range(0, states.Count);
        if (states.Count > 1 && index == previous) index = (index + 1) % states.Count;
        previous = index;
        kick = kicks[index];
        kneeStrike = knees[index];
        animator.speed = Mathf.Max(0.01f, lengths[index]) / 0.72f;
        animator.CrossFadeInFixedTime(states[index], 0.05f, 0, 0f);
        startTime = Time.time;
        attacking = true;
        struck.Clear();
        contactReady = false;
    }

    private void LateUpdate()
    {
        if (root == null || fighter == null || !fighter.IsAlive) return;
        Vector3 delta = root.position - lastPosition;
        lastPosition = root.position;
        delta.y = 0f;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = Mathf.Min(delta.magnitude / dt, 8f);
        filteredSpeed = Mathf.Lerp(filteredSpeed, speed, 1f - Mathf.Exp(-12f * dt));
        float desired = (fighter.IsMoving || fighter.IsEntering) && !attacking ? Mathf.Clamp01(filteredSpeed / 2.8f) : 0f;
        blend = Mathf.MoveTowards(blend, desired, dt * 8f);
        phase += filteredSpeed * dt * 7f;
        // Each bone's axis is transformed from fighter space into its parent
        // space, so mirrored FBX local axes do not invert one leg's phase.
        guardWeight = Mathf.MoveTowards(guardWeight, attacking ? 0f : 1f, dt * 9f);
        if (!attacking)
        {
            float stride = Mathf.Sin(phase);
            // Short shuffling steps from a flexed stance, rather than a march.
            PoseLeg(leftThigh, leftRest, -12f + stride * 10f * blend);
            PoseLeg(rightThigh, rightRest, -16f - stride * 10f * blend);
            PoseLeg(leftKnee, leftShinRest, 24f + Mathf.Max(0f, stride) * 12f * blend);
            PoseLeg(rightKnee, rightShinRest, 28f + Mathf.Max(0f, -stride) * 12f * blend);
            PoseLeg(chest, chestRest, 14f);
            ApplyGuard(leftUpperArm, leftForearm, leftHand, leftArmRest, leftForearmRest, -1f);
            ApplyGuard(rightUpperArm, rightForearm, rightHand, rightArmRest, rightForearmRest, 1f);
        }
        if (!attacking) return;
        Transform left = kick ? (kneeStrike ? leftKnee : leftFoot) : leftHand;
        Transform right = kick ? (kneeStrike ? rightKnee : rightFoot) : rightHand;
        if (left == null || right == null) return;
        Vector3 a = left.position, b = right.position;
        float elapsed = Time.time - startTime;
        // Wind-up and recovery cannot damage. Sweep between frames prevents
        // a fast hand/foot from tunnelling across the opponent collider.
        if (contactReady && elapsed >= 0.12f && elapsed <= 0.64f)
        {
            CheckContact(previousLeft, a, kick ? 0.23f : 0.19f);
            CheckContact(previousRight, b, kick ? 0.23f : 0.19f);
        }
        previousLeft = a;
        previousRight = b;
        contactReady = true;
    }

    private void Bend(Transform bone, Quaternion rest, float angle)
    {
        if (bone == null || bone.parent == null) return;
        Vector3 axis = bone.parent.InverseTransformDirection(root.right).normalized;
        bone.localRotation = Quaternion.AngleAxis(angle, axis) * rest;
    }

    private void PoseLeg(Transform bone, Quaternion rest, float angle)
    {
        if (bone == null) return;
        Quaternion animated = bone.localRotation;
        Bend(bone, rest, angle);
        bone.localRotation = Quaternion.Slerp(animated, bone.localRotation, guardWeight);
    }

    // Two-bone geometric IK places fists near the cheeks regardless of the
    // imported arm axes. Elbows stay down and slightly out from the ribs.
    private void ApplyGuard(Transform upper, Transform fore, Transform hand,
        Quaternion upperRest, Quaternion foreRest, float side)
    {
        if (upper == null || fore == null || hand == null || head == null) return;
        Quaternion animatedUpper = upper.localRotation;
        Quaternion animatedFore = fore.localRotation;
        upper.localRotation = upperRest;
        fore.localRotation = foreRest;
        Vector3 shoulder = upper.position;
        float a = Vector3.Distance(shoulder, fore.position);
        float b = Vector3.Distance(fore.position, hand.position);
        Vector3 target = head.position + root.right * (side * 0.16f)
            + root.forward * (side < 0f ? 0.25f : 0.17f) + root.up * 0.12f;
        Vector3 direction = target - shoulder;
        float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);
        if (a < 0.001f || b < 0.001f || direction.sqrMagnitude < 0.00001f) return;
        Vector3 axis = direction.normalized;
        Vector3 pole = -root.up + root.right * (side * 0.35f);
        Vector3 bend = Vector3.ProjectOnPlane(pole, axis).normalized;
        float along = (a * a - b * b + distance * distance) / (2f * distance);
        Vector3 elbow = shoulder + axis * along + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
        upper.rotation = Quaternion.FromToRotation(fore.position - shoulder, elbow - shoulder) * upper.rotation;
        fore.rotation = Quaternion.FromToRotation(hand.position - fore.position, target - fore.position) * fore.rotation;
        Quaternion guardUpper = upper.localRotation;
        Quaternion guardFore = fore.localRotation;
        upper.localRotation = Quaternion.Slerp(animatedUpper, guardUpper, guardWeight);
        fore.localRotation = Quaternion.Slerp(animatedFore, guardFore, guardWeight);
    }

    private void CheckContact(Vector3 from, Vector3 to, float radius)
    {
        int count = Physics.OverlapCapsuleNonAlloc(from, to, radius, contacts, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            ArenaFighter victim = contacts[i].GetComponentInParent<ArenaFighter>();
            if (victim == null || victim == fighter || !victim.IsAlive || !struck.Add(victim)) continue;
            if (fighter.ApplyLimbContactDamage(victim)) ContactHitCount++;
        }
    }

    private void OnDisable()
    {
        if (animator != null) animator.speed = 1f;
        attacking = false;
        contactReady = false;
    }
}
