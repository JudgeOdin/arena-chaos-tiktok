using UnityEngine;

/// <summary>
/// Visual-only locomotion for the first Male Athlate test character.
/// It reads the fighter root displacement and animates only the imported
/// visual bones. It never moves the fighter root or affects physics.
/// </summary>
public class VisualMotionDriver : MonoBehaviour
{
    [Header("Gait")]
    [SerializeField] private float referenceSpeed = 2.8f;
    [SerializeField] private float cycleSpeed = 7.5f;
    [SerializeField] private float upperLegSwing = 28f;
    [SerializeField] private float kneeBend = 24f;
    [SerializeField] private float footCompensation = 18f;

    private Transform leftArm;
    private Transform rightArm;
    private Transform leftUpperLeg;
    private Transform rightUpperLeg;
    private Transform leftLowerLeg;
    private Transform rightLowerLeg;
    private Transform leftFoot;
    private Transform rightFoot;

    private Quaternion leftArmBase;
    private Quaternion rightArmBase;
    private Quaternion leftUpperLegBase;
    private Quaternion rightUpperLegBase;
    private Quaternion leftLowerLegBase;
    private Quaternion rightLowerLegBase;
    private Quaternion leftFootBase;
    private Quaternion rightFootBase;

    private Vector3 baseLocalPosition;
    private Vector3 lastRootPosition;
    private float phase;
    private float motionBlend;
    private bool isMoving;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        lastRootPosition = transform.parent != null ? transform.parent.position : transform.position;

        leftArm = FindChild(transform, "Arm_Upper.L");
        rightArm = FindChild(transform, "Arm_Upper.R");
        leftUpperLeg = FindChild(transform, "Leg_Upper.L");
        rightUpperLeg = FindChild(transform, "Leg_Upper.R");
        leftLowerLeg = FindChild(transform, "Leg_Lower.L");
        rightLowerLeg = FindChild(transform, "Leg_Lower.R");
        leftFoot = FindChild(transform, "Foot.L");
        rightFoot = FindChild(transform, "Foot.R");

        CaptureCurrentPose();
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    private void LateUpdate()
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Transform fighterRoot = transform.parent;
        Vector3 currentRootPosition = fighterRoot != null ? fighterRoot.position : transform.position;
        float actualSpeed = Vector3.Distance(currentRootPosition, lastRootPosition) / deltaTime;
        lastRootPosition = currentRootPosition;

        // ArenaFighter uses Rigidbody.MovePosition from Update. Depending on
        // the physics timing, LateUpdate can run before that MovePosition is
        // reflected in the root Transform, which would otherwise report zero
        // speed and keep the gait permanently disabled. When the fighter has
        // an active movement target but the physics step has not published its
        // displacement yet, use the configured reference speed as a visual
        // estimate. The root and Rigidbody are never modified here.
        float visualSpeed = actualSpeed;
        if (isMoving && visualSpeed < 0.05f)
        {
            visualSpeed = referenceSpeed;
        }

        float speed01 = Mathf.Clamp01(visualSpeed / Mathf.Max(referenceSpeed, 0.01f));
        float targetBlend = isMoving ? speed01 : 0f;
        motionBlend = Mathf.MoveTowards(motionBlend, targetBlend, deltaTime * 8f);

        // Let the imported Idle controller provide the resting pose. Capture it
        // only after the procedural gait has fully faded out.
        if (!isMoving && motionBlend <= 0.001f)
        {
            CaptureCurrentPose();
            return;
        }

        if (motionBlend <= 0.001f)
        {
            return;
        }

        float currentCycleSpeed = Mathf.Lerp(2.5f, cycleSpeed, speed01);
        phase += deltaTime * currentCycleSpeed;
        float stride = Mathf.Sin(phase);
        float oppositeStride = -stride;
        float amplitude = upperLegSwing * Mathf.Clamp01(motionBlend);

        // Male Athlate's mirrored leg bones use local Z for the visible
        // forward/back swing. The same local phase lets the mirrored geometry
        // produce the alternating left/right step without moving the model root.
        ApplyRotation(leftUpperLeg, leftUpperLegBase, stride * amplitude, Vector3.forward, 14f, deltaTime);
        ApplyRotation(rightUpperLeg, rightUpperLegBase, stride * amplitude, Vector3.forward, 14f, deltaTime);

        // Bend the knee mostly during the backward part of each stride.
        float kneePhase = Mathf.Max(0f, stride);
        float leftKnee = kneePhase * kneeBend * motionBlend;
        float rightKnee = kneePhase * kneeBend * motionBlend;
        ApplyRotation(leftLowerLeg, leftLowerLegBase, leftKnee, Vector3.forward, 10f, deltaTime);
        ApplyRotation(rightLowerLeg, rightLowerLegBase, rightKnee, Vector3.forward, 10f, deltaTime);

        // Keep the feet visually closer to level while the legs swing.
        ApplyRotation(leftFoot, leftFootBase, -leftKnee * footCompensation / Mathf.Max(kneeBend, 0.01f), Vector3.forward, 10f, deltaTime);
        ApplyRotation(rightFoot, rightFootBase, -rightKnee * footCompensation / Mathf.Max(kneeBend, 0.01f), Vector3.forward, 10f, deltaTime);

        // The arm bones use a different local axis from the legs. Local Z
        // folds the arms inward/outward on this model; local X produces the
        // forward/back swing. For this armature the arms need opposite local
        // phases to produce the alternating world-space walk swing.
        ApplyRotation(leftArm, leftArmBase, stride * amplitude * 0.65f, Vector3.right, 12f, deltaTime);
        ApplyRotation(rightArm, rightArmBase, oppositeStride * amplitude * 0.65f, Vector3.right, 12f, deltaTime);

        // Do not translate the visual root or pelvis. The Rigidbody owns
        // world movement; moving this child would make the model slide or
        // appear to teleport relative to its physics capsule.
    }

    private void CaptureCurrentPose()
    {
        if (leftArm != null) leftArmBase = leftArm.localRotation;
        if (rightArm != null) rightArmBase = rightArm.localRotation;
        if (leftUpperLeg != null) leftUpperLegBase = leftUpperLeg.localRotation;
        if (rightUpperLeg != null) rightUpperLegBase = rightUpperLeg.localRotation;
        if (leftLowerLeg != null) leftLowerLegBase = leftLowerLeg.localRotation;
        if (rightLowerLeg != null) rightLowerLegBase = rightLowerLeg.localRotation;
        if (leftFoot != null) leftFootBase = leftFoot.localRotation;
        if (rightFoot != null) rightFootBase = rightFoot.localRotation;
        baseLocalPosition = transform.localPosition;
    }

    private static void ApplyRotation(Transform bone, Quaternion baseRotation, float angle, Vector3 localAxis, float smoothing, float deltaTime)
    {
        if (bone == null) return;
        Quaternion target = baseRotation * Quaternion.AngleAxis(angle, localAxis);
        bone.localRotation = Quaternion.Slerp(bone.localRotation, target, Mathf.Clamp01(deltaTime * smoothing));
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
