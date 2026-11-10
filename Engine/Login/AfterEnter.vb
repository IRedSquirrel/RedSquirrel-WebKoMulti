Imports System.Threading
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Input
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Core.Vision
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Engine.Login

    ''' <summary>A grayscale template in CANVAS-1280 pixels (the game draws its UI for a 1280-wide canvas; scaled views scale it).</summary>
    Friend NotInheritable Class GrayTemplate
        Public ReadOnly W As Integer, H As Integer
        ''' <summary>Where it was cut (canvas-1280): the expected place.</summary>
        Public ReadOnly X0 As Integer, Y0 As Integer
        Public ReadOnly Data As Byte()
        Public ReadOnly Mean As Double, Norm As Double

        Public Sub New(w As Integer, h As Integer, x0 As Integer, y0 As Integer, base64 As String)
            Me.W = w : Me.H = h : Me.X0 = x0 : Me.Y0 = y0
            Data = Convert.FromBase64String(base64)
            Mean = Data.Average(Function(b) CDbl(b))
            Norm = Math.Sqrt(Data.Sum(Function(b) (b - Mean) * (b - Mean)))
        End Sub
    End Class

    ''' <summary>One read of the in-game screen (positions in VIEW pixels). Side-effect free to build.</summary>
    Public Class WorldUi
        Public Canvas As Rectangle
        ''' <summary>Open windows that ESC closes: "Notice", "Temporary Item List" (names for the log).</summary>
        Public ReadOnly Popups As New List(Of String)()
        ''' <summary>The window's own X (view px); Empty when that window is not open.</summary>
        Public NoticeClose As Point, TempClose As Point
        ''' <summary>Yellow X of each whisper (PM) row, top to bottom.</summary>
        Public ReadOnly PmCloses As New List(Of Point)()
        ''' <summary>Site's speaker button: True = sound on (yellow), False = off (grey ✕) / not seen.</summary>
        Public SoundOn As Boolean
        Public Speaker As Point

        Public Overrides Function ToString() As String
            Return $"pencere: {If(Popups.Count = 0, "yok", String.Join(", ", Popups))} · PM: {PmCloses.Count} · ses: {If(SoundOn, "açık", "kapalı")}"
        End Function
    End Class

    ''' <summary>
    ''' Right after entering the world, every account (user: "ekranda bunlar varsa 2-3 kere ESC'ye basmalı; sesleri mutelemeli
    ''' açıksa; PM'leri X'tan kapatmalı"). Measured on the user's live frame (9 Nov 23:55, canvas 1280×800):
    ''' - windows: "Temporary Item List" (centre) + "Notice" (right) — found by their title (templates); ESC ONLY when one is SEEN
    '''   (on a clean screen ESC opens the game menu — desktop KO lesson), max 3, each ESC checked on a new frame;
    ''' - sound: the site's round speaker button at ~31,6 % of the canvas width, 21 px from the top (HTML over the canvas — no script,
    '''   a click + frame): yellow = on → click → must turn grey;
    ''' - PMs: rows at the left with a yellow X — clicked one by one, each must disappear (bounded).
    ''' Never fails the login: everything goes to the log as one summary line.
    ''' </summary>
    Public Module AfterEnter

        Friend Const PopupMinScore As Double = 0.75
        Friend Const PmMinScore As Double = 0.8
        Friend Const MaxEsc As Integer = 3
        ''' <summary>Between two ESC / X actions (one ESC = one window; 1,1 s was too fast on the 3rd live run).</summary>
        Friend Const EscGapMs As Integer = 1500
        ''' <summary>The window's X from its title template corner (canvas-1280): Temporary 389,2 (two frames: 859,178 and 874,197
        ''' crop); Notice 212,10 (1280×800 frame: title 1040,86 → X 1252,96).</summary>
        Private ReadOnly TempCloseOff As New Point(389, 2)
        Private ReadOnly NoticeCloseOff As New Point(212, 10)
        Friend Const MaxPmClicks As Integer = 8
        Friend Const SettleMs As Integer = 2500   ' after the HUD is first seen, windows are still popping up (ölçülmedi)

        Public Async Function RunAsync(session As GameSession, ct As CancellationToken) As Task
            Dim n = session.Number
            Dim cw = session.Cw
            Dim escClosed = 0, escSent = 0, xClosed = 0, pmClosed = 0, soundDone As String = "zaten kapalı"
            Dim lastEscHelped = True
            Try
                Await Task.Delay(SettleMs, ct)
                Dim ui = Await ReadAsync(cw)
                If ui Is Nothing Then BotLog.Warn("giris", $"{n}. görünüm: oyun içi temizlik — kare yok") : Return
                BotLog.Add("giris", $"{n}. görünüm: oyuna girişte ekran — {ui}")

                ' 1) windows. ESC only while one is SEEN, one ESC = the top window, ≥ 1,5 s apart (3rd live run: two ESC 1,1 s
                '    apart closed Notice but never the Temporary Item List). A window ESC did not close → its OWN X.
                Dim actions = 0, xClicks = 0
                While ui.Popups.Count > 0 AndAlso actions < MaxEsc + 2
                    ct.ThrowIfCancellationRequested()
                    Dim before As New List(Of String)(ui.Popups)
                    session.SetStatus(SessionState.InGame, "Pencereler kapatılıyor")
                    Dim viaX = escSent >= MaxEsc OrElse (escSent > 0 AndAlso Not lastEscHelped)
                    Dim target = If(Not ui.TempClose.IsEmpty, ui.TempClose, ui.NoticeClose)
                    If viaX AndAlso Not target.IsEmpty Then
                        Await CdpInput.Mouse(cw, "mouseMoved", target.X, target.Y)
                        Await Task.Delay(200, ct)
                        Await CdpInput.ClickAsync(cw, target.X, target.Y)
                        xClicks += 1
                    Else
                        Await LoginKeys.EscAsync(cw)
                        escSent += 1
                    End If
                    actions += 1
                    Await Task.Delay(EscGapMs, ct)
                    ui = Await ReadAsync(cw)
                    If ui Is Nothing Then Exit While
                    Dim gone = before.Where(Function(p) Not ui.Popups.Contains(p)).ToList()
                    If viaX AndAlso Not target.IsEmpty Then
                        xClosed += gone.Count
                        If gone.Count = 0 Then BotLog.Warn("giris", $"{n}. görünüm: pencerenin X'i tıklandı ama kapanmadı ({target.X},{target.Y}; açık: {String.Join(", ", ui.Popups)})")
                    Else
                        escClosed += gone.Count
                        lastEscHelped = gone.Count > 0
                        If gone.Count = 0 Then BotLog.Warn("giris", $"{n}. görünüm: ESC pencereyi kapatmadı ({escSent}. ESC; açık: {String.Join(", ", ui.Popups)}) — X denenecek")
                    End If
                End While
                If ui Is Nothing Then Return

                ' 2) sound
                If ui.SoundOn Then
                    session.SetStatus(SessionState.InGame, "Ses kapatılıyor")
                    Await CdpInput.ClickAsync(cw, ui.Speaker.X, ui.Speaker.Y)
                    Await Task.Delay(500, ct)
                    Dim again = Await ReadAsync(cw)
                    soundDone = If(again IsNot Nothing AndAlso Not again.SoundOn, "kapatıldı", "kapatılamadı")
                    If again IsNot Nothing Then ui = again
                End If

                ' 3) PMs: top one each time, must disappear
                Dim clicks = 0
                While ui.PmCloses.Count > 0 AndAlso clicks < MaxPmClicks
                    ct.ThrowIfCancellationRequested()
                    Dim before = ui.PmCloses.Count
                    Dim p = ui.PmCloses(0)
                    session.SetStatus(SessionState.InGame, "PM'ler kapatılıyor")
                    Await CdpInput.Mouse(cw, "mouseMoved", p.X, p.Y)
                    Await Task.Delay(150, ct)
                    Await CdpInput.ClickAsync(cw, p.X, p.Y)
                    clicks += 1
                    Await Task.Delay(450, ct)
                    Dim again = Await ReadAsync(cw)
                    If again Is Nothing Then Exit While
                    If again.PmCloses.Count < before Then
                        pmClosed += before - again.PmCloses.Count
                    Else
                        BotLog.Warn("giris", $"{n}. görünüm: PM X tıkı satırı kapatmadı ({p.X},{p.Y})")
                    End If
                    ui = again
                End While

                Dim left = If(ui.PmCloses.Count > 0 OrElse ui.Popups.Count > 0, $" · kalan: {ui}", "")
                BotLog.Add("giris", $"{n}. görünüm: oyun içi temizlik — {escClosed} pencere ESC ile ({escSent} ESC), {xClosed} pencere X ile kapandı, ses {soundDone}, {pmClosed} PM kapatıldı{left}")
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                BotLog.Fail("giris", $"{n}. görünüm: oyun içi temizlik hatası", ex)
            Finally
                session.SetStatus(SessionState.InGame, "Oyunda")
            End Try
        End Function

        Private Async Function ReadAsync(cw As Microsoft.Web.WebView2.Core.CoreWebView2) As Task(Of WorldUi)
            Using f = Await ViewCapture.FullAsync(cw)
                If f Is Nothing Then Return Nothing
                Return Await Task.Run(Function() Read(f))
            End Using
        End Function

        ' ---------------- reading (side-effect free) ----------------

        Public Function Read(frame As Bitmap) As WorldUi
            Dim fw = frame.Width, fh = frame.Height
            Dim px = LoginScreens.ReadPixels(frame)
            Dim cr = LoginScreens.CanvasRect(px, fw, fh)
            Dim ui As New WorldUi With {.Canvas = cr}
            Dim lum(px.Length - 1) As Byte
            For i = 0 To px.Length - 1
                Dim c = px(i)
                lum(i) = CByte((((c >> 16) And &HFF) * 299 + ((c >> 8) And &HFF) * 587 + (c And &HFF) * 114) \ 1000)
            Next
            Dim s = cr.Width / 1280.0
            Dim view = Function(p As Point) New Point(cr.X + CInt(p.X * s), cr.Y + CInt(p.Y * s))
            Dim nt = AfterEnterTemplates.NoticeTitle, tt = AfterEnterTemplates.TempTitle
            Dim nm = Locate(lum, fw, fh, cr, s, nt, New Rectangle(nt.X0 - 140, nt.Y0 - 80, nt.W + 280, nt.H + 160))
            If nm.V >= PopupMinScore Then
                ui.Popups.Add("Notice")
                ui.NoticeClose = view(New Point(nm.P.X + NoticeCloseOff.X, nm.P.Y + NoticeCloseOff.Y))
            End If
            Dim tm = Locate(lum, fw, fh, cr, s, tt, New Rectangle(tt.X0 - 120, tt.Y0 - 120, tt.W + 240, tt.H + 240))
            If tm.V >= PopupMinScore Then
                ui.Popups.Add("Temporary Item List")
                ui.TempClose = view(New Point(tm.P.X + TempCloseOff.X, tm.P.Y + TempCloseOff.Y))
            End If
            ui.PmCloses.AddRange(FindPmCloses(lum, fw, fh, cr, s))
            ReadSpeaker(px, fw, fh, cr, ui)
            Return ui
        End Function

        ''' <summary>NCC of the template at canvas-1280 position (tx,ty) (top-left) — sampled every <paramref name="stepT"/> template pixels.</summary>
        Friend Function Score(lum() As Byte, fw As Integer, fh As Integer, cr As Rectangle, s As Double, t As GrayTemplate,
                               tx As Integer, ty As Integer, stepT As Integer) As Double
            Dim sumF As Double = 0, sumF2 As Double = 0, sumFT As Double = 0, n = 0
            Dim sumT As Double = 0, sumT2 As Double = 0
            For j = 0 To t.H - 1 Step stepT
                Dim fy = cr.Y + CInt((ty + j) * s)
                If fy < 0 OrElse fy >= fh Then Return 0
                For i = 0 To t.W - 1 Step stepT
                    Dim fx = cr.X + CInt((tx + i) * s)
                    If fx < 0 OrElse fx >= fw Then Return 0
                    Dim f As Double = lum(fy * fw + fx)
                    Dim v As Double = t.Data(j * t.W + i)
                    sumF += f : sumF2 += f * f : sumFT += f * v : sumT += v : sumT2 += v * v : n += 1
                Next
            Next
            If n = 0 Then Return 0
            Dim cov = sumFT - sumF * sumT / n
            Dim vf = sumF2 - sumF * sumF / n, vt = sumT2 - sumT * sumT / n
            If vf <= 1 OrElse vt <= 1 Then Return 0
            Return cov / Math.Sqrt(vf * vt)
        End Function

        ''' <summary>Luminance (PIL "L" weights) of a pixel array.</summary>
        Friend Function Luma(px() As Integer) As Byte()
            Dim lum(px.Length - 1) As Byte
            For i = 0 To px.Length - 1
                Dim c = px(i)
                lum(i) = CByte((((c >> 16) And &HFF) * 299 + ((c >> 8) And &HFF) * 587 + (c And &HFF) * 114) \ 1000)
            Next
            Return lum
        End Function

        ''' <summary>
        ''' Best place of a template inside an AREA of the canvas (canvas-1280 px): coarse grid then ±2 fine.
        ''' Returns the score and the template's top-left in canvas-1280 px.
        ''' </summary>
        Friend Function Locate(lum() As Byte, fw As Integer, fh As Integer, cr As Rectangle, s As Double, t As GrayTemplate,
                               area As Rectangle, Optional coarse As Integer = 3) As (V As Double, P As Point)
            ' coarse grid → the best few candidates → each refined at full resolution (a sharp text template loses most of its
            ' score 1 px off the grid, so the single coarse best is often the wrong place)
            ' coarse score on BLOCK MEANS (b×b) of template and frame: insensitive to the 1 px grid miss that made the sharp-text
            ' "Option" title score ~0,4 at its true place on the 3rd live run (window open, read as closed)
            Const MaxCands As Integer = 24
            Dim b = Math.Max(2, coarse)
            Dim integ = Integral(lum, fw, fh)
            Dim tb = TemplateBlocks(t, b)
            Dim cands As New List(Of (V As Double, X As Integer, Y As Integer))()
            For ty = area.Y To area.Bottom - t.H Step coarse
                For tx = area.X To area.Right - t.W Step coarse
                    Dim v = BlockScore(integ, fw, fh, cr, s, tb, t, b, tx, ty)
                    If cands.Count < MaxCands OrElse v > cands(cands.Count - 1).V Then
                        ' one candidate per neighbourhood: a strong wrong text must not fill the whole list
                        Dim txx = tx, tyy = ty
                        Dim near = cands.FindIndex(Function(c) Math.Abs(c.X - txx) <= 4 AndAlso Math.Abs(c.Y - tyy) <= 4)
                        If near >= 0 Then
                            If cands(near).V < v Then cands(near) = (v, tx, ty) : cands.Sort(Function(p1, p2) p2.V.CompareTo(p1.V))
                            Continue For
                        End If
                        cands.Add((v, tx, ty))
                        cands.Sort(Function(p1, p2) p2.V.CompareTo(p1.V))
                        If cands.Count > MaxCands Then cands.RemoveAt(MaxCands)
                    End If
                Next
            Next
            Dim fv = -1.0, fx = area.X, fy = area.Y
            Dim r = coarse \ 2 + 1
            For Each c In cands
                For dy = -r To r
                    For dx = -r To r
                        Dim v = Score(lum, fw, fh, cr, s, t, c.X + dx, c.Y + dy, 1)
                        If v > fv Then fv = v : fx = c.X + dx : fy = c.Y + dy
                    Next
                Next
            Next
            Return (fv, New Point(fx, fy))
        End Function

        Friend Function Integral(lum() As Byte, fw As Integer, fh As Integer) As Long()
            Dim ii((fw + 1) * (fh + 1) - 1) As Long
            For y = 1 To fh
                Dim row As Long = 0
                For x = 1 To fw
                    row += lum((y - 1) * fw + x - 1)
                    ii(y * (fw + 1) + x) = ii((y - 1) * (fw + 1) + x) + row
                Next
            Next
            Return ii
        End Function

        ''' <summary>Template means over b×b blocks (row-major, full blocks only).</summary>
        Private Function TemplateBlocks(t As GrayTemplate, b As Integer) As Double()
            Dim bw = t.W \ b, bh = t.H \ b
            Dim o(Math.Max(0, bw * bh - 1)) As Double
            For j = 0 To bh - 1
                For i = 0 To bw - 1
                    Dim sum = 0
                    For y = 0 To b - 1
                        For x = 0 To b - 1
                            sum += t.Data((j * b + y) * t.W + i * b + x)
                        Next
                    Next
                    o(j * bw + i) = sum / (b * b)
                Next
            Next
            Return o
        End Function

        Private Function BlockScore(ii() As Long, fw As Integer, fh As Integer, cr As Rectangle, s As Double, tb() As Double,
                                    t As GrayTemplate, b As Integer, tx As Integer, ty As Integer) As Double
            Dim bw = t.W \ b, bh = t.H \ b
            If bw * bh < 4 Then Return 0
            Dim sf As Double = 0, sf2 As Double = 0, sft As Double = 0, st As Double = 0, st2 As Double = 0
            Dim n = bw * bh, W1 = fw + 1
            For j = 0 To bh - 1
                Dim y0 = cr.Y + CInt((ty + j * b) * s), y1 = cr.Y + CInt((ty + (j + 1) * b) * s)
                If y0 < 0 OrElse y1 > fh OrElse y1 <= y0 Then Return 0
                For i = 0 To bw - 1
                    Dim x0 = cr.X + CInt((tx + i * b) * s), x1 = cr.X + CInt((tx + (i + 1) * b) * s)
                    If x0 < 0 OrElse x1 > fw OrElse x1 <= x0 Then Return 0
                    Dim sum = ii(y1 * W1 + x1) - ii(y0 * W1 + x1) - ii(y1 * W1 + x0) + ii(y0 * W1 + x0)
                    Dim f = sum / CDbl((x1 - x0) * (y1 - y0))
                    Dim v = tb(j * bw + i)
                    sf += f : sf2 += f * f : sft += f * v : st += v : st2 += v * v
                Next
            Next
            Dim cov = sft - sf * st / n, vf = sf2 - sf * sf / n, vt = st2 - st * st / n
            If vf <= 1 OrElse vt <= 1 Then Return 0
            Return cov / Math.Sqrt(vf * vt)
        End Function

        ''' <summary>Best NCC around the template's own place (±rx, ±ry canvas px): coarse step 3, then ±2 fine.</summary>
        Private Function Best(lum() As Byte, fw As Integer, fh As Integer, cr As Rectangle, s As Double, t As GrayTemplate, rx As Integer, ry As Integer) As Double
            Dim bx = t.X0, by = t.Y0, bs = -1.0
            For dy = -ry To ry Step 3
                For dx = -rx To rx Step 3
                    Dim v = Score(lum, fw, fh, cr, s, t, t.X0 + dx, t.Y0 + dy, 3)
                    If v > bs Then bs = v : bx = t.X0 + dx : by = t.Y0 + dy
                Next
            Next
            Dim fine = bs
            For dy = -2 To 2
                For dx = -2 To 2
                    fine = Math.Max(fine, Score(lum, fw, fh, cr, s, t, bx + dx, by + dy, 1))
                Next
            Next
            Return fine
        End Function

        ''' <summary>Every PM row X in the left part of the canvas (x 40..420, y 90..740 canvas-1280), local maxima, top to bottom.</summary>
        Friend Function FindPmCloses(lum() As Byte, fw As Integer, fh As Integer, cr As Rectangle, s As Double) As List(Of Point)
            Dim t = AfterEnterTemplates.PmClose
            Dim hits As New List(Of (X As Integer, Y As Integer, V As Double))()
            For ty = 90 To 740 - t.H
                For tx = 40 To 420 - t.W
                    Dim v = Score(lum, fw, fh, cr, s, t, tx, ty, 2)
                    If v < PmMinScore - 0.15 Then Continue For
                    ' refine
                    Dim bv = v, bx = tx, by = ty
                    For dy = -1 To 1
                        For dx = -1 To 1
                            Dim v2 = Score(lum, fw, fh, cr, s, t, tx + dx, ty + dy, 1)
                            If v2 > bv Then bv = v2 : bx = tx + dx : by = ty + dy
                        Next
                    Next
                    If bv < PmMinScore Then Continue For
                    Dim near = hits.FindIndex(Function(hh) Math.Abs(hh.X - bx) < 14 AndAlso Math.Abs(hh.Y - by) < 14)
                    If near >= 0 Then
                        If hits(near).V < bv Then hits(near) = (bx, by, bv)
                    Else
                        hits.Add((bx, by, bv))
                    End If
                Next
            Next
            Return hits.OrderBy(Function(hh) hh.Y).
                Select(Function(hh) New Point(cr.X + CInt((hh.X + t.W / 2.0) * s), cr.Y + CInt((hh.Y + t.H / 2.0) * s))).ToList()
        End Function

        ''' <summary>
        ''' Speaker: x 27..36 % of the canvas width, rows 2..42 from the canvas top (HTML button, not scaled vertically — measured at
        ''' 1280×720 / 1280×800 / 960×540 / 800×600 / full-screen 1920×1032 views). Yellow pixels = on (open 51-58, muted 0).
        ''' </summary>
        Friend Sub ReadSpeaker(px() As Integer, fw As Integer, fh As Integer, cr As Rectangle, ui As WorldUi)
            Dim n = 0, sx As Long = 0, sy As Long = 0
            For y = cr.Y + 2 To Math.Min(fh - 1, cr.Y + 42)
                For x = cr.X + CInt(cr.Width * 0.27) To cr.X + CInt(cr.Width * 0.36)
                    Dim c = px(y * fw + x)
                    Dim r = (c >> 16) And &HFF, g = (c >> 8) And &HFF, b = c And &HFF
                    If r >= 170 AndAlso g >= 120 AndAlso b <= 110 Then n += 1 : sx += x : sy += y
                Next
            Next
            ui.SoundOn = n >= 8
            If n > 0 Then ui.Speaker = New Point(CInt(sx \ n), CInt(sy \ n))
        End Sub

    End Module

End Namespace
