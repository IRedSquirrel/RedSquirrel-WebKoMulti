Imports System.ComponentModel
Imports System.Drawing.Drawing2D

''' <summary>
''' On/off switch with its label and an optional faint description under it — the settings' check box. Click anywhere
''' on it (or Space when focused) to switch.
''' </summary>
<ToolboxItem(True), DesignerCategory("Code"), DefaultEvent("CheckedChanged")>
Public Class KoToggle
    Inherits Control

    Private _checked As Boolean
    Private _description As String = ""
    Private _hover As Boolean
    Private Const TrackW As Integer = 32
    Private Const TrackH As Integer = 18

    Public Event CheckedChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor Or ControlStyles.Selectable, True)
        MyBase.BackColor = Color.Transparent
        MyBase.Font = New Font("Segoe UI", 9.0!)
        MyBase.Cursor = Cursors.Hand
        Size = New Size(220, 22)
    End Sub

    <Category("Ko"), DefaultValue(False)>
    Public Property Checked As Boolean
        Get
            Return _checked
        End Get
        Set(value As Boolean)
            If value = _checked Then Return
            _checked = value
            Invalidate()
            RaiseEvent CheckedChanged(Me, EventArgs.Empty)
        End Set
    End Property

    <Category("Ko"), DefaultValue(""), Description("Etiketin altındaki soluk açıklama (boş = yok).")>
    Public Property Description As String
        Get
            Return _description
        End Get
        Set(value As String)
            _description = If(value, "")
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

    Protected Overrides Sub OnClick(e As EventArgs)
        MyBase.OnClick(e)
        If Enabled Then Checked = Not Checked
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Space AndAlso Enabled Then Checked = Not Checked
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        _hover = True : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hover = False : Invalidate()
    End Sub

    Protected Overrides Sub OnTextChanged(e As EventArgs)
        MyBase.OnTextChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim lineH As Integer = Math.Max(TrackH, Font.Height)
        Dim track As New RectangleF(0.5F, (lineH - TrackH) / 2.0F + 0.5F, TrackW, TrackH)
        Dim onColor As Color = If(Enabled, Primary, DisabledBg)
        Dim offColor As Color = If(_hover AndAlso Enabled, KoPaint.SecondaryHover, KoPaint.SecondaryFill)
        Using path As GraphicsPath = KoPaint.RoundPath(track, TrackH / 2.0F)
            Using b As New SolidBrush(If(_checked, onColor, offColor))
                g.FillPath(b, path)
            End Using
        End Using
        Dim knob As Single = TrackH - 6
        Dim kx As Single = If(_checked, track.Right - 3 - knob, track.X + 3)
        Using b As New SolidBrush(If(Enabled, Color.White, TextFaint))
            g.FillEllipse(b, kx, track.Y + 3, knob, knob)
        End Using
        Dim textX As Integer = TrackW + 10
        TextRenderer.DrawText(g, Text, Font, New Rectangle(textX, 0, Width - textX, lineH), If(Enabled, TextMain, TextFaint),
                              TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine Or TextFormatFlags.EndEllipsis)
        If _description <> "" Then
            Using f As New Font("Segoe UI", 8.0!)
                TextRenderer.DrawText(g, _description, f, New Rectangle(textX, lineH + 1, Width - textX, Height - lineH - 1), TextFaint,
                                      TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.WordBreak Or TextFormatFlags.EndEllipsis)
            End Using
        End If
    End Sub

End Class
