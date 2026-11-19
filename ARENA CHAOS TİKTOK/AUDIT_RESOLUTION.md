# Ayrıntılı inceleme sonrası yapılanlar

Bu liste ilk incelemedeki 17 başlığı izler. Gerçek yayın hesabına erişim gerektiren kabul kontrolleri ayrıca belirtilmiştir. Yeni asset importu gerekmemiştir.

| Başlık | Uygulama | Kanıt / sınır |
|---|---|---|
| LIVE alıcısı | TikFinity Event API için yerel WebSocket alıcısı, iptal ve yeniden bağlantı | Ham JSON ve gerçek loopback WebSocket: dört bağlantı, parçalı mesaj, otomatik/elle yeniden bağlantı, toplam combo=3 (LiveTransport.txt). Gerçek hesap/yayın testi için kullanıcı adı ve kullanılan araç bilgisi eksik. |
| Dolu arena / giriş / sonuç sırasında hediye | Diskte kalıcı, adil taranan bekleme kuyruğu | `GiftReliability.txt`: dolu 50 kişilik arena, boşalan yer, tur sonu, yeniden yükleme. |
| Combo ve cooldown | Olay tekrar engelleme, kümülatif combo farkı; uygulanamayan ödülü bekletme | Aynı mesaj + combo 1 → 3 + tekrarlanan final toplam 3 sayılır. Yetenek beklemeleri ödülü düşürmez. |
| Skor tutarlılığı | Sabit izleyici ID'siyle ortak kayıt; HUD ve şampiyon aynı sıralama | İki karakterin 10+20 rakip hasarı tek kayıtta 30; sonuç aynı izleyiciyi seçer. Kendi karakterine hasar/eleme puan yazmaz. |
| Hediye eşleştirme | F2 ayarları, ID çakışması denetimi, kaydet/bağlan ve inceleme tekrar oynatma | Gerçek hediye ID'leri ve bölgesel fiyatlar uydurulmadı; yayıncı eşleştirmesi gerekiyor. |
| Denge | CPU katkısı %25; meteor aralığı 15 sn; bekleyen meteor korunur | Meteorun bütün rakipleri eleme gücü korunur; gönderen düşüş sırasında ölür/silinirse de başlatılmış atış tamamlanır. Gerçek hediye fiyatlarına göre son ekonomi ayarı yayın hesabına bağlıdır. |
| Profil resimleri | HTTPS indirme, JPEG/PNG alternatifi, LRU 64 kayıt, 2 eşzamanlı indirme | `AvatarDownloadCache.txt`: 160×160 gerçek HTTPS PNG; 80 ek kimlik sonrası 64 sınırı. Gerçek TikTok CDN testi ayrıca gerekir. |
| Taktik AI | Can/mesafe/güç temelli hedef, kaçış, dolaşma, menzil, kaçınma, kalabalık ve engel yönlendirmesi | `ClassesAndAI.txt`: geri çekilme ve gerçek ok isabetleri. Açık arena için yerel yönlendirme; genel NavMesh/LLM sistemi değildir. |
| Sınıflar ve ekipman | Okçu, muhafız, yay çıkış noktası, gerçek çarpışan ok, önden kalkan tepkisi | Sınıf girişleri, 3/5 dakika ve kalkan tüketimi geçti. Atlı sınıf eklenmedi; alternatif premium sınıflar okçu ve muhafızdır. |
| Birleşen efektler | Buz/gölge alfa düzeltmesi; gölge materyallerinin aynı karakterde yeniden kullanılması | Yetenek kombinasyonu ve süre sonunda geri dönüş `PowerUps.txt` içinde. |
| Ayarlar | Tur/AI/CPU puanı/meteor; yanma/zehir, yarıçaplar, süre/güç/bekleme değerleri assetlerde | `MatchSettings.asset`, `GiftRoster.asset`, `PowerUps.asset`; güncel notlarda alanların açıklaması var. |
| İsim kalabalığı | Ekran konumunda çakışma denetimi ve en çok 12 isim; izleyici önceliği | 50 kişilik bağımsız test ve ekran görüntüleri. Kullanıcı adlarının rich text etiketleri kaçırılır. |
| 9:16 | Ayrı HUD ve kamera yerleşimi; oran değişince yeniden kurulum; yeniden boyutlanabilir pencere | `Standalone_20_540x960.txt` gerçek 9:16 PASS. Mevcut yatay monitörde 1080×1920 isteği kareye sınırlandığı için Full HD dikey testi sayılmadı. |
| Sonuç ekranı | Profil görseli, KO/hasar, geri sayım; aynı sıralama; metin düzeltmeleri | Tur testi yeni sahne ve aynı müzik kaynağını doğrular. |
| Ses | Mevcut vuruş/kapı/tezahürata ek özgün ateş, buz, şimşek, büyü ve meteor sesleri | Üretici `Tools/generate_spell_audio.py`; üçüncü taraf yeni kayıt indirilmedi. Son yayın ses dengesi LIVE Studio ile yapılmalı. |
| Uzun yük / yayın uygulaması | 50 kişi başlangıçlı, karma sınıf/güç ve tam tur yenilemeli 1800 sn gerçek zamanlı test | `Builds/TestResults/SoakTest.txt` ve `SoakTelemetry.csv`. LIVE Studio eşzamanlılığı bu testin kapsamına dahil değildir. |
| Uyarılar / bakım / belgeler | Kullanılmayan inference compute varyantları çıkarıldı; IDE paketindeki eksik resmi kaynak kalıcı embedded pakette onarıldı; eski belgeler tarihsel işaretlendi | `WindowsBuild.txt`, `BuildWarnings.txt`, `InferenceShaderPolicy.txt`. Güncel tek giriş belgesi `LOCAL_RELEASE_NOTES.md`. |

## Kabul için dışarıdan gereken bilgi

- Yayın kullanıcı adı, kullanılan LIVE bağlantı aracı ve gerçek hediye ID listesi.
- Açık gerçek yayınla hediye/combo/profil resmi ve bağlantı kopması kontrolü.
- LIVE Studio/OBS sahnesindeki son kadraj, ses ve eşzamanlı performans kontrolü.

Kuyruk dosyası atomik yazılır ve yedeklenir; oyun efekti ile dosya yazımı tek veritabanı işlemi değildir. Çökme/yedek kurtarma sırasında kesin bir kez teslimat garantisi verilmez. Ayrıntılar güncel sürüm notlarındadır.


Son yerel kabul kapsamı: 30 dakikalık test 10 tur, sıfır hata, sonunda sıfır bekleyen hediyeyle tamamlandı. Son meteor, puan ve ayar taslağı düzeltmeleri bu uzun testin ardından uygulandı; meteor ve puan özel Editor testleriyle, yeniden üretilen oyuncu ise kısa 10/20/50 ve 9:16 testleriyle doğrulandı. Son binary ikinci kez 30 dakika çalıştırılmadı. Hash ve rapor tarihleri `Tools/TestResults/FinalAcceptance.json` içindedir.
