using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Local operator controls. Hidden during broadcasting; F2 opens configuration.</summary>
public sealed partial class ArenaLiveOperator : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    bool audioTests;
    Vector2 scroll;
    string firstTestName = "Efkan", secondTestName = "Deniz";
    string feedback="";
    ArenaLiveSettings settings;
    ArenaLiveSettings activeSettings;
    public static string SettingsPath=>Path.Combine(Application.persistentDataPath,"live-settings.json");
    public static ArenaLiveSettings LoadSettings()
    {
        var original=Resources.Load<ArenaLiveSettings>("ArenaChaos/LiveSettings");
        var value=original!=null?Instantiate(original):ScriptableObject.CreateInstance<ArenaLiveSettings>();
        if(File.Exists(SettingsPath))
        {
            try{JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath),value);}
            catch(Exception exception){Debug.LogWarning("LIVE ayarı okunamadı: "+exception.Message);}
        }
        if(value.gifts==null)value.gifts=new ArenaLiveSettings.Binding[0];return value;
    }
    public void Initialize(ArenaLiveSettings value){activeSettings=value;settings=Instantiate(value);}
    void OnDestroy(){IsOpen=false;if(settings!=null)Destroy(settings);}
    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.f2Key.wasPressedThisFrame) return;
        IsOpen = !IsOpen;
        if (!IsOpen && audioTests) PlayerPrefs.Save();
    }
    void DrawAudioTests()
    {
        var announcer = ArenaAnnouncer.GetOrCreate();
        bool enabled = GUILayout.Toggle(announcer.Enabled, "Arena spikeri açık");
        if (enabled != announcer.Enabled) announcer.SetEnabled(enabled);
        float oldVolume = announcer.Volume;
        GUILayout.Label($"Spiker sesi  %{Mathf.RoundToInt(oldVolume * 100)}");
        float volume = GUILayout.HorizontalSlider(oldVolume, .10f, 1f);
        if (!Mathf.Approximately(volume, oldVolume)) announcer.SetVolume(volume);
        GUILayout.Label(announcer.Status);

        GUILayout.Label("Birinci oyuncu adı");
        firstTestName = GUILayout.TextField(firstTestName, 36);
        GUILayout.Label("İkinci oyuncu adı");
        secondTestName = GUILayout.TextField(secondTestName, 36);
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            if (GUILayout.Button("Açılış anonsu")) announcer.Preview(ArenaAnnouncer.IntroLine);
            if (GUILayout.Button("Oyuncu girişi")) announcer.Preview(ArenaAnnouncer.EntryLine(firstTestName));
            if (GUILayout.Button("Çeyrek final")) announcer.Preview(ArenaAnnouncer.DuelLine("Çeyrek final", firstTestName, secondTestName));
            if (GUILayout.Button("Final")) announcer.Preview(ArenaAnnouncer.DuelLine("Büyük final", firstTestName, secondTestName));
            if (GUILayout.Button("Şampiyonluk")) announcer.Preview(ArenaAnnouncer.ChampionLine(firstTestName));
        }
        GUILayout.Label("Anons boyunca müzik ve seyirci sesi otomatik kısılır.");
    }

    void OnGUI()
    {
        if(!IsOpen||settings==null)return;
        var old=GUI.matrix;float scale=Mathf.Clamp(Screen.height/900f,.65f,1.5f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(16,16,Mathf.Min(660,Screen.width/scale-32),Mathf.Min(820,Screen.height/scale-32)),GUI.skin.box);
        scroll=GUILayout.BeginScrollView(scroll);
        GUILayout.Label("YAYIN OPERATÖRÜ / F2 kapatır");
        if (GUILayout.Button(audioTests ? "Yayın ayarlarına dön" : "Spiker ve ses testi"))
        {
            audioTests = !audioTests;
            scroll = Vector2.zero;
        }
        if (audioTests)
        {
            DrawAudioTests();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = old;
            return;
        }
        GUILayout.Label(ArenaLiveBridge.Current!=null?ArenaLiveBridge.Current.Status:"");
        var arena = FindAnyObjectByType<BattleArenaManager>();
        if (arena != null) DrawModeControls(arena);
        GUILayout.Label("3B görüntü ölçeği: %"+ArenaRenderQuality.Percent);
        GUILayout.BeginHorizontal();
        foreach (int percent in ArenaRenderQuality.Choices)
            if (GUILayout.Button("%"+percent)) ArenaRenderQuality.Select(percent);
        GUILayout.EndHorizontal();
        GUILayout.Label("TikFinity masaüstü Event API adresi");settings.endpoint=GUILayout.TextField(settings.endpoint??"");
        GUILayout.Label("Yayın kullanıcı adı / kanal kimliği");settings.channel=GUILayout.TextField(settings.channel??"");
        settings.connectOnStart=GUILayout.Toggle(settings.connectOnStart,"Oyun açıldığında bağlan");
        GUILayout.Label("Gerçek hediye ID'lerini yayındaki hediye listesinden gir. Fiyat tahmini yapılmaz.");
        GUILayout.Label("Oyun etkisi                         Hediye ID              TikTok hediye adı             HTTPS simge adresi");
        foreach(GiftEventType type in Enum.GetValues(typeof(GiftEventType)))
        {
            ArenaLiveSettings.Binding binding=null;
            foreach(var item in settings.gifts)if(item.type==type){binding=item;break;}
            if(binding==null)
            {
                binding=new ArenaLiveSettings.Binding{type=type,label=type.ToString(),giftId=""};
                var items=settings.gifts;Array.Resize(ref items,items.Length+1);items[items.Length-1]=binding;settings.gifts=items;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label(ArenaGiftGuide.Describe(type).Name, GUILayout.Width(155));
            binding.giftId = GUILayout.TextField(binding.giftId ?? "", GUILayout.Width(90));
            binding.label = GUILayout.TextField(binding.label == type.ToString() ? "" : binding.label ?? "");
            binding.iconUrl = GUILayout.TextField(binding.iconUrl ?? "");
            GUILayout.EndHorizontal();
        }
        GUILayout.Label("Puan: izleyici KO 100, hasar 0.1/HP; CPU katkısı varsayılan %25. Meteor bütün rakipleri eler.");
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Kaydet ve bağlan"))
        {
            try
            {
                if(!Uri.TryCreate(settings.endpoint,UriKind.Absolute,out var endpoint)||(endpoint.Scheme!="ws"&&endpoint.Scheme!="wss"))throw new InvalidOperationException("Geçerli bir ws:// veya wss:// adresi girin");
                var ids=new System.Collections.Generic.HashSet<string>();
                foreach(var item in settings.gifts)if(!string.IsNullOrWhiteSpace(item.giftId)&&!ids.Add(item.giftId.Trim()))throw new InvalidOperationException("Aynı hediye ID iki ödüle atanamaz");
                foreach (var item in settings.gifts)
                {
                    item.giftId = (item.giftId ?? "").Trim();
                    item.label = (item.label ?? "").Trim();
                    item.iconUrl = (item.iconUrl ?? "").Trim();
                }
                Directory.CreateDirectory(Application.persistentDataPath);File.WriteAllText(SettingsPath,JsonUtility.ToJson(settings,true));
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings),activeSettings);
                ArenaLiveBridge.Current.Disconnect();ArenaLiveBridge.Current.Connect();feedback=ArenaLiveBridge.IsOffline?"Kaydedildi. Yerel mod etkin.":"Kaydedildi. Bağlantı durumu yukarıda.";
            }
            catch(Exception exception){feedback=exception.Message;}
        }
        if(GUILayout.Button("Bağlantıyı durdur"))ArenaLiveBridge.Current.Disconnect();
        GUILayout.EndHorizontal();
        if(GUILayout.Button("Eşleşmemiş hediyeleri tekrar değerlendir"))
        {
            try{feedback=ArenaLiveBridge.Current.ReplayReviewed()+" kayıt kuyruğa kabul edildi (tekrarlar tek sayılır).";}
            catch(Exception exception){feedback=exception.Message;}
        }
        GUILayout.Label(feedback);GUILayout.Label("Bekleyen hediye: "+ArenaGiftInbox.Instance.PendingUnits+" / "+ArenaGiftInbox.Instance.LastError);
        GUILayout.Label("Kayıt klasörü: "+Application.persistentDataPath);
        GUILayout.EndScrollView();GUILayout.EndArea();GUI.matrix=old;
    }
}
