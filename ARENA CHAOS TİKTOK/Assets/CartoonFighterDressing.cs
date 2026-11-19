using UnityEngine;

/// <summary>
/// Transforms realistic/generic humanoid models into vibrant, stylized cartoon fighters.
/// Adds chunky boxing gloves, champion belt, headband, and custom-colored trunks,
/// properly compensating for any parent armature scaling (e.g. 100x Blender export).
/// </summary>
public sealed class CartoonFighterDressing : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Colors")]
    [SerializeField] private Color primaryColor = Color.red;
    [SerializeField] private Color secondaryColor = Color.white;
    [SerializeField] private Color goldColor = new Color(1f, 0.82f, 0.12f);

    private bool isApplied;

    public void Apply(Color fighterColor, bool isPlayer = false)
    {
        if (isApplied) return;
        isApplied = true;

        primaryColor = fighterColor;
        secondaryColor = isPlayer ? Color.white : Color.Lerp(fighterColor, Color.white, 0.5f);

        Transform armature = transform.Find("Armature");
        if (armature == null) return;

        Transform hip = armature.Find("Hip");
        Transform spine = hip != null ? hip.Find("Spine") : null;
        Transform chest = spine != null ? spine.Find("Chest") : null;
        Transform neck = chest != null ? chest.Find("Neck") : null;
        Transform head = neck != null ? neck.Find("Head") : null;

        Transform handL = chest != null ? chest.Find("Arm_Upper.L/Arm_Lower.L/Hand.L") : null;
        Transform handR = chest != null ? chest.Find("Arm_Upper.R/Arm_Lower.R/Hand.R") : null;

        // 1. Headband on Head
        if (head != null)
        {
            AttachHeadband(head, primaryColor);
        }

        // 2. Chunky cartoon boxing gloves on both hands
        if (handL != null) AttachBoxingGlove(handL, "Left Cartoon Glove", primaryColor, true);
        if (handR != null) AttachBoxingGlove(handR, "Right Cartoon Glove", primaryColor, false);

        // 3. Champion belt on Hip
        if (hip != null)
        {
            AttachChampionBelt(hip, goldColor);
        }

        // 4. Stylize shorts and sportswear materials to match fighter's color
        TintFighterOutfit(fighterColor);
    }

    private void AttachBoxingGlove(Transform handBone, string gloveName, Color gloveColor, bool isLeft)
    {
        Transform existing = handBone.Find(gloveName);
        if (existing != null) Destroy(existing.gameObject);

        GameObject gloveRoot = new GameObject(gloveName);
        gloveRoot.transform.SetParent(handBone, false);
        gloveRoot.transform.localRotation = Quaternion.identity;

        Vector3 pScale = handBone.lossyScale;
        float sx = Mathf.Max(0.001f, pScale.x);
        float sy = Mathf.Max(0.001f, pScale.y);
        float sz = Mathf.Max(0.001f, pScale.z);

        // Position root slightly forward along the fist
        gloveRoot.transform.localPosition = new Vector3(
            (isLeft ? -0.015f : 0.015f) / sx,
            0.06f / sy,
            0.02f / sz
        );

        // Main puffy glove
        GameObject mainGlove = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mainGlove.name = "Puffy Glove Body";
        mainGlove.transform.SetParent(gloveRoot.transform, false);
        mainGlove.transform.localPosition = Vector3.zero;
        mainGlove.transform.localScale = new Vector3(0.24f / sx, 0.24f / sy, 0.26f / sz);
        Destroy(mainGlove.GetComponent<Collider>());
        SetGloveMaterial(mainGlove, gloveColor);

        // Glove thumb
        GameObject thumb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        thumb.name = "Glove Thumb";
        thumb.transform.SetParent(gloveRoot.transform, false);
        thumb.transform.localPosition = new Vector3(
            (isLeft ? -0.08f : 0.08f) / sx,
            -0.015f / sy,
            0.05f / sz
        );
        thumb.transform.localScale = new Vector3(0.10f / sx, 0.10f / sy, 0.12f / sz);
        Destroy(thumb.GetComponent<Collider>());
        SetGloveMaterial(thumb, gloveColor);

        // Wrist cuff / band
        GameObject cuff = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cuff.name = "Glove Wrist Cuff";
        cuff.transform.SetParent(gloveRoot.transform, false);
        cuff.transform.localPosition = new Vector3(0f, -0.06f / sy, 0f);
        cuff.transform.localScale = new Vector3(0.20f / sx, 0.02f / sy, 0.20f / sz);
        Destroy(cuff.GetComponent<Collider>());
        SetGloveMaterial(cuff, Color.white);
    }

    private void AttachHeadband(Transform headBone, Color bandColor)
    {
        Transform existing = headBone.Find("Fighter Headband");
        if (existing != null) Destroy(existing.gameObject);

        GameObject headband = new GameObject("Fighter Headband");
        headband.transform.SetParent(headBone, false);

        Vector3 pScale = headBone.lossyScale;
        float sx = Mathf.Max(0.001f, pScale.x);
        float sy = Mathf.Max(0.001f, pScale.y);
        float sz = Mathf.Max(0.001f, pScale.z);

        headband.transform.localPosition = new Vector3(0f, 0.08f / sy, 0.015f / sz);
        headband.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

        // Forehead band
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Band Ring";
        band.transform.SetParent(headband.transform, false);
        band.transform.localPosition = Vector3.zero;
        band.transform.localScale = new Vector3(0.25f / sx, 0.025f / sy, 0.28f / sz);
        Destroy(band.GetComponent<Collider>());
        SetGloveMaterial(band, bandColor);

        // Hanging ribbon tails at the back
        GameObject tail1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail1.name = "Band Tail L";
        tail1.transform.SetParent(headband.transform, false);
        tail1.transform.localPosition = new Vector3(-0.035f / sx, -0.06f / sy, -0.14f / sz);
        tail1.transform.localRotation = Quaternion.Euler(20f, -15f, -10f);
        tail1.transform.localScale = new Vector3(0.03f / sx, 0.12f / sy, 0.012f / sz);
        Destroy(tail1.GetComponent<Collider>());
        SetGloveMaterial(tail1, bandColor);

        GameObject tail2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail2.name = "Band Tail R";
        tail2.transform.SetParent(headband.transform, false);
        tail2.transform.localPosition = new Vector3(0.035f / sx, -0.07f / sy, -0.14f / sz);
        tail2.transform.localRotation = Quaternion.Euler(22f, 15f, 10f);
        tail2.transform.localScale = new Vector3(0.03f / sx, 0.14f / sy, 0.012f / sz);
        Destroy(tail2.GetComponent<Collider>());
        SetGloveMaterial(tail2, bandColor);
    }

    private void AttachChampionBelt(Transform hipBone, Color gold)
    {
        Transform existing = hipBone.Find("Champion Belt");
        if (existing != null) Destroy(existing.gameObject);

        GameObject beltRoot = new GameObject("Champion Belt");
        beltRoot.transform.SetParent(hipBone, false);

        Vector3 pScale = hipBone.lossyScale;
        float sx = Mathf.Max(0.001f, pScale.x);
        float sy = Mathf.Max(0.001f, pScale.y);
        float sz = Mathf.Max(0.001f, pScale.z);

        beltRoot.transform.localPosition = new Vector3(0f, 0.02f / sy, 0f);

        // Leather waist band
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Belt Leather Band";
        band.transform.SetParent(beltRoot.transform, false);
        band.transform.localPosition = Vector3.zero;
        band.transform.localScale = new Vector3(0.40f / sx, 0.045f / sy, 0.32f / sz);
        Destroy(band.GetComponent<Collider>());
        SetGloveMaterial(band, new Color(0.12f, 0.12f, 0.14f));

        // Big Gold Buckle in front
        GameObject buckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buckle.name = "Gold Buckle Plate";
        buckle.transform.SetParent(beltRoot.transform, false);
        buckle.transform.localPosition = new Vector3(0f, 0.005f / sy, 0.17f / sz);
        buckle.transform.localScale = new Vector3(0.14f / sx, 0.09f / sy, 0.035f / sz);
        Destroy(buckle.GetComponent<Collider>());
        SetGloveMaterial(buckle, gold, true);
    }

    private void TintFighterOutfit(Color fighterColor)
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (r.gameObject.name.Contains("Underwear"))
            {
                Material trunkMat = CreateToonMaterial(fighterColor);
                r.material = trunkMat;
            }
            else if (r.gameObject.name.Contains("Top_Sport"))
            {
                Color topColor = Color.Lerp(fighterColor, Color.black, 0.25f);
                Material topMat = CreateToonMaterial(topColor);
                r.material = topMat;
            }
        }
    }

    private static void SetGloveMaterial(GameObject target, Color color, bool isGold = false)
    {
        Renderer r = target.GetComponent<Renderer>();
        if (r == null) return;
        r.material = CreateToonMaterial(color, isGold);
    }

    private static Material CreateToonMaterial(Color color, bool isGold = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader)
        {
            color = color
        };
        mat.SetColor(BaseColorId, color);
        mat.SetColor(ColorId, color);

        if (isGold)
        {
            mat.SetFloat(MetallicId, 0.95f);
            mat.SetFloat(SmoothnessId, 0.88f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(EmissionColorId, color * 1.5f);
        }
        else
        {
            mat.SetFloat(MetallicId, 0.15f);
            mat.SetFloat(SmoothnessId, 0.65f);
        }
        return mat;
    }
}