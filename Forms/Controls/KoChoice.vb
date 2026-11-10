Imports System.ComponentModel
Imports System.Drawing.Drawing2D

''' <summary>
''' A radio choice drawn as a card — bold title, faint description, radio dot. Choices with the same parent form one
''' group: checking one unchecks the others. Used for the computer's role (Ana tarayıcı / Pazarcı).
''' </summary>
<ToolboxItem(True), DesignerCategory("Code"), DefaultEvent("CheckedChanged")>
Public Class KoChoice
    Inherits Control

    Private _checked As Boolean
    Private _description As String = ""
    Private _hover As Boolean

    Public Event CheckedChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor Or ControlStyles.Selectable, True)
        MyBase.BackColor = Color.Transparent
        MyBase.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        MyBase.Cursor = Cursors.Hand
        Size = New Size(200, 62)
    End Sub

    <Category("Ko"), DefaultValue(False)>
    Public Property Checked As Boolean
        Get
            Return _checked
        End Get
        Set(value As Boolean)
            If value = _checked Then Return
            _checked = value
            If value AndAlso Parent IsNot Nothing Then
                For Each other As KoChoice In Parent.Controls.OfType(Of KoChoice)()
                    If other IsNot Me Then other.Checked = False
                Next
            End If
            Invalidate()
            RaiseEvent CheckedChanged(Me, EventArgs.Empty)
        End Set
    End Property

    <Category("Ko"), DefaultValue("")>
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
        If Enabled Then Checked = True
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

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim r As New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F)
        Using path As GraphicsPath = KoPaint.RoundPath(r, 8)
            Using b As New SolidBrush(If(_checked, Color.FromArgb(30, 32, 78), If(_hover, Color.FromArgb(22, 25, 52), NightDeep)))
                g.FillPath(b, path)
            End Using
            Using p As New Pen(If(_checked, Primary, KoPaint.CardBorder), If(_checked, 1.5F, 1.0F))
                g.DrawPath(p, path)
            End Using
        End Using
        ' Radio dot.
        Dim cx As Single = 20, cy As Single = 20
        Using p As New Pen(If(_checked, PrimarySoft, TextFaint), 1.5F)
            g.DrawEllipse(p, cx - 7, cy - 7, 14, 14)
        End Using
        If _checked Then
            Using b As New SolidBrush(PrimarySoft)
                g.FillEllipse(b, cx - 3.5F, cy - 3.5F, 7, 7)
            End Using
        End If
        TextRenderer.DrawText(g, Text, Font, New Rectangle(36, 9, Width - 44, 22), If(_checked, TextMain, Color.FromArgb(200, 204, 228)),
                              TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine Or TextFormatFlags.EndEllipsis Or TextFormatFlags.VerticalCenter)
        If _description <> "" Then
            Using f As New Font("Segoe UI", 8.0!)
                TextRenderer.DrawText(g, _description, f, New Rectangle(36, 31, Width - 44, Height - 34), TextFaint,
                                      TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.WordBreak Or TextFormatFlags.EndEllipsis)
            End Using
        End If
    End Sub

End Class
