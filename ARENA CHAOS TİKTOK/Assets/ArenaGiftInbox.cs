using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable] public sealed class ArenaGiftEvent
{
    public string eventId,viewerId,displayName,avatarUrl,comboId;
    public GiftEventType type;
    public int count=1;
}

/// <summary>Persistent accepted-event inbox. Failed deliveries stay pending across rounds and restarts.</summary>
public sealed class ArenaGiftInbox : MonoBehaviour
{
    [Serializable] sealed class Delivery { public ArenaGiftEvent gift;public int remaining; }
    [Serializable] sealed class Seen {public string id;public int count;}
    [Serializable] sealed class State { public List<Delivery> pending=new List<Delivery>();public List<Seen> seen=new List<Seen>();public int delivered; }
    static ArenaGiftInbox instance;
    State state=new State();
    readonly Dictionary<string,Seen> seen=new Dictionary<string,Seen>();
    string path;
    float nextDelivery;
    int cursor;
    public string LastError { get; private set; }
    public int DeliveredUnits=>state.delivered;
    public static ArenaGiftInbox CreateStore(string filename,bool persistent)
    {
        if(Path.GetFileName(filename)!=filename)throw new ArgumentException("Store filename required");
        var obj=new GameObject("Isolated gift store");var inbox=obj.AddComponent<ArenaGiftInbox>();
        if(persistent)DontDestroyOnLoad(obj);inbox.path=Path.Combine(Application.persistentDataPath,filename);inbox.LoadState();return inbox;
    }
#if UNITY_EDITOR
    public void UseTestStore(string file){path=file;LoadState();}
#endif
    public int PendingUnits { get {int count=0;foreach(var delivery in state.pending)count+=delivery.remaining;return count;} }
    public static ArenaGiftInbox Instance
    {
        get
        {
            if(instance==null){var obj=new GameObject("Persistent gift inbox");instance=obj.AddComponent<ArenaGiftInbox>();DontDestroyOnLoad(obj);instance.Load();}
            return instance;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){instance=null;}
    void Load()
    {
        path=Path.Combine(Application.persistentDataPath,Application.isEditor?"gift-inbox-editor.json":"gift-inbox.json");
        LoadState();
    }
    void LoadState()
    {
        state=new State();LastError=null;enabled=true;
        try{if(File.Exists(path))state=ReadState(path);}
        catch(Exception exception)
        {
            try
            {
                state=ReadState(path+".bak");
                File.Copy(path,path+".damaged-"+DateTime.UtcNow.Ticks,true);
                File.Copy(path+".bak",path,true);
                Debug.LogWarning("Hediye kaydı yedekten kurtarıldı; son teslimat yeniden uygulanabilir.");
            }
            catch{LastError="Hediye kaydı okunamadı: "+exception.Message;enabled=false;return;}
        }
        seen.Clear();foreach(var item in state.seen)seen[item.id]=item;
    }
    static State ReadState(string file)
    {
        var loaded=JsonUtility.FromJson<State>(File.ReadAllText(file));
        if(loaded==null||loaded.pending==null||loaded.seen==null)throw new InvalidDataException("Invalid gift store");
        foreach(var delivery in loaded.pending)if(delivery==null||delivery.gift==null||delivery.remaining<1)throw new InvalidDataException("Invalid pending gift");
        return loaded;
    }
    bool Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+".tmp";
            File.WriteAllText(temporary,JsonUtility.ToJson(state));
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
            LastError=null;return true;
        }
        catch(Exception exception){LastError="Hediye kaydedilemedi: "+exception.Message;return false;}
    }
    public bool Submit(ArenaGiftEvent gift)
    {
        if(!enabled||gift==null||string.IsNullOrWhiteSpace(gift.eventId)||string.IsNullOrWhiteSpace(gift.viewerId)||gift.count<1||gift.count>10000||!Enum.IsDefined(typeof(GiftEventType),gift.type))return false;
        string key=string.IsNullOrEmpty(gift.comboId)?"event:"+gift.eventId:"combo:"+gift.viewerId+":"+gift.comboId+":"+(int)gift.type;
        seen.TryGetValue(key,out var existing);int previous=existing!=null?existing.count:0;
        int total=string.IsNullOrEmpty(gift.comboId)?1:gift.count;
        if(total<=previous)return true;
        int units=string.IsNullOrEmpty(gift.comboId)?gift.count:total-previous;
        var mark=existing??new Seen{id=key};mark.count=total;
        if(existing==null){seen.Add(key,mark);state.seen.Add(mark);}
        var delivery=new Delivery{gift=gift,remaining=units};state.pending.Add(delivery);
        if(Save())return true;
        state.pending.Remove(delivery);mark.count=previous;if(existing==null){state.seen.Remove(mark);seen.Remove(key);}return false;
    }
    void Update()
    {
        if(Time.timeScale==0||Time.unscaledTime<nextDelivery||state.pending.Count==0)return;
        nextDelivery=Time.unscaledTime+.15f;
        var arena=UnityEngine.Object.FindAnyObjectByType<BattleArenaManager>();if(arena==null||arena.IsFinished)return;
        int attempts=Mathf.Min(16,state.pending.Count);
        for(int i=0;i<attempts;i++)
        {
            cursor%=state.pending.Count;int index=cursor;cursor++;
            var delivery=state.pending[index];var gift=delivery.gift;
            if(!arena.TryApplyGiftToViewer(gift.type,gift.viewerId,gift.displayName,gift.avatarUrl))continue;
            delivery.remaining--;
            state.delivered++;
            if(delivery.remaining==0)state.pending.RemoveAt(index);
            if(!Save()){enabled=false;Debug.LogError(LastError);}
            break;
        }
    }
    void OnDestroy(){if(instance==this)instance=null;}
}
