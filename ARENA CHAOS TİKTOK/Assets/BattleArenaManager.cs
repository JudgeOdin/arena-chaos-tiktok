using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleArenaManager : MonoBehaviour
{
    [SerializeField] private int fighterCount = 10;
    [SerializeField] private float arenaRadius = 8f;

    public ArenaMatchSettings MatchSettings { get; private set; }
    public bool AutomaticReinforcements { get; set; } = true;
    public float RemainingSeconds=>Mathf.Max(0,roundEndsAt-Time.time);
    public string ChampionName { get; private set; }
    public string ChampionViewerId { get; private set; }
    float roundEndsAt,nextReinforcement,roundStartedAt,nextMeteor;
    public bool TryReserveMeteor()
    {
        if(IsFinished||Time.time<nextMeteor)return false;
        nextMeteor=Time.time+(MatchSettings!=null?MatchSettings.meteorMinimumInterval:15);return true;
    }
    int reserveNumber;
    public sealed class Standing { public ArenaFighter fighter;public string name,id,avatarUrl;public int kills,hits,visualIndex;public float damage,points;public bool bot; }
    readonly List<Standing> ranked=new List<Standing>();
    readonly Dictionary<string,Standing> standings=new Dictionary<string,Standing>();
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
    public IReadOnlyList<ArenaFighter> Fighters => fighters;
    public string StatusMessage => statusMessage;
    public ArenaFighter Champion => champion;

    private void AttachSelectedVisual(ArenaFighter fighter, int index)
    {
        var content = ArenaContent.Load();
        if (content != null && content.fighters != null && content.fighters.Length > 0)
            fighter.AttachExternalVisual(content.fighters[index % content.fighters.Length], Vector3.down, Vector3.one, content.controller);
        else if (firstFighterVisualAsset != null)
            fighter.AttachExternalVisual(firstFighterVisualAsset, firstFighterVisualLocalPosition, firstFighterVisualLocalScale, firstFighterAnimatorController);
    }

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
        Application.runInBackground = true;
        string[] args = System.Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++)
            if(args[i]=="-arenaFighters" && int.TryParse(args[i+1],out int count))fighterCount=Mathf.Clamp(count,2,50);
        MatchSettings=Resources.Load<ArenaMatchSettings>("ArenaChaos/MatchSettings");
        roundStartedAt=Time.time;
        int seconds=PlayerPrefs.GetInt("ArenaRoundSeconds",MatchSettings!=null?(int)MatchSettings.roundLength:180);
        roundEndsAt=Time.time+(seconds==300?300:180);
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
        if (ArenaContent.Load() != null || firstFighterVisualAsset != null)
        {
            AttachSelectedVisual(playerFighter, 0);
        }
        else
        {
            Debug.LogWarning("BattleArenaManager: PLAYER için harici görsel asset atanmadı; PLAYER görseli boş kalacak.");
        }
        playerFighter.IsBot=true;
        fighters.Add(playerFighter);

        for (int i = 1; i < fighterCount; i++)
        {
            float angle = (i / (float)(fighterCount - 1)) * Mathf.PI * 2f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 5.2f, 1f, Mathf.Sin(angle) * 5.2f);
            if(fighterCount>20)
            {
                int cell=i-1;
                if(cell>=24)cell++;
                position=new Vector3((cell%7-3)*1.7f,1f,(cell/7-3)*1.7f);
            }
            GameObject fighterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fighterObject.name = "Fighter " + (i + 1);
            fighterObject.transform.position = position;
            fighterObject.transform.localScale = Vector3.one * 0.9f;

            ArenaFighter fighter = fighterObject.AddComponent<ArenaFighter>();
            fighter.Initialize(this, "@ArenaHero" + (i + 1).ToString("00"), Color.HSVToRGB(i / (float)fighterCount, 0.78f, 1f));
            if (ArenaContent.Load() != null || firstFighterVisualAsset != null)
            {
                AttachSelectedVisual(fighter, i);
            }
            fighter.IsBot=true;
            fighters.Add(fighter);
        }

        statusMessage = "DÖVÜŞ BAŞLADI!";
    }

    public ArenaFighter SpawnGiftFighter(string displayName, Color color, int modelIndex = -1)
    {
        var entranceDirector = GetComponent<ArenaPresentationDirector>();
        if(entranceDirector != null && !entranceDirector.CanAcceptEntrance)
        { statusMessage="Entrance queue full"; return null; }
        if(LivingCount>=50){statusMessage="Arena dolu: en fazla 50 aktif dövüşçü.";return null;}
        if(IsFinished){statusMessage="Tur sonucu gösteriliyor; sonraki turu bekle.";return null;}
        fighterCount++;

        int entranceSide = Random.value < 0.5f ? -1 : 1;
        Vector3 position = new Vector3(entranceSide * (arenaRadius + 3.2f), 1f, 0f);
        GameObject fighterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fighterObject.name = "Gift Fighter " + fighterCount;
        fighterObject.transform.position = position;
        fighterObject.transform.localScale = Vector3.one * 0.9f;

        ArenaFighter fighter = fighterObject.AddComponent<ArenaFighter>();
        fighter.Initialize(this, displayName, color);
        if (ArenaContent.Load() != null || firstFighterVisualAsset != null)
        {
            AttachSelectedVisual(fighter, modelIndex>=0?modelIndex:(Random.value<.5f?0:Random.Range(4,6)));
        }
        fighter.enabled = false;
        fighters.Add(fighter);
        ArenaPresentationDirector presentation = GetComponent<ArenaPresentationDirector>();
        if (presentation != null) presentation.PlayEntrance(fighter, position);
        else { fighter.transform.position = new Vector3(entranceSide * (arenaRadius - 2f), 1f, 0f); fighter.enabled = true; }
        statusMessage = displayName + " GÜL İLE ARENAYA KATILDI!";
        return fighter;
    }

    public ArenaFighter SpawnGiftFighter(string viewerId,string displayName,string avatarUrl,Color color)
    {
        var fighter=SpawnGiftFighter(displayName.StartsWith("@")?displayName:"@"+displayName,color);
        if(fighter!=null)fighter.SetViewerProfile(viewerId,avatarUrl);
        return fighter;
    }
    public ArenaFighter SpawnClassFighter(string viewerId,string displayName,string avatarUrl,ArenaFighterRole role)
    {
        var roster=Resources.Load<ArenaGiftRoster>("ArenaChaos/GiftRoster");var tier=roster!=null?roster.Find(role):null;
        if(tier==null){statusMessage="Karakter sınıfı kataloğu eksik";return null;}
        var fighter=SpawnGiftFighter(displayName.StartsWith("@")?displayName:"@"+displayName,Color.white,tier.modelIndex);
        if(fighter!=null)
        {
            fighter.SetViewerProfile(viewerId,avatarUrl);fighter.ConfigureRole(tier);
            ArenaBroadcastHUD.NotifyGift(displayName,tier.label,"Yeni sınıf arenaya katılıyor");
        }
        return fighter;
    }
    public ArenaFighter FindViewerFighter(string viewerId)
    {
        for(int i=fighters.Count-1;i>=0;i--)
            if(fighters[i]!=null&&fighters[i].IsAlive&&fighters[i].ViewerId==viewerId)return fighters[i];
        return null;
    }
    public void ApplyGiftToPlayer(GiftEventType giftType,string senderName)
    {ApplyGiftToViewer(giftType,senderName.TrimStart('@'),senderName,null);}

    public void ApplyGiftToViewer(GiftEventType giftType,string viewerId,string senderName,string avatarUrl)
    {TryApplyGiftToViewer(giftType,viewerId,senderName,avatarUrl);}
    public bool TryApplyGiftToViewer(GiftEventType giftType,string viewerId,string senderName,string avatarUrl)
    {
        if(IsFinished||string.IsNullOrWhiteSpace(viewerId))return false;
        if(giftType==GiftEventType.ArcherEntry||giftType==GiftEventType.GuardianEntry)
        {
            var role=giftType==GiftEventType.ArcherEntry?ArenaFighterRole.Archer:ArenaFighterRole.Guardian;
            bool spawned=SpawnClassFighter(viewerId,senderName,avatarUrl,role)!=null;
            if(spawned)ArenaBroadcastHUD.RecordGift(senderName);return spawned;
        }
        if (giftType == GiftEventType.Rose)
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(fighterCount * 0.137f, 1f), 0.78f, 1f);
            bool spawned=SpawnGiftFighter(viewerId,senderName,avatarUrl,color)!=null;
            if(spawned)ArenaBroadcastHUD.RecordGift(senderName);return spawned;
        }

        ArenaFighter recipient = FindViewerFighter(viewerId);
        if (recipient == null)
        {
            statusMessage = senderName + ": önce gül ile arenaya katıl.";
            return false;
        }

        recipient.SetViewerProfile(viewerId,avatarUrl??recipient.AvatarUrl);
        if((recipient.IsEntering||!recipient.gameObject.activeInHierarchy)&&(System.Enum.TryParse<ArenaPower>(giftType.ToString(),out _)||giftType==GiftEventType.FirePunch))return false;
        if(System.Enum.TryParse<ArenaPower>(giftType.ToString(),out var power))
        {bool applied=recipient.PowerUps.Activate(power);if(applied)ArenaBroadcastHUD.RecordGift(senderName);return applied;}
        if(giftType==GiftEventType.FirePunch){bool applied=recipient.PowerUps.Activate(ArenaPower.Fire);if(applied)ArenaBroadcastHUD.RecordGift(senderName);return applied;}
        ArenaBroadcastHUD.RecordGift(senderName);
        ArenaBroadcastHUD.NotifyGift(senderName,giftType.ToString(),recipient.DisplayName);
        switch (giftType)
        {
            case GiftEventType.Health:
                recipient.GiftIncreaseMaxHealth(15f);
                statusMessage = recipient.DisplayName + " CAN KAZANDI!";
                break;
            case GiftEventType.Attack:
                recipient.GiftIncreaseAttack(4f);
                statusMessage = recipient.DisplayName + " SALDIRISINI GÜÇLENDİRDİ!";
                break;
            case GiftEventType.Speed:
                recipient.GiftIncreaseSpeed(0.35f);
                statusMessage = recipient.DisplayName + " HIZ KAZANDI!";
                break;
            case GiftEventType.FirePunch:
                recipient.PowerUps.Activate(ArenaPower.Fire);
                statusMessage = senderName + " ALEVLI YUMRUK GÖNDERDİ!";
                break;
            case GiftEventType.Shield:
                recipient.GiftShield(35f);
                statusMessage = recipient.DisplayName + " KALKAN KAZANDI!";
                break;
            case GiftEventType.MegaBlast:
                ApplyAreaGiftDamage(28f, recipient);
                CreateMegaBlastEffect();
                statusMessage = senderName + " BÜYÜK PATLAMA GÖNDERDİ!";
                break;
        }
        return true;
    }

    public ArenaFighter FindSenderFighter(string senderName)
    {
        string name = "@" + senderName.TrimStart('@');
        for(int i=fighters.Count-1;i>=0;i--)
            if(fighters[i]!=null&&fighters[i].IsAlive&&fighters[i].DisplayName==name)return fighters[i];
        return null;
    }

    private void CreateMegaBlastEffect()
    {
        ArenaPresentationDirector presentation = GetComponent<ArenaPresentationDirector>();
        if(ArenaContent.Load()!=null)
        { if(presentation!=null)presentation.PlayMegaBlast();else ArenaAudioEffects.Blast();return; }
        if (presentation != null)
        {
            presentation.PlayMegaBlast();
        }

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
            if (fighter == null || fighter == seeker || !fighter.IsAlive || !fighter.isActiveAndEnabled) continue;
            float distance = (fighter.transform.position - seeker.transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = fighter;
            }
        }
        return nearest;
    }

    Standing GetStanding(ArenaFighter fighter)
    {
        string id=string.IsNullOrEmpty(fighter.ViewerId)?fighter.DisplayName:fighter.ViewerId;
        if(!standings.TryGetValue(id,out var standing))
        {standing=new Standing{id=id,name=fighter.DisplayName,bot=fighter.IsBot};standings.Add(id,standing);}
        var previous=standing.fighter;
        if(previous==null||previous==fighter||!previous.IsAlive&&fighter.IsAlive||previous.IsAlive&&fighter.IsAlive&&(previous.IsEntering&&!fighter.IsEntering||previous.IsEntering==fighter.IsEntering&&fighter.Health>previous.Health))
        {standing.fighter=fighter;standing.visualIndex=fighter.VisualIndex;}
        standing.name=fighter.DisplayName;if(!string.IsNullOrEmpty(fighter.AvatarUrl))standing.avatarUrl=fighter.AvatarUrl;return standing;
    }
    public IReadOnlyList<Standing> GetRankedStandings()
    {
        foreach(var actor in fighters)if(actor!=null)GetStanding(actor);
        ranked.Clear();bool viewers=false;foreach(var entry in standings.Values)if(!entry.bot)viewers=true;
        foreach(var entry in standings.Values)if(!viewers||!entry.bot)ranked.Add(entry);
        ranked.Sort((a,b)=>{int c=b.points.CompareTo(a.points);if(c==0)c=b.kills.CompareTo(a.kills);if(c==0)c=b.damage.CompareTo(a.damage);return c!=0?c:string.CompareOrdinal(a.id,b.id);});
        return ranked;
    }
    public void RecordRoundHit(ArenaFighter actor){if(actor!=null&&!IsFinished)GetStanding(actor).hits++;}
    public void RecordRoundDamage(ArenaFighter attacker,float damage,ArenaFighter victim=null)
    {
        if(attacker==null||IsFinished||SameViewer(attacker,victim))return;
        var standing=GetStanding(attacker);damage=Mathf.Max(0,damage);standing.damage+=damage;
        standing.points+=damage*.1f*(victim!=null&&victim.IsBot?(MatchSettings!=null?MatchSettings.botScoreWeight:.25f):1);
    }
    public void NotifyElimination(ArenaFighter eliminated,ArenaFighter attacker)
    {
        if(attacker!=null&&attacker!=eliminated&&!IsFinished&&!SameViewer(attacker,eliminated))
        {var row=GetStanding(attacker);row.kills++;row.points+=100*(eliminated.IsBot?(MatchSettings!=null?MatchSettings.botScoreWeight:.25f):1);}
        statusMessage=eliminated.DisplayName+" elendi; tur devam ediyor.";
    }
    static bool SameViewer(ArenaFighter first,ArenaFighter second)
    {return first!=null&&second!=null&&!string.IsNullOrEmpty(first.ViewerId)&&first.ViewerId==second.ViewerId;}
    public void SelectRoundLength(ArenaRoundLength length)
    {
        if(IsFinished)return;
        roundEndsAt=roundStartedAt+(int)length;PlayerPrefs.SetInt("ArenaRoundSeconds",(int)length);PlayerPrefs.Save();
    }
    public bool ApplyMappedCharacterGift(string giftId,string viewerId,string displayName,string avatarUrl)
    {
        var roster=Resources.Load<ArenaGiftRoster>("ArenaChaos/GiftRoster");if(roster==null||string.IsNullOrEmpty(giftId))return false;
        foreach(var tier in roster.tiers)if(!string.IsNullOrEmpty(tier.tiktokGiftId)&&tier.tiktokGiftId==giftId)
            return SpawnClassFighter(viewerId,displayName,avatarUrl,tier.role)!=null;
        return false;
    }
    public void FinishTimedRound()
    {
        if(IsFinished)return;
        var ordered=GetRankedStandings();Standing best=ordered.Count>0?ordered[0]:null;
        champion=best!=null?best.fighter:null;ChampionName=best!=null?best.name:"BERABERE";ChampionViewerId=best!=null?best.id:"";
        IsFinished=true;statusMessage=ChampionName+" TUR ŞAMPİYONU!";
    }
    void Update()
    {
        if(IsFinished)return;
        if(Time.time>=roundEndsAt){FinishTimedRound();return;}
        if(!AutomaticReinforcements||Time.time<nextReinforcement)return;
        nextReinforcement=Time.time+(MatchSettings!=null?MatchSettings.reinforcementInterval:3);
        int population=0;foreach(var actor in fighters)if(actor!=null&&actor.IsAlive&&!actor.IsEntering)population++;
        int target=MatchSettings!=null?MatchSettings.reservePopulation:10;
        if(population>=target-2)return;
        int count=Mathf.Min(target-population,50-LivingCount);
        for(int i=0;i<count;i++)SpawnReserve(i,count);
        for(int i=fighters.Count-1;i>=0;i--)
        {
            var actor=fighters[i];
            if(actor!=null&&actor.IsBot&&!actor.IsAlive&&!actor.gameObject.activeSelf&&fighters.Count>32)
            {ArenaBroadcastHUD.ForgetBot(actor);fighters.RemoveAt(i);Destroy(actor.gameObject);}
        }
    }
    void SpawnReserve(int index,int total)
    {
        float angle=(index/(float)Mathf.Max(1,total))*Mathf.PI*2+reserveNumber*.63f;
        var obj=GameObject.CreatePrimitive(PrimitiveType.Capsule);obj.name="Arena reserve bot";
        obj.transform.position=new Vector3(Mathf.Cos(angle)*(arenaRadius-1.5f),1,Mathf.Sin(angle)*(arenaRadius-1.5f));obj.transform.localScale=Vector3.one*.9f;
        var fighter=obj.AddComponent<ArenaFighter>();fighter.Initialize(this,"CPU Guard "+(++reserveNumber),Color.gray);fighter.IsBot=true;
        AttachSelectedVisual(fighter,reserveNumber%6);fighters.Add(fighter);
        ArenaAudioEffects.Impact(obj.transform.position,false);
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
        if (GetComponent<ArenaBroadcastHUD>() != null) return;
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
