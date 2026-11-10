Imports System.ComponentModel
Imports System.Drawing.Drawing2D

''' <summary>
''' Glass card — rounded, thin border, optional small-caps title and a thin accent strip on the left. The section
''' container of the KO windows; draws itself, so the Visual Studio designer shows the real look.
''' </summary>
<ToolboxItem(True), DesignerCategory("Code")>
Public Class KoCard
    Inherits Panel

    Private _title As String = ""
    Private _radius As Integer = 10
    Private _accent As Color = Color.Empty
    Private _fill As Color = KoPaint.CardFill
    Private _border As Color = KoPaint.CardBorder

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor, True)
        MyBase.BackColor = Color.Transparent
    End Sub

    <Category("Ko"), DefaultValue(""), Description("Sol üstteki küçük başlık (boş = başlık yok).")>
    Public Property Title As String
        Get
            Return _title
        End Get
        Set(value As String)
            _title = If(value, "")
            Invalidate()
        End Set
    End Property

    <Category("Ko"), DefaultValue(10)>
    Public Property Radius As Integer
        Get
            Return _radius
        End Get
        Set(value As Integer)
            _radius = Math.Max(0, value)
            Invalidate()
        End Set
    End Property

    <Category("Ko"), Description("Sol kenardaki ince vurgu şeridi (boş = yok).")>
    Public Property Accent As Color
        Get
            Return _accent
        End Get
        Set(value As Color)
            _accent = value
            Invalidate()
        End Set
    End Property

    Private Function ShouldSerializeAccent() As Boolean
        Return _accent <> Color.Empty
    End Function

    Private Sub ResetAccent()
        Accent = Color.Empty
    End Sub

    <Category("Ko")>
    Public Property FillColor As Color
        Get
            Return _fill
        End Get
        Set(value As Color)
            _fill = value
            Invalidate()
        End Set
    End Property

    Private Function ShouldSerializeFillColor() As Boolean
        Return _fill <> KoPaint.CardFill
    End Function

    Private Sub ResetFillColor()
        FillColor = KoPaint.CardFill
    End Sub

    <Category("Ko")>
    Public Property BorderColor As Color
        Get
            Return _border
        End Get
        Set(value As Color)
            _border = value
            Invalidate()
        End Set
    End Property

    Private Function ShouldSerializeBorderColor() As Boolean
        Return _border <> KoPaint.CardBorder
    End Function

    Private Sub ResetBorderColor()
        BorderColor = KoPaint.CardBorder
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim r As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)
        Using path As GraphicsPath = KoPaint.RoundPath(r, _radius)
            Using b As New SolidBrush(_fill)
                g.FillPath(b, path)
            End Using
            If _accent <> Color.Empty Then
                Dim clip As Region = g.Clip
                g.SetClip(path, CombineMode.Intersect)
                Using b As New SolidBrush(_accent)
                    g.FillRectangle(b, 0, 0, 3, Height)
                End Using
                g.Clip = clip
            End If
            Using p As New Pen(_border, 1)
                g.DrawPath(p, path)
            End Using
        End Using
        If _title <> "" Then
            Using f As New Font("Segoe UI", 7.5!, FontStyle.Bold)
                TextRenderer.DrawText(g, _title, f, New Point(14, 11), TextFaint, TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix)
            End Using
        End If
    End Sub

End Class
