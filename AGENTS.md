# Arena Chaos geliştirme standardı

Unity projesi `ARENA CHAOS TİKTOK/` klasöründedir. Editör sürümünü `ProjectSettings/ProjectVersion.txt` dosyasından, etkin sahne ve doğrulama akışını proje içindeki yayın notlarından ve mevcut Unity ayarlarından kontrol et.

## Tasarım kararlarını üstlen

- “Vampir ekle” gibi kısa bir istek aldığında, kullanıcının açık kısıtlarına uyarak özelliğin tasarımını üstlen. Mevcut karakterleri, maç döngüsünü ve yayın kadrajını incele. Özelliğin oyundaki rolünü, onu akılda kalıcı yapan davranışı, görsel kimliğini, sesini ve denge sınırlarını kendin belirle.
- Birkaç makul tasarım fikrini Arena Chaos'a uyum, izleyicinin tek bakışta anlayabilmesi ve oynanışa katkı açısından değerlendir. En güçlü fikri seç ve uygula. Rutin yaratıcı kararlar için kullanıcıdan seçim veya onay isteme. Birbiriyle uyumlu az sayıda mekanikle karakterin kimliğini belirginleştir.
- Tasarımı gerçek maçta dene. İlk sonuç zayıfsa okunabilirliği, zamanlamayı veya dengeyi iyileştirip tekrar dene. Teslimde seçtiğin fikri, Arena Chaos'a neden yakıştığını ve maçta gözlemlediğin sonucu kısaca anlat. Gerekli asset eksikse aşağıdaki asset akışını uygula.

## Görsel ve işitsel kalite

- Bir özellik istendiğinde oyuncunun veya yayın izleyicisinin ekranda ne görmesi, ne duyması ve neyi anlaması gerektiğini belirle. Göreve göre silüet, ölçek, animasyon, zamanlama, materyal, ışık, ses, VFX ve kamera uyumunu değerlendir.
- Önce projedeki prefab, model, animasyon, VFX ve sesleri incele. Var olan bir asseti ancak istenen işlevi, görünümü ve Arena Chaos'un görsel dilini karşılıyorsa yeniden kullan. Yeniden kullanım kararını oyun içi sonuçla destekle.
- Küre, küp, rastgele dönen geometri veya benzeri geçici gösterimleri istenen model, animasyon ya da efektin yerine koyup teslim etme. Kullanıcı prototip isterse geçici içeriği açıkça etiketle. Geometrik bir öğe tasarım gereği uygunsa görünümünü ve hareketini oyun içinde değerlendir.

## Asset eksikse

- İstenen kalite için gerekli asset projede yoksa kullanıcıya ihtiyacı bildir. Resmî mağaza veya üretici sayfasından bir ana öneri ve uygunsa bir alternatif için doğrudan link ver. Bulduğun seçeneklerin neden uyduğunu, ücret veya lisans durumunu ve görevle ilgili Unity, render pipeline, rig ya da format uyumluluğunu belirt. Doğrulayamadığın bilgiyi açıkça işaretle.
- Kullanıcı importu kendisi yapacağını söylediyse paketi ve gerekiyorsa içindeki ilgili model, prefab veya animasyonu belirt. Satın alma ve import kullanıcıya aittir. Assete bağlı görsel entegrasyonu importtan sonra tamamla. Assetten bağımsız ve görev kapsamındaki işleri ilerlet, eksik entegrasyonu sonuçta açıkça bildir.

## Teslim öncesi kontrol

- Görsel değişikliği gerçek oyun sahnesinde, normal oyun kamerasıyla ve ilgili yayın kadrajında test et. Hareketi ve zamanlamayı oynatarak izle. Kalabalık bir sahnede okunabilirliği kontrol et. Uygun olduğunda önce ve sonra ekran görüntüsü veya kısa klip göster.
- İlgili Unity Console hatalarını, prefab ve Inspector referanslarını ve bozulan davranışları kontrol et. Derlemenin geçmesiyle doğrulanan teknik durumu, Play Mode'da gözlemlenen görsel sonuçla birlikte raporla. Çalıştıramadığın kontrolleri ve eksik assetleri açıkça yaz.
