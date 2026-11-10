Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Vision

Namespace Engine.Follow

    ''' <summary>
    ''' Debug check without the game (`RedSquirrel.WebKoMulti.exe follow-selftest [kareler=&lt;klasör&gt;] [tamekran=&lt;klasör&gt;]`), sends NO input anywhere:
    '''  1. coordinate read: every recorded full view frame (goruntu*.png) read whole and as the 240×96 crop the follow round
    '''     captures → both must agree; the recorded 320×30 strips (koord\*.png, view rows 66..95) placed into a crop must read
    '''     and stay continuous;
    '''  2. FollowWalker decisions in a virtual world (simulator): unknown camera yaw, input lag,
    '''     sliding, integer coordinates, lost presses, unreadable frames, teleport, leader walking / out of range, three view
    '''     sizes (1280×800, 742×735, 1920×1080).
    ''' Result: Logs\follow-selftest-&lt;time&gt;.log, last line "SONUC: GECTI n/m" or "SONUC: DUSTU n/m"; exit 0 / 1.
    ''' </summary>
    Public Module FollowSelfTest

        Private _sb As StringBuilder
        Private _pass As Integer, _total As Integer

        Public Function Run(args As String()) As Integer
            _sb = New StringBuilder()
            _pass = 0 : _total = 0
            Dim frames As String = Nothing
            Dim fullDir As String = Nothing
            For Each a In If(args, New String() {})
                If a.StartsWith("kareler=", StringComparison.OrdinalIgnoreCase) Then frames = a.Substring(8)
                If a.StartsWith("tamekran=", StringComparison.OrdinalIgnoreCase) Then fullDir = a.Substring(9)
            Next
            If String.IsNullOrEmpty(frames) Then frames = FindFrames()
            Line($"follow-selftest {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            Try
                ReadTests(frames)
            Catch ex As Exception
                Check("koordinat okuma koştu", False, ex.ToString())
            End Try
            Try
                FullScreenTests(If(fullDir, FindDocsFrames()))
            Catch ex As Exception
                Check("tam ekran karesi koştu", False, ex.ToString())
            End Try
            Try
                MirrorTests()
            Catch ex As Exception
                Check("yansıtma dönüşümleri koştu", False, ex.ToString())
            End Try
            Try
                PaceTests()
                ModeTests()
            Catch ex As Exception
                Check("tempo / kip koştu", False, ex.ToString())
            End Try
            Try
                WalkTests()
            Catch ex As Exception
                Check("yürüyüş benzetimi koştu", False, ex.ToString())
            End Try
            Dim ok = _pass = _total AndAlso _total > 0
            Line($"SONUC: {If(ok, "GECTI", "DUSTU")} {_pass}/{_total}")
            Try
                Dim dir = Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Logs"))
                Directory.CreateDirectory(dir)
                File.WriteAllText(Path.Combine(dir, $"follow-selftest-{DateTime.Now:yyyyMMdd-HHmmss}.log"), _sb.ToString(), New UTF8Encoding(False))
            Catch
            End Try
            Console.Write(_sb.ToString())
            Return If(ok, 0, 1)
        End Function

        ''' <summary>Text of the last run (for a caller that loads the assembly instead of starting the exe).</summary>
        Public ReadOnly Property Output As String
            Get
                Return If(_sb Is Nothing, "", _sb.ToString())
            End Get
        End Property

        Private Sub Line(s As String)
            _sb.AppendLine(s)
        End Sub

        Private Sub Check(name As String, ok As Boolean, Optional detail As String = "")
            _total += 1
            If ok Then _pass += 1
            Line($"[{If(ok, "OK ", "BAD")}] {name}{If(detail = "", "", " — " & detail)}")
        End Sub

        ' Recorded frames (optional, not shipped) at 1280 / 1284 wide (HUD at its 1:1 size). Narrower views draw the HUD smaller;
        ' goruntu2-2327*/2329* are a loading screen (no HUD at all) — listed so their "—" is not read as a failure.
        Private ReadOnly NoHud As String() = {"goruntu2-232705", "goruntu2-232706", "goruntu2-232707", "goruntu2-232744", "goruntu2-232930", "goruntu2-232939"}

        Private Function IsStdSize(f As String) As Boolean
            Dim n = Path.GetFileNameWithoutExtension(f)
            If NoHud.Contains(n) Then Return False
            Using b As New Bitmap(f)
                Return b.Width >= 1270 AndAlso b.Width <= 1300
            End Using
        End Function

        Private Function FindFrames() As String
            Dim d = New DirectoryInfo(AppPaths.ExeDir())
            While d IsNot Nothing
                Dim c = Path.Combine(d.FullName, "Docs", "olcum", "koord")
                If Directory.Exists(c) Then Return c
                d = d.Parent
            End While
            Return ""
        End Function

        Private Function Fmt(p As Point?) As String
            Return If(p.HasValue, $"{p.Value.X},{p.Value.Y}", "—")
        End Function

        ' ------------------------------------------------------------------ 1. reading

        Private Sub ReadTests(dir As String)
            If String.IsNullOrEmpty(dir) OrElse Not Directory.Exists(dir) Then
                Line("SKIP atlandı: test kareleri yok — koordinat okuma (kareler=<klasör> ile verilebilir)")
                Return
            End If
            Line("kareler: " & dir)
            Dim files = Directory.GetFiles(dir, "goruntu*.png").OrderBy(Function(f) f).ToList()
            Check("tam kareler var", files.Count > 0, $"{files.Count} kare")
            Dim same = 0, readFull = 0
            For Each f In files
                Using full As New Bitmap(f)
                    Dim a = WebCoordReader.Read(full)
                    Dim crop As Bitmap
                    Using tmp = full.Clone(WebCoordReader.Region, full.PixelFormat)
                        crop = New Bitmap(tmp)   ' fresh bitmap, as ViewCapture.RegionAsync returns
                    End Using
                    Dim b As Point?
                    Using crop
                        b = WebCoordReader.Read(crop)
                    End Using
                    If a.HasValue Then readFull += 1
                    If Nullable.Equals(a, b) Then same += 1
                    Line($"   {Path.GetFileName(f)} {full.Width}×{full.Height}: tam {Fmt(a)} · kesit {Fmt(b)}")
                End Using
            Next
            Check("kesit = tam kare (her karede aynı sonuç)", same = files.Count, $"{same}/{files.Count}")
            Dim std = files.Where(Function(f) IsStdSize(f)).ToList()
            Dim readStd = std.Where(Function(f)
                                        Using b As New Bitmap(f)
                                            Return WebCoordReader.Read(b).HasValue
                                        End Using
                                    End Function).Count()
            Check("1280 genişliğindeki oyun karelerinin hepsi okunuyor", readStd = std.Count, $"{readStd}/{std.Count} (bütün kareler: {readFull}/{files.Count} — küçük görünümlerde yazı ölçeklenmiş, aşağıya bak)")

            ' The canvas locator on the old frames must not change any read: reading through the located canvas gives the
            ' same answer as reading the frame as before.
            Dim sameViaCanvas = 0
            For Each f In files
                Using full As New Bitmap(f)
                    Dim cv = GameCanvas.LocateForCoords(full)
                    Dim viaCanvas = ReadCrop(full, WebCoordReader.RegionIn(cv))
                    If Nullable.Equals(viaCanvas, WebCoordReader.Read(full)) Then sameViaCanvas += 1 Else Line($"   {Path.GetFileName(f)}: tuval {cv} → {Fmt(viaCanvas)}")
                End Using
            Next
            Check("tuval bulucu eski karelerde okumayı değiştirmiyor", sameViaCanvas = files.Count, $"{sameViaCanvas}/{files.Count}")

            ' The follow round captures JPEG (FollowCapture — the clipped PNG capture flickered): every frame re-encoded as
            ' JPEG quality 60 / 80 must read exactly as the PNG did.
            Dim jpgSame = 0, jpgN = 0
            For Each f In files
                Using full As New Bitmap(f)
                    Dim want = ReadCrop(full, WebCoordReader.RegionIn(GameCanvas.LocateForCoords(full)))
                    For Each q In {60L, 80L}
                        Using j = Jpeg(full, q)
                            Dim got = ReadCrop(j, WebCoordReader.RegionIn(GameCanvas.LocateForCoords(j)))
                            jpgN += 1
                            If Nullable.Equals(got, want) Then jpgSame += 1 Else Line($"   {Path.GetFileName(f)} jpeg q{q}: {Fmt(got)} (png {Fmt(want)})")
                        End Using
                    Next
                End Using
            Next
            Check("JPEG kare (q60 / q80) PNG ile aynı okunuyor", jpgSame = jpgN, $"{jpgSame}/{jpgN}")

            Dim stripDir = Path.Combine(dir, "koord")
            If Directory.Exists(stripDir) Then
                Dim strips = Directory.GetFiles(stripDir, "*.png").OrderBy(Function(f) f).ToList()
                Dim ok = 0, jumps = 0
                Dim prev As Point? = Nothing
                For Each f In strips
                    Using s As New Bitmap(f)
                        Using crop As New Bitmap(WebCoordReader.Region.Width, WebCoordReader.Region.Height)
                            Using g = Graphics.FromImage(crop)
                                g.Clear(Color.Black)
                                g.DrawImageUnscaled(s, 0, 66)
                            End Using
                            Dim p = WebCoordReader.Read(crop)
                            If p.HasValue Then
                                ok += 1
                                If prev.HasValue AndAlso Math.Abs(p.Value.X - prev.Value.X) + Math.Abs(p.Value.Y - prev.Value.Y) > 25 Then jumps += 1
                                prev = p
                            End If
                        End Using
                    End Using
                Next
                Check("kayıtlı koordinat şeritleri kesitte okunuyor", strips.Count > 0 AndAlso ok >= strips.Count * 0.97, $"{ok}/{strips.Count}")
                Check("şerit okumaları sürekli (yanlış okuma yok)", jumps = 0, $"{jumps} sıçrama")
            End If
        End Sub

        Private Function Jpeg(b As Bitmap, quality As Long) As Bitmap
            Dim codec = Imaging.ImageCodecInfo.GetImageEncoders().First(Function(c) c.FormatID = Imaging.ImageFormat.Jpeg.Guid)
            Using ps As New Imaging.EncoderParameters(1)
                ps.Param(0) = New Imaging.EncoderParameter(Imaging.Encoder.Quality, quality)
                Using ms As New MemoryStream()
                    b.Save(ms, codec, ps)
                    ms.Position = 0
                    Using d As New Bitmap(ms)
                        Return New Bitmap(d)
                    End Using
                End Using
            End Using
        End Function

        Private Function ReadCrop(full As Bitmap, r As Rectangle) As Point?
            r.Intersect(New Rectangle(0, 0, full.Width, full.Height))
            If r.Width <= 0 OrElse r.Height <= 0 Then Return Nothing
            Using tmp = full.Clone(r, full.PixelFormat)
                Using crop As New Bitmap(tmp)
                    Return WebCoordReader.Read(crop)
                End Using
            End Using
        End Function

        Private Function FindDocsFrames() As String
            Dim d = New DirectoryInfo(AppPaths.ExeDir())
            While d IsNot Nothing
                Dim c = Path.Combine(d.FullName, "Docs", "olcum", "takip")
                If Directory.Exists(c) Then Return c
                d = d.Parent
            End While
            Return ""
        End Function

        ''' <summary>
        ''' Real game frames (optional, not shipped), in Docs\olcum\takip. In a view larger than the game the canvas is
        ''' NOT scaled: 1280×800 in the middle with black around it (and a light page strip on top in the full-screen one).
        ''' lider-tamekran-1920x1032.png is converted from a lossy .webp, so it also shows the read survives compression.
        ''' </summary>
        Private Sub FullScreenTests(dir As String)
            If String.IsNullOrEmpty(dir) OrElse Not Directory.Exists(dir) Then
                Line("SKIP atlandı: test kareleri yok — tam ekran tuval kareleri (tamekran=<klasör> ile verilebilir)")
                Return
            End If
            Dim cases = {
                ("lider-tamekran-1920x1032.png", New Rectangle(320, 143, 1280, 800)),
                ("tamekran-1920x977.png", New Rectangle(320, 88, 1280, 800)),
                ("gorunum-1280x800.png", New Rectangle(0, 0, 1280, 800))}
            For Each c In cases
                Dim f = If(String.IsNullOrEmpty(dir), "", Path.Combine(dir, c.Item1))
                If Not File.Exists(f) Then
                    Check($"gerçek kare bulundu: {c.Item1}", False, "yok: " & f)
                    Continue For
                End If
                Using full As New Bitmap(f)
                    Dim want = c.Item2
                    Dim cv = GameCanvas.Locate(full)
                    Dim tag = $"{c.Item1} ({full.Width}×{full.Height})"
                    Check($"{tag}: oyun tuvali {want.X},{want.Y} · {want.Width}×{want.Height}",
                          Math.Abs(cv.X - want.X) <= 1 AndAlso Math.Abs(cv.Y - want.Y) <= 1 AndAlso cv.Width = want.Width AndAlso cv.Height = want.Height,
                          $"bulunan {cv.X},{cv.Y} · {cv.Width}×{cv.Height}")
                    Dim old = ReadCrop(full, WebCoordReader.Region)
                    Dim cr = GameCanvas.LocateForCoords(full)
                    Dim p = ReadCrop(full, WebCoordReader.RegionIn(cr))
                    Check($"{tag}: tuvale göre kesit 1503,503 okuyor", Nullable.Equals(p, New Point(1503, 503)),
                          $"{Fmt(p)} · tuval üstü {cr.Y} (eski sabit kesit: {Fmt(old)})")
                    Dim cc = FollowWalker.CircleCenter(cr)
                    Check($"{tag}: yürüme çemberi tuvalin ortasında", Math.Abs(cc.X - (want.X + 640)) <= 1 AndAlso Math.Abs(cc.Y - (want.Y + 448)) <= 1, $"merkez {cc.X},{cc.Y}")
                End Using
            Next
        End Sub

        ' ------------------------------------------------------------------ input mirror (side-effect-free part)

        ''' <summary>
        ''' Engine/Mirror/MirrorMap: leader view point → canvas point → follower view point, and the DevTools JSON. Views:
        ''' leader full-screen 1920×1032 (canvas 320,143 · 1280×800, measured), follower 1280×800 (view = canvas),
        ''' follower full-screen 1920×977 (canvas 320,88), follower small view 742×464 (game drawn smaller, view = canvas).
        ''' </summary>
        Private Sub MirrorTests()
            Dim leaderCv = New Rectangle(320, 143, 1280, 800)
            Dim inBorder = Engine.Mirror.MirrorMap.ToCanvas(New PointF(100, 100), leaderCv)
            Dim onCanvas = Engine.Mirror.MirrorMap.ToCanvas(New PointF(400, 200), leaderCv)
            Check("yansıtma: siyah kenara tık aktarılmaz, tuvaldeki tık tuvale göre", Not inBorder.HasValue AndAlso
                  onCanvas.HasValue AndAlso onCanvas.Value = New PointF(80, 57), $"kenar {inBorder.HasValue}, tuval {onCanvas}")
            Dim lsz = leaderCv.Size
            Dim f1 = Engine.Mirror.MirrorMap.ToFollower(onCanvas.Value, lsz, New Rectangle(0, 0, 1280, 800))
            Dim f2 = Engine.Mirror.MirrorMap.ToFollower(onCanvas.Value, lsz, New Rectangle(320, 88, 1280, 800))
            Dim f3 = Engine.Mirror.MirrorMap.ToFollower(onCanvas.Value, lsz, New Rectangle(0, 0, 742, 464))
            Check("yansıtma: takipçinin kendi tuvaline (1280×800 / tam ekran / küçük görünüm)",
                  f1 = New Point(80, 57) AndAlso f2 = New Point(400, 145) AndAlso f3 = New Point(46, 33), $"{f1} · {f2} · {f3}")
            ' The game's own centre (skill bar, middle of the screen) lands on the follower's centre at every size.
            Dim mid = Engine.Mirror.MirrorMap.ToCanvas(New PointF(320 + 640, 143 + 400), leaderCv).Value
            Dim m3 = Engine.Mirror.MirrorMap.ToFollower(mid, lsz, New Rectangle(0, 0, 742, 464))
            Dim edge = Engine.Mirror.MirrorMap.ToFollower(New PointF(1279, 799), lsz, New Rectangle(320, 88, 1280, 800))
            Check("yansıtma: orta noktası ortaya, köşe tuvalin içinde kalır", m3 = New Point(371, 232) AndAlso edge = New Point(1599, 887), $"{m3} · {edge}")
            Dim clamp = Engine.Mirror.MirrorMap.ToCanvasClamped(New PointF(2000, 50), leaderCv)
            Check("yansıtma: tuvalden çıkan sürükleme kenarda tutulur", clamp = New PointF(1279, 0), clamp.ToString())

            Dim cdp = Function(ev As Engine.Mirror.MirrorEvent) Engine.Mirror.MirrorMap.ToCdp(ev, New Point(80, 57))
            Dim down = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.MouseDown, .Button = "left", .Buttons = 1, .ClickCount = 2})
            Dim drag = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.MouseMove, .Buttons = 1})
            Dim up = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.MouseUp, .Button = "right", .Buttons = 0, .ClickCount = 1})
            Dim wheel = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.Wheel, .WheelDelta = 120})
            Check("yansıtma: fare JSON (basış çift tık, basılı sürükleme, sağ bırakma, tekerlek yukarı)",
                  down.Json.Contains("""type"":""mousePressed""") AndAlso down.Json.Contains("""clickCount"":2") AndAlso down.Json.Contains("""x"":80,""y"":57") AndAlso
                  drag.Json.Contains("""type"":""mouseMoved""") AndAlso drag.Json.Contains("""button"":""left"",""buttons"":1") AndAlso
                  up.Json.Contains("""type"":""mouseReleased""") AndAlso up.Json.Contains("""button"":""right""") AndAlso
                  wheel.Json.Contains("""type"":""mouseWheel""") AndAlso wheel.Json.Contains("""deltaY"":-120"),
                  down.Json)
            Dim ka = Engine.Mirror.MirrorMap.KeyNames(&H41, False, "a")
            Dim kf = Engine.Mirror.MirrorMap.KeyNames(&H70, False, "")
            Dim keyA = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.KeyDown, .Vk = &H41, .Key = ka.Key, .Code = ka.Code, .Text = "a"})
            Dim ctrlA = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.KeyDown, .Vk = &H41, .Key = ka.Key, .Code = ka.Code, .Text = "a", .Modifiers = 2})
            Dim f1k = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.KeyDown, .Vk = &H70, .Key = kf.Key, .Code = kf.Code})
            Dim f1u = cdp(New Engine.Mirror.MirrorEvent With {.Kind = Engine.Mirror.MirrorKind.KeyUp, .Vk = &H70, .Key = kf.Key, .Code = kf.Code})
            Check("yansıtma: tuş JSON (harf yazıyla, Ctrl+harf yazısız, F1 yazısız, bırakma)",
                  keyA.Json.Contains("""type"":""keyDown""") AndAlso keyA.Json.Contains("""text"":""a""") AndAlso keyA.Json.Contains("""code"":""KeyA""") AndAlso
                  ctrlA.Json.Contains("""type"":""rawKeyDown""") AndAlso Not ctrlA.Json.Contains("""text""") AndAlso ctrlA.Json.Contains("""modifiers"":2") AndAlso
                  f1k.Json.Contains("""key"":""F1""") AndAlso f1k.Json.Contains("""type"":""rawKeyDown""") AndAlso f1k.Json.Contains("""windowsVirtualKeyCode"":112") AndAlso
                  f1u.Json.Contains("""type"":""keyUp"""),
                  keyA.Json)
        End Sub

        ' ------------------------------------------------------------------ adaptive pace + mirror mode

        Private Sub PaceTests()
            Dim p As New FollowPace()
            Dim t As Long = 0
            Dim fastWhileMoving = True
            For i = 0 To 40
                t += 120
                If p.NextDelay(New Point(1500 + i, 500), True, t) <> FollowPace.FastMs Then fastWhileMoving = False
            Next
            Check("tempo: lider yürürken hep hızlı tur", fastWhileMoving)
            Dim firstSlowAt As Long = -1
            Dim calmStart = t
            For i = 1 To 30
                t += 120
                If p.NextDelay(New Point(1540, 500), True, t) = FollowPace.SlowMs AndAlso firstSlowAt < 0 Then firstSlowAt = t - calmStart
            Next
            Check("tempo: lider durup herkes yanındayken ~1,5 sn sonra seyrek tur",
                  firstSlowAt >= FollowPace.CalmAfterMs AndAlso firstSlowAt <= FollowPace.CalmAfterMs + 240, $"{firstSlowAt} ms sonra")
            t += FollowPace.SlowMs
            Dim wake = p.NextDelay(New Point(1541, 500), True, t)
            Check("tempo: lider kıpırdayınca ilk turda hızlıya döner", wake = FollowPace.FastMs AndAlso Not p.IsSlow, CStr(wake))
            Dim q As New FollowPace()
            Dim walkingFast = True
            For i = 1 To 30
                If q.NextDelay(New Point(1540, 500), False, i * 120L) <> FollowPace.FastMs Then walkingFast = False
            Next
            Dim r As New FollowPace()
            Dim unreadFast = True
            For i = 1 To 30
                If r.NextDelay(Nothing, True, i * 120L) <> FollowPace.FastMs Then unreadFast = False
            Next
            Check("tempo: takipçi yürürken ya da lider okunamazken seyrekleşmez", walkingFast AndAlso unreadFast)
        End Sub

        ''' <summary>
        ''' Mirror mode switch with no sessions (nothing is sent anywhere). The hooks are real but only for a few ms and never
        ''' swallow input; messages are pumped between steps so the user's input is not held up.
        ''' </summary>
        Private Sub ModeTests()
            Dim m As New Engine.Mirror.InputMirror(New List(Of Core.Session.GameSession)(), Function() 0)
            Dim okDefault = m.Mode = Engine.Mirror.MirrorMode.KeyboardOnly
            Try
                m.Enabled = True
                Application.DoEvents()
                Dim kbOnly = m.Enabled AndAlso Not m.MouseHooked AndAlso Not FollowGate.MirrorActive
                m.Mode = Engine.Mirror.MirrorMode.KeyboardAndMouse
                Application.DoEvents()
                Dim both = m.MouseHooked AndAlso FollowGate.MirrorActive
                m.Mode = Engine.Mirror.MirrorMode.KeyboardOnly
                Application.DoEvents()
                Dim back = Not m.MouseHooked AndAlso Not FollowGate.MirrorActive AndAlso m.Enabled
                m.Mode = Engine.Mirror.MirrorMode.KeyboardAndMouse
                m.Enabled = False
                Application.DoEvents()
                Dim off = Not m.MouseHooked AndAlso Not FollowGate.MirrorActive AndAlso Not m.Enabled
                Check("yansıtma kipi: varsayılan yalnız klavye; fare kancası ve takip duraklatma yalnız 'klavye ve fare'de",
                      okDefault AndAlso kbOnly AndAlso both AndAlso back AndAlso off,
                      $"varsayılan {okDefault}, klavye {kbOnly}, ikisi {both}, geri {back}, kapalı {off}")
            Finally
                m.Dispose()
                FollowGate.MirrorActive = False
            End Try
        End Sub

        ' ------------------------------------------------------------------ 2. walking

        ''' <summary>A follower's virtual game: camera yaw, run speed, lag between a command and its effect, slide.</summary>
        Private NotInheritable Class SimWorld
            Implements IFollowInput

            Public X As Double, Y As Double
            Public Theta As Double
            Public SpeedPerMs As Double = 0.0035
            Public CmdLagMs As Integer = 60
            Public SlideMs As Integer = 200
            Public Size As Size
            Public CanvasRect As Rectangle      ' empty = the whole view
            Public DropPresses As Integer         ' first N presses are lost by the "game"
            Public ThetaChangeAt As Long = -1     ' the player turns the camera at this time …
            Public ThetaAfter As Double           ' … to this yaw
            Public Presses As Integer, HomeKeys As Integer
            Private _now As Long
            Private _pressed As Boolean
            Private _angle As Double
            Private _slideUntil As Long
            Private ReadOnly _pending As New List(Of (T As Long, Kind As Integer, Angle As Double))  ' 1 press/move, 0 release, 2 stop

            Public Function Canvas() As Rectangle Implements IFollowInput.Canvas
                Return If(CanvasRect.Width > 0, CanvasRect, New Rectangle(Point.Empty, Size))
            End Function

            Private Function AngleOf(x As Integer, y As Integer) As Double
                Dim c = FollowWalker.CircleCenter(Canvas())
                Return Math.Atan2(-(y - c.Y), x - c.X) * 180 / Math.PI
            End Function

            Public Function MoveAsync(x As Integer, y As Integer, pressed As Boolean) As Task Implements IFollowInput.MoveAsync
                If pressed Then _pending.Add((_now + CmdLagMs, 1, AngleOf(x, y)))
                Advance(2)
                Return Task.CompletedTask
            End Function

            Public Function PressAsync(x As Integer, y As Integer) As Task Implements IFollowInput.PressAsync
                Presses += 1
                If DropPresses > 0 Then
                    DropPresses -= 1
                Else
                    _pending.Add((_now + CmdLagMs, 1, AngleOf(x, y)))
                End If
                Advance(2)
                Return Task.CompletedTask
            End Function

            Public Function ReleaseAsync(x As Integer, y As Integer) As Task Implements IFollowInput.ReleaseAsync
                _pending.Add((_now + CmdLagMs, 0, 0))
                Advance(2)
                Return Task.CompletedTask
            End Function

            Public Function HoldKeyAsync(key As String, code As String, vk As Integer, ms As Integer) As Task Implements IFollowInput.HoldKeyAsync
                If vk = 36 Then HomeKeys += 1
                If vk = 83 Then _pending.Add((_now + CmdLagMs, 2, 0))
                Advance(ms)
                Return Task.CompletedTask
            End Function

            Public ReadOnly Property NowMs As Long Implements IFollowInput.NowMs
                Get
                    Return _now
                End Get
            End Property

            Public Function DelayAsync(ms As Integer) As Task Implements IFollowInput.DelayAsync
                Advance(ms)
                Return Task.CompletedTask
            End Function

            Public Sub Advance(ms As Integer)
                Dim endT = _now + ms
                While _now < endT
                    Dim stepMs = CInt(Math.Min(5, endT - _now))
                    _now += stepMs
                    If ThetaChangeAt >= 0 AndAlso _now >= ThetaChangeAt Then Theta = ThetaAfter : ThetaChangeAt = -1
                    For Each p In _pending.Where(Function(q) q.T <= _now).ToList()
                        _pending.Remove(p)
                        Select Case p.Kind
                            Case 1
                                If Not _pressed AndAlso p.Kind = 1 Then _pressed = True
                                _angle = p.Angle
                            Case 0
                                If _pressed Then _slideUntil = _now + SlideMs
                                _pressed = False
                            Case 2
                                _slideUntil = 0
                        End Select
                    Next
                    Dim v = 0.0
                    If _pressed Then
                        v = SpeedPerMs
                    ElseIf _now < _slideUntil Then
                        v = SpeedPerMs * 0.5
                    End If
                    If v > 0 Then
                        Dim w = (_angle - Theta) * Math.PI / 180   ' screenAngle = worldAngle + theta (mirror +1)
                        X += v * stepMs * Math.Cos(w)
                        Y += v * stepMs * Math.Sin(w)
                    End If
                End While
            End Sub

            Public ReadOnly Property Moving As Boolean
                Get
                    Return _pressed OrElse _now < _slideUntil
                End Get
            End Property

            Public Function Read() As Point
                Return New Point(CInt(Math.Round(X)), CInt(Math.Round(Y)))
            End Function
        End Class

        Private Structure SimResult
            Public FinalDist As Double
            Public ArriveMs As Long         ' first Arrived step after the leader stopped (-1 never)
            Public Kinds As HashSet(Of FollowKind)
            Public PressedAtEnd As Boolean
            Public MaxDistWhileLeaderMoved As Double
            Public ReleasedDuringGap As Boolean
            Public ThetaErr As Double
            Public Rounds As Integer
            Public World As SimWorld
            Public WalksAfterArrive As Integer
            Public DistAt8s As Double
            Public Nudges As Integer
        End Structure

        ''' <summary>
        ''' One follower vs a leader path. leaderAt(t) = leader's world point (Nothing = unreadable). gapFrom/gapTo = the
        ''' follower's own coordinate is unreadable in that window. Rounds of 120 ms (capture 35 ms of it).
        ''' </summary>
        Private Function Simulate(theta As Double, size As Size, startX As Double, startY As Double,
                                  leaderAt As Func(Of Long, PointF?), leaderStopsAt As Long, totalMs As Long,
                                  Optional gapFrom As Long = -1, Optional gapTo As Long = -1, Optional dropPresses As Integer = 0,
                                  Optional speed As Double = 0.0035, Optional cmdLag As Integer = 60,
                                  Optional thetaChangeAt As Long = -1, Optional thetaAfter As Double = 0,
                                  Optional canvas As Rectangle = Nothing) As SimResult
            Dim w As New SimWorld With {.X = startX, .Y = startY, .Theta = theta, .Size = size, .DropPresses = dropPresses, .SpeedPerMs = speed,
                                        .CmdLagMs = cmdLag, .ThetaChangeAt = thetaChangeAt, .ThetaAfter = thetaAfter, .CanvasRect = canvas}
            Dim walker As New FollowWalker(w)
            Dim fMe As New PosFilter(), fLead As New PosFilter()
            Dim res As New SimResult With {.Kinds = New HashSet(Of FollowKind), .ArriveMs = -1, .World = w}
            Dim lastRound As Long = 120
            Dim arrivedOnce = False
            While w.NowMs < totalMs
                Dim t0 = w.NowMs
                w.Advance(35)   ' coordinate crops of all views
                Dim lp = leaderAt(w.NowMs)
                Dim leadRaw As Point? = If(lp.HasValue, New Point(CInt(Math.Round(lp.Value.X)), CInt(Math.Round(lp.Value.Y))), CType(Nothing, Point?))
                Dim meRaw As Point? = If(w.NowMs >= gapFrom AndAlso w.NowMs < gapTo, CType(Nothing, Point?), w.Read())
                Dim mePos = fMe.Update(meRaw, w.NowMs)
                Dim target = fLead.Update(leadRaw, w.NowMs)
                walker.LagMs = lastRound + 80
                Dim st = walker.TickAsync(mePos, target).GetAwaiter().GetResult()
                res.Kinds.Add(st.Kind)
                If st.Kind = FollowKind.Nudge Then res.Nudges += 1
                If st.Kind = FollowKind.NoCoord AndAlso Not walker.Pressed Then res.ReleasedDuringGap = True
                Dim d = Double.NaN
                If lp.HasValue Then
                    d = Math.Sqrt((lp.Value.X - w.X) ^ 2 + (lp.Value.Y - w.Y) ^ 2)
                    If res.DistAt8s = 0 AndAlso w.NowMs >= 8000 Then res.DistAt8s = d
                    If w.NowMs < leaderStopsAt AndAlso w.NowMs > 8000 Then res.MaxDistWhileLeaderMoved = Math.Max(res.MaxDistWhileLeaderMoved, d)
                End If
                If st.Kind = FollowKind.Arrived AndAlso d <= 3.5 AndAlso w.NowMs >= leaderStopsAt AndAlso Not arrivedOnce Then
                    arrivedOnce = True : res.ArriveMs = w.NowMs - leaderStopsAt
                End If
                If arrivedOnce AndAlso (st.Kind = FollowKind.Walking OrElse st.Kind = FollowKind.Nudge) Then res.WalksAfterArrive += 1
                res.Rounds += 1
                Dim used = w.NowMs - t0
                If used < 120 Then w.Advance(CInt(120 - used))
                lastRound = Math.Max(120, used)
            End While
            Dim lf = leaderAt(w.NowMs)
            If lf.HasValue Then res.FinalDist = Math.Sqrt((lf.Value.X - w.X) ^ 2 + (lf.Value.Y - w.Y) ^ 2) Else res.FinalDist = Double.NaN
            res.PressedAtEnd = walker.Pressed
            res.ThetaErr = If(Double.IsNaN(walker.Theta), Double.NaN, Math.Abs(FollowWalker.Norm(walker.Theta - w.Theta)))
            Return res
        End Function

        Private Sub WalkTests()
            ' View → game canvas in it: the view itself, a smaller view, and the measured full-screen leader (1920×1032,
            ' canvas 1280×800 at 320,143).
            Dim views = {(New Size(1280, 800), Rectangle.Empty), (New Size(742, 735), Rectangle.Empty),
                         (New Size(1920, 1032), New Rectangle(320, 143, 1280, 800))}
            Dim thetas = {-146.0, 13.0, 90.0, 170.0, -60.0}

            ' A. Leader standing 40 units away, every camera yaw × every view size.
            Dim aOk = 0, aN = 0, worstArrive As Long = 0, worstDist = 0.0, worstTheta = 0.0
            For Each vw In views
                Dim sz = vw.Item1
                For Each th In thetas
                    Dim r = Simulate(th, sz, 1500, 500, Function(t) New PointF(1530, 527), 0, 30000, canvas:=vw.Item2)
                    aN += 1
                    Dim ok = r.FinalDist <= 3.0 AndAlso Not r.PressedAtEnd AndAlso r.ArriveMs >= 0 AndAlso r.ArriveMs <= 25000 AndAlso r.WalksAfterArrive = 0
                    If ok Then aOk += 1
                    worstArrive = Math.Max(worstArrive, r.ArriveMs) : worstDist = Math.Max(worstDist, r.FinalDist)
                    If Not Double.IsNaN(r.ThetaErr) Then worstTheta = Math.Max(worstTheta, r.ThetaErr)
                    If Not ok Then Line($"   A {sz.Width}×{sz.Height} kamera {th}°: uzaklık {r.FinalDist:0.0}, varış {r.ArriveMs} ms, basılı {r.PressedAtEnd}, varıştan sonra yürüme {r.WalksAfterArrive}")
                Next
            Next
            Check("A: duran lidere varır ve içinde durur (5 kamera × 3 görünüm: 1280×800, 742×735, tam ekran 1920×1032)", aOk = aN,
                  $"{aOk}/{aN}, en geç varış {worstArrive / 1000.0:0.0} sn, en büyük son uzaklık {worstDist:0.0}, kamera hatası ≤ {worstTheta:0}°")

            ' B. Leader walks an L at the follower's own speed (25 s) and stops: the gap must not grow while it walks.
            Dim path As Func(Of Long, PointF?) = Function(t)
                                                      Dim s = Math.Min(t, 25000L) * 0.0035
                                                      If s <= 40 Then Return New PointF(CSng(1500 + s), 500)
                                                      Return New PointF(1540, CSng(500 + (s - 40)))
                                                  End Function
            Dim bOk = 0, bN = 0, bWorstLag = 0.0, bWorstArrive As Long = 0
            For Each th In thetas
                Dim r = Simulate(th, New Size(1280, 800), 1490, 495, path, 25000, 40000)
                bN += 1
                Dim ok = r.FinalDist <= 3.0 AndAlso r.ArriveMs >= 0 AndAlso r.ArriveMs <= 6000 AndAlso r.MaxDistWhileLeaderMoved <= r.DistAt8s + 6
                If ok Then bOk += 1
                bWorstLag = Math.Max(bWorstLag, r.MaxDistWhileLeaderMoved) : bWorstArrive = Math.Max(bWorstArrive, r.ArriveMs)
                Line($"   B kamera {th}°: 8. sn'de {r.DistAt8s:0.0} birim geride, yürürken en çok {r.MaxDistWhileLeaderMoved:0.0}, durduktan sonra {r.ArriveMs} ms'de {r.FinalDist:0.0} birimde")
            Next
            Check("B: yürüyen lideri izler (ara açılmaz), durunca yanında durur", bOk = bN,
                  $"{bOk}/{bN}, yürürken en çok {bWorstLag:0.0} birim geride, durduktan sonra ≤ {bWorstArrive / 1000.0:0.0} sn")

            ' C. Leader teleports 120 units at 15 s → camera again (HOME), then arrives.
            Dim c = Simulate(13, New Size(1280, 800), 1500, 500,
                             Function(t) If(t < 15000, New PointF(1503, 502), New PointF(1600, 570)), 15000, 70000)
            Check("C: lider ışınlanınca yeniden yetişir", c.FinalDist <= 3.0 AndAlso c.ArriveMs >= 0,
                  $"son uzaklık {c.FinalDist:0.0}, ışınlanmadan sonra varış {c.ArriveMs / 1000.0:0.0} sn")
            ' C2. The follower itself teleports (town) → HOME again.
            Dim w2 As New SimWorld With {.X = 1500, .Y = 500, .Theta = 13, .Size = New Size(1280, 800)}
            Dim wk2 As New FollowWalker(w2)
            Dim homeBefore = 0
            For i = 1 To 60
                If i = 30 Then homeBefore = w2.HomeKeys : w2.X = 1400 : w2.Y = 420
                wk2.TickAsync(w2.Read(), New Point(CInt(w2.X) + 1, CInt(w2.Y))).GetAwaiter().GetResult()
                w2.Advance(120)
            Next
            Check("C2: takipçi ışınlanınca kamera yeniden tepeye alınır", w2.HomeKeys >= homeBefore + 2, $"HOME {homeBefore} → {w2.HomeKeys}")

            ' D. Leader out of range (300 units) → doesn't follow, button not held.
            Dim d = Simulate(13, New Size(1280, 800), 1500, 500, Function(t) New PointF(1800, 500), 0, 10000)
            Check("D: çok uzak liderde yürümez, fare bırakılır", d.Kinds.Contains(FollowKind.TooFar) AndAlso Not d.PressedAtEnd AndAlso
                  Not d.Kinds.Contains(FollowKind.Walking) AndAlso Math.Abs(d.World.X - 1500) < 3, $"son konum {d.World.X:0},{d.World.Y:0}")

            ' E. Own coordinate unreadable for 2.5 s mid-walk → lets go, then resumes and arrives.
            Dim e = Simulate(90, New Size(1280, 800), 1440, 470, Function(t) New PointF(1520, 520), 0, 40000, gapFrom:=6000, gapTo:=8500)
            Check("E: koordinat okunamayınca bırakır, okununca sürer", e.Kinds.Contains(FollowKind.NoCoord) AndAlso e.ReleasedDuringGap AndAlso e.FinalDist <= 3.0,
                  $"son uzaklık {e.FinalDist:0.0}")

            ' F. The game drops the first two presses → stuck watchdog / probe retry presses again and still arrives.
            Dim f = Simulate(-146, New Size(1280, 800), 1500, 500, Function(t) New PointF(1530, 520), 0, 40000, dropPresses:=2)
            Check("F: oyun basışı yutarsa yeniden basar ve varır", f.FinalDist <= 3.0 AndAlso f.World.Presses >= 3,
                  $"son uzaklık {f.FinalDist:0.0}, {f.World.Presses} basış")

            ' G. Faster character (twice the speed) still stops inside, no back-and-forth.
            Dim gOk = 0
            For Each th In thetas
                Dim r = Simulate(th, New Size(1280, 800), 1500, 500, Function(t) New PointF(1560, 540), 0, 30000, speed:=0.007)
                If r.FinalDist <= 3.0 AndAlso r.WalksAfterArrive = 0 AndAlso r.ArriveMs >= 0 Then gOk += 1 Else Line($"   G kamera {th}°: son uzaklık {r.FinalDist:0.0}, varış {r.ArriveMs}, varıştan sonra yürüme {r.WalksAfterArrive}")
            Next
            Check("G: iki kat hızlı karakter de liderin içinde durur", gOk = thetas.Length, $"{gOk}/{thetas.Length}")

            ' I. The camera turns 50° while the follower walks a long way (180 units): the closed loop must re-learn it.
            Dim iOk = 0
            For Each th In thetas
                Dim r = Simulate(th, New Size(1280, 800), 1500, 500, Function(t) New PointF(1650, 600), 0, 90000,
                                 thetaChangeAt:=12000, thetaAfter:=th + 50)
                If r.FinalDist <= 3.0 AndAlso r.ThetaErr <= 12 Then iOk += 1 Else Line($"   I kamera {th}° → {th + 50}°: son uzaklık {r.FinalDist:0.0}, kamera hatası {r.ThetaErr:0}°")
            Next
            Check("I: kamera yürürken dönerse yönünü yeniden öğrenir", iOk = thetas.Length, $"{iOk}/{thetas.Length}")

            ' J. Slow game (250 ms from command to effect) and a fast character: stopping early must still land inside
            '    without a string of short steps (back and forth).
            Dim jOk = 0, jMaxNudge = 0
            For Each th In thetas
                Dim r = Simulate(th, New Size(1280, 800), 1500, 500, Function(t) New PointF(1560, 540), 0, 30000, speed:=0.007, cmdLag:=250)
                jMaxNudge = Math.Max(jMaxNudge, r.Nudges)
                If r.FinalDist <= 3.0 AndAlso r.WalksAfterArrive = 0 AndAlso r.Nudges <= 1 Then jOk += 1 Else Line($"   J kamera {th}°: son uzaklık {r.FinalDist:0.0}, kısa adım {r.Nudges}, varıştan sonra yürüme {r.WalksAfterArrive}")
            Next
            Check("J: yavaş oyunda da erken bırakıp içinde durur (en çok 1 kısa adım)", jOk = thetas.Length, $"{jOk}/{thetas.Length}, en çok {jMaxNudge} kısa adım")

            ' H. Cursor circle stays inside the view for every size (the press must land on the game, not off-view).
            Dim hOk = True
            For Each vw In views
                Dim cv = If(vw.Item2.Width > 0, vw.Item2, New Rectangle(Point.Empty, vw.Item1))
                Dim txt = WebCoordReader.RegionIn(cv)
                For a = 0 To 359 Step 15
                    Dim p = FollowWalker.CirclePoint(cv, a)
                    If Not cv.Contains(p) Then hOk = False
                    If txt.Contains(p) Then hOk = False
                Next
                Dim cc = FollowWalker.CircleCenter(cv)
                If Math.Abs(cc.X - (cv.X + cv.Width / 2)) > 1 Then hOk = False
            Next
            Check("H: imleç çemberi oyun tuvalinin içinde ve ortasında, koordinat yazısının dışında (tam ekran dahil)", hOk)
        End Sub

    End Module

End Namespace
