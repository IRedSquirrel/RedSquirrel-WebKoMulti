Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Namespace Engine.Login

    Public Enum ScreenKind
        ''' <summary>No frame / blank (loading, all one colour).</summary>
        Blank
        ''' <summary>Something we don't recognise.</summary>
        Unknown
        ''' <summary>The game's ID / password box.</summary>
        Login
        ''' <summary>In the world: the HP / MP bars at the top left are seen.</summary>
        InGame
        ''' <summary>"Select Server" list with the orange "Connect" button at the bottom centre (measured 1280×800, 9 Nov).</summary>
        Server
        ''' <summary>Character select: ◄ ► arrow buttons at the bottom centre + right panel (name / LV / Job, Start Game).</summary>
        Character
    End Enum

    Public Class ScreenRead
        Public Kind As ScreenKind
        Public Detail As String = ""
        ''' <summary>Login only: where the boxes are (view pixels = frame pixels).</summary>
        Public Login As LoginBox
        ''' <summary>Server only: the Connect button centre.</summary>
        Public Connect As Point
        ''' <summary>Character only.</summary>
        Public Character As CharacterLayout
        ''' <summary>Where the game canvas is in the view (whole frame unless letterboxed in black margins).</summary>
        Public Canvas As Rectangle
    End Class

    Public Class CharacterLayout
        Public LeftArrow As Point
        Public RightArrow As Point
        ''' <summary>◄ gold = there is a next character that way.</summary>
        Public LeftEnabled As Boolean
        ''' <summary>► gold = there is a previous character (grey = this is the first one).</summary>
        Public RightEnabled As Boolean
        ''' <summary>Start Game centre; Point.Empty when the panel was not found (then only Enter).</summary>
        Public StartGame As Point

        Public Overrides Function ToString() As String
            Return $"◄ {LeftArrow.X},{LeftArrow.Y} {If(LeftEnabled, "açık", "gri")} · ► {RightArrow.X},{RightArrow.Y} {If(RightEnabled, "açık", "gri")}" &
                   If(StartGame.IsEmpty, " · Start Game yok", $" · Start Game {StartGame.X},{StartGame.Y}")
        End Function
    End Class

    Public Class LoginBox
        Public IdBox As Rectangle
        Public PasswordBox As Rectangle
        Public OkButton As Point

        Public Shared Function Center(r As Rectangle) As Point
            Return New Point(r.X + r.Width \ 2, r.Y + r.Height \ 2)
        End Function

        Public Overrides Function ToString() As String
            Dim a = Center(IdBox), b = Center(PasswordBox)
            Return $"ID {a.X},{a.Y} ({IdBox.Width}×{IdBox.Height}) · Şifre {b.X},{b.Y} · OK {OkButton.X},{OkButton.Y}"
        End Function
    End Class

    ''' <summary>
    ''' "Which screen are we on" from a view frame. SIDE-EFFECT FREE (self-test runs it on synthetic frames).
    ''' Frame = ViewCapture.FullAsync = view pixels, same coordinates CdpInput takes (at 100 % DPI).
    ''' - InGame: HP bar frame line (80,96,73) + black row below — measured on ardream (1920 canvas);
    '''   the length is taken relative to the frame width because the canvas is scaled to the view.
    ''' - Login: same layout as the desktop KO client (1024×768): two pitch-black boxes stacked + red OK band.
    '''   Whether ardream draws the same box: see LOGIN.md (measurement status is written there, not assumed here).
    ''' </summary>
    Public Module LoginScreens

        ' ---- tuning in ONE place (LOGIN.md lists which are measured) ----
        Friend Const DarkThreshold As Integer = 36        ' r+g+b; box interior
        Friend Const RunGap As Integer = 4
        ''' <summary>OK centre = password centre + k × box spacing. 1.39 ardream (measured 9 Nov, 1280×720: boxes 269/319, OK 397).
        ''' The desktop KO ratio 0.84 was REMOVED: the character screen's LV / Job boxes + red "Create Character" fit it and the
        ''' character screen was read as "back at login" (user's first live run, 9 Nov 23:44).</summary>
        Friend ReadOnly OkRatios As Double() = {1.39}
        ''' <summary>Login box is horizontally centred (643 / 1280 measured); the character panel sits at the right.</summary>
        Friend Const LoginCenterTolerance As Double = 0.06
        ''' <summary>Character arrows (1280×800: ◄ 601..633, ► 646..679, rows 722..755) — sizes relative to s = min(w, 1,6·h).</summary>
        Friend Const ArrowMinRatio As Double = 0.018
        Friend Const ArrowMaxRatio As Double = 0.04
        ''' <summary>Start Game centre = Job box centre + 4.45 × (LV→Job spacing) (1280×800: Job 352, spacing 53, Start Game 588).</summary>
        Friend Const StartGameRatio As Double = 4.45
        ''' <summary>Connect: orange text, bottom centre (1280×800: x 593..694, y 660..685, 180 px).</summary>
        Friend Const ConnectMinShare As Double = 0.00008
        Friend Const OkSearchRatio As Double = 1.9
        Friend Const BoxMinWidthRatio As Double = 0.04    ' box width ≥ 4 % of the frame (desktop: 134 / 1024 = 13 %)
        Friend Const HudMinRunRatio As Double = 0.04      ' HP frame run ≥ 4 % of width (191 / 1920 ≈ 10 %)
        Friend Const HudMaxRunRatio As Double = 0.2
        Friend Const HudSearchHeightRatio As Double = 0.2 ' bars are in the top 20 %
        Friend Const BlankSpread As Integer = 24          ' max-min of r+g+b over the samples ≤ this → blank

        Public Function Classify(frame As Bitmap) As ScreenRead
            If frame Is Nothing OrElse frame.Width < 100 OrElse frame.Height < 80 Then Return New ScreenRead With {.Kind = ScreenKind.Blank, .Detail = "kare yok"}
            Dim fw = frame.Width, fh = frame.Height
            Dim full = ReadPixels(frame)
            If IsBlank(full, fw, fh) Then Return New ScreenRead With {.Kind = ScreenKind.Blank, .Detail = "boş / tek renk", .Canvas = New Rectangle(0, 0, fw, fh)}
            ' a full-screen / larger view shows the game canvas CENTRED in black margins (user's leader: 1920×1032 view, canvas
            ' 1280×800 at 320,87): measure inside the canvas and give points back in view pixels
            Dim cr = CanvasRect(full, fw, fh)
            Dim r = ClassifyCanvas(Crop(full, fw, cr), cr.Width, cr.Height)
            r.Canvas = cr
            If cr.X <> 0 OrElse cr.Y <> 0 Then Shift(r, cr.Location)
            Return r
        End Function

        Private Sub Shift(r As ScreenRead, o As Point)
            Dim mv = Function(p As Point) If(p.IsEmpty, p, New Point(p.X + o.X, p.Y + o.Y))
            If r.Login IsNot Nothing Then
                r.Login.IdBox.Offset(o) : r.Login.PasswordBox.Offset(o) : r.Login.OkButton = mv(r.Login.OkButton)
                r.Detail = r.Login.ToString()
            End If
            If Not r.Connect.IsEmpty Then r.Connect = mv(r.Connect) : r.Detail = $"Connect {r.Connect.X},{r.Connect.Y}"
            If r.Character IsNot Nothing Then
                r.Character.LeftArrow = mv(r.Character.LeftArrow) : r.Character.RightArrow = mv(r.Character.RightArrow)
                r.Character.StartGame = mv(r.Character.StartGame)
                r.Detail = r.Character.ToString()
            End If
            r.Detail &= $" (tuval {r.Canvas.Width}×{r.Canvas.Height} @{o.X},{o.Y})"
        End Sub

        ''' <summary>
        ''' The game canvas inside the view: bounding box of non-black pixels, but ONLY when the black margins are symmetric
        ''' (letterboxing: left ≈ right and/or top ≈ bottom). Otherwise the whole frame. Side-effect free.
        ''' </summary>
        Friend Function CanvasRect(px() As Integer, w As Integer, h As Integer) As Rectangle
            Dim colLit = Function(x As Integer)
                             For y = 0 To h - 1 Step 3
                                 If Sum(px(y * w + x)) > 45 Then Return True
                             Next
                             Return False
                         End Function
            Dim rowLit = Function(y As Integer, xa As Integer, xb As Integer)
                             For x = xa To xb Step 3
                                 If Sum(px(y * w + x)) > 45 Then Return True
                             Next
                             Return False
                         End Function
            Dim l = 0, r = w - 1, t = 0, b = h - 1
            While l < w \ 2 AndAlso Not colLit(l) : l += 1 : End While
            While r > w \ 2 AndAlso Not colLit(r) : r -= 1 : End While
            While t < h \ 2 AndAlso Not rowLit(t, l, r) : t += 1 : End While
            While b > h \ 2 AndAlso Not rowLit(b, l, r) : b -= 1 : End While
            Dim tol = Math.Max(6, w \ 100)
            Dim cropX = l >= 8 AndAlso Math.Abs(l - (w - 1 - r)) <= tol
            Dim cropY = t >= 8 AndAlso Math.Abs(t - (h - 1 - b)) <= Math.Max(6, h \ 100)
            Dim x0 = If(cropX, l, 0), x1 = If(cropX, r, w - 1), y0 = If(cropY, t, 0), y1 = If(cropY, b, h - 1)
            Return New Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1)
        End Function

        Friend Function Crop(px() As Integer, w As Integer, r As Rectangle) As Integer()
            If r.X = 0 AndAlso r.Y = 0 AndAlso r.Width = w AndAlso r.Width * r.Height = px.Length Then Return px
            Dim o(r.Width * r.Height - 1) As Integer
            For y = 0 To r.Height - 1
                Array.Copy(px, (r.Y + y) * w + r.X, o, y * r.Width, r.Width)
            Next
            Return o
        End Function

        Private Function ClassifyCanvas(px() As Integer, w As Integer, h As Integer) As ScreenRead
            Dim hud As String = ""
            If HasHud(px, w, h, hud) Then Return New ScreenRead With {.Kind = ScreenKind.InGame, .Detail = hud}
            Dim ch = FindCharacter(px, w, h)
            If ch IsNot Nothing Then Return New ScreenRead With {.Kind = ScreenKind.Character, .Character = ch, .Detail = ch.ToString()}
            Dim connect As Point
            If FindConnect(px, w, h, connect) Then Return New ScreenRead With {.Kind = ScreenKind.Server, .Connect = connect, .Detail = $"Connect {connect.X},{connect.Y}"}
            Dim why As String = ""
            Dim box = FindLogin(px, w, h, why)
            If box IsNot Nothing Then Return New ScreenRead With {.Kind = ScreenKind.Login, .Login = box, .Detail = box.ToString()}
            Return New ScreenRead With {.Kind = ScreenKind.Unknown, .Detail = why}
        End Function

        Friend Function ReadPixels(frame As Bitmap) As Integer()
            Dim w = frame.Width, h = frame.Height
            Dim px(w * h - 1) As Integer
            Using b32 As New Bitmap(w, h, PixelFormat.Format32bppArgb)
                Using g = Graphics.FromImage(b32)
                    g.DrawImageUnscaled(frame, 0, 0)
                End Using
                Dim data = b32.LockBits(New Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
                Try
                    For y = 0 To h - 1
                        Marshal.Copy(data.Scan0 + y * data.Stride, px, y * w, w)
                    Next
                Finally
                    b32.UnlockBits(data)
                End Try
            End Using
            Return px
        End Function

        Private Function Sum(c As Integer) As Integer
            Return ((c >> 16) And &HFF) + ((c >> 8) And &HFF) + (c And &HFF)
        End Function

        Friend Function IsBlank(px() As Integer, w As Integer, h As Integer) As Boolean
            Dim mn = Integer.MaxValue, mx = Integer.MinValue
            For y = h \ 20 To h - 1 Step Math.Max(1, h \ 24)
                For x = w \ 20 To w - 1 Step Math.Max(1, w \ 32)
                    Dim s = Sum(px(y * w + x))
                    If s < mn Then mn = s
                    If s > mx Then mx = s
                Next
            Next
            Return mx - mn <= BlankSpread
        End Function

        ''' <summary>HP / MP frame line: (80,96,73) run in the top part, black just below both ends.</summary>
        Friend Function HasHud(px() As Integer, w As Integer, h As Integer, ByRef detail As String) As Boolean
            Dim minRun = Math.Max(24, CInt(w * HudMinRunRatio)), maxRun = CInt(w * HudMaxRunRatio)
            Dim lines As Integer = 0, firstY As Integer = -1, firstLen As Integer = 0
            Dim yMax = Math.Min(h - 2, CInt(h * HudSearchHeightRatio))
            Dim y = 0
            While y < yMax
                Dim runX0 = -1, best = 0, bestX0 = -1
                For x = 0 To w - 1
                    Dim c = px(y * w + x)
                    Dim r = (c >> 16) And &HFF, g = (c >> 8) And &HFF, b = c And &HFF
                    If Math.Abs(r - 82) <= 12 AndAlso Math.Abs(g - 95) <= 10 AndAlso Math.Abs(b - 76) <= 12 Then   ' exact capture (80,96,76); lossy screenshot (84-90,90-93,78-90)
                        If runX0 < 0 Then runX0 = x
                        If x - runX0 + 1 > best Then best = x - runX0 + 1 : bestX0 = runX0
                    Else
                        runX0 = -1
                    End If
                Next
                If best >= minRun AndAlso best <= maxRun AndAlso Sum(px((y + 1) * w + bestX0)) < 60 AndAlso Sum(px((y + 1) * w + bestX0 + best - 1)) < 60 Then
                    lines += 1
                    If firstY < 0 Then firstY = y : firstLen = best
                    y += 3
                Else
                    y += 1
                End If
            End While
            If lines = 0 Then Return False
            detail = $"HP çerçevesi y={firstY} uzunluk {firstLen} ({lines} çizgi)"
            Return True
        End Function

        Private Structure Box
            Public X0, X1, Y0, Y1, Rows As Integer
            ReadOnly Property Width As Integer
                Get
                    Return X1 - X0 + 1
                End Get
            End Property
            ReadOnly Property Height As Integer
                Get
                    Return Y1 - Y0 + 1
                End Get
            End Property
        End Structure

        Private Function CollectBoxes(px() As Integer, w As Integer, h As Integer) As List(Of Box)
            Dim minLen = Math.Max(30, CInt(w * BoxMinWidthRatio))
            Dim boxes As New List(Of Box)()
            For y = 0 To h - 1
                For Each seg In DarkRuns(px, w, y)
                    Dim len = seg.X1 - seg.X0 + 1
                    If len < minLen OrElse len > w * 0.6 OrElse seg.X0 = 0 OrElse seg.X1 = w - 1 Then Continue For
                    Dim merged = False
                    For i = boxes.Count - 1 To 0 Step -1
                        Dim b = boxes(i)
                        Dim tol = Math.Max(16, CInt(b.Width * 0.12))
                        If Math.Abs(b.X0 - seg.X0) <= 3 AndAlso Math.Abs(b.X1 - seg.X1) <= 3 AndAlso y - b.Y1 <= tol Then
                            b.Y1 = y : b.Rows += 1 : boxes(i) = b : merged = True : Exit For
                        End If
                    Next
                    If Not merged Then boxes.Add(New Box With {.X0 = seg.X0, .X1 = seg.X1, .Y0 = y, .Y1 = y, .Rows = 1})
                Next
            Next
            Return boxes.Where(Function(b) b.Rows >= 3 AndAlso b.Height >= b.Width * 0.1 AndAlso b.Height <= b.Width * 0.32).ToList()
        End Function

        Private Function SamePair(a As Box, b As Box) As Boolean
            If b.Y0 <= a.Y0 Then Return False
            If Math.Abs(a.X0 - b.X0) > 3 OrElse Math.Abs(a.X1 - b.X1) > 3 Then Return False
            ' 1280×720 real frame: ID 17 rows, password 22 (its dark top border row merges in) → ±35 %, not ±25 %
            If Math.Abs(a.Height - b.Height) > Math.Max(4, CInt(Math.Ceiling(a.Height * 0.35))) Then Return False
            Dim gap = b.Y0 - a.Y0
            Return gap >= a.Height * 1.6 AndAlso gap <= a.Height * 3.4
        End Function

        ''' <summary>Two same-width pitch-black boxes stacked, horizontally centred, + a red band below. Nothing + reason if not.</summary>
        Friend Function FindLogin(px() As Integer, w As Integer, h As Integer, ByRef reason As String) As LoginBox
            Dim boxes = CollectBoxes(px, w, h)
            If boxes.Count < 2 Then reason = $"yazı kutusu yok ({boxes.Count} aday)" : Return Nothing
            Dim best As LoginBox = Nothing, bestErr = Double.MaxValue
            For Each a In boxes
                For Each b In boxes
                    If Not SamePair(a, b) Then Continue For
                    If Math.Abs((a.X0 + a.X1) / 2.0 - w / 2.0) > w * LoginCenterTolerance Then Continue For
                    Dim gap = b.Y0 - a.Y0
                    Dim bMid = (b.Y0 + b.Y1) / 2.0
                    Dim okY = RedBandMiddle(px, w, h, a.X0, a.X1, b.Y1 + 2, CInt(bMid + OkSearchRatio * gap))
                    If okY < 0 Then Continue For
                    Dim err = OkRatios.Min(Function(k) Math.Abs(okY - (bMid + k * gap)))
                    If err > 0.3 * gap Then Continue For
                    If err < bestErr Then
                        bestErr = err
                        best = New LoginBox With {.IdBox = New Rectangle(a.X0, a.Y0, a.Width, a.Height),
                                                  .PasswordBox = New Rectangle(b.X0, b.Y0, b.Width, b.Height),
                                                  .OkButton = New Point((a.X0 + a.X1) \ 2, okY)}
                    End If
                Next
            Next
            If best Is Nothing Then reason = $"{boxes.Count} kutu var ama ID / Şifre / OK düzeni yok"
            Return best
        End Function

        ' ---------------- character select ----------------

        Private Function IsGoldFrame(c As Integer) As Boolean
            Dim r = (c >> 16) And &HFF, g = (c >> 8) And &HFF, b = c And &HFF
            Return r >= 80 AndAlso r >= b + 40 AndAlso g >= r * 0.55 AndAlso g <= r + 8
        End Function

        Private Function IsGreyFrame(c As Integer) As Boolean
            Dim r = (c >> 16) And &HFF, g = (c >> 8) And &HFF, b = c And &HFF
            Dim mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b))
            Return mx - mn <= 14 AndAlso mx >= 60 AndAlso mx <= 215
        End Function

        ''' <summary>
        ''' Two square arrow buttons side by side at the bottom centre: their top border is a run of gold (can be pressed) or grey
        ''' (no character that way) pixels on the same row. Panel (Start Game) from the right-side LV / Job boxes. Nothing if no arrows.
        ''' </summary>
        Friend Function FindCharacter(px() As Integer, w As Integer, h As Integer) As CharacterLayout
            Dim s = Math.Min(w, h * 1.6)
            Dim minRun = Math.Max(12, CInt(s * ArrowMinRatio)), maxRun = CInt(s * ArrowMaxRatio) + 2
            Dim cx = w / 2.0
            Dim x0 = Math.Max(0, CInt(cx - s * 0.12)), x1 = Math.Min(w - 1, CInt(cx + s * 0.12))
            For y = CInt(h * 0.75) To h - 4
                Dim runs As New List(Of (A As Integer, B As Integer, Gold As Integer))()
                Dim start = -1, gold = 0, gap = 0, last = -1
                For x = x0 To x1 + 1
                    Dim c = If(x <= x1, px(y * w + x), 0)
                    Dim isG = x <= x1 AndAlso IsGoldFrame(c)
                    Dim isF = x <= x1 AndAlso (isG OrElse IsGreyFrame(c))
                    If isF Then
                        If start < 0 Then start = x : gold = 0
                        If isG Then gold += 1
                        last = x : gap = 0
                    ElseIf start >= 0 Then
                        gap += 1
                        If gap > 2 OrElse x > x1 Then
                            runs.Add((start, last, gold)) : start = -1 : gap = 0
                        End If
                    End If
                Next
                runs = runs.Where(Function(r) r.B - r.A + 1 >= minRun AndAlso r.B - r.A + 1 <= maxRun).ToList()
                For i = 0 To runs.Count - 2
                    Dim a = runs(i), b = runs(i + 1)
                    Dim between = b.A - a.B
                    If between < s * 0.004 OrElse between > s * 0.022 Then Continue For
                    If Math.Abs((a.B - a.A) - (b.B - b.A)) > Math.Max(4, s * 0.006) Then Continue For
                    If Math.Abs((a.A + b.B) / 2.0 - cx) > s * 0.02 Then Continue For
                    ' both must be boxes: the same columns stay framed down the sides (left / right edge of each button)
                    Dim size = a.B - a.A + 1
                    ' (left edge of each: at 0,75 scale the grey button's right edge blurs away — measured)
                    If Not SideFramed(px, w, h, a.A, y, size) OrElse Not SideFramed(px, w, h, b.A, y, size) Then Continue For
                    ' the buttons sit on the dark strip at the bottom: dark just above and just below (1280×800: rows 719 and 758 ≈ 0..10);
                    ' a bright login-screen scene with two gold-ish runs is not this
                    If RowMean(px, w, h, y - 3, a.A, b.B) > 90 OrElse RowMean(px, w, h, y + size + 3, a.A, b.B) > 90 Then Continue For
                    Dim lay As New CharacterLayout With {
                        .LeftArrow = New Point((a.A + a.B) \ 2, y + size \ 2),
                        .RightArrow = New Point((b.A + b.B) \ 2, y + size \ 2),
                        .LeftEnabled = a.Gold * 2 >= (a.B - a.A + 1),
                        .RightEnabled = b.Gold * 2 >= (b.B - b.A + 1)}
                    lay.StartGame = FindStartGame(px, w, h)
                    Return lay
                Next
            Next
            Return Nothing
        End Function

        Private Function RowMean(px() As Integer, w As Integer, h As Integer, y As Integer, x0 As Integer, x1 As Integer) As Double
            If y < 0 OrElse y >= h Then Return 999
            Dim t As Long = 0
            For x = x0 To x1
                t += Sum(px(y * w + x))
            Next
            Return t / Math.Max(1, x1 - x0 + 1)
        End Function

        ''' <summary>The button's side edge: framed (gold / grey) pixels in a column near x for at least half of the button height.</summary>
        Private Function SideFramed(px() As Integer, w As Integer, h As Integer, x As Integer, y As Integer, size As Integer) As Boolean
            Dim best = 0
            For dx = -2 To 2
                Dim xx = x + dx
                If xx < 0 OrElse xx >= w Then Continue For
                Dim n = 0
                For yy = y + 2 To Math.Min(h - 1, y + size - 2)
                    Dim c = px(yy * w + xx)
                    If IsGoldFrame(c) OrElse IsGreyFrame(c) Then n += 1
                Next
                best = Math.Max(best, n)
            Next
            Return best >= (size - 4) \ 2
        End Function

        ''' <summary>Right-side panel: the LOWEST pair of stacked black boxes right of 65 % width (LV, Job) → Start Game below it.</summary>
        Private Function FindStartGame(px() As Integer, w As Integer, h As Integer) As Point
            Dim boxes = CollectBoxes(px, w, h).Where(Function(b) (b.X0 + b.X1) / 2.0 > w * 0.65).ToList()
            Dim bestY = -1, result = Point.Empty
            For Each a In boxes
                For Each b In boxes
                    If Not SamePair(a, b) Then Continue For
                    Dim gap = b.Y0 - a.Y0
                    Dim y = CInt((b.Y0 + b.Y1) / 2.0 + StartGameRatio * gap)
                    If y >= h OrElse b.Y1 <= bestY Then Continue For
                    bestY = b.Y1
                    result = New Point((b.X0 + b.X1) \ 2, y)
                Next
            Next
            Return result
        End Function

        ' ---------------- server list ----------------

        Private Function IsOrangeText(c As Integer) As Boolean
            Dim r = (c >> 16) And &HFF, g = (c >> 8) And &HFF, b = c And &HFF
            Return r >= 170 AndAlso g >= 90 AndAlso g <= 190 AndAlso b <= 110 AndAlso r - g >= 40
        End Function

        ''' <summary>"Connect": orange letters at the bottom centre (x 40..60 %, y 78..90 %), the cluster centred on w/2.</summary>
        Friend Function FindConnect(px() As Integer, w As Integer, h As Integer, ByRef center As Point) As Boolean
            Dim n = 0, minX = Integer.MaxValue, maxX = -1, minY = Integer.MaxValue, maxY = -1
            For y = CInt(h * 0.78) To Math.Min(h - 1, CInt(h * 0.9))
                For x = CInt(w * 0.4) To CInt(w * 0.6)
                    If IsOrangeText(px(y * w + x)) Then
                        n += 1
                        minX = Math.Min(minX, x) : maxX = Math.Max(maxX, x) : minY = Math.Min(minY, y) : maxY = Math.Max(maxY, y)
                    End If
                Next
            Next
            If n < Math.Max(20, CInt(w * h * ConnectMinShare)) Then Return False
            ' one compact text line (real: 25 rows = 3 % of 800); in-game orange chat text + item-list gold spread over many rows
            ' and were read as "Connect" on the user's second run (Enter + click went into the world)
            If maxY - minY > h * 0.045 Then Return False
            If maxX - minX < w * 0.05 OrElse Math.Abs((minX + maxX) / 2.0 - w / 2.0) > w * 0.03 Then Return False
            center = New Point((minX + maxX) \ 2, (minY + maxY) \ 2)
            Return True
        End Function

        Private Function DarkRuns(px() As Integer, w As Integer, y As Integer) As List(Of (X0 As Integer, X1 As Integer))
            Dim runs As New List(Of (X0 As Integer, X1 As Integer))()
            Dim start = -1, last = -1, gap = 0, row = y * w
            For x = 0 To w
                Dim dark = x < w AndAlso Sum(px(row + x)) < DarkThreshold
                If dark Then
                    If start < 0 Then start = x
                    last = x : gap = 0
                ElseIf start >= 0 Then
                    gap += 1
                    If gap > RunGap OrElse x = w Then runs.Add((start, last)) : start = -1 : gap = 0
                End If
            Next
            Return runs
        End Function

        Private Function RedBandMiddle(px() As Integer, w As Integer, h As Integer, x0 As Integer, x1 As Integer, y0 As Integer, y1 As Integer) As Integer
            y0 = Math.Max(0, y0) : y1 = Math.Min(h - 1, y1)
            Dim first = -1, last = -1
            For y = y0 To y1
                Dim n = 0
                For x = Math.Max(0, x0) To Math.Min(w - 1, x1)
                    Dim c = px(y * w + x)
                    Dim r = (c >> 16) And &HFF, g = (c >> 8) And &HFF, b = c And &HFF
                    If r >= 45 AndAlso r * 2 > g * 5 AndAlso r * 2 > b * 5 Then n += 1
                Next
                If n >= (x1 - x0 + 1) * 0.6 Then
                    If first < 0 Then first = y
                    last = y
                ElseIf first >= 0 Then
                    Exit For
                End If
            Next
            If first < 0 OrElse last - first < 2 Then Return -1
            Return (first + last) \ 2
        End Function

        ''' <summary>Columns with ≥ 2 bright pixels inside the box (text / asterisks). Caret = 1 column. -1 without a frame.</summary>
        Public Function TextColumns(frame As Bitmap, box As Rectangle) As Integer
            If frame Is Nothing Then Return -1
            Dim px = ReadPixels(frame)
            Return TextColumns(px, frame.Width, frame.Height, box)
        End Function

        Friend Function TextColumns(px() As Integer, w As Integer, h As Integer, box As Rectangle) As Integer
            Dim x0 = Math.Max(0, box.X + 3), x1 = Math.Min(w - 1, box.Right - 4)
            Dim y0 = Math.Max(0, box.Y + Math.Max(2, box.Height \ 5)), y1 = Math.Min(h - 1, box.Bottom - 1 - Math.Max(1, box.Height \ 8))
            If x1 <= x0 OrElse y1 <= y0 Then Return -1
            Dim cols = 0
            For x = x0 To x1
                Dim n = 0
                For y = y0 To y1
                    If Sum(px(y * w + x)) > 330 Then n += 1
                Next
                If n >= 2 Then cols += 1
            Next
            Return cols
        End Function

        Public Const TextColumnThreshold As Integer = 4

        ''' <summary>Coarse scene signature (64 samples of r+g+b) — two frames "differ" when many samples moved. Side-effect free.</summary>
        Public Function Signature(frame As Bitmap) As Integer()
            If frame Is Nothing Then Return Nothing
            Dim px = ReadPixels(frame)
            Dim w = frame.Width, h = frame.Height
            Dim s As New List(Of Integer)()
            For j = 1 To 8
                For i = 1 To 8
                    s.Add(Sum(px((h * j \ 9) * w + w * i \ 9)))
                Next
            Next
            Return s.ToArray()
        End Function

        ''' <summary>Share (0..1) of samples that moved by more than 40.</summary>
        Public Function Difference(a() As Integer, b() As Integer) As Double
            If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return 1
            Dim n = 0
            For i = 0 To a.Length - 1
                If Math.Abs(a(i) - b(i)) > 40 Then n += 1
            Next
            Return n / a.Length
        End Function

    End Module

End Namespace
