Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core

Namespace Core.Input

    ''' <summary>
    ''' Keys / mouse INTO a view through the DevTools protocol (Input.dispatch*) — no focus, no foreground needed.
    ''' Coordinates are view CSS pixels (top-left = 0,0). Measured: the game takes these.
    ''' Shared contract — additions are fine, signature changes need every caller updated.
    ''' </summary>
    Public Module CdpInput

        Public Function Key(cw As CoreWebView2, type As String, keyName As String, code As String, vk As Integer, Optional text As String = Nothing) As Task(Of String)
            Dim t = If(text Is Nothing, "", $",""text"":""{Esc(text)}"",""unmodifiedText"":""{Esc(text)}""")
            Return cw.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent",
                $"{{""type"":""{type}"",""key"":""{Esc(keyName)}"",""code"":""{code}"",""windowsVirtualKeyCode"":{vk},""nativeVirtualKeyCode"":{vk}{t}}}")
        End Function

        ''' <summary>A non-character key (Enter / 13, Tab / 9, Backspace / 8, Home / 36 …) pressed and released.</summary>
        Public Async Function TapAsync(cw As CoreWebView2, keyName As String, code As String, vk As Integer, Optional holdMs As Integer = 30) As Task
            Await Key(cw, "rawKeyDown", keyName, code, vk)
            Await Task.Delay(holdMs)
            Await Key(cw, "keyUp", keyName, code, vk)
        End Function

        ''' <summary>Text typed character by character (keyDown with text + keyUp).</summary>
        Public Async Function TypeTextAsync(cw As CoreWebView2, text As String, Optional gapMs As Integer = 25) As Task
            For Each ch In text
                Dim s = ch.ToString()
                Dim vk = AscW(Char.ToUpperInvariant(ch))
                Await Key(cw, "keyDown", s, "", vk, s)
                Await Key(cw, "keyUp", s, "", vk)
                Await Task.Delay(gapMs)
            Next
        End Function

        Public Function Mouse(cw As CoreWebView2, type As String, x As Integer, y As Integer, Optional button As String = "none", Optional buttons As Integer = 0, Optional clickCount As Integer = 0) As Task(Of String)
            Return cw.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent",
                $"{{""type"":""{type}"",""x"":{x},""y"":{y},""button"":""{button}"",""buttons"":{buttons},""clickCount"":{clickCount}}}")
        End Function

        Public Async Function ClickAsync(cw As CoreWebView2, x As Integer, y As Integer, Optional doubleClick As Boolean = False) As Task
            Await Mouse(cw, "mouseMoved", x, y)
            Dim n = If(doubleClick, 2, 1)
            For c = 1 To n
                Await Mouse(cw, "mousePressed", x, y, "left", 1, c)
                Await Task.Delay(40)
                Await Mouse(cw, "mouseReleased", x, y, "left", 0, c)
            Next
        End Function

        Private Function Esc(s As String) As String
            Return s.Replace("\", "\\").Replace("""", "\""")
        End Function
    End Module

End Namespace
