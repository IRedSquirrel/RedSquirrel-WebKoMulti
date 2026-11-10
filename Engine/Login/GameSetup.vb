Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports Newtonsoft.Json.Linq
Imports RedSquirrel.WebKoMulti.Core.Input
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Core.Vision
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Engine.Login

    ''' <summary>One read of the game's Option window (+ its opener button). Positions in VIEW pixels. Side-effect free to build.</summary>
    Public Class OptionRead
        ''' <summary>The yellow-haired face button on the bottom bar (opens Option); Empty = not seen.</summary>
        Public Face As Point
        Public WindowOpen As Boolean
        Public GraphicsTab As Point, DisplayTab As Point, LowButton As Point, Apply As Point, CloseX As Point
        ''' <summary>"Low (Patates) Graphics" visible = the Graphics page is shown.</summary>
        Public LowVisible As Boolean
        Public ReadOnly Boxes As New List(Of OptionBox)()
        ''' <summary>Display page values read in the window's font; -1 = not readable (text like "Off", unmeasured digit 4/5/9).</summary>
        Public Camera As Integer = -1, FrameLimit As Integer = -1
        ''' <summary>Raw reads ('?' = unknown glyph) — for the log.</summary>
        Public CameraText As String = "", FrameLimitText As String = ""
        Public CameraLeft As Point, CameraRight As Point, FrameLeft As Point, FrameRight As Point
        ''' <summary>"N ayar uygulandı." under the buttons (shows after Apply).</summary>
        Public AppliedSeen As Boolean

        Public Overrides Function ToString() As String
            Return $"simge {If(Face.IsEmpty, "yok", $"{Face.X},{Face.Y}")} · Option {If(WindowOpen, "açık", "kapalı")}" &
                   If(WindowOpen, $" · Low {If(LowVisible, "görünüyor", "yok")} · " & String.Join(", ", Boxes.Select(Function(b) b.ToString())) &
                      $" · kamera '{CameraText}' · kare sınırı '{FrameLimitText}'{If(AppliedSeen, " · uygulandı yazısı", "")}", "")
        End Function
    End Class

    Public Class OptionBox
        Public Name As String
        Public Found As Boolean
        Public Checked As Boolean
        Public Box As Point
        ''' <summary>Best label match (for the log when not found).</summary>
        Public Score As Double

        Public Overrides Function ToString() As String
            Return If(Found, $"{Name} {If(Checked, "işaretli", "kapalı")}", $"{Name} yok ({Score:0.00})")
        End Function
    End Class

    ''' <summary>
    ''' ONE-TIME game settings per account (user: "sol alttan çocuğa tıklayıp Grafik'e tıklayın, Low (Patates) Graphics tıklamalı.
    ''' Display menüsünden ise Play Intro movies, VRR, Browser power saving kapanmalı"). Runs after AfterEnter. Every click is
    ''' checked on a new frame; a step that does not take stops the setup (window closed with its X if open — never ESC),
    ''' the log says why, the login stays good. Success is remembered in ..\Settings\multi-gamesetup.json under SHA-256(ID)
    ''' (never the ID itself) and the setup is skipped on later logins.
    ''' Window geometry from the user's crops (1:1 with the 1280-wide canvas): every button is an offset from the "Option" title.
    ''' Not measured live: the window's real position, whether Apply asks something, what Low (Patates) changes.
    ''' </summary>
    Public Module GameSetup

        Friend Const MinScore As Double = 0.8
        ''' <summary>Display labels are searched only in their own narrow column; at a 0,75 view "VRR mode" blurs to 0,74.</summary>
        Friend Const LabelMinScore As Double = 0.7
        ' offsets from the OptionTitle template's top-left (option-graphics.png: title corner 240,3)
        Private ReadOnly OffGraphicsTab As New Point(73 - 240, 218 - 3)
        Private ReadOnly OffDisplayTab As New Point(73 - 240, 170 - 3)
        Private ReadOnly OffLowButton As New Point(221 - 240, 69 - 3)
        Private ReadOnly OffLowTemplate As New Point(152 - 240, 57 - 3)
        Private ReadOnly OffApply As New Point(485 - 240, 562 - 3)
        Private ReadOnly OffClose As New Point(604 - 240, 18 - 3)
        ''' <summary>Display-page labels: corner x = title corner - 69 (MEASURED on option-display-fps30-kamera16.png: title corner
        ''' 250,24, "Play intro movies" 181,240). The first value (-82) came from assuming the older crop shared the graphics crop's
        ''' x — it did not (13 px off); the search column would have missed every label live.</summary>
        Private Const LabelColumnX As Integer = 181 - 250
        ' Display page rows (same crop, title corner 250,24): Camera Range ◄ 337,88 · ► 530,88 · value 545..598 × 78..99;
        ' Frame rate limit ◄ 337,195 · ► 584,195 · value 352..570 × 184..206; "ayar uygulandı." 268,610.
        Private ReadOnly OffCameraLeft As New Point(337 - 250, 88 - 24)
        Private ReadOnly OffCameraRight As New Point(530 - 250, 88 - 24)
        Private ReadOnly CameraValueArea As New Rectangle(545 - 250, 78 - 24, 53, 21)
        Private ReadOnly OffFrameLeft As New Point(337 - 250, 195 - 24)
        Private ReadOnly OffFrameRight As New Point(584 - 250, 195 - 24)
        Private ReadOnly FrameValueArea As New Rectangle(352 - 250, 184 - 24, 218, 22)
        Private ReadOnly OffApplied As New Point(268 - 250, 610 - 24)
        Public Const TargetFrameLimit As Integer = 30
        Public Const TargetCamera As Integer = 16
        ''' <summary>Bump when the settings change: older "done" marks do not skip the new settings (v2: + frame limit 30, camera 16).</summary>
        Public Const SetupVersion As Integer = 2
        ''' <summary>Checkbox centre = label template corner + (-11, +8) (option-display.png).</summary>
        Private ReadOnly OffBoxFromLabel As New Point(-11, 8)

        ' ---------------- run ----------------

        Public Async Function RunAsync(session As GameSession, ct As CancellationToken) As Task(Of Boolean)
            Dim n = session.Number
            Dim key = AccountKey(session.Account?.UserId)
            If key.Length = 0 Then Return False
            If IsDone(key) Then
                BotLog.Add("giris", $"{n}. görünüm: oyun ayarı daha önce uygulanmış — atlandı")
                Return True
            End If
            Dim cw = session.Cw
            Dim stepName = "başlangıç"
            Try
                session.SetStatus(SessionState.InGame, "Oyun ayarı uygulanıyor")
                Dim r = Await ReadAsync(cw)
                If r.WindowOpen Then
                    stepName = "açık pencere"
                Else
                    stepName = "Option simgesi"
                    If r.Face.IsEmpty Then Return Stop_(n, "alt çubukta Option simgesi (sarı saçlı yüz) bulunamadı", Nothing)
                    Await HoverClick(cw, r.Face)
                    ' ≥ 2 looks, 6 s: the 3rd live run looked ONCE (the window was still opening) and said "açılmadı" while it opened
                    r = Await WaitAsync(cw, Function(x) x.WindowOpen, 6000, ct)
                    If r Is Nothing Then Return Await StopAndClose(cw, n, "simgeye tıklandı ama Option penceresi görülmedi")
                End If

                stepName = "Graphics sekmesi"
                Await HoverClick(cw, r.GraphicsTab)
                r = Await WaitAsync(cw, Function(x) x.WindowOpen AndAlso x.LowVisible, 4000, ct)
                If r Is Nothing Then Return Await StopAndClose(cw, n, "Graphics sekmesi açılmadı")

                stepName = "Low (Patates) Graphics"
                Await HoverClick(cw, r.LowButton)
                Await Task.Delay(600, ct)

                stepName = "Display sekmesi"
                r = Await ReadAsync(cw)
                If Not r.WindowOpen Then Return Stop_(n, "Low (Patates) sonrası Option penceresi kayboldu", Nothing)
                Await HoverClick(cw, r.DisplayTab)
                r = Await WaitAsync(cw, Function(x) x.WindowOpen AndAlso x.Boxes.All(Function(b) b.Found), 4000, ct, boxes:=True)
                If r Is Nothing Then Return Await StopAndClose(cw, n, "Display sekmesi açılmadı (kutular görünmedi)")

                For Each name In {"Play intro movies", "VRR mode", "Browser power saving"}
                    stepName = name
                    Dim b = r.Boxes.First(Function(x) x.Name = name)
                    If Not b.Checked Then Continue For
                    Dim nm = name
                    Dim cleared As OptionRead = Nothing
                    For attempt = 1 To 2
                        Await HoverClick(cw, b.Box)
                        cleared = Await WaitAsync(cw, Function(x) x.Boxes.Any(Function(y) y.Name = nm AndAlso y.Found AndAlso Not y.Checked), 3000, ct, boxes:=True)
                        If cleared IsNot Nothing Then Exit For
                    Next
                    If cleared Is Nothing Then Return Await StopAndClose(cw, n, $"'{name}' kutusu kapanmadı")
                    r = cleared
                Next

                ' frame rate limit 30, camera range 16 — read, step with ◄ / ►, read again after every press
                stepName = "kare sınırı"
                Dim fpsNote = Await StepValueAsync(cw, n, "Kare sınırı", TargetFrameLimit, Function(x) x.FrameLimit, Function(x) x.FrameLimitText,
                                                   Function(x) x.FrameLeft, Function(x) x.FrameRight, ct)
                stepName = "kamera"
                Dim camNote = Await StepValueAsync(cw, n, "Kamera", TargetCamera, Function(x) x.Camera, Function(x) x.CameraText,
                                                   Function(x) x.CameraLeft, Function(x) x.CameraRight, ct)
                r = Await ReadAsync(cw, boxes:=True)

                stepName = "Apply"
                Await HoverClick(cw, r.Apply)
                Dim applied = Await WaitAsync(cw, Function(x) x.AppliedSeen, 3000, ct, boxes:=True)
                r = Await ReadAsync(cw)
                stepName = "kapatma (X)"
                If r.WindowOpen Then
                    Await HoverClick(cw, r.CloseX)
                    Dim gone = Await WaitAsync(cw, Function(x) Not x.WindowOpen, 3000, ct)
                    If gone Is Nothing Then BotLog.Warn("giris", $"{n}. görünüm: oyun ayarı uygulandı ama Option penceresi X ile kapanmadı")
                End If
                Dim summary = $"patates grafik, intro / VRR / güç tasarrufu kapalı, {fpsNote}, {camNote}"
                If applied Is Nothing Then
                    ' not marked → tried again next login
                    BotLog.Warn("giris", $"{n}. görünüm: Apply'dan sonra 'ayar uygulandı' yazısı görülmedi — ayar işaretlenmedi, sonraki girişte yeniden ({summary})")
                    Return False
                End If
                If fpsNote <> $"kare sınırı {TargetFrameLimit}" OrElse camNote <> $"kamera {TargetCamera}" Then
                    BotLog.Warn("giris", $"{n}. görünüm: oyun ayarı kısmen uygulandı ({summary}) — işaretlenmedi, sonraki girişte yeniden")
                    Return False
                End If
                MarkDone(key)
                BotLog.Add("giris", $"{n}. görünüm: oyun ayarı uygulandı ({summary})")
                Return True
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                BotLog.Fail("giris", $"{n}. görünüm: oyun ayarı hatası ({stepName})", ex)
                Return False
            Finally
                session.SetStatus(SessionState.InGame, "Oyunda")
            End Try
        End Function

        Private Function Stop_(n As Integer, why As String, dummy As Object) As Boolean
            BotLog.Warn("giris", $"{n}. görünüm: oyun ayarı yarıda bırakıldı — {why}")
            Return False
        End Function

        Private Async Function StopAndClose(cw As Microsoft.Web.WebView2.Core.CoreWebView2, n As Integer, why As String) As Task(Of Boolean)
            ' the window WE opened must not be left open (3rd live run: left on the Game tab) — its own X, never ESC; looked twice
            For i = 1 To 2
                Dim r = Await ReadAsync(cw)
                If r.WindowOpen Then
                    Await HoverClick(cw, r.CloseX)
                    Await Task.Delay(800)
                    Dim again = Await ReadAsync(cw)
                    BotLog.Add("giris", $"{n}. görünüm: yarıda kalan Option penceresi X ile {If(again.WindowOpen, "KAPANMADI", "kapatıldı")}")
                    Exit For
                End If
                Await Task.Delay(800)
            Next
            Return Stop_(n, why, Nothing)
        End Function

        ''' <summary>
        ''' One value to a target with ◄ / ►: read → press toward the target → read again; at most 24 presses. Stops (value left as it
        ''' is, reason returned) when it cannot be read, does not move after two presses, or jumps OVER the target (the value list
        ''' does not contain it). Returns "kare sınırı 30" on success, otherwise "kare sınırı: &lt;why&gt;".
        ''' </summary>
        Private Async Function StepValueAsync(cw As Microsoft.Web.WebView2.Core.CoreWebView2, n As Integer, label As String, target As Integer,
                                              value As Func(Of OptionRead, Integer), text As Func(Of OptionRead, String),
                                              leftOf As Func(Of OptionRead, Point), rightOf As Func(Of OptionRead, Point),
                                              ct As CancellationToken) As Task(Of String)
            Dim name = label.ToLowerInvariant()
            Dim r = Await ReadAsync(cw, boxes:=True)
            Dim v = value(r)
            If v < 0 Then
                BotLog.Warn("giris", $"{n}. görünüm: {name} okunamadı ('{text(r)}') — dokunulmadı")
                Return $"{name}: okunamadı"
            End If
            Dim start = v, stuck = 0
            For press = 1 To 24
                If v = target Then Exit For
                Dim up = v < target
                Await HoverClick(cw, If(up, rightOf(r), leftOf(r)))
                Await Task.Delay(350, ct)
                r = Await ReadAsync(cw, boxes:=True)
                Dim v2 = value(r)
                If v2 < 0 Then
                    BotLog.Warn("giris", $"{n}. görünüm: {name} basıştan sonra okunamadı ('{text(r)}', önceki {v})")
                    Return $"{name}: okunamadı"
                End If
                If v2 = v Then
                    stuck += 1
                    If stuck >= 2 Then Return $"{name}: {v}'de kaldı (ok değiştirmiyor)"
                    Continue For
                End If
                stuck = 0
                If (up AndAlso v2 > target) OrElse (Not up AndAlso v2 < target) Then
                    BotLog.Warn("giris", $"{n}. görünüm: {name} {v} → {v2}: {target} değer listesinde yok — bırakıldı")
                    Return $"{name}: {target} yok ({v2})"
                End If
                v = v2
            Next
            If v <> target Then Return $"{name}: {v}"
            If start <> target Then BotLog.Add("giris", $"{n}. görünüm: {name} {start} → {target}")
            Return $"{name} {target}"
        End Function

        Private Async Function HoverClick(cw As Microsoft.Web.WebView2.Core.CoreWebView2, p As Point) As Task
            Await CdpInput.Mouse(cw, "mouseMoved", p.X, p.Y)
            Await Task.Delay(200)
            Await CdpInput.ClickAsync(cw, p.X, p.Y)
        End Function

        Private Async Function ReadAsync(cw As Microsoft.Web.WebView2.Core.CoreWebView2, Optional boxes As Boolean = False) As Task(Of OptionRead)
            Using f = Await ViewCapture.FullAsync(cw)
                If f Is Nothing Then Return New OptionRead()
                Return Await Task.Run(Function() Read(f, boxes))
            End Using
        End Function

        Private Async Function WaitAsync(cw As Microsoft.Web.WebView2.Core.CoreWebView2, ok As Func(Of OptionRead, Boolean), timeoutMs As Integer,
                                         ct As CancellationToken, Optional boxes As Boolean = False) As Task(Of OptionRead)
            ' at least TWO looks whatever the timeout (one slow read must not use the whole wait up)
            Dim sw = Stopwatch.StartNew()
            Dim looks = 0
            While sw.ElapsedMilliseconds < timeoutMs OrElse looks < 2
                Await Task.Delay(400, ct)
                Dim r = Await ReadAsync(cw, boxes)
                looks += 1
                If ok(r) Then Return r
            End While
            Return Nothing
        End Function

        ' ---------------- reading (side-effect free) ----------------

        ''' <summary><paramref name="boxes"/> = also search the three Display labels (the slow part — only when needed).</summary>
        Public Function Read(frame As Bitmap, Optional boxes As Boolean = True) As OptionRead
            Dim fw = frame.Width, fh = frame.Height
            Dim px = LoginScreens.ReadPixels(frame)
            Dim cr = LoginScreens.CanvasRect(px, fw, fh)
            Dim lum = AfterEnter.Luma(px)
            Dim s = cr.Width / 1280.0
            Dim canvasH = CInt(cr.Height / s)
            Dim view = Function(p As Point) New Point(cr.X + CInt(p.X * s), cr.Y + CInt(p.Y * s))
            Dim r As New OptionRead()

            Dim face = AfterEnter.Locate(lum, fw, fh, cr, s, GameSetupTemplates.FaceIcon, New Rectangle(0, Math.Max(0, canvasH - 110), 440, 110), 2)
            If face.V >= MinScore Then r.Face = view(New Point(face.P.X + GameSetupTemplates.FaceIcon.W \ 2, face.P.Y + GameSetupTemplates.FaceIcon.H \ 2))

            Dim title = AfterEnter.Locate(lum, fw, fh, cr, s, GameSetupTemplates.OptionTitle, New Rectangle(0, 0, 1280, canvasH), 3)
            If title.V < MinScore Then Return r
            r.WindowOpen = True
            Dim t = title.P
            Dim at = Function(o As Point) view(New Point(t.X + o.X, t.Y + o.Y))
            r.GraphicsTab = at(OffGraphicsTab) : r.DisplayTab = at(OffDisplayTab) : r.LowButton = at(OffLowButton)
            r.Apply = at(OffApply) : r.CloseX = at(OffClose)
            Dim low = AfterEnter.Locate(lum, fw, fh, cr, s, GameSetupTemplates.LowButton,
                                        New Rectangle(t.X + OffLowTemplate.X - 8, t.Y + OffLowTemplate.Y - 8, GameSetupTemplates.LowButton.W + 16, GameSetupTemplates.LowButton.H + 16), 1)
            r.LowVisible = low.V >= MinScore
            If Not boxes Then Return r
            ' Display-page values + arrows + the "ayar uygulandı" line (cheap: small areas)
            r.CameraLeft = at(OffCameraLeft) : r.CameraRight = at(OffCameraRight)
            r.FrameLeft = at(OffFrameLeft) : r.FrameRight = at(OffFrameRight)
            r.CameraText = ReadDigits(lum, fw, fh, cr, s, New Rectangle(t.X + CameraValueArea.X, t.Y + CameraValueArea.Y, CameraValueArea.Width, CameraValueArea.Height))
            r.FrameLimitText = ReadDigits(lum, fw, fh, cr, s, New Rectangle(t.X + FrameValueArea.X, t.Y + FrameValueArea.Y, FrameValueArea.Width, FrameValueArea.Height))
            r.Camera = ToNumber(r.CameraText) : r.FrameLimit = ToNumber(r.FrameLimitText)
            Dim at2 = GameSetupDigits.AppliedText
            Dim ap = AfterEnter.Locate(lum, fw, fh, cr, s, at2, New Rectangle(t.X + OffApplied.X - 30, t.Y + OffApplied.Y - 8, at2.W + 60, at2.H + 16), 1)
            r.AppliedSeen = ap.V >= MinScore
            ' labels sit in the content's left column: label corner x = title corner - 82 (both crops: tabs at the same x)
            Dim area As New Rectangle(t.X + LabelColumnX - 16, t.Y + 40, 32, 560)
            For Each pair In {("Play intro movies", GameSetupTemplates.IntroLabel), ("VRR mode", GameSetupTemplates.VrrLabel),
                              ("Browser power saving", GameSetupTemplates.PowerLabel)}
                Dim b As New OptionBox With {.Name = pair.Item1}
                Dim m = AfterEnter.Locate(lum, fw, fh, cr, s, pair.Item2, New Rectangle(area.X, area.Y, area.Width + pair.Item2.W, area.Height + pair.Item2.H), 1)
                b.Score = m.V
                If m.V >= LabelMinScore Then
                    b.Found = True
                    b.Box = view(New Point(m.P.X + OffBoxFromLabel.X, m.P.Y + OffBoxFromLabel.Y))
                    b.Checked = TickPixels(px, fw, fh, b.Box, Math.Max(4, CInt(7 * s))) >= 5
                End If
                r.Boxes.Add(b)
            Next
            Return r
        End Function

        ''' <summary>
        ''' Digits in an area of the Option window (canvas-1280 px). The area is resampled to canvas scale (bilinear when the view
        ''' is scaled); text = columns with a pixel brighter than 90 (measured: glyph bodies 155-180 on a ~0-4 background, gaps clean);
        ''' each column group compared to the measured glyphs (mean abs diff ≤ 45 and 10 better than the second best).
        ''' "" = no text; '?' = a glyph we have not measured (4 5 9) or a letter. Side-effect free.
        ''' </summary>
        Friend Function ReadDigits(lum() As Byte, fw As Integer, fh As Integer, cr As Rectangle, s As Double, area As Rectangle) As String
            Dim w = area.Width, h = area.Height
            Dim g(w * h - 1) As Double
            For y = 0 To h - 1
                For x = 0 To w - 1
                    Dim fx = cr.X + (area.X + x) * s, fy = cr.Y + (area.Y + y) * s
                    Dim x0 = CInt(Math.Floor(fx)), y0 = CInt(Math.Floor(fy))
                    If x0 < 0 OrElse y0 < 0 OrElse x0 + 1 >= fw OrElse y0 + 1 >= fh Then Continue For
                    Dim ax = fx - x0, ay = fy - y0
                    g(y * w + x) = (1 - ay) * ((1 - ax) * lum(y0 * fw + x0) + ax * lum(y0 * fw + x0 + 1)) +
                                   ay * ((1 - ax) * lum((y0 + 1) * fw + x0) + ax * lum((y0 + 1) * fw + x0 + 1))
                Next
            Next
            Dim top = -1, bottom = -1
            For y = 0 To h - 1
                For x = 0 To w - 1
                    If g(y * w + x) > 90 Then
                        If top < 0 Then top = y
                        bottom = y
                        Exit For
                    End If
                Next
            Next
            If top < 0 Then Return ""
            If bottom - top + 1 > GameSetupDigits.GlyphH + 2 Then Return "?"
            Dim sb As New Text.StringBuilder()
            Dim xStart = -1
            For x = 0 To w
                Dim lit = False
                If x < w Then
                    For y = top To bottom
                        If g(y * w + x) > 90 Then lit = True : Exit For
                    Next
                End If
                If lit AndAlso xStart < 0 Then xStart = x
                If Not lit AndAlso xStart >= 0 Then
                    sb.Append(Glyph(g, w, h, top, xStart, x - 1))
                    xStart = -1
                End If
            Next
            Return sb.ToString()
        End Function

        Private Function Glyph(g() As Double, w As Integer, h As Integer, top As Integer, x0 As Integer, x1 As Integer) As Char
            Dim gw = GameSetupDigits.GlyphW, gh = GameSetupDigits.GlyphH
            If x1 - x0 + 1 < 2 OrElse x1 - x0 + 1 > gw Then Return "?"c
            Dim best = Double.MaxValue, second = Double.MaxValue, bestCh = "?"c
            For Each kv In GameSetupDigits.Glyphs
                Dim d As Double = 0
                For y = 0 To gh - 1
                    For x = 0 To gw - 1
                        Dim gx = x0 + x, gy = top + y
                        Dim v = If(gx <= x1 AndAlso gy < h, g(gy * w + gx), 0.0)
                        d += Math.Abs(v - kv.Value(y * gw + x))
                    Next
                Next
                d /= gw * gh
                If d < best Then
                    second = best : best = d : bestCh = kv.Key
                ElseIf d < second Then
                    second = d
                End If
            Next
            If best > 45 OrElse second - best < 10 Then Return "?"c
            Return bestCh
        End Function

        Friend Function ToNumber(t As String) As Integer
            If String.IsNullOrEmpty(t) OrElse t.Length > 4 OrElse Not t.All(Function(c) Char.IsDigit(c)) Then Return -1
            Return Integer.Parse(t)
        End Function

        ''' <summary>Cyan tick pixels in the checkbox (option-display.png: checked 14, empty 0).</summary>
        Friend Function TickPixels(px() As Integer, fw As Integer, fh As Integer, c As Point, rad As Integer) As Integer
            Dim n = 0
            For y = Math.Max(0, c.Y - rad) To Math.Min(fh - 1, c.Y + rad)
                For x = Math.Max(0, c.X - rad) To Math.Min(fw - 1, c.X + rad)
                    Dim v = px(y * fw + x)
                    Dim r = (v >> 16) And &HFF, g = (v >> 8) And &HFF, b = v And &HFF
                    If g >= 100 AndAlso b >= 100 AndAlso r <= g - 50 Then n += 1
                Next
            Next
            Return n
        End Function

        ' ---------------- once per account ----------------

        Friend PathOverride As String = Nothing
        Private ReadOnly _lock As New Object()

        Public Function StorePath() As String
            If Not String.IsNullOrEmpty(PathOverride) Then Return PathOverride
            Return Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Settings", "multi-gamesetup.json"))
        End Function

        ''' <summary>SHA-256 of the lower-cased ID, hex. "" for an empty ID. The ID itself is never stored.</summary>
        Public Function AccountKey(userId As String) As String
            If String.IsNullOrWhiteSpace(userId) Then Return ""
            Using sha = SHA256.Create()
                Dim h = sha.ComputeHash(Encoding.UTF8.GetBytes(userId.Trim().ToLowerInvariant()))
                Return String.Concat(h.Select(Function(b) b.ToString("x2")))
            End Using
        End Function

        ''' <summary>Store key = "v<SetupVersion>:" + SHA-256(ID): a mark from an older settings set does not count.</summary>
        Private Function Versioned(key As String) As String
            Return $"v{SetupVersion}:{key}"
        End Function

        Public Function IsDone(key As String) As Boolean
            SyncLock _lock
                Dim o = LoadStore()
                Return o.GetValue(Versioned(key)) IsNot Nothing
            End SyncLock
        End Function

        Public Sub MarkDone(key As String)
            SetDone(key, True)
        End Sub

        ''' <summary>For a "Oyun ayarlarını yeniden uygula" button: forget the mark → applied again on the next login.</summary>
        Public Sub Forget(userId As String)
            SetDone(AccountKey(userId), False)
            BotLog.Add("giris", "Oyun ayarı bir sonraki girişte yeniden uygulanacak")
        End Sub

        Private Sub SetDone(key As String, done As Boolean)
            If key.Length = 0 Then Return
            SyncLock _lock
                Try
                    Dim o = LoadStore()
                    If done Then o(Versioned(key)) = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") Else o.Remove(Versioned(key))
                    Dim file = StorePath()
                    Directory.CreateDirectory(Path.GetDirectoryName(file))
                    Dim tmp = file & ".tmp"
                    IO.File.WriteAllText(tmp, New JObject From {{"done", o}}.ToString(), New UTF8Encoding(False))
                    If IO.File.Exists(file) Then IO.File.Replace(tmp, file, Nothing) Else IO.File.Move(tmp, file)
                Catch ex As Exception
                    BotLog.Fail("giris", "Oyun ayarı işareti kaydedilemedi", ex)
                End Try
            End SyncLock
        End Sub

        Private Function LoadStore() As JObject
            Try
                Dim file = StorePath()
                If Not IO.File.Exists(file) Then Return New JObject()
                Dim d = TryCast(JObject.Parse(IO.File.ReadAllText(file)).GetValue("done", StringComparison.OrdinalIgnoreCase), JObject)
                Return If(d, New JObject())
            Catch
                Return New JObject()
            End Try
        End Function

    End Module

End Namespace
