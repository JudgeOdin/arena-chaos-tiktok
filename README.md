# ARENA CHAOS

Unity ile geliştirilmiş arena savaş oyunu.

## Gereksinimler

- Unity `6000.6.7f1`
- Windows veya Unity Editor destekleyen bir işletim sistemi

## Açma

1. Bu depoyu klonlayın.
2. Unity Hub'da `ARENA CHAOS TİKTOK` klasörünü proje olarak ekleyin.
3. Projeyi Unity `6000.6.7f1` ile açın.

Büyük model, ses ve video dosyaları Git LFS ile saklanır. Klonlamadan önce
Git LFS kurun; mevcut klonda `git lfs install` ve `git lfs pull` çalıştırın.

Güncel oyun ve yayın kurulum notları:
[LOCAL_RELEASE_NOTES.md](<ARENA CHAOS TİKTOK/LOCAL_RELEASE_NOTES.md>).
İnceleme sonuçları:
[AUDIT_RESOLUTION.md](<ARENA CHAOS TİKTOK/AUDIT_RESOLUTION.md>).
Hazırlanan tanıtım videoları `ARENA CHAOS TİKTOK/Videos/TikTok_Ilk_Seri` içindedir.

Etkin oyun, depoda bulunan KayKit içerik kataloğunu kullanır. Eski Normalgon ve
EEJANAI deneme paketleri, yerel Roslyn araç DLL'leri, kişisel araç ayarları,
ham video ara dosyaları ve yerel yedekler bu herkese açık depoya dahil edilmez.
Eski paketlere dayanan alternatif denemeleri açmak için ilgili orijinal paketler
ayrıca import edilmelidir. Yerel dosyalar silinmez.

`Library`, `Temp`, `Logs` ve `UserSettings` klasörleri GitHub'a gönderilmez; Unity bunları projeyi açarken yeniden oluşturur.

## Lisans

Bu proje henüz bir açık kaynak lisansı seçilmeden paylaşılmıştır.
