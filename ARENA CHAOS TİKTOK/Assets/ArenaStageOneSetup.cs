// ArenaStageOneSetup.cs - cleaned up version
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates the first playable arena dressing at runtime without replacing the
/// objects already present in the scene.
/// </summary>
public class ArenaStageOneSetup : MonoBehaviour
{
    private const string ImportedPlayerVisualPath = "Assets/CharCrafter – Free Preset Characters Pack (Vol. 1)/Prefabs/Male Athlate.prefab";
    private const string ImportedPlayerControllerPath = "Assets/MaleAthlate.controller";

    [Header("Arena")]
    [SerializeField] private float arenaRadius = 8f;
    [SerializeField] private float wallHeight = 1.5f;
    [SerializeField] private float wallThickness = 0.35f;

    [Header("Camera")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 12f, -16f);
    [SerializeField] private Vector3 cameraEulerAngles = new Vector3(35f, 0f, 0f);

    [Header("First Fighter Visual Test")]
    [SerializeField] private UnityEngine.Object firstFighterVisualAsset;
    [SerializeField] private RuntimeAnimatorController firstFighterAnimatorController;
    [SerializeField] private Vector3 firstFighterVisualLocalPosition = new Vector3(0f, -1f, 0f);
    [SerializeField] private Vector3 firstFighterVisualLocalScale = Vector3.one;

    private void Awake()
    {
        // Unity's default Plane is 10x10 world units. Match it to the arena.
        transform.localScale = new Vector3(arenaRadius / 5f, 1f, arenaRadius / 5f);
        BuildArenaWalls();
        ConfigureCamera();
        EnsurePlayerMovement();
        BattleArenaManager battleManager = GetComponent<BattleArenaManager>();
        if (battleManager == null)
        {
            battleManager = gameObject.AddComponent<BattleArenaManager>();
        }

        if (ArenaContent.Load() == null) ResolveFirstFighterVisualAsset();
        battleManager.ConfigureFirstFighterVisual(firstFighterVisualAsset, firstFighterAnimatorController,
            firstFighterVisualLocalPosition, firstFighterVisualLocalScale);

        if (GetComponent<GiftEventSimulator>() == null)
        {
            gameObject.AddComponent<GiftEventSimulator>();
        }
        if (GetComponent<ArenaPresentationDirector>() == null)
        {
            gameObject.AddComponent<ArenaPresentationDirector>();
        }
        if (GetComponent<ArenaBroadcastHUD>() == null) gameObject.AddComponent<ArenaBroadcastHUD>();
        if (GetComponent<ArenaAudioEffects>() == null) gameObject.AddComponent<ArenaAudioEffects>();
    }

    // New method to upgrade arena size and rebuild walls.
    public void UpgradeArena(float newRadius)
    {
        if (newRadius <= 0f)
        {
            Debug.LogWarning("UpgradeArena: newRadius must be positive.");
            return;
        }
        arenaRadius = newRadius;
        // Update ground scaling.
        transform.localScale = new Vector3(arenaRadius / 5f, 1f, arenaRadius / 5f);
        // Rebuild walls to match new size.
        BuildArenaWalls();
        // Adjust camera distance if needed.
        ConfigureCamera();
        Debug.Log($"Arena upgraded to radius {arenaRadius}");
    }

    private void ResolveFirstFighterVisualAsset()
    {
        if (firstFighterVisualAsset != null && !IsLegacyNormalgonReference() && !IsLegacyEEJANAIReference())
        {
            return;
        }
#if UNITY_EDITOR
        // Resolve the model asset by path.
        GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedPlayerVisualPath);
        if (importedModel == null)
        {
            importedModel = AssetDatabase.LoadMainAssetAtPath(ImportedPlayerVisualPath) as GameObject;
        }
        if (importedModel != null)
        {
            firstFighterVisualAsset = importedModel;
            firstFighterAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ImportedPlayerControllerPath);
            Debug.Log("ArenaStageOneSetup: Male Athlate model and animator automatically linked.");
        }
        else
        {
            Debug.LogWarning("ArenaStageOneSetup: Male Athlate model not found at " + ImportedPlayerVisualPath);
        }
#endif
    }

    private bool IsLegacyEEJANAIReference()
    {
#if UNITY_EDITOR
        if (firstFighterVisualAsset == null) return false;
        string path = AssetDatabase.GetAssetPath(firstFighterVisualAsset);
        return path.Contains("Assets/EEJANAI_Team/");
#else
        return false;
#endif
    }

    private bool IsLegacyNormalgonReference()
    {
#if UNITY_EDITOR
        if (firstFighterVisualAsset == null) return false;
        string path = AssetDatabase.GetAssetPath(firstFighterVisualAsset);
        return path.Contains("Assets/Normalgon/");
#else
        return false;
#endif
    }

    private void BuildArenaWalls()
    {
        if (ArenaContent.Load() != null)
        {
            var previous = GameObject.Find("Arena Walls (Stage 1)");
            if (previous != null) previous.SetActive(false);
            previous = GameObject.Find("ArenaVisuals");
            if (previous != null) previous.SetActive(false);
            KayKitArenaEnvironment.Build(arenaRadius, GetComponent<Renderer>());
            return;
        }
        GameObject legacyWalls = GameObject.Find("Arena Walls (Stage 1)");
        if (legacyWalls != null)
        {
            Destroy(legacyWalls);
        }

        GameObject existingVisuals = GameObject.Find("ArenaVisuals");
        if (existingVisuals != null)
        {
            Destroy(existingVisuals);
        }

        GameObject arenaVisuals = new GameObject("ArenaVisuals");
        Transform root = arenaVisuals.transform;
        Color metal = new Color(0.025f, 0.035f, 0.055f);
        Color panel = new Color(0.055f, 0.075f, 0.11f);
        Color redNeon = new Color(1f, 0.015f, 0.035f);
        Color blueNeon = new Color(0.015f, 0.35f, 1f);

        CreateWallSet(root, "North Wall", new Vector3(0f, wallHeight * 0.5f, arenaRadius),
            new Vector3(arenaRadius * 2f, wallHeight, wallThickness), Vector3.back, blueNeon, true, metal, panel);
        CreateWallSet(root, "South Wall", new Vector3(0f, wallHeight * 0.5f, -arenaRadius),
            new Vector3(arenaRadius * 2f, wallHeight, wallThickness), Vector3.forward, redNeon, true, metal, panel);
        CreateWallSet(root, "East Wall", new Vector3(arenaRadius, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, arenaRadius * 2f), Vector3.left, blueNeon, false, metal, panel);
        CreateWallSet(root, "West Wall", new Vector3(-arenaRadius, wallHeight * 0.5f, 0f),
            new Vector3(wallThickness, wallHeight, arenaRadius * 2f), Vector3.right, redNeon, false, metal, panel);

        CreateNeonTower(root, "Tower NE", new Vector3(arenaRadius - 0.35f, 0f, arenaRadius - 0.35f), redNeon, blueNeon);
        CreateNeonTower(root, "Tower NW", new Vector3(-arenaRadius + 0.35f, 0f, arenaRadius - 0.35f), blueNeon, redNeon);
        CreateNeonTower(root, "Tower SE", new Vector3(arenaRadius - 0.35f, 0f, -arenaRadius + 0.35f), blueNeon, redNeon);
        CreateNeonTower(root, "Tower SW", new Vector3(-arenaRadius + 0.35f, 0f, -arenaRadius + 0.35f), redNeon, blueNeon);
    }

    private void CreateWallSet(Transform parent, string wallName, Vector3 center, Vector3 baseScale, Vector3 inward, Color accent, bool horizontal, Color metal, Color panel)
    {
        // Leave a real 3.2m opening at each side entrance.
        if (!horizontal && baseScale.z > arenaRadius)
        {
            float length = arenaRadius - 1.6f;
            Vector3 size = new Vector3(baseScale.x, baseScale.y, length);
            Vector3 offset = Vector3.forward * (1.6f + length * 0.5f);
            CreateWallSet(parent, wallName + " A", center + offset, size, inward, accent, false, metal, panel);
            CreateWallSet(parent, wallName + " B", center - offset, size, inward, accent, false, metal, panel);
            // A permanent invisible barrier prevents combatants escaping;
            // entrance actors temporarily disable collision until inside.
            GameObject barrier = new GameObject(wallName + " Gate Boundary");
            barrier.transform.SetParent(parent, false);
            barrier.transform.position = center;
            barrier.AddComponent<BoxCollider>().size = new Vector3(baseScale.x, 3.6f, 3.2f);
            return;
        }
        Material metalMaterial = CreateMaterial(metal);
        Material panelMaterial = CreateMaterial(panel);
        Material neonMaterial = CreateMaterial(accent, true);

        CreateBox(parent, wallName + " - Physical Boundary", center, baseScale, metalMaterial, true);

        Vector3 topPos = center + Vector3.up * (wallHeight * 0.5f + 0.16f);
        Vector3 topScale = horizontal ? new Vector3(baseScale.x, 0.22f, 0.58f) : new Vector3(0.58f, 0.22f, baseScale.z);
        CreateBox(parent, wallName + " - Top Beam", topPos, topScale, metalMaterial, false);

        Vector3 panelPos = center + inward * 0.22f + Vector3.up * 0.84f;
        Vector3 panelScale = horizontal ? new Vector3(baseScale.x * 0.78f, 0.72f, 0.08f) : new Vector3(0.08f, 0.72f, baseScale.z * 0.78f);
        CreateBox(parent, wallName + " - Tech Panels", panelPos, panelScale, panelMaterial, false);

        Vector3 neonPos = center + inward * 0.3f + Vector3.up * 0.38f;
        Vector3 neonScale = horizontal ? new Vector3(baseScale.x * 0.94f, 0.07f, 0.08f) : new Vector3(0.08f, 0.07f, baseScale.z * 0.94f);
        CreateBox(parent, wallName + " - Neon Strip", neonPos, neonScale, neonMaterial, false);

        if (horizontal)
        {
            CreateBox(parent, wallName + " - Panel Divider A", center + inward * 0.31f + new Vector3(-baseScale.x * 0.24f, 0.84f, 0f), new Vector3(0.035f, 0.78f, 0.1f), neonMaterial, false);
            CreateBox(parent, wallName + " - Panel Divider B", center + inward * 0.31f + new Vector3(baseScale.x * 0.24f, 0.84f, 0f), new Vector3(0.035f, 0.78f, 0.1f), neonMaterial, false);
        }
        else
        {
            CreateBox(parent, wallName + " - Panel Divider A", center + inward * 0.31f + new Vector3(0f, 0.84f, -baseScale.z * 0.24f), new Vector3(0.1f, 0.78f, 0.035f), neonMaterial, false);
            CreateBox(parent, wallName + " - Panel Divider B", center + inward * 0.31f + new Vector3(0f, 0.84f, baseScale.z * 0.24f), new Vector3(0.1f, 0.78f, 0.035f), neonMaterial, false);
        }

        Vector3 signPos = center + inward * 0.36f + Vector3.up * 1.55f;
        Quaternion signRot = horizontal ? Quaternion.Euler(0f, inward.z > 0f ? 180f : 0f, 0f)
            : Quaternion.Euler(0f, inward.x < 0f ? 90f : -90f, 0f);
        CreateArenaSign(parent, wallName + " - ARENA CHAOS Sign", signPos, signRot, accent);
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = pos;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider)
        {
            Destroy(box.GetComponent<Collider>());
        }
        return box;
    }

    private static void CreateArenaSign(Transform parent, string name, Vector3 pos, Quaternion rot, Color accent)
    {
        GameObject sign = new GameObject(name);
        sign.transform.SetParent(parent, false);
        sign.transform.SetPositionAndRotation(pos, rot);
        TextMesh text = sign.AddComponent<TextMesh>();
        text.text = "ARENA CHAOS";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.16f;
        text.color = accent;
    }

    private static void CreateNeonTower(Transform parent, string towerName, Vector3 pos, Color primary, Color secondary)
    {
        Material dark = CreateMaterial(new Color(0.018f, 0.025f, 0.04f));
        Material prim = CreateMaterial(primary, true);
        Material sec = CreateMaterial(secondary, true);

        CreateBox(parent, towerName + " - Column", pos + Vector3.up * 1.2f, new Vector3(0.62f, 2.4f, 0.62f), dark, false);
        CreateBox(parent, towerName + " - Primary LED", pos + Vector3.up * 1.2f + Vector3.forward * 0.33f, new Vector3(0.12f, 2.1f, 0.035f), prim, false);
        CreateBox(parent, towerName + " - Secondary LED", pos + Vector3.up * 1.2f + Vector3.right * 0.33f, new Vector3(0.035f, 2.1f, 0.12f), sec, false);

        for (int i = 0; i < 3; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = towerName + " - Neon Ring " + i;
            ring.transform.SetParent(parent, false);
            ring.transform.position = pos + Vector3.up * (0.45f + i * 0.72f);
            ring.transform.localScale = new Vector3(0.42f, 0.035f, 0.42f);
            ring.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? prim : sec;
            Destroy(ring.GetComponent<Collider>());
        }
    }

    private void ConfigureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.transform.SetPositionAndRotation(cameraPosition, Quaternion.Euler(cameraEulerAngles));
        cam.fieldOfView = 55f;
        if(ArenaContent.Load()!=null)
        {
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.085f,.065f);
        }
    }

    private static void EnsurePlayerMovement()
    {
        GameObject player = GameObject.Find("PLAYER");
        if (player != null && player.GetComponent<PlayerMove>() == null)
        {
            player.AddComponent<PlayerMove>();
        }
        if (player != null && player.transform.position.y < 1f)
        {
            player.transform.position = new Vector3(player.transform.position.x, 1f, player.transform.position.z);
        }
    }

    private static Material CreateMaterial(Color color, bool emission = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { color = color };
        mat.SetFloat("_Metallic", 0.78f);
        mat.SetFloat("_Smoothness", 0.72f);
        if (emission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 4f);
        }
        return mat;
    }
}
