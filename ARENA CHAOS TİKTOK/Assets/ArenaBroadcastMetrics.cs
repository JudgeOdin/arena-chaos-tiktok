using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class ArenaBroadcastMetrics
{
    static readonly HashSet<string> joined=new HashSet<string>(), supporters=new HashSet<string>(), senders=new HashSet<string>();
    static HashSet<string> previous=new HashSet<string>();
    static int eligible, returned, rounds;
    public static string Summary => $"Katılan {joined.Count} • Dönen %{(eligible==0?0:100f*returned/eligible):0} • Destek {supporters.Count} • Gönderen {senders.Count}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        joined.Clear(); supporters.Clear(); senders.Clear(); previous.Clear();
        eligible=returned=rounds=0;
        Application.quitting-=Save;
        Application.quitting+=Save;
    }

    public static bool Join(string id)
    {
        if (!LiveViewer(id)) return false;
        joined.Add(id);
        DateTime now=DateTime.UtcNow;
        DateTime monday=now.Date.AddDays(-((6+(int)now.DayOfWeek)%7));
        string key="ArenaParticipation."+id;
        string current=monday.ToString("yyyy-MM-dd");
        if (PlayerPrefs.GetString(key)==current) return false;
        if (!ArenaLiveBridge.IsOffline) PlayerPrefs.SetString(key,current);
        return true;
    }
    static bool LiveViewer(string id) => !string.IsNullOrEmpty(id) && !id.StartsWith("arena-test:",StringComparison.Ordinal);
    public static void Support(string id) { if (LiveViewer(id)) supporters.Add(id); }
    public static void Gift(string id) { if (LiveViewer(id)) senders.Add(id); }
    public static void EndRound(IEnumerable<string> participants)
    {
        var next=new HashSet<string>(participants.Where(LiveViewer));
        eligible+=previous.Count;
        returned+=next.Count(previous.Contains);
        previous=next;
        rounds++;
        Save();
    }
    public static void Save() => Write("arena-session.json",JsonConvert.SerializeObject(new
    {
        rounds,uniqueParticipants=joined.Count,eligibleForReturn=eligible,returned,
        returnRate=eligible==0?0:(double)returned/eligible,eliminatedSupporters=supporters.Count,uniqueGiftSenders=senders.Count
    },Formatting.Indented),false);

    public static void Clip(string mode,string type,string detail,float seconds) =>
        Write("arena-clip-candidates.jsonl",JsonConvert.SerializeObject(new { utc=DateTime.UtcNow, mode,type,detail,seconds })+Environment.NewLine,true);

    static void Write(string name,string content,bool append)
    {
        if (ArenaLiveBridge.IsOffline) return;
        try
        {
            string path=Path.Combine(Application.persistentDataPath,name);
            if (append) File.AppendAllText(path,content);
            else File.WriteAllText(path,content);
        }
        catch (IOException e) { Debug.LogWarning(e.Message); }
        catch (UnauthorizedAccessException e) { Debug.LogWarning(e.Message); }
    }
}

