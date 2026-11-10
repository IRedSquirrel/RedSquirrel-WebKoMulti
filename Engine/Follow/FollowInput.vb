Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Input
Imports RedSquirrel.WebKoMulti.Core.Session

Namespace Engine.Follow

    ''' <summary>
    ''' What a <see cref="FollowWalker"/> needs from the outside world: where the game canvas is in the view, the left button,
    ''' a held key, and a clock. The real one goes into a game view through the DevTools protocol; the self-test
    ''' plugs in a virtual world (FollowSelfTest) so the walker's decisions can be run without the game.
    ''' </summary>
    Public Interface IFollowInput
        ''' <summary>Game canvas inside the view (input coordinates); the whole view when the view is the canvas.</summary>
        Function Canvas() As Rectangle
        Function MoveAsync(x As Integer, y As Integer, pressed As Boolean) As Task
        Function PressAsync(x As Integer, y As Integer) As Task
        Function ReleaseAsync(x As Integer, y As Integer) As Task
        Function HoldKeyAsync(key As String, code As String, vk As Integer, ms As Integer) As Task
        ReadOnly Property NowMs As Long
        Function DelayAsync(ms As Integer) As Task
    End Interface

    ''' <summary>Monotonic milliseconds (Environment.TickCount wraps after 24.8 days of uptime).</summary>
    Public Module FollowClock
        Private ReadOnly _sw As Stopwatch = Stopwatch.StartNew()
        Public ReadOnly Property NowMs As Long
            Get
                Return _sw.ElapsedMilliseconds
            End Get
        End Property
    End Module

    ''' <summary>Real input: one game session's view (Core.Input.CdpInput — no focus / foreground needed). UI thread only.</summary>
    Public NotInheritable Class CdpFollowInput
        Implements IFollowInput

        Private ReadOnly _s As GameSession
        Private ReadOnly _canvas As Func(Of Rectangle)

        ''' <param name="canvas">Located canvas (FollowController's cache); Nothing / empty → the whole view.</param>
        Public Sub New(session As GameSession, Optional canvas As Func(Of Rectangle) = Nothing)
            _s = session
            _canvas = canvas
        End Sub

        Public Function Canvas() As Rectangle Implements IFollowInput.Canvas
            Dim r = If(_canvas Is Nothing, Rectangle.Empty, _canvas())
            If r.Width > 0 AndAlso r.Height > 0 Then Return r
            Return New Rectangle(Point.Empty, CssSize(_s))
        End Function

        ''' <summary>
        ''' CdpInput takes CSS pixels; View.Size is device pixels. At 100 % display scale they are equal (all measurements so far);
        ''' other scales are divided by the view's DPI — not measured in the game.
        ''' </summary>
        Public Shared Function CssSize(s As GameSession) As Size
            Dim v = s.View
            Dim k = If(v.DeviceDpi > 0, v.DeviceDpi / 96.0, 1.0)
            Return New Size(CInt(v.Width / k), CInt(v.Height / k))
        End Function

        Public Async Function MoveAsync(x As Integer, y As Integer, pressed As Boolean) As Task Implements IFollowInput.MoveAsync
            If pressed Then
                Await CdpInput.Mouse(_s.Cw, "mouseMoved", x, y, "left", 1)
            Else
                Await CdpInput.Mouse(_s.Cw, "mouseMoved", x, y)
            End If
        End Function

        Public Async Function PressAsync(x As Integer, y As Integer) As Task Implements IFollowInput.PressAsync
            Await CdpInput.Mouse(_s.Cw, "mousePressed", x, y, "left", 1, 1)
        End Function

        Public Async Function ReleaseAsync(x As Integer, y As Integer) As Task Implements IFollowInput.ReleaseAsync
            Await CdpInput.Mouse(_s.Cw, "mouseReleased", x, y, "left", 0, 1)
        End Function

        ''' <summary>Key held like a real keyboard: keyDown, auto-repeat keyDowns every 33 ms, keyUp.</summary>
        Public Async Function HoldKeyAsync(key As String, code As String, vk As Integer, ms As Integer) As Task Implements IFollowInput.HoldKeyAsync
            Dim cw = _s.Cw
            Dim body = $"""key"":""{key}"",""code"":""{code}"",""windowsVirtualKeyCode"":{vk},""nativeVirtualKeyCode"":{vk}"
            Await cw.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", "{""type"":""rawKeyDown""," & body & "}")
            Dim sw = Stopwatch.StartNew()
            While sw.ElapsedMilliseconds < ms
                Await Task.Delay(33)
                Await cw.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", "{""type"":""rawKeyDown"",""autoRepeat"":true," & body & "}")
            End While
            Await cw.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", "{""type"":""keyUp""," & body & "}")
        End Function

        Public ReadOnly Property NowMs As Long Implements IFollowInput.NowMs
            Get
                Return FollowClock.NowMs
            End Get
        End Property

        Public Function DelayAsync(ms As Integer) As Task Implements IFollowInput.DelayAsync
            Return Task.Delay(ms)
        End Function
    End Class

End Namespace
