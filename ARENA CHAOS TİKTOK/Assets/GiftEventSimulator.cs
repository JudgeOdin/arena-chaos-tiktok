using UnityEngine;
using UnityEngine.InputSystem;

public class GiftEventSimulator : MonoBehaviour
{
    [SerializeField] private string senderName = "TestViewer";
    private BattleArenaManager arena;

    private void Start()
    {
        arena = GetComponent<BattleArenaManager>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame) Send(GiftEventType.Rose);
        if (keyboard.hKey.wasPressedThisFrame) Send(GiftEventType.Health);
        if (keyboard.aKey.wasPressedThisFrame) Send(GiftEventType.Attack);
        if (keyboard.sKey.wasPressedThisFrame) Send(GiftEventType.Speed);
        if (keyboard.fKey.wasPressedThisFrame) Send(GiftEventType.FirePunch);
        if (keyboard.bKey.wasPressedThisFrame) Send(GiftEventType.Shield);
        if (keyboard.mKey.wasPressedThisFrame) Send(GiftEventType.MegaBlast);
    }

    private void Send(GiftEventType giftType)
    {
        if (arena != null)
        {
            arena.ApplyGiftToPlayer(giftType, senderName);
        }
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width - 275f, 15f, 260f, 305f), "HEDİYE SİMÜLASYONU");
        GUI.Label(new Rect(Screen.width - 260f, 45f, 230f, 25f), "Gerçek TikTok bağlantısı yok");
        if (arena != null && arena.PlayerFighter != null)
        {
            ArenaFighter player = arena.PlayerFighter;
            GUI.Label(new Rect(Screen.width - 260f, 58f, 230f, 20f),
                string.Format("PLAYER CAN: {0:0}/{1:0}  KALKAN: {2:0}", player.Health, player.MaxHealth, player.ShieldHealth));
        }
        DrawButton("R - Gül / Yeni Oyuncu", GiftEventType.Rose, 75f);
        DrawButton("H - Can +15", GiftEventType.Health, 108f);
        DrawButton("A - Saldırı +4", GiftEventType.Attack, 141f);
        DrawButton("S - Hız +0.35", GiftEventType.Speed, 174f);
        DrawButton("F - Alevli Yumruk", GiftEventType.FirePunch, 207f);
        DrawButton("B - Kalkan", GiftEventType.Shield, 240f);
        DrawButton("M - Büyük Patlama", GiftEventType.MegaBlast, 273f);
    }

    private void DrawButton(string label, GiftEventType giftType, float y)
    {
        if (GUI.Button(new Rect(Screen.width - 260f, y, 230f, 27f), label))
        {
            Send(giftType);
        }
    }
}
