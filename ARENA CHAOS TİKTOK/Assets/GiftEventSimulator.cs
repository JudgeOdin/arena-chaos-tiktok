using UnityEngine;
using UnityEngine.InputSystem;

public class GiftEventSimulator : MonoBehaviour
{
    [SerializeField] private string senderName = "TestViewer";
    BattleArenaManager arena;
    bool showControls;
    Vector2 scroll;

    static readonly GiftEventType[] Gifts = (GiftEventType[])System.Enum.GetValues(typeof(GiftEventType));
    string TestViewerId => "arena-test:" + senderName.TrimStart('@');

    void Start() => arena = GetComponent<BattleArenaManager>();

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || ArenaLiveOperator.IsOpen) return;
        if (keyboard.f1Key.wasPressedThisFrame) showControls = !showControls;
        if (!showControls || arena == null) return;

        if (keyboard.rKey.wasPressedThisFrame) Send(GiftEventType.Rose);
        if (keyboard.hKey.wasPressedThisFrame) Send(GiftEventType.Health);
        if (keyboard.aKey.wasPressedThisFrame) Send(GiftEventType.Attack);
        if (keyboard.sKey.wasPressedThisFrame) Send(GiftEventType.Speed);
        if (keyboard.fKey.wasPressedThisFrame) Send(GiftEventType.FirePunch);
        if (keyboard.bKey.wasPressedThisFrame) Send(GiftEventType.Shield);
        if (keyboard.mKey.wasPressedThisFrame) Send(GiftEventType.MegaBlast);
    }

    void Send(GiftEventType giftType)
    {
        ArenaGiftInbox.Local.Submit(new ArenaGiftEvent
        {
            eventId = System.Guid.NewGuid().ToString("N"),
            viewerId = TestViewerId,
            displayName = senderName,
            type = giftType
        });
    }

    void OnGUI()
    {
        if (!showControls || arena == null || ArenaLiveOperator.IsOpen) return;
        float width = Mathf.Min(440,Screen.width-24);
        GUILayout.BeginArea(new Rect(Screen.width-width-12,12,width,Mathf.Min(600,Screen.height-24)), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        if (GUILayout.Button("Yerel hediye testini kapat [F1]")) showControls = false;
        GUILayout.Label("Test kuyruğu: "+ArenaGiftInbox.Local.PendingUnits);
        if (GUILayout.Button("Test kuyruğunu temizle")) ArenaGiftInbox.Local.ClearLocal();

        var fighter = arena.FindViewerFighter(TestViewerId);
        if (fighter != null) GUILayout.Label($"Can {fighter.Health:0}/{fighter.MaxHealth:0} • Kalkan {fighter.ShieldHealth:0}");
        if (arena.IsTournament)
        {
            if (GUILayout.Button("Test: 8 izleyici kaydet"))
                for (int i=1;i<=8;i++) arena.TryJoinFromLike("arena-test:duel-"+i,"Test "+i,null);
        }
        else
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("3 dakika")) arena.SelectRoundLength(ArenaRoundLength.ThreeMinutes);
            if (GUILayout.Button("5 dakika")) arena.SelectRoundLength(ArenaRoundLength.FiveMinutes);
            GUILayout.EndHorizontal();
        }
        foreach (var gift in Gifts)
            if (GUILayout.Button(ArenaGiftGuide.Describe(gift,arena.IsTournament).Name)) Send(gift);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
