Imports System.ComponentModel

''' <summary>Dark drop-down list: owner drawn — a visual-styles DropDownList ignores BackColor and stays white.</summary>
<ToolboxItem(True), DesignerCategory("Code")>
Public Class KoCombo
    Inherits ComboBox

    Public Sub New()
        MyBase.DropDownStyle = ComboBoxStyle.DropDownList
        MyBase.FlatStyle = FlatStyle.Flat
        MyBase.BackColor = NightDeep
        MyBase.ForeColor = TextMain
        MyBase.Font = New Font("Segoe UI", 9.0!)
        MyBase.DrawMode = DrawMode.OwnerDrawFixed
        MyBase.ItemHeight = 20
    End Sub

    Private Const WM_PAINT As Integer = &HF
    Private Const WM_PRINT As Integer = &H317
    Private Const WM_PRINTCLIENT As Integer = &H318

    ''' <summary>The closed box: the system draws a flat DropDownList white (the old screen's Kanal / Karakter boxes) — painted over.</summary>
    Protected Overrides Sub WndProc(ByRef m As Message)
        MyBase.WndProc(m)
        If m.Msg = WM_PAINT Then
            Using g As Graphics = CreateGraphics()
                PaintClosed(g)
            End Using
        ElseIf (m.Msg = WM_PRINT OrElse m.Msg = WM_PRINTCLIENT) AndAlso m.WParam <> IntPtr.Zero Then
            Using g As Graphics = Graphics.FromHdc(m.WParam)
                PaintClosed(g)
            End Using
        End If
    End Sub

    Private Sub PaintClosed(g As Graphics)
        Using b As New SolidBrush(If(Enabled, NightDeep, Color.FromArgb(14, 16, 32)))
            g.FillRectangle(b, 0, 0, Width, Height)
        End Using
        Using p As New Pen(If(Focused OrElse DroppedDown, PrimarySoft, KoPaint.CardBorder))
            g.DrawRectangle(p, 0, 0, Width - 1, Height - 1)
        End Using
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Dim cx As Single = Width - 13, cy As Single = Height / 2.0F
        Using p As New Pen(If(Enabled, TextMuted, TextFaint), 1.6F)
            g.DrawLines(p, {New PointF(cx - 4, cy - 2), New PointF(cx, cy + 2), New PointF(cx + 4, cy - 2)})
        End Using
        TextRenderer.DrawText(g, Text, Font, New Rectangle(7, 0, Math.Max(0, Width - 30), Height), If(Enabled, TextMain, TextFaint),
                              TextFormatFlags.VerticalCenter Or TextFormatFlags.Left Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine)
    End Sub

    Protected Overrides Sub OnSelectedIndexChanged(e As EventArgs)
        MyBase.OnSelectedIndexChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)
        MyBase.OnDrawItem(e)
        Dim edit As Boolean = (e.State And DrawItemState.ComboBoxEdit) = DrawItemState.ComboBoxEdit
        Dim selected As Boolean = (e.State And DrawItemState.Selected) = DrawItemState.Selected AndAlso Not edit
        Using back As New SolidBrush(If(selected, Primary, NightDeep))
            e.Graphics.FillRectangle(back, e.Bounds)
        End Using
        If e.Index >= 0 Then
            TextRenderer.DrawText(e.Graphics, GetItemText(Items(e.Index)), Font, Rectangle.Inflate(e.Bounds, -4, 0),
                                  If(selected, Color.White, TextMain),
                                  TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix)
        End If
    End Sub

End Class
