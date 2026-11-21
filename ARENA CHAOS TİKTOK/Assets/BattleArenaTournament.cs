using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class BattleArenaManager
{
    const string TournamentPreference = "ArenaDuelTournament";
    const int TournamentSize = 8;
    const float DuelSeconds = 60f;
    const float IntermissionSeconds = 4f;

    sealed class TournamentViewer
    {
        public string Id, Name, AvatarUrl;
    }

    readonly HashSet<string> tournamentViewerIds = new HashSet<string>();
    List<TournamentViewer> bracket = new List<TournamentViewer>(TournamentSize);
    List<TournamentViewer> advancing = new List<TournamentViewer>(TournamentSize / 2);
    ArenaFighter leftDuelist, rightDuelist;
    int matchIndex;
    float nextDuelAt;
    string tournamentMessage;

    public bool IsTournament { get; private set; }
    public bool DuelInProgress { get; private set; }
    public bool DuelCombatStarted { get; private set; }
    string duelStage;
    public string TournamentHeading
    {
        get
        {
            if (IsFinished) return "TURNUVA TAMAMLANDI";
            if (tournamentViewerIds.Count < TournamentSize)
                return $"KAYIT {tournamentViewerIds.Count}/{TournamentSize}";
            string stage = bracket.Count == 8 ? "ÇEYREK FİNAL" : bracket.Count == 4 ? "YARI FİNAL" : "FİNAL";
            return bracket.Count == 2 ? stage : $"{stage} {matchIndex + 1}/{bracket.Count / 2}";
        }
    }
    public string TournamentMessage => tournamentMessage;

    public void SwitchGameMode()
    {
        RequestMode(IsTournament ? ArenaMode.Survival : ArenaMode.Tournament);
    }

    void Awake() => LoadMode();

    void StartTournament()
    {
        IsTournament = true;
        var scenePlayer = GameObject.Find("PLAYER");
        if (scenePlayer != null) scenePlayer.SetActive(false);
        roundEndsAt = Time.time;
        tournamentMessage = "Katılmak için yayını beğen";
    }

    bool RegisterTournamentViewer(string viewerId, string displayName, string avatarUrl)
    {
        if (tournamentViewerIds.Contains(viewerId)) return true;
        if (tournamentViewerIds.Count >= TournamentSize) return false;

        tournamentViewerIds.Add(viewerId);
        roundEntrants.Add(viewerId);
        ArenaBroadcastMetrics.Join(viewerId);
        bracket.Add(new TournamentViewer { Id = viewerId, Name = displayName, AvatarUrl = avatarUrl });
        return true;
    }

    void UpdateTournament()
    {
        if (tournamentViewerIds.Count < TournamentSize) return;
        if (!DuelInProgress)
        {
            if (Time.time >= nextDuelAt)
            {
                if (leftDuelist != null) matchIndex++;
                StartDuel();
            }
            return;
        }

        if (!DuelCombatStarted)
        {
            if (leftDuelist.IsEntering || rightDuelist.IsEntering) return;
            DuelCombatStarted = true;
            roundEndsAt = Time.time + DuelSeconds;
            ArenaAnnouncer.DuelStarted(duelStage,leftDuelist.DisplayName,rightDuelist.DisplayName);
            ArenaBroadcastHUD.NotifyModeMoment(leftDuelist.DisplayName,"DÜELLO BAŞLADI",rightDuelist.DisplayName);
        }
        if (!leftDuelist.IsAlive || !rightDuelist.IsAlive || Time.time >= roundEndsAt)
            CompleteDuel();
    }

    void StartDuel()
    {
        foreach (var fighter in fighters)
        {
            fighter.gameObject.SetActive(false);
            Destroy(fighter.gameObject);
        }

        fighters.Clear();
        standings.Clear();
        ranked.Clear();

        if (matchIndex * 2 == bracket.Count)
        {
            bracket = advancing;
            advancing = new List<TournamentViewer>(bracket.Count / 2);
            matchIndex = 0;
        }

        int leftIndex = matchIndex * 2;
        leftDuelist = CreateDuelist(bracket[leftIndex], -1, new Color(.95f, .35f, .24f), 0);
        rightDuelist = CreateDuelist(bracket[leftIndex + 1], 1, new Color(.25f, .65f, 1f), 4);
        tournamentMessage = leftDuelist.DisplayName + "  /  " + rightDuelist.DisplayName;
        nextMeteor = Time.time;
        roundEndsAt = Time.time;
        DuelInProgress = true;
        DuelCombatStarted = false;
        duelStage = bracket.Count == 8 ? "Çeyrek final" : bracket.Count == 4 ? "Yarı final" : "Büyük final";
        var director = GetComponent<ArenaPresentationDirector>();
        if (director != null)
        {
            director.PlayDuelEntrance(leftDuelist,-1);
            director.PlayDuelEntrance(rightDuelist,1);
        }
    }

    ArenaFighter CreateDuelist(TournamentViewer viewer, int side, Color color, int modelIndex)
    {
        string displayName = viewer.Name.StartsWith("@") ? viewer.Name : "@" + viewer.Name;
        var fighter = CreateViewerFighter(displayName, color, modelIndex, new Vector3(side * 3f, 1f, 0f));
        fighter.SetViewerProfile(viewer.Id, viewer.AvatarUrl);
        return fighter;
    }

    void CompleteDuel()
    {
        DuelInProgress = false;
        DuelCombatStarted = false;
        leftDuelist.enabled = rightDuelist.enabled = false;
        var left = GetStanding(leftDuelist);
        var right = GetStanding(rightDuelist);
        float leftHealth = leftDuelist.IsAlive ? leftDuelist.Health / leftDuelist.MaxHealth : 0f;
        float rightHealth = rightDuelist.IsAlive ? rightDuelist.Health / rightDuelist.MaxHealth : 0f;
        bool leftWon = leftDuelist.IsAlive != rightDuelist.IsAlive
            ? leftDuelist.IsAlive
            : Mathf.Approximately(leftHealth, rightHealth)
                ? left.damage >= right.damage
                : leftHealth > rightHealth;
        var winner = leftWon ? leftDuelist : rightDuelist;
        tournamentMessage = winner.DisplayName + " maçı kazandı";

        if (bracket.Count == 2)
        {
            champion = winner;
            ChampionName = winner.DisplayName;
            ChampionViewerId = winner.ViewerId;
            IsFinished = true;
            ArenaAnnouncer.ChampionWon(ChampionName);
            return;
        }

        advancing.Add(bracket[matchIndex * 2 + (leftWon ? 0 : 1)]);
        nextDuelAt = Time.time + IntermissionSeconds;
    }
}
