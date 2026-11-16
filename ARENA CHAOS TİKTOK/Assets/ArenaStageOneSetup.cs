using UnityEngine;

/// <summary>
/// Creates the first playable arena dressing at runtime without replacing the
/// objects already present in the scene.
/// </summary>
public class ArenaStageOneSetup : MonoBehaviour
{
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

        battleManager.ConfigureFirstFighterVisual(firstFighterVisualAsset, firstFighterAnimatorController, firstFighterVisualLocalPosition, firstFighterVisualLocalScale);
        if (GetComponent<GiftEventSimulator>() == null)
        {
            gameObject.AddComponent<GiftEventSimulator>();
        }
    }

    private void BuildArenaWalls()
    {
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

        CreateWallSet(root, "North Wall", new Vector3(0f, wallHeight * 0.5f, arenaRadius), new Vector3(arenaRadius * 2f, wallHeight, wallThickness), Vector3.back, blueNeon, true, metal, panel);
        CreateWallSet(root, "South Wall", new Vector3(0f, wallHeight * 0.5f, -arenaRadius), new Vector3(arenaRadius * 2f, wallHeight, wallThickness), Vector3.forward, redNeon, true, metal, panel);
        CreateWallSet(root, "East Wall", new Vector3(arenaRadius, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaRadius * 2f), Vector3.left, blueNeon, false, metal, panel);
        CreateWallSet(root, "West Wall", new Vector3(-arenaRadius, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaRadius * 2f), Vector3.right, redNeon, false, metal, panel);

        CreateNeonTower(root, "Tower NE", new Vector3(arenaRadius - 0.35f, 0f, arenaRadius - 0.35f), redNeon, blueNeon);
        CreateNeonTower(root, "Tower NW", new Vector3(-arenaRadius + 0.35f, 0f, arenaRadius - 0.35f), blueNeon, redNeon);
        CreateNeonTower(root, "Tower SE", new Vector3(arenaRadius - 0.35f, 0f, -arenaRadius + 0.35f), blueNeon, redNeon);
        CreateNeonTower(root, "Tower SW", new Vector3(-arenaRadius + 0.35f, 0f, -arenaRadius + 0.35f), redNeon, blueNeon);
    }

    private void CreateWallSet(Transform parent, string wallName, Vector3 center, Vector3 baseScale, Vector3 inward, Color accent, bool horizontal, Color metal, Color panel)
    {
        Material metalMaterial = CreateMaterial(metal);
        Material panelMaterial = CreateMaterial(panel);
        Material neonMaterial = CreateMaterial(accent, true);

        CreateBox(parent, wallName + " - Physical Boundary", center, baseScale, metalMaterial, true);

        Vector3 topPosition = center + Vector3.up * (wallHeight * 0.5f + 0.16f);
        Vector3 topScale = horizontal ? new Vector3(baseScale.x, 0.22f, 0.58f) : new Vector3(0.58f, 0.22f, baseScale.z);
        CreateBox(parent, wallName + " - Top Beam", topPosition, topScale, metalMaterial, false);

        Vector3 panelPosition = center + inward * 0.22f + Vector3.up * 0.84f;
        Vector3 panelScale = horizontal ? new Vector3(baseScale.x * 0.78f, 0.72f, 0.08f) : new Vector3(0.08f, 0.72f, baseScale.z * 0.78f);
        CreateBox(parent, wallName + " - Tech Panels", panelPosition, panelScale, panelMaterial, false);

        Vector3 neonPosition = center + inward * 0.3f + Vector3.up * 0.38f;
        Vector3 neonScale = horizontal ? new Vector3(baseScale.x * 0.94f, 0.07f, 0.08f) : new Vector3(0.08f, 0.07f, baseScale.z * 0.94f);
        CreateBox(parent, wallName + " - Neon Strip", neonPosition, neonScale, neonMaterial, false);

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

        Vector3 signPosition = center + inward * 0.36f + Vector3.up * 1.55f;
        Quaternion signRotation = horizontal
            ? Quaternion.Euler(0f, inward.z > 0f ? 0f : 180f, 0f)
            : Quaternion.Euler(0f, inward.x < 0f ? 90f : -90f, 0f);
        CreateArenaSign(parent, wallName + " - ARENA CHAOS Sign", signPosition, signRotation, accent);
    }

    private static GameObject CreateBox(Transform parent, string objectName, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objectName;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Destroy(box.GetComponent<Collider>());
        }
        return box;
    }

    private static void CreateArenaSign(Transform parent, string objectName, Vector3 position, Quaternion rotation, Color accent)
    {
        GameObject sign = new GameObject(objectName);
        sign.transform.SetParent(parent, false);
        sign.transform.SetPositionAndRotation(position, rotation);
        TextMesh text = sign.AddComponent<TextMesh>();
        text.text = "ARENA CHAOS";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 42;
        text.characterSize = 0.16f;
        text.color = accent;
    }

    private static void CreateNeonTower(Transform parent, string towerName, Vector3 position, Color primary, Color secondary)
    {
        Material darkMaterial = CreateMaterial(new Color(0.018f, 0.025f, 0.04f));
        Material primaryMaterial = CreateMaterial(primary, true);
        Material secondaryMaterial = CreateMaterial(secondary, true);

        CreateBox(parent, towerName + " - Column", position + Vector3.up * 1.2f, new Vector3(0.62f, 2.4f, 0.62f), darkMaterial, false);
        CreateBox(parent, towerName + " - Primary LED", position + Vector3.up * 1.2f + Vector3.forward * 0.33f, new Vector3(0.12f, 2.1f, 0.035f), primaryMaterial, false);
        CreateBox(parent, towerName + " - Secondary LED", position + Vector3.up * 1.2f + Vector3.right * 0.33f, new Vector3(0.035f, 2.1f, 0.12f), secondaryMaterial, false);

        for (int i = 0; i < 3; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = towerName + " - Neon Ring " + i;
            ring.transform.SetParent(parent, false);
            ring.transform.position = position + Vector3.up * (0.45f + i * 0.72f);
            ring.transform.localScale = new Vector3(0.42f, 0.035f, 0.42f);
            ring.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? primaryMaterial : secondaryMaterial;
            Destroy(ring.GetComponent<Collider>());
        }
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.transform.SetPositionAndRotation(cameraPosition, Quaternion.Euler(cameraEulerAngles));
        camera.fieldOfView = 55f;
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
        Material material = new Material(shader)
        {
            color = color
        };
        material.SetFloat("_Metallic", 0.78f);
        material.SetFloat("_Smoothness", 0.72f);
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 4f);
        }
        return material;
    }
}
