Imports System.ComponentModel
Imports System.Drawing.Drawing2D
Imports System.IO

''' <summary>
''' The app's logo (the red robot squirrel) — drawn from the embedded PNG (Assets\logo.png, logical name
''' "RedSquirrel.WebKoMulti.logo.png"), scaled to the control and clipped to a rounded square. The image is loaded once
''' and shared; a missing resource draws nothing (the logo never breaks the window).
'''
''' Designer-safe: every property starts with its default, setters only store + Invalidate.
''' </summary>
Public Class BrandLogo
    Inherits Control

    Private Const ResourceName As String = "RedSquirrel.WebKoMulti.logo.png"
    Private Shared ReadOnly _lock As New Object()
    Private Shared _image As Image
    Private Shared _loaded As Boolean

    Private _corner As Single = 0.22F

    ''' <summary>The embedded logo image (500×500), or Nothing when the resource is missing. Loaded once.</summary>
    Public Shared Function LogoImage() As Image
        SyncLock _lock
            If Not _loaded Then
                _loaded = True
                Try
                    Using s As Stream = GetType(BrandLogo).Assembly.GetManifestResourceStream(ResourceName)
                        If s IsNot Nothing Then
                            Using tmp = Image.FromStream(s)
                                _image = New Bitmap(tmp)
                            End Using
                        End If
                    End Using
                Catch
                    _image = Nothing
                End Try
            End If
            Return _image
        End SyncLock
    End Function

    ''' <summary>Corner radius as a fraction of the shorter side (0 = square corners, 0.5 = circle).</summary>
    <DefaultValue(0.22F), Category("Appearance")>
    Public Property CornerRatio As Single
        Get
            Return _corner
        End Get
        Set(value As Single)
            _corner = Math.Max(0F, Math.Min(0.5F, value))
            Invalidate()
        End Set
    End Property

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor, True)
        BackColor = Color.Transparent
        Size = New Size(24, 24)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim side = Math.Min(ClientSize.Width, ClientSize.Height)
        If side <= 0 Then Return
        Dim r As New RectangleF((ClientSize.Width - side) / 2.0F, (ClientSize.Height - side) / 2.0F, side, side)
        DrawMark(e.Graphics, r, _corner)
    End Sub

    ''' <summary>Draws the logo into <paramref name="bounds"/> (kept square), clipped to a rounded square.</summary>
    Public Shared Sub DrawMark(g As Graphics, bounds As RectangleF, Optional cornerRatio As Single = 0.22F)
        Dim img = LogoImage()
        If img Is Nothing OrElse bounds.Width <= 0 OrElse bounds.Height <= 0 Then Return
        Dim oldMode = g.InterpolationMode
        Dim oldSmooth = g.SmoothingMode
        Dim oldPix = g.PixelOffsetMode
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.PixelOffsetMode = PixelOffsetMode.HighQuality
        Dim radius = Math.Min(bounds.Width, bounds.Height) * cornerRatio
        Using path = RoundRect(bounds, radius)
            Dim oldClip = g.Clip
            If radius > 0 Then g.SetClip(path, CombineMode.Intersect)
            g.DrawImage(img, bounds)
            g.Clip = oldClip
        End Using
        g.InterpolationMode = oldMode
        g.SmoothingMode = oldSmooth
        g.PixelOffsetMode = oldPix
    End Sub

    Private Shared Function RoundRect(r As RectangleF, radius As Single) As GraphicsPath
        Dim p As New GraphicsPath()
        If radius <= 0 Then
            p.AddRectangle(r)
            Return p
        End If
        Dim d = radius * 2
        p.AddArc(r.X, r.Y, d, d, 180, 90)
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

End Class
