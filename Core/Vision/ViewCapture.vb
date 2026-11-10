Imports System.IO
Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core

Namespace Core.Vision

    ''' <summary>Screenshots FROM a view: whole view (CapturePreview) or a clipped region (Page.captureScreenshot, much cheaper).</summary>
    Public Module ViewCapture

        Public Async Function FullAsync(cw As CoreWebView2) As Task(Of Bitmap)
            If cw Is Nothing Then Return Nothing
            Using ms As New MemoryStream()
                Await cw.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms)
                ms.Position = 0
                Using b As New Bitmap(ms)
                    Return New Bitmap(b)
                End Using
            End Using
        End Function

        Public Async Function RegionAsync(cw As CoreWebView2, r As Rectangle) As Task(Of Bitmap)
            If cw Is Nothing Then Return Nothing
            Dim json = Await cw.CallDevToolsProtocolMethodAsync("Page.captureScreenshot",
                $"{{""format"":""png"",""clip"":{{""x"":{r.X},""y"":{r.Y},""width"":{r.Width},""height"":{r.Height},""scale"":1}},""captureBeyondViewport"":false}}")
            Dim key = """data"":"""
            Dim a = json.IndexOf(key, StringComparison.Ordinal)
            If a < 0 Then Return Nothing
            a += key.Length
            Dim b = json.IndexOf(""""c, a)
            Using ms As New MemoryStream(Convert.FromBase64String(json.Substring(a, b - a)))
                Using bmp As New Bitmap(ms)
                    Return New Bitmap(bmp)
                End Using
            End Using
        End Function
    End Module

End Namespace
