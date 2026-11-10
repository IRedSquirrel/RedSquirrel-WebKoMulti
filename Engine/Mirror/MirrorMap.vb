Imports System.Globalization

Namespace Engine.Mirror

    ''' <summary>What the input mirror repeats in the followers.</summary>
    Public Enum MirrorMode
        ''' <summary>Keys only; following keeps running (the user plays the leader, followers walk after it).</summary>
        KeyboardOnly
        ''' <summary>Keys + clicks / drags / wheel; following pauses meanwhile.</summary>
        KeyboardAndMouse
    End Enum

    Public Enum MirrorKind
        MouseDown
        MouseMove
        MouseUp
        Wheel
        KeyDown
        KeyUp
    End Enum

    ''' <summary>
    ''' One user action on the leader, already in CANVAS-relative coordinates (0,0 = the game canvas' top-left, in the leader
    ''' canvas' CSS pixels). Followers get it scaled onto their own canvas.
    ''' </summary>
    Public Structure MirrorEvent
        Public Kind As MirrorKind
        Public Rel As PointF
        ''' <summary>"left" / "right" / "middle" / "none".</summary>
        Public Button As String
        ''' <summary>Buttons held after the event (CDP bitmask: 1 left, 2 right, 4 middle).</summary>
        Public Buttons As Integer
        Public ClickCount As Integer
        ''' <summary>Windows wheel delta (+120 = away from the user).</summary>
        Public WheelDelta As Integer
        Public Vk As Integer
        Public Key As String
        Public Code As String
        Public Text As String
        Public AutoRepeat As Boolean
        ''' <summary>CDP modifiers: 1 Alt, 2 Ctrl, 4 Meta, 8 Shift.</summary>
        Public Modifiers As Integer
    End Structure

    ''' <summary>
    ''' Side-effect-free part of the input mirror: screen / view / canvas point conversions and the DevTools protocol JSON.
    ''' The hook and the sending live in InputMirror; follow-selftest checks this module.
    ''' </summary>
    Public Module MirrorMap

        ''' <summary>
        ''' A point in the leader view's client area (CSS pixels) → canvas-relative point, or Nothing when outside the canvas.
        ''' </summary>
        Public Function ToCanvas(clientCss As PointF, canvasCss As Rectangle) As PointF?
            Dim rx = clientCss.X - canvasCss.X, ry = clientCss.Y - canvasCss.Y
            If rx < 0 OrElse ry < 0 OrElse rx >= canvasCss.Width OrElse ry >= canvasCss.Height Then Return Nothing
            Return New PointF(rx, ry)
        End Function

        ''' <summary>Same, without the inside test (a drag that leaves the canvas keeps going, clamped at its edge).</summary>
        Public Function ToCanvasClamped(clientCss As PointF, canvasCss As Rectangle) As PointF
            Dim rx = Math.Max(0F, Math.Min(canvasCss.Width - 1.0F, clientCss.X - canvasCss.X))
            Dim ry = Math.Max(0F, Math.Min(canvasCss.Height - 1.0F, clientCss.Y - canvasCss.Y))
            Return New PointF(rx, ry)
        End Function

        ''' <summary>
        ''' Canvas-relative point of the leader → a point in a follower view (CSS pixels). Different canvas sizes (a smaller
        ''' view draws the game smaller) are scaled; a canvas centred in a bigger view is offset.
        ''' </summary>
        Public Function ToFollower(rel As PointF, leaderCanvas As Size, followerCanvas As Rectangle) As Point
            Dim sx = If(leaderCanvas.Width > 0, followerCanvas.Width / CDbl(leaderCanvas.Width), 1.0)
            Dim sy = If(leaderCanvas.Height > 0, followerCanvas.Height / CDbl(leaderCanvas.Height), 1.0)
            Dim x = followerCanvas.X + CInt(Math.Round(rel.X * sx))
            Dim y = followerCanvas.Y + CInt(Math.Round(rel.Y * sy))
            x = Math.Max(followerCanvas.X, Math.Min(followerCanvas.Right - 1, x))
            y = Math.Max(followerCanvas.Y, Math.Min(followerCanvas.Bottom - 1, y))
            Return New Point(x, y)
        End Function

        ''' <summary>The DevTools protocol call that replays the event at a follower point.</summary>
        Public Function ToCdp(ev As MirrorEvent, at As Point) As (Method As String, Json As String)
            Select Case ev.Kind
                Case MirrorKind.MouseDown, MirrorKind.MouseUp, MirrorKind.MouseMove
                    Dim type = If(ev.Kind = MirrorKind.MouseDown, "mousePressed", If(ev.Kind = MirrorKind.MouseUp, "mouseReleased", "mouseMoved"))
                    Dim button = If(ev.Kind = MirrorKind.MouseMove, If((ev.Buttons And 1) <> 0, "left", If((ev.Buttons And 2) <> 0, "right", If((ev.Buttons And 4) <> 0, "middle", "none"))), ev.Button)
                    Dim cc = If(ev.Kind = MirrorKind.MouseMove, 0, Math.Max(1, ev.ClickCount))
                    Return ("Input.dispatchMouseEvent",
                            $"{{""type"":""{type}"",""x"":{at.X},""y"":{at.Y},""button"":""{button}"",""buttons"":{ev.Buttons},""clickCount"":{cc},""modifiers"":{ev.Modifiers}}}")
                Case MirrorKind.Wheel
                    ' Windows +120 = wheel away from the user = scroll up = negative deltaY in the browser.
                    Dim dy = -ev.WheelDelta
                    Return ("Input.dispatchMouseEvent",
                            $"{{""type"":""mouseWheel"",""x"":{at.X},""y"":{at.Y},""deltaX"":0,""deltaY"":{dy.ToString(CultureInfo.InvariantCulture)},""button"":""none"",""buttons"":{ev.Buttons},""modifiers"":{ev.Modifiers}}}")
                Case Else
                    Dim down = ev.Kind = MirrorKind.KeyDown
                    Dim hasText = down AndAlso Not String.IsNullOrEmpty(ev.Text) AndAlso (ev.Modifiers And (1 Or 2 Or 4)) = 0
                    Dim type = If(Not down, "keyUp", If(hasText, "keyDown", "rawKeyDown"))
                    Dim txt = If(hasText, $",""text"":""{Esc(ev.Text)}"",""unmodifiedText"":""{Esc(ev.Text)}""", "")
                    Dim rep = If(down AndAlso ev.AutoRepeat, ",""autoRepeat"":true", "")
                    Return ("Input.dispatchKeyEvent",
                            $"{{""type"":""{type}"",""key"":""{Esc(ev.Key)}"",""code"":""{ev.Code}"",""windowsVirtualKeyCode"":{ev.Vk},""nativeVirtualKeyCode"":{ev.Vk},""modifiers"":{ev.Modifiers}{rep}{txt}}}")
            End Select
        End Function

        ''' <summary>DOM key / code names of a Windows virtual key; text = the character it types (or "").</summary>
        Public Function KeyNames(vk As Integer, extended As Boolean, text As String) As (Key As String, Code As String)
            Select Case vk
                Case &H41 To &H5A : Return (If(text <> "", text, ChrW(vk + 32).ToString()), "Key" & ChrW(vk))
                Case &H30 To &H39 : Return (If(text <> "", text, ChrW(vk).ToString()), "Digit" & ChrW(vk))
                Case &H60 To &H69 : Return (If(text <> "", text, ChrW(vk - &H30).ToString()), "Numpad" & ChrW(vk - &H30))
                Case &H70 To &H87 : Return ("F" & (vk - &H6F), "F" & (vk - &H6F))
                Case &H1B : Return ("Escape", "Escape")
                Case &HD : Return ("Enter", If(extended, "NumpadEnter", "Enter"))
                Case &H9 : Return ("Tab", "Tab")
                Case &H20 : Return (" ", "Space")
                Case &H8 : Return ("Backspace", "Backspace")
                Case &H2E : Return ("Delete", "Delete")
                Case &H2D : Return ("Insert", "Insert")
                Case &H24 : Return ("Home", "Home")
                Case &H23 : Return ("End", "End")
                Case &H21 : Return ("PageUp", "PageUp")
                Case &H22 : Return ("PageDown", "PageDown")
                Case &H25 : Return ("ArrowLeft", "ArrowLeft")
                Case &H26 : Return ("ArrowUp", "ArrowUp")
                Case &H27 : Return ("ArrowRight", "ArrowRight")
                Case &H28 : Return ("ArrowDown", "ArrowDown")
                Case &HA0 : Return ("Shift", "ShiftLeft")
                Case &HA1 : Return ("Shift", "ShiftRight")
                Case &H10 : Return ("Shift", "ShiftLeft")
                Case &HA2 : Return ("Control", "ControlLeft")
                Case &HA3 : Return ("Control", "ControlRight")
                Case &H11 : Return ("Control", "ControlLeft")
                Case &HA4 : Return ("Alt", "AltLeft")
                Case &HA5 : Return ("AltGraph", "AltRight")
                Case &H12 : Return ("Alt", "AltLeft")
                Case &H14 : Return ("CapsLock", "CapsLock")
                Case Else : Return (If(text <> "", text, "Unidentified"), "")
            End Select
        End Function

        ''' <summary>CDP modifier bitmask from the held modifier keys.</summary>
        Public Function Modifiers(alt As Boolean, ctrl As Boolean, shift As Boolean) As Integer
            Return If(alt, 1, 0) Or If(ctrl, 2, 0) Or If(shift, 8, 0)
        End Function

        Private Function Esc(s As String) As String
            If s Is Nothing Then Return ""
            Return s.Replace("\", "\\").Replace("""", "\""")
        End Function

    End Module

End Namespace
