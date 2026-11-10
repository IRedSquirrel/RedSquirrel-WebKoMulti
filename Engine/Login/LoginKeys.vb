Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core

Namespace Engine.Login

    ''' <summary>
    ''' Keys for the login chain, through the DevTools protocol (same path as Core.Input.CdpInput — into the view, no focus needed).
    ''' Difference to CdpInput.TypeTextAsync: each character carries its real US-layout key code ("KeyA", "Digit1", "Minus") and
    ''' Shift where the character needs it ('_', capitals) — a page / game that reads e.code or the Shift state sees a real key.
    ''' Ardream's rules (hesap.js): ID [a-z0-9_] 3-20, password [A-Za-z0-9] 6-16 → <see cref="Plan"/> covers those; other
    ''' characters go as text only. Plan is side-effect free (self-test).
    ''' </summary>
    Public Module LoginKeys

        Public Structure KeyStroke
            Public Text As String
            Public Key As String
            Public Code As String
            Public Vk As Integer
            Public Shift As Boolean
        End Structure

        Public Function Plan(ch As Char) As KeyStroke
            Dim s = ch.ToString()
            If ch >= "a"c AndAlso ch <= "z"c Then Return New KeyStroke With {.Text = s, .Key = s, .Code = "Key" & Char.ToUpperInvariant(ch), .Vk = AscW(Char.ToUpperInvariant(ch))}
            If ch >= "A"c AndAlso ch <= "Z"c Then Return New KeyStroke With {.Text = s, .Key = s, .Code = "Key" & ch, .Vk = AscW(ch), .Shift = True}
            If ch >= "0"c AndAlso ch <= "9"c Then Return New KeyStroke With {.Text = s, .Key = s, .Code = "Digit" & ch, .Vk = AscW(ch)}
            Select Case ch
                Case "_"c : Return New KeyStroke With {.Text = s, .Key = s, .Code = "Minus", .Vk = &HBD, .Shift = True}
                Case "-"c : Return New KeyStroke With {.Text = s, .Key = s, .Code = "Minus", .Vk = &HBD}
                Case "."c : Return New KeyStroke With {.Text = s, .Key = s, .Code = "Period", .Vk = &HBE}
                Case "@"c : Return New KeyStroke With {.Text = s, .Key = s, .Code = "Digit2", .Vk = &H32, .Shift = True}
            End Select
            Return New KeyStroke With {.Text = s, .Key = s, .Code = "", .Vk = 0}
        End Function

        Public Async Function TypeAsync(cw As CoreWebView2, text As String, Optional gapMs As Integer = 45) As Task
            For Each ch In If(text, "")
                Dim k = Plan(ch)
                Dim mods = If(k.Shift, 8, 0)
                If k.Shift Then Await Send(cw, "rawKeyDown", "Shift", "ShiftLeft", 16, Nothing, 8) : Await Task.Delay(30)
                Await Send(cw, "keyDown", k.Key, k.Code, k.Vk, k.Text, mods)
                Await Task.Delay(25)
                Await Send(cw, "keyUp", k.Key, k.Code, k.Vk, Nothing, mods)
                If k.Shift Then Await Task.Delay(30) : Await Send(cw, "keyUp", "Shift", "ShiftLeft", 16, Nothing, 0)
                Await Task.Delay(gapMs)
            Next
        End Function

        Public Function EnterAsync(cw As CoreWebView2) As Task
            Return Press(cw, "Enter", "Enter", 13, vbCr)
        End Function

        ''' <summary>ESC — only when a window was SEEN (AfterEnter); on a clean game screen ESC opens the game menu.</summary>
        Public Function EscAsync(cw As CoreWebView2) As Task
            Return Press(cw, "Escape", "Escape", 27, Nothing)
        End Function

        Public Function TabAsync(cw As CoreWebView2) As Task
            Return Press(cw, "Tab", "Tab", 9, Nothing)
        End Function

        Public Function BackspaceAsync(cw As CoreWebView2) As Task
            Return Press(cw, "Backspace", "Backspace", 8, Nothing)
        End Function

        Public Function EndAsync(cw As CoreWebView2) As Task
            Return Press(cw, "End", "End", 35, Nothing)
        End Function

        Public Function ArrowAsync(cw As CoreWebView2, right As Boolean) As Task
            Return If(right, Press(cw, "ArrowRight", "ArrowRight", 39, Nothing), Press(cw, "ArrowLeft", "ArrowLeft", 37, Nothing))
        End Function

        Private Async Function Press(cw As CoreWebView2, key As String, code As String, vk As Integer, text As String) As Task
            Await Send(cw, If(text Is Nothing, "rawKeyDown", "keyDown"), key, code, vk, text, 0)
            Await Task.Delay(40)
            Await Send(cw, "keyUp", key, code, vk, Nothing, 0)
        End Function

        Private Function Send(cw As CoreWebView2, type As String, key As String, code As String, vk As Integer, text As String, mods As Integer) As Task(Of String)
            Dim t = If(text Is Nothing, "", $",""text"":""{Esc(text)}"",""unmodifiedText"":""{Esc(text)}""")
            Return cw.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent",
                $"{{""type"":""{type}"",""modifiers"":{mods},""key"":""{Esc(key)}"",""code"":""{code}"",""windowsVirtualKeyCode"":{vk},""nativeVirtualKeyCode"":{vk}{t}}}")
        End Function

        Private Function Esc(s As String) As String
            Return s.Replace("\", "\\").Replace("""", "\""").Replace(vbCr, "\r")
        End Function

    End Module

End Namespace
