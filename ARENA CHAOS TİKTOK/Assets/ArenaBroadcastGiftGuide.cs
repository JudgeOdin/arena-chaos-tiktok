using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ArenaBroadcastHUD
{
    const int GiftCardsPerPage = 2;
    const float GiftPageSeconds = 6f;
    static readonly GiftEventType[] GiftPreviews =
    {
        GiftEventType.FirePunch, GiftEventType.Meteor,
        GiftEventType.ArcherEntry, GiftEventType.GuardianEntry,
        GiftEventType.Health, GiftEventType.Shield
    };

    readonly List<(GiftEventType Type, string Id, string Label, string IconUrl)> guideGifts =
        new List<(GiftEventType, string, string, string)>();
    readonly HashSet<string> guideIds = new HashSet<string>();
    readonly Text[] guideNames = new Text[GiftCardsPerPage];
    readonly Text[] guideEffects = new Text[GiftCardsPerPage];
    readonly RawImage[] guideIcons = new RawImage[GiftCardsPerPage];
    readonly Dictionary<GiftEventType, Texture2D> fallbackGiftIcons = new Dictionary<GiftEventType, Texture2D>();
    Text giftGuideTitle;
    ArenaGiftRoster guideRoster;

    void BuildGiftGuide(Transform root, bool portrait)
    {
        var guide = Panel(root, "Gift guide",
            portrait ? new Vector2(.53f, .79f) : new Vector2(.79f, .68f),
            portrait ? new Vector2(.97f, .89f) : new Vector2(.975f, .89f),
            Teal);
        var accent = new Color(.95f, .78f, .45f);
        giftGuideTitle = GiftGuideLabel(guide, "", 20, new Vector2(.06f, .80f), new Vector2(.96f, .96f), accent);

        for (int i = 0; i < GiftCardsPerPage; i++)
        {
            float top = .79f - i * .31f;
            float bottom = top - .30f;
            guideIcons[i] = GiftGuideIcon(guide, new Vector2(.06f, bottom + .02f), new Vector2(.19f, top - .02f));
            guideNames[i] = GiftGuideLabel(guide, "", 21, new Vector2(.23f, bottom + .14f), new Vector2(.95f, top), Color.white);
            guideEffects[i] = GiftGuideLabel(guide, "", 17, new Vector2(.23f, bottom), new Vector2(.95f, bottom + .14f), new Color(.70f, .87f, .90f));
        }

        var likeIcon = GiftGuideIcon(guide, new Vector2(.065f, .042f), new Vector2(.125f, .15f));
        likeIcon.texture = Resources.Load<Texture2D>("ArenaChaos/GiftIcons/Like");
        likeIcon.enabled = likeIcon.texture != null;
        GiftGuideLabel(guide, "Yayını beğen, arenaya katıl", 16, new Vector2(.15f, .035f), new Vector2(.95f, .155f), accent);
        guideRoster = Resources.Load<ArenaGiftRoster>("ArenaChaos/GiftRoster");
    }

    Text GiftGuideLabel(RectTransform parent, string text, int maximum, Vector2 min, Vector2 max, Color color)
    {
        var label = Label(parent, text, maximum, min, max, color);
        label.supportRichText = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = maximum;
        return label;
    }

    static RawImage GiftGuideIcon(RectTransform parent, Vector2 min, Vector2 max)
    {
        var icon = new GameObject("Gift icon", typeof(RectTransform));
        var rect = (RectTransform)icon.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var slot = new GameObject("Square image", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        slot.transform.SetParent(rect, false);
        var fit = slot.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1f;
        var image = slot.GetComponent<RawImage>();
        image.raycastTarget = false;
        return image;
    }

    void RefreshGiftGuide()
    {
        if (giftGuideTitle == null) return;
        guideGifts.Clear();
        guideIds.Clear();

        var bridge = ArenaLiveBridge.Current;
        if (bridge != null)
        {
            foreach (var binding in bridge.GiftBindings)
            {
                if (binding == null || !Enum.IsDefined(typeof(GiftEventType), binding.type)
                    || string.IsNullOrWhiteSpace(binding.giftId) || !guideIds.Add(binding.giftId)) continue;
                string name = binding.label == binding.type.ToString() ? null : binding.label;
                guideGifts.Add((binding.type, binding.giftId, name, binding.iconUrl));
            }
        }
        if (guideRoster != null)
        {
            foreach (var tier in guideRoster.tiers)
            {
                if (tier == null || string.IsNullOrWhiteSpace(tier.tiktokGiftId) || !guideIds.Add(tier.tiktokGiftId)) continue;
                var type = tier.role == ArenaFighterRole.Archer ? GiftEventType.ArcherEntry
                    : tier.role == ArenaFighterRole.Guardian ? GiftEventType.GuardianEntry : GiftEventType.Rose;
                guideGifts.Add((type, tier.tiktokGiftId, null, null));
            }
        }

        bool preview = guideGifts.Count == 0;
        if (preview)
            foreach (var type in GiftPreviews) guideGifts.Add((type, null, null, null));

        int pages = (guideGifts.Count + GiftCardsPerPage - 1) / GiftCardsPerPage;
        int page = Mathf.FloorToInt((Time.time - started) / GiftPageSeconds) % pages;
        giftGuideTitle.text = preview ? $"ÖRNEK ETKİLER  {page + 1}/{pages}" : $"TIKTOK HEDİYELERİ  {page + 1}/{pages}";

        for (int i = 0; i < GiftCardsPerPage; i++)
        {
            if (guideNames[i] == null || guideEffects[i] == null || guideIcons[i] == null) continue;
            int index = page * GiftCardsPerPage + i;
            bool available = index < guideGifts.Count;
            guideNames[i].enabled = guideEffects[i].enabled = guideIcons[i].enabled = available;
            if (!available) continue;

            var gift = guideGifts[index];
            var description = ArenaGiftGuide.Describe(gift.Type, arena.IsTournament);
            guideNames[i].text = preview ? description.Name
                : string.IsNullOrWhiteSpace(gift.Label) ? $"Hediye #{gift.Id}" : gift.Label;
            guideEffects[i].text = description.Effect;
            if (!fallbackGiftIcons.TryGetValue(gift.Type, out var fallback))
            {
                fallback = Resources.Load<Texture2D>("ArenaChaos/GiftIcons/" + gift.Type);
                fallbackGiftIcons.Add(gift.Type, fallback);
            }
            guideIcons[i].texture = viewerProfiles.Resolve("gift:" + gift.Id, gift.IconUrl) ?? fallback;
            guideIcons[i].enabled = guideIcons[i].texture != null;
        }
    }
}

static class ArenaGiftGuide
{
    public static (string Name, string Effect) Describe(GiftEventType type, bool tournament = false) => type switch
    {
        GiftEventType.Rose => ("Gül", tournament ? "Yayın desteği" : "Temel savaşçı girişi"),
        GiftEventType.Health => ("Can", "+15 azami can"),
        GiftEventType.Attack => ("Saldırı", "+4 saldırı gücü"),
        GiftEventType.Speed => ("Hız", "+0,35 hareket hızı"),
        GiftEventType.FirePunch => ("Alevli Yumruk", "İsabetler rakibi yakar"),
        GiftEventType.Shield => ("Kalkan", "+35 kalkan"),
        GiftEventType.MegaBlast => ("Büyük Patlama", "Rakiplere 28 hasar"),
        GiftEventType.Lightning => ("Şimşek Hızı", "Hareketi ve saldırıyı hızlandırır"),
        GiftEventType.Giant => ("Dev Güç", "Savaşçıyı büyütür ve güçlendirir"),
        GiftEventType.Ice => ("Buz Zırhı", "Hasarı azaltır, rakibi yavaşlatır"),
        GiftEventType.Explosive => ("Patlayıcı Yumruk", "İsabet çevreye hasar verir"),
        GiftEventType.Vampire => ("Vampir Gücü", "Vurdukça can kazanırsın"),
        GiftEventType.Shadow => ("Gölge Modu", "Saydamlaştırır ve hız kazandırır"),
        GiftEventType.Meteor => ("Meteor", "Gönderen hariç rakipleri eler"),
        GiftEventType.HolyShield => ("Kutsal Kalkan", "Savaşçıya kutsal kalkan verir"),
        GiftEventType.Poison => ("Zehirli Darbe", "İsabetler rakibi zehirler"),
        GiftEventType.Firestorm => ("Ateş Fırtınası", "Yakındaki rakipleri yakar"),
        GiftEventType.FrozenTime => ("Donmuş Zaman", "Yakındaki rakipleri yavaşlatır"),
        GiftEventType.ArcherEntry => ("Okçu", "Uzaktan saldıran savaşçı"),
        GiftEventType.GuardianEntry => ("Muhafız", "Kalkanlı savaşçı"),
        GiftEventType.TeamShield => ("Takım Kalkanı", "Seçilen hedefe 12 sn kalkan"),
        GiftEventType.NextRule => ("Sonraki Tur Kuralı", "Hız, dar arena veya merkez"),
        _ => (type.ToString(), "")
    };
}
