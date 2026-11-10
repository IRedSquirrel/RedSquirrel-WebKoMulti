Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core
Imports Newtonsoft.Json.Linq

Namespace Engine.Login

    ''' <summary>
    ''' The ardream.net HOME page (launcher). Measured 9 Nov 2026 (site.js / hesap.js read, page opened, nothing typed or sent):
    ''' the page loads the game in its own iframe (#oyunCerceve → /play). Panel: #durum (status text), #bar --p (progress),
    ''' #start (START; class "hazir" + enabled = can be pressed). START → files download (~135 MB first time, cache later) →
    ''' the page shows the frame by itself (body gets class "oyunda") → the game's own ID / password screen in the canvas.
    ''' The site's GİRİŞ YAP modal is only the account portal (password change etc.) — NOT needed to play.
    ''' If the page cannot be cross-origin isolated it falls back to "dogrudan": START navigates to /play (not handled, see LOGIN.md).
    ''' SCRIPTS: only this read-only probe, only on the home page (top frame), only BEFORE the game frame is shown. Never on /play,
    ''' never inside the game frame (its guard hooks WebAssembly / WebSocket / Function.prototype.toString).
    ''' </summary>
    Public Module LauncherPage

        Public Const HomeUrl As String = "https://ardream.net/"

        ''' <summary>One read of the home page. Side-effect free to build; <see cref="ReadAsync"/> runs the read-only script.</summary>
        Public Class LauncherState
            Public Url As String = ""
            Public HasStart As Boolean
            Public StartEnabled As Boolean
            ''' <summary>START centre in view CSS pixels.</summary>
            Public StartPoint As Point
            Public StatusText As String = ""
            Public Progress As Double
            Public HasFrame As Boolean
            ''' <summary>Body has class "oyunda": the page put the game frame in front.</summary>
            Public GameShown As Boolean
            Public ViewSize As Size
        End Class

        Public Enum LauncherStep
            ''' <summary>Page not loaded / gate page (cookie check reloads once) — wait.</summary>
            Wait
            ''' <summary>START can be pressed.</summary>
            PressStart
            ''' <summary>Files are downloading after START — wait (progress).</summary>
            Downloading
            ''' <summary>The site reports an error — START once more (the page reloads the frame).</summary>
            RetryStart
            ''' <summary>The game frame is in front: launcher done, the rest is in the canvas.</summary>
            GameShown
            ''' <summary>The page went to /play by itself (no cross-origin isolation) — not handled.</summary>
            DirectPlay
            ''' <summary>Not ardream.net at all (redirect, error page).</summary>
            WrongPage
        End Enum

        Private Const Probe As String =
            "(function(){var s=document.getElementById('start'),d=document.getElementById('durum'),b=document.getElementById('bar')," &
            "f=document.getElementById('oyunCerceve'),r=s?s.getBoundingClientRect():null,p=b?parseFloat(getComputedStyle(b).getPropertyValue('--p')):0;" &
            "return JSON.stringify({url:location.href,start:!!s,dis:s?!!s.disabled:true,hazir:s?s.classList.contains('hazir'):false," &
            "x:r?r.left+r.width/2:0,y:r?r.top+r.height/2:0,w:r?r.width:0,durum:d?d.textContent:'',p:isNaN(p)?0:p,frame:!!f," &
            "oyunda:!!document.querySelector('.oyunda'),iw:innerWidth,ih:innerHeight});})()"

        ''' <summary>Runs the read-only probe. Caller guarantees: top page is the home page and the game is not shown yet.</summary>
        Public Async Function ReadAsync(cw As CoreWebView2) As Task(Of LauncherState)
            Dim result As String = Await cw.ExecuteScriptAsync(Probe)
            Return Parse(result, cw.Source)
        End Function

        ''' <summary>Parses the probe's result (a JSON string literal holding JSON). Side-effect free.</summary>
        Public Function Parse(scriptResult As String, source As String) As LauncherState
            Dim st As New LauncherState With {.Url = If(source, "")}
            If String.IsNullOrEmpty(scriptResult) OrElse scriptResult = "null" Then Return st
            Dim inner As String
            Try
                Dim tok As JToken = JToken.Parse(scriptResult)
                inner = If(tok.Type = JTokenType.String, tok.ToString(), scriptResult)
                Dim o As JObject = JObject.Parse(inner)
                st.Url = Str(o, "url", st.Url)
                st.HasStart = Bool(o, "start")
                st.StartEnabled = Not Bool(o, "dis") AndAlso Bool(o, "hazir")
                st.StartPoint = New Point(CInt(Num(o, "x")), CInt(Num(o, "y")))
                st.StatusText = Str(o, "durum", "").Trim()
                st.Progress = Num(o, "p")
                st.HasFrame = Bool(o, "frame")
                st.GameShown = Bool(o, "oyunda")
                st.ViewSize = New Size(CInt(Num(o, "iw")), CInt(Num(o, "ih")))
            Catch
            End Try
            Return st
        End Function

        ''' <summary>What to do with this read. <paramref name="startPressed"/> = we already pressed START this run. Side-effect free.</summary>
        Public Function Decide(st As LauncherState, startPressed As Boolean) As LauncherStep
            Dim url As String = If(st.Url, "").ToLowerInvariant()
            If url.Length > 0 AndAlso Not url.StartsWith("https://ardream.net") Then Return LauncherStep.WrongPage
            If url.StartsWith("https://ardream.net/play") Then Return LauncherStep.DirectPlay
            If st.GameShown Then Return LauncherStep.GameShown
            If Not st.HasStart Then Return LauncherStep.Wait
            Dim text As String = If(st.StatusText, "").ToLowerInvariant()
            Dim isError As Boolean = text.Contains("sorun") OrElse text.Contains("hata")
            If st.StartEnabled Then
                If Not startPressed Then Return LauncherStep.PressStart
                ' pressed once: enabled again = the site says ready-but-not-shown or error → press again
                Return If(isError, LauncherStep.RetryStart, LauncherStep.PressStart)
            End If
            Return If(startPressed, LauncherStep.Downloading, LauncherStep.Wait)
        End Function

        Private Function Str(o As JObject, k As String, def As String) As String
            Dim t = o.GetValue(k, StringComparison.OrdinalIgnoreCase)
            If t Is Nothing OrElse t.Type = JTokenType.Null Then Return def
            Return t.ToString()
        End Function

        Private Function Bool(o As JObject, k As String) As Boolean
            Dim t = o.GetValue(k, StringComparison.OrdinalIgnoreCase)
            Return t IsNot Nothing AndAlso t.Type = JTokenType.Boolean AndAlso CBool(t)
        End Function

        Private Function Num(o As JObject, k As String) As Double
            Dim t = o.GetValue(k, StringComparison.OrdinalIgnoreCase)
            If t Is Nothing OrElse (t.Type <> JTokenType.Integer AndAlso t.Type <> JTokenType.Float) Then Return 0
            Return CDbl(t)
        End Function

    End Module

End Namespace
