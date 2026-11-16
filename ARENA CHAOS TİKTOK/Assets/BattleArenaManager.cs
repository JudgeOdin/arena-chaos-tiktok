using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleArenaManager : MonoBehaviour
{
    [SerializeField] private int fighterCount = 10;
    [SerializeField] private float arenaRadius = 8f;

    private readonly List<ArenaFighter> fighters = new List<ArenaFighter>(50);
    private string statusMessage = "Hazırlanıyor...";
    private ArenaFighter champion;
    private ArenaFighter playerFighter;
    private UnityEngine.Object firstFighterVisualAsset;
    private RuntimeAnimatorController firstFighterAnimatorController;
    private Vector3 firstFighterVisualLocalPosition;
    private Vector3 firstFighterVisualLocalScale = Vector3.one;

    public float ArenaRadius => arenaRadius;
    public bool IsFinished { get; private set; }
    public ArenaFighter PlayerFighter => playerFighter;

    public void ConfigureFirstFighterVisual(UnityEngine.Object visualPrefab, RuntimeAnimatorController animatorController, Vector3 localPosition, Vector3 localScale)
    {
        firstFighterVisualAsset = visualPrefab;
        firstFighterAnimatorController = animatorController;
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
        this.playerFighter = playerFighter;
        playerFighter.Initialize(this, "@PLAYER", Color.red);
        if (firstFighterVisualAsset != null)
        {
            playerFighter.AttachExternalVisual(firstFighterVisualAsset, firstFighterVisualLocalPosition, firstFighterVisualLocalScale, firstFighterAnimatorController);
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

    public ArenaFighter SpawnGiftFighter(string displayName, Color color)
    {
        IsFinished = false;
        champion = null;
        fighterCount++;

        float angle = (fighterCount * 1.618f) % (Mathf.PI * 2f);
        Vector3 position = new Vector3(Mathf.Cos(angle), 1f, Mathf.Sin(angle)) * (arenaRadius * 0.55f);
        GameObject fighterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fighterObject.name = "Gift Fighter " + fighterCount;
        fighterObject.transform.position = position;
        fighterObject.transform.localScale = Vector3.one * 0.9f;

        ArenaFighter fighter = fighterObject.AddComponent<ArenaFighter>();
        fighter.Initialize(this, displayName, color);
        fighters.Add(fighter);
        statusMessage = displayName + " GÜL İLE ARENAYA KATILDI!";
        return fighter;
    }

    public void ApplyGiftToPlayer(GiftEventType giftType, string senderName)
    {
        if (giftType == GiftEventType.Rose)
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(fighterCount * 0.137f, 1f), 0.78f, 1f);
            SpawnGiftFighter("@" + senderName, color);
            return;
        }

        if (playerFighter == null || !playerFighter.IsAlive)
        {
            statusMessage = "PLAYER ELENDİ; HEDİYE İÇİN YENİ GÜL GÖNDER!";
            return;
        }

        switch (giftType)
        {
            case GiftEventType.Health:
                playerFighter.GiftIncreaseMaxHealth(15f);
                statusMessage = senderName + " PLAYER'A CAN HEDİYESİ GÖNDERDİ!";
                break;
            case GiftEventType.Attack:
                playerFighter.GiftIncreaseAttack(4f);
                statusMessage = senderName + " PLAYER'IN SALDIRISINI GÜÇLENDİRDİ!";
                break;
            case GiftEventType.Speed:
                playerFighter.GiftIncreaseSpeed(0.35f);
                statusMessage = senderName + " PLAYER'IN HIZINI ARTIRDI!";
                break;
            case GiftEventType.FirePunch:
                playerFighter.GiftIncreaseAttack(10f);
                statusMessage = senderName + " ALEVLI YUMRUK GÖNDERDİ!";
                break;
            case GiftEventType.Shield:
                playerFighter.GiftShield(35f);
                statusMessage = senderName + " PLAYER'A KALKAN VERDİ!";
                break;
            case GiftEventType.MegaBlast:
                ApplyAreaGiftDamage(28f, playerFighter);
                CreateMegaBlastEffect();
                statusMessage = senderName + " BÜYÜK PATLAMA GÖNDERDİ!";
                break;
        }
    }

    private void CreateMegaBlastEffect()
    {
        GameObject blast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        blast.name = "Gift Mega Blast VFX";
        blast.transform.position = Vector3.up * 0.35f;
        blast.transform.localScale = Vector3.one * 0.35f;

        Renderer blastRenderer = blast.GetComponent<Renderer>();
        blastRenderer.material = CreateEffectMaterial(new Color(1f, 0.22f, 0.02f));
        Destroy(blast.GetComponent<Collider>());

        GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shockwave.name = "Gift Mega Blast Shockwave";
        shockwave.transform.position = Vector3.up * 0.04f;
        shockwave.transform.localScale = new Vector3(0.35f, 0.025f, 0.35f);
        Renderer shockwaveRenderer = shockwave.GetComponent<Renderer>();
        shockwaveRenderer.material = CreateEffectMaterial(new Color(1f, 0.82f, 0.08f));
        Destroy(shockwave.GetComponent<Collider>());

        StartCoroutine(AnimateMegaBlast(blast, shockwave));
    }

    private IEnumerator AnimateMegaBlast(GameObject blast, GameObject shockwave)
    {
        const float duration = 0.55f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            float blastScale = Mathf.Lerp(0.35f, 4.5f, eased);
            float waveScale = Mathf.Lerp(0.35f, 6.5f, eased);

            if (blast != null) blast.transform.localScale = Vector3.one * blastScale;
            if (shockwave != null) shockwave.transform.localScale = new Vector3(waveScale, 0.025f, waveScale);
            yield return null;
        }

        if (blast != null) Destroy(blast);
        if (shockwave != null) Destroy(shockwave);
    }

    private static Material CreateEffectMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader)
        {
            color = color
        };
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 2.5f);
        return material;
    }

    private void ApplyAreaGiftDamage(float damage, ArenaFighter protectedFighter)
    {
        ArenaFighter[] snapshot = fighters.ToArray();
        foreach (ArenaFighter fighter in snapshot)
        {
            if (fighter != null && fighter != protectedFighter && fighter.IsAlive)
            {
                fighter.TakeDamage(damage, protectedFighter);
            }
        }
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
