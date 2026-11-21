using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>Optional local TikFinity Event API receiver. All gameplay and persistence stay on the main thread.</summary>
public sealed class ArenaLiveBridge : MonoBehaviour
{
    static ArenaLiveBridge instance;
    readonly ConcurrentQueue<string> incoming=new ConcurrentQueue<string>();
    readonly Queue<(string viewerId, string displayName, string avatarUrl)> pendingLikes =
        new Queue<(string viewerId, string displayName, string avatarUrl)>();
    readonly HashSet<string> pendingLikeViewers = new HashSet<string>();
    const int LikeQueueLimit = 512;
    CancellationTokenSource cancellation;
    Task connection;
    ArenaLiveSettings settings;
    bool replaying,reconnectRequested;
    volatile string status="LIVE bağlı değil";
    public string Status=>status;
    public static ArenaLiveBridge Current=>instance;
    public IReadOnlyList<ArenaLiveSettings.Binding> GiftBindings => settings?.gifts ?? Array.Empty<ArenaLiveSettings.Binding>();
    public int UnmappedEvents { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if(instance!=null)return;
        var obj=new GameObject("Persistent LIVE receiver");instance=obj.AddComponent<ArenaLiveBridge>();DontDestroyOnLoad(obj);
        instance.settings=ArenaLiveOperator.LoadSettings();
        obj.AddComponent<ArenaLiveOperator>().Initialize(instance.settings);
        ArenaAnnouncer.GetOrCreate();
        if(instance.settings!=null&&instance.settings.connectOnStart && !IsOffline)instance.Connect();
    }
    public static bool IsOffline => AutomatedTestMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-arenaOffline")>=0;

    public static bool AutomatedTestMode
    {
        get
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("ArenaJob.Active",false) || UnityEditor.SessionState.GetBool("ArenaModes.Test",false) || UnityEditor.SessionState.GetBool("ArenaPromo.Pending",false)) return true;
#endif
            var arguments = Environment.GetCommandLineArgs();
            return Array.IndexOf(arguments,"-arenaSmokeTest") >= 0 || Array.IndexOf(arguments,"-arenaSoakTest") >= 0;
        }
    }

    public void Connect()
    {
        if(settings==null)return;
        if (IsOffline) { status="Yerel mod • LIVE kapalı"; return; }
        if(connection!=null&&!connection.IsCompleted){reconnectRequested=true;cancellation?.Cancel();return;}
        reconnectRequested=false;
        if(!Uri.TryCreate(settings.endpoint,UriKind.Absolute,out var endpoint)||(endpoint.Scheme!="ws"&&endpoint.Scheme!="wss")){status="Geçersiz LIVE adresi";return;}
        cancellation?.Dispose();cancellation=new CancellationTokenSource();connection=Receive(endpoint,cancellation.Token);
    }
    public void Disconnect(){reconnectRequested=false;cancellation?.Cancel();status="LIVE bağlantısı durduruldu";}
    async Task Receive(Uri endpoint,CancellationToken token)
    {
        int delay=1;
        try
        {
            while(!token.IsCancellationRequested)
            {
                try
                {
                    using(var socket=new ClientWebSocket())
                    {
                        status="LIVE bağlantısı kuruluyor";await socket.ConnectAsync(endpoint,token);status="Event API bağlı / yayın durumu bekleniyor";delay=1;
                        var bytes=new byte[8192];
                        while(socket.State==WebSocketState.Open&&!token.IsCancellationRequested)
                        {
                            using(var message=new MemoryStream())
                            {
                                WebSocketReceiveResult result;
                                do
                                {
                                    result=await socket.ReceiveAsync(new ArraySegment<byte>(bytes),token);
                                    if(result.MessageType==WebSocketMessageType.Close)break;
                                    message.Write(bytes,0,result.Count);
                                    if(message.Length>65536)throw new InvalidDataException("LIVE mesaj boyutu sınırı aşıldı");
                                }while(!result.EndOfMessage);
                                if(result.MessageType==WebSocketMessageType.Close)
                                {
                                    status="Event API kapandı / yeniden bağlanıyor";
                                    break;
                                }
                                if(result.MessageType!=WebSocketMessageType.Text)continue;
                                while(incoming.Count>=512)await Task.Delay(100,token);
                                incoming.Enqueue(Encoding.UTF8.GetString(message.ToArray()));
                            }
                        }
                    }
                }
                catch(OperationCanceledException){break;}
                catch(Exception exception){status="LIVE yeniden bağlanacak: "+exception.Message;}
                await Task.Delay(delay*1000,token);delay=Math.Min(30,delay*2);
            }
        }
        catch(OperationCanceledException) { }
    }
    void Update()
    {
        if(reconnectRequested&&(connection==null||connection.IsCompleted))Connect();
        for(int i=0;i<32&&incoming.TryDequeue(out var json);i++)
        {
            try{Process(json);}
            catch(Exception exception){status="LIVE olay ayrıştırma hatası: "+exception.Message;}
        }
        DeliverLikeEntries();
    }

    void DeliverLikeEntries()
    {
        if (pendingLikes.Count == 0 || Time.timeScale == 0) return;
        var arena = UnityEngine.Object.FindAnyObjectByType<BattleArenaManager>();
        if (arena == null || arena.IsFinished) return;

        for (int i = 0; i < 16 && pendingLikes.Count > 0; i++)
        {
            var entry = pendingLikes.Peek();
            if (!arena.TryJoinFromLike(entry.viewerId, entry.displayName, entry.avatarUrl)) break;
            pendingLikes.Dequeue();
            pendingLikeViewers.Remove(entry.viewerId);
        }
    }

    readonly HashSet<string> chatSeen=new HashSet<string>();
    readonly Queue<string> chatOrder=new Queue<string>();

    public bool Process(string json,ArenaGiftInbox destination=null)
    {
        var envelope = JObject.Parse(json);
        string eventType = (string)envelope["event"];
        if (eventType != "gift" && eventType != "like" && eventType != "chat" && eventType != "comment") return false;
        if (!(envelope["data"] is JObject data)) return false;
        if (eventType == "chat" || eventType == "comment")
        {
            string id = data["userId"]?.ToString();
            string message = (string)data["comment"] ?? (string)data["text"];
            string chatMessageId = data["msgId"]?.ToString();
            string roomId = data["roomId"]?.ToString() ?? settings?.channel ?? "";
            string key = string.IsNullOrEmpty(chatMessageId) ? null : roomId + ":" + chatMessageId;
            if (key != null && chatSeen.Contains(key)) return true;
            var arena = UnityEngine.Object.FindAnyObjectByType<BattleArenaManager>();
            bool chatAccepted = arena != null && arena.ProcessChat(id,(string)data["nickname"] ?? (string)data["uniqueId"] ?? id,GetAvatarUrl(data),message);
            if (key != null)
            {
                chatSeen.Add(key); chatOrder.Enqueue(key);
                if (chatOrder.Count>2048) chatSeen.Remove(chatOrder.Dequeue());
            }
            return chatAccepted;
        }
        if (eventType == "like")
        {
            string viewerId = data["userId"]?.ToString();
            if (string.IsNullOrWhiteSpace(viewerId)) return false;
            if (pendingLikeViewers.Contains(viewerId)) return true;
            if (pendingLikes.Count>=LikeQueueLimit) return false;
            if (pendingLikeViewers.Add(viewerId))
            {
                string displayName = (string)data["nickname"];
                if (string.IsNullOrWhiteSpace(displayName)) displayName = (string)data["uniqueId"];
                if (string.IsNullOrWhiteSpace(displayName)) displayName = viewerId;
                pendingLikes.Enqueue((viewerId, displayName, GetAvatarUrl(data)));
            }
            return true;
        }
        string giftId=data["giftId"]?.ToString();string viewer=data["userId"]?.ToString();
        string messageId=data["msgId"]?.ToString();string group=data["groupId"]?.ToString();
        if(string.IsNullOrEmpty(viewer)||string.IsNullOrEmpty(messageId)){Review(json,"Olay/kullanıcı kimliği eksik");return false;}
        GiftEventType type=default;bool mapped=false;
        if(settings!=null)foreach(var binding in settings.gifts)if(!string.IsNullOrEmpty(binding.giftId)&&binding.giftId==giftId){type=binding.type;mapped=true;break;}
        if(!mapped)
        {
            var roster=Resources.Load<ArenaGiftRoster>("ArenaChaos/GiftRoster");
            if(roster!=null)foreach(var tier in roster.tiers)if(!string.IsNullOrEmpty(tier.tiktokGiftId)&&tier.tiktokGiftId==giftId)
            {type=tier.role==ArenaFighterRole.Archer?GiftEventType.ArcherEntry:tier.role==ArenaFighterRole.Guardian?GiftEventType.GuardianEntry:GiftEventType.Rose;mapped=true;break;}
        }
        if(!mapped){Review(json,"Eşleşmemiş hediye: "+giftId);return false;}
        int repeats=Math.Max(1,(int?)data["repeatCount"]??1);int giftType=(int?)data["giftType"]??0;
        if(giftType==1&&string.IsNullOrEmpty(group))
        {Review(json,"Combo kimliği eksik; çift sayımı önlemek için inceleme gerekiyor");return false;}
        string room=data["roomId"]?.ToString()??data["monitorExtra"]?["room_id"]?.ToString()??settings?.channel??"";
        var gift=new ArenaGiftEvent{eventId="tikfinity:"+room+":"+messageId,comboId=giftType==1?room+":"+giftId+":"+group:null,viewerId=viewer,displayName=(string)data["nickname"]??(string)data["uniqueId"]??viewer,avatarUrl=GetAvatarUrl(data),count=repeats,type=type};
        bool accepted=(destination!=null?destination:ArenaGiftInbox.Instance).Submit(gift);if(!accepted)Review(json,"Hediye kaydedilemedi");return accepted;
    }

    static string GetAvatarUrl(JObject data)
    {
        string avatar=(string)data["profilePictureUrl"];
        if(data["userDetails"]?["profilePictureUrls"] is JArray pictures)
        foreach(var picture in pictures)
        {
            string candidate=picture.ToString();
            if(Uri.TryCreate(candidate,UriKind.Absolute,out var photo)&&photo.Scheme=="https"&&(photo.AbsolutePath.EndsWith(".jpeg",StringComparison.OrdinalIgnoreCase)||photo.AbsolutePath.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase)||photo.AbsolutePath.EndsWith(".png",StringComparison.OrdinalIgnoreCase)))
            {avatar=candidate;break;}
        }
        return avatar;
    }
#if UNITY_EDITOR
    public void ConfigureForTest(ArenaLiveSettings value){settings=value;}
#endif
    void Review(string json,string reason)
    {
        if(replaying)return;
        Directory.CreateDirectory(Application.persistentDataPath);
        string file=Path.Combine(Application.persistentDataPath,"live-review-"+DateTime.UtcNow.ToString("yyyyMMdd")+".jsonl");
        File.AppendAllText(file,new JObject{{"utc",DateTime.UtcNow.ToString("O")},{"reason",reason},{"payload",JToken.Parse(json)}}.ToString(Newtonsoft.Json.Formatting.None)+Environment.NewLine);
        UnmappedEvents++;status=reason;
    }
    public int ReplayReviewed()
    {
        int accepted=0;replaying=true;
        try
        {
            foreach(var file in Directory.GetFiles(Application.persistentDataPath,"live-review-*.jsonl"))
            foreach(var line in File.ReadLines(file))
            {
                try{var row=JObject.Parse(line);if(Process(row["payload"].ToString(Newtonsoft.Json.Formatting.None)))accepted++;}
                catch(Exception exception){Debug.LogWarning("İnceleme kaydı atlandı: "+exception.Message);}
            }
        }
        finally{replaying=false;}return accepted;
    }
    void OnApplicationQuit(){Disconnect();}
    void OnDestroy(){cancellation?.Cancel();if(instance==this)instance=null;}
}
