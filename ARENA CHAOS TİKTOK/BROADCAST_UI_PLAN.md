> Tarihsel tasarım/uygulama kaydıdır. Güncel davranış ve sınırlar için [LOCAL_RELEASE_NOTES.md](./LOCAL_RELEASE_NOTES.md) dosyasını kullanın.

# ARENA CHAOS — Yayın arayüzü ve scoreboard

17 Kasım 2026. Durum: tasarım/uygulama planı. Bu belge oluşturulurken oyun kodu veya sahne değiştirilmedi.
Ana tamamlama planının E ve F bölümlerinin ayrıntısıdır.

## Sanat yönü

KayKit cartoon kolezyumla aynı dünya: koyu lacivert/füme zeminli kartlar, sıcak krem yazı,
altın vurgular, kırmızı/mavi takım şeritleri. Kenarlar yumuşak, kontrollü gölge ve ince çerçeve.
Eski büyük gri debug kutuları yayın görünümünde bulunmayacak. Bütün paneller neonla parlamayacak.
Portreler gerçek KayKit modellerinden bir kez üretilecek; her karede render texture çalıştırılmayacak.
Ana öncelik karakterleri ve vuruşları görebilmek; gösterişli UI arenayı kapatmamalı.

## Yayın yerleşimi

9:16 öncelikli, 16:9 alternatif yerleşim. Platform üst/alt düğmeleri için ayarlanabilir güvenli boşluk;
kesin boşluk ölçüleri LIVE Studio gerçek kadrajında doğrulanacak.

| Alan | İçerik | Davranış |
|---|---|---|
| Üst şerit | ARENA CHAOS, raund numarası, yaşayan sayısı, raund süresi | Tek kompakt satır; süre gösterimi otomatik ölüm kuralı getirmez. |
| Üst bölümde kompakt kart | İlk 3 dövüşçü, portre, ad, eleme | Altın/gümüş/bronz vurgu; dikey kadrajda arena üstüne uzun liste bindirilmez. |
| Kenar olay akışı | A, B'yi eledi; çoklu eleme; yeni lider | En fazla 3 kısa satır, süre dolunca silinir. |
| Alt kart | Son katılan ve son destek | Kısa süre görünür; hediye testleri açıkça simülasyon olarak işaretlenir. |
| Karakter üstü | Kısa kullanıcı adı, can/kalkan | Uzaklık ve kalabalığa göre sadeleşir; rastgele isim kalabalığı yok. |
| Raund sonu | Şampiyon portresi, kullanıcı adı, elemeler, hasar, galibiyetler | Kısa kutlama, ardından geri sayımla yeni raund. |

## Scoreboard verisi ve kuralları

Mevcut BattleArenaManager yalnızca LivingCount ve statusMessage tutuyor. NotifyElimination saldırganı
alıyor fakat eleme istatistiği kaydetmiyor. Yeni scoreboard bu olaylardan gerçek veri alacak.

- İzleyici kimliği, görünen ad ve fighter yaşamı birbirinden ayrı tutulur.
- Raund istatistikleri: eleme sayısı, gerçek uygulanan can hasarı, hayatta kalma süresi ve sonuç.
- Kalkanın emdiği hasar can hasarına eklenmez; istenirse ayrı istatistik olur. Overkill fazlası sayılmaz.
- Aynı eleme iki kez puanlanmaz; kendine hasar/çevre hasarı hatalı oyuncuya eleme yazmaz.
- Dövüşçü sıralaması: elemeler azalan, gerçek can hasarı azalan, hayatta kalma süresi azalan;
  tam eşitlikte sabit kimlik sırası, her karede titreşen liste yok.
- Canlı ilk 3 yalnızca aktif dövüşçülerden; raund sonuçları elenenler dahil bütün kayıtları içerir.
- Oturum galibiyetleri ayrı tutulur. Raund sıfırlama oturum galibiyetlerini silmez.
- Destekçi listesi ayrıdır; hediye olay sayısı/adedi gerçek harcama/coin değeri gibi gösterilmez.
- Gerçek TikTok kimliği henüz bağlı değil; simülasyon verisi gerçek LIVE istatistiği gibi sunulmaz.
- Uzun adlarda kısaltma, aynı görünen adda ayrı kimlik, Unicode/Türkçe karakter ve boş liste ele alınır.
- Kullanıcı adındaki metin işaretleri UI rich-text etiketi olarak yorumlanmaz.

## Seyir keyfi ve animasyonlar

- Yeni giriş: portre ve isimle tek katılım kartı; aynı anda gelişte bildirim kuyruğu ve gruplanmış özet.
- Lider değişimi: ilk sırada kısa vurgu; sürekli büyüyüp küçülen paneller yok.
- Çoklu eleme bildirimi yalnızca aynı oyuncunun belirlenmiş kısa zaman penceresindeki gerçek elemelerine bağlı.
- İlk 3'e girerken hafif kayma/parlama, ayrılırken kısa fade; sıralama paneli her karede yeniden kurulmaz.
- Büyük saldırı öncesi okunabilir kısa uyarı, ardından kontrollü kamera/efekt; tam ekran yazı bombardımanı yok.
- Kalabalık sahnede oyuncu adları için çakışma/mesafe ve en önemli dövüşçüleri öne çıkarma sınırı.

## Teknik uygulama

- Mevcut projedeki UI altyapısı kullanılır; dışarıdan yeni UI framework veya ücretli paket gerekmez.
- Tek yayın HUD'u ve ayrı operatör/test paneli; yayın modunda debug ve test düğmeleri görünmez.
- Skor kaydı UI'dan bağımsız; UI kapanınca puan toplama durmaz.
- Panel ve bildirimler havuzlanır; olay bazlı güncelleme ve en fazla 4 Hz scoreboard yenilemesi hedeflenir.
- Türkçe karakterli font, ekran boyutuna göre ölçekleme, portre atlası ve ortak UI materyalleri.
- Ayrı küçük canvas bölgeleriyle can barı değişiminde bütün ekran yeniden oluşturulmaz.
- Eski OnGUI görüntüsü yeni HUD doğrulanınca devre dışı bırakılır; eski/yeni UI üst üste kalmaz.

## Doğrulama

- 0/1/10/20/50 oyuncu, eşit skor, uzun ve tekrarlayan adlar, aynı anda çoklu eleme/giriş.
- Etkin can kaybı ile hasar puanının, tek eleme ile tek kill puanının eşleşmesi.
- Şampiyon, beraberlik/kimsenin kalmaması, yeni raund ve oturum galibiyetlerinin korunması.
- 9:16 ve 16:9 görüntüleri; son kontrol telefon büyüklüğünde izlenebilirlik üzerinden yapılır.
- GTX 1650 + yayın yazılımıyla FPS, UI yeniden çizim ve bellek maliyeti ölçülür.
- Başarı yalnızca derleme değil: gerçek Play görüntüsü, doğru skor verisi ve okunaklı yayın kadrajı.

