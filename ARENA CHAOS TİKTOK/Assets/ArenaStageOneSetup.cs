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

        battleManager.ConfigureFirstFighterVisual(firstFighterVisualAsset, firstFighterVisualLocalPosition, firstFighterVisualLocalScale);
    }

    private void BuildArenaWalls()
    {
        const string containerName = "Arena Walls (Stage 1)";
        if (GameObject.Find(containerName) != null)
        {
            return;
        }

        GameObject container = new GameObject(containerName);
        Material wallMaterial = CreateMaterial(new Color(0.12f, 0.08f, 0.22f));

        CreateWall(container.transform, "North Wall", new Vector3(0f, wallHeight * 0.5f, arenaRadius), new Vector3(arenaRadius * 2f, wallHeight, wallThickness), wallMaterial);
        CreateWall(container.transform, "South Wall", new Vector3(0f, wallHeight * 0.5f, -arenaRadius), new Vector3(arenaRadius * 2f, wallHeight, wallThickness), wallMaterial);
        CreateWall(container.transform, "East Wall", new Vector3(arenaRadius, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaRadius * 2f), wallMaterial);
        CreateWall(container.transform, "West Wall", new Vector3(-arenaRadius, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaRadius * 2f), wallMaterial);
    }

    private static void CreateWall(Transform parent, string wallName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = wallName;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = material;
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

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        return material;
    }
}
