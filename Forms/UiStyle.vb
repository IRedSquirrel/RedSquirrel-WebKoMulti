Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

''' <summary>
''' Shared look for the code-built windows: glass cards, flat label buttons, dark text boxes.
''' Module without a namespace → used unqualified (like Theme).
''' </summary>
Friend Module UiStyle

    ' Form cosmetics stay local to the forms' own region code (rule: the Native module is for game / input calls).
    <DllImport("Gdi32.dll")>
    Private Function CreateRoundRectRgn(x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer, cx As Integer, cy As Integer) As IntPtr
    End Function

    Private ReadOnly _iconLock As New Object()
    Private _appIcon As Icon
    Private _iconLoaded As Boolean

    ''' <summary>
    ''' The app icon (embedded Assets\app.ico, logical name "RedSquirrel.WebKoMulti.app.ico", every size it holds) for the
    ''' windows' title bar / taskbar button. Loaded once; Nothing when the resource is missing (the window keeps its default).
    ''' </summary>
    Friend Function AppIcon() As Icon
        SyncLock _iconLock
            If Not _iconLoaded Then
                _iconLoaded = True
                Try
                    Using s As IO.Stream = GetType(UiStyle).Assembly.GetManifestResourceStream("RedSquirrel.WebKoMulti.app.ico")
                        If s IsNot Nothing Then _appIcon = New Icon(s)
                    End Using
                Catch
                    _appIcon = Nothing
                End Try
            End If
            Return _appIcon
        End SyncLock
    End Function

    ''' <summary>Puts the app icon on a window (no-op when it can't be loaded).</summary>
    Friend Sub ApplyAppIcon(f As Form)
        Dim ic As Icon = AppIcon()
        If ic Is Nothing Then Return
        f.Icon = ic
        f.ShowIcon = True
    End Sub

    Friend Function RoundRegion(width As Integer, height As Integer, radius As Integer) As Region
        Return Region.FromHrgn(CreateRoundRectRgn(0, 0, width, height, radius, radius))
    End Function

    Friend Function CardTitle(text As String) As Label
        Return New Label With {
            .Text = text, .Font = New Font("Segoe UI", 7.5!, FontStyle.Bold), .ForeColor = TextFaint,
            .AutoSize = True, .Location = New Point(16, 12), .BackColor = Color.Transparent
        }
    End Function

    Friend Sub StyleTextBox(t As TextBox, location As Point, width As Integer)
        t.BorderStyle = BorderStyle.FixedSingle
        t.BackColor = NightDeep
        t.ForeColor = TextMain
        t.Font = New Font("Segoe UI", 10.0!)
        t.Location = location
        t.Width = width
        t.MaxLength = 40
    End Sub

    Friend Sub StyleCheckBox(c As CheckBox, text As String, location As Point)
        c.Text = text
        c.Font = New Font("Segoe UI", 8.25!)
        c.ForeColor = TextMuted
        c.BackColor = Glass
        c.AutoSize = True
        c.Location = location
    End Sub

    Friend Sub StyleInfoLabel(l As Label, bounds As Rectangle)
        l.Font = New Font("Segoe UI", 8.25!)
        l.ForeColor = TextMuted
        l.BackColor = Color.Transparent
        l.AutoSize = False
        l.Bounds = bounds
    End Sub

    ''' <summary>Dark drop-down: owner drawn — a visual-styles DropDownList ignores BackColor and stays white.</summary>
    Friend Sub StyleComboBox(c As ComboBox, bounds As Rectangle)
        c.DropDownStyle = ComboBoxStyle.DropDownList
        c.FlatStyle = FlatStyle.Flat
        c.BackColor = NightDeep
        c.ForeColor = TextMain
        c.Font = New Font("Segoe UI", 9.0!)
        c.DrawMode = DrawMode.OwnerDrawFixed
        c.ItemHeight = 18
        c.Bounds = bounds
        AddHandler c.DrawItem, Sub(s, e)
                                   Dim selected As Boolean = (e.State And DrawItemState.Selected) = DrawItemState.Selected AndAlso
                                                             (e.State And DrawItemState.ComboBoxEdit) <> DrawItemState.ComboBoxEdit
                                   Using back As New SolidBrush(If(selected, Primary, NightDeep))
                                       e.Graphics.FillRectangle(back, e.Bounds)
                                   End Using
                                   If e.Index >= 0 Then
                                       TextRenderer.DrawText(e.Graphics, c.Items(e.Index).ToString(), c.Font, e.Bounds,
                                                             If(selected, Color.White, TextMain),
                                                             TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
                                   End If
                               End Sub
    End Sub

    ''' <summary>Flat label button: primary (indigo) or secondary, hover lightens. Tag holds the normal colour.</summary>
    Friend Sub StyleButton(b As Label, text As String, bounds As Rectangle, primaryStyle As Boolean)
        Dim normal As Color = If(primaryStyle, Primary, Color.FromArgb(34, 39, 76))   ' secondary must stand out on a Glass card
        Dim hover As Color = If(primaryStyle, PrimarySoft, Color.FromArgb(46, 52, 98))
        b.Text = text
        b.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        b.ForeColor = If(primaryStyle, Color.White, TextMain)
        b.BackColor = normal
        b.Cursor = Cursors.Hand
        b.TextAlign = ContentAlignment.MiddleCenter
        b.Bounds = bounds
        b.Tag = normal
        AddHandler b.MouseEnter, Sub() If b.Enabled Then b.BackColor = hover
        AddHandler b.MouseLeave, Sub() If b.Enabled Then b.BackColor = DirectCast(b.Tag, Color)
    End Sub

    Friend Sub SetButtonEnabled(b As Label, enabled As Boolean, Optional text As String = Nothing)
        b.Enabled = enabled
        If text IsNot Nothing Then b.Text = text
        b.BackColor = If(enabled, DirectCast(b.Tag, Color), DisabledBg)
    End Sub

    ''' <summary>Rounded glass card (like the view cards).</summary>
    Friend Function Card(r As Rectangle) As Panel
        Dim p As New Panel With {.Bounds = r, .BackColor = Glass}
        AddHandler p.Paint, Sub(s, e)
                                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                Using pen As New Pen(Color.FromArgb(40, 46, 84), 1)
                                    Using path = RoundedRect(New Rectangle(0, 0, p.Width - 1, p.Height - 1), 8)
                                        e.Graphics.DrawPath(pen, path)
                                    End Using
                                End Using
                            End Sub
        AddHandler p.HandleCreated, Sub() p.Region = RoundRegion(p.Width + 1, p.Height + 1, 16)
        Return p
    End Function

    ''' <summary>
    ''' Rounded rectangle path (radius in px).
    ''' </summary>
    Friend Function RoundedRect(r As Rectangle, radius As Integer) As GraphicsPath
        Dim d = radius * 2
        Dim gp As New GraphicsPath()
        gp.AddArc(r.X, r.Y, d, d, 180, 90)
        gp.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        gp.CloseFigure()
        Return gp
    End Function

End Module
