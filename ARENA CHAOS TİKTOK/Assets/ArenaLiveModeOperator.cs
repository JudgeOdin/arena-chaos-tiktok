using Newtonsoft.Json;
using UnityEngine;

public sealed partial class ArenaLiveOperator
{
    string localId="local-1", localName="Yerel Oyuncu", localChat="!katıl";
    string rotationText, guestText;
    bool modeControls;
    int localGift;
    string LocalViewerId => "arena-test:" + localId.Trim();

    void DrawModeControls(BattleArenaManager arena)
    {
        GUILayout.Label("Oyun modu: "+arena.ModeLabel);
        GUILayout.Label(arena.ModeGoal);
        if (PlayerPrefs.HasKey("ArenaNextMode"))
            GUILayout.Label("Sonraki: "+BattleArenaManager.ModeName((ArenaMode)Mathf.Clamp(PlayerPrefs.GetInt("ArenaNextMode"),0,10)));
        bool previousEnabled=GUI.enabled;
        GUI.enabled=previousEnabled&&PlayerPrefs.HasKey("ArenaNextMode");
        if(GUILayout.Button("SEÇİLEN MODA ŞİMDİ GEÇ",GUILayout.Height(34)))arena.SwitchToRequestedModeNow();
        GUI.enabled=previousEnabled;
        if (GUILayout.Button(modeControls?"Mod seçeneklerini kapat":"Modlar ve yerel sohbet")) modeControls=!modeControls;
        if (!modeControls) return;

        for (int i=0;i<=10;i++)
        {
            var mode=(ArenaMode)i;
            if (GUILayout.Button(i+"  "+BattleArenaManager.ModeName(mode)))
            {
                arena.RequestMode(mode);
                feedback="Mod seçildi. Hemen başlatmak için SEÇİLEN MODA ŞİMDİ GEÇ düğmesini kullan.";
                if (arena.CanSwitchIdleTournament && mode!=ArenaMode.Tournament) arena.AdvanceMode();
            }
        }
        rotationText ??= PlayerPrefs.GetString("ArenaRotation","");
        guestText ??= PlayerPrefs.GetString("ArenaGuestChampion","");
        GUILayout.Label("Rotasyon sırası (mod numaraları, virgülle). Boş = kapalı.");
        rotationText=GUILayout.TextField(rotationText,50);
        GUILayout.Label("Davet edilen yayıncının şampiyon adı");
        guestText=GUILayout.TextField(guestText,36);
        if (GUILayout.Button("Rotasyonu ve şampiyonu kaydet"))
        {
            PlayerPrefs.SetString("ArenaRotation",rotationText);
            PlayerPrefs.SetString("ArenaGuestChampion",guestText);
            PlayerPrefs.Save();
        }
        PreferenceToggle("Dönüşümlü yan hedefler","ArenaSideObjectives");
        PreferenceToggle("Üç saniye işaretli alan Meteoru","ArenaAreaMeteor");
        int final=PlayerPrefs.GetInt("ArenaFinalRule");
        GUILayout.Label("Son 30 saniye: "+(final==0?"Kapalı":final==1?"Daralma":"Merkez kapmaca"));
        if (GUILayout.Button("Son 30 saniye kuralını değiştir"))
        {
            PlayerPrefs.SetInt("ArenaFinalRule",(final+1)%3);
            PlayerPrefs.Save();
        }
        GUILayout.Label("Yerel sohbet kimliği, adı ve komutu");
        localId=GUILayout.TextField(localId,40);
        localName=GUILayout.TextField(localName,40);
        localChat=GUILayout.TextField(localChat,100);
        if (GUILayout.Button("Yerel sohbet olayı gönder"))
        {
            var payload=JsonConvert.SerializeObject(new
            {
                @event="chat", data=new { userId=LocalViewerId,nickname=localName,comment=localChat,msgId="local-"+System.Guid.NewGuid() }
            });
            feedback=ArenaLiveBridge.Current!=null && ArenaLiveBridge.Current.Process(payload)?"Sohbet olayı kabul edildi":"Komut bu turda kullanılamıyor";
        }
        GiftEventType[] gifts=(GiftEventType[])System.Enum.GetValues(typeof(GiftEventType));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<<",GUILayout.Width(36))) localGift=(localGift+gifts.Length-1)%gifts.Length;
        GUILayout.Label(ArenaGiftGuide.Describe(gifts[localGift]).Name);
        if (GUILayout.Button(">>",GUILayout.Width(36))) localGift=(localGift+1)%gifts.Length;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Yerel hediyeyi kuyruğa al"))
        {
            feedback=ArenaGiftInbox.Local.Submit(new ArenaGiftEvent
            {
                eventId="local-"+System.Guid.NewGuid(), viewerId=LocalViewerId, displayName=localName, type=gifts[localGift]
            }) ? "Hediye kuyruğa alındı" : "Hediye kaydedilemedi";
        }
        var inbox=ArenaGiftInbox.Instance;
        GUILayout.Label("LIVE kuyruğu: "+inbox.PendingUnits+" "+inbox.PendingStatus);
        GUILayout.Label("Test kuyruğu: "+ArenaGiftInbox.Local.PendingUnits);
        if (GUILayout.Button("Yerel test kuyruğunu temizle")) ArenaGiftInbox.Local.ClearLocal();
        GUILayout.Label("Son teslimat: "+inbox.LastDelivery);
        GUILayout.Label(ArenaBroadcastMetrics.Summary);
        if (GUILayout.Button("Yayın raporunu kaydet")) ArenaBroadcastMetrics.Save();
    }

    static void PreferenceToggle(string label,string key)
    {
        bool was=PlayerPrefs.GetInt(key)==1;
        bool value=GUILayout.Toggle(was,label);
        if (value==was) return;
        PlayerPrefs.SetInt(key,value?1:0);
        PlayerPrefs.Save();
    }
}

