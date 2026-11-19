using UnityEngine;
using UnityEngine.InputSystem;

public class GiftEventSimulator : MonoBehaviour
{
    [SerializeField] private string senderName = "TestViewer";
    private BattleArenaManager arena;
    private bool showControls;

    private void Start()
    {
        arena = GetComponent<BattleArenaManager>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.f1Key.wasPressedThisFrame) showControls = !showControls;

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
            ArenaGiftInbox.Instance.Submit(new ArenaGiftEvent{eventId=System.Guid.NewGuid().ToString("N"),viewerId=senderName.TrimStart('@'),displayName=senderName,type=giftType});
        }
    }

    private void OnGUI()
    {
        if (!showControls && ArenaContent.Load() != null) return;
        if (GUI.Button(new Rect(Mathf.Max(10f, Screen.width - 180f), 15f, 165f, 26f),
            showControls ? "Kontrolleri kapat [F1]" : "Hediye testleri [F1]"))
            showControls = !showControls;
        if (!showControls) return;
        GUI.Box(new Rect(Screen.width - 275f, 15f, 260f, 305f), "HEDİYE SİMÜLASYONU");
        GUI.Label(new Rect(Screen.width - 260f, 45f, 230f, 25f), "Gerçek TikTok bağlantısı yok");
        if (arena != null && arena.FindSenderFighter(senderName) != null)
        {
            ArenaFighter player = arena.FindSenderFighter(senderName);
            GUI.Label(new Rect(Screen.width - 260f, 58f, 230f, 20f),
                string.Format("PLAYER CAN: {0:0}/{1:0}  KALKAN: {2:0}", player.Health, player.MaxHealth, player.ShieldHealth));
        }
        if(GUI.Button(new Rect(Screen.width-275,330,125,28),"Tur: 3 dakika"))arena.SelectRoundLength(ArenaRoundLength.ThreeMinutes);
        if(GUI.Button(new Rect(Screen.width-145,330,125,28),"Tur: 5 dakika"))arena.SelectRoundLength(ArenaRoundLength.FiveMinutes);
        DrawButton("Okcu ile katil",GiftEventType.ArcherEntry,370);
        DrawButton("Kalkanli muhafiz ile katil",GiftEventType.GuardianEntry,405);
        var powers=System.Enum.GetValues(typeof(ArenaPower));
        for(int i=0;i<powers.Length;i++)
        {
            var power=(ArenaPower)powers.GetValue(i);
            if(GUI.Button(new Rect(Screen.width-530,75+i*30,245,27),ArenaPowerUps.Title(power)))
            {
                var recipient=arena!=null?arena.FindSenderFighter(senderName):null;
                if(System.Enum.TryParse<GiftEventType>(power==ArenaPower.Fire?"FirePunch":power.ToString(),out var type))Send(type);
            }
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
