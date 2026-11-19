using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Local operator controls. Hidden during broadcasting; F2 opens configuration.</summary>
public sealed class ArenaLiveOperator : MonoBehaviour
{
    bool visible;
    Vector2 scroll;
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
    void OnDestroy(){if(settings!=null)Destroy(settings);}
    void Update(){if(Keyboard.current!=null&&Keyboard.current.f2Key.wasPressedThisFrame)visible=!visible;}
    void OnGUI()
    {
        if(!visible||settings==null)return;
        var old=GUI.matrix;float scale=Mathf.Clamp(Screen.height/900f,.65f,1.5f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(16,16,Mathf.Min(660,Screen.width/scale-32),Mathf.Min(820,Screen.height/scale-32)),GUI.skin.box);
        scroll=GUILayout.BeginScrollView(scroll);
        GUILayout.Label("YAYIN OPERATÖRÜ / F2 kapatır");
        GUILayout.Label(ArenaLiveBridge.Current!=null?ArenaLiveBridge.Current.Status:"");
        GUILayout.Label("TikFinity masaüstü Event API adresi");settings.endpoint=GUILayout.TextField(settings.endpoint??"");
        GUILayout.Label("Yayın kullanıcı adı / kanal kimliği");settings.channel=GUILayout.TextField(settings.channel??"");
        settings.connectOnStart=GUILayout.Toggle(settings.connectOnStart,"Oyun açıldığında bağlan");
        GUILayout.Label("Gerçek hediye ID'lerini yayındaki hediye listesinden gir. Fiyat tahmini yapılmaz.");
        foreach(GiftEventType type in Enum.GetValues(typeof(GiftEventType)))
        {
            ArenaLiveSettings.Binding binding=null;
            foreach(var item in settings.gifts)if(item.type==type){binding=item;break;}
            if(binding==null)
            {
                binding=new ArenaLiveSettings.Binding{type=type,label=type.ToString(),giftId=""};
                var items=settings.gifts;Array.Resize(ref items,items.Length+1);items[items.Length-1]=binding;settings.gifts=items;
            }
            GUILayout.BeginHorizontal();GUILayout.Label(type.ToString(),GUILayout.Width(180));binding.giftId=GUILayout.TextField(binding.giftId??"");GUILayout.EndHorizontal();
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
                foreach(var item in settings.gifts)item.giftId=item.giftId.Trim();
                Directory.CreateDirectory(Application.persistentDataPath);File.WriteAllText(SettingsPath,JsonUtility.ToJson(settings,true));
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings),activeSettings);
                ArenaLiveBridge.Current.Disconnect();ArenaLiveBridge.Current.Connect();feedback="Kaydedildi. Bağlantı durumu yukarıda.";
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
