# RedSquirrel - WebKo Multi — oyuna kendi kendine giriş

Kod: `Engine/Login/` (WebLogin · LauncherPage · LoginScreens · LoginKeys · LoginShots · LoginSelfTest · LoginProbe),
hesap deposu `Services/Accounts/AccountStore.vb`. Ölçüm kareleri `Docs/olcum/login/` — bu kareler başka oyuncuları gösterdiği için
**depoda yoktur**; sınamalar klasör yoksa o bölümleri atlar ("atlandı: test kareleri yok").

## Akış (9 Kas 2026, ardream.net açılıp okundu — kimlik girilmedi, form gönderilmedi)
1. **Ana sayfa** `https://ardream.net/` (ilk ziyarette bir çerez kapısı sayfası kendini bir kez yeniler).
   Alt panel: `#durum` (durum yazısı), `#bar` (`--p` = 0..1 ilerleme), **`#start`** (START; `hazir` sınıfı + etkin = basılabilir).
   Sitenin **GİRİŞ YAP** penceresi (`kad` / `sif`, `/api/hesap/giris`) yalnız hesap yönetimi (şifre değiştir, kilit şifresi) —
   oyuna girmek için **gerekmez**, kullanılmıyor.
2. **START** → oyun dosyaları iner (ilk açılış ~135 MB, sonra önbellekten) → sayfa oyunu kendi çerçevesinde (`#oyunCerceve` → `/play`)
   öne alır (`body.oyunda`). Ölçüldü: 1280×720'de ilk START'tan oyuna 7 sn (önbellekten 3 sn).
   Tarayıcı çerçeveyi "cross-origin isolated" çalıştıramazsa site "doğrudan" kipe geçer ve START `/play`'e gider —
   **desteklenmiyor** (WebView2'de ölçülen: isolated = true); zincir sebebiyle durur.
3. **Oyunun giriş kutusu (tuvalde):** "Login" başlığı · **ID** · **Password** (iki siyah kutu) · Remember Me · kırmızı **O K** ·
   Cancel · mavi Register. Kutuya tık → yazı tuşlarla (DevTools girdisi) gider, oyun yazıyı kutuda çiziyor (ölçüldü, sahte ID ile).
   ⚠ **OTO ÜYELİK:** kutunun yanında "Oto üyelik sistemi aktif. Girdiğiniz bilgiler ile otomatik üyelik açılmaktadır" yazıyor —
   **yanlış yazılan ID hata vermez, YENİ HESAP açar.** Bu yüzden zincir ID'yi sitenin kuralına (`[A-Za-z0-9_]` 3-20) uymuyorsa hiç
   yazmaz; kullanıcı ID'yi doğru girmekle sorumlu.
4. **Sunucu listesi** ("Select Server", tek satır ARDREAM.NET NEW seçili, altta turuncu **Connect**) — kullanıcının ilk canlı
   denemesinde (9 Kas 23:44, 3 hesap, 1280×800) Enter ekranı 0,6-2,5 sn'de değiştirdi.
5. **Karakter seçimi:** altta ortada ◄ ► (altın = basılabilir, gri = o yönde karakter yok; 1. karakterde ► gri), sağda panel
   (ad / LV / Job siyah kutuları, Create Character / Back / Finish, altında **Start Game**). Masaüstü KO'nun düzeni.
   İlk denemede bu ekran "giriş ekranına geri dönüldü" sanıldı: LV / Job kutuları + kırmızı Create Character, masaüstü KO'nun
   ID / şifre / OK oranına (0,84) uyuyordu. Düzeltildi (aşağıda).
6. **Ulus seçimi** (Select Elmorad / Karus) — 3. görünümün ilk denemesinde, dosyalar inerken bir kez görüldü (karakteri olmayan
   hesabın ekranı). Zincir bu ekranda tuşa BASMAZ (kör Enter ulus seçtirebilir); bekler, kare kaydeder.

## Zincir (WebLogin.RunAsync) — her adımın zaman aşımı ve çıkışı var
| Adım | Ne yapar | Bitti sayılır | Çıkış |
|---|---|---|---|
| Doğrulama | ID / şifre boş mu, ID kuralı | — | `Failed` "ID geçersiz…" |
| Site | ana sayfaya git, salt-okuma DOM sorusu (`LauncherPage.ReadAsync`, yalnız ana sayfada, oyun açılmadan) → START'a DevTools tıkı | `body.oyunda` | 10 dk; indirme 120 sn ilerlemezse; 3 START'ta açılmazsa; `/play`'e düşerse |
| Giriş kutusu | 0,7 sn'de bir kare → `LoginScreens.Classify` | Login (ya da zaten oyunda: HUD) | 90 sn → kare kaydı + `Failed` |
| ID / şifre | kutuya tık; kutuda eski yazı varsa (yalnız o kutu, en çok 24) sil; yaz; **yazı kutuda görülmeden geçmez** (2 sn, 1 tekrar); şifreden sonra ID kutusu hâlâ dolu mu | iki kutu da dolu | "… kutusuna yazı geçmedi" |
| Gönder | Enter; 3 sn'de kutu gitmezse bir kez **O K** | giriş kutusu kayboldu | 30 sn → "Giriş kabul edilmedi" |
| Oyuna | ekran durulunca sınıflandır. **Sunucu** → Enter, ekran değişmezse Connect'e tık. **Karakter** → ► gri olana dek ► (1. karakter), sonra ◄ (slot-1) kez (her biri altın olmalı); her tıkta sahnenin değiştiği ve okların geri geldiği ölçülür (3 deneme); sonra Enter, ekran gitmezse Start Game. **Tanınmayan** ekranda tuş YOK, yalnız bekleme + kare (dakikada bir, en çok 5) | HP/MP çerçevesi görüldü | 10 dk; sunucu 3 / karakter 2 deneme; N. karakter yoksa "bu hesapta K karakter var"; gerçek ID/şifre düzeni geri gelirse "geri dönüldü" |

Script kuralı: `/play` sayfasında ve oyun çerçevesinin içinde **hiç** script yok (oyun WebAssembly / WebSocket / toString'i yakalıyor).
Ana sayfadaki tek script salt-okumadır ve oyun gösterildikten sonra çalışmaz. Tuvaldeki her şey `ViewCapture` + DevTools girdisi.

Defter: kategori `giris` / `hesap`; ID yalnız uzunluğuyla, şifre hiçbir yerde. Kareler `<exe>\..\Logs\multi-login\` (7 gün):
tanınmayan her ekranda ve hata anında; **ID kutuda görünürken kare yazılmaz**.

## Ölçüldü / ölçülmedi
| Konu | Durum |
|---|---|
| Ana sayfa alanları, START, indirme, oyunun gösterilmesi | **ölçüldü** — gerçek sitede üretim kodu (`LoginProbe.Run`, 3 görünüm boyutu) |
| Giriş kutusu bulucu (iki siyah kutu + kırmızı OK; OK = şifre ortası + 1,39 × kutu aralığı) | **ölçüldü** — gerçek kareler 1280×720, 960×540, 800×600 (+ 0,75 / 1,5 ölçekli kopyalar). Tuval görünüme göre ölçekleniyor |
| ID / şifre yazma (Shift'li `_` dahil), yazının kutuda görülmesi, silme | **ölçüldü** — üretim zinciri sahte ID `probe_x9` ile **Enter'dan önce durdurularak** (`LoginProbe.RunChain`, `StopBeforeSubmit`); kare `1280x720-giris-yazili-sahte.png` |
| Enter ile giriş, sunucu ekranında Enter | **ölçüldü** — kullanıcının canlı denemesi (3 hesap, 1280×800) |
| Sunucu ekranı tanıma (alt ortada turuncu Connect) | **ölçüldü** — gerçek kare + 0,75 / 1,5 ölçek; Connect'e tık yalnız yedek, canlı **denenmedi** |
| Karakter ekranı tanıma (◄ ► + Start Game = Job kutusu + 4,45 × LV→Job aralığı) | **ölçüldü** — 3 gerçek kare (1280×800) + ölçekler; ► altına boyanmış kopya |
| Karakter okları tıkı, Enter / Start Game ile oyuna giriş, yükleme süresi | **ölçülmedi** — ilk canlı denemede zincir buraya gelmeden düştü |
| Yanlış şifrede oyunun yanıtı | **ölçülmedi** |
| 16:10 dışı görünümde sunucu / karakter ekranı | **ölçülmedi** (giriş kutusu 16:9 ve 4:3'te ölçüldü) |
| Oyunda (HUD) algısı | renk ardream'de tek hesaplı görünümde ölçüldü (`HudReader`: çerçeve 80,96,73); burada ölçekli görünümde **ölçülmedi** (sentetik sınama var) |
| Çok küçük görünüm (< ~640 genişlik) | **ölçülmedi** — kutu < 50 px olunca yazı sütun sayımı zorlaşır |


## Oyuna girişten sonra (AfterEnter.vb, 10 Kas — kullanıcının 2. canlı denemesinin karesinden)
Kullanıcı: *"ekranda bunlar varsa 2-3 kere ESC'ye basmalı; sesleri mutelemeli açıksa; PM'leri X'tan kapatmalı; her biri yapmalı"*.
HUD görülünce 2,5 sn beklenir, sonra her hesap:
1. **Pencereler:** "Temporary Item List" (ortada) ve "Notice" (sağda) başlıklarından tanınır (gri şablon, `AfterEnterTemplates.vb` —
   gerçek kareden üretildi). ESC **yalnız biri görülürken**, en çok 3; her ESC'den sonra yeni kareyle bakılır. Temiz ekranda ESC yok
   (oyun menüsünü açar). Başka bir pencere türü tanınmaz → ESC de basılmaz.
2. **Ses:** sitenin yuvarlak hoparlör düğmesi tuvalin ~%31,6 genişliğinde, üstten 21 px (HTML, ölçeklenmez). Sarı = açık → tık →
   gri (✕) görülmeli. Kapalıysa dokunulmaz.
3. **PM'ler:** soldaki fısıltı satırlarının sarı X'i (şablon) — en üstteki tıklanır, satırın kaybolduğu bakılır; en çok 8 tık.
4. Defter tek satır: "oyun içi temizlik — 2 pencere ESC ile kapandı (2 ESC), ses kapatıldı, 4 PM kapatıldı". Temizlik girişi
   ASLA düşürmez; sonunda durum "Oyunda".

**Tuval ortalanabilir:** tam ekran lider görünümünde (1920×977) oyun tuvali 1280×800 olarak siyah kenarlar içinde ortada
(sol üst 320,88). `LoginScreens.CanvasRect` siyah kenarları (simetrikse) bulur; bütün okuyucular (giriş, sunucu, karakter, HUD,
pencereler, PM, ses) tuvalin içinde ölçer ve noktaları görünüm pikseline geri çevirir.

**2. denemede bulunan hata (düzeltildi):** 1. görünümde karakter ekranından sonra oyun dünyası yüklenirken alt ortadaki turuncu
yazılar (sohbetteki "Raptor(+5)", Temporary Item List altını) "Connect" sanıldı → oyunun içinde Enter + bir tık gitti, durum 16 sn
"Sunucuya bağlanıyor"da kaldı. Artık (a) Connect tek satırlık yazı olmalı (yükseklik ≤ %4,5), (b) karakter adımından sonra sunucu
ekranına hiç tuş basılmaz.

## Bir kezlik oyun ayarı (GameSetup.vb, 10 Kas)
Kullanıcı: *"sol alttan çocuğa tıklayıp Grafik'e tıklayın, Low (Patates) Graphics tıklamalı. Display menüsünden ise Play Intro
movies, VRR, Browser power saving kapanmalı"*. AfterEnter'dan sonra, hesap başına bir kez:
alt çubukta sarı saçlı yüz (tuval ≈126,777; şablon gerçek oyun karesinden) → "Option" penceresi başlıktan bulunur → Graphics
sekmesi (sayfa "Low (Patates) Graphics" düğmesi görülünce açık sayılır) → Low (Patates) → Display sekmesi (üç kutunun yazısı
görülünce) → işaretli olan kutular tıklanır, tikin kalktığı görülür (camgöbeği tik pikseli) → Apply → pencerenin kendi **X**'i
(ESC değil). Her tık yeni kareyle doğrulanır; tutmayan adımda ayar yarıda bırakılır, pencere açıksa X ile kapatılır, defter
sebebi yazar, giriş bozulmaz. Başarıda `..\Settings\multi-gamesetup.json`'a hesap ID'sinin SHA-256'sı yazılır (ID değil);
sonraki girişlerde atlanır. `GameSetup.Forget(userId)` = "Oyun ayarlarını yeniden uygula" (arayüz düğmesi için).
Ölçü: kullanıcının kırpıntıları tuvalle 1:1 (simge NCC 1,00); pencere düğmeleri "Option" başlığının köşesinden sabit uzaklıkta.
**Ölçülmedi:** pencerenin oyundaki gerçek yeri (aranıyor), Apply'ın bir soru açıp açmadığı, Low (Patates)'in neyi değiştirdiği
(tıklanır ama sonucu ayrıca doğrulanmaz), Display sayfasının Graphics sayfasıyla aynı x'te olması (iki kırpıntıda sekmeler aynı x'te).

**3. canlı deneme (10 Kas 00:55) — iki hata düzeltildi:**
- *Temporary Item List ESC ile kapanmadı:* ilk ESC Notice'i kapattı, sonraki iki ESC (1,1 sn arayla) Temporary'ye işlemedi. Artık
  ESC'ler arası 1,5 sn; bir ESC hiçbir pencereyi kapatmazsa (ya da 3 ESC dolunca) pencerenin KENDİ X'i tıklanır — yeri başlık
  şablonunun köşesinden: Temporary +389,+2 (iki ayrı karede birebir), Notice +212,+10. "Do not show today" işaretlenmez.
- *Option açıldı ama "açılmadı" denildi:* pencere Game sekmesindeydi; başlık şablonu orada da 0,999 tutuyor ama kaba arama ince
  yazılı başlığı ızgaranın 1 px dışında kaçırıp başka yerleri aday seçiyordu. Kaba aşama artık BLOK ORTALAMALARIYLA (1 px kaymaya
  duyarsız) karşılaştırıyor, en iyi adaylar tam çözünürlükte ölçülüyor (`AfterEnter.Locate`, bütün şablon aramaları ortak).
  Ayrıca bekleme en az iki bakış + 6 sn; pencere aranırken yavaş kutu araması yapılmıyor; yarıda kalınca açılan pencere X ile
  kapatılıyor ve deftere "yarıda kalan Option penceresi X ile kapatıldı / KAPANMADI" yazılıyor.
- *Elle sınama:* `WebLogin.RunInGameStepsAsync(session, ct, forceSetup)` — oyundaki bir görünümde temizlik + oyun ayarını yeniden
  girişsiz çalıştırır (forceSetup = bir kezlik işareti silip ayarı yeniden uygula). Arayüzde Oyunlar penceresindeki adım düğmesi.

**v2 (10 Kas): kare sınırı 30, kamera 16** (kullanıcı; amaç işlemci yükü — her görünüm sınırsız ~142 FPS çiziyordu). Display
sekmesinde, üç kutudan sonra: değer OKUNUR (pencerenin kendi yazı tipi, `GameSetupDigits.vb`), hedefe doğru ◄ / ► ile birer
basılır, her basıştan sonra yeniden okunur; en çok 24 basış; okunamazsa (ör. "Off" gibi yazı, ölçülmemiş rakam) DOKUNULMAZ;
iki basışta değişmezse ya da hedefin üstünden atlarsa (değer listesinde 30 / 16 yok) bırakılır. Apply'dan sonra "N ayar
uygulandı." yazısı görülürse başarı. İşaret artık sürümlü (`v2:<SHA-256>`): eski v1 işareti yeni ayarları atlatmaz; kısmi ya da
doğrulanmamış uygulamada işaret yazılmaz (sonraki girişte yeniden). Defter: "oyun ayarı uygulandı (…, kare sınırı 30, kamera 16)".
Ölçülen rakamlar: **0 1 2 3 6 7 8** (option-display-fps30-kamera16.png); **4 5 9 ölçülmedi** → "?" okunur, değer bırakılır.
Küçültülmüş görünümde (×0,75) rakamlar bulanıklaşır → okunamaz (yanlış sayı okunmaz; sınamada ölçüldü). Kare sınırının
başlangıç değeri (sınırsızken ne yazdığı) ve değer listesi ölçülmedi.
Düzeltme (aynı karede ölçüldü): Display sayfası yazılarının sütunu başlık köşesinin 69 px solu — önce 82 varsayılmıştı (eski
kırpıntının x'i grafik kırpıntısından 13 px farklıymış); bu hâliyle canlıda kutular bulunamayacaktı.

## Sınama
- Ağsız: `LoginSelfTest.Run(framesDir)` — hesap deposu gidip-gelme (DPAPI, atomik, bozuk dosya), site karar tablosu, tuş planı, ID kuralı,
  gerçek karelerde giriş kutusu yeri + yazı ölçümü, sunucu / karakter / ulus kareleri, oyun içi kareler (pencere, PM, ses, ortalanmış tuval), Option penceresi (kırpıntılar gerçek karenin üstüne, 1,0 ve 0,75), bir kezlik işaret, 3. denemenin iki karesi (Temporary X, Game sekmesinde Option), olumsuzlar. 10 Kas: **177/177** (kare sınırı / kamera okuması, '60' sentetik, 'Off' okunmaz, v1 işaret atlatmaz dahil).
  Mutasyon (ölçüldü): karakter tanıma kapalı + masaüstü 0,84 oranı geri → karakter kareleri "Login ID 1155,299" okunuyor (82/91, ilk canlı
  denemedeki hatanın aynısı); kutu boy toleransı eski ±%25'e çekilince 1280×720 giriş kareleri düşüyor.
- Komut (Debug derlemesi): `RedSquirrel.WebKoMulti.exe login-selftest [kare-klasörü]` → `Engine.Login.LoginSelfTest.Run()`.
- Canlı ölçüm (hesapsız): `LoginProbe.Run(profil, çıkış, sn, gen, yük)` kare kaydeder; `LoginProbe.RunChain(profil, sn)` üretim zincirini
  sahte hesapla Enter'dan önce durdurur. İkisi de ekran dışı pencere açar.

## Üçüncü canlı denemede bakılacaklar (kullanıcı, kendi hesabıyla)
1. Her görünümde defterde: "oyuna girişte ekran — pencere: … · PM: N · ses: …" ve ardından "oyun içi temizlik — …" satırı.
   Sayılar ekrandakiyle tutuyor mu (ESC kaç pencere kapattı, kaç PM kapandı, ses kapandı mı).
2. "ESC pencereyi kapatmadı" ya da "PM X tıkı satırı kapatmadı" uyarısı çıkarsa o anın karesi lazım (ekran görüntüsü).
3. ESC sonrası oyun menüsü açık kalmış mı (olmamalı — ESC yalnız pencere görülürken gider).
4. Durum çubuğu oyuna girince "Oyunda"ya dönüyor mu; karakter ekranından sonra artık "Sunucuya bağlanıyor"da takılmamalı.
5. Tam ekran lider de aynı sonucu vermeli (tuval ortada).
6. İlk girişte defterde "oyun ayarı uygulandı (patates grafik, intro / VRR / güç tasarrufu kapalı)" ya da "oyun ayarı yarıda
   bırakıldı — <sebep>". Oyunda Option'ı elle açıp Low (Patates) ve üç kutunun gerçekten kapalı olduğuna bakın. İkinci girişte
   "daha önce uygulanmış — atlandı" çıkmalı.
7. v2: defterde "kare sınırı 30, kamera 16" ya da "kare sınırı okunamadı ('…')" — okunamadıysa o anki Option › Display karesi lazım
   (kare sınırının başlangıç yazısı / ölçülmemiş rakamlar). Daha önce v1 ile işaretlenen hesaplarda ayar bir kez daha çalışmalı.
