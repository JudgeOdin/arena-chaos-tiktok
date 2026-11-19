> Tarihsel tasarım/uygulama kaydıdır. Güncel davranış ve sınırlar için [LOCAL_RELEASE_NOTES.md](./LOCAL_RELEASE_NOTES.md) dosyasını kullanın.

# ARENA CHAOS — Sıralı asset kontrol listesi

17 Kasım 2026. Bu liste asset toplamak içindir; sahne entegrasyonu başlamadı.

## Zaten mevcut — tekrar indirme

- [x] KayKit Dungeon Pack 1.1 Free: taş çevre, merdiven, kolon, flama ve kapı çevresi.
- [x] EEJANAI Free Fighter Animations: yumruk, tekme ve diz klipleri.
- [x] CharCrafter: Male Athlate ve dört diğer karakter; mevcut çalışan Humanoid temel.

## Sırayla toplanacaklar

1. [x] **Animasyon paketi import edildi:** Quaternius Universal Animation Library **Standard**.
   https://quaternius.itch.io/universal-animation-library
   Download Now > No thanks, just take me to the downloads > Standard.zip (15 MB).
   Standard ücretsiz, CC0. Pro ve Source ücretli. 120+ sayısı tüm paket tanıtımıdır;
   ücretsiz sürümdeki gerçek klipler import sonrası listelenecek.
   Hedef: locomotion, hasar tepkisi, düşme ve kutlama. Özel boks gardlı adım bulunduğu garanti değil.
   Eksik kalan boks klipleri için https://www.mixamo.com/ üzerinden seçim yapılacak.
   Adobe ID gerekir; ticari kullanım bilgisi https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html

   17 Kasım incelemesi: Assets/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx
   Unity MCP ile okunarak 43 klip (A_TPose dahil) doğrulandı. Walk_Loop, Walk_Formal_Loop,
   Jog_Fwd_Loop, Punch_Jab, Punch_Cross, Hit_Chest, Hit_Head, Death01 ve Dance_Loop mevcut.
   Özel boks gardlı yürüyüş klibi adı yok. Import Generic (animationType=2, avatarSetup=0),
   kliplerde humanMotion=False; Male Athlate'a doğrudan Humanoid retarget henüz hazır değil.
   Humanoid dönüştürme ve uyumluluk testi entegrasyon aşamasına bırakıldı.
   README: _RM son ekli dosya root motion içerir, normal dosyada root motion kapalıdır.
   Lisans dosyası CC0 olarak doğrulandı. Kaynak paket ve oyun dosyaları değiştirilmedi.

2. [x] **KayKit karakter ailesi toplandı:** Kullanıcı KayKit görünümünü beğendi ve Adventurers Free 2.0 paketini import etti. Oyun içi son görünüm/animasyon henüz doğrulanmadı.
   17 Kasım dosya incelemesi: Assets/KayKit_Adventurers_2.0_FREE/Characters/fbx altında
   Barbarian, Knight, Mage, Ranger, Rogue ve Rogue_Hooded var. 5 ana karakter + kapüşonlu Rogue varyantı.
   License.txt CC0. Barbarian_Large model dosyası yok; Samples altındaki görseli model sanılmamalı.
   Altı FBX şu an Generic (animationType=2, avatarSetup=0); otomatik Humanoid eşleştirmesi yapılmadı.
   Animations/Rig_Medium setleri mevcut: General (15 klip), MovementBasic (11 klip); iki T-Pose dahil.
   GLB animasyon adları okunarak Walking_A/B/C, Running_A/B, Idle_A/B, Hit_A/B, Death_A/B doğrulandı.
   Karakter GLB'leri skinned rig içeriyor; animasyonlar ayrı rig dosyalarında. Hazır controller bulunmadı.
   Yeni gereken tamamlayıcı paket: https://kaylousberg.itch.io/kaykit-character-animations
   Free 1.1 (14 MB), CC0. Üretici KayKit Rig_Medium için silahsız dövüş, blok, cheering ve sitting kategorilerini belirtiyor.
   Character Animations 1.1 import edildi; lisans dosyası CC0 olarak doğrulandı. Source ücretli; gerekmez.
   17 Kasım gerçek GLB klip incelemesi: Rig_Medium_CombatMelee içinde Melee_Unarmed_Idle,
   Melee_Unarmed_Attack_Punch_A, Melee_Unarmed_Attack_Kick, Melee_Blocking ve Melee_Block_Hit mevcut.
   Rig_Medium_Simulation içinde Cheering, Waving ve oturma klipleri mevcut.
   MovementBasic: Walking_A/B/C, Running_A/B; MovementAdvanced: Walking_Backwards ve 4 yönde Dodge.
   Mevcut paketler uygulamaya başlamak için yeterli; yeni zorunlu asset indirmesi önerilmiyor.
   Paketlerin bulunduğu doğrulandı, kaynak riglerin Unity'de oynatılması/retarget uyumu henüz test edilmedi.

   Aşağıdaki Warrior araştırması tarihsel alternatiftir; KayKit seçimi sonrası indirilmesi gerekmiyor:
   17 Kasım araştırma adayı: igelgala / Stylized warior low poly
   https://www.cgtrader.com/free-3d-models/character/man/stylized-warior-low-poly
   Üretici sayfası Free, Royalty Free License (no AI), kişisel/ticari kullanım olarak işaretliyor.
   Görsel önizleme incelendi: geniş omuzlar, belirgin kaslı gövde, büyük eller, komik yüz ve zırh aksesuarları.
   Sayfada 8313 polygon / 8652 vertex ve 2048 diffuse-normal dokular belirtilmiş; gerçek triangle maliyeti ölçülmedi.
   Açıklama rig/Mixamo olasılığı belirtiyor ancak dosya listesi BLEND/OBJ/MAX/PNG gösteriyor, FBX görünmüyor.
   Hazır Humanoid/Avatar, skin weights, animasyon kalitesi ve URP uyumu doğrulanmadı; indirilen dosyada incelenecek.
   Bu bir onaylı final model değil, görsel açıdan güçlü adaydır. Henüz projeye import/entegrasyon yapılmadı.
   Alternatif: KayKit Adventurers Free 2.0 (CC0, 5 rigli karakter); Dungeon paketiyle ortak stil.
   https://kaylousberg.itch.io/kaykit-adventurers
   Büyük Barbarian_Large EXTRA ücretli sürümdedir; ücretsiz diye önerilmeyecek.
   Mevcut Male Athlate geçici tabandır; sırf aksesuar eklemek özel kaslı cartoon model üretmez.
   Kriterler: kaslı cartoon anatomi, modellenmiş yüz/eller, geçerli Humanoid, URP materyali,
   kaplamalar ve GTX 1650 maliyeti. Bu kriterleri karşılayan ücretsiz aday doğrulanmadan yeni
   rastgele fighter paketi indirilmez. Final görünüm onayı bir açık karar olarak kalır.

3. [ ] **Modellenmiş seyirci çeşitleri:** önce mevcut dört CharCrafter preset incelenir.
   Yetmezse aday Quaternius Ultimate Modular Men:
   https://quaternius.com/packs/ultimatemodularcharacters.html
   11 karakter, 24 animasyon, CC0. Bu aday seyirci içindir; profesyonel kaslı dövüşçü olarak önerilmez.
   Stile uymayan paket sırf ücretsiz olduğu için kullanılmaz.

4. [x] **Gerçek tezahürat ve ortam sesi import edildi:**
   https://gregor-quendel.itch.io/free-crowd-cheering-sounds
   Ücretsiz indirme, CC BY 4.0; Gregor Quendel'e atıf gerektirir.
   17 Kasım incelemesi: Assets/Gregor Quendel - Free Crowd Cheering Sounds altında 11 WAV doğrulandı.
   Dosya adlarına göre adaylar: 10 - Ambience (arka plan), 08 - Rhythmic cheering (ritmik tezahürat),
   04 - Strong cheering - II - Short (giriş/şampiyon vurgusu).
   Kayıtlar henüz dinlenip kesilmedi ve oyuna bağlanmadı. Uzun Full recording dosyası yaklaşık 129 MB;
   entegrasyonda bütün kayıtlar aynı anda belleğe alınmayacak. Import klasöründe lisans metni bulunmadı;
   kaynak sayfasının CC BY 4.0 bilgisi ve Gregor Quendel atfı final atıf dosyasına kaydedilecek.

5. [x] **Darbe/kapı/ayak sesi kaynakları import edildi:**
   https://kenney.nl/assets/impact-sounds
   CC0, ücretsiz. Sesler dinlenerek kullanım alanına göre seçilir; hazır boks ses sistemi değildir.
   17 Kasım kontrolü: Assets/Audio klasöründe impactGeneric, impactMetal, impactWood,
   impactGlass ve diğer OGG dosyaları bulundu. Henüz ses seçimi, seviye ayarı veya oyun bağlantısı yapılmadı.

6. [x] **Efekt dokuları import edildi:**
   https://kenney.nl/assets/particle-pack
   CC0, ücretsiz. 2D particle dokuları; URP 3D duman/darbe/şok dalgası sistemine bağlanacak.
   17 Kasım kontrolü: Assets/PNG (Transparent) altında 80 ana PNG ve Rotated alt klasörüyle
   birlikte toplam 96 PNG doğrulandı. smoke, spark, flame, fire, circle, slash, star ve magic
   grupları mevcut. Assets/PNG (Black background) alternatifleri de geldi.
   Assets/Unity samples/particlePack_samples.unitypackage dosyası yalnızca kopyalanmış;
   içindeki eski örnek materyaller/prefablar ayrıca import edilmedi ve gerekli değiller.
   Kaynak dokuların URP materyal/particle bağlantısı henüz yapılmadı.

## Toplama durumu

Çevre, genel animasyon, kalabalık sesleri, darbe/ayak sesleri ve efekt dokuları artık projede.
Eksik kararlar: final kaslı cartoon dövüşçü ailesi, uygun seyirci modelleri ve gerçek gardlı boks adımı.
Bu üç başlık tamamlanmış kabul edilmez. Karakter uyumluluğu/görünümü doğrulanmadan aynı amaçla
yeni rastgele paketler import edilmez. Toplama aşamasında sahne veya çalışan kod değiştirilmedi.

## Asset olmayan kalan işler

- Tüm taş arena/kapı/tribün kompozisyonu ve ışıklandırma.
- Kadraj, dikey yayın HUD'u, okunabilir isimler, olay kartları ve şampiyon ekranı.
- Kesintisiz raundlar, katılım sırası, liderlik ve skor verileri.
- Efekt/ses zamanlaması; Animator ve fizik/temas uyumu.
- 10/20/50 kişi performansı, Windows build ve LIVE Studio testi.
- Gerçek TikTok bağlantısı ve erişim/koşulların doğrulanması.

İlerleme kuralı: Bir paketi import ettikten sonra içeriğini doğrula ve bu listeye kaydet.
Bu toplama aşamasında mevcut sahne veya dövüş sistemi değiştirilmez.

