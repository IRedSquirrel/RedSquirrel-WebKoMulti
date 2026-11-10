Imports System.IO
Imports System.Text
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Services.Accounts

Namespace Engine.Login

    ''' <summary>
    ''' Offline self-test of the login side (no network, no view, no game): account store round trip (temp file, DPAPI),
    ''' launcher decision table, key plan, screen classifier on real ardream login frames (Docs\olcum\login, optional) + scaled
    ''' copies + negatives, text-in-box measure. Returns a report; last line "SONUC: GECTI n/m" or "SONUC: DUSTU n/m".
    ''' Run(framesDir): folder with the real frames (Nothing = &lt;exe&gt;\..\..\Docs\olcum\login, i.e. from bin\Debug).
    ''' The real frames are NOT shipped with the source (they show other players); when the folder is missing those parts
    ''' are skipped ("atlandı: test kareleri yok") and everything synthetic / offline still runs.
    ''' Wire-up (Program.vb): `RedSquirrel.WebKoMulti.exe login-selftest` → Console.
    ''' </summary>
    Public Module LoginSelfTest

        Private _sb As StringBuilder
        Private _pass, _total As Integer

        Public Function Run(Optional framesDir As String = Nothing) As String
            _sb = New StringBuilder() : _pass = 0 : _total = 0
            Try
                AccountTests()
            Catch ex As Exception
                Check("hesap: istisna yok", False, ex.Message)
            End Try
            LauncherTests()
            KeyTests()
            Dim dir = If(framesDir, Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "..", "Docs", "olcum", "login")))
            Try
                ScreenTests(dir)
            Catch ex As Exception
                Check("ekran: istisna yok", False, ex.ToString())
            End Try
            Try
                WorldTests(dir, Path.Combine(Path.GetDirectoryName(dir.TrimEnd("\"c)), "kullanici-oyunda"))
            Catch ex As Exception
                Check("oyun içi: istisna yok", False, ex.ToString())
            End Try
            _sb.AppendLine($"SONUC: {If(_pass = _total, "GECTI", "DUSTU")} {_pass}/{_total}")
            Return _sb.ToString()
        End Function

        Private Sub Skip(what As String)
            _sb.AppendLine($"SKIP atlandı: test kareleri yok — {what}")
        End Sub

        Private Sub Check(name As String, ok As Boolean, Optional detail As String = "")
            _total += 1
            If ok Then _pass += 1
            _sb.AppendLine($"{If(ok, "OK  ", "FAIL")} {name}{If(String.IsNullOrEmpty(detail), "", " — " & detail)}")
        End Sub

        ' ---------------- accounts ----------------

        Private Sub AccountTests()
            Dim tmp = Path.Combine(Path.GetTempPath(), "wwkm-acc-" & Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory(tmp)
            Dim file = Path.Combine(tmp, "multi-accounts.dat")
            Dim old = AccountStore.PathOverride
            AccountStore.PathOverride = file
            Try
                Check("hesap: dosya yokken boş liste", AccountStore.Load().Count = 0)
                Dim list As New List(Of GameAccount) From {
                    New GameAccount With {.Label = "Ana", .UserId = "oyuncu_01", .Password = "Abc123xyZ", .CharacterSlot = 3, .Enabled = True, .UseVpn = True},
                    New GameAccount With {.Label = "Şövalye ""çift""", .UserId = "ikinci", .Password = "p\ass""9", .CharacterSlot = 1, .Enabled = False}
                }
                Check("hesap: kaydet", AccountStore.Save(list))
                Dim raw = IO.File.ReadAllBytes(file)
                Dim txt = Encoding.UTF8.GetString(raw)
                Check("hesap: dosyada düz şifre / ID yok", Not txt.Contains("Abc123xyZ") AndAlso Not txt.Contains("oyuncu_01"))
                Check("hesap: WMA1 başlığı", raw.Length > 4 AndAlso Encoding.ASCII.GetString(raw, 0, 4) = "WMA1")
                Dim back = AccountStore.Load()
                Check("hesap: gidip-gelme sayısı", back.Count = 2, back.Count.ToString())
                If back.Count = 2 Then
                    Check("hesap: alanlar 1", back(0).Label = "Ana" AndAlso back(0).UserId = "oyuncu_01" AndAlso back(0).Password = "Abc123xyZ" AndAlso back(0).CharacterSlot = 3 AndAlso back(0).Enabled AndAlso back(0).UseVpn)
                    Check("hesap: alanlar 2 (tırnak, ters bölü, Türkçe)", back(1).Label = "Şövalye ""çift""" AndAlso back(1).Password = "p\ass""9" AndAlso Not back(1).Enabled AndAlso Not back(1).UseVpn)
                End If
                Check("hesap: üzerine yazma (Replace)", AccountStore.Save(New List(Of GameAccount) From {list(0)}) AndAlso AccountStore.Load().Count = 1)
                Check("hesap: .tmp kalmadı", Not IO.File.Exists(file & ".tmp"))
                Dim parsed = AccountStore.FromJson("{""Accounts"":[{""USERID"":""x"",""characterSlot"":9}]}")
                Check("hesap: harf duyarsız + slot sınırı + useVpn yoksa False (eski dosya)", parsed.Count = 1 AndAlso parsed(0).UserId = "x" AndAlso parsed(0).CharacterSlot = 4 AndAlso parsed(0).Enabled AndAlso Not parsed(0).UseVpn)
                IO.File.WriteAllBytes(file, Encoding.ASCII.GetBytes("WMA1bozukveri"))
                Check("hesap: bozuk dosya → boş liste", AccountStore.Load().Count = 0)
                Check("hesap: bozuk dosya kenara alındı", Directory.GetFiles(tmp, "multi-accounts.dat.bozuk-*").Length = 1 AndAlso Not IO.File.Exists(file))
            Finally
                AccountStore.PathOverride = old
                Try
                    Directory.Delete(tmp, True)
                Catch
                End Try
            End Try
        End Sub

        ' ---------------- launcher ----------------

        Private Function St(url As String, Optional start As Boolean = True, Optional enabled As Boolean = False, Optional status As String = "",
                            Optional shown As Boolean = False) As LauncherPage.LauncherState
            Return New LauncherPage.LauncherState With {.Url = url, .HasStart = start, .StartEnabled = enabled, .StatusText = status, .GameShown = shown,
                                                       .StartPoint = New Point(650, 443), .ViewSize = New Size(1280, 720)}
        End Function

        Private Sub LauncherTests()
            Dim home = "https://ardream.net/"
            Dim D = Function(s As LauncherPage.LauncherState, pr As Boolean) LauncherPage.Decide(s, pr)
            Check("site: çerez kapısı (START yok) → bekle", D(St(home, start:=False), False) = LauncherPage.LauncherStep.Wait)
            Check("site: hazırlanıyor (START kapalı) → bekle", D(St(home, enabled:=False, status:="Oyun dosyaları hazırlanıyor…"), False) = LauncherPage.LauncherStep.Wait)
            Check("site: START açık → bas", D(St(home, enabled:=True, status:="Oyun dosyaları START'a basınca iner."), False) = LauncherPage.LauncherStep.PressStart)
            Check("site: basıldı, kapalı → iniyor", D(St(home, enabled:=False, status:="Dosya listesi alınıyor…"), True) = LauncherPage.LauncherStep.Downloading)
            Check("site: hata → yeniden START", D(St(home, enabled:=True, status:="Bir sorun oluştu — START ile yeniden deneyin"), True) = LauncherPage.LauncherStep.RetryStart)
            Check("site: oyunda → bitti", D(St(home, enabled:=True, shown:=True), True) = LauncherPage.LauncherStep.GameShown)
            Check("site: /play → doğrudan (desteklenmez)", D(St("https://ardream.net/play"), False) = LauncherPage.LauncherStep.DirectPlay)
            Check("site: başka alan adı → yanlış sayfa", D(St("https://example.com/"), False) = LauncherPage.LauncherStep.WrongPage)
            ' parse: real probe output shape (string literal holding JSON)
            Dim raw = """{\""url\"":\""https://ardream.net/\"",\""start\"":true,\""dis\"":false,\""hazir\"":true,\""x\"":650.5,\""y\"":443,\""w\"":68,\""durum\"":\""Oyun hazır\"",\""p\"":1,\""frame\"":true,\""oyunda\"":false,\""iw\"":1280,\""ih\"":720}"""
            Dim p = LauncherPage.Parse(raw, "")
            Check("site: yanıt ayrıştırma", p.HasStart AndAlso p.StartEnabled AndAlso p.StartPoint = New Point(650, 443) AndAlso p.Progress = 1 AndAlso p.ViewSize.Width = 1280 AndAlso p.StatusText = "Oyun hazır",
                  $"{p.StartPoint} {p.Progress} {p.ViewSize}")
            Dim p2 = LauncherPage.Parse("""{\""start\"":true,\""dis\"":false,\""hazir\"":false}""", home)
            Check("site: 'hazir' sınıfı yoksa basılamaz", p2.HasStart AndAlso Not p2.StartEnabled)
            Check("site: boş yanıt", Not LauncherPage.Parse("null", home).HasStart)
        End Sub

        ' ---------------- keys ----------------

        Private Sub KeyTests()
            Dim a = LoginKeys.Plan("a"c), z = LoginKeys.Plan("Z"c), d = LoginKeys.Plan("7"c), u = LoginKeys.Plan("_"c)
            Check("tuş: a", a.Code = "KeyA" AndAlso a.Vk = 65 AndAlso Not a.Shift)
            Check("tuş: Z Shift'li", z.Code = "KeyZ" AndAlso z.Vk = 90 AndAlso z.Shift)
            Check("tuş: 7", d.Code = "Digit7" AndAlso d.Vk = 55 AndAlso Not d.Shift)
            Check("tuş: _ = Shift+Minus (VK 95 uyku tuşu değil)", u.Code = "Minus" AndAlso u.Vk = &HBD AndAlso u.Shift)
            Check("ID kuralı: geçerliler", WebLogin.IdLooksValid("oyuncu_01") AndAlso WebLogin.IdLooksValid("Abc"))
            Check("ID kuralı: boşluk / Türkçe / kısa / uzun reddedilir (oto üyelik)",
                  Not WebLogin.IdLooksValid("oyuncu 01") AndAlso Not WebLogin.IdLooksValid("şövalye") AndAlso Not WebLogin.IdLooksValid("ab") AndAlso
                  Not WebLogin.IdLooksValid(New String("a"c, 21)) AndAlso Not WebLogin.IdLooksValid(Nothing))
        End Sub

        ' ---------------- in game (live-run frames) ----------------

        Private Sub WorldTests(loginDir As String, dir As String)
            If Not Directory.Exists(dir) Then
                Skip("oyun içi kareler / Option kırpıntıları (" & dir & ")")
                Return
            End If
            Dim f1 = Path.Combine(dir, "1280x800-oyunda-popuplar.png")
            Dim f2 = Path.Combine(dir, "tamekran-gorunum-oyunda.png")
            Check("oyun içi: gerçek kareler var", File.Exists(f1) AndAlso File.Exists(f2), dir)
            If Not File.Exists(f1) OrElse Not File.Exists(f2) Then Return
            ' measured on the 1280×800 frame: PM X centres x≈139, y≈420/452/484/516; speaker (400,21)
            For Each c In {(f1, Point.Empty), (f2, New Point(320, 88))}
                Using bmp As New Bitmap(c.Item1)
                    Dim name = Path.GetFileName(c.Item1), o = c.Item2
                    Dim r = LoginScreens.Classify(bmp)
                    Check($"oyun içi: {name} → oyunda (sunucu DEĞİL)", r.Kind = ScreenKind.InGame, $"{r.Kind} {r.Detail}")
                    Check($"oyun içi: {name} tuval yeri", Math.Abs(r.Canvas.X - o.X) <= 3 AndAlso Math.Abs(r.Canvas.Y - o.Y) <= 3 AndAlso
                          Math.Abs(r.Canvas.Width - 1280) <= 4 AndAlso Math.Abs(r.Canvas.Height - 800) <= 4, r.Canvas.ToString())
                    Dim ui = AfterEnter.Read(bmp)
                    Check($"oyun içi: {name} pencereler (Notice + Temporary Item List)", ui.Popups.Contains("Notice") AndAlso ui.Popups.Contains("Temporary Item List"), ui.ToString())
                    Check($"oyun içi: {name} 4 PM", ui.PmCloses.Count = 4, ui.ToString())
                    If ui.PmCloses.Count = 4 Then
                        Dim ys = {420, 452, 484, 516}
                        Dim ok = Enumerable.Range(0, 4).All(Function(i) Math.Abs(ui.PmCloses(i).X - (139 + o.X)) <= 4 AndAlso Math.Abs(ui.PmCloses(i).Y - (ys(i) + o.Y)) <= 4)
                        Check($"oyun içi: {name} PM X yerleri", ok, String.Join(" ", ui.PmCloses))
                    End If
                    Check($"oyun içi: {name} ses açık + yerinde", ui.SoundOn AndAlso Math.Abs(ui.Speaker.X - (400 + o.X)) <= 8 AndAlso Math.Abs(ui.Speaker.Y - (21 + o.Y)) <= 8, ui.Speaker.ToString())
                    ' windows painted over (closed) → none; speaker painted grey (muted) → off; PM rows painted over → none
                    Using t As New Bitmap(bmp)
                        Using g = Graphics.FromImage(t)
                            Dim br As New SolidBrush(Color.FromArgb(60, 80, 40))
                            g.FillRectangle(br, o.X + 370, o.Y + 140, 540, 520)      ' Temporary Item List
                            g.FillRectangle(br, o.X + 900, o.Y + 75, 380, 460)       ' Notice
                            g.FillRectangle(br, o.X + 95, o.Y + 400, 225, 135)        ' PM rows
                            g.FillEllipse(New SolidBrush(Color.FromArgb(90, 90, 95)), o.X + 384, o.Y + 5, 32, 32)   ' muted speaker
                        End Using
                        Dim u2 = AfterEnter.Read(t)
                        Check($"oyun içi: {name} kapatılmış → pencere yok", u2.Popups.Count = 0, u2.ToString())
                        Check($"oyun içi: {name} kapatılmış → PM yok", u2.PmCloses.Count = 0, u2.ToString())
                        Check($"oyun içi: {name} gri hoparlör → ses kapalı", Not u2.SoundOn, u2.ToString())
                        Check($"oyun içi: {name} pencereler kapalıyken de oyunda", LoginScreens.Classify(t).Kind = ScreenKind.InGame)
                    End Using
                End Using
            Next
            SetupTests(dir, f1)

            ' 3rd live run (10 Nov 00:55): Temporary Item List left open after ESC → its own X; Option open on the GAME tab
            Dim f3 = Path.Combine(dir, "deneme3-temporary-kapanmadi-tuval.png")
            If File.Exists(f3) Then
                Using bmp As New Bitmap(f3)
                    Dim ui = AfterEnter.Read(bmp)
                    Check("deneme3: Temporary Item List açık", ui.Popups.Contains("Temporary Item List"), ui.ToString())
                    Check("deneme3: Temporary X'i yerinde (859,178)", Math.Abs(ui.TempClose.X - 859) <= 4 AndAlso Math.Abs(ui.TempClose.Y - 178) <= 4, ui.TempClose.ToString())
                    Check("deneme3: Notice kapalı → X'i yok", Not ui.Popups.Contains("Notice") AndAlso ui.NoticeClose.IsEmpty, ui.ToString())
                End Using
            Else
                Check("deneme3: Temporary karesi var", False, f3)
            End If
            Using bmp As New Bitmap(f1)
                Dim ui = AfterEnter.Read(bmp)
                Check("ilk kare: Temporary X (859,178) + Notice X (1252,96)", Math.Abs(ui.TempClose.X - 859) <= 4 AndAlso Math.Abs(ui.TempClose.Y - 178) <= 4 AndAlso
                      Math.Abs(ui.NoticeClose.X - 1252) <= 5 AndAlso Math.Abs(ui.NoticeClose.Y - 96) <= 5, $"{ui.TempClose} {ui.NoticeClose}")
            End Using
            Dim f4 = Path.Combine(dir, "deneme3-option-game-sekmesi-tuval.png")
            If File.Exists(f4) Then
                Using bmp As New Bitmap(f4)
                    Dim sw = Stopwatch.StartNew()
                    Dim r = GameSetup.Read(bmp, boxes:=False)
                    Dim ms = sw.ElapsedMilliseconds
                    Check("deneme3: Option Game sekmesinde de AÇIK görülür", r.WindowOpen, r.ToString())
                    Check("deneme3: Game sekmesinde Low görünmez (Graphics'e geçilecek)", r.WindowOpen AndAlso Not r.LowVisible, r.ToString())
                    Check("deneme3: Option X yerinde (942,102)", Math.Abs(r.CloseX.X - 942) <= 5 AndAlso Math.Abs(r.CloseX.Y - 102) <= 5, r.CloseX.ToString())
                    Check("deneme3: Graphics sekmesi yerinde (410,303)", Math.Abs(r.GraphicsTab.X - 410) <= 6 AndAlso Math.Abs(r.GraphicsTab.Y - 303) <= 6, r.GraphicsTab.ToString())
                    Check($"deneme3: hafif okuma hızlı ({ms} ms < 1500)", ms < 1500, ms.ToString())
                End Using
            Else
                Check("deneme3: Option karesi var", False, f4)
            End If

            ' no false windows / PMs on the pre-game screens; the character screen centred in a big black view still works
            For Each f In Directory.GetFiles(loginDir, "1280x*.png")
                Using bmp As New Bitmap(f)
                    Dim ui = AfterEnter.Read(bmp)
                    Check($"oyun içi: {Path.GetFileName(f)} → pencere / PM yok", ui.Popups.Count = 0 AndAlso ui.PmCloses.Count = 0, ui.ToString())
                End Using
            Next
            Dim ch = Path.Combine(loginDir, "1280x800-karakter-1.png")
            If File.Exists(ch) Then
                Using src As New Bitmap(ch), big As New Bitmap(1920, 1032)
                    Using g = Graphics.FromImage(big)
                        g.Clear(Color.Black)
                        g.DrawImageUnscaled(src, 320, 116)
                    End Using
                    Dim r = LoginScreens.Classify(big)
                    Dim okc = r.Kind = ScreenKind.Character AndAlso Math.Abs(r.Character.LeftArrow.X - 937) <= 6 AndAlso Math.Abs(r.Character.LeftArrow.Y - 855) <= 8 AndAlso
                              Math.Abs(r.Character.StartGame.X - 1477) <= 14 AndAlso Math.Abs(r.Character.StartGame.Y - 704) <= 12
                    Check("ekran: ortalanmış karakter ekranı (1920×1032) → oklar + Start Game görünüm pikselinde", okc, $"{r.Kind} {r.Detail}")
                End Using
            End If
        End Sub

        ' ---------------- one-time game settings (Option window) ----------------

        Private Sub SetupTests(dir As String, worldFrame As String)
            Dim gfx = Path.Combine(dir, "option-graphics.png"), disp = Path.Combine(dir, "option-display.png")
            Check("ayar: Option kırpıntıları var", File.Exists(gfx) AndAlso File.Exists(disp))
            If Not File.Exists(gfx) OrElse Not File.Exists(disp) Then Return
            Using world As New Bitmap(worldFrame)
                ' real world frame: the face button found at canvas ≈126,777, no Option window
                Dim r0 = GameSetup.Read(world)
                Check("ayar: oyun karesinde Option simgesi (≈126,777)", Not r0.Face.IsEmpty AndAlso Math.Abs(r0.Face.X - 126) <= 5 AndAlso Math.Abs(r0.Face.Y - 777) <= 5, r0.ToString())
                Check("ayar: oyun karesinde Option penceresi yok", Not r0.WindowOpen, r0.ToString())
                ' user's crops pasted on the world frame at (330,100): Graphics page, then Display rows below (same window x; dy 139)
                For Each sc In {1.0, 0.75}
                    Using comp As New Bitmap(world)
                        Using g = Graphics.FromImage(comp)
                            Using a As New Bitmap(gfx), b As New Bitmap(disp)
                                g.DrawImageUnscaled(a, 330, 100)
                            End Using
                        End Using
                        Using shown As New Bitmap(comp, CInt(comp.Width * sc), CInt(comp.Height * sc))
                            Dim r = GameSetup.Read(shown)
                            Dim k = sc
                            Dim near = Function(p As Point, x As Integer, y As Integer) Math.Abs(p.X - x * k) <= 6 AndAlso Math.Abs(p.Y - y * k) <= 6
                            Check($"ayar ×{sc}: Option açık + Graphics sayfası", r.WindowOpen AndAlso r.LowVisible, r.ToString())
                            Check($"ayar ×{sc}: düğme yerleri (Graphics, Display, Low, Apply, X)",
                                  near(r.GraphicsTab, 403, 318) AndAlso near(r.DisplayTab, 403, 270) AndAlso near(r.LowButton, 551, 169) AndAlso
                                  near(r.Apply, 815, 662) AndAlso near(r.CloseX, 934, 118),
                                  $"{r.GraphicsTab} {r.DisplayTab} {r.LowButton} {r.Apply} {r.CloseX}")
                        End Using
                        Using g = Graphics.FromImage(comp)
                            Using b As New Bitmap(disp)
                                g.DrawImageUnscaled(b, 343, 239)   ' measured: the older display crop is 13 px right of the graphics crop
                            End Using
                        End Using
                        Using shown As New Bitmap(comp, CInt(comp.Width * sc), CInt(comp.Height * sc))
                            Dim r = GameSetup.Read(shown)
                            Dim box = Function(nm As String) r.Boxes.FirstOrDefault(Function(x) x.Name = nm)
                            Dim intro = box("Play intro movies"), vrr = box("VRR mode"), pw = box("Browser power saving")
                            Check($"ayar ×{sc}: Display kutuları bulundu", r.Boxes.Count = 3 AndAlso r.Boxes.All(Function(x) x.Found), r.ToString())
                            Check($"ayar ×{sc}: intro işaretli, VRR kapalı, güç tasarrufu işaretli",
                                  intro IsNot Nothing AndAlso intro.Checked AndAlso vrr IsNot Nothing AndAlso Not vrr.Checked AndAlso pw IsNot Nothing AndAlso pw.Checked, r.ToString())
                            If intro IsNot Nothing AndAlso intro.Found Then
                                Check($"ayar ×{sc}: intro kutusu yerinde (490,327)", Math.Abs(intro.Box.X - 490 * sc) <= 4 AndAlso Math.Abs(intro.Box.Y - 327 * sc) <= 4, intro.Box.ToString())
                            End If
                        End Using
                        If sc = 1.0 Then
                            ' tick removed (user's click) → read as cleared
                            Using g = Graphics.FromImage(comp)
                                g.FillRectangle(New SolidBrush(Color.FromArgb(10, 10, 8)), 483, 320, 14, 14)
                            End Using
                            Dim r2 = GameSetup.Read(comp)
                            Dim i2 = r2.Boxes.FirstOrDefault(Function(x) x.Name = "Play intro movies")
                            Check("ayar: tik silinince intro kapalı okunur", i2 IsNot Nothing AndAlso i2.Found AndAlso Not i2.Checked, r2.ToString())
                        End If
                    End Using
                Next
            End Using
            ' Display page with frame limit 30 / camera 16 / "1 ayar uygulandı." (user's crop) pasted at (300,80) → title corner 550,104
            Dim fps = Path.Combine(dir, "option-display-fps30-kamera16.png")
            Check("ayar: kare sınırı / kamera kırpıntısı var", File.Exists(fps))
            If File.Exists(fps) Then
                Using world As New Bitmap(worldFrame), crop As New Bitmap(fps)
                    For Each sc In {1.0, 0.75}
                        Using comp As New Bitmap(world)
                            Using g = Graphics.FromImage(comp)
                                g.DrawImageUnscaled(crop, 300, 80)
                            End Using
                            Using shown As New Bitmap(comp, CInt(comp.Width * sc), CInt(comp.Height * sc))
                                Dim r = GameSetup.Read(shown)
                                Dim k = sc
                                Dim near = Function(p As Point, x As Integer, y As Integer) Math.Abs(p.X - x * k) <= 5 AndAlso Math.Abs(p.Y - y * k) <= 5
                                If sc = 1.0 Then
                                    Check($"ayar ×{sc}: kamera 16 okundu", r.Camera = 16, r.ToString())
                                    Check($"ayar ×{sc}: kare sınırı 30 okundu", r.FrameLimit = 30, r.ToString())
                                Else
                                    ' scaled-down view: 8 px digits blur — must read right OR not at all, never a wrong number
                                    Check($"ayar ×{sc}: kamera doğru ya da okunamadı (yanlış sayı yok)", r.Camera = 16 OrElse r.Camera = -1, r.ToString())
                                    Check($"ayar ×{sc}: kare sınırı doğru ya da okunamadı (yanlış sayı yok)", r.FrameLimit = 30 OrElse r.FrameLimit = -1, r.ToString())
                                End If
                                Check($"ayar ×{sc}: 'ayar uygulandı' görüldü", r.AppliedSeen, r.ToString())
                                If sc = 1.0 Then Check($"ayar ×{sc}: Display kutuları (yeni yerleşim, hepsi kapalı)", r.Boxes.All(Function(b) b.Found AndAlso Not b.Checked), r.ToString())
                                Check($"ayar ×{sc}: ok yerleri (kamera ◄ 637,168 ► 830,168 · kare ◄ 637,275 ► 884,275)",
                                      near(r.CameraLeft, 637, 168) AndAlso near(r.CameraRight, 830, 168) AndAlso near(r.FrameLeft, 637, 275) AndAlso near(r.FrameRight, 884, 275),
                                      $"{r.CameraLeft} {r.CameraRight} {r.FrameLeft} {r.FrameRight}")
                            End Using
                            If sc = 1.0 Then
                                ' "30" → "60": the measured '6' glyph over the '3' (crop 453..458 / 570..575 → +300,+80)
                                Using t As New Bitmap(comp)
                                    For y = 0 To 7
                                        For x = 0 To 6
                                            t.SetPixel(753 + x, 272 + y, comp.GetPixel(870 + x, 164 + y))
                                        Next
                                    Next
                                    Dim r6 = GameSetup.Read(t)
                                    Check("ayar: '60' okunur (ölçülen 6 glyph'iyle)", r6.FrameLimit = 60, r6.FrameLimitText)
                                End Using
                                ' a letter-like word ("Off") → not a number, left alone
                                Using t As New Bitmap(comp)
                                    Using g = Graphics.FromImage(t)
                                        g.FillRectangle(New SolidBrush(Color.FromArgb(4, 4, 4)), 700, 266, 120, 20)
                                        g.DrawString("Off", New Font("Verdana", 7, FontStyle.Bold), Brushes.White, 748, 268)
                                    End Using
                                    Dim rOff = GameSetup.Read(t)
                                    Check("ayar: yazı (Off) sayı okunmaz → -1", rOff.FrameLimit = -1, $"'{rOff.FrameLimitText}'")
                                End Using
                                ' no "ayar uygulandı" (before Apply) → not seen
                                Using t As New Bitmap(comp)
                                    Using g = Graphics.FromImage(t)
                                        g.FillRectangle(New SolidBrush(Color.FromArgb(20, 18, 12)), 540, 685, 200, 25)
                                    End Using
                                    Check("ayar: Apply öncesi 'uygulandı' görülmez", Not GameSetup.Read(t).AppliedSeen)
                                End Using
                            End If
                        End Using
                    Next
                End Using
            End If

            ' once-per-account store: SHA-256 key, ID never on disk
            Dim tmp = Path.Combine(Path.GetTempPath(), "wwkm-setup-" & Guid.NewGuid().ToString("N") & ".json")
            Dim old = GameSetup.PathOverride
            GameSetup.PathOverride = tmp
            Try
                Dim k = GameSetup.AccountKey("Oyuncu_01")
                Check("ayar: anahtar SHA-256 (64 hex), büyük/küçük harf aynı", k.Length = 64 AndAlso k = GameSetup.AccountKey(" oyuncu_01 "))
                Check("ayar: ilk girişte yapılmamış", Not GameSetup.IsDone(k))
                GameSetup.MarkDone(k)
                Dim txt = File.ReadAllText(tmp)
                Check("ayar: işaret kaydedildi, dosyada ID yok", GameSetup.IsDone(k) AndAlso Not txt.ToLowerInvariant().Contains("oyuncu"), txt.Replace(vbLf, " "))
                Check("ayar: başka hesap etkilenmez", Not GameSetup.IsDone(GameSetup.AccountKey("ikinci")))
                GameSetup.Forget("oyuncu_01")
                Check("ayar: yeniden uygula (Forget) işareti siler", Not GameSetup.IsDone(k))
                ' an OLD (v1, unversioned) mark must not skip the new settings
                File.WriteAllText(tmp, "{""done"":{""" & k & """:""2026-11-10T00:30:00""}}")
                Check($"ayar: eski (v1) işaret v{GameSetup.SetupVersion} ayarını atlatmaz", Not GameSetup.IsDone(k))
                GameSetup.MarkDone(k)
                Check("ayar: v2 işareti sayılır", GameSetup.IsDone(k) AndAlso File.ReadAllText(tmp).Contains($"v{GameSetup.SetupVersion}:{k}"))
            Finally
                GameSetup.PathOverride = old
                Try
                    File.Delete(tmp)
                Catch
                End Try
            End Try
        End Sub

        ' ---------------- screens ----------------

        Private Sub ScreenTests(dir As String)
            Dim real = If(Directory.Exists(dir), Directory.GetFiles(dir, "*giris*.png"), New String() {})
            If Directory.Exists(dir) Then
                Check($"ekran: gerçek giriş kareleri var ({dir})", real.Length > 0, real.Length.ToString())
            Else
                Skip("gerçek giriş / sunucu / karakter / ulus kareleri (" & dir & ")")
            End If
            For Each f In real
                Using bmp As New Bitmap(f)
                    Dim r = LoginScreens.Classify(bmp)
                    Dim name = Path.GetFileName(f)
                    Dim ok = r.Kind = ScreenKind.Login
                    Check($"ekran: {name} → giriş", ok, r.Detail)
                    If ok AndAlso bmp.Width = 1280 AndAlso bmp.Height = 720 Then
                        ' measured: ID box rows 269..286, password 319..336, OK band 390..404, boxes x ≈ 580..707
                        Dim idc = LoginBox.Center(r.Login.IdBox), pwc = LoginBox.Center(r.Login.PasswordBox)
                        Check($"ekran: {name} ID kutusu yerinde", Math.Abs(idc.Y - 277) <= 4 AndAlso Math.Abs(idc.X - 644) <= 12, idc.ToString())
                        Check($"ekran: {name} şifre kutusu yerinde", Math.Abs(pwc.Y - 327) <= 4 AndAlso Math.Abs(pwc.X - 644) <= 12, pwc.ToString())
                        Check($"ekran: {name} OK yerinde", Math.Abs(r.Login.OkButton.Y - 397) <= 4, r.Login.OkButton.ToString())
                        Dim px = LoginScreens.ReadPixels(bmp)
                        Dim c1 = LoginScreens.TextColumns(px, bmp.Width, bmp.Height, r.Login.IdBox)
                        If name.Contains("yazili") Then
                            ' real frame with the game's own drawing of a typed dummy ID ("probe_x9") and 8 asterisks
                            Dim cp = LoginScreens.TextColumns(px, bmp.Width, bmp.Height, r.Login.PasswordBox)
                            Check($"ekran: {name} oyunun çizdiği ID görülür", c1 >= LoginScreens.TextColumnThreshold, c1.ToString())
                            Check($"ekran: {name} oyunun çizdiği yıldızlar görülür", cp >= LoginScreens.TextColumnThreshold, cp.ToString())
                        Else
                            Check($"ekran: {name} boş ID kutusu yazısız", c1 >= 0 AndAlso c1 < LoginScreens.TextColumnThreshold, c1.ToString())
                        End If
                        ' typed text (white glyph-like strokes) and asterisks must be seen
                        Using t As New Bitmap(bmp)
                            Using g = Graphics.FromImage(t)
                                g.TextRenderingHint = Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit
                                Dim rb = r.Login.IdBox
                                g.DrawString("oyuncu01", New Font("Verdana", 9, FontStyle.Bold), Brushes.White, rb.X + 4, rb.Y + 2)
                                Dim pb = r.Login.PasswordBox
                                g.DrawString("******", New Font("Verdana", 9, FontStyle.Bold), Brushes.White, pb.X + 4, pb.Y + 3)
                            End Using
                            Dim tp = LoginScreens.ReadPixels(t)
                            Dim c2 = LoginScreens.TextColumns(tp, t.Width, t.Height, r.Login.IdBox)
                            Dim c3 = LoginScreens.TextColumns(tp, t.Width, t.Height, r.Login.PasswordBox)
                            Check($"ekran: {name} yazılı ID görülür", c2 >= LoginScreens.TextColumnThreshold, c2.ToString())
                            Check($"ekran: {name} yıldızlar görülür", c3 >= LoginScreens.TextColumnThreshold, c3.ToString())
                            Dim again = LoginScreens.Classify(t)
                            Check($"ekran: {name} yazılıyken de giriş ekranı", again.Kind = ScreenKind.Login, again.Detail)
                        End Using
                    End If
                    ' scaled copies (the canvas follows the view size)
                    For Each k In {0.75, 1.5}
                        Using s As New Bitmap(bmp, CInt(bmp.Width * k), CInt(bmp.Height * k))
                            Dim rs = LoginScreens.Classify(s)
                            Check($"ekran: {name} ×{k} → giriş", rs.Kind = ScreenKind.Login, rs.Detail)
                        End Using
                    Next
                End Using
            Next

            ' after-login screens from a live run (1280×800)
            For Each f In If(Directory.Exists(dir), Directory.GetFiles(dir, "*sunucu*.png"), New String() {})
                Using bmp As New Bitmap(f)
                    Dim name = Path.GetFileName(f)
                    Dim r = LoginScreens.Classify(bmp)
                    Check($"ekran: {name} → sunucu", r.Kind = ScreenKind.Server, $"{r.Kind} {r.Detail}")
                    If r.Kind = ScreenKind.Server AndAlso bmp.Width = 1280 Then
                        Check($"ekran: {name} Connect yerinde (644,673)", Math.Abs(r.Connect.X - 644) <= 12 AndAlso Math.Abs(r.Connect.Y - 673) <= 10, r.Connect.ToString())
                    End If
                    For Each k In {0.75, 1.5}
                        Using s As New Bitmap(bmp, CInt(bmp.Width * k), CInt(bmp.Height * k))
                            Dim rs = LoginScreens.Classify(s)
                            Check($"ekran: {name} ×{k} → sunucu", rs.Kind = ScreenKind.Server, $"{rs.Kind} {rs.Detail}")
                        End Using
                    Next
                End Using
            Next
            Dim chars = If(Directory.Exists(dir), Directory.GetFiles(dir, "*karakter*.png"), New String() {})
            If Directory.Exists(dir) Then Check("ekran: gerçek karakter kareleri var", chars.Length > 0, chars.Length.ToString())
            For Each f In chars
                Using bmp As New Bitmap(f)
                    Dim name = Path.GetFileName(f)
                    Dim r = LoginScreens.Classify(bmp)
                    Check($"ekran: {name} → karakter (giriş DEĞİL)", r.Kind = ScreenKind.Character, $"{r.Kind} {r.Detail}")
                    If r.Kind = ScreenKind.Character AndAlso bmp.Width = 1280 Then
                        Dim c = r.Character
                        Check($"ekran: {name} ◄ yerinde + açık", Math.Abs(c.LeftArrow.X - 617) <= 6 AndAlso Math.Abs(c.LeftArrow.Y - 739) <= 8 AndAlso c.LeftEnabled, c.ToString())
                        Check($"ekran: {name} ► yerinde + gri", Math.Abs(c.RightArrow.X - 663) <= 6 AndAlso Math.Abs(c.RightArrow.Y - 739) <= 8 AndAlso Not c.RightEnabled, c.ToString())
                        Check($"ekran: {name} Start Game yerinde (1157,588)", Not c.StartGame.IsEmpty AndAlso Math.Abs(c.StartGame.X - 1157) <= 14 AndAlso Math.Abs(c.StartGame.Y - 588) <= 12, c.StartGame.ToString())
                    End If
                    For Each k In {0.75, 1.5}
                        Using s As New Bitmap(bmp, CInt(bmp.Width * k), CInt(bmp.Height * k))
                            Dim rs = LoginScreens.Classify(s)
                            Check($"ekran: {name} ×{k} → karakter", rs.Kind = ScreenKind.Character, $"{rs.Kind} {rs.Detail}")
                        End Using
                    Next
                    ' ► gold too (a previous character exists): paint the grey button gold → RightEnabled
                    If r.Kind = ScreenKind.Character Then
                        Using t As New Bitmap(bmp)
                            For y = r.Character.RightArrow.Y - 20 To r.Character.RightArrow.Y + 20
                                For x = r.Character.RightArrow.X - 20 To r.Character.RightArrow.X + 20
                                    Dim p = t.GetPixel(x, y)
                                    If Math.Abs(CInt(p.R) - p.G) <= 14 AndAlso p.R >= 60 AndAlso p.R <= 215 Then t.SetPixel(x, y, Color.FromArgb(140, 110, 30))
                                Next
                            Next
                            Dim rt = LoginScreens.Classify(t)
                            Check($"ekran: {name} altın ► → açık", rt.Kind = ScreenKind.Character AndAlso rt.Character.RightEnabled, rt.Detail)
                        End Using
                    End If
                End Using
            Next
            For Each f In If(Directory.Exists(dir), Directory.GetFiles(dir, "*ulus*.png"), New String() {})
                Using bmp As New Bitmap(f)
                    Dim r = LoginScreens.Classify(bmp)
                    Check($"ekran: {Path.GetFileName(f)} (ulus seçimi) → giriş / sunucu / karakter DEĞİL", r.Kind = ScreenKind.Unknown OrElse r.Kind = ScreenKind.Blank, $"{r.Kind} {r.Detail}")
                End Using
            Next
            ' the login frames must not read as server / character either (covered: they must read as Login above)

            ' negatives
            Using b As New Bitmap(1280, 720)
                Using g = Graphics.FromImage(b)
                    g.Clear(Color.Black)
                End Using
                Check("ekran: siyah kare → boş", LoginScreens.Classify(b).Kind = ScreenKind.Blank)
            End Using
            If real.Length > 0 Then
                Using bmp As New Bitmap(real(0))
                    ' login panel painted over with scene colour → not login
                    Using t As New Bitmap(bmp)
                        Using g = Graphics.FromImage(t)
                            g.FillRectangle(New SolidBrush(Color.FromArgb(120, 90, 80)), 520, 180, 260, 340)
                        End Using
                        Dim r = LoginScreens.Classify(t)
                        Check("ekran: giriş paneli yokken giriş değil", r.Kind <> ScreenKind.Login, r.Kind.ToString())
                    End Using
                    ' OK button removed (only two black boxes, e.g. a different dialog) → not login
                    Using t As New Bitmap(bmp)
                        Using g = Graphics.FromImage(t)
                            g.FillRectangle(New SolidBrush(Color.FromArgb(70, 68, 50)), 560, 380, 170, 60)
                        End Using
                        Dim r = LoginScreens.Classify(t)
                        Check("ekran: kırmızı OK yoksa giriş değil", r.Kind <> ScreenKind.Login, r.Kind.ToString())
                    End Using
                End Using
            End If
            ' synthetic HUD (measured on ardream: frame line (80,96,73), black row below, HP then MP)
            Using b As New Bitmap(1280, 720)
                Using g = Graphics.FromImage(b)
                    g.Clear(Color.FromArgb(40, 70, 40))
                    For Each y0 In {22, 33}
                        g.FillRectangle(New SolidBrush(Color.FromArgb(80, 96, 73)), 30, y0, 128, 1)
                        g.FillRectangle(Brushes.Black, 30, y0 + 1, 128, 9)
                        g.FillRectangle(New SolidBrush(If(y0 = 22, Color.FromArgb(230, 10, 10), Color.FromArgb(25, 60, 230))), 31, y0 + 2, 100, 7)
                    Next
                End Using
                Dim r = LoginScreens.Classify(b)
                Check("ekran: HP/MP çerçevesi → oyunda (sentetik)", r.Kind = ScreenKind.InGame, r.Detail)
            End Using
            ' signature / difference
            If real.Length > 0 Then
                Using a As New Bitmap(real(0))
                    Dim s1 = LoginScreens.Signature(a)
                    Check("ekran: aynı kare farkı 0", LoginScreens.Difference(s1, LoginScreens.Signature(a)) = 0)
                    Using b As New Bitmap(a.Width, a.Height)
                        Using g = Graphics.FromImage(b)
                            g.Clear(Color.Black)
                        End Using
                        Check("ekran: siyaha geçiş büyük fark", LoginScreens.Difference(s1, LoginScreens.Signature(b)) > 0.5)
                    End Using
                End Using
            End If
        End Sub

    End Module

End Namespace
