using UnityEngine;

/// <summary>
/// Visual-only fallback locomotion for characters that only ship with an Idle clip.
/// It never moves the fighter root or affects physics.
/// </summary>
public class VisualMotionDriver : MonoBehaviour
{
    private Transform leftArm;
    private Transform rightArm;
    private Transform leftLeg;
    private Transform rightLeg;
    private Quaternion leftArmBase;
    private Quaternion rightArmBase;
    private Quaternion leftLegBase;
    private Quaternion rightLegBase;
    private Vector3 baseLocalPosition;
    private bool isMoving;
    private float phase;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        leftArm = FindChild(transform, "Arm_Upper.L");
        rightArm = FindChild(transform, "Arm_Upper.R");
        leftLeg = FindChild(transform, "Leg_Upper.L");
        rightLeg = FindChild(transform, "Leg_Upper.R");

        if (leftArm != null) leftArmBase = leftArm.localRotation;
        if (rightArm != null) rightArmBase = rightArm.localRotation;
        if (leftLeg != null) leftLegBase = leftLeg.localRotation;
        if (rightLeg != null) rightLegBase = rightLeg.localRotation;
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    private void LateUpdate()
    {
        float blend = Mathf.Clamp01(Time.deltaTime * 12f);
        if (isMoving)
        {
            phase += Time.deltaTime * 8f;
            float swing = Mathf.Sin(phase) * 28f;
            float oppositeSwing = -swing;
            transform.localPosition = baseLocalPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * 0.035f);
            ApplyRotation(leftArm, leftArmBase, oppositeSwing, blend);
            ApplyRotation(rightArm, rightArmBase, swing, blend);
            ApplyRotation(leftLeg, leftLegBase, swing, blend);
            ApplyRotation(rightLeg, rightLegBase, oppositeSwing, blend);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, baseLocalPosition, blend);
            ApplyRotation(leftArm, leftArmBase, 0f, blend);
            ApplyRotation(rightArm, rightArmBase, 0f, blend);
            ApplyRotation(leftLeg, leftLegBase, 0f, blend);
            ApplyRotation(rightLeg, rightLegBase, 0f, blend);
        }
    }

    private static void ApplyRotation(Transform bone, Quaternion baseRotation, float angle, float blend)
    {
        if (bone == null) return;
        bone.localRotation = Quaternion.Slerp(bone.localRotation, baseRotation * Quaternion.Euler(angle, 0f, 0f), blend);
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform result = FindChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}
