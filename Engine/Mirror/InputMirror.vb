Imports System.Collections.Concurrent
Imports System.Threading
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Native
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Core.Vision
Imports RedSquirrel.WebKoMulti.Engine.Follow
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Engine.Mirror

    ''' <summary>
    ''' INPUT MIRROR. While Enabled, the user's mouse clicks / drags / wheel inside the leader's game canvas and keys typed
    ''' into the leader's view are repeated in every other in-game session — same canvas-relative point (scaled onto each
    ''' follower's canvas), through the DevTools protocol.
    ''' • Capture: system-wide WH_MOUSE_LL + WH_KEYBOARD_LL hooks on the UI thread (the game view is a WebView2 in another
    '''   process; its input never reaches our window procedures). The hook only copies the event into a queue and passes
    '''   it on — nothing is swallowed, the leader gets its own input as always.
    ''' • No loop: what we send goes through Input.dispatch* straight into the followers' renderers, never through the
    '''   Windows input queue, so the hooks don't see it.
    ''' • Mode: KeyboardOnly (default) repeats keys only — following keeps running, no mouse hook, no capture;
    '''   KeyboardAndMouse also repeats clicks and pauses following (FollowGate): a held walking press would mix with them.
    ''' Must be created and switched on the UI thread.
    ''' </summary>
    Public NotInheritable Class InputMirror
        Implements IDisposable

        Private Const Cat As String = "yansitma"
        ''' <summary>Drag moves are thinned to one per this many ms (the last one before a release is always sent).</summary>
        Private Const MoveEveryMs As Long = 30
        Private Const DoubleClickMs As Long = 500

        Private ReadOnly _sessions As IList(Of GameSession)
        Private ReadOnly _leaderIndex As Func(Of Integer)

        ''' <summary>Overall state text (UI thread). Ex: "Yansıtma açık: 2 takipçi", "Lider oyunda değil".</summary>
        Public Event Status(text As String)
        ''' <summary>One mirrored action (UI thread), for a short live line. Ex: "Tık 640,412 → 2 hesap".</summary>
        Public Event Mirrored(text As String)

        Private _enabled As Boolean
        Private _ui As SynchronizationContext
        Private _mouseHook, _keyHook As IntPtr
        Private _mouseProc, _keyProc As HookNative.HookProc   ' kept alive while hooked
        Private ReadOnly _queue As New ConcurrentQueue(Of RawInput)
        Private _drainPosted As Integer
        Private _gestureActive As Boolean    ' read by the hook: moves are queued only during a drag

        ' Mouse gesture on the leader (between a button down and the last button up).
        Private _buttons As Integer
        Private _pendingMove As MirrorEvent?
        Private _lastMoveSent As Long
        Private _lastDown As (T As Long, Button As String, Rel As PointF)
        ' Keyboard.
        Private _shift, _ctrl, _alt, _altGr As Boolean
        ' Keys whose down we mirrored → their up is mirrored too, with the same key / code names as the down.
        Private ReadOnly _keysDown As New Dictionary(Of Integer, (Key As String, Code As String))

        ' Canvas per session (CSS pixels) and the follower send queues.
        Private ReadOnly _canvas As New Dictionary(Of Integer, Rectangle)
        Private ReadOnly _locating As New HashSet(Of Integer)
        Private ReadOnly _locateAt As New Dictionary(Of Integer, Long)
        Private ReadOnly _sizeHooks As New Dictionary(Of Integer, (View As Control, Handler As EventHandler))
        Private ReadOnly _send As New Dictionary(Of Integer, Queue(Of (Method As String, Json As String)))
        Private ReadOnly _pumping As New HashSet(Of Integer)
        Private ReadOnly _clock As Stopwatch = Stopwatch.StartNew()

        Private _count As Integer
        Private _failed As Integer

        Private Structure RawInput
            Public IsMouse As Boolean
            Public Msg As Integer
            Public X, Y As Integer
            Public Data As UInteger
            Public Vk, Scan As Integer
            Public Flags As UInteger
        End Structure

        Public Sub New(sessions As IList(Of GameSession), leaderIndex As Func(Of Integer))
            _sessions = sessions
            _leaderIndex = leaderIndex
        End Sub

        ''' <summary>Mirrored actions since switched on (clicks, drags, wheel notches, key presses).</summary>
        Public ReadOnly Property Count As Integer
            Get
                Return _count
            End Get
        End Property

        ''' <summary>Mouse hook installed (KeyboardAndMouse mode while enabled) — for follow-selftest.</summary>
        Friend ReadOnly Property MouseHooked As Boolean
            Get
                Return _mouseHook <> IntPtr.Zero
            End Get
        End Property

        Public Property Enabled As Boolean
            Get
                Return _enabled
            End Get
            Set(value As Boolean)
                If value = _enabled Then Return
                If value Then TurnOn() Else TurnOff()
            End Set
        End Property

        ''' <summary>
        ''' What is repeated. KeyboardOnly (default): the user plays the leader, followers walk after it (following keeps running)
        ''' and repeat its keys (skills); no mouse hook at all, no screen capture. KeyboardAndMouse: clicks / drags / wheel too, and
        ''' following pauses (a held walking press would mix with mirrored clicks). May change while on (UI thread).
        ''' </summary>
        Public Property Mode As MirrorMode
            Get
                Return _mode
            End Get
            Set(value As MirrorMode)
                If value = _mode Then Return
                _mode = value
                If _enabled Then
                    If value = MirrorMode.KeyboardAndMouse Then MouseOn() Else MouseOff()
                End If
                BotLog.Add(Cat, "Yansıtma kipi: " & ModeText(value))
                If _enabled Then RaiseEvent Status(StatusText())
            End Set
        End Property
        Private _mode As MirrorMode = MirrorMode.KeyboardOnly

        Private Shared Function ModeText(m As MirrorMode) As String
            Return If(m = MirrorMode.KeyboardOnly, "yalnız klavye", "klavye ve fare")
        End Function

        Private Function StatusText() As String
            If LeaderSession() Is Nothing Then Return "Yansıtma açık — lider oyunda değil"
            Return $"Yansıtma açık ({ModeText(_mode)}): {Followers().Count} takipçi"
        End Function

        Private Sub TurnOn()
            _ui = If(SynchronizationContext.Current, New WindowsFormsSynchronizationContext())
            _keyProc = AddressOf KeyHook
            _keyHook = HookNative.SetHook(HookNative.WH_KEYBOARD_LL, _keyProc)
            If _keyHook = IntPtr.Zero Then
                BotLog.Warn(Cat, "Yansıtma açılamadı: klavye kancası kurulamadı")
                RaiseEvent Status("Yansıtma açılamadı")
                Return
            End If
            _enabled = True
            _count = 0 : _failed = 0
            If _mode = MirrorMode.KeyboardAndMouse Then MouseOn()
            Dim n = Followers().Count
            Dim leader = _leaderIndex()
            BotLog.Add(Cat, $"Yansıtma açıldı ({ModeText(_mode)}): lider {leader + 1}. hesap, {n} takipçi")
            RaiseEvent Status(StatusText())
        End Sub

        ''' <summary>Mouse part on: hook, canvas of every view (one capture each), following paused.</summary>
        Private Sub MouseOn()
            If _mouseHook <> IntPtr.Zero Then Return
            _mouseProc = AddressOf MouseHook
            _mouseHook = HookNative.SetHook(HookNative.WH_MOUSE_LL, _mouseProc)
            If _mouseHook = IntPtr.Zero Then
                BotLog.Warn(Cat, "Fare yansıtılamıyor: fare kancası kurulamadı (klavye sürüyor)")
                RaiseEvent Status("Fare yansıtılamıyor")
                Return
            End If
            FollowGate.MirrorActive = True
            For Each s In InGame()
                EnsureCanvas(s)
            Next
        End Sub

        ''' <summary>Mouse part off: hook removed, a held drag released in the followers, following resumes.</summary>
        Private Sub MouseOff()
            HookNative.RemoveHook(_mouseHook)
            _mouseHook = IntPtr.Zero
            If _buttons <> 0 Then
                Dim up As New MirrorEvent With {.Kind = MirrorKind.MouseUp, .Rel = _lastDown.Rel, .Button = ButtonName(_buttons), .Buttons = 0, .ClickCount = 1}
                SendToFollowers(up)
                _buttons = 0
            End If
            _pendingMove = Nothing
            _gestureActive = False
            For Each h In _sizeHooks.Values
                RemoveHandler h.View.SizeChanged, h.Handler
            Next
            _sizeHooks.Clear()
            _canvas.Clear()
            FollowGate.MirrorActive = False
        End Sub

        Private Sub TurnOff()
            HookNative.RemoveHook(_keyHook)
            _keyHook = IntPtr.Zero
            MouseOff()
            ' Nothing must stay held in a follower: every mirrored key is released.
            For Each kv In _keysDown.ToList()
                SendToFollowers(New MirrorEvent With {.Kind = MirrorKind.KeyUp, .Vk = kv.Key, .Key = kv.Value.Key, .Code = kv.Value.Code})
            Next
            _keysDown.Clear()
            _enabled = False
            BotLog.Add(Cat, $"Yansıtma kapandı: {_count} olay aktarıldı{If(_failed > 0, $", {_failed} gönderim düştü", "")}")
            RaiseEvent Status("Yansıtma kapalı")
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If _enabled Then TurnOff()
        End Sub

        ' ---------------------------------------------------------------- hooks (keep them short)

        Private Function MouseHook(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
            If nCode >= 0 Then
                Dim msg = wParam.ToInt32()
                If msg <> HookNative.WM_MOUSEMOVE OrElse _gestureActive Then
                    Dim d = HookNative.MouseData(lParam)
                    Enqueue(New RawInput With {.IsMouse = True, .Msg = msg, .X = d.Pt.X, .Y = d.Pt.Y, .Data = d.MouseData, .Flags = d.Flags})
                End If
            End If
            Return HookNative.NextHook(_mouseHook, nCode, wParam, lParam)
        End Function

        Private Function KeyHook(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
            If nCode >= 0 Then
                Dim d = HookNative.KeyData(lParam)
                Enqueue(New RawInput With {.Msg = wParam.ToInt32(), .Vk = CInt(d.VkCode), .Scan = CInt(d.ScanCode), .Flags = d.Flags})
            End If
            Return HookNative.NextHook(_keyHook, nCode, wParam, lParam)
        End Function

        Private Sub Enqueue(r As RawInput)
            _queue.Enqueue(r)
            If Interlocked.Exchange(_drainPosted, 1) = 0 Then _ui.Post(AddressOf Drain, Nothing)
        End Sub

        ' ---------------------------------------------------------------- UI thread

        Private Sub Drain(state As Object)
            Interlocked.Exchange(_drainPosted, 0)
            Dim r As RawInput
            While _queue.TryDequeue(r)
                If Not _enabled Then Continue While
                Try
                    If r.IsMouse Then
                        If _mouseHook <> IntPtr.Zero Then OnMouse(r)
                    Else
                        OnKey(r)
                    End If
                Catch ex As Exception
                    BotLog.FailOnce("Yansıtma olayı işlenemedi", ex)
                End Try
            End While
            FlushMove(False)
        End Sub

        Private Sub OnMouse(r As RawInput)
            Dim leader = LeaderSession()
            Select Case r.Msg
                Case HookNative.WM_LBUTTONDOWN, HookNative.WM_RBUTTONDOWN, HookNative.WM_MBUTTONDOWN
                    If leader Is Nothing Then Return
                    Dim rel = LeaderRel(leader, r.X, r.Y, mustBeInside:=True)
                    If Not rel.HasValue Then Return
                    Dim btn = If(r.Msg = HookNative.WM_LBUTTONDOWN, "left", If(r.Msg = HookNative.WM_RBUTTONDOWN, "right", "middle"))
                    Dim now = _clock.ElapsedMilliseconds
                    Dim clicks = 1
                    If _lastDown.Button = btn AndAlso now - _lastDown.T <= DoubleClickMs AndAlso
                       Math.Abs(_lastDown.Rel.X - rel.Value.X) <= 4 AndAlso Math.Abs(_lastDown.Rel.Y - rel.Value.Y) <= 4 Then clicks = 2
                    _lastDown = (now, btn, rel.Value)
                    If _buttons = 0 Then
                        ' Cursor to the spot first (games act on the press position).
                        SendToFollowers(New MirrorEvent With {.Kind = MirrorKind.MouseMove, .Rel = rel.Value, .Buttons = 0})
                    End If
                    _buttons = _buttons Or ButtonBit(btn)
                    _gestureActive = True
                    SendToFollowers(New MirrorEvent With {.Kind = MirrorKind.MouseDown, .Rel = rel.Value, .Button = btn, .Buttons = _buttons,
                                                          .ClickCount = clicks, .Modifiers = Mods()})
                Case HookNative.WM_MOUSEMOVE
                    If _buttons = 0 OrElse leader Is Nothing Then Return
                    Dim rel = LeaderRel(leader, r.X, r.Y, mustBeInside:=False)
                    If Not rel.HasValue Then Return
                    _pendingMove = New MirrorEvent With {.Kind = MirrorKind.MouseMove, .Rel = rel.Value, .Buttons = _buttons, .Modifiers = Mods()}
                    FlushMove(False)
                Case HookNative.WM_LBUTTONUP, HookNative.WM_RBUTTONUP, HookNative.WM_MBUTTONUP
                    Dim btn = If(r.Msg = HookNative.WM_LBUTTONUP, "left", If(r.Msg = HookNative.WM_RBUTTONUP, "right", "middle"))
                    If (_buttons And ButtonBit(btn)) = 0 Then Return      ' its press was not mirrored
                    FlushMove(True)
                    Dim rel As PointF? = If(leader Is Nothing, Nothing, LeaderRel(leader, r.X, r.Y, mustBeInside:=False))
                    _buttons = _buttons And Not ButtonBit(btn)
                    _gestureActive = _buttons <> 0
                    Dim at = If(rel, _lastDown.Rel)
                    SendToFollowers(New MirrorEvent With {.Kind = MirrorKind.MouseUp, .Rel = at, .Button = btn, .Buttons = _buttons,
                                                          .ClickCount = If(_lastDown.Button = btn, 1, 1), .Modifiers = Mods()})
                    Dim dragged = Math.Abs(at.X - _lastDown.Rel.X) + Math.Abs(at.Y - _lastDown.Rel.Y) > 6
                    _count += 1
                    Say($"{If(dragged, "Sürükle", If(btn = "right", "Sağ tık", "Tık"))} {CInt(at.X)},{CInt(at.Y)}")
                Case HookNative.WM_MOUSEWHEEL
                    If leader Is Nothing Then Return
                    Dim rel = LeaderRel(leader, r.X, r.Y, mustBeInside:=True)
                    If Not rel.HasValue Then Return
                    Dim delta = CShort((r.Data >> 16) And &HFFFFUI)
                    SendToFollowers(New MirrorEvent With {.Kind = MirrorKind.Wheel, .Rel = rel.Value, .WheelDelta = delta, .Buttons = _buttons, .Modifiers = Mods()})
                    _count += 1
                    Say($"Tekerlek {If(delta > 0, "yukarı", "aşağı")}")
            End Select
        End Sub

        Private Sub FlushMove(force As Boolean)
            If Not _pendingMove.HasValue Then Return
            Dim now = _clock.ElapsedMilliseconds
            If Not force AndAlso now - _lastMoveSent < MoveEveryMs Then Return
            SendToFollowers(_pendingMove.Value)
            _pendingMove = Nothing
            _lastMoveSent = now
        End Sub

        Private Sub OnKey(r As RawInput)
            Dim down = r.Msg = HookNative.WM_KEYDOWN OrElse r.Msg = HookNative.WM_SYSKEYDOWN
            Dim extended = (r.Flags And 1UI) <> 0
            ' Modifier state is tracked from every key, wherever it was typed.
            Select Case r.Vk
                Case &H10, &HA0, &HA1 : _shift = down
                Case &H11, &HA2, &HA3 : _ctrl = down
                Case &H12, &HA4 : _alt = down
                Case &HA5 : _altGr = down
            End Select
            If down Then
                Dim leader = LeaderSession()
                If leader Is Nothing OrElse Not KeysGoToLeader(leader) Then Return
                Dim text = If(_ctrl AndAlso Not _altGr OrElse _alt AndAlso Not _altGr, "",
                              HookNative.KeyText(r.Vk, r.Scan, _shift, _altGr, Control.IsKeyLocked(Keys.CapsLock)))
                Dim names = MirrorMap.KeyNames(r.Vk, extended, text)
                Dim rep = _keysDown.ContainsKey(r.Vk)
                _keysDown(r.Vk) = (names.Key, names.Code)
                SendToFollowers(New MirrorEvent With {.Kind = MirrorKind.KeyDown, .Vk = r.Vk, .Key = names.Key, .Code = names.Code, .Text = text,
                                                      .AutoRepeat = rep, .Modifiers = Mods()})
                If Not rep Then
                    _count += 1
                    Say($"Tuş {If(names.Key = " ", "Space", names.Key)}")
                End If
            Else
                Dim names As (Key As String, Code As String) = Nothing
                If Not _keysDown.TryGetValue(r.Vk, names) Then Return     ' only ups of downs we mirrored
                _keysDown.Remove(r.Vk)
                SendToFollowers(New MirrorEvent With {.Kind = MirrorKind.KeyUp, .Vk = r.Vk, .Key = names.Key, .Code = names.Code, .Modifiers = Mods()})
            End If
        End Sub

        Private Function Mods() As Integer
            Return MirrorMap.Modifiers(_alt AndAlso Not _altGr, _ctrl AndAlso Not _altGr, _shift)
        End Function

        ''' <summary>Keys count for the leader only when its window is in front and focus is inside its view.</summary>
        Private Shared Function KeysGoToLeader(leader As GameSession) As Boolean
            Dim v = leader.View
            Dim form = v.FindForm()
            If form Is Nothing OrElse HookNative.Foreground() <> form.Handle Then Return False
            Return v.ContainsFocus OrElse HookNative.IsSameOrChild(v.Handle, HookNative.ForegroundFocus())
        End Function

        ''' <summary>
        ''' Screen point → canvas-relative point on the leader, or Nothing when the point is not on the leader's view (another
        ''' window on top, outside the view) or — for a press / wheel — outside its game canvas.
        ''' </summary>
        Private Function LeaderRel(leader As GameSession, x As Integer, y As Integer, mustBeInside As Boolean) As PointF?
            Dim v = leader.View
            Dim form = v.FindForm()
            If form Is Nothing Then Return Nothing
            If mustBeInside AndAlso HookNative.RootAt(x, y) <> form.Handle Then Return Nothing
            Dim client = v.PointToClient(New Point(x, y))
            If mustBeInside AndAlso Not New Rectangle(Point.Empty, v.ClientSize).Contains(client) Then Return Nothing
            Dim k = If(v.DeviceDpi > 0, v.DeviceDpi / 96.0, 1.0)
            Dim css = New PointF(CSng(client.X / k), CSng(client.Y / k))
            Dim cv = CanvasOf(leader)
            If mustBeInside Then Return MirrorMap.ToCanvas(css, cv)
            Return MirrorMap.ToCanvasClamped(css, cv)
        End Function

        ' ---------------------------------------------------------------- followers

        Private Sub SendToFollowers(ev As MirrorEvent)
            Dim leader = LeaderSession()
            If leader Is Nothing Then Return
            Dim isKey = ev.Kind = MirrorKind.KeyDown OrElse ev.Kind = MirrorKind.KeyUp
            Dim lc = If(isKey, Size.Empty, CanvasOf(leader).Size)   ' keys have no point → no canvas, no capture
            For Each f In Followers()
                Dim at = If(isKey, Point.Empty, MirrorMap.ToFollower(ev.Rel, lc, CanvasOf(f)))
                Dim call_ = MirrorMap.ToCdp(ev, at)
                Dim q As Queue(Of (Method As String, Json As String)) = Nothing
                If Not _send.TryGetValue(f.Index, q) Then q = New Queue(Of (Method As String, Json As String)) : _send(f.Index) = q
                q.Enqueue(call_)
                If _pumping.Add(f.Index) Then Pump(f)
            Next
        End Sub

        ''' <summary>One queue per follower, sent in order, one call at a time.</summary>
        Private Async Sub Pump(f As GameSession)
            Dim q = _send(f.Index)
            Try
                While q.Count > 0
                    Dim c = q.Dequeue()
                    Try
                        Await f.Cw.CallDevToolsProtocolMethodAsync(c.Method, c.Json)
                    Catch ex As Exception
                        _failed += 1
                        BotLog.FailOnce($"Yansıtma: {f.Number}. hesaba gönderilemedi", ex)
                    End Try
                End While
            Finally
                _pumping.Remove(f.Index)
            End Try
        End Sub

        Private Function LeaderSession() As GameSession
            Dim i = _leaderIndex()
            If i < 0 OrElse i >= _sessions.Count Then Return Nothing
            Dim s = _sessions(i)
            Return If(IsInGame(s), s, Nothing)
        End Function

        Private Function Followers() As List(Of GameSession)
            Dim l = _leaderIndex()
            Return _sessions.Where(Function(s) s.Index <> l AndAlso IsInGame(s)).ToList()
        End Function

        Private Function InGame() As List(Of GameSession)
            Return _sessions.Where(Function(s) IsInGame(s)).ToList()
        End Function

        Private Shared Function IsInGame(s As GameSession) As Boolean
            Return s IsNot Nothing AndAlso s.State = SessionState.InGame AndAlso s.Ready
        End Function

        ' ---------------------------------------------------------------- canvas (game area inside each view)

        ''' <summary>Located canvas in CSS pixels; the whole view until it is found.</summary>
        Private Function CanvasOf(s As GameSession) As Rectangle
            Dim r As Rectangle
            If _canvas.TryGetValue(s.Index, r) Then Return r
            EnsureCanvas(s)
            Return New Rectangle(Point.Empty, CdpFollowInput.CssSize(s))
        End Function

        Private Sub EnsureCanvas(s As GameSession)
            If _canvas.ContainsKey(s.Index) Then Return
            ' One look per view at most every 5 s: a failing lookup must not capture on every click.
            Dim last As Long
            If _locateAt.TryGetValue(s.Index, last) AndAlso _clock.ElapsedMilliseconds - last < 5000 Then Return
            If Not _locating.Add(s.Index) Then Return
            _locateAt(s.Index) = _clock.ElapsedMilliseconds
            If Not _sizeHooks.ContainsKey(s.Index) Then
                Dim idx = s.Index
                Dim h As EventHandler = Sub(sender, e)
                                            _canvas.Remove(idx)
                                            _locateAt.Remove(idx)
                                        End Sub
                AddHandler s.View.SizeChanged, h
                _sizeHooks(idx) = (s.View, h)
            End If
            LocateAsync(s)
        End Sub

        Private Async Sub LocateAsync(s As GameSession)
            Try
                Using frame = Await FollowCapture.FrameAsync(s.Cw)
                    If frame Is Nothing OrElse Not _enabled Then Return
                    Dim r = GameCanvas.LocateForCoords(frame)
                    Dim css = CdpFollowInput.CssSize(s)
                    Dim k = If(frame.Width > 0, css.Width / frame.Width, 1.0)
                    _canvas(s.Index) = New Rectangle(CInt(r.X * k), CInt(r.Y * k), CInt(r.Width * k), CInt(r.Height * k))
                End Using
            Catch ex As Exception
                BotLog.FailOnce($"Yansıtma: {s.Number}. hesabın oyun alanı bulunamadı", ex)
            Finally
                _locating.Remove(s.Index)
            End Try
        End Sub

        Private Sub Say(what As String)
            RaiseEvent Mirrored($"{what} → {Followers().Count} hesap")
        End Sub

        Private Shared Function ButtonBit(b As String) As Integer
            Return If(b = "left", 1, If(b = "right", 2, 4))
        End Function

        Private Shared Function ButtonName(bits As Integer) As String
            Return If((bits And 1) <> 0, "left", If((bits And 2) <> 0, "right", "middle"))
        End Function

    End Class

End Namespace
