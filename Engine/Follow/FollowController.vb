Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Core.Vision
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Engine.Follow

    ''' <summary>
    ''' N-account LEADER FOLLOW. Every in-game session except the leader walks to the leader's coordinate. LeaderIndex may
    ''' change while running. Only sessions with State = InGame take part; when the leader is not in game the followers
    ''' let go and wait.
    ''' One round: the 240×96 coordinate crop of every in-game view (FollowCapture: whole-view JPEG cut in memory — the
    ''' clipped Page.captureScreenshot made the views flicker; at the located game canvas, in
    ''' parallel; the canvas is found once per view size from one whole frame — GameCanvas) → WebCoordReader
    ''' → PosFilter → each follower's FollowWalker step (in parallel). CoreWebView2 is UI-thread bound, so StartAsync must
    ''' be called on the UI thread; the loop then runs there between awaits (nothing blocks it).
    ''' Log: changes only (who walks / arrives / can't read), plus the round time every 25 rounds — never a line per round.
    ''' </summary>
    Public NotInheritable Class FollowController
        Private Const Cat As String = "takip"
        Private Const TargetRoundMs As Integer = 120
        Private Const ReportEvery As Integer = 25
        Private Const StatusRepeatMs As Long = 400

        Private ReadOnly _sessions As IList(Of GameSession)
        Private ReadOnly _walkers As New Dictionary(Of Integer, FollowWalker)
        Private ReadOnly _filters As New Dictionary(Of Integer, PosFilter)
        Private ReadOnly _lastKind As New Dictionary(Of Integer, String)
        Private ReadOnly _lastStatus As New Dictionary(Of Integer, (Text As String, T As Long))
        Private ReadOnly _lastStuckLog As New Dictionary(Of Integer, Long)
        Private _loop As Task
        Private _stop As Boolean
        Private _activeLeader As Integer = -1
        Private _leaderWaitSaid As String = ""
        Private _pausedForMirror As Boolean
        ' Adaptive pace (FollowPace): calm group → a round every 450 ms instead of 120 ms (each round = a capture per view).
        Private ReadOnly _pace As New FollowPace()
        Private _nextDelay As Integer = FollowPace.FastMs
        Private Const PausedDelayMs As Integer = 300
        Private Const NoLeaderDelayMs As Integer = 500

        ' Game canvas per session (input / CSS pixels). The browser game does not scale in a bigger view: it draws 1280×800
        ' in the middle with black around it (Core.Vision.GameCanvas). Located once from a whole-view frame, kept until the
        ' view's size changes (SizeChanged) or the coordinate stays unreadable for a while.
        Private ReadOnly _canvas As New Dictionary(Of Integer, Rectangle)        ' input (CSS) pixels — walking circle
        Private ReadOnly _canvasPx As New Dictionary(Of Integer, Rectangle)      ' captured-frame pixels — coordinate crop
        Private ReadOnly _frameSize As New Dictionary(Of Integer, Size)
        Private ReadOnly _canvasAt As New Dictionary(Of Integer, Long)
        Private ReadOnly _misses As New Dictionary(Of Integer, Integer)
        Private ReadOnly _sizeHooks As New Dictionary(Of Integer, (View As Control, Handler As EventHandler))
        Private Const RelocateAfterMisses As Integer = 15
        Private Const RelocateMinGapMs As Long = 5000

        ''' <summary>(session index, text) — UI thread while running. Index = -1 for an overall message.</summary>
        Public Event Status(index As Integer, text As String)

        Public Sub New(sessions As IList(Of GameSession))
            _sessions = sessions
        End Sub

        ''' <summary>Index into the session list. May be changed while running — picked up at the next round.</summary>
        Public Property LeaderIndex As Integer

        Private _running As Boolean
        Public ReadOnly Property Running As Boolean
            Get
                Return _running
            End Get
        End Property

        ''' <summary>Average round time so far (ms), 0 before the first round.</summary>
        Public ReadOnly Property AverageRoundMs As Double
            Get
                Return If(_rounds = 0, 0, _totalMs / _rounds)
            End Get
        End Property

        Private _rounds As Integer
        Private _totalMs As Long

        ''' <summary>Starts the loop and returns at once. Call on the UI thread.</summary>
        Public Function StartAsync() As Task
            If _running Then Return Task.CompletedTask
            _running = True
            _stop = False
            _rounds = 0 : _totalMs = 0
            _activeLeader = -1
            BotLog.Add(Cat, $"Takip başladı: lider {LeaderIndex + 1}. hesap")
            _loop = LoopAsync()
            Return Task.CompletedTask
        End Function

        ''' <summary>Stops the loop, every follower lets the mouse go.</summary>
        Public Async Function StopAsync() As Task
            If Not _running Then Return
            _stop = True
            Dim l = _loop
            If l IsNot Nothing Then
                Try
                    Await l
                Catch
                End Try
            End If
        End Function

        Private Async Function LoopAsync() As Task
            Dim lastRoundMs As Long = TargetRoundMs
            _pace.Reset()
            Dim blockStart = FollowClock.NowMs
            Dim blockWork As Long = 0, blockSlow = 0
            While Not _stop
                Dim sw = Stopwatch.StartNew()
                _nextDelay = FollowPace.FastMs
                Try
                    Await RoundAsync(lastRoundMs)
                Catch ex As Exception
                    BotLog.FailOnce("Takip turu düştü", ex)
                    Say(-1, "Takip turu düştü: " & ex.Message)
                End Try
                lastRoundMs = sw.ElapsedMilliseconds
                _rounds += 1 : _totalMs += lastRoundMs
                blockWork += lastRoundMs
                If _nextDelay >= FollowPace.SlowMs Then blockSlow += 1
                Dim rest = _nextDelay - CInt(sw.ElapsedMilliseconds)
                If rest > 0 Then Await Task.Delay(rest)
                If _rounds Mod ReportEvery = 0 Then
                    Dim span = FollowClock.NowMs - blockStart
                    BotLog.Add(Cat, $"Tur işi ortalama {blockWork \ ReportEvery} ms, tur aralığı ortalama {span \ ReportEvery} ms (bekleme dahil), " &
                                    $"{blockSlow}/{ReportEvery} tur seyrek ({ParticipantCount()} hesap)")
                    blockStart = FollowClock.NowMs : blockWork = 0 : blockSlow = 0
                End If
            End While
            ' RoundAsync never throws out of the loop (caught above), so this always runs.
            For Each w In _walkers.Values.ToList()
                Await w.ReleaseAsync()
            Next
            _walkers.Clear()
            _filters.Clear()
            _lastKind.Clear()
            For Each h In _sizeHooks.Values
                RemoveHandler h.View.SizeChanged, h.Handler
            Next
            _sizeHooks.Clear()
            _canvas.Clear()
            _canvasPx.Clear()
            _frameSize.Clear()
            _misses.Clear()
            _running = False
            BotLog.Add(Cat, $"Takip durdu, takipçiler bırakıldı (tur ortalaması {If(_rounds = 0, 0, _totalMs \ _rounds)} ms)")
            Say(-1, "Takip durdu")
        End Function

        Private Function ParticipantCount() As Integer
            Return Enumerable.Count(_sessions, Function(s) InGame(s))
        End Function

        Private Shared Function InGame(s As GameSession) As Boolean
            Return s IsNot Nothing AndAlso s.State = SessionState.InGame AndAlso s.Ready
        End Function

        Private Async Function RoundAsync(lastRoundMs As Long) As Task
            Dim leader = LeaderIndex
            If leader <> _activeLeader Then Await SwitchLeaderAsync(_activeLeader, leader)

            ' Input mirror on: followers get the leader's clicks instead — no walking press in their views meanwhile.
            If FollowGate.MirrorActive Then
                For Each w In _walkers.Values
                    Await w.ReleaseAsync()
                Next
                If Not _pausedForMirror Then
                    _pausedForMirror = True
                    _lastKind.Clear()
                    BotLog.Add(Cat, "Takip duraklatıldı: girdi yansıtma açık")
                End If
                Say(-1, "Takip duraklatıldı (yansıtma açık)")
                _nextDelay = PausedDelayMs      ' nothing captured while paused
                _pace.Reset()
                Return
            End If
            If _pausedForMirror Then
                _pausedForMirror = False
                BotLog.Add(Cat, "Takip sürüyor: girdi yansıtma kapandı")
            End If

            ' Sessions that left the game (or never entered) let go and drop out.
            For Each idx In _walkers.Keys.ToList()
                If idx >= _sessions.Count OrElse Not InGame(_sessions(idx)) Then
                    Await _walkers(idx).ReleaseAsync()
                    _walkers.Remove(idx)
                    _filters.Remove(idx)
                    Report(idx, "left", $"{idx + 1}. hesap oyunda değil — takipten çıktı", True, False)
                End If
            Next

            Dim leaderSession = If(leader >= 0 AndAlso leader < _sessions.Count, _sessions(leader), Nothing)
            If Not InGame(leaderSession) Then
                For Each w In _walkers.Values
                    Await w.ReleaseAsync()
                Next
                Dim why = If(leaderSession Is Nothing, "lider seçilmedi", $"lider ({leader + 1}. hesap) oyunda değil")
                If why <> _leaderWaitSaid Then
                    _leaderWaitSaid = why
                    BotLog.Warn(Cat, $"Takipçiler bekliyor: {why}")
                End If
                Say(-1, $"Takipçiler bekliyor: {why}")
                _nextDelay = NoLeaderDelayMs
                _pace.Reset()
                Return
            End If
            If _leaderWaitSaid <> "" Then
                _leaderWaitSaid = ""
                BotLog.Add(Cat, $"Lider oyunda ({leader + 1}. hesap) — takip sürüyor")
            End If

            ' Coordinate crop of every in-game view, in parallel (canvas located first where not known yet).
            Dim parts = _sessions.Where(Function(s) InGame(s)).ToList()
            Await Task.WhenAll(parts.Where(Function(s) Not _canvas.ContainsKey(s.Index)).Select(Function(s) LocateCanvasAsync(s)))
            Dim shots = Await Task.WhenAll(parts.Select(Function(s) CaptureSafeAsync(s)))
            Dim pos As New Dictionary(Of Integer, Point?)
            Dim raw As New Dictionary(Of Integer, Point?)
            For k = 0 To parts.Count - 1
                Dim s = parts(k)
                Dim r As Point? = Nothing
                Using b = shots(k)
                    If b IsNot Nothing Then r = WebCoordReader.Read(b)
                End Using
                raw(s.Index) = r
                NoteRead(s.Index, r.HasValue)
                Dim f As PosFilter = Nothing
                If Not _filters.TryGetValue(s.Index, f) Then f = New PosFilter() : _filters(s.Index) = f
                pos(s.Index) = f.Update(r)
            Next
            Dim target = pos(leader)
            Say(leader, If(target.HasValue, $"lider ({target.Value.X},{target.Value.Y})", "lider: koordinat okunamıyor"))
            Report(leader, If(target.HasValue, "leader-ok", "leader-nocoord"),
                   If(target.HasValue, $"Lider ({leader + 1}. hesap) koordinatı okunuyor", $"Lider ({leader + 1}. hesap): koordinat okunamıyor — takipçiler bekliyor"),
                   True, Not target.HasValue)

            Dim lag = lastRoundMs + 80
            Dim settled = True
            Dim steps = parts.Where(Function(s) s.Index <> leader).Select(Async Function(s)
                                                                              Dim w = WalkerFor(s)
                                                                              w.LagMs = lag
                                                                              Dim st = Await w.TickAsync(pos(s.Index), target)
                                                                              If st.Kind <> FollowKind.Arrived AndAlso st.Kind <> FollowKind.TooFar Then settled = False
                                                                              OnStep(s.Index, st)
                                                                          End Function).ToArray()
            Await Task.WhenAll(steps)
            _nextDelay = _pace.NextDelay(target, settled, FollowClock.NowMs)
        End Function

        ''' <summary>Coordinate crop of one view; a frame of another size than when the canvas was found → find it again.</summary>
        Private Async Function CaptureSafeAsync(s As GameSession) As Task(Of Bitmap)
            Try
                Dim px As Rectangle
                If Not _canvasPx.TryGetValue(s.Index, px) Then px = Rectangle.Empty
                Dim res = Await FollowCapture.RegionAsync(s.Cw, WebCoordReader.RegionIn(px))
                Dim known As Size
                If _frameSize.TryGetValue(s.Index, known) AndAlso res.FrameSize <> Size.Empty AndAlso res.FrameSize <> known Then
                    _canvas.Remove(s.Index)
                End If
                Return res.Crop
            Catch
                Return Nothing
            End Try
        End Function

        ''' <summary>Located canvas, or the view's origin (old behaviour) while not located.</summary>
        Private Function CanvasOf(index As Integer) As Rectangle
            Dim r As Rectangle
            If _canvas.TryGetValue(index, r) Then Return r
            Return Rectangle.Empty
        End Function

        ''' <summary>One whole-view frame → where the game canvas is. Logged when it changes.</summary>
        Private Async Function LocateCanvasAsync(s As GameSession) As Task
            HookSize(s)
            _canvasAt(s.Index) = FollowClock.NowMs
            Dim css = CdpFollowInput.CssSize(s)
            Dim found As Rectangle
            Try
                Using frame = Await FollowCapture.FrameAsync(s.Cw)
                    If frame Is Nothing Then Return
                    Dim r = GameCanvas.LocateForCoords(frame)
                    _canvasPx(s.Index) = r
                    _frameSize(s.Index) = frame.Size
                    ' The whole-view frame is in device pixels; region / input are CSS pixels (equal at 100 % scale).
                    Dim k = If(frame.Width > 0, css.Width / frame.Width, 1.0)
                    found = New Rectangle(CInt(r.X * k), CInt(r.Y * k), CInt(r.Width * k), CInt(r.Height * k))
                End Using
            Catch ex As Exception
                BotLog.FailOnce($"Takip: {s.Number}. hesabın oyun alanı bulunamadı", ex)
                Return
            End Try
            Dim prev As Rectangle
            Dim had = _canvas.TryGetValue(s.Index, prev)
            _canvas(s.Index) = found
            _misses(s.Index) = 0
            If Not had OrElse prev <> found Then
                Dim isWhole = found.X = 0 AndAlso found.Y = 0 AndAlso found.Width >= css.Width - 2 AndAlso found.Height >= css.Height - 2
                BotLog.Add(Cat, If(isWhole,
                    $"{s.Number}. hesap: oyun görünümü dolduruyor ({css.Width}×{css.Height})",
                    $"{s.Number}. hesap: oyun görünümün içinde {found.Width}×{found.Height}, {found.X},{found.Y} konumunda (görünüm {css.Width}×{css.Height})"))
            End If
        End Function

        Private Sub HookSize(s As GameSession)
            If _sizeHooks.ContainsKey(s.Index) Then Return
            Dim idx = s.Index
            Dim h As EventHandler = Sub(sender, e) _canvas.Remove(idx)   ' located again at the next round
            AddHandler s.View.SizeChanged, h
            _sizeHooks(idx) = (s.View, h)
        End Sub

        ''' <summary>Coordinate unreadable for a while → maybe the canvas moved without a resize (page layout): look again.</summary>
        Private Sub NoteRead(index As Integer, ok As Boolean)
            Dim n As Integer = 0
            _misses.TryGetValue(index, n)
            n = If(ok, 0, n + 1)
            _misses(index) = n
            Dim at As Long = 0
            _canvasAt.TryGetValue(index, at)
            If n >= RelocateAfterMisses AndAlso FollowClock.NowMs - at >= RelocateMinGapMs Then
                _canvas.Remove(index)
                _misses(index) = 0
            End If
        End Sub

        Private Function WalkerFor(s As GameSession) As FollowWalker
            Dim w As FollowWalker = Nothing
            If Not _walkers.TryGetValue(s.Index, w) Then
                Dim idx = s.Index
                w = New FollowWalker(New CdpFollowInput(s, Function() CanvasOf(idx)))
                _walkers(s.Index) = w
            End If
            Return w
        End Function

        Private Async Function SwitchLeaderAsync(oldLeader As Integer, newLeader As Integer) As Task
            ' The new leader stops being a follower: let its mouse go and forget its walker.
            Dim w As FollowWalker = Nothing
            If _walkers.TryGetValue(newLeader, w) Then
                Await w.ReleaseAsync()
                _walkers.Remove(newLeader)
            End If
            _lastKind.Remove(newLeader)
            ' The old leader becomes a follower; its walker starts fresh (created on its first step).
            _walkers.Remove(oldLeader)
            _lastKind.Remove(oldLeader)
            _activeLeader = newLeader
            If oldLeader >= 0 Then
                BotLog.Add(Cat, $"Lider değişti: {oldLeader + 1} → {newLeader + 1}")
                Say(-1, $"Lider değişti: {oldLeader + 1}. hesap → {newLeader + 1}. hesap")
            End If
        End Function

        Private Sub OnStep(index As Integer, st As FollowStep)
            Dim n = index + 1
            Dim kind As String, line As String, warn As Boolean = False
            Select Case st.Kind
                Case FollowKind.Walking, FollowKind.Nudge, FollowKind.Settling
                    kind = "walk" : line = $"{n}. hesap lidere yürüyor"
                Case FollowKind.Arrived
                    kind = "arrived" : line = $"{n}. hesap liderin yanında"
                Case FollowKind.NoCoord
                    kind = "nocoord" : line = $"{n}. hesap: {st.Text}" : warn = True
                Case FollowKind.TooFar
                    kind = "far" : line = $"{n}. hesap: {st.Text}" : warn = True
                Case FollowKind.Camera
                    kind = "camera" : line = $"{n}. hesap: {st.Text}"
                Case FollowKind.Learning
                    kind = "learn" : line = $"{n}. hesap yönünü öğreniyor"
                Case FollowKind.Stuck
                    ' Same group as walking (no flapping), but the event itself is logged — at most every 10 s.
                    kind = "walk" : line = $"{n}. hesap lidere yürüyor"
                    Dim last As Long = Long.MinValue \ 2
                    _lastStuckLog.TryGetValue(index, last)
                    If FollowClock.NowMs - last > 10000 Then
                        _lastStuckLog(index) = FollowClock.NowMs
                        BotLog.Warn(Cat, $"{n}. hesap takıldı, yeniden yürütüldü")
                    End If
                Case Else
                    kind = "input" : line = $"{n}. hesap: {st.Text}" : warn = True
            End Select
            Report(index, kind, line, False, warn)
            Say(index, st.Text)
        End Sub

        ''' <summary>Writes the line only when this session's state group changes (last-state pattern).</summary>
        Private Sub Report(index As Integer, kind As String, line As String, sayAlso As Boolean, warn As Boolean)
            Dim prev As String = Nothing
            If _lastKind.TryGetValue(index, prev) AndAlso prev = kind Then Return
            _lastKind(index) = kind
            If warn Then BotLog.Warn(Cat, line) Else BotLog.Add(Cat, line)
            If sayAlso Then Say(index, line)
        End Sub

        ''' <summary>Status to the UI: on every text change, but the same session at most every 400 ms unless the text is new in kind.</summary>
        Private Sub Say(index As Integer, text As String)
            Dim now = FollowClock.NowMs
            Dim prev As (Text As String, T As Long) = Nothing
            If _lastStatus.TryGetValue(index, prev) Then
                If prev.Text = text Then Return
                If now - prev.T < StatusRepeatMs AndAlso SameShape(prev.Text, text) Then Return
            End If
            _lastStatus(index) = (text, now)
            RaiseEvent Status(index, text)
        End Sub

        ''' <summary>"lidere yürüyor (12,3 birim)" and "lidere yürüyor (11,8 birim)" — only the number differs.</summary>
        Private Shared Function SameShape(a As String, b As String) As Boolean
            Dim cut = Function(s As String) As String
                          Dim i = s.IndexOf("("c)
                          Return If(i < 0, s, s.Substring(0, i))
                      End Function
            Return cut(a) = cut(b)
        End Function
    End Class

End Namespace
