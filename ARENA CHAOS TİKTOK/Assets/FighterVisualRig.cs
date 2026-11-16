using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight, code-built fighter rig for generated arena fighters.
/// It is visual-only: it never owns a collider, Rigidbody or gameplay position.
/// </summary>
public sealed class FighterVisualRig : MonoBehaviour
{
    private static readonly Dictionary<int, Material> materialCache = new Dictionary<int, Material>();

    private ArenaFighter fighter;
    private Transform rigRoot;
    private Transform head;
    private Transform leftUpperArm;
    private Transform rightUpperArm;
    private Transform leftLowerArm;
    private Transform rightLowerArm;
    private Transform leftUpperLeg;
    private Transform rightUpperLeg;
    private Transform leftLowerLeg;
    private Transform rightLowerLeg;
    private Transform leftFoot;
    private Transform rightFoot;
    private Quaternion leftUpperArmBase;
    private Quaternion rightUpperArmBase;
    private Quaternion leftLowerArmBase;
    private Quaternion rightLowerArmBase;
    private Quaternion leftUpperLegBase;
    private Quaternion rightUpperLegBase;
    private Quaternion leftLowerLegBase;
    private Quaternion rightLowerLegBase;
    private Quaternion leftFootBase;
    private Quaternion rightFootBase;
    private Vector3 headBasePosition;
    private float phase;
    private bool visible = true;

    public void Build(Color fighterColor, int style)
    {
        fighter = GetComponent<ArenaFighter>();
        rigRoot = new GameObject("Procedural Fighter Rig").transform;
        rigRoot.SetParent(transform, false);
        rigRoot.localPosition = Vector3.up * 0.7f;

        Color darkColor = Color.Lerp(fighterColor, Color.black, 0.38f);
        Color lightColor = Color.Lerp(fighterColor, Color.white, 0.35f);
        Color skinColor = new Color(1f, 0.63f, 0.42f);

        Transform hips = CreateBone("Hips", rigRoot, new Vector3(0f, -0.2f, 0f));
        Transform chest = CreateBone("Chest", hips, new Vector3(0f, 0.42f, 0f));
        Transform neck = CreateBone("Neck", chest, new Vector3(0f, 0.48f, 0f));
        head = CreateBone("Head", neck, new Vector3(0f, 0.25f, 0f));
        CreatePart(PrimitiveType.Sphere, "Head Mesh", head, Vector3.zero, new Vector3(0.72f, 0.78f, 0.72f), fighterColor);
        CreatePart(PrimitiveType.Sphere, "Left Eye", head, new Vector3(-0.14f, 0.03f, 0.32f), new Vector3(0.1f, 0.13f, 0.06f), Color.black);
        CreatePart(PrimitiveType.Sphere, "Right Eye", head, new Vector3(0.14f, 0.03f, 0.32f), new Vector3(0.1f, 0.13f, 0.06f), Color.black);
        CreatePart(PrimitiveType.Cube, "Chest Armor", chest, new Vector3(0f, 0.02f, 0f), new Vector3(0.72f, 0.65f, 0.46f), fighterColor);
        CreatePart(PrimitiveType.Cube, "Belt", hips, new Vector3(0f, 0.02f, 0f), new Vector3(0.78f, 0.14f, 0.5f), darkColor);

        leftUpperArm = CreateBone("Left Upper Arm", chest, new Vector3(-0.46f, 0.24f, 0f));
        rightUpperArm = CreateBone("Right Upper Arm", chest, new Vector3(0.46f, 0.24f, 0f));
        CreatePart(PrimitiveType.Capsule, "Left Bicep", leftUpperArm, new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.27f, 0.16f), fighterColor);
        CreatePart(PrimitiveType.Capsule, "Right Bicep", rightUpperArm, new Vector3(0f, -0.22f, 0f), new Vector3(0.16f, 0.27f, 0.16f), fighterColor);
        leftLowerArm = CreateBone("Left Forearm", leftUpperArm, new Vector3(0f, -0.42f, 0f));
        rightLowerArm = CreateBone("Right Forearm", rightUpperArm, new Vector3(0f, -0.42f, 0f));
        CreatePart(PrimitiveType.Capsule, "Left Forearm Mesh", leftLowerArm, new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.22f, 0.14f), lightColor);
        CreatePart(PrimitiveType.Capsule, "Right Forearm Mesh", rightLowerArm, new Vector3(0f, -0.18f, 0f), new Vector3(0.14f, 0.22f, 0.14f), lightColor);
        CreatePart(PrimitiveType.Sphere, "Left Boxing Glove", leftLowerArm, new Vector3(0f, -0.43f, 0.04f), new Vector3(0.26f, 0.26f, 0.26f), lightColor);
        CreatePart(PrimitiveType.Sphere, "Right Boxing Glove", rightLowerArm, new Vector3(0f, -0.43f, 0.04f), new Vector3(0.26f, 0.26f, 0.26f), lightColor);

        leftUpperLeg = CreateBone("Left Upper Leg", hips, new Vector3(-0.2f, -0.12f, 0f));
        rightUpperLeg = CreateBone("Right Upper Leg", hips, new Vector3(0.2f, -0.12f, 0f));
        CreatePart(PrimitiveType.Capsule, "Left Thigh", leftUpperLeg, new Vector3(0f, -0.25f, 0f), new Vector3(0.2f, 0.3f, 0.2f), darkColor);
        CreatePart(PrimitiveType.Capsule, "Right Thigh", rightUpperLeg, new Vector3(0f, -0.25f, 0f), new Vector3(0.2f, 0.3f, 0.2f), darkColor);
        leftLowerLeg = CreateBone("Left Lower Leg", leftUpperLeg, new Vector3(0f, -0.5f, 0f));
        rightLowerLeg = CreateBone("Right Lower Leg", rightUpperLeg, new Vector3(0f, -0.5f, 0f));
        CreatePart(PrimitiveType.Capsule, "Left Shin", leftLowerLeg, new Vector3(0f, -0.23f, 0f), new Vector3(0.16f, 0.27f, 0.16f), fighterColor);
        CreatePart(PrimitiveType.Capsule, "Right Shin", rightLowerLeg, new Vector3(0f, -0.23f, 0f), new Vector3(0.16f, 0.27f, 0.16f), fighterColor);
        leftFoot = CreateBone("Left Foot", leftLowerLeg, new Vector3(0f, -0.5f, 0f));
        rightFoot = CreateBone("Right Foot", rightLowerLeg, new Vector3(0f, -0.5f, 0f));
        CreatePart(PrimitiveType.Sphere, "Left Boot", leftFoot, new Vector3(0f, -0.05f, 0.13f), new Vector3(0.3f, 0.18f, 0.43f), Color.black);
        CreatePart(PrimitiveType.Sphere, "Right Boot", rightFoot, new Vector3(0f, -0.05f, 0.13f), new Vector3(0.3f, 0.18f, 0.43f), Color.black);

        CreateStyleAccessory(style, head, fighterColor, darkColor, skinColor);

        leftUpperArmBase = leftUpperArm.localRotation;
        rightUpperArmBase = rightUpperArm.localRotation;
        leftLowerArmBase = leftLowerArm.localRotation;
        rightLowerArmBase = rightLowerArm.localRotation;
        leftUpperLegBase = leftUpperLeg.localRotation;
        rightUpperLegBase = rightUpperLeg.localRotation;
        leftLowerLegBase = leftLowerLeg.localRotation;
        rightLowerLegBase = rightLowerLeg.localRotation;
        leftFootBase = leftFoot.localRotation;
        rightFootBase = rightFoot.localRotation;
        headBasePosition = head.localPosition;
    }

    public void SetVisible(bool isVisible)
    {
        visible = isVisible;
        if (rigRoot != null) rigRoot.gameObject.SetActive(isVisible);
    }

    private void LateUpdate()
    {
        if (!visible || fighter == null || !fighter.IsAlive) return;

        bool moving = fighter.IsMoving;
        phase += Time.deltaTime * (moving ? 8f : 2.2f);
        float stride = moving ? Mathf.Sin(phase) * 28f : Mathf.Sin(phase) * 4f;
        float oppositeStride = -stride;
        float knee = moving ? Mathf.Max(0f, Mathf.Sin(phase)) * 20f : 0f;

        leftUpperLeg.localRotation = leftUpperLegBase * Quaternion.Euler(stride, 0f, 0f);
        rightUpperLeg.localRotation = rightUpperLegBase * Quaternion.Euler(oppositeStride, 0f, 0f);
        leftLowerLeg.localRotation = leftLowerLegBase * Quaternion.Euler(knee, 0f, 0f);
        rightLowerLeg.localRotation = rightLowerLegBase * Quaternion.Euler(Mathf.Max(0f, Mathf.Sin(phase + Mathf.PI)) * 20f, 0f, 0f);
        leftFoot.localRotation = leftFootBase * Quaternion.Euler(-stride * 0.35f, 0f, 0f);
        rightFoot.localRotation = rightFootBase * Quaternion.Euler(-oppositeStride * 0.35f, 0f, 0f);

        if (fighter.IsPunching)
        {
            bool rightStrike = fighter.AttackCycle % 2 == 0;
            float punch = Mathf.Sin(Mathf.Clamp01(fighter.AttackProgress) * Mathf.PI) * 105f;
            leftUpperArm.localRotation = leftUpperArmBase * Quaternion.Euler(rightStrike ? 20f : -punch, 0f, 0f);
            rightUpperArm.localRotation = rightUpperArmBase * Quaternion.Euler(rightStrike ? -punch : 20f, 0f, 0f);
            leftLowerArm.localRotation = leftLowerArmBase * Quaternion.Euler(rightStrike ? -25f : -punch * 0.35f, 0f, 0f);
            rightLowerArm.localRotation = rightLowerArmBase * Quaternion.Euler(rightStrike ? -punch * 0.35f : -25f, 0f, 0f);

            if (fighter.IsHeadbutting)
            {
                head.localPosition = headBasePosition + Vector3.forward * (0.16f * Mathf.Sin(Mathf.Clamp01(fighter.AttackProgress) * Mathf.PI));
            }
        }
        else
        {
            float guard = moving ? Mathf.Sin(phase) * 8f : 0f;
            leftUpperArm.localRotation = leftUpperArmBase * Quaternion.Euler(guard, 0f, 0f);
            rightUpperArm.localRotation = rightUpperArmBase * Quaternion.Euler(-guard, 0f, 0f);
            leftLowerArm.localRotation = leftLowerArmBase;
            rightLowerArm.localRotation = rightLowerArmBase;
            head.localPosition = Vector3.Lerp(head.localPosition, headBasePosition, Time.deltaTime * 12f);
        }
    }

    private Transform CreateBone(string name, Transform parent, Vector3 localPosition)
    {
        GameObject bone = new GameObject(name);
        bone.transform.SetParent(parent, false);
        bone.transform.localPosition = localPosition;
        return bone.transform;
    }

    private void CreatePart(PrimitiveType primitiveType, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = GetMaterial(color);
        Destroy(part.GetComponent<Collider>());
    }

    private void CreateStyleAccessory(int style, Transform headTransform, Color color, Color darkColor, Color skinColor)
    {
        if (style % 4 == 0)
        {
            CreatePart(PrimitiveType.Cube, "Champion Headband", headTransform, new Vector3(0f, 0.13f, 0f), new Vector3(0.78f, 0.12f, 0.78f), Color.white);
        }
        else if (style % 4 == 1)
        {
            CreatePart(PrimitiveType.Sphere, "Mohawk", headTransform, new Vector3(0f, 0.4f, -0.02f), new Vector3(0.18f, 0.35f, 0.5f), darkColor);
        }
        else if (style % 4 == 2)
        {
            CreatePart(PrimitiveType.Cube, "Golden Crown", headTransform, new Vector3(0f, 0.42f, 0f), new Vector3(0.52f, 0.16f, 0.42f), new Color(1f, 0.72f, 0.05f));
        }
        else
        {
            CreatePart(PrimitiveType.Sphere, "Bandana Knot", headTransform, new Vector3(0f, 0.1f, -0.35f), new Vector3(0.22f, 0.22f, 0.22f), skinColor);
        }
    }

    private static Material GetMaterial(Color color)
    {
        Color32 color32 = color;
        int key = color32.r | (color32.g << 8) | (color32.b << 16) | (color32.a << 24);
        if (materialCache.TryGetValue(key, out Material cached)) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        materialCache[key] = material;
        return material;
    }
}
