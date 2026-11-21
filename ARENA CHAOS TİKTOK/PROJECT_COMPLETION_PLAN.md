> Tarihsel tasarım/uygulama kaydıdır. Güncel davranış ve sınırlar için [LOCAL_RELEASE_NOTES.md](./LOCAL_RELEASE_NOTES.md) dosyasını kullanın.

# ARENA CHAOS — Tamamlama planı

Tarih: 17 Kasım 2026. Durum: planlama ve asset toplama; bu belge oyunun tamamlandığı anlamına gelmez.
Bu oturumda sahne, oyun kodu veya paket ayarları değiştirilmedi.

## 1. Sabit hedef ve kapsam

Tek yayıncı için Windows/Unity 6000.6.7f1 URP arena oyunu. Görsel yön: özgün cartoon taş kolezyum;
Clash of Clans benzeri sıcak, hacimli stil referanstır, o oyunun karakter/logo/assetleri kopyalanmaz.
Ana renkler kum/taş, koyu ahşap, kırmızı ve mavi kumaş; neon yerine sınırlı büyü/vuruş ışığı.
İlk yayın adayı: 10–20 dövüşçü, 50 karakter stres testi; GTX 1650 4 GB üzerinde ölçülerek karar verilir.
Yayın tasarım varsayımı 9:16; masaüstünde 16:9 test görünümü de desteklenecek.
Fizik oyun alanının boyutu ve mevcut dövüş kökü korunur.

## 2. Mevcut malzemelerin durumu

| Kaynak | Kullanım | Durum / sınır |
|---|---|---|
| KayKit Dungeon Pack 1.1 Free | Duvar, zemin, sütun, kapı çevresi, flama, basamak | Import edildi. 211 FBX; kendi License.txt dosyası CC0. Unity FBX varyantları tercih edilecek. |
| CharCrafter / Male Athlate | Mevcut Humanoid dövüşçü | Çalışan taban; kullanıcının istediği özel kaslı cartoon model kalitesi henüz onaylı değil. |
| CharCrafter diğer 4 preset | Seyirci adayları | Rig, materyal, maliyet ve görünüm incelenecek; dövüşçü gibi her birine Animator takılmayacak. |
| EEJANAI Free Fighter Animations | Gerçek yumruk/tekme klipleri | Mevcut projede; her klibin teması ve hızı ayrı doğrulanacak. |
| Normalgon ve eski procedural rig | Eski denemeler | Yeni tasarımın görünür parçası olmayacak; dosyalar otomatik silinmeyecek. |

KayKit hazır kolezyum veya kaliteli insan karakter paketi değildir. Tribün kompozisyonu ve ışıklandırma
ayrıca kurulmalıdır. Paket promosyon görseli oyunun hazır ekran görüntüsü gibi sunulamaz.

## 3. Eksik assetlerin tek listesi

1. Kenney Impact Sounds — https://kenney.nl/assets/impact-sounds
   CC0. Vuruş, ayak sesi, kapı ve temas sesleri için kaynak; her dosyanın uygunluğu dinlenerek seçilecek.
2. Kenney Particle Pack — https://kenney.nl/assets/particle-pack
   CC0. Bu bir 2D efekt doku paketidir, hazır Unity 3D efekt sistemi değildir.
   Duman, darbe ve kıvılcım dokuları URP materyal/particle sistemlerine bağlanacak.
3. Gregor Quendel Free Crowd Cheering Sounds — https://gregor-quendel.itch.io/free-crowd-cheering-sounds
   Ücretsiz sürüm, CC BY 4.0. İndirilen lisans dosyası saklanacak ve Gregor Quendel'e atıf eklenecek.
   Alkış, ortam uğultusu ve güçlü tezahürat seçilecek. Sayfadaki ücretsiz indirme seçeneği kullanılacak.
4. Animasyon açığı — https://www.mixamo.com/
   Adobe ID ile ücretsiz; Adobe FAQ ticari projelerde karakter/animasyon kullanımına izin veriyor:
   https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html
   Gereken hareketler: gardlı bekleme, kısa ileri/geri ve yan adım, hasar tepkisi, düşme.
   Bunlar doğrulanmış Mixamo klip adları değil, seçim gereksinimleridir. Katalogda uygun gerçek klipler
   birlikte seçilecek; isim veya uyumluluk uydurulmayacak. Tercih FBX for Unity, yerinde hareket,
   orijinal projeye dokunmadan Humanoid retarget testi. Root Motion kapalı.

Yeni karakter paketi bu aşamada rastgele seçilmeyecek. Mevcut Male Athlate'ın görünümü yeterli bulunmazsa
mekân ve ses çalışmasından önce tek modelin lisansı/rig/materyali/görünümü doğrulanıp seçim sabitlenecek.
Bu açık karar çözülmeden 'karakterler tamamlandı' denmeyecek.

## 4. Uygulama sırası ve teslimler

### A. Asset ve proje hazırlığı

- Yukarıdaki eksikler toplanır; ZIP dosyaları açılır, orijinal lisanslar saklanır.
- URP materyalleri, ölçek, pivot, model yönü ve mesh maliyeti kontrol edilir.
- Unity sahnesinin ve değişikliklerin geri dönülebilir kaydı alınır; kirli çalışma ağacı korunur.
- Yeni proje assetleri Assets/ArenaChaos altında; import edilen kaynak paketler değiştirilmez.
- Runtime dekor üreten eski kod ile yeni sahne dekorunun iki kez oluşması engellenir.

### B. Tam arena kompozisyonu

- Taş dövüş alanı, merkez arması, alçak ön duvar, yüksek arka mimari ve köşe sütunları.
- İki büyük kemerli giriş; içi görünmeyen backstage, açılan kapı ve sıra sistemi.
- Taş/ahşap tribünler, kırmızı/mavi flamalar; dövüş alanında gereksiz eşya yok.
- Sıcak ana ışık, serin dolgu ve sınırlı vurgular. Dövüşçüler zeminden ayrışır.
- Mevcut kamera alanı kapatmayan, okunaklı bir kadraja ayarlanır; mobil görüntü ayrıca değerlendirilir.

### C. Karakter ve animasyonun bitirilmesi

- Tek onaylı Humanoid aile; rastgele renk/saç/kıyafet seçimleri model ve materyal kataloğundan yapılır.
- Prosedürel eldiven/kostüm ekleri ancak düzgün oturuyorsa kalır; basit şekil yığılması final sayılmaz.
- Gerçek gardlı locomotion varsa kullanılır; yoksa mevcut prosedürel yürüyüşün sınırı açıkça belirtilir.
- Her yumruk/tekme: hazırlık, temas, toparlanma; el/ayak collider temasıyla tek hedefe tek hasar.
- Darbe tepkisi, eleme/düşme ve şampiyon pozu. Her modelde Avatar testi.

### D. Tribünler ve seyirciler

- Ön sıralarda gerçek modellenmiş karakterler; arkalarda daha ucuz sabit poz/LOD kalabalık.
- Her seyirciye tam fizik/AI/Animator yüklenmez. Küçük gruplarda alkış ve el sallama.
- Ortam sesi düşük seviyede; giriş, güçlü vuruş ve şampiyon anında kontrollü tezahürat.
- Seyirci tepkileri aynı karede tamamen senkron ve sürekli aynı güçte olmayacak.

### E. Yayın sunumu, arayüz ve efekt

Arayüz ve scoreboard için ayrıntılı çalışma taslağı: [BROADCAST_UI_PLAN.md](./BROADCAST_UI_PLAN.md).
KayKit tarzında yayın HUD'u, eleme/gerçek hasar skorları, ayrı destekçi listesi, katılım/eleme
bildirimleri ve şampiyon/oturum sonuçları aynı teslimin kapsamındadır. Bunlar henüz uygulanmadı.

- Kalıcı sade HUD: yaşayan sayısı, raund durumu, şampiyon ve kısa olay akışı.
- Okunabilir ad/can etiketleri; kalabalıkta görünürlük sınırı ve mesafeye bağlı sadeleşme.
- Katılım kartı ve kısa kapı odaklanması; art arda hediyede kamera sürekli zıplamaz.
- Gerçek temasta ses/efekt; darbe yokken sahte impact yok.
- Patlama: hazırlık uyarısı, merkez patlaması, şok dalgası, duman; ekranı uzun süre kapatmaz.
- Hediye test paneli yayın görünümünde gizli, ayrı operatör kontrolü mevcut.

### F. Kesintisiz oyun ve hediye simülasyonu

- Bekleme, giriş, dövüş, şampiyon ve yeni raund akışı.
- Kullanıcı kimliği/görünen ad ayrımı; aynı kullanıcı için karakter politikası belirlenir.
- Hediye sırası, event kimliği, tekrar olayları ve combo/streak sayımı simülasyonda test edilir.
- Sıfır, tek, 10, 20, 50 kişi; aynı anda iki kapıya çoklu giriş; elenenin yeniden katılması.
- Manuel raund sıfırlama ve bağlantı kesilince oyunun devam etmesi.

### G. Gerçek TikTok LIVE bağlantısı

- Görsel oyun tamamlanması ile gerçek LIVE hazır olma ayrı kontrol noktalarıdır.
- Önce resmi erişim seçenekleri güncel kaynaklardan doğrulanır. Uygun erişim yoksa bu açık engeldir;
  resmi API varmış gibi vaat verilmez. Resmi olmayan yöntem için kararlılık/koşullar açıklanır.
- Oyun hediye adaptörü üzerinden çalışır; gerçek para harcamadan önce simülasyon testi geçer.
- Tek canlı hesaba kontrollü bağlantı, yeniden bağlanma ve mükerrer hediye engeli doğrulanır.
- LIVE Studio'da gerçek yayın kadrajı ve ses seviyeleri kontrol edilir.

## 5. Performans hedefleri (ölçülmüş sonuç değildir)

- Öncelik 1080p/60 FPS; GTX 1650 ve LIVE Studio eşzamanlı testinde ayar düşürme profili hazırlanır.
- 50 dövüşçüde hedef tutmazsa gölgeler, seyirci/efekt sayısı, skinning ve isim etiketleri ölçülerek azaltılır.
- 4 GB VRAM sınırında bellek ölçümü; paylaşılan materyaller, efekt havuzu, LOD ve sınırlı ışıklar.
- Uzun testte sürekli yükselen bellek, bitmeyen coroutine veya biriken inactive fighter olmamalı.

## 6. Bitti sayılması için kontrol listesi

- [ ] Sanat yönü ve tek final karakter ailesi onaylandı.
- [ ] Gri prototip/kutu dekor kalmadı; model ve materyal eksikliği/pembe karakter yok.
- [ ] Her kapıda kapalı alanda doğma, yürüyerek giriş ve art arda sıra testi geçti.
- [ ] Gardlı hareket, yumruk/tekme temas hasarı, ıskalama, eleme ve şampiyon davranışları doğrulandı.
- [ ] Seyirci hareketi ve gerçek tezahürat sesi bağlandı; lisans/atıf dosyası hazır.
- [ ] Dikey yayın görüntüsünde adlar, canlar ve olaylar okunuyor.
- [ ] En az 30 dakikalık denemede kırmızı hata yok; üç ardışık Play/Stop testi geçti.
- [ ] 10/20/50 karakter + çoklu giriş + efekt yük testi; FPS/bellek sonuçları kaydedildi.
- [ ] Windows build, editör dışında açılıp aynı sahne/assetlerle test edildi.
- [ ] Gerçek LIVE adaptörü erişimi ve tek hesap testi doğrulandı; simülasyon gerçek bağlantı diye sunulmadı.
- [ ] LIVE Studio kadrajı/sesleri ve operatör kontrolleri denendi.

## 7. Sıradaki somut adım

Şu an yalnızca eksik ses/efekt paketleri ve locomotion klipleri toplanacak; ana sahneye entegrasyon başlamayacak.
Assetler geldiğinde bu listedeki bağımlılık sırasıyla tek uygulama süreci yürütülecek.
Küçük bölümler ayrı ayrı derlenip test edilecek, fakat her küçük değişiklik yeni bir tasarım yönüne dönüşmeyecek.



## 8. Planlanan özellik: Beğeniyle karakter geliştirme

- İlk TikTok LIVE beğenisi izleyiciyi arenaya kaydeder. Sonraki beğeniler aynı karakterin gelişimine sayılır.
- Biriken beğeniler belirli eşiklere ulaştığında karakter rastgele bir geliştirme kazanır. Eşikler ve geliştirme havuzu uygulama sırasında belirlenecek.
- Toplam beğeni ve kazanılan geliştirmeler, TikTok kullanıcı kimliğine göre kaydedilir. İlerleme sonraki maçlarda ve yayınlarda yüklenir.
- Tek karakter kuralı geçerlidir. Geliştirmelerin 1’e 1 turnuvada nasıl uygulanacağı ayrıca belirlenecek.
