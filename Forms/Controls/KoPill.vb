Imports System.ComponentModel

''' <summary>Small rounded status label with a coloured dot ("● Ana tarayıcı", "● KS") — tone picks the colours.</summary>
<ToolboxItem(True), DesignerCategory("Code")>
Public Class KoPill
    Inherits Control

    Private _tone As KoTone = KoTone.Neutral
    Private _dot As Boolean = True

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor, True)
        MyBase.BackColor = Color.Transparent
        MyBase.Font = New Font("Segoe UI", 8.25!, FontStyle.Bold)
        Size = New Size(96, 22)
    End Sub

    <Category("Ko"), DefaultValue(GetType(KoTone), "Neutral")>
    Public Property Tone As KoTone
        Get
            Return _tone
        End Get
        Set(value As KoTone)
            _tone = value
            Invalidate()
        End Set
    End Property

    <Category("Ko"), DefaultValue(True)>
    Public Property ShowDot As Boolean
        Get
            Return _dot
        End Get
        Set(value As Boolean)
            _dot = value
            Invalidate()
        End Set
    End Property

    ''' <summary>Fits the width to the text (the designer keeps the size it is given).</summary>
    Public Sub FitWidth()
        Width = KoPaint.PillWidth(Text, Font, Height, _dot)
    End Sub

    Protected Overrides Sub OnTextChanged(e As EventArgs)
        MyBase.OnTextChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        KoPaint.DrawPill(e.Graphics, ClientRectangle, Text, _tone, Font, _dot)
    End Sub

End Class
