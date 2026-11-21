# ARENA CHAOS — Güncel sürüm ve yayın kurulumu

18 Kasım 2026. Bu belge güncel davranışı açıklar. Eski sürüm notları `Tools/Archive/RELEASE_HISTORY_BEFORE_AUDIT_2026-11-18.md` altında korunur. Güncel test kanıtları `Tools/TestResults` ve `Builds/TestResults` altındadır; raporların tarihine bakılmalıdır.

## Oyun

- Turlar 3 veya 5 dakika sürer. Son kişi kalması turu bitirmez; CPU takviyesi çatışmayı sürdürür. Sonuçtan 9 saniye sonra yeni tur başlar. Aynı müzik kaynağı tur geçişinde devam eder.
- Gül temel dövüşçü getirir. Okçu gerçek çarpışma taramalı ok atar ve menzil korur; muhafız daha yüksek can, kalkan ve önden hasar azaltmasına sahiptir. Atlı sınıf eklenmedi; okçu ve muhafız mevcut premium seçeneklerdir.
- Karakterler düşük canda geri çekilir, çevresinden dolaşır, hedef seçerken mesafeyi/canı/rakip yeteneklerini değerlendirir, saldırıdan kaçınır ve yerel engel/seperasyon yönlendirmesi kullanır. Bu yerel taktik sistemidir; bir LLM servisine bağlı değildir ve genel amaçlı NavMesh yol planlayıcısı değildir.
- Meteor, gönderen hariç bütün canlı rakipleri kalkan/zırh/giriş sırası dahil eler. Gönderen düşüş sırasında ölse veya sahneden silinse de başlatılmış atış tamamlanır. Varsayılan meteor aralığı 15 saniyedir. Bu aralıkta gelen kabul edilmiş meteor hediyeleri sırada bekler.
- Tek izleyicinin bütün karakterleri aynı skor kaydını paylaşır. Puan = gerçek can hasarı × 0,1 + KO × 100. CPU rakiplere katkı varsayılan 0,25 ile çarpılır. Aynı izleyicinin kendi karakterleri arasındaki hasar ve eleme puan yazmaz. Sıralama puan, KO, hasar ve sabit kullanıcı kimliğiyle çözülür; sonuç ekranı aynı listeyi kullanır. Eşitlikte kimlik sırası deterministik seçimdir.
- Dış döşeme, arka galeriler, seyirciler, ayrı büyü efektleri ve büyü sesleri vardır. Gölge/buz birleşimi saydamlığı korur; gölge materyalleri aynı karakterde yeniden kullanılır. Aynı anda görünen isimler çakışma kontrolüyle sınırlandırılır.

## Çalıştırma ve kontroller

Windows: `Builds/Windows/ARENA_CHAOS.exe`. Unity: `Assets/Scenes/SampleScene.unity`.

F1 yerel hediye test panelini, F2 yayın operatörü ayarlarını açar. Paneller varsayılan kapalıdır. Gerçek yayın görüntüsünde kapalı tutulmalıdır. Dikey örnek başlatma:

```powershell
.\Builds\Windows\ARENA_CHAOS.exe -screen-fullscreen 0 -screen-width 540 -screen-height 960
```

HUD açılıştaki en/boy oranına göre düzenlenir; dikey kamera arenayı orta alana sığdırır. Yayın uygulamasının sohbet/hediye katmanlarıyla son güvenli alan kontrolü ayrıca yapılır.

## TikTok LIVE kurulumu

Yerel TikFinity Event API WebSocket alıcısı eklendi. TikFinity üçüncü taraf masaüstü aracıdır; resmi TikTok API entegrasyonu olarak sunulmaz. Aynı bilgisayarda TikFinity çalışmalı ve yayın hesabına bağlanmış olmalıdır. Varsayılan adres `ws://127.0.0.1:21213/`.

1. F2 ekranında kanal adını ve gerekiyorsa WebSocket adresini girin.
2. Yayındaki gerçek hediye listesinden ID'leri ilgili ödüllere eşleyin. Rose, ArcherEntry ve GuardianEntry katılım ödülleridir; diğerleri karakter güçlendirmeleridir. Fiyatlar veya bölgesel hediye ID'leri tahmin edilmez. Bir ID iki ödüle atanamaz.
3. Kaydet ve bağlan seçeneğini kullanın. İsteğe bağlı otomatik bağlantı ayarı sonraki açılışta uygulanır. Ayarlar Windows oyuncusunun `Application.persistentDataPath/live-settings.json` dosyasındadır; F2 tam klasörü gösterir.
4. Küçük bir gerçek hediye ve bir combo ile kullanıcı kimliği, toplam adet, profil fotoğrafı, bağlantı kesilmesi ve tur geçişi uçtan uca kontrol edilmelidir. Yerel JSON testi bu gerçek yayın kontrolünün yerine geçmez.

Bağlantı 1–30 saniye artan aralıkla yeniden denenir. Sağlayıcının kaçırılan olayları yeniden göndermesi garanti edilmez. Kabul edilmiş olaylar diskteki kuyruğa alınır; dolu arena, dolu giriş sırası, sonuç ekranı ve yetenek bekleme süresinde silinmez. Ödül yalnızca gerçekten uygulandığında hediye sayacına işlenir. Karakteri olmayan bir izleyicinin güçlendirmesi, karakter katılana kadar bekler.

`msgId` yinelenen olayları, combo `groupId` ve kümülatif `repeatCount` ise artışları ayırır. Eksik kimlikli veya eşleşmemiş hediyeler günlük `live-review-YYYYMMDD.jsonl` dosyasında tutulur. F2'den eşleştirme düzeltildikten sonra tekrar değerlendirilebilir. Tekrar değerlendirme kalıcı tekrar engelleme kaydını kullanır. İnceleme sayacı, tekrarlar dahil kabul edilen kayıt sayısıdır; yeni verilen ödül adedi değildir.

Profil fotoğrafları olayın HTTPS URL'sinden indirilir. JPEG/PNG alternatifleri tercih edilir; desteklenmeyen biçim/ağ hatasında model portresi gösterilir. En fazla 64 fotoğraf, aynı anda 2 indirme, 8 saniye zaman aşımı, 2 MB ve 1024×1024 sınırı vardır. En eski kullanılmayan kayıtlar çıkarılır. Gerçek TikTok CDN URL'leri yayın hesabıyla ayrıca doğrulanmalıdır.

## Ayarlanabilir içerik

`Assets/Resources/ArenaChaos/` içinde:

- `MatchSettings.asset`: tur, takviye, kaçış/iyileşme, kaçınma, muhafız azaltması, CPU skor ağırlığı ve meteor aralığı.
- `GiftRoster.asset`: sınıf modeli, can/hasar/hız/menzil/bekleme/kalkan değerleri ve alternatif sınıf hediye ID'leri. F2 eşleştirmeleri önceliklidir.
- `PowerUps.asset`: 12 yeteneğin süre/bekleme/gücü; yanma/zehir süresi ve hasarı, alan yarıçapları, yavaşlama ve dev ölçeği.
- `LiveSettings.asset`: dış ayar dosyası yoksa kullanılan bağlantı varsayılanları. Gerçek hediye ID'leri bilerek boş bırakılmıştır.

## Testler

`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/RunRegression.ps1` açık Unity Editor üzerinden hediye güvenilirliği, güçler, sınıflar/AI, tur/müzik, kimlik/profil yönlendirmesi, giriş/dövüş ve içerik doğrulamasını çalıştırıp Windows build üretir. Başarısız testte durur.

Bağımsız kısa yük testi: `-arenaSmokeTest -arenaFighters 10`, `20` veya `50`. Üç saniye ısınma, on saniye ölçüm; test sırasında can yükseltilir. Uzun test: `-arenaSoakTest -arenaSoakSeconds 1800 -arenaFighters 50`. Gerçek zamanlı turlar, karma sınıflar/yetenekler, tekrar kurulan sahneler ve ayrı test kuyruğu kullanır. Normal hediye dosyasına test hediyesi eklemez. Raporlar `Builds/TestResults` altındadır.

Bellek raporları Unity CPU allocated belleğidir; VRAM değildir. FPS sonuçları eşzamanlı çalışan programlara bağlıdır. LIVE Studio/OBS ile eşzamanlı yayın testi ve gerçek para harcanan canlı hediye testi ayrı kabul adımlarıdır.

## Kalıcılık sınırı

Kuyruk JSON'u geçici dosya ve atomik değiştirme ile yazılır; önceki sürümü `.bak` olarak tutulur. Bozuk ana kayıt kurtarılırsa hasarlı dosya saklanır ve uyarı verilir. Yedek en son yazımdan eski olabilir. Oyunun verdiği efekt ile dosya yazımı tek veritabanı işlemi değildir; tam bu sırada çökme/yedekten kurtarma son teslimatı yineleyebilir. Kesintisiz çalışmada olay/comboda tek sayım test edilir; her türlü çökmede kesin bir kez teslimat garantisi verilmez. Tekrar engelleme ve inceleme dosyaları otomatik silinmez; uzun süreli işletimde klasör yedeklenmeli ve boyutu izlenmelidir.

## Shader build kapsamı

Oyunda sinir ağı modeli kullanılmadığından yalnızca Windows oyuncu build'inde kullanılmayan Unity inference shader varyantları çıkarılır. Editor paketleri değiştirilmez. Projeye `.onnx`/`.sentis` modeli eklendiğinde veya `Arena Chaos > Build > Include neural model inference` işaretlendiğinde çıkarma durur. `Tools/TestResults/InferenceShaderPolicy.txt` build'in uyguladığı politikayı kaydeder. Compute varyantlarının çıkarılması 494 uyarıyı 34 paket shader uyarısına indirdi; kalan uyarılar saklanır ve `BuildWarnings.txt` içinde listelenir. Shader paket dosyaları düzenlenerek uyarı gizlenmedi.


Dikey başlatıcı: `powershell -ExecutionPolicy Bypass -File Tools/StartPortrait.ps1`. Varsayılan 540×960, 1080 piksel yüksekliğindeki mevcut monitöre sığar ve gerçek 9:16 testinde doğrulanmıştır. 1080×1920 komutu mevcut yatay monitörde Unity/Windows tarafından 1080×1080'e sınırlandığı için Full HD dikey doğrulaması olarak kabul edilmedi. Uygun dikey ekran/çıktı ortamında `-Width 1080 -Height 1920` ayrıca denenebilir. HUD ve kamera en/boy oranı değiştiğinde yeniden düzenlenir; player penceresi yeniden boyutlandırılabilir.


## Son doğrulama kapsamı

30 dakikalık gerçek zamanlı karma yük: 10 tur, 0 hata, en çok 50 canlı karakter, sonunda bekleyen hediye 0; ortalama 149,8 FPS. Unity allocated CPU belleği ilk 394 MB, tepe 447 MB, son 443 MB. Bu test sırasında bazı yerel protokol kontrolleri için Editor ayrıca Play modunda çalıştı; FPS izole bir yayın performansı ölçümü değildir. Adapter GPU örnekleri tüm programları içerir; oyun başına VRAM ölçümü değildir.

Uzun test, son meteorun gönderen öldükten sonra tamamlanması düzeltmesinden, aynı izleyicinin kendinden puan kazanmasının engellenmesinden ve F2 ayar taslağının aktif bağlantıdan ayrılmasından önceki oyuncu derlemesinde yapıldı. Meteor ve puan değişiklikleri ayrı Editor testleriyle doğrulandı; ayar taslağı derlenip incelendi. Son Windows çıktısında 10/20/50 yatay ve 20 karakterli gerçek 9:16 kısa testler yeniden çalıştırıldı. Son binary için ikinci bir 30 dakikalık test yapıldığı iddia edilmez. İki oyuncu assembly hash'i ve rapor tarihleri `Tools/TestResults/FinalAcceptance.json` içinde yer alır.

Yerel gerçek WebSocket testi (`LiveTransport.txt`): el sıkışma, parçalı JSON, otomatik yeniden bağlantı ve hemen durdur/bağlan; dört bağlantıda yinelenen combo toplam 3 kaldı. Bu TikTok hesabına veya gerçek bir yayına bağlanmaz. Editor kod yenilemesi/Play çıkışında bağlantı görevleri iptal edilir. IDE paketi 2.0.28, eksik resmi SDK proje üretim dosyasının güncellemelerde kaybolmaması için `Packages/com.unity.ide.visualstudio` altında embedded tutulur.

F2 alanlarını değiştirmek aktif hediye eşleştirmesini anında değiştirmez; yeni taslak ancak doğrulama ve başarılı kayıttan sonra uygulanır. Bilinmeyen olaylar silinmeden inceleme kaydında bekler.

## 19 Kasım 2026 — Yayın modları

Mod seçimi, rotasyon, sohbet komutları, hediye hedefleme ve klip adayları için [Arena modları ve yayın operatörü](ARENA_MODES.md) kılavuzuna bakın.

## Yayın güvenliği ve görüntü ölçeği

- F1 hediye kısayolları yalnızca yerel test paneli açıkken çalışır. F1 ve F2 test hediyeleri ayrı bir oturum kuyruğuna gider. F2 altında LIVE ve test sayıları ayrı görünür. Test kuyruğunu temizlemek yalnızca test olaylarını kaldırır.
- Çevrimdışı başlatma, kısa oyuncu testi ve uzun yük testi otomatik LIVE bağlantısını kapatır. Çevrimdışı oturum gerçek hediye dosyasını ve yayın istatistiklerini kullanmaz. Yerel test oyuncuları yayın ölçümlerinden hariç tutulur.
- F2’de 3B görüntü ölçeği %100, %85 veya %70 seçilebilir. HUD pencerenin tam çözünürlüğünde kalır. Varsayılan %100’dür. Başlatma komutuyla geçici ölçüm için -arenaRenderPercent 85 kullanılabilir.
- Alan Meteoru işaretlendiğinde savaşçılar tehlikeden uzaklaşır. Takımlı modlarda gecikmeli vuruş, gönderen elense bile atıldığı andaki takım bilgisini kullanır.

Son denetimde 11 modun tur başlangıcı, bitişi ve sahne geçişi doğrulandı. Hediye güvenilirliği, güçler, sınıflar, tur döngüsü, izleyici profili, normal maç ve içerik denetimi geçti. Windows build raporu 0 hata ve 0 uyarı gösterdi.

Intel UHD ve 20 dövüşçüyle 10 saniyelik Windows ölçümleri: [1920×1080 %100](Builds/TestResults/Audit-Wide-100.txt) 19,2 FPS, [1920×1080 %70](Builds/TestResults/Audit-Wide-70.txt) 30,9 FPS, [540×960 %100](Builds/TestResults/Audit-Portrait-100.txt) 53,8 FPS. HUD ve arena kadrajı [dikey görüntüde](Builds/TestResults/Audit-Portrait-100.png) ve [düşük 3B ölçekte](Builds/TestResults/Audit-Wide-70.png) kontrol edildi.

[210 saniyelik dikey karma yük testi](Builds/TestResults/Audit-Soak.txt) iki turu, en fazla 50 canlı dövüşçüyü ve sıfır çalışma zamanı hatasını kaydetti. Test bittiğinde bekleyen test hediyesi sıfırdı. Canlı hesap ve OBS/LIVE Studio ile birlikte çalışma ayrı yayın doğrulamasıdır.
