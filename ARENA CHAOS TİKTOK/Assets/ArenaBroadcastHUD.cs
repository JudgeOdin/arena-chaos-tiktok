using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public sealed partial class ArenaBroadcastHUD : MonoBehaviour
{
    static ArenaBroadcastHUD instance;
    readonly Queue<string> feed = new Queue<string>();
    readonly Dictionary<string,int> supporters = new Dictionary<string,int>();
    readonly List<string> supporterNames = new List<string>();
    BattleArenaManager arena;
    Text counter, clock, clockCaption, events, champion, support, entrance, connectionInfo;
    static int round;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession(){instance=null;round=0;}
    GameObject winnerPanel;
    GameObject canvasRoot;
    bool portraitLayout;
    float nextUpdate, started, restartAt;
    bool ended;
    ArenaViewerProfiles viewerProfiles;
    RawImage[] portraits=new RawImage[3];
    RawImage championPortrait;
    Text[] rowNames=new Text[3],rowStats=new Text[3],rowPowers=new Text[3];
    Image[] rowHealth=new Image[3];
    Font font;
    static readonly Color Gold = new Color(.98f, .74f, .36f);
    static readonly Color Teal = new Color(.30f, .90f, .79f);
    static readonly Color Muted = new Color(.70f, .79f, .88f);
    static readonly Color Silver = new Color(.63f, .76f, .94f);
    static readonly Color Bronze = new Color(.88f, .51f, .29f);
    readonly Queue<string> giftNotices=new Queue<string>();
    Text giftNotice;
    CanvasGroup giftGroup;
    float noticeUntil, modeMomentUntil;
    string modeMoment;
    public static void NotifyGift(string sender,string gift,string benefit)
    {
        if(instance==null)return;
        if(instance.giftNotices.Count<24)instance.giftNotices.Enqueue(ArenaUiText.Short(sender)+"  /  "+ArenaUiText.Safe(gift)+"\n<size=18>"+ArenaUiText.Safe(benefit)+"</size>");
    }
    public static void NotifyModeMoment(string actor, string moment, string detail)
    {
        if (instance == null) return;
        instance.modeMoment = "<b>" + ArenaUiText.Safe(moment) + "</b>\n<size=18>" +
            ArenaUiText.Short(actor) + "  /  " + ArenaUiText.Safe(detail) + "</size>";
        instance.modeMomentUntil = Time.time + 3f;
    }

    public static void ForgetBot(ArenaFighter fighter) { }
    public static void RecordHit(ArenaFighter attacker,ArenaFighter victim)
    {if(attacker!=null)attacker.Arena.RecordRoundHit(attacker);}
    public static void RecordDamage(ArenaFighter attacker,float damage)
    {if(attacker!=null)attacker.Arena.RecordRoundDamage(attacker,damage);}
    public static void RecordGift(string sender)
    {
        if(instance==null)return;
        if(!instance.supporters.ContainsKey(sender)){instance.supporters.Add(sender,0);instance.supporterNames.Add(sender);}
        instance.supporters[sender]++;
    }
    public static void RecordElimination(ArenaFighter victim, ArenaFighter attacker)
    {
        if (instance == null) return;

        instance.feed.Enqueue(ArenaUiText.Short(attacker != null ? attacker.DisplayName : "ARENA") + "  >  " + ArenaUiText.Short(victim.DisplayName));
        while (instance.feed.Count > 3) instance.feed.Dequeue();
    }
    void Awake() { instance = this; }
    void OnDestroy() { if (instance == this) instance = null; }
    void Start()
    {
        viewerProfiles=gameObject.AddComponent<ArenaViewerProfiles>();
        gameObject.AddComponent<ArenaNameplateLayout>();
        arena = GetComponent<BattleArenaManager>(); started = Time.time; round++;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildHud();
    }
    void BuildHud()
    {
        if(canvasRoot!=null)Destroy(canvasRoot);
        var root = new GameObject("Broadcast HUD", typeof(Canvas), typeof(CanvasScaler));canvasRoot=root;
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        bool portrait = Screen.width < Screen.height;portraitLayout=portrait;
        scaler.referenceResolution = portrait ? new Vector2(1080,1920) : new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        if (portrait)
        {
            var band = new Color(.017f,.025f,.043f);
            Strip(root.transform,"Bottom band",Vector2.zero,new Vector2(1,.19f),band);
            Strip(root.transform,"Top band",new Vector2(0,.73f),Vector2.one,band);
            Backdrop(root.transform, Vector2.zero, new Vector2(1, .22f), true);
            Backdrop(root.transform, new Vector2(0, .70f), Vector2.one, false);
        }

        var brand = Panel(root.transform, "Arena masthead",
            portrait ? new Vector2(.03f, .939f) : new Vector2(.025f, .907f),
            portrait ? new Vector2(.62f, .987f) : new Vector2(.30f, .982f), Gold);
        var emblem = Panel(brand, "Arena monogram", new Vector2(.035f, .17f), new Vector2(.18f, .83f), Gold);
        Headline(emblem, "AC", 25, new Vector2(.05f, .05f), new Vector2(.95f, .95f), Gold).alignment = TextAnchor.MiddleCenter;
        Headline(brand, "ARENA <color=#FFC86B>CHAOS</color>", 37,
            new Vector2(.22f, .32f), new Vector2(.97f, .93f), Color.white);
        Headline(brand, arena.ModeLabel.ToUpperInvariant(),
            16, new Vector2(.225f, .06f), new Vector2(.97f, .34f), Muted);

        var roundPlate = Panel(root.transform, "Round clock",
            portrait ? new Vector2(.64f, .939f) : new Vector2(.79f, .907f),
            portrait ? new Vector2(.97f, .987f) : new Vector2(.975f, .982f), Gold);
        clockCaption = Headline(roundPlate, "", 15, new Vector2(.06f, .64f), new Vector2(.94f, .91f), Gold);
        clockCaption.alignment = TextAnchor.MiddleCenter;
        clock = Headline(roundPlate, "", 40, new Vector2(.05f, .07f), new Vector2(.95f, .65f), Color.white);
        clock.alignment = TextAnchor.MiddleCenter;

        var alivePlate = Panel(root.transform, "Match status",
            portrait ? new Vector2(.53f, .897f) : new Vector2(.405f, .927f),
            portrait ? new Vector2(.97f, .932f) : new Vector2(.595f, .982f), Teal);
        counter = Headline(alivePlate, "", 25, new Vector2(.045f, .12f), new Vector2(.955f, .88f), Color.white);
        counter.alignment = TextAnchor.MiddleCenter;
        var board=new GameObject("Ranking cards",typeof(RectTransform)).GetComponent<RectTransform>();board.SetParent(root.transform,false);
        board.anchorMin=portrait?new Vector2(.03f,.73f):new Vector2(.025f,.56f);board.anchorMax=portrait?new Vector2(.50f,.93f):new Vector2(.255f,.89f);board.offsetMin=board.offsetMax=Vector2.zero;
        Headline(board, arena.IsTournament ? "KARŞILAŞMA" : "ARENA SIRALAMASI",
            21, new Vector2(.035f, .91f), new Vector2(.98f, 1), Gold);
        for(int i=0;i<3;i++)
        {
            float bottom=.025f+(2-i)*.29f;
            Color rankColor = i == 0 ? Gold : i == 1 ? Silver : Bronze;
            var card = Panel(board, "Rank " + (i + 1), new Vector2(0, bottom), new Vector2(1, bottom + .275f), rankColor);
            var rank = Headline(card, (i + 1).ToString(), 26, new Vector2(.026f, .19f), new Vector2(.125f, .81f), rankColor);
            rank.alignment = TextAnchor.MiddleCenter;
            var avatarFrame = Panel(card, "Avatar frame", new Vector2(.133f, .13f), new Vector2(.305f, .87f), rankColor);
            var icon=new GameObject("Viewer avatar",typeof(RectTransform),typeof(RawImage));icon.transform.SetParent(avatarFrame,false);
            portraits[i]=icon.GetComponent<RawImage>();portraits[i].raycastTarget=false;
            var rect=icon.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(4,4);rect.offsetMax=new Vector2(-4,-4);
            rowNames[i] = Headline(card, "", 24, new Vector2(.34f, .57f), new Vector2(.96f, .93f), Color.white);
            rowStats[i] = Headline(card, "", 16, new Vector2(.34f, .32f), new Vector2(.96f, .58f), Muted);
            rowPowers[i] = Headline(card, "", 13, new Vector2(.34f, .04f), new Vector2(.96f, .23f), rankColor);
            Strip(card, "Health track", new Vector2(.34f, .23f), new Vector2(.95f, .31f), new Color(.02f, .028f, .044f));
            var health=new GameObject("Live health",typeof(RectTransform),typeof(Image));health.transform.SetParent(card,false);rowHealth[i]=health.GetComponent<Image>();rowHealth[i].color=new Color(.18f,.85f,.63f);rowHealth[i].raycastTarget=false;
            var bar=health.GetComponent<RectTransform>();bar.anchorMin=new Vector2(.34f,.25f);bar.anchorMax=new Vector2(.95f,.29f);bar.offsetMin=bar.offsetMax=Vector2.zero;
        }
        var goal = Panel(root.transform, "Mode goal",
            portrait ? new Vector2(.03f,.694f) : new Vector2(.34f,.854f),
            portrait ? new Vector2(.97f,.728f) : new Vector2(.76f,.911f), Teal);
        Headline(goal, arena.ModeGoal, 20, new Vector2(.04f,.09f), new Vector2(.96f,.90f), Color.white);
        var feedPanel = Panel(root.transform, "Elimination feed",
            portrait ? new Vector2(.03f, .112f) : new Vector2(.025f, .035f),
            portrait ? new Vector2(.49f, .186f) : new Vector2(.31f, .158f), Bronze);
        Headline(feedPanel, "ELENENLER", 15, new Vector2(.055f, .71f), new Vector2(.95f, .93f), Bronze);
        events = Label(feedPanel, "", 18, new Vector2(.055f, .08f), new Vector2(.95f, .71f), Muted);
        events.alignment = TextAnchor.UpperLeft;
        feedPanel.gameObject.SetActive(false);

        entrance = Label(root.transform, "", 25,
            portrait ? new Vector2(.53f, .74f) : new Vector2(.36f, .717f),
            portrait ? new Vector2(.96f, .79f) : new Vector2(.76f, .767f), Gold);

        var supportPanel = Panel(root.transform, "Supporters",
            portrait ? new Vector2(.53f, .063f) : new Vector2(.79f, .065f),
            portrait ? new Vector2(.97f, .109f) : new Vector2(.975f, .165f), Teal);
        Headline(supportPanel, "DESTEKÇİLER", 14, new Vector2(.055f, .62f), new Vector2(.95f, .92f), Teal);
        support = Label(supportPanel, "", 16, new Vector2(.055f, .08f), new Vector2(.95f, .64f), Muted);
        support.alignment = TextAnchor.UpperLeft;
        supportPanel.gameObject.SetActive(false);
        connectionInfo=Label(root.transform,"",14,portrait?new Vector2(.04f,.03f):new Vector2(.50f,.015f),portrait?new Vector2(.94f,.065f):new Vector2(.975f,.055f),new Color(.8f,.85f,.9f));
        var win = Panel(root.transform,"Champion",portrait?new Vector2(.08f,.4f):new Vector2(.30f,.38f),portrait?new Vector2(.92f,.61f):new Vector2(.70f,.65f),Gold);
        champion = Headline(win,"",30,new Vector2(.30f,.08f),new Vector2(.96f,.92f),Gold);
        var winnerImage=new GameObject("Champion avatar",typeof(RectTransform),typeof(RawImage));winnerImage.transform.SetParent(win,false);championPortrait=winnerImage.GetComponent<RawImage>();championPortrait.raycastTarget=false;
        var winnerRect=winnerImage.GetComponent<RectTransform>();winnerRect.anchorMin=new Vector2(.04f,.25f);winnerRect.anchorMax=new Vector2(.27f,.75f);winnerRect.offsetMin=winnerRect.offsetMax=Vector2.zero;
        BuildGiftGuide(root.transform, portrait);
        var notice=Panel(root.transform,"Gift notification",portrait?new Vector2(.51f,.115f):new Vector2(.35f,.772f),portrait?new Vector2(.97f,.185f):new Vector2(.76f,.842f),Gold);
        giftGroup=notice.gameObject.AddComponent<CanvasGroup>();giftGroup.alpha=0;
        giftNotice=Label(notice,"",24,new Vector2(.04f,.05f),new Vector2(.96f,.95f),new Color(1,.8f,.42f));
        winnerPanel = win.gameObject; winnerPanel.SetActive(false);
    }
    void Update()
    {
        if(canvasRoot!=null&&portraitLayout!=(Screen.width<Screen.height))BuildHud();
        if(giftGroup!=null)
        {
            if(Time.time>=noticeUntil&&giftNotices.Count>0){giftNotice.text=giftNotices.Dequeue();noticeUntil=Time.time+3.5f;}
            giftGroup.alpha=Mathf.MoveTowards(giftGroup.alpha,Time.time<noticeUntil?1:0,Time.deltaTime*4);
            giftGroup.transform.localScale=Vector3.one*Mathf.Lerp(.94f,1,giftGroup.alpha);
        }
        if (arena == null || Time.time < nextUpdate) return;
        nextUpdate = Time.time + .2f;
        RefreshGiftGuide();
        var ranking=arena.GetRankedStandings();
        connectionInfo.text=ArenaUiText.Safe(ArenaLiveBridge.Current!=null?ArenaLiveBridge.Current.Status:"Yerel test")+" / BEKLEYEN: "+ArenaGiftInbox.Instance.PendingUnits;
        counter.text = "HAYATTA  <b>" + arena.LivingCount + "</b> / " + arena.Fighters.Count;
        float elapsed = arena.RemainingSeconds;
        clockCaption.text = arena.IsTournament ? "KALAN SÜRE" : $"TUR {round}";
        clock.text = $"{(int)elapsed / 60:00}:{(int)elapsed % 60:00}";
        clock.color = elapsed <= 10f && (!arena.IsTournament || arena.DuelCombatStarted)
            ? Color.Lerp(Gold, new Color(1f, .32f, .23f), Mathf.PingPong(Time.unscaledTime * 2f, 1f))
            : Color.white;
        if (arena.IsTournament)
        {
            counter.text = arena.TournamentHeading;
            if (!arena.DuelCombatStarted) clock.text = "—";
        }
        if (!arena.IsTournament)
        {
            counter.text = arena.Mode == ArenaMode.Survival
                ? counter.text : ArenaUiText.Safe(arena.ModeScore);
        }
        for(int i=0;i<3;i++)rowNames[i].transform.parent.gameObject.SetActive(i<ranking.Count);

        for(int i=0;i<Mathf.Min(3,ranking.Count);i++)
        {
            var row=ranking[i];var fighter=row.fighter;
            var catalog=ArenaContent.Load();
            portraits[i].texture=(fighter==null||!fighter.IsZombie)&&catalog!=null&&catalog.fighterPortraits!=null&&row.visualIndex<catalog.fighterPortraits.Length?catalog.fighterPortraits[row.visualIndex]:null;
            var viewerPhoto=viewerProfiles.Resolve(row.id,row.avatarUrl);
            if(viewerPhoto!=null)portraits[i].texture=viewerPhoto;
            portraits[i].enabled=portraits[i].texture!=null;
            portraits[i].color=fighter!=null&&fighter.IsAlive?Color.white:new Color(.4f,.4f,.4f);
            bool alive=fighter!=null&&fighter.IsAlive;
            float fraction=alive?Mathf.Clamp01(fighter.Health/fighter.MaxHealth):0;
            bool zombie = fighter!=null && fighter.IsZombie;
            rowNames[i].text=ArenaUiText.Short(row.name)+(zombie?" [ZOMBİ]":fighter!=null&&fighter.IsBot?" [BOT]":"");
            rowNames[i].color=alive?(zombie?new Color(.71f,.91f,.43f):Color.white):new Color(.45f,.48f,.52f);
            rowStats[i].text=row.points.ToString("0")+" PUAN / "+row.kills+" KO / "+(alive?fighter.Health.ToString("0")+" HP":"ELENDİ");
            string powers=fighter!=null&&fighter.PowerUps!=null?fighter.PowerUps.ActiveNames:"";
            rowPowers[i].text=(fighter!=null&&fighter.Role!=ArenaFighterRole.Basic?fighter.Role.ToString()+" / ":"")+(powers.Length>32?powers.Substring(0,32)+"...":powers);
            var healthRect=rowHealth[i].rectTransform;healthRect.anchorMax=new Vector2(.34f+.61f*fraction,.29f);
            rowHealth[i].color=Color.Lerp(new Color(.95f,.2f,.17f),new Color(.18f,.85f,.63f),fraction);
        }
        events.text = string.Join("\n",feed);
        events.transform.parent.gameObject.SetActive(feed.Count > 0);
        supporterNames.Sort((a,b)=>supporters[b].CompareTo(supporters[a]));
        support.transform.parent.gameObject.SetActive(supporterNames.Count > 0);
        support.text="";
        for(int i=0;i<Mathf.Min(2,supporterNames.Count);i++)support.text+=ArenaUiText.Short(supporterNames[i])+"  •  "+supporters[supporterNames[i]]+" hediye\n";
        entrance.text="";
        foreach(var fighter in arena.Fighters)if(fighter!=null&&fighter.IsEntering&&fighter.gameObject.activeInHierarchy){entrance.text="ARENAYA GİRİYOR  »  "+ArenaUiText.Short(fighter.DisplayName);break;}
        if (arena.IsTournament) entrance.text = ArenaUiText.Safe(arena.TournamentMessage);
        else if (arena.IsFinished || string.IsNullOrEmpty(entrance.text))
            entrance.text = ArenaUiText.Safe(!string.IsNullOrEmpty(arena.JoinQueue) ? arena.JoinQueue : arena.ModeScore);
        if (!arena.IsFinished && Time.time < modeMomentUntil) entrance.text = modeMoment;
        if (arena.IsFinished && !ended)
        {
            ended = true; restartAt = Time.time + 9f;
            if (!string.IsNullOrEmpty(arena.ChampionViewerId))
            {
                string key = "ArenaWins." + arena.ChampionViewerId;
                PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key)+1); PlayerPrefs.Save();
            }
            ArenaAudioEffects.Cheer();
        }
        if (!arena.IsFinished) { ended = false; winnerPanel.SetActive(false); }
        if (ended)
        {
            championPortrait.texture=arena.IsTournament ? viewerProfiles.Resolve(arena.ChampionViewerId, arena.Champion?.AvatarUrl) : portraits[0].texture;championPortrait.enabled=championPortrait.texture!=null;
            winnerPanel.SetActive(Time.time>=restartAt-7.8f);
            champion.text = (arena.IsTournament ? "TURNUVA ŞAMPİYONU" : "ŞAMPİYON") + "\n<b>"+ArenaUiText.Short(arena.ChampionName)+"</b>\n<size=18>"+(!arena.IsTournament && ranking.Count>0?ranking[0].kills+" KO / "+ranking[0].damage.ToString("0")+" HASAR":"")+"</size>\n<size=21>Yeni tur: "+Mathf.CeilToInt(restartAt-Time.time)+"</size>";
            if (!string.IsNullOrEmpty(arena.ModeResult))
                champion.text = "<b>" + ArenaUiText.Safe(arena.ModeResult) + "</b>";
            champion.text += "\n<size=17>" + ArenaUiText.Safe(arena.ModeAchievements) + "</size>";
            if (!string.IsNullOrEmpty(arena.ModeResult))
                champion.text += "\n<size=19>Yeni tur: " + Mathf.CeilToInt(restartAt-Time.time) + "  !hazır</size>";
            if (Time.time >= restartAt) arena.AdvanceMode();
        }
    }
    RectTransform Panel(Transform parent, string name, Vector2 min, Vector2 max, Color accent)
    {
        var rect = Place(parent, name, min, max, typeof(ArenaPanelGraphic));
        var graphic = rect.GetComponent<ArenaPanelGraphic>();
        graphic.accent = accent;
        graphic.raycastTarget = false;
        return rect;
    }

    void Backdrop(Transform parent, Vector2 min, Vector2 max, bool fadeUp)
    {
        var rect = Place(parent, "Portrait gradient", min, max, typeof(ArenaPanelGraphic));
        var graphic = rect.GetComponent<ArenaPanelGraphic>();
        graphic.backdrop = true;
        graphic.fadeUp = fadeUp;
        graphic.raycastTarget = false;
    }

    void Strip(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var image = Place(parent, name, min, max, typeof(Image)).GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    static RectTransform Place(Transform parent, string name, Vector2 min, Vector2 max, System.Type component)
    {
        var obj = new GameObject(name, typeof(RectTransform), component);
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    Text Headline(Transform parent, string text, int size, Vector2 min, Vector2 max, Color color)
    {
        var label = Label(parent, text, size, min, max, color);
        label.fontStyle = FontStyle.Bold;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(10, size * 2 / 3);
        label.resizeTextMaxSize = size;
        return label;
    }

    Text Label(Transform parent,string text,int size,Vector2 min,Vector2 max,Color? color=null)
    {
        var obj=new GameObject("Text",typeof(RectTransform),typeof(Text));obj.transform.SetParent(parent,false);
        var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var label=obj.GetComponent<Text>();label.font=font;label.fontSize=size;label.color=color??Color.white;
        label.text=text;label.supportRichText=true;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
        return label;
    }
}
