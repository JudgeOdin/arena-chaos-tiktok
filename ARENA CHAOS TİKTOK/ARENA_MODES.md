# Arena Chaos — Modlar ve yayın operatörü

Unity sahnesi `Assets/Scenes/SampleScene.unity`. Windows başlatıcısı `Builds/Windows/ARENA_CHAOS.exe`.

## Mod seçimi

F2 → **Modlar ve yerel sohbet** → mod düğmesine bas. Geçiş sonuç ekranındaki geri sayım tamamlanınca yapılır. Turnuva kaydı başladıysa bütün eleme ağacı tamamlanır. Henüz kayıt bulunmayan Turnuva bekleme ekranından elle çıkılabilir. Rotasyon alanına mod numaralarını virgülle yaz, örneğin `0,2,4,6`. Boş alan rotasyonu kapatır. Elle seçilen sonraki mod rotasyona önceliklidir.

| No | Mod | Amaç ve komutlar |
| --- | --- | --- |
| 0 | Herkese Karşı | Süre dolana kadar mevcut KO ve hasar puanlarını topla. `!katıl`. |
| 1 | Turnuva | Yayını beğenerek sekiz kişilik eleme ağacına gir. |
| 2 | Kralı Koru | Rakip kralı düşür. Süre dolduğunda kral can yüzdeleri sonucu belirler. `!katıl`, `!mavi`, `!kırmızı`. |
| 3 | Zombi İstilası | İstiladan kurtul. Zombinin elediği izleyici aynı adla zombi olur. Yeni katılanlar sağ kalan olarak başlar. `!katıl`. |
| 4 | Altın Kaçakçısı | Altını üç kez kendi kasana taşı. Süre dolarsa teslimat skoru geçerli. `!katıl`, `!mavi`, `!kırmızı`. |
| 5 | Köprü Savunması | Kapıyı üç dalga koru. `!katıl`. Aralarda `!tamir` veya `!okçu` ile oy ver. |
| 6 | Dev Avı | Kırmızı saldırı alanlarından kaç ve devi yen. `!katıl`. |
| 7 | Kelle Avı | Hedefken zamanla puan biriktir. Hedefi eleyince işareti devral. `!katıl`. |
| 8 | Sınıf Seçimi | Açılıştaki 12 saniyede `!temel`, `!okçu` veya `!muhafız`. `!katıl` varsayılan temel dövüşçüyü verir. |
| 9 | Şampiyonun Rövanşı | Normal skorla tur birincisi belirlenir. Önceki kazanan geri gelirse taç alabilir. İlk geçerli KO ve sonraki taç devri sonuçta ayrı izlenir. `!katıl`. |
| 10 | Yayıncı Şampiyonu | F2’ye girilen davetli şampiyon adıyla Dev Avı kuralları uygulanır. Rakibi yerel oyun AI’ı oynatır. `!katıl`. |

Takım modunda elenen izleyici `!destek` verebilir. Aynı taraftan iki farklı destek on saniyelik iyileşme alanı açar. Normal sonuç ekranında `!hazır` bir sonraki uygun ücretsiz tura kayıt verir. Bireysel modlarda `!düello oyuncu` komutuna karşı taraf `!düello gönderen` ile cevap verirse iki oyuncu uygun bir sonraki turda karşılaşır. Ana maç sürer.

## Hediyeler ve yerel deneme

- F2’de **Takım Kalkanı** ve **Sonraki Tur Kuralı** satırlarına yayınınızdaki gerçek hediye kimliklerini gir. `!hedef kral` veya `!hedef kullanıcı` ile kalkan hedefi seçilir. Hedef uygun olana kadar teslimat bekler.
- Kural hediyeleri sıraya girer. Sırası gelen gönderenin seçimi için 20 saniye var. `!kural hız`, `!kural dar`, `!kural merkez`. Süre aşımı hız turu seçer. Uygun turlar bireysel skor turlarıdır.
- Ücretsiz girişten sonra gelen Gül aynı adla bir takviye oluşturur. F2’de alan Meteoru açılırsa düşüş noktası yaklaşık üç saniye görünür. Alan dışındaki savaşçılar bu varyanttan etkilenmez.
- Yerel sohbet için F2’ye kimlik, ad ve komut gir. Aynı test oyuncusu için kimliği sabit tut. F1 yerel hediye akışını çalıştırır. Windows oyuncusunda `-arenaOffline` otomatik LIVE bağlantısını engeller.
- F2 **Yayın raporunu kaydet** düğmesi, Unity kalıcı veri klasöründe `arena-session.json` üretir. `arena-clip-candidates.jsonl` UTC zamanı ve tur süresine göre klip adaylarını kaydeder.

## Üç kısa yayın klibi taslağı

1. **Kritik teslimat, 11–14 saniye.** Son teslimattan beş saniye önce taşıyıcıyı ve kasayı göster. Kasa temasında skoru büyüt. Son iki saniyede kazanan takım ve oyuncu adını ver.
2. **Taç üç saniyede el değiştirdi, 10–13 saniye.** Önce taç sahibini HUD’da göster. KO anında yakın plana geç. Taç devri ve tur skor lideri farklıysa ikisini ayrı satırda yaz.
3. **Dev düşüyor, 12–15 saniye.** Kırmızı alanın çıkışıyla aç. Kaçıştan son darbeye kes. Boss canı sıfıra indiğinde toplam hasar ve emilen kalkan hasarını sonuç kartında göster.

Aday zaman damgalarını mevcut `Tools/Video` promo çalışma akışında kesim noktası olarak kullan. Bir aday, kayıt dosyası veya TikTok gönderisi oluşturmaz.

Klip listesindeki UTC işaretleri kayda göre saniyeye çevirmek için `python Tools/Video/record_clips.py --candidates dosya.jsonl --recording-start 2026-11-19T19:00:00+03:00 --output klipler.json` komutunu kullan. F2 yerel hediye seçicisi, hediye ID girmeden bütün etkileri mevcut kuyruğa gönderir.

## Doğrulama kayıtları

- [Mod Play Mode testi](Tools/TestResults/ArenaModes.txt) bütün modların başlangıç, bitiş ve sahne geçişini kapsar. Turnuva ağacı, yinelenen sohbet komutu, bir kişilik köprü dalgası, kral kalkanı, zombi dönüşümü, altın teslimatı, boss işareti, hedef ayrılması ve mod değişirken bekleyen hediye de denendi.
- Mevcut [hediye güvenilirliği](Tools/TestResults/GiftReliability.txt), [güçler](Tools/TestResults/PowerUps.txt), [sınıflar ve AI](Tools/TestResults/ClassesAndAI.txt), [tur döngüsü](Tools/TestResults/RoundCycle.txt), [profiller](Tools/TestResults/ViewerProfiles.txt) ve [normal maç](Tools/TestResults/PlayMode.txt) testleri geçti. Hediye testinde 50 savaşçıyla dolu arena ve bir yer açıldığında gecikmiş teslimat doğrulandı.
- Windows derleme durumu [WindowsBuild.txt](Tools/TestResults/WindowsBuild.txt) dosyasındadır. Oyuncu testleri ve ekran görüntüleri `Builds/TestResults` klasöründedir.
- Gerçek yayında sohbet paketi ve kullanıcı kimliği, gerçek hediye eşlemeleri, combo, bağlantı kopup geri geldiğinde teslimat ve TikTok’un 9:16 üst katmanlarıyla güvenli alan kontrolü ayrı doğrulamadır. [TikFinity Event API](https://tikfinity.zerody.one/)

Son Windows çıktısı 0 hata ve 0 uyarıyla derlendi. 1920×1080 testleri 10, 20 ve 50 dövüşçüyle geçti. Aynı çıktı 540×960 dikey testte 20 dövüşçüyle geçti (Intel UHD, ortalama 52,7 FPS). Bu Windows oturumunda tam dikey çözünürlük için kenarlıksız pencere kullanıldı:

```powershell
.\Builds\Windows\ARENA_CHAOS.exe -screen-fullscreen 0 -screen-width 540 -screen-height 960 -popupwindow
```

## Mod görselleri ve girişler

Kelle Avı hedefi, taç sahipleri, altın, boss, kapı ve sınıflar arenada dönen 3B işaretlerle gösterilir. İstila takımının ayrı zombi görünümü, zombiye dönüşen izleyicinin adına da uygulanır. Turnuvada iki kapı eşzamanlı açılır ve süre iki savaşçı girişini bitirince başlar. Dev ve davetli şampiyon arenanın ortasında doğar.

## Zombi İstilası görsel geçişi

İstila takımı, Quaternius'un iskelet animasyonlu modeli ve arenaya uyarlanmış dokulu malzemesiyle gösterilir. Dönüşen oyuncu aynı adla geri gelir. Eğilerek kalkış, kısa yeşil parçacık efekti ve dönüşüm sesi tek olay olarak çalışır.

540×960 Unity kontrolü: [dönüşüm pozu](Tools/TestResults/Infection-Turn.png) ve [ayakta duruş](Tools/TestResults/Infection-Character.png). Yakın plan, görsel kontrol için kullanılan test kadrajıdır.
