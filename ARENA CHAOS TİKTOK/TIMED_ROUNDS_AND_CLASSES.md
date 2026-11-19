> Tarihsel tasarım/uygulama kaydıdır. Güncel davranış ve sınırlar için [LOCAL_RELEASE_NOTES.md](./LOCAL_RELEASE_NOTES.md) dosyasını kullanın.

# Süreli turlar, taktik AI ve hediye sınıfları

18 Kasım 2026

- Varsayılan tur 3 dakika; F1 panelinden 3 veya 5 dakika seçilebilir. Süre, tur başlangıcından hesaplanır. Tek kişi kalması veya meteor turu erken bitirmez.
- Süre bitince toplam eleme, eşitlikte gerçek hasar kazananı belirler. İzleyici kimliği üzerinden farklı karakterlerin puanları birleşir. İzleyici katılmışsa botlar şampiyon seçilmez.
- Aktif dövüşçü sayısı azaldığında takviye botları küçük grup halinde çevreye gelir. BOT etiketi vardır; gerçek izleyici gibi gösterilmez. İzleyici kapı kuyruğu ve 50 aktif karakter sınırı korunur. Eski ölü botlar temizlenir.
- Taktik AI: düşük canda kaçış, çevreden dolaşma, kısa duraksama, kalabalıktan ayrılma, kenardan uzaklaşma ve güvenli mesafede sınırlı toparlanma. Okçu yakın rakipten uzaklaşıp menzilini korur. Bunlar yerel oyun AI davranışlarıdır.
- Müzik `ArenaMusicPlayer` içinde, sahneden bağımsız ve döngüde yaşar; tur yeniden kurulunca aynı AudioSource ve oynatım devam eder.

## Hediye karakterleri

| Sınıf | Can | Hasar | Farkı |
|---|---:|---:|---|
| Gül / temel dövüşçü | 100 | 16 | Yakın dövüş |
| Okçu | 140 | 22 | Gerçek yay animasyonu, 7 m menzil, havuzlanan ve uçuş yolu üzerinden çarpışan ok |
| Kalkanlı muhafız | 240 | 24 | 80 kalkan, önden gelen hasarda azalma, daha yavaş hareket |

F1 paneli iki yeni sınıfı ayrı katılım olarak test eder. Gerçek hediye kimlikleri/fiyatları uydurulmadı; `Assets/Resources/ArenaChaos/GiftRoster.asset` içindeki `tiktokGiftId` alanları canlı sağlayıcı seçilince doldurulur. Adaptör `ApplyMappedCharacterGift` kullanabilir. Profil resmi ve kullanıcı kimliği sınıflarda korunur. Atlı karakter eklenmedi; mevcut pakette at modeli bulunmadı.

## Ayarlar ve dosyalar

- `Assets/Resources/ArenaChaos/MatchSettings.asset`: tur süresi, takviye nüfusu/aralığı, kaçış eşiği ve toparlanma.
- `Assets/Resources/ArenaChaos/GiftRoster.asset`: sınıf canı, hasarı, hızı, menzili, kalkanı, modeli ve hediye eşleşmesi.
- Yeni kod: `ArenaMatchSettings`, `ArenaTacticalBrain`, `ArenaMusicPlayer`, `ArenaGiftRoster`, `ArenaClassEquipment`, `ArenaArrowPool`, `Editor/ArenaMatchContentBuilder`.
- Entegrasyon: `BattleArenaManager`, `ArenaFighter`, `KayKitFighterDriver`, `ArenaBroadcastHUD`, `ArenaAudioEffects`, `ArenaContent`, `GiftEventType`, `GiftEventSimulator`, `ArenaPowerUps`, `Editor/ArenaEditorJobs`.
- `Arena Chaos > Prepare Match And Gift Classes`: ayar varlıkları, ekipman prefabları ve Generic yay kliplerini hazırlar.

## Testler

Son Windows build başarılı: 0 hata, 495 paket/shader uyarısı. GTX 1650 / 1920×1080 görünür pencere 50 karakter kısa testi PASS: 166,2 ortalama FPS, 391 MB Unity allocated CPU belleği, 631 animasyonlu temas, 0 hata, geçerli materyaller. Ölçüm 3 saniye ısınma ve 10 saniyelik örnek içerir; uzun yayın kararlılığı ölçümü değildir.

- `classes-test`: 3/5 dakika seçimi, iki sınıfın hediye yolundan doğması, kapı girişleri, 6 gerçek ok isabeti, muhafız kalkanının azalması ve düşük canda uzaklaşma: PASS, 0 hata.
- `round-test`: tek kişinin turu bitirmemesi, takviye grubuyla 10 kişiye dönülmesi, süre sonunda skor şampiyonu, yeni sahnede aynı müzik kaynağının devamı: PASS, 0 hata.
- `power-test`: yetenekler ve meteor bağışıklığı; meteorun süreli turu erken bitirmemesi: PASS, 0 hata.
- Son giriş/dövüş testi: 32 animasyon teması, tamamlanan girişler, 0 hata.

Gerçek LIVE hediye akışı ve uzun yayın kararlılığı bu yerel testlerle doğrulanmış değildir.

