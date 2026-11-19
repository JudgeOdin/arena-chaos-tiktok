> Tarihsel tasarım/uygulama kaydıdır. Güncel davranış ve sınırlar için [LOCAL_RELEASE_NOTES.md](./LOCAL_RELEASE_NOTES.md) dosyasını kullanın.

# Arena görsel ve oynanış geliştirmesi — 17 Kasım 2026

## Ayrıntı ve görünürlük revizyonu

- Dış döşeme arenanın bütün çevresine uzatıldı; taş/ahşap bantlar, basamak, korkuluk ve kapısı çıkarılmış kemerli geçitlerle yükseltilmiş arka galeri eklendi.
- Giriş varsayılanları: sürgü 0,35 s, kanat 0,4 s, yürüyüş 1,3 s, sıra arası 0,25 s. İlk sürümün 3 saniyelik ek sıra aralığı kaldırıldı. Aynı anda tek giriş ve kapı sıralaması korunuyor.
- HUD artık ayrı köşeli metal sıralama kartları, portre, canlı renkli can çubuğu, eleme/hasar/etki satırı ve bağımsız üst bilgi plakaları kullanır. Profil resmi önceliği korunur.
- `ArenaPowerVisual.cs`: 12 yeteneğin ayrı görsel imzası; iki el kemiğini dünya ölçeğinde takip eden alev/çekirdek/köz katmanları, elektrik yayları ve izleri, ağır güç tozu, buz rengi ve kristaller, patlayıcı kıvılcım, vampir enerjisi, gölge dumanı, kalkan yörüngeleri, zehir bulutu, dönen ateş ve genişleyen zaman halkaları. Dinamik yetenek ışıkları en fazla 6'dır.
- Alev materyali parlak eklemeli karışımla güncellendi; içerik yeniden üreticisi aynı ayarı korur.
- Meteor ortama düşen taş/alev çekirdeği, hacimli ateş, ışık parlaması, genişleyen şok halkası, köz ve yükselen duman kullanır. Gönderen dışındaki bütün yaşayan rakipleri, kalkan/zırh/yüksek can ve giriş sırası dahil, eler. Şampiyon kartı patlamayı kapatmamak için 1,2 saniye gecikir.
- Meteor testi gönderenin canının değişmediğini, zırhlı/kalkanlı ve sıradaki rakiplerin elendiğini doğrular. `PowerShowcase.png` ve `MeteorDetonation.png` test görüntüleridir. Aşağıdaki önceki FPS ölçümleri bu son revizyonun ölçümü olarak kullanılmamalıdır.

Son revizyonun Windows build'i başarılı: 0 hata, 509 uyarı, 165147601 bayt. GTX 1650, 1920×1080 görünür pencerede 50 karakter kısa testi PASS: 161,7 ortalama FPS, 398 MB Unity allocated CPU belleği, 530 animasyon teması, 0 hata, geçerli materyaller, 0,9–1,0 metre kök yüksekliği. Bu 10 saniyelik örnekleme uzun yayın veya 50 karakterin hepsinde eşzamanlı bütün yeteneklerin yük ölçümü değildir.

## Uygulanan değişiklikler

- `Assets/ArenaPresentationDirector.cs`: iki kapı için ortak ve sınırlı FIFO kuyruğu; tek aktif giriş; dolu kuyrukta yeni karakter oluşturmadan ret; varsayılan 3 saniye ara. Mevcut `wall_doorway_door` parçası kendi menteşesinden açılır. Önce sürgü, sonra tahta kanat, sonra yürüyüş; çıkıştan sonra ters sırada kapanış. Süreler Inspector'da. Cinemachine sanal kamerası kontrollü yaklaşma, takip ve dönüş yapar. Sıra oyun zamanı ile durur. Kullanıcı tek kanatlı kapıyı kabul etti.
- `Assets/KayKitArenaEnvironment.cs`, `Assets/ArenaStageOneSetup.cs`: mevcut KayKit mimarisiyle çevre salonu, yüksek duvarlar, sütunlar, flamalar, animasyonlu meşaleler, sıcak ortam ve sis. Ana savaş alanı korunur.
- `Assets/ArenaBroadcastHUD.cs`: koyu dokulu paneller, bronz sınırlar, can/eleme/hasar ve aktif güç bilgisi, elenen oyuncu görünümü, model portreleri, sınırlı ve animasyonlu bildirim kuyruğu.
- `Assets/ArenaPowerUps.cs`, `Assets/ArenaPowerSettings.cs`: 12 süreli etki; yenilenen süre, tekrar kullanım sınırı, birlikte hesaplanan hız/hasar değişiklikleri, gerçek temas sonrası yanma/zehir/can emme, kalkan, savurma ve alan hasarı. Dev güç model ölçeğini değiştirir; fizik kökünü büyütmez. Gölge etkisi bittikten sonra orijinal materyaller geri gelir. F1 operatör paneli bu etkileri gönderenin karakterinde deneyebilir.
- `Assets/ArenaFighter.cs`, `Assets/KayKitFighterDriver.cs`, `Assets/BattleArenaManager.cs`, `Assets/GiftEventType.cs`, `Assets/GiftEventSimulator.cs`: mevcut savaş ve hediye akışına entegrasyon.
- `Assets/ArenaAudioEffects.cs`, `Assets/ArenaContent.cs`, `Assets/Editor/ArenaContentBuilder.cs`: import edilen `Assets/Audio/the_final_battle.ogg`, streaming müzik, ayrı müzik/efekt/ortam/tezahürat seviyeleri; havuzlanan darbe ve patlama efektleri, yoğun darbe seslerine sınır. Efekt sesleri mevcut Kenney kayıtlarını kullanır; her yetenek için ayrı yeni ses kaydı eklenmiş değildir.

## TikTok profil fotoğrafları

- `Assets/ArenaViewerProfiles.cs`: kullanıcı kimliğine bağlı HTTPS fotoğraf yükleme; eşzamanlı indirme ve önbellek sınırı, zaman aşımı, başarısız indirmede tekrar aralığı ve tur sonunda temizlik.
- Canlı adaptör `BattleArenaManager.ApplyGiftToViewer(type, viewerId, displayName, avatarUrl)` çağırmalıdır. Aynı görünen ada sahip izleyiciler `viewerId` ile ayrılır.
- HUD önce gerçek izleyici fotoğrafını kullanır. Fotoğraf yoksa/yüklenemiyorsa model portresi yedektir. Model portresi gerçek TikTok profil fotoğrafı diye sunulmaz.
- Gerçek LIVE bağlantısı ve gerçek TikTok CDN fotoğrafı bu yerel testte doğrulanmadı. Bu çalışma bir LIVE istemcisi eklemez.

## Üretilen ayarlar ve görseller

- `Assets/Resources/ArenaChaos/PowerUps.asset`: süre, tekrar kullanım aralığı ve güç değerleri.
- `Assets/ArenaChaos/Portraits/Fighter_0.png`–`Fighter_5.png`: model portreleri. `Arena Chaos > Bake Fighter Portraits` ile tekrar üretilebilir.
- Müzik referansı `Assets/Resources/ArenaChaos/Content.asset` içindedir. Lisans kaydı `THIRD_PARTY_NOTICES.md` içinde.

## Doğrulama

Son Windows build başarılı: 0 hata, 509 uyarı, 165133827 bayt. Görünür pencerede 1920×1080, GTX 1650, 3 saniye ısınma ve 10 saniye örnekleme: 10 karakter 261,1 FPS / 362 MB / 119 temas; 20 karakter 240,8 FPS / 362 MB / 200 temas; 50 karakter 190,2 FPS / 376 MB / 496 temas. Üçü de PASS; 0 hata, geçerli materyaller ve 0,9–1,0 metre kök yüksekliği. MB değeri Unity allocated CPU belleğidir. Bu yük testinde can yükseltilir; bütün karakterlerde eşzamanlı 12 güçlendirme yükü ölçülmüş değildir.

Giriş/dövüş, güçlendirme, profil kimliği/müzik ve tur yenileme testleri geçti. Gerçek TikTok CDN fotoğraf indirmesi test edilmedi.

- `Assets/Editor/ArenaEditorJobs.cs`: yeniden yükleme sonrası test takibi, gerçek sahne başlangıcını bekleme, kesilen testin kapanması, eşzamanlı giriş kontrolü.
- `play-test`: tek aktif giriş/iki bekleyen, girişlerin tamamlanması, doğru hediye alıcısı ve gerçek animasyon teması.
- `power-test`: temas hasarı, can emme, zamanla hasar, kalkanın hasar emmesi, 12 etkinin başlaması/bitmesi, değiştiricilerin geri dönmesi.
- `profile-test`: aynı görünen adlı iki ayrı kimlikte doğru hediye yönlendirmesi ve gerçek müzik oynatımı.
- `round-test`: şampiyon ve otomatik sonraki tur.
- Ayrıntılı raporlar `Tools/TestResults`; bağımsız oyun raporları `Builds/TestResults`.

## Sınırlar

Bu kısa testler 30 dakikalık yayın kararlılığı, VRAM ölçümü, LIVE Studio eşzamanlı performansı, 9:16 kadraj veya gerçek hediye olaylarının mükerrer gönderim güvenliği yerine geçmez. Testte kullanılan karakter portreleri canlı izleyici fotoğrafı değildir. Geçmiş Windows ölçümleri yeni derleme ölçümleriyle karıştırılmamalıdır.

Editör Visual Studio 2.0.28 paketinde eksik olan `SdkStyleProjectGeneration.cs`, Unity'nin aynı sürüm resmi paket arşivinden geri yüklendi. Onarım oyun mimarisini değiştirmez.

