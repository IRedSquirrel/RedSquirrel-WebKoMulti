Imports System.IO
Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core

Namespace Engine.Follow

    ''' <summary>
    ''' Flicker-free capture for the follow round. ViewCapture.RegionAsync (Page.captureScreenshot with a clip) made the
    ''' views FLICKER: on a WebGL page the clipped capture forces a frame where the canvas is black except the clip
    ''' (live 10 Nov, "ekran pırpır yapıyor"; probe on a WebGL page drawing every frame: 11-13 % of on-screen samples black).
    ''' Every unclipped way showed 0 %. Measured with 4 views of 1280×800 (round = 4 in parallel):
    '''   CapturePreview jpeg ≈ 60 ms, machine CPU ≈ 6 %   ← used
    '''   captureScreenshot jpeg (no clip, optimizeForSpeed) ≈ 60-66 ms, CPU ≈ 9 %
    '''   screencast ≈ 16 ms but CPU 12-16 % all the time and frames up to ~0.5 s old
    ''' JPEG is safe for the coordinate reader: every readable real frame read the same at quality 60-95, none wrong.
    ''' Frames are in the view's device pixels (= CSS pixels at 100 % display scale).
    ''' </summary>
    Public Module FollowCapture

        ''' <summary>The whole view as a JPEG-decoded bitmap, or Nothing.</summary>
        Public Async Function FrameAsync(cw As CoreWebView2) As Task(Of Bitmap)
            If cw Is Nothing Then Return Nothing
            Using ms As New MemoryStream()
                Await cw.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, ms)
                If ms.Length = 0 Then Return Nothing
                ms.Position = 0
                Using b As New Bitmap(ms)
                    Return New Bitmap(b)
                End Using
            End Using
        End Function

        ''' <summary>
        ''' The whole view, cut to <paramref name="region"/> (frame pixels) in memory. FrameSize = the whole frame's size, so the
        ''' caller can see a resize without another call.
        ''' </summary>
        Public Async Function RegionAsync(cw As CoreWebView2, region As Rectangle) As Task(Of (Crop As Bitmap, FrameSize As Size))
            If cw Is Nothing Then Return (Nothing, Size.Empty)
            Using ms As New MemoryStream()
                Await cw.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, ms)
                If ms.Length = 0 Then Return (Nothing, Size.Empty)
                ms.Position = 0
                Using b As New Bitmap(ms)
                    Dim r = region
                    r.Intersect(New Rectangle(0, 0, b.Width, b.Height))
                    If r.Width <= 0 OrElse r.Height <= 0 Then Return (Nothing, b.Size)
                    Return (b.Clone(r, Imaging.PixelFormat.Format32bppArgb), b.Size)
                End Using
            End Using
        End Function

    End Module

End Namespace
