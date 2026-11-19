using UnityEngine;

/// <summary>
/// PLAYER-only procedural motion for the Normalgon Humanoid skeleton.
/// It animates bones locally and never moves the gameplay root.
/// </summary>
public sealed class NormalgonMotionDriver : MonoBehaviour
{
    private ArenaFighter fighter;
    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private Transform leftForearm;
    private Transform rightForearm;
    private Transform leftThigh;
    private Transform rightThigh;
    private Transform leftShin;
    private Transform rightShin;
    private Transform leftFoot;
    private Transform rightFoot;
    private Transform spine;

    private Quaternion leftUpperArmBase;
    private Quaternion rightUpperArmBase;
    private Quaternion leftForearmBase;
    private Quaternion rightForearmBase;
    private Quaternion leftThighBase;
    private Quaternion rightThighBase;
    private Quaternion leftShinBase;
    private Quaternion rightShinBase;
    private Quaternion leftFootBase;
    private Quaternion rightFootBase;
    private Quaternion spineBase;
    private float phase;

    private void Awake()
    {
        fighter = GetComponentInParent<ArenaFighter>();
        leftUpperArm = FindBone("upper_arm.L");
        rightUpperArm = FindBone("upper_arm.R");
        leftForearm = FindBone("forearm.L");
        rightForearm = FindBone("forearm.R");
        leftThigh = FindBone("thigh.L");
        rightThigh = FindBone("thigh.R");
        leftShin = FindBone("shin.L");
        rightShin = FindBone("shin.R");
        leftFoot = FindBone("foot.L");
        rightFoot = FindBone("foot.R");
        spine = FindBone("spine.002") ?? FindBone("spine.001");

        CaptureBases();
        ApplyFighterStance();
    }

    private void LateUpdate()
    {
        if (fighter == null || !fighter.IsAlive) return;

        bool moving = fighter.IsMoving;
        phase += Time.deltaTime * (moving ? 8.5f : 2.2f);
        float stride = moving ? Mathf.Sin(phase) : 0f;
        float oppositeStride = -stride;

        SetRotation(leftThigh, leftThighBase, Quaternion.Euler(stride * 28f, 0f, 0f));
        SetRotation(rightThigh, rightThighBase, Quaternion.Euler(oppositeStride * 28f, 0f, 0f));
        SetRotation(leftShin, leftShinBase, Quaternion.Euler(Mathf.Max(0f, -stride) * 24f, 0f, 0f));
        SetRotation(rightShin, rightShinBase, Quaternion.Euler(Mathf.Max(0f, stride) * 24f, 0f, 0f));
        SetRotation(leftFoot, leftFootBase, Quaternion.Euler(-stride * 12f, 0f, 0f));
        SetRotation(rightFoot, rightFootBase, Quaternion.Euler(oppositeStride * -12f, 0f, 0f));

        // Lower the original T-pose arms into a readable boxing guard.
        float armSwing = moving ? stride * 10f : Mathf.Sin(phase) * 2f;
        SetRotation(leftUpperArm, leftUpperArmBase, Quaternion.Euler(0f, 15f + armSwing, -78f));
        SetRotation(rightUpperArm, rightUpperArmBase, Quaternion.Euler(0f, -15f - armSwing, 78f));
        SetRotation(leftForearm, leftForearmBase, Quaternion.Euler(-65f, 0f, 0f));
        SetRotation(rightForearm, rightForearmBase, Quaternion.Euler(-65f, 0f, 0f));

        if (fighter.IsPunching)
        {
            float punch = Mathf.Sin(Mathf.Clamp01(fighter.AttackProgress) * Mathf.PI);
            int attackType = fighter.AttackCycle % 6;
            bool rightStrike = fighter.AttackCycle % 2 == 0;
            ApplyAttackMotion(punch, attackType, rightStrike);
        }

        if (spine != null)
        {
            spine.localRotation = spineBase * Quaternion.Euler(0f, 0f, moving ? Mathf.Sin(phase) * 2f : 0f);
        }
    }

    private void CaptureBases()
    {
        leftUpperArmBase = GetRotation(leftUpperArm);
        rightUpperArmBase = GetRotation(rightUpperArm);
        leftForearmBase = GetRotation(leftForearm);
        rightForearmBase = GetRotation(rightForearm);
        leftThighBase = GetRotation(leftThigh);
        rightThighBase = GetRotation(rightThigh);
        leftShinBase = GetRotation(leftShin);
        rightShinBase = GetRotation(rightShin);
        leftFootBase = GetRotation(leftFoot);
        rightFootBase = GetRotation(rightFoot);
        spineBase = GetRotation(spine);
    }

    private void ApplyFighterStance()
    {
        SetRotation(leftUpperArm, leftUpperArmBase, Quaternion.Euler(0f, 15f, -78f));
        SetRotation(rightUpperArm, rightUpperArmBase, Quaternion.Euler(0f, -15f, 78f));
        SetRotation(leftForearm, leftForearmBase, Quaternion.Euler(-65f, 0f, 0f));
        SetRotation(rightForearm, rightForearmBase, Quaternion.Euler(-65f, 0f, 0f));
    }

    private void ApplyAttackMotion(float punch, int attackType, bool rightStrike)
    {
        Transform upperArm = rightStrike ? rightUpperArm : leftUpperArm;
        Transform forearm = rightStrike ? rightForearm : leftForearm;
        Quaternion upperBase = rightStrike ? rightUpperArmBase : leftUpperArmBase;
        Quaternion forearmBase = rightStrike ? rightForearmBase : leftForearmBase;
        float side = rightStrike ? -1f : 1f;

        switch (attackType)
        {
            case 0:
                // Direct: a compact straight punch from the guard.
                SetRotation(upperArm, upperBase, Quaternion.Euler(-55f * punch, side * 12f, side * 78f));
                SetRotation(forearm, forearmBase, Quaternion.Euler(-65f + 65f * punch, 0f, 0f));
                break;

            case 1:
                // Uppercut: elbow drops, then the fist drives upward.
                SetRotation(upperArm, upperBase, Quaternion.Euler(-88f * punch, side * 24f, side * 70f));
                SetRotation(forearm, forearmBase, Quaternion.Euler(-65f + 35f * punch, 0f, side * 12f));
                break;

            case 2:
                // Hook: shoulder rotates around the body while the forearm
                // stays bent, producing a visible sideways arc.
                SetRotation(upperArm, upperBase, Quaternion.Euler(-28f * punch, side * (18f + 48f * punch), side * 78f));
                SetRotation(forearm, forearmBase, Quaternion.Euler(-65f, side * 42f * punch, side * 20f * punch));
                break;

            case 3:
                // Knee strike: the leg folds sharply toward the opponent.
                SetRotation(leftThigh, leftThighBase, Quaternion.Euler(-58f * punch, 0f, 0f));
                SetRotation(leftShin, leftShinBase, Quaternion.Euler(72f * punch, 0f, 0f));
                SetRotation(rightUpperArm, rightUpperArmBase, Quaternion.Euler(-35f * punch, -18f, 72f));
                SetRotation(rightForearm, rightForearmBase, Quaternion.Euler(-45f + 45f * punch, 0f, 0f));
                break;

            case 4:
                // Roundhouse: hip turns first, then the lower leg snaps out.
                SetRotation(rightThigh, rightThighBase, Quaternion.Euler(-38f * punch, side * 48f * punch, side * 12f * punch));
                SetRotation(rightShin, rightShinBase, Quaternion.Euler(58f * punch, side * 18f * punch, 0f));
                SetRotation(spine, spineBase, Quaternion.Euler(0f, side * 16f * punch, 0f));
                break;

            default:
                // Spinning kick: rotate the torso and launch the leg in an
                // exaggerated cartoon arc without moving the root transform.
                SetRotation(spine, spineBase, Quaternion.Euler(0f, 135f * punch, 0f));
                SetRotation(leftThigh, leftThighBase, Quaternion.Euler(-48f * punch, side * 62f * punch, 0f));
                SetRotation(leftShin, leftShinBase, Quaternion.Euler(35f * punch, 0f, 0f));
                SetRotation(rightUpperArm, rightUpperArmBase, Quaternion.Euler(-38f * punch, -side * 35f * punch, 70f));
                SetRotation(rightForearm, rightForearmBase, Quaternion.Euler(-32f + 32f * punch, 0f, 0f));
                break;
        }
    }

    private Transform FindBone(string boneName)
    {
        foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == boneName) return candidate;
        }
        return null;
    }

    private static Quaternion GetRotation(Transform bone)
    {
        return bone == null ? Quaternion.identity : bone.localRotation;
    }

    private static void SetRotation(Transform bone, Quaternion baseRotation, Quaternion offset)
    {
        if (bone != null) bone.localRotation = baseRotation * offset;
    }
}
