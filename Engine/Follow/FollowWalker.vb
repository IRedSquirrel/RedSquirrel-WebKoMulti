Imports System.Threading.Tasks

Namespace Engine.Follow

    ''' <summary>What one walker step decided — Kind drives the log (only changes are written), Text is for the screen.</summary>
    Public Enum FollowKind
        NoCoord
        Camera
        Learning
        Walking
        Settling
        Nudge
        Stuck
        Arrived
        TooFar
        InputFailed
    End Enum

    Public Structure FollowStep
        Public Kind As FollowKind
        Public Text As String
        Public Distance As Double
        Public Sub New(kind As FollowKind, text As String, Optional distance As Double = Double.NaN)
            Me.Kind = kind : Me.Text = text : Me.Distance = distance
        End Sub
    End Structure

    ''' <summary>
    ''' Walks one follower view toward a target world point (the leader's coordinate), click-and-hold walk model:
    ''' the left button is held inside the view and the cursor sits on a circle around the character; KO walks toward the
    ''' cursor. screenAngle = mirror × worldAngle + theta. Camera yaw (theta) is learned from one probe walk (screen up)
    ''' and corrected every step from the real movement. Mirror is +1 (every good learn in the measurements was +1).
    ''' The outside world (mouse / key / clock) comes through <see cref="IFollowInput"/>.
    ''' </summary>
    Public NotInheritable Class FollowWalker

        ' Cursor circle around the character, as a share of the GAME CANVAS (centre 0.5 / 0.56, radius 0.17 × height) — in a
        ' view larger than the game the canvas is a 1280×800 rectangle in the middle (Core.Vision.GameCanvas).
        Private Const CenterX As Double = 0.5, CenterY As Double = 0.56, Radius As Double = 0.17

        Public Property ArriveDistance As Double = 2.0
        ''' <summary>Start walking again only past this (arrive at ArriveDistance) — no stop / go flicker at the edge.</summary>
        Public Property ResumeDistance As Double = 4.0
        ''' <summary>Closer than this while standing → short steps into the leader instead of a run.</summary>
        Public Property NudgeRange As Double = 5.0
        ''' <summary>Farther than this → the leader is elsewhere (another zone / far teleport): don't follow.</summary>
        Public Property MaxFollowDistance As Double = 200.0
        ''' <summary>How late a decision reaches the game (round time + margin), set by the caller every round.</summary>
        Public Property LagMs As Double = 250

        Private Const SettleMs As Integer = 300
        Private Const MaxNudges As Integer = 2
        Private Const StuckMs As Integer = 1200
        Private Const ProbeRetryMs As Integer = 2500
        Private Const TopDownHoldMs As Integer = 1500
        Private Const TopDownRepeatMs As Integer = 400
        Private Const StopTapMs As Integer = 40
        Private Const JumpUnits As Double = 25

        Private ReadOnly _in As IFollowInput
        Private _pressed As Boolean
        Private _mirror As Integer = 1
        Private _theta As Double = Double.NaN
        Private _probe As Integer           ' 0 = none yet, 1 = walking screen-up
        Private _probeStart As Point?
        Private _probeT0 As Long
        Private _screenAngle As Double
        Private _lastPos As Point?
        Private _settleFrom As Long = Long.MinValue \ 2
        Private _nudges As Integer
        Private _speed As Double
        Private _speedRef As Point?
        Private _speedT As Long
        Private _arrived As Boolean
        Private _lastMoveT As Long
        Private _moveRef As Point?
        Private _topDown As Boolean
        Private _prevPos As Point?

        Public Sub New(input As IFollowInput)
            _in = input
            _lastMoveT = input.NowMs
        End Sub

        ''' <summary>Learned camera yaw in degrees, NaN until the probe walk finished.</summary>
        Public ReadOnly Property Theta As Double
            Get
                Return _theta
            End Get
        End Property

        Public ReadOnly Property Pressed As Boolean
            Get
                Return _pressed
            End Get
        End Property

        Public ReadOnly Property State As String
            Get
                If Double.IsNaN(_theta) Then Return If(_probe = 0, "yön öğrenilecek", "yön öğreniliyor")
                Return $"kamera {_theta:0}°"
            End Get
        End Property

        ''' <summary>One step (call every round). me_ / target come through a PosFilter (short gaps already bridged).</summary>
        Public Async Function TickAsync(me_ As Point?, target As Point?) As Task(Of FollowStep)
            Try
                Return Await TickCoreAsync(me_, target)
            Catch ex As Exception
                ' A failed mouse call: forget "pressed" so the next step presses again instead of only moving the cursor.
                _pressed = False
                Return New FollowStep(FollowKind.InputFailed, "fare komutu gitmedi, yeniden basılacak: " & ex.Message)
            End Try
        End Function

        Private Async Function TickCoreAsync(me_ As Point?, target As Point?) As Task(Of FollowStep)
            If Not me_.HasValue OrElse Not target.HasValue Then
                Await ReleaseAsync()
                Return New FollowStep(FollowKind.NoCoord, If(Not me_.HasValue, "koordinat okunamıyor — bekliyor", "liderin koordinatı okunamıyor — bekliyor"))
            End If
            Dim jumped = _prevPos.HasValue AndAlso Distance(_prevPos.Value, me_.Value) > JumpUnits
            _prevPos = me_
            ' Top-down camera: seen from above, screen ↔ world is a pure rotation.
            ' Again after a teleport / town — the loading can turn the camera back.
            If Not _topDown OrElse jumped Then
                Await TopDownAsync()
                _lastMoveT = _in.NowMs
                Return New FollowStep(FollowKind.Camera, If(jumped, "ışınlandı — kamera yeniden tepeye alındı", "kamera tepeye alındı"))
            End If
            Dim dx = target.Value.X - me_.Value.X, dy = target.Value.Y - me_.Value.Y
            Dim dist = Math.Sqrt(dx * dx + dy * dy)
            If dist > MaxFollowDistance Then
                Await ReleaseAsync()
                Return New FollowStep(FollowKind.TooFar, $"lider çok uzak ({dist:0} birim) — takip etmiyor", dist)
            End If
            Dim now = _in.NowMs
            ' Moved since the last check? (watchdog for a press the game lost)
            If Not _moveRef.HasValue OrElse Distance(_moveRef.Value, me_.Value) >= 1 Then
                _moveRef = me_
                _lastMoveT = now
            End If

            ' --- learning the camera: one probe walk toward screen-up ---
            If Double.IsNaN(_theta) Then
                If _probe = 0 Then
                    _probe = 1 : _probeStart = me_ : _probeT0 = now
                    Await PressAtAsync(90)
                    Return New FollowStep(FollowKind.Learning, "yön öğreniliyor: ekranın yukarısına yürüyor", dist)
                End If
                Dim moved = Distance(_probeStart.Value, me_.Value)
                If moved < 2.5 Then
                    If now - _probeT0 > ProbeRetryMs Then  ' did not move: try again from here
                        _probeStart = me_ : _probeT0 = now
                        Await ReleaseAsync()
                        Await PressAtAsync(90)
                    End If
                    Return New FollowStep(FollowKind.Learning, "yön öğreniliyor: hareket bekleniyor", dist)
                End If
                Dim worldAngle = Math.Atan2(me_.Value.Y - _probeStart.Value.Y, me_.Value.X - _probeStart.Value.X) * 180 / Math.PI
                _theta = Norm(90 - _mirror * worldAngle)
                Await StopAsync()
                _settleFrom = now
                _lastPos = me_
            End If

            ' Measured running speed (units / ms) → stop that much earlier: the release reaches the game one round late.
            If _speedRef.HasValue Then
                Dim dt = now - _speedT
                If dt >= 80 Then
                    Dim v = Distance(_speedRef.Value, me_.Value) / dt
                    _speed = If(_pressed, 0.6 * _speed + 0.4 * v, 0.5 * _speed)
                    _speedRef = me_ : _speedT = now
                End If
            Else
                _speedRef = me_ : _speedT = now
            End If
            Dim stopAt = ArriveDistance + Math.Min(8.0, _speed * LagMs)
            Dim bearing = Math.Atan2(dy, dx) * 180 / Math.PI
            ' After letting go / a short step the character still slides and the coordinate updates late — wait for it.
            If Not _pressed AndAlso now - _settleFrom < SettleMs Then
                Return New FollowStep(FollowKind.Settling, $"duruluyor ({dist:0.0} birim)", dist)
            End If
            ' Inside the leader, or close and out of short steps: stand. Only a real move of the leader (past
            ' ResumeDistance) starts a new approach — no hunting for the exact coordinate.
            If dist <= ArriveDistance OrElse (_arrived AndAlso dist <= ResumeDistance) OrElse
               (Not _pressed AndAlso dist <= NudgeRange AndAlso _nudges >= MaxNudges) Then
                _arrived = True
                If _pressed Then Await StopAsync()
                _lastPos = me_
                _lastMoveT = now
                Return New FollowStep(FollowKind.Arrived, $"liderin yanında ({dist:0.0} birim)", dist)
            End If
            If _arrived Then _arrived = False : _nudges = 0
            ' Running and the leader is within the stopping distance: let go now — the character slides the last bit.
            If _pressed AndAlso dist <= stopAt Then
                Await StopAsync()
                _settleFrom = now
                _lastPos = me_
                _lastMoveT = now
                Return New FollowStep(FollowKind.Settling, $"yaklaşırken bıraktı ({dist:0.0} birim)", dist)
            End If
            ' Standing close but not inside: one short step (at most MaxNudges per approach), then let it settle.
            If Not _pressed AndAlso dist <= NudgeRange Then
                Dim ms = CInt(Math.Min(220, 70 + 30 * dist))
                Await PressAtAsync(Norm(_mirror * bearing + _theta))
                Await _in.DelayAsync(ms)
                Await StopAsync()
                _nudges += 1
                _settleFrom = _in.NowMs
                _lastPos = me_
                _lastMoveT = _in.NowMs
                Return New FollowStep(FollowKind.Nudge, $"kısa adım {_nudges}/{MaxNudges} ({dist:0.0} birim)", dist)
            End If
            ' Walking but not moving for 1.2 s: the game dropped the press (or the cursor sat on UI) → press again.
            If _pressed AndAlso now - _lastMoveT > StuckMs Then
                Await ReleaseAsync()
                _lastMoveT = now
                Await PressAtAsync(Norm(_mirror * bearing + _theta))
                Return New FollowStep(FollowKind.Stuck, $"takıldı, yeniden basıldı ({dist:0.0} birim)", dist)
            End If
            ' --- closed loop: correct theta from the movement since the reference point ---
            ' The reference is kept until the character has moved 1.5 units: at ~120 ms rounds one round is only ~0.4
            ' units, so comparing round to round never reached the threshold and the correction never ran.
            If _pressed AndAlso _lastPos.HasValue Then
                Dim m = Distance(_lastPos.Value, me_.Value)
                If m >= 1.5 Then
                    Dim obs = Math.Atan2(me_.Value.Y - _lastPos.Value.Y, me_.Value.X - _lastPos.Value.X) * 180 / Math.PI
                    Dim est = Norm(_screenAngle - _mirror * obs)
                    _theta = Norm(_theta + 0.35 * Norm(est - _theta))
                    _lastPos = me_
                End If
            Else
                _lastPos = me_
            End If
            Await PressAtAsync(Norm(_mirror * bearing + _theta))
            Return New FollowStep(FollowKind.Walking, $"lidere yürüyor ({dist:0.0} birim)", dist)
        End Function

        ''' <summary>Let go and tap S: the character stops on the spot instead of sliding on (user, 9 Nov).</summary>
        Private Async Function StopAsync() As Task
            Await ReleaseAsync()
            Await _in.HoldKeyAsync("s", "KeyS", 83, StopTapMs)
        End Function

        Private Async Function TopDownAsync() As Task
            Await ReleaseAsync()
            Await _in.HoldKeyAsync("Home", "Home", 36, TopDownHoldMs)
            Await _in.DelayAsync(150)
            Await _in.HoldKeyAsync("Home", "Home", 36, TopDownRepeatMs)   ' the first key can be lost
            Await _in.DelayAsync(200)
            _topDown = True
        End Function

        ''' <summary>Cursor point on the circle at a screen angle (0 = right, 90 = up), relative to the game canvas.</summary>
        Public Shared Function CirclePoint(canvas As Rectangle, angle As Double) As Point
            Dim r = canvas.Height * Radius
            Return New Point(canvas.X + CInt(canvas.Width * CenterX + r * Math.Cos(angle * Math.PI / 180)),
                             canvas.Y + CInt(canvas.Height * CenterY - r * Math.Sin(angle * Math.PI / 180)))
        End Function

        ''' <summary>The character's screen spot (circle centre) on the game canvas.</summary>
        Public Shared Function CircleCenter(canvas As Rectangle) As Point
            Return New Point(canvas.X + canvas.Width \ 2, canvas.Y + CInt(canvas.Height * CenterY))
        End Function

        Private Async Function PressAtAsync(angle As Double) As Task
            _screenAngle = angle
            Dim p = CirclePoint(_in.Canvas(), angle)
            If Not _pressed Then
                Await _in.MoveAsync(p.X, p.Y, False)
                Await _in.PressAsync(p.X, p.Y)
                _pressed = True
            Else
                Await _in.MoveAsync(p.X, p.Y, True)
            End If
        End Function

        ''' <summary>Lets the left button go (no-op when not held). Never throws.</summary>
        Public Async Function ReleaseAsync() As Task
            If Not _pressed Then Return
            _pressed = False
            Dim c = CircleCenter(_in.Canvas())
            Try
                Await _in.ReleaseAsync(c.X, c.Y)
            Catch
            End Try
        End Function

        Private Shared Function Distance(a As Point, b As Point) As Double
            Return Math.Sqrt((a.X - b.X) ^ 2 + (a.Y - b.Y) ^ 2)
        End Function

        Friend Shared Function Norm(a As Double) As Double
            a = a Mod 360
            If a > 180 Then a -= 360
            If a <= -180 Then a += 360
            Return a
        End Function

    End Class

End Namespace
