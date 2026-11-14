using System.Collections.Generic;
using UnityEngine;

public class BattleArenaManager : MonoBehaviour
{
    [SerializeField] private int fighterCount = 10;
    [SerializeField] private float arenaRadius = 8f;

    private readonly List<ArenaFighter> fighters = new List<ArenaFighter>(50);
    private string statusMessage = "Hazırlanıyor...";
    private ArenaFighter champion;
    private UnityEngine.Object firstFighterVisualAsset;
    private Vector3 firstFighterVisualLocalPosition;
    private Vector3 firstFighterVisualLocalScale = Vector3.one;

    public float ArenaRadius => arenaRadius;
    public bool IsFinished { get; private set; }

    public void ConfigureFirstFighterVisual(UnityEngine.Object visualPrefab, Vector3 localPosition, Vector3 localScale)
    {
        firstFighterVisualAsset = visualPrefab;
        firstFighterVisualLocalPosition = localPosition;
        firstFighterVisualLocalScale = localScale;
    }
    public int LivingCount
    {
        get
        {
            int count = 0;
            foreach (ArenaFighter fighter in fighters)
            {
                if (fighter != null && fighter.IsAlive) count++;
            }
            return count;
        }
    }

    private void Start()
    {
        SpawnFighters();
    }

    private void SpawnFighters()
    {
        GameObject player = GameObject.Find("PLAYER");
        if (player == null)
        {
            Debug.LogError("BattleArenaManager: PLAYER nesnesi bulunamadı.");
            return;
        }

        PlayerMove playerMove = player.GetComponent<PlayerMove>();
        if (playerMove != null) playerMove.enabled = false;
        player.transform.position = Vector3.up;

        ArenaFighter playerFighter = player.GetComponent<ArenaFighter>() ?? player.AddComponent<ArenaFighter>();
        playerFighter.Initialize(this, "@PLAYER", Color.red);
        if (firstFighterVisualAsset != null)
        {
            playerFighter.AttachExternalVisual(firstFighterVisualAsset, firstFighterVisualLocalPosition, firstFighterVisualLocalScale);
        }
        fighters.Add(playerFighter);

        for (int i = 1; i < fighterCount; i++)
        {
            float angle = (i / (float)(fighterCount - 1)) * Mathf.PI * 2f;
            Vector3 position = new Vector3(Mathf.Cos(angle), 1f, Mathf.Sin(angle)) * 5.2f;
            GameObject fighterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fighterObject.name = "Fighter " + (i + 1);
            fighterObject.transform.position = position;
            fighterObject.transform.localScale = Vector3.one * 0.9f;

            ArenaFighter fighter = fighterObject.AddComponent<ArenaFighter>();
            fighter.Initialize(this, "@ArenaHero" + (i + 1).ToString("00"), Color.HSVToRGB(i / (float)fighterCount, 0.78f, 1f));
            fighters.Add(fighter);
        }

        statusMessage = "DÖVÜŞ BAŞLADI!";
    }

    public ArenaFighter FindNearestOpponent(ArenaFighter seeker)
    {
        ArenaFighter nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (ArenaFighter fighter in fighters)
        {
            if (fighter == null || fighter == seeker || !fighter.IsAlive) continue;
            float distance = (fighter.transform.position - seeker.transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = fighter;
            }
        }
        return nearest;
    }

    public void NotifyElimination(ArenaFighter eliminated, ArenaFighter attacker)
    {
        if (LivingCount <= 1)
        {
            IsFinished = true;
            champion = FindLivingFighter();
            statusMessage = champion == null ? "ŞAMPİYON YOK" : champion.DisplayName + " ŞAMPİYON!";
            return;
        }

        statusMessage = eliminated.DisplayName + " elendi!";
    }

    private ArenaFighter FindLivingFighter()
    {
        foreach (ArenaFighter fighter in fighters)
        {
            if (fighter != null && fighter.IsAlive) return fighter;
        }
        return null;
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        GUI.Box(new Rect(15f, 15f, 310f, 92f), GUIContent.none);
        GUI.Label(new Rect(25f, 22f, 290f, 32f), "HAYATTA KALAN: " + LivingCount + " / " + fighterCount, style);
        GUI.Label(new Rect(25f, 60f, 290f, 32f), statusMessage, style);
    }
}
