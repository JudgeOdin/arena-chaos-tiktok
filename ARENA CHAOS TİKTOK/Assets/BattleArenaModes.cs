using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ArenaMode { Survival, Tournament, King, Zombies, Gold, Bridge, Giant, Bounty, Classes, Rematch, GuestChampion }

public partial class BattleArenaManager
{
    static readonly string[] Names = { "Herkese Karşı", "Turnuva", "Kralı Koru", "Zombi İstilası", "Altın Kaçakçısı", "Köprü Savunması", "Dev Avı", "Kelle Avı", "Sınıf Seçimi", "Şampiyonun Rövanşı", "Yayıncı Şampiyonu" };
    static readonly string[] Goals = { "KO ve hasarla puan topla.", "Sekiz oyunculu elemeyi kazan.", "Kralını koru ve rakip kralı düşür.", "İstiladan süre sonuna kadar kurtul.", "Altını kendi kasana üç kez teslim et.", "Kapıyı üç dalgaya karşı koru.", "İşaretli saldırılardan kaç ve devi yen.", "İşareti taşıyarak puan kazan.", "Sınıfını seç ve maç puanı topla.", "Skorda öne geç ve tacı ele geçir.", "Davet edilen yayıncı şampiyonunu yen." };
    static readonly Color GoldAccent = new Color(1f,.70f,.16f);
    static readonly Color Blue = new Color(.2f,.55f,1f), Red = new Color(1f,.3f,.22f);
    sealed class Entrant { public string id, name, avatar; public int side; public ArenaFighterRole role; }
    static readonly Dictionary<string,Entrant> Ready = new Dictionary<string,Entrant>();
    static readonly Dictionary<string,string> Challenges = new Dictionary<string,string>();
    static readonly Dictionary<string,Entrant> DuelViewers=new Dictionary<string,Entrant>();
    static readonly Queue<(string left,string right)> Duels = new Queue<(string,string)>();
    static readonly Queue<Entrant> Rules = new Queue<Entrant>();
    static string previousWinner, nextRule, nextRuleSender;
    static bool loadedRules;
    const string RulePreference = "ArenaPendingRules";
    [Serializable] sealed class RuleState
    {
        public string selected, sender;
        public List<RuleGrant> pending = new List<RuleGrant>();
    }
    [Serializable] sealed class RuleGrant { public string id, name; }

    static void LoadRules()
    {
        if (loadedRules) return;
        loadedRules=true;
        if (ArenaLiveBridge.IsOffline) return;
        try
        {
            var saved=JsonUtility.FromJson<RuleState>(PlayerPrefs.GetString(RulePreference,""));
            if (saved==null) return;
            nextRule=saved.selected;
            nextRuleSender=saved.sender;
            foreach(var item in saved.pending) Rules.Enqueue(new Entrant { id=item.id,name=item.name });
        }
        catch (ArgumentException e) { Debug.LogWarning(e.Message); }
    }

    static void SaveRules()
    {
        if (ArenaLiveBridge.IsOffline) return;
        var saved=new RuleState {
            selected=nextRule, sender=nextRuleSender,
            pending=Rules.Select(x=>new RuleGrant { id=x.id,name=x.name }).ToList()
        };
        PlayerPrefs.SetString(RulePreference,JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
    }
    readonly Dictionary<string,Entrant> entries = new Dictionary<string,Entrant>();
    readonly Queue<Entrant> waiting = new Queue<Entrant>();
    readonly Dictionary<ArenaFighter,int> sides = new Dictionary<ArenaFighter,int>();
    readonly Dictionary<string,float> bounty = new Dictionary<string,float>();
    readonly HashSet<string> freeEntrants=new HashSet<string>();
    readonly HashSet<string> supportUsed = new HashSet<string>(), voters = new HashSet<string>();
    readonly Queue<ArenaFighter> infections = new Queue<ArenaFighter>();
    readonly List<string> achievements = new List<string>();
    readonly ArenaFighter[] kings = new ArenaFighter[2];
    readonly int[] deliveries = new int[2], supportVotes = new int[2];
    readonly GameObject[] healing = new GameObject[2];
    readonly float[] healUntil = new float[2];
    ArenaFighter boss, gate, carrier, marked, crown, duelLeft, duelRight;
    GameObject warning, objective, meteorWarning;
    ArenaFighter meteorCaster;
    int meteorSide;
    ArenaModeEmblem gold, specialMarker;
    Vector3 goldAt, warningPosition, objectivePosition;
    float warningAt, strikeAt, voteUntil, classUntil, radius, defense, objectiveAt, ruleUntil;
    int wave, repair, archer;
    bool hadPlayers, firstKo, duelResolved, finalThreeRecorded;
    string rule, ruleSender, modeResult, previousLeader;
    Entrant ruleRequest;
    public ArenaMode Mode { get; private set; }
    bool modeTransitioning;
    public string ModeLabel => ModeName(Mode);
    public static string ModeName(ArenaMode mode) => Names[(int)mode];
    public string ModeGoal => Goals[(int)Mode] + (IsTournament ? "  •  Yayını beğen" : "  •  !katıl");
    public string ModeResult => modeResult;
    public string ModeAchievements => string.Join(" • ", achievements.Take(3));
    bool TeamMode => Mode >= ArenaMode.King && Mode <= ArenaMode.Giant || Mode == ArenaMode.GuestChampion;
    bool TwoTeams => Mode == ArenaMode.King || Mode == ArenaMode.Gold;
    public bool ModeCombatOpen => IsTournament ? DuelCombatStarted : Time.time >= classUntil;
    public bool CanSwitchIdleTournament => IsTournament && tournamentViewerIds.Count==0 && !IsFinished;
    public float ModeRadius => radius > 0 ? radius : arenaRadius;
    public bool CanAttack(ArenaFighter source, ArenaFighter target) => source != null && target != null && source != target && ModeCombatOpen && (!TeamMode || SideOf(source) != SideOf(target));
    public ArenaFighter DuelOpponent(ArenaFighter actor)
    {
        if (duelResolved) return null;
        var enemy=actor==duelLeft?duelRight:actor==duelRight?duelLeft:null;
        return Alive(enemy)?enemy:null;
    }
    public bool Stationary(ArenaFighter actor) => actor == gate || actor == boss && warning!=null;
    public float ModeSpeed(ArenaFighter actor) => actor == carrier ? .58f : rule == "hız" ? 1.3f : 1f;
    int SideOf(ArenaFighter actor) => actor != null && sides.TryGetValue(actor, out int side) ? side : 0;
    static bool Alive(ArenaFighter x) => x != null && x.IsAlive && x.gameObject.activeInHierarchy;
    static bool LivingViewer(ArenaFighter x) => x!=null && x.IsAlive && (x.IsEntering || x.gameObject.activeInHierarchy) &&
        !x.IsBot && !string.IsNullOrEmpty(x.ViewerId);
    static bool Viewer(ArenaFighter x) => LivingViewer(x) && x.gameObject.activeInHierarchy && !x.IsEntering;
    static float Fraction(ArenaFighter x) => x != null && x.MaxHealth > 0 ? x.Health / x.MaxHealth : 0;
    static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x,a.z), new Vector2(b.x,b.z));
    Vector3 Base(int side) => new Vector3((side == 0 ? -1 : 1) * arenaRadius * .6f, 1, 0);
    static string Team(int side) => side == 0 ? "Mavi" : "Kırmızı";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetModes()
    {
        Ready.Clear(); Challenges.Clear(); DuelViewers.Clear(); Duels.Clear(); Rules.Clear(); shieldTargets.Clear();
        previousWinner = nextRule = nextRuleSender = null;
        loadedRules=false;
    }

    void LoadMode()
    {
        Mode = (ArenaMode)Mathf.Clamp(PlayerPrefs.GetInt("ArenaActiveMode", PlayerPrefs.GetInt(TournamentPreference)), 0, Names.Length - 1);
        if (Array.IndexOf(Environment.GetCommandLineArgs(),"-arenaSmokeTest")>=0) Mode=ArenaMode.Survival;
        IsTournament = Mode == ArenaMode.Tournament;
        LoadRules();
    }

    public void RequestMode(ArenaMode mode)
    {
        PlayerPrefs.SetInt("ArenaNextMode", (int)mode);
        PlayerPrefs.Save();
    }

    public void AdvanceMode()
    {
        if(modeTransitioning)return;
        modeTransitioning=true;
        previousWinner = Champion != null && !Champion.IsBot ? ChampionViewerId : null;
        ArenaBroadcastMetrics.EndRound(roundEntrants);
        foreach (var entry in waiting) Ready[entry.id]=entry;
        int next = PlayerPrefs.GetInt("ArenaNextMode", -1);
        if (next < 0)
        {
            int[] rotation = PlayerPrefs.GetString("ArenaRotation", "").Split(',')
                .Select(x => int.TryParse(x, out int n) ? n : -1).Where(x => x >= 0 && x < Names.Length).Distinct().ToArray();
            if (rotation.Length != 0) next = rotation[(Array.IndexOf(rotation,(int)Mode)+1)%rotation.Length];
        }
        if (next >= 0)
        {
            PlayerPrefs.SetInt("ArenaActiveMode", next);
            PlayerPrefs.SetInt(TournamentPreference, next == 1 ? 1 : 0);
            PlayerPrefs.DeleteKey("ArenaNextMode");
            PlayerPrefs.Save();
        }
        if (ruleRequest != null && nextRule == null) SetRule("hız");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SwitchToRequestedModeNow()
    {
        if(!PlayerPrefs.HasKey("ArenaNextMode"))return;
        foreach(var actor in fighters)
            if(LivingViewer(actor))Ready[actor.ViewerId]=Entry(actor.ViewerId,actor.DisplayName,actor.AvatarUrl);
        Time.timeScale=1;
        AdvanceMode();
    }

    void StartMode()
    {
        radius = arenaRadius;
        confirmedDuels=Duels.Count;
        if (!TeamMode && confirmedDuels>0)
            foreach (var pair in Duels)
                foreach (var id in new[]{pair.left,pair.right})
                    if (DuelViewers.TryGetValue(id,out var participant)) QueueEntry(id,participant.name,participant.avatar);
        if (!TeamMode) SpawnFighters();
        else
        {
            var original = GameObject.Find("PLAYER");
            if (original != null) original.SetActive(false);
        }

        switch (Mode)
        {
            case ArenaMode.King:
                for (int side = 0; side < 2; side++)
                {
                    kings[side] = SpawnObjective(Team(side) + " Kral",side,Base(side),220,royal:true);
                    SpawnObjective(Team(side) + " Muhafız",side,Base(side)+Vector3.forward*2,25,ArenaFighterRole.Guardian);
                    SpawnObjective(Team(side) + " Okçu",side,Base(side)+Vector3.back*2,15,ArenaFighterRole.Archer);
                }
                break;
            case ArenaMode.Zombies:
                for (int i = 0; i < 3; i++) SpawnObjective("Zombi "+(i+1),1,Base(1)+Vector3.forward*(i-1)*2,15);
                break;
            case ArenaMode.Gold:
                SpawnObjective("Mavi takviye",0,Base(0),10);
                SpawnObjective("Kırmızı takviye",1,Base(1),10);
                Marker("Mavi kasa",Base(0),Blue,2.7f);
                Marker("Kırmızı kasa",Base(1),Red,2.7f);
                goldAt = Vector3.up;
                gold = ArenaModeEmblem.Create(transform,ArenaModeEmblem.Shape.Ingot,new Color(1f,.70f,.16f));
                gold.SetAnchor(null,0, goldAt);
                break;
            case ArenaMode.Bridge:
                gate = SpawnObjective("Savunma kapısı",0,Base(0),350);
                AttachEmblem(gate,ArenaModeEmblem.Shape.Gate,Blue);
                break;
            case ArenaMode.Giant:
            case ArenaMode.GuestChampion:
                string guest = PlayerPrefs.GetString("ArenaGuestChampion","").Trim();
                boss = SpawnObjective(Mode == ArenaMode.GuestChampion && guest.Length > 0 ? guest : "Arena Devi",1,Vector3.up,450);
                boss.transform.localScale *= 1.55f;
                AttachEmblem(boss,ArenaModeEmblem.Shape.Boss,Red,2.8f);
                strikeAt = Time.time + 7;
                break;
            case ArenaMode.Classes:
                classUntil = Time.time + 12;
                roundEndsAt += 12;
                break;
        }
        if (!TeamMode && nextRule != null)
        {
            rule = nextRule; ruleSender = nextRuleSender;
            nextRule = nextRuleSender = null;
            SaveRules();
            if (rule == "hız") roundEndsAt = Mathf.Min(roundEndsAt,Time.time+90);
            if (rule == "dar") radius *= .6f;
            ArenaBroadcastHUD.NotifyGift(ruleSender,"TUR KURALI",rule);
        }
        if (!TeamMode && PlayerPrefs.GetInt("ArenaSideObjectives") == 1) objectiveAt = Time.time + 20;
        foreach (var entry in Ready.Values) QueueEntry(entry.id,entry.name,entry.avatar);
        Ready.Clear();
    }

    ArenaFighter SpawnObjective(string name,int side,Vector3 position,float health,ArenaFighterRole role = ArenaFighterRole.Basic,bool royal = false)
    {
        bool zombie=Mode==ArenaMode.Zombies && side==1;
        var look = royal ? ArenaFighter.Look.Royal : zombie ? ArenaFighter.Look.Zombie :
            Mode==ArenaMode.Bridge && side==0 && gate==null ? ArenaFighter.Look.Gate : ArenaFighter.Look.Standard;
        var actor = CreateViewerFighter(name,side == 0 ? Blue : Red,side == 0 ? 0 : 4,position,look);
        if (look==ArenaFighter.Look.Gate) actor.transform.rotation=Quaternion.Euler(0,90,0);
        actor.IsBot = true;
        sides[actor] = side;
        if (health > 0) actor.GiftIncreaseMaxHealth(health);
        FreeRole(actor,role);
        return actor;
    }

    static void AttachEmblem(ArenaFighter actor, ArenaModeEmblem.Shape shape, Color color, float height = 2.05f) =>
        ArenaModeEmblem.Create(actor.transform,shape,color,height).SetAnchor(actor.transform,height);

    static void FreeRole(ArenaFighter actor,ArenaFighterRole role)
    {
        if (role == ArenaFighterRole.Basic) return;
        var roster = Resources.Load<ArenaGiftRoster>("ArenaChaos/GiftRoster");
        var tier = roster != null ? roster.Find(role) : null;
        if (tier != null) actor.ConfigureRole(tier);
    }

    GameObject Marker(string label,Vector3 position,Color color,float size)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = label;
        marker.transform.SetParent(transform);
        marker.transform.position = new Vector3(position.x,.075f,position.z);
        marker.transform.localScale = new Vector3(size,.045f,size);
        var collider = marker.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        marker.GetComponent<Renderer>().material.color = color;
        return marker;
    }

    Entrant Entry(string id,string name,string avatar)
    {
        if (entries.TryGetValue(id,out var entry)) return entry;
        int blue = entries.Values.Count(x=>x.side==0);
        entry = new Entrant { id=id, name=string.IsNullOrWhiteSpace(name)?id:name, avatar=avatar, side=blue*2<=entries.Count?0:1 };
        entries.Add(id,entry);
        return entry;
    }

    void RegisterModeActor(ArenaFighter actor)
    {
        var entry = Entry(actor.ViewerId,actor.DisplayName,actor.AvatarUrl);
        sides[actor] = TwoTeams ? entry.side : 0;
        hadPlayers = true;
        if (ArenaBroadcastMetrics.Join(actor.ViewerId)) achievements.Add(actor.DisplayName+" • Haftanın katılımcısı");
        if (Mode == ArenaMode.Rematch && actor.ViewerId == previousWinner && crown == null)
            SetCrown(actor);
    }

    bool QueueEntry(string id,string name,string avatar)
    {
        if (IsTournament || IsFinished || string.IsNullOrWhiteSpace(id)) return false;
        if (roundEntrants.Contains(id) || waiting.Any(x=>x.id==id)) return true;
        if (waiting.Count >= 100) return false;
        waiting.Enqueue(Entry(id,name,avatar));
        statusMessage = $"{name} • Katılım sırası {waiting.Count}";
        return true;
    }

    void DeliverEntries()
    {
        if (!ModeCombatOpen) return;
        while (waiting.Count > 0 && LivingCount < 50)
        {
            var entry = waiting.Peek();
            if (roundEntrants.Contains(entry.id)) { waiting.Dequeue(); continue; }
            var actor = SpawnGiftFighter(entry.id,entry.name,entry.avatar,entry.side == 0 ? Blue : Red);
            if (actor == null) return;
            waiting.Dequeue();
            freeEntrants.Add(entry.id);
            if (Mode == ArenaMode.Classes)
            {
                FreeRole(actor,entry.role);
                if (actor.Role==ArenaFighterRole.Archer) AttachEmblem(actor,ArenaModeEmblem.Shape.Archer,Blue);
                if (actor.Role==ArenaFighterRole.Guardian) AttachEmblem(actor,ArenaModeEmblem.Shape.Guardian,GoldAccent);
            }
            ArenaBroadcastHUD.NotifyGift(entry.name,"ARENAYA KATILDI",Mode == ArenaMode.Zombies ? "Hayatta kalan" : TwoTeams ? Team(entry.side) : ModeLabel);
        }
    }

    public bool ProcessChat(string id,string name,string avatar,string message)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(message)) return false;
        var parts = message.Trim().Split(new[]{' '},2,StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLower(new System.Globalization.CultureInfo("tr-TR"));
        string arg = parts.Length > 1 ? parts[1].Trim() : "";
        if ((cmd == "!hazır" || cmd == "!hazir") && IsFinished && !IsTournament)
        {
            Ready[id] = new Entrant { id=id, name=name, avatar=avatar };
            return true;
        }
        if (IsFinished || IsTournament) return false;
        if (cmd == "!katıl" || cmd == "!katil") return QueueEntry(id,name,avatar);
        if (TwoTeams && !roundEntrants.Contains(id) && (cmd == "!mavi" || cmd == "!kırmızı" || cmd == "!kirmizi"))
        {
            Entry(id,name,avatar).side = cmd == "!mavi" ? 0 : 1;
            return QueueEntry(id,name,avatar);
        }
        if (Mode == ArenaMode.Classes && !ModeCombatOpen &&
            (cmd == "!temel" || cmd == "!okçu" || cmd == "!okcu" || cmd == "!muhafız" || cmd == "!muhafiz"))
        {
            Entry(id,name,avatar).role = cmd == "!temel" ? ArenaFighterRole.Basic : cmd.StartsWith("!ok",StringComparison.Ordinal) ? ArenaFighterRole.Archer : ArenaFighterRole.Guardian;
            return QueueEntry(id,name,avatar);
        }
        if (cmd == "!destek" && TeamMode && roundEntrants.Contains(id) && FindViewerFighter(id) == null && supportUsed.Add(id))
        {
            int side = TwoTeams ? entries[id].side : 0;
            ArenaBroadcastMetrics.Support(id);
            if (++supportVotes[side] >= 2)
            {
                supportVotes[side] = 0;
                healUntil[side] = Time.time + 10;
                if (healing[side] == null) healing[side] = Marker("Takım iyileşmesi",Base(side),Color.green,4);
                achievements.Add(Team(side) + " • Takım desteği");
            }
            return true;
        }
        if (Mode == ArenaMode.Bridge && voteUntil > Time.time && roundEntrants.Contains(id) &&
            (cmd == "!tamir" || cmd == "!okçu") && voters.Add(id))
        {
            if (cmd == "!tamir") repair++; else archer++;
            return true;
        }
        if ((cmd == "!düello" || cmd == "!duello") && !TeamMode && roundEntrants.Contains(id))
        {
            var target = entries.Values.FirstOrDefault(e=>e.id!=id && roundEntrants.Contains(e.id) &&
                (e.id==arg || string.Equals(e.name.TrimStart('@'),arg.TrimStart('@'),StringComparison.OrdinalIgnoreCase)));
            if (target == null) return false;
            if (Challenges.TryGetValue(target.id,out var rival) && rival == id)
            {
                Challenges.Remove(target.id); Challenges.Remove(id);
                DuelViewers[id]=entries[id]; DuelViewers[target.id]=target;
                Duels.Enqueue((id,target.id));
                ArenaBroadcastHUD.NotifyGift(name,"DÜELLO ONAYLANDI",target.name);
            }
            else
            {
                Challenges[id] = target.id;
                ArenaBroadcastHUD.NotifyGift(name,"DÜELLO MEYDAN OKUMASI",target.name+" • !düello "+name);
            }
            return true;
        }
        if (cmd == "!kural" && ruleRequest != null && ruleRequest.id == id &&
            (arg == "hız" || arg == "dar" || arg == "merkez"))
        {
            SetRule(arg);
            return true;
        }
        if (cmd == "!hedef" && TeamMode)
        {
            shieldTargets[id] = arg;
            return true;
        }
        return false;
    }

    public bool ModeSteering(ArenaFighter actor,ArenaFighter enemy,out Vector3 direction)
    {
        direction = Vector3.zero;
        Vector3 goal;
        if (TryDodgeHazard(actor,out var escape)) goal = escape;
        else if (carrier == actor) goal = Base(SideOf(actor));
        else if (Mode == ArenaMode.Gold && !Alive(carrier)) goal = goldAt;
        else if (Mode == ArenaMode.Gold && SideOf(carrier) == SideOf(actor) && (enemy == null || Flat(actor.transform.position,enemy.transform.position)>3)) goal = carrier.transform.position+Vector3.forward*1.5f;
        else if (Mode == ArenaMode.Gold && SideOf(carrier) != SideOf(actor) && Flat(actor.transform.position,carrier.transform.position)>3) goal = carrier.transform.position;

        else if (Mode == ArenaMode.King && actor.Role == ArenaFighterRole.Guardian && Alive(kings[SideOf(actor)]) &&
            Flat(actor.transform.position,kings[SideOf(actor)].transform.position)>4)
            goal = kings[SideOf(actor)].transform.position;
        else if (objective != null && (enemy == null || Flat(actor.transform.position,enemy.transform.position)>4)) goal = objectivePosition;
        else return false;
        direction = goal - actor.transform.position;
        direction.y = 0;
        direction = direction.sqrMagnitude > .16f ? direction.normalized : Vector3.zero;
        return true;
    }

    bool TryDodgeHazard(ArenaFighter actor,out Vector3 goal)
    {
        goal=Vector3.zero;
        Vector3 center;
        if (meteorWarning!=null && MeteorTarget(meteorCaster,actor,meteorSide) &&
            Flat(actor.transform.position,meteorWarning.transform.position)<4f)
            center=meteorWarning.transform.position;
        else if (warning!=null && SideOf(actor)==0 && Flat(actor.transform.position,warningPosition)<3.7f)
            center=warningPosition;
        else return false;

        Vector3 outward=actor.transform.position-center;
        outward.y=0;
        if (outward.sqrMagnitude<.01f) outward=actor.transform.right;
        goal=actor.transform.position+outward.normalized*5;
        if (Mathf.Abs(goal.x)>ModeRadius-.8f || Mathf.Abs(goal.z)>ModeRadius-.8f)
        {
            var tangent=Vector3.Cross(Vector3.up,outward.normalized)*5;
            goal=actor.transform.position+(Vector3.Dot(actor.transform.position,tangent)>0?-tangent:tangent);
        }
        return true;
    }

    void TickMode()
    {
        DeliverEntries();
        if (Mode==ArenaMode.Bounty || Mode==ArenaMode.Rematch)
        {
            var special=Mode==ArenaMode.Bounty?marked:crown;
            if (Alive(special))
            {
                if (specialMarker==null)
                    specialMarker=ArenaModeEmblem.Create(transform,
                        Mode==ArenaMode.Bounty?ArenaModeEmblem.Shape.BountySkull:ArenaModeEmblem.Shape.Crown,
                        Mode==ArenaMode.Bounty?Red:GoldAccent);
                specialMarker.SetAnchor(special.transform,Mode==ArenaMode.Bounty?2.65f:2.1f);
            }
            else if (specialMarker!=null) { Destroy(specialMarker.gameObject); specialMarker=null; }
        }
        while (infections.Count>0)
        {
            var victim=infections.Dequeue();
            if (victim==null) continue;
            var zombie=SpawnObjective(victim.DisplayName,1,victim.transform.position,0);
            zombie.IsBot=false;
            zombie.SetViewerProfile(victim.ViewerId,victim.AvatarUrl);
            zombie.GetComponentInChildren<ArenaAnimatedCharacter>()?.Rise();
            ArenaAudioEffects.Infection(zombie.transform.position);
            ArenaBroadcastHUD.NotifyModeMoment(victim.DisplayName,"ZOMBİYE DÖNÜŞTÜ","İstila takımı");
        }
        TickRules();
        TickObjectives();
        TickDuel();
        for (int i = 0; i < healing.Length; i++)
        {
            if (healing[i] == null) continue;
            if (Time.time >= healUntil[i]) { Destroy(healing[i]); healing[i]=null; continue; }
            foreach (var actor in fighters)
                if (Alive(actor) && SideOf(actor)==i && Flat(actor.transform.position,Base(i))<2)
                    actor.Heal(7*Time.deltaTime);
        }
        switch (Mode)
        {
            case ArenaMode.King:
                if (!Alive(kings[0]) || !Alive(kings[1]) || RemainingSeconds<=0) FinishTeams(Fraction(kings[0]),Fraction(kings[1]));
                break;
            case ArenaMode.Zombies:
                bool survivors = fighters.Any(x=>LivingViewer(x)&&SideOf(x)==0);
                if ((hadPlayers && !survivors) || RemainingSeconds<=0) FinishObjective(survivors?"Hayatta kalanlar kazandı":"Zombiler kazandı");
                break;
            case ArenaMode.Gold:
                TickGold();
                if (deliveries[0]>=3 || deliveries[1]>=3 || RemainingSeconds<=0) FinishTeams(deliveries[0],deliveries[1]);
                break;
            case ArenaMode.Bridge: TickBridge(); break;
            case ArenaMode.Giant:
            case ArenaMode.GuestChampion: TickBoss(); break;
            case ArenaMode.Bounty: TickBounty(); break;
        }
    }

    void FinishTeams(float blue,float red) => FinishObjective(Mathf.Approximately(blue,red)?"Berabere":Team(blue>red?0:1)+" takım kazandı");

    void FinishObjective(string result,ArenaFighter winner=null)
    {
        if(warning!=null){Destroy(warning);warning=null;}
        modeResult=result;
        champion=winner;
        ChampionName=winner!=null?winner.DisplayName:result;
        ChampionViewerId=winner!=null?winner.ViewerId:"";
        IsFinished=true;
        statusMessage=result;
        CompleteAchievements();
        if (winner!=null) ArenaAnnouncer.ChampionWon(ChampionName);
    }

    void TickGold()
    {
        if (!Alive(carrier)) carrier = fighters.FirstOrDefault(x=>Alive(x)&&!x.IsEntering&&Flat(x.transform.position,goldAt)<1.05f);
        if (carrier!=null)
        {
            goldAt=carrier.transform.position;
            int side=SideOf(carrier);
            if (Flat(goldAt,Base(side))<1.45f)
            {
                deliveries[side]++;
                ArenaBroadcastHUD.NotifyModeMoment(carrier.DisplayName,"ALTIN TESLİM EDİLDİ",$"{Team(side)} • {deliveries[side]}/3");
                Clip("altın teslimatı",carrier.DisplayName);
                carrier=null;
                goldAt=Vector3.up;
            }
        }
        gold.SetAnchor(carrier!=null?carrier.transform:null,carrier==null?0f:2.1f,goldAt);
    }

    void TickBridge()
    {
        if (!Alive(gate)) { FinishObjective($"Kapı düştü • {wave}/3 dalga"); return; }
        if (RemainingSeconds<=0) { FinishObjective($"Süre doldu • {wave}/3 dalga"); return; }
        if (!hadPlayers || voteUntil>Time.time || fighters.Any(x=>Alive(x)&&SideOf(x)==1)) return;
        if (wave>=3) { FinishObjective($"Kapı korundu • %{Fraction(gate)*100:0} can"); return; }
        if (wave>0 && voteUntil==0)
        {
            voteUntil=Time.time+12; voters.Clear(); repair=archer=0;
            ArenaBroadcastHUD.NotifyModeMoment(ModeLabel,"TAKIM OYLAMASI","!tamir / !okçu • 12 sn");
            return;
        }
        if (voteUntil>0)
        {
            if (archer>repair) SpawnObjective("Okçu takviyesi",0,Base(0)+Vector3.forward*2,20,ArenaFighterRole.Archer);
            else gate.Heal(80);
            voteUntil=0;
        }
        wave++;
        ArenaBroadcastHUD.NotifyModeMoment(ModeLabel,$"DALGA {wave}/3","Kapıyı savun");
        int count=Mathf.Clamp(fighters.Count(LivingViewer)+wave,2,12);
        for (int i=0;i<count;i++)
            SpawnObjective($"Dalga {wave} • {i+1}",1,Base(1)+Vector3.forward*(i-(count-1)*.5f),wave*6);
    }

    void TickBoss()
    {
        if (!Alive(boss))
        {
            Clip("boss zaferi",boss!=null?boss.DisplayName:ModeLabel);
            FinishObjective("Takım kazandı • "+BossStats()); return;
        }
        if (RemainingSeconds<=0 || hadPlayers&&!fighters.Any(LivingViewer))
        {
            FinishObjective((RemainingSeconds<=0?"Süre doldu":"Takım elendi")+" • "+BossStats()); return;
        }
        if (warning!=null && Time.time>=warningAt)
        {
            var impact=warning.GetComponent<ArenaBossSlamVisual>();
            warning=null;strikeAt=Time.time+6;
            impact.Strike();
            foreach(var x in fighters)
                if (Viewer(x)&&Flat(x.transform.position,warningPosition)<=3.4f)
                {
                    x.TakeDamage(55,boss);
                    x.KnockBackFrom(warningPosition,6f,5.4f);
                }
        }
        if (warning==null && Time.time>=strikeAt)
        {
            var target=fighters.FirstOrDefault(Viewer);
            if (target==null) return;
            warningPosition=boss.transform.position;
            warning=ArenaBossSlamVisual.Begin(transform,warningPosition,3.4f,1.4f);
            warningAt=Time.time+1.4f;
        }
    }
    string BossStats() => $"Boss hasarı {(boss!=null?boss.MaxHealth-boss.Health:0):0} • Emilen {defense:0}";
    void TickBounty()
    {
        if (!Alive(marked)) SetBountyTarget(fighters.FirstOrDefault(Viewer)??fighters.FirstOrDefault(Alive));
        if (Alive(marked))
        {
            var key=GetStanding(marked).id;
            bounty.TryGetValue(key,out float score);
            bounty[key]=score+Time.deltaTime;
        }
        if (RemainingSeconds>0) return;
        var leader=BountyLeader();
        FinishObjective(leader!=null?$"{leader.name} • {bounty[leader.id]:0.0} hedef puanı":"Hedef puanı yok",leader?.fighter);
        if (leader!=null)
        {
            ChampionName=leader.name;
            ChampionViewerId=leader.id;
        }
    }
    Standing BountyLeader() => standings.Values.Where(x=>bounty.ContainsKey(x.id))
        .OrderByDescending(x=>bounty[x.id]).ThenBy(x=>x.id,StringComparer.Ordinal).FirstOrDefault();

    void OnModeElimination(ArenaFighter victim,ArenaFighter attacker)
    {
        if (IsTournament || IsFinished) return;
        if (carrier==victim)
        {
            goldAt=victim.transform.position;
            carrier=null;
            ArenaBroadcastHUD.NotifyModeMoment(victim.DisplayName,"ALTIN YERE DÜŞTÜ","Altını al ve kasana götür");
        }
        bool valid=attacker!=null && attacker!=victim && !SameViewer(attacker,victim);
        if (!finalThreeRecorded && roundEntrants.Count>=4 && !victim.IsBot)
        {
            var remaining=fighters.Where(Viewer).GroupBy(x=>x.ViewerId).Select(x=>x.First().DisplayName).Take(4).ToArray();
            if (remaining.Length==3)
            {
                finalThreeRecorded=true;
                achievements.Add("Son üç: "+string.Join(", ",remaining));
            }
        }
        if (Mode==ArenaMode.Zombies && valid && SideOf(attacker)==1 && SideOf(victim)==0 && !string.IsNullOrEmpty(victim.ViewerId))
        {
            infections.Enqueue(victim);
        }
        if (Mode==ArenaMode.Bounty && victim==marked) SetBountyTarget(valid&&Alive(attacker)?attacker:null);
        if (Mode==ArenaMode.Rematch && valid && (victim==crown || crown==null)) SetCrown(attacker);
        if (valid && !attacker.IsBot && !string.IsNullOrEmpty(attacker.ViewerId))
        {
            if (!firstKo) { firstKo=true; achievements.Add(attacker.DisplayName+" • İlk KO"); }
            int kills=GetStanding(attacker).kills;
            string key="ArenaKoRecord."+attacker.ViewerId;
            if (kills>PlayerPrefs.GetInt(key))
            {
                PlayerPrefs.SetInt(key,kills);
                string title=attacker.DisplayName+" • KO rekoru";
                if (!achievements.Contains(title)) achievements.Add(title);
            }
        }
    }

    void SetBountyTarget(ArenaFighter actor)
    {
        if (marked == actor) return;
        marked = actor;
        if (actor != null) ArenaBroadcastHUD.NotifyModeMoment(actor.DisplayName,"HEDEF DEĞİŞTİ","İşareti taşı, puan kazan");
    }

    void SetCrown(ArenaFighter actor)
    {
        crown=actor;
        Clip("taç devri",actor.DisplayName);
        ArenaBroadcastHUD.NotifyModeMoment(actor.DisplayName,"TAÇ DEVRİ","Yeni taç sahibi");
    }

    void CompleteAchievements()
    {
        if (Mode==ArenaMode.Rematch) modeResult="Tur birincisi: "+ChampionName+" • Taç: "+(crown!=null?crown.DisplayName:"Sahipsiz");
        if (giftedDefense>0) achievements.Insert(0,$"Hediye kalkanı {giftedDefense:0} hasar emdi");
        PlayerPrefs.Save();
    }

    public string JoinQueue => waiting.Count>0?$"{waiting.Peek().name} • Katılım sırası: {waiting.Count}":"";
    public string ModeScore
    {
        get
        {
            if (IsFinished) return "Sonraki tur için !hazır • "+Ready.Count;

            if (Mode==ArenaMode.King) return $"Mavi kral %{Fraction(kings[0])*100:0}  /  Kırmızı kral %{Fraction(kings[1])*100:0}";
            if (Mode==ArenaMode.Zombies) return $"Sağ {fighters.Count(x=>Alive(x)&&SideOf(x)==0)}  /  Zombi {fighters.Count(x=>Alive(x)&&SideOf(x)==1)}";
            if (Mode==ArenaMode.Gold) return $"Mavi {deliveries[0]} : {deliveries[1]} Kırmızı • {(Alive(carrier)?carrier.DisplayName:GoldLocation())}";
            if (Mode==ArenaMode.Bridge)
                return voteUntil>Time.time?$"Kapı %{Fraction(gate)*100:0} • !tamir {repair} / !okçu {archer} • {Mathf.CeilToInt(voteUntil-Time.time)} sn":
                    $"Kapı %{Fraction(gate)*100:0} • Dalga {wave}/3";
            if (Mode==ArenaMode.Giant || Mode==ArenaMode.GuestChampion) return $"{boss?.DisplayName} %{Fraction(boss)*100:0}";
            if (Mode==ArenaMode.Bounty)
            {
                var leader=BountyLeader();
                return $"Hedef {marked?.DisplayName??"—"} • Lider {(leader==null?"—":leader.name+" "+bounty[leader.id].ToString("0.0"))}";
            }
            if (Mode==ArenaMode.Classes && !ModeCombatOpen) return $"!temel / !okçu / !muhafız • {Mathf.CeilToInt(classUntil-Time.time)} sn";
            if (Mode==ArenaMode.Rematch) return $"Taç {crown?.DisplayName??"İlk KO veya önceki şampiyon"}";
            if (ruleRequest!=null) return $"{ruleRequest.name} • !kural hız / dar / merkez • {Mathf.CeilToInt(ruleUntil-Time.time)} sn";
            if (objective!=null) return objectivePosition==Vector3.zero?"Merkezi tut":"İşaretli noktadan geç";
            if (ruleSender!=null) return $"{ruleSender} • {rule} turu";
            int final=PlayerPrefs.GetInt("ArenaFinalRule");
            return final==0?"Giriş: !katıl":final==1?"Son 30 sn: arena daralır":"Son 30 sn: merkezi ele geçir";
        }
    }
    string GoldLocation() => Flat(goldAt,Vector3.zero)<2?"Altın merkezde":$"Altın {Team(goldAt.x<0?0:1)} kanatta";

    int confirmedDuels;
    void TickDuel()
    {
        if (TeamMode) return;
        if (duelLeft!=null && duelRight!=null && !duelResolved)
        {
            if (Alive(duelLeft)&&Alive(duelRight)&&RemainingSeconds>0) return;
            duelResolved=true;
            float leftScore=Alive(duelLeft)?Fraction(duelLeft):0f;
            float rightScore=Alive(duelRight)?Fraction(duelRight):0f;
            string result=Mathf.Approximately(leftScore,rightScore)?"Berabere":
                (leftScore>rightScore?duelLeft.DisplayName:duelRight.DisplayName);
            Clip("düello sonucu",result);
            ArenaBroadcastHUD.NotifyGift("DÜELLO","SONUÇ",result);
        }
        if (duelLeft!=null || confirmedDuels<=0 || Duels.Count==0) return;
        var pair=Duels.Peek();
        var left=FindViewerFighter(pair.left);
        var right=FindViewerFighter(pair.right);
        if (left==null || right==null || left.IsEntering || right.IsEntering) return;
        Duels.Dequeue();
        duelLeft=left; duelRight=right;
        ArenaAnnouncer.DuelStarted("Meydan okuma",left.DisplayName,right.DisplayName);
        ArenaBroadcastHUD.NotifyGift(left.DisplayName,"DÜELLO",right.DisplayName);
    }

    void TickObjectives()
    {
        if (TeamMode) return;
        if (RemainingSeconds<=12)
        {
            var lead=GetRankedStandings().FirstOrDefault();
            if (lead!=null && previousLeader!=null && previousLeader!=lead.id) Clip("son saniye lider değişimi",lead.name);
            previousLeader=lead?.id;
        }
        int final=PlayerPrefs.GetInt("ArenaFinalRule");
        if (RemainingSeconds<=30 && final==1)
            radius=Mathf.Min(radius,Mathf.Lerp(arenaRadius*.55f,arenaRadius,RemainingSeconds/30));
        bool center=rule=="merkez" || RemainingSeconds<=30 && final==2;
        if (center && objective!=null && objectivePosition==Vector3.zero || !center && (objectiveAt<=0 || Time.time<objectiveAt)) return;
        if (objective!=null) Destroy(objective);
        objectivePosition=center?Vector3.zero:objectivePosition==Vector3.zero?new Vector3(UnityEngine.Random.Range(-4,5),0,UnityEngine.Random.Range(-4,5)):Vector3.zero;
        objective=Marker("Yan hedef",objectivePosition,new Color(.2f,.85f,.7f),4);
        objectiveAt=Time.time+16;
        bool holdCenter = center || objectivePosition==Vector3.zero;
        StartCoroutine(ScoreObjective(objective,holdCenter));
        ArenaBroadcastHUD.NotifyModeMoment(ModeLabel,"YAN HEDEF",holdCenter?"Merkezi tut":"İşaretli bölgeden geç");
    }
    System.Collections.IEnumerator ScoreObjective(GameObject marker,bool center)
    {
        var crossed=new HashSet<string>();
        Vector3 location=objectivePosition;
        while (marker!=null && !IsFinished)
        {
            var occupants=fighters.Where(x=>Viewer(x)&&Flat(x.transform.position,location)<2f);
            if (center)
            {
                var two=occupants.Take(2).ToArray();
                if (two.Length==1) GetStanding(two[0]).points+=Time.deltaTime*2;
            }
            else
            {
                foreach (var viewer in occupants)
                    if (crossed.Add(viewer.ViewerId)) GetStanding(viewer).points+=12;
            }
            yield return null;
        }
    }

    void TickRules()
    {
        if (ruleRequest!=null && Time.time>=ruleUntil) SetRule("hız");
        if (ruleRequest!=null || nextRule!=null || Rules.Count==0 || RemainingSeconds<20f) return;
        ruleRequest=Rules.Peek();
        ruleUntil=Time.time+20;
        ArenaBroadcastHUD.NotifyGift(ruleRequest.name,"SONRAKİ TUR","!kural hız / dar / merkez • 20 sn");
    }
    void SetRule(string choice)
    {
        nextRule=choice; nextRuleSender=ruleRequest.name;
        Rules.Dequeue();
        ruleRequest=null;
        SaveRules();
    }

    static readonly Dictionary<string,string> shieldTargets=new Dictionary<string,string>();
    bool TryModeGift(GiftEventType gift,string id,string name,string avatar,out bool applied)
    {
        applied=false;
        if (gift==GiftEventType.NextRule)
        {
            if (Rules.Count>=40) return true;
            Rules.Enqueue(new Entrant { id=id,name=name });
            SaveRules();
            applied=true;
            return true;
        }
        if (gift==GiftEventType.TeamShield)
        {
            if (!TeamMode || !shieldTargets.TryGetValue(id,out string choice)) return true;
            var entry=Entry(id,name,avatar);
            int side=TwoTeams?entry.side:0;
            var target=choice=="kral"?kings[side]:fighters.FirstOrDefault(x=>Viewer(x)&&SideOf(x)==side &&
                (x.ViewerId==choice || string.Equals(x.DisplayName.TrimStart('@'),choice.TrimStart('@'),StringComparison.OrdinalIgnoreCase)));
            if (!Alive(target)) return true;
            target.GiftTimedShield(75,12f);
            shieldTargets.Remove(id);
            ArenaBroadcastHUD.NotifyGift(name,"TAKIM KALKANI",target.DisplayName+" • 75 kalkan / 12 sn");
            applied=true;
            return true;
        }
        if (gift==GiftEventType.Rose && freeEntrants.Contains(id))
        {
            if (LivingCount>=50) return true;
            var entry=Entry(id,name,avatar);
            int side=TwoTeams?entry.side:0;
            var reserve=SpawnObjective(entry.name.TrimStart('@')+" • Takviye",side,Base(side),10);
            reserve.SetViewerProfile(id,entry.avatar);
            Destroy(Marker(entry.name+" sancağı",reserve.transform.position,side==0?Blue:Red,2),8f);
            ArenaBroadcastHUD.NotifyGift(name,"GÜL TAKVİYESİ",reserve.DisplayName);
            applied=true;
            return true;
        }
        return false;
    }
    public bool AreaMeteor => PlayerPrefs.GetInt("ArenaAreaMeteor")==1;
    public GameObject MarkMeteor(Vector3 position,ArenaFighter caster)
    {
        meteorWarning=Marker("Meteor",position,Red,7f);
        meteorCaster=caster;
        meteorSide=MeteorSide(caster);
        return meteorWarning;
    }
    public int MeteorSide(ArenaFighter caster) => TeamMode?SideOf(caster):-1;
    public bool MeteorTarget(ArenaFighter caster,ArenaFighter target,int casterSide) =>
        target!=null && target!=caster && (casterSide<0 || casterSide!=SideOf(target));
    float giftedDefense;
    public void RecordDefense(float amount,float gifted)
    {
        if (!TeamMode) return;
        defense+=Mathf.Max(0,amount);
        giftedDefense+=Mathf.Max(0,gifted);
    }
    void Clip(string kind,string detail) => ArenaBroadcastMetrics.Clip(ModeLabel,kind,detail,Time.time-roundStartedAt);
}

