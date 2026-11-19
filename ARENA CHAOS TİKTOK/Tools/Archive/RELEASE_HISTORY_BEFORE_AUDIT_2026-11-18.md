# ARENA CHAOS — yerel oyun sürümü

## 18 Kasım — süreli turlar ve karakter sınıfları

Varsayılan 3 dakika, F1'den 3/5 dakika seçimi; son kişi kalınca tur devam eder. Bot takviyesi kalabalığı korur. Düşük canda kaçış, dolaşma ve menzil koruyan taktik AI eklendi. Müzik kaynağı sahne geçişinde yaşamaya devam eder. Gül temel dövüşçü; ayrı test hediyeleri okçu ve kalkanlı muhafız getirir. Gerçek hediye kimlikleri `GiftRoster.asset` içinde eşleştirilmelidir. Ayrıntılar: [TIMED_ROUNDS_AND_CLASSES.md](./TIMED_ROUNDS_AND_CLASSES.md).

Tur/takviye/müzik, sınıf/ok isabeti/kaçış, yetenek ve giriş testleri geçti; çalışma zamanı hatası yok. Windows build başarılı: 0 hata, 495 paket/shader uyarısı.

## Yeni görsel ve oynanış sürümü

Son ayrıntı revizyonu: bütün dış zeminin döşemesi ve arka galeriler, bağımsız metal HUD kartları ve can çubukları, 12 yeteneğin ayrı görsel imzası eklendi. Girişin ek beklemesi 0,25 saniyeye indirildi. Meteor, gönderen dışındaki bütün rakipleri kalkan/zırh/giriş sırası dahil eler; hacimli ateş, parlama, duman ve şok halkası oluşturur. Son kaynak ve Windows build 0 hatayla tamamlandı (509 paket/shader uyarısı). Yeni meteor ve yetenek testleri 0 çalışma zamanı hatasıyla geçti. Görüntüler `Tools/TestResults/PowerShowcase.png` ve `MeteorDetonation.png` altındadır.

17 Kasım güncel değişiklikler ve dosya dökümü: [V2_IMPLEMENTATION.md](./V2_IMPLEMENTATION.md). Tekli giriş kuyruğu, menteşeli kapı, Cinemachine kamera, çevre salonu, yenilenen HUD, 12 süreli güçlendirme, müzik ve izleyici kimliği/profil fotoğrafı bağlantı noktası eklendi. Gerçek LIVE adaptörü bağlı değildir; model portreleri yalnızca yedektir.

Yeni Windows build: 0 hata, 509 uyarı. GTX 1650 / 1080p kısa bağımsız oyun testleri: 10 kişi 261,1 FPS, 20 kişi 240,8 FPS, 50 kişi 190,2 FPS; tamamı PASS, 0 çalışma zamanı hatası. Aşağıdaki önceki ölçümler tarihsel kayıttır.

## 17 Kasım 2026 — devam durumu

- Güncel içerik doğrulaması geçti: 14040 animasyon bağı, 47 materyal yuvası, 0 hata. Play testi geçti: üç katılımcının girişi tamamlandı, hediye doğru karaktere yönlendirildi, 19 animasyonlu temas ve 0 çalışma zamanı hatası kaydedildi.
- Dövüşçü hareketi `FixedUpdate` içine taşındı; ilerleme fizik konumu, hedef yönü ve sabit fizik adımıyla hesaplanıyor. Girişteki dövüşçü AI güncellemesine katılmıyor.
- Yayın penceresi odağı kaybettiğinde oyunun devam etmesi için arka planda çalışma açıldı.
- Editör testi artık simülasyon süresini ölçüyor, 120 saniyelik gerçek süre sınırı uyguluyor ve tamamlanmamış girişleri başarısız sayıyor.
- Tur yenileme testi geçti: PLAYER şampiyon seçildi, sonraki tur 10 dövüşçüyle kuruldu; 0 hata.
- Windows build başarılı: 0 hata, 510 uyarı, 162842659 bayt. Uyarılar arasında `ConvGeneric` paket compute shader'ının desteklenmeyen varyant uyarıları bulunuyor; build uyarısız değildir.
- GTX 1650 üzerinde görünür pencerede 1920×1080 bağımsız oyun testi, her biri 3 saniye ısınma ve 10 saniye örnekleme: 10 karakter 304,4 FPS / 348 MB / 111 temas; 20 karakter 279,1 FPS / 348 MB / 202 temas; 50 karakter 187,2 FPS / 382 MB / 495 temas. Üçü de PASS, 0 hata, geçerli materyaller ve 0,9–1,0 metre kök yüksekliği. Bellek Unity allocated CPU belleğidir; VRAM ölçümü değildir. Test canları yükseltilmiştir.
- İlk gizli pencere denemesi ekran görüntüsü üretemediği ve çizim yükünü temsil etmediği için performans değerlendirmesinden çıkarıldı; sonuç dosyaları görünür pencere denemeleriyle yenilendi.
- Raporlar `Tools/TestResults` ve `Builds/TestResults` altında. Oyun `Builds/Windows/ARENA_CHAOS.exe`. 30 dakikalık kararlılık, LIVE Studio ile eşzamanlı performans, 9:16 kadraj ve gerçek TikTok bağlantısı bu kısa testlerle doğrulanmış değildir.

## Çalıştırma

Unity 6000.6.7f1 ile `Assets/Scenes/SampleScene.unity` sahnesini açıp Play'e basın. Oyunun yeni içeriği `Assets/Resources/ArenaChaos/Content.asset` üzerinden yüklenir; Inspector'da eski Male Athlate referansını elle değiştirmeniz gerekmez. KayKit içerik kataloğu bulunamazsa önce `Arena Chaos > Build Imported Content`, ardından `Validate Imported Content` menülerini çalıştırın.

`F1`: operatör test paneli. `R`: kapıdan yeni test oyuncusu; `H`: can; `A`: saldırı; `S`: hız; `F`: saldırı güçlendirmesi; `B`: kalkan; `M`: alan patlaması. Önce R ile TestViewer karakterini katın. Güçlendirmeler gönderenin en son hayatta olan karakterine gider. Aynı test adıyla tekrar R, yeni bir karakter oluşturur. Aktif/katılım sırasında toplam sınır 50'dir. Gerçek coin veya ödeme işlenmez.

Son dövüşçü şampiyondur. Sonuç kartından dokuz saniye sonra sahne yeni tur için yeniden kurulur. Galibiyet sayıları yerel PlayerPrefs içinde kullanıcı adına göre saklanır. Oyunun bu sürümünde TikTok LIVE bağlantısı yoktur; yayın arayüzü bunu simülasyon olarak belirtir.

## Görsel içerik ve çalışma şekli

- Barbarian, Knight, Mage, Ranger, Rogue ve Rogue_Hooded: hazır KayKit modelleri ve kendilerine ait atlas dokuları. PLAYER da aynı aileyi kullanır. Yeni katılanın görünümü rastgele seçilir.
- Eşleşen Generic iskelette gerçek KayKit klipleri: gard, yürüyüş, yumruk, tekme, vurulma, düşme, tezahürat ve oturma. Humanoid dönüşümü yapılmadı. Root Motion kapalı; klip kopyalarındaki global konum eğrileri temizlendi. Yürüyüşün üst gövdesinde gard maskesi vardır.
- Hasar, animasyonun temas penceresindeki el/ayak taramasından gelir. Bir saldırı birden fazla rakibe değebilir, aynı rakibe o saldırıda ikinci kez hasar vermez. Darbe sesi ve havuzlanmış kıvılcım gerçek isabette çıkar.
- Taş duvarlar, sütunlar, döşemeler, flamalar, taş giriş tünelleri ve ahşap tribünler. Arena sınırı 16 × 16 metre kalır. Kaynak paket dosyaları bu entegrasyonda değiştirilmez.
- 48 modellenmiş seyirci, altı ortak tezahürat mesh'i kullanır. Seyircilerde Animator, Rigidbody ve AI çalışmaz. Grup hareketi 15 Hz ile güncellenir.
- İki kapının bağımsız katılım sırası, kapalı backstage, yükselen kapı, boş çıkış konumu seçimi ve giriş kamerası bulunur. Kamera odağı beş saniyeden sık tekrarlanmaz.
- Üst yayın şeridi, tur/süre/hayatta sayısı, ilk üç eleme/gerçek hasar skoru, üç satırlı eleme akışı, giriş bildirimi, ayrı test destekçi sayacı ve şampiyon kartı.
- Kıvılcım, duman, ateş ve şok dalgası için Kenney dokuları; yumruk/ayak/kapı sesleri; lisanslı ortam uğultusu ve tezahürat. Ses atfı `THIRD_PARTY_NOTICES.md` dosyasındadır.
- Kendi URP profili: render scale 1.0, 4× MSAA, iki gölge kademesi, 2048 ana ışık gölgesi. Bu ayarlar tek başına FPS garantisi değildir. Windows varsayılan görüntüsü 1920 × 1080 olarak ayarlanır.

## Test ve yeniden üretim

Editör test işleri `Assets/Editor/ArenaEditorJobs.cs` üzerinden yürür. Sonuçlar `Tools/TestResults` altındadır. `Tools/ArenaJob.txt` içine tek komut yazılabilir: `build-content`, `validate`, `play-test`, `round-test`, `build-windows`. Bu dosya yalnızca açık Unity Editor tarafından tüketilir; gerçek oyun çalıştırılabilir dosyasında bu editör otomasyonu bulunmaz.

Windows kısa yük testi, çıktı oluşturulduktan sonra `ARENA_CHAOS.exe -arenaSmokeTest -arenaFighters 10` ile başlar. 20 ve 50 sayıları da desteklenir. Ölçüm sırasında can artırılır; bu yalnızca test seçeneğinde uygulanır. Üç saniye ısınma ve on saniyelik örnek sonrasında isabet, Console, materyal, ortalama FPS ve CPU tarafındaki Unity bellek ölçümü kaydedilir; oyun kendiliğinden kapanır. Sonuçlar `Builds/TestResults` altındadır. Bu test uzun yayın kararlılığı veya LIVE Studio ile eşzamanlı performans onayı yerine geçmez.

## Bilinen sınırlar

Gerçek LIVE hediye olayları, bağlantı kopması/yeniden bağlanma, olay kimliği ve combo tekrar engelleme henüz bağlı değildir. Bu alanlar simülasyonla veya görsel tamamlanmayla bitmiş sayılmaz. KayKit'in ücretsiz paketinde yumruk ve tekme klipleri kullanılır; mevcut olmayan profesyonel kroşe/aparkat klipleri varmış gibi sunulmaz. Seyirciler ortak sabit tezahürat pozlarıyla hareket eder; ayrı yüz animasyonları yoktur. Yayının son 9:16 güvenli alanı ve ses seviyeleri LIVE Studio'da ayrıca kontrol edilmelidir.
