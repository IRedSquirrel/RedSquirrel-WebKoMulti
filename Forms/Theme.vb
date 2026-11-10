Imports System.Drawing

''' <summary>
''' Uygulamanın renk paleti — TEK KAYNAK. Bütün view'lar ve kontroller renklerini buradan alır.
''' Namespace'siz Public Module olduğu için üyelerine niteleme gerekmeden (Primary, Night, ...) erişilir.
'''
''' Kimlik: "indigo aurora" — derin gece laciverti zemin + indigo→menekşe→camgöbeği vurgu, cam yüzeyler.
''' Yeni ortak renk gerekirse buraya ekleyin; view içinde yerel renk tanımlamayın.
''' </summary>
Public Module Theme

    ''' <summary>Marka aksanı (indigo) — #6366F1</summary>
    Public ReadOnly Primary As Color = Color.FromArgb(99, 102, 241)

    ''' <summary>Açık indigo — hover/çerçeve vurgusu — #818CF8</summary>
    Public ReadOnly PrimarySoft As Color = Color.FromArgb(129, 140, 248)

    ''' <summary>İkincil marka rengi (menekşe) — gradyanın ikinci durağı — #8B5CF6</summary>
    Public ReadOnly Secondary As Color = Color.FromArgb(139, 92, 246)

    ''' <summary>Vurgu (camgöbeği) — gradyanın son durağı — #22D3EE</summary>
    Public ReadOnly Accent As Color = Color.FromArgb(34, 211, 238)

    ''' <summary>Aurora metin gradyanı 1. durak — #A5B4FC</summary>
    Public ReadOnly Aurora1 As Color = Color.FromArgb(165, 180, 252)

    ''' <summary>Aurora metin gradyanı 2. durak — #C4B5FD</summary>
    Public ReadOnly Aurora2 As Color = Color.FromArgb(196, 181, 253)

    ''' <summary>Aurora metin gradyanı 3. durak — #67E8F9</summary>
    Public ReadOnly Aurora3 As Color = Color.FromArgb(103, 232, 249)

    ''' <summary>Ana zemin (gece laciverti) — #0A0C1C</summary>
    Public ReadOnly Night As Color = Color.FromArgb(10, 12, 28)

    ''' <summary>En derin zemin (login/loading, form dışı çerçeve) — #05060F</summary>
    Public ReadOnly NightDeep As Color = Color.FromArgb(5, 6, 15)

    ''' <summary>Panel/kontrol zemini — #10132A</summary>
    Public ReadOnly NightPanel As Color = Color.FromArgb(16, 19, 42)

    ''' <summary>Cam yüzey (glass) zemini — rgb(18,21,45)</summary>
    Public ReadOnly Glass As Color = Color.FromArgb(18, 21, 45)

    ''' <summary>İnce ayraç/çerçeve çizgisi — rgb(148,163,233,%14)</summary>
    Public ReadOnly Line As Color = Color.FromArgb(36, 148, 163, 233)

    ''' <summary>Açık metin — #E7E9F4</summary>
    Public ReadOnly TextMain As Color = Color.FromArgb(231, 233, 244)

    ''' <summary>Soluk/etiket metni — #9AA1C0</summary>
    Public ReadOnly TextMuted As Color = Color.FromArgb(154, 161, 192)

    ''' <summary>En soluk metin (mikro etiket/sürüm) — #5D6484</summary>
    Public ReadOnly TextFaint As Color = Color.FromArgb(93, 100, 132)

    ''' <summary>Pasifleştirilmiş kontrol zemini</summary>
    Public ReadOnly DisabledBg As Color = Color.FromArgb(46, 52, 92)

    ''' <summary>Olumlu/aktif durum yeşili</summary>
    Public ReadOnly OkGreen As Color = Color.FromArgb(74, 222, 128)

    ''' <summary>Hata/uyarı kırmızısı</summary>
    Public ReadOnly BadRed As Color = Color.FromArgb(248, 113, 113)

    ''' <summary>Bilgi/durum mavisi (salt-okunur göstergeler — toggle'dan ayrışsın)</summary>
    Public ReadOnly InfoBlue As Color = Color.FromArgb(96, 165, 250)

    ''' <summary>Uyarı kehribarı (log seviyesi "warn") — #FBBF24. BadRed hatadan,
    ''' bu ise "dikkat ama hata değil" durumundan sorumlu.</summary>
    Public ReadOnly WarnAmber As Color = Color.FromArgb(251, 191, 36)

    ''' <summary>Eksik/yapılacak turuncusu — #FB923C. Eksik bir şeyi gösteren işaret; aynı
    ''' bölümdeki "yakında" rozeti kehribar (WarnAmber) olduğu için ayrı ton — ikisi karışmasın.</summary>
    Public ReadOnly WarnOrange As Color = Color.FromArgb(251, 146, 60)

    ' Item class = the colour of the item's name in the game (TooltipReader measured: Unique tan (220,199,124), Rare yellow
    ' (255,255,0) — softened a little on the dark background). The slot editor's item picker draws names with them (Oct 20).
    Public ReadOnly ItemUnique As Color = Color.FromArgb(220, 199, 124)
    Public ReadOnly ItemRare As Color = Color.FromArgb(255, 240, 90)
    ''' <summary>A Reverse item's name — the pink class colour the market list shows (MarketReader (255,131,166), not yet
    ''' confirmed in the game as Reverse; Oct 20).</summary>
    Public ReadOnly ItemReverse As Color = Color.FromArgb(255, 131, 166)

    ' Walk path chips (WaypointStrip, user's choice Oct 18: passed = dark red / dimmed, current = green / lit,
    ' pending = an easy-on-the-eye orange). Each state: chip background + text colour.
    Public ReadOnly WaypointPassedBack As Color = Color.FromArgb(52, 18, 22)
    Public ReadOnly WaypointPassedText As Color = Color.FromArgb(150, 62, 62)
    Public ReadOnly WaypointCurrentBack As Color = Color.FromArgb(22, 92, 54)
    Public ReadOnly WaypointCurrentText As Color = Color.FromArgb(220, 252, 231)
    Public ReadOnly WaypointPendingBack As Color = Color.FromArgb(64, 44, 22)
    Public ReadOnly WaypointPendingText As Color = Color.FromArgb(232, 178, 112)

    ''' <summary>
    ''' Fade-in başlangıç saydamlığı — 0 DEĞİL (ölçüldü): Opacity = 0 ile
    ''' gösterilen pencereye Explorer görev çubuğu düğmesi vermiyor.
    ''' </summary>
    Public Const FadeBaslangicOpacity As Double = 0.01

End Module
