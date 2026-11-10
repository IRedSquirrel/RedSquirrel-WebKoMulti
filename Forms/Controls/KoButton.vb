Imports System.ComponentModel
Imports System.Drawing.Drawing2D

''' <summary>
''' Flat rounded button: Primary (indigo) · Secondary · Ghost (outline) · Danger · Success, hover / pressed / disabled
''' looks, optional icon glyph (Segoe MDL2 Assets) left of the text. Draws itself → the designer shows the real look.
''' </summary>
<ToolboxItem(True), DesignerCategory("Code"), DefaultEvent("Click")>
Public Class KoButton
    Inherits Control

    Private _kind As KoButtonKind = KoButtonKind.Secondary
    Private _glyph As String = ""
    Private _radius As Integer = 7
    Private _hover As Boolean
    Private _pressed As Boolean

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor Or ControlStyles.StandardClick, True)
        MyBase.BackColor = Color.Transparent
        MyBase.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        MyBase.Cursor = Cursors.Hand
        Size = New Size(110, 30)
    End Sub

    <Category("Ko"), DefaultValue(GetType(KoButtonKind), "Secondary")>
    Public Property Kind As KoButtonKind
        Get
            Return _kind
        End Get
        Set(value As KoButtonKind)
            _kind = value
            Invalidate()
        End Set
    End Property

    <Category("Ko"), DefaultValue(""), Description("Metnin solundaki simge (Segoe MDL2 Assets karakteri, ör. E710 = artı).")>
    Public Property Glyph As String
        Get
            Return _glyph
        End Get
        Set(value As String)
            _glyph = If(value, "")
            Invalidate()
        End Set
    End Property

    <Category("Ko"), DefaultValue(7)>
    Public Property Radius As Integer
        Get
            Return _radius
        End Get
        Set(value As Integer)
            _radius = Math.Max(0, value)
            Invalidate()
        End Set
    End Property

    <DefaultValue(GetType(Cursor), "Hand")>
    Public Overrides Property Cursor As Cursor
        Get
            Return MyBase.Cursor
        End Get
        Set(value As Cursor)
            MyBase.Cursor = value
        End Set
    End Property

    Protected Overrides Sub OnTextChanged(e As EventArgs)
        MyBase.OnTextChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        _hover = True : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hover = False : _pressed = False : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If e.Button = MouseButtons.Left Then _pressed = True : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        _pressed = False : Invalidate()
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        Invalidate()
    End Sub

    ''' <summary>(fill, border, text) for the current state.</summary>
    Private Function Colors() As (Fill As Color, Border As Color, Fore As Color)
        If Not Enabled Then Return (DisabledBg, Color.Empty, TextFaint)
        Dim lift As Boolean = _hover AndAlso Not _pressed
        Select Case _kind
            Case KoButtonKind.Primary
                Return (If(_pressed, Color.FromArgb(79, 70, 229), If(lift, PrimarySoft, Primary)), Color.Empty, Color.White)
            Case KoButtonKind.Danger
                Return (If(_pressed, Color.FromArgb(150, 30, 40), If(lift, Color.FromArgb(200, 50, 60), Color.FromArgb(176, 40, 52))), Color.Empty, Color.White)
            Case KoButtonKind.Success
                Return (If(_pressed, Color.FromArgb(18, 110, 60), If(lift, Color.FromArgb(30, 150, 84), Color.FromArgb(22, 130, 72))), Color.Empty, Color.White)
            Case KoButtonKind.Ghost
                Return (If(_pressed, KoPaint.SecondaryFill, If(lift, Color.FromArgb(24, 28, 56), Color.Empty)), KoPaint.CardBorder, If(lift, TextMain, TextMuted))
            Case Else
                Return (If(_pressed, Color.FromArgb(28, 32, 64), If(lift, KoPaint.SecondaryHover, KoPaint.SecondaryFill)), Color.Empty, TextMain)
        End Select
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim c = Colors()
        Using path As GraphicsPath = KoPaint.RoundPath(New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F), _radius)
            If c.Fill <> Color.Empty Then
                Using b As New SolidBrush(c.Fill)
                    g.FillPath(b, path)
                End Using
            End If
            If c.Border <> Color.Empty Then
                Using p As New Pen(c.Border, 1)
                    g.DrawPath(p, path)
                End Using
            End If
        End Using
        Dim flags As TextFormatFlags = TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine Or TextFormatFlags.EndEllipsis
        Dim textSize As Size = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine)   ' not with EndEllipsis: then it measures "…" (16 px for "Şimdi tara")
        If _glyph <> "" Then
            ' Fixed glyph box: TextRenderer measures a private-use glyph far too wide and the text got cut ("Ürün e…").
            Using gf As New Font("Segoe MDL2 Assets", Math.Max(7.0F, Font.Size - 0.5F))
                Dim box As Integer = CInt(Math.Ceiling(gf.Size * 1.6F))
                Dim gap As Integer = If(Text = "", 0, 5)
                Dim total As Integer = box + gap + textSize.Width
                Dim x As Integer = Math.Max(6, (Width - total) \ 2)
                TextRenderer.DrawText(g, _glyph, gf, New Rectangle(x, 0, box, Height), c.Fore,
                                      TextFormatFlags.VerticalCenter Or TextFormatFlags.HorizontalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine)
                If Text <> "" Then TextRenderer.DrawText(g, Text, Font, New Rectangle(x + box + gap, 0, Math.Max(0, Width - x - box - gap - 4), Height), c.Fore, flags)
            End Using
        Else
            TextRenderer.DrawText(g, Text, Font, New Rectangle(4, 0, Width - 8, Height), c.Fore, flags Or TextFormatFlags.HorizontalCenter)
        End If
    End Sub

End Class
