using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public sealed class ArenaBroadcastHUD : MonoBehaviour
{
    static ArenaBroadcastHUD instance;
    readonly Queue<string> feed = new Queue<string>();
    readonly Dictionary<string,int> supporters = new Dictionary<string,int>();
    readonly List<string> supporterNames = new List<string>();
    BattleArenaManager arena;
    Text counter, clock, leaders, events, champion, support, entrance,connectionInfo;
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
    Sprite panelSprite;
    Texture2D panelTexture;
    readonly Queue<string> giftNotices=new Queue<string>();
    Text giftNotice;
    CanvasGroup giftGroup;
    float noticeUntil;
    public static void NotifyGift(string sender,string gift,string benefit)
    {
        if(instance==null)return;
        if(instance.giftNotices.Count<24)instance.giftNotices.Enqueue(ArenaUiText.Short(sender)+"  /  "+ArenaUiText.Safe(gift)+"\n<size=18>"+ArenaUiText.Safe(benefit)+"</size>");
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
    void OnDestroy() { if (instance == this) instance = null;if(panelSprite!=null)Destroy(panelSprite);if(panelTexture!=null)Destroy(panelTexture); }
    void Start()
    {
        viewerProfiles=gameObject.AddComponent<ArenaViewerProfiles>();
        gameObject.AddComponent<ArenaNameplateLayout>();
        arena = GetComponent<BattleArenaManager>(); started = Time.time; round++;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        panelTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);
        panelTexture.wrapMode=TextureWrapMode.Repeat;
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float grain=((x*73+y*31+x*y*17)%97)/970f;
            float value=.52f+grain+(y%8==0?.08f:0);
            panelTexture.SetPixel(x,y,new Color(value,value,value,1));
        }
        panelTexture.Apply();panelSprite=Sprite.Create(panelTexture,new Rect(0,0,64,64),Vector2.one*.5f,64);
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
        if(portrait)
        {
            Backdrop(root.transform,new Vector2(0,0),new Vector2(1,.19f));
            Backdrop(root.transform,new Vector2(0,.73f),new Vector2(1,1));
        }
        var brand=Panel(root.transform,"Tournament brand",portrait?new Vector2(.03f,.94f):new Vector2(.025f,.91f),portrait?new Vector2(.62f,.985f):new Vector2(.29f,.98f),Color.white);
        Label(brand,"ARENA <color=#E7C979>CHAOS</color>",30,new Vector2(.06f,.25f),new Vector2(.95f,.95f));
        Label(brand,"THE PALACE  /  SURVIVAL TOURNAMENT",11,new Vector2(.07f,.05f),new Vector2(.95f,.32f),new Color(.6f,.75f,.8f));
        var roundPlate=Panel(root.transform,"Round clock",portrait?new Vector2(.64f,.94f):new Vector2(.79f,.91f),portrait?new Vector2(.97f,.985f):new Vector2(.975f,.98f),Color.white);
        clock=Label(roundPlate,"",22,new Vector2(.08f,.05f),new Vector2(.95f,.95f));
        var alivePlate=Panel(root.transform,"Survivors",portrait?new Vector2(.54f,.895f):new Vector2(.40f,.925f),portrait?new Vector2(.97f,.93f):new Vector2(.60f,.98f),Color.white);
        counter=Label(alivePlate,"",24,new Vector2(.08f,.05f),new Vector2(.95f,.95f));counter.alignment=TextAnchor.MiddleCenter;
        var board=new GameObject("Ranking cards",typeof(RectTransform)).GetComponent<RectTransform>();board.SetParent(root.transform,false);
        board.anchorMin=portrait?new Vector2(.03f,.73f):new Vector2(.025f,.56f);board.anchorMax=portrait?new Vector2(.50f,.93f):new Vector2(.255f,.89f);board.offsetMin=board.offsetMax=Vector2.zero;
        Label(board,"ARENA SIRALAMASI",16,new Vector2(.03f,.91f),new Vector2(.98f,1),new Color(.8f,.86f,.91f));
        for(int i=0;i<3;i++)
        {
            float bottom=.025f+(2-i)*.29f;
            var card=Panel(board,"Rank "+(i+1),new Vector2(0,bottom),new Vector2(1,bottom+.275f),Color.white);
            Label(card,(i+1).ToString("00"),15,new Vector2(.025f,.58f),new Vector2(.10f,.94f),new Color(.9f,.75f,.35f));
            var icon=new GameObject("Viewer avatar",typeof(RectTransform),typeof(RawImage));icon.transform.SetParent(card,false);
            portraits[i]=icon.GetComponent<RawImage>();portraits[i].raycastTarget=false;
            var rect=icon.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.105f,.16f);rect.anchorMax=new Vector2(.26f,.86f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            rowNames[i]=Label(card,"",19,new Vector2(.29f,.58f),new Vector2(.96f,.95f));
            rowStats[i]=Label(card,"",13,new Vector2(.29f,.30f),new Vector2(.96f,.58f),new Color(.70f,.83f,.87f));
            rowPowers[i]=Label(card,"",11,new Vector2(.29f,.04f),new Vector2(.96f,.26f),new Color(.95f,.73f,.35f));
            var health=new GameObject("Live health",typeof(RectTransform),typeof(Image));health.transform.SetParent(card,false);rowHealth[i]=health.GetComponent<Image>();rowHealth[i].color=new Color(.18f,.85f,.63f);rowHealth[i].raycastTarget=false;
            var bar=health.GetComponent<RectTransform>();bar.anchorMin=new Vector2(.29f,.27f);bar.anchorMax=new Vector2(.95f,.30f);bar.offsetMin=bar.offsetMax=Vector2.zero;
        }
        events = Label(root.transform,"",21,portrait?new Vector2(.04f,.115f):new Vector2(.03f,.035f),portrait?new Vector2(.94f,.18f):new Vector2(.65f,.15f));
        events.alignment = TextAnchor.LowerLeft;
        entrance=Label(root.transform,"",25,portrait?new Vector2(.53f,.74f):new Vector2(.36f,.80f),portrait?new Vector2(.96f,.79f):new Vector2(.96f,.87f),new Color(1,.8f,.4f));
        support=Label(root.transform,"",18,portrait?new Vector2(.04f,.065f):new Vector2(.71f,.075f),portrait?new Vector2(.94f,.11f):new Vector2(.975f,.18f));
        support.alignment=TextAnchor.LowerRight;
        connectionInfo=Label(root.transform,"",14,portrait?new Vector2(.04f,.03f):new Vector2(.50f,.015f),portrait?new Vector2(.94f,.065f):new Vector2(.975f,.055f),new Color(.8f,.85f,.9f));
        var win = Panel(root.transform,"Champion",portrait?new Vector2(.08f,.4f):new Vector2(.30f,.38f),portrait?new Vector2(.92f,.61f):new Vector2(.70f,.65f),new Color(.12f,.115f,.10f,.94f));
        champion = Label(win,"",32,new Vector2(.30f,.08f),new Vector2(.96f,.92f),new Color(1,.76f,.35f));
        var winnerImage=new GameObject("Champion avatar",typeof(RectTransform),typeof(RawImage));winnerImage.transform.SetParent(win,false);championPortrait=winnerImage.GetComponent<RawImage>();championPortrait.raycastTarget=false;
        var winnerRect=winnerImage.GetComponent<RectTransform>();winnerRect.anchorMin=new Vector2(.04f,.25f);winnerRect.anchorMax=new Vector2(.27f,.75f);winnerRect.offsetMin=winnerRect.offsetMax=Vector2.zero;
        var notice=Panel(root.transform,"Gift notification",portrait?new Vector2(.53f,.80f):new Vector2(.35f,.81f),portrait?new Vector2(.97f,.885f):new Vector2(.78f,.90f),new Color(.15f,.09f,.04f,.9f));
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
        var ranking=arena.GetRankedStandings();
        connectionInfo.text=ArenaUiText.Safe(ArenaLiveBridge.Current!=null?ArenaLiveBridge.Current.Status:"Yerel test")+" / BEKLEYEN: "+ArenaGiftInbox.Instance.PendingUnits;
        counter.text = "HAYATTA  <b>" + arena.LivingCount + "</b> / " + arena.Fighters.Count;
        float elapsed = arena.RemainingSeconds;
        clock.text = "TUR " + round + "  •  " + ((int)elapsed/60).ToString("00") + ":" + ((int)elapsed%60).ToString("00");
        for(int i=0;i<3;i++)rowNames[i].transform.parent.gameObject.SetActive(i<ranking.Count);

        for(int i=0;i<Mathf.Min(3,ranking.Count);i++)
        {
            var row=ranking[i];var fighter=row.fighter;
            var catalog=ArenaContent.Load();
            portraits[i].texture=catalog!=null&&catalog.fighterPortraits!=null&&row.visualIndex<catalog.fighterPortraits.Length?catalog.fighterPortraits[row.visualIndex]:null;
            var viewerPhoto=viewerProfiles.Resolve(row.id,row.avatarUrl);
            if(viewerPhoto!=null)portraits[i].texture=viewerPhoto;
            portraits[i].enabled=portraits[i].texture!=null;
            portraits[i].color=fighter!=null&&fighter.IsAlive?Color.white:new Color(.4f,.4f,.4f);
            bool alive=fighter!=null&&fighter.IsAlive;
            float fraction=alive?Mathf.Clamp01(fighter.Health/fighter.MaxHealth):0;
            rowNames[i].text=ArenaUiText.Short(row.name)+(fighter!=null&&fighter.IsBot?" [BOT]":"");rowNames[i].color=alive?Color.white:new Color(.45f,.48f,.52f);
            rowStats[i].text=row.points.ToString("0")+" PUAN / "+row.kills+" KO / "+(alive?fighter.Health.ToString("0")+" HP":"ELENDİ");
            string powers=fighter!=null&&fighter.PowerUps!=null?fighter.PowerUps.ActiveNames:"";
            rowPowers[i].text=(fighter!=null&&fighter.Role!=ArenaFighterRole.Basic?fighter.Role.ToString()+" / ":"")+(powers.Length>32?powers.Substring(0,32)+"...":powers);
            var healthRect=rowHealth[i].rectTransform;healthRect.anchorMax=new Vector2(.29f+.66f*fraction,.30f);
            rowHealth[i].color=Color.Lerp(new Color(.95f,.2f,.17f),new Color(.18f,.85f,.63f),fraction);
        }
        events.text = string.Join("\n",feed);
        supporterNames.Sort((a,b)=>supporters[b].CompareTo(supporters[a]));
        support.text="";
        for(int i=0;i<Mathf.Min(2,supporterNames.Count);i++)support.text+=ArenaUiText.Short(supporterNames[i])+"  •  "+supporters[supporterNames[i]]+" hediye\n";
        entrance.text="";
        foreach(var fighter in arena.Fighters)if(fighter!=null&&fighter.IsEntering&&fighter.gameObject.activeInHierarchy){entrance.text="ARENAYA GİRİYOR  »  "+ArenaUiText.Short(fighter.DisplayName);break;}
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
            championPortrait.texture=portraits[0].texture;championPortrait.enabled=championPortrait.texture!=null;
            winnerPanel.SetActive(Time.time>=restartAt-7.8f);
            champion.text = "ŞAMPİYON\n<b>"+ArenaUiText.Short(arena.ChampionName)+"</b>\n<size=18>"+(ranking.Count>0?ranking[0].kills+" KO / "+ranking[0].damage.ToString("0")+" HASAR":"")+"</size>\n<size=21>Yeni tur: "+Mathf.CeilToInt(restartAt-Time.time)+"</size>";
            if (Time.time >= restartAt) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(ArenaPanelGraphic));obj.transform.SetParent(parent,false);
        var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        obj.GetComponent<ArenaPanelGraphic>().raycastTarget=false;
        var backing=new GameObject("Steel plate backing",typeof(RectTransform),typeof(Image));backing.transform.SetParent(obj.transform,false);
        var backRect=backing.GetComponent<RectTransform>();backRect.anchorMin=Vector2.zero;backRect.anchorMax=Vector2.one;backRect.offsetMin=new Vector2(4,4);backRect.offsetMax=new Vector2(-4,-4);
        backing.GetComponent<Image>().color=new Color(.025f,.04f,.06f,.92f);backing.GetComponent<Image>().raycastTarget=false;
        var edge=backing.AddComponent<Outline>();edge.effectColor=new Color(.25f,.38f,.45f,.9f);edge.effectDistance=new Vector2(1,-1);
        var accent=new GameObject("Metal inlay",typeof(RectTransform),typeof(Image));accent.transform.SetParent(obj.transform,false);
        var accentRect=accent.GetComponent<RectTransform>();accentRect.anchorMin=new Vector2(.035f,.96f);accentRect.anchorMax=new Vector2(.28f,.98f);accentRect.offsetMin=accentRect.offsetMax=Vector2.zero;accent.GetComponent<Image>().color=new Color(.85f,.72f,.37f);accent.GetComponent<Image>().raycastTarget=false;
        return rect;
    }
    void Backdrop(Transform parent,Vector2 min,Vector2 max)
    {
        var obj=new GameObject("Portrait backdrop",typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
        var rect=obj.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=obj.GetComponent<Image>();image.color=new Color(.025f,.04f,.055f);image.raycastTarget=false;
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
