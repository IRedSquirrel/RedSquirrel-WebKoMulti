Imports System.ComponentModel
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms.Design

''' <summary>
''' Top navigation of the KO main window: tabs from one designer string ("Ürünler|Pazar|…"), the selected tab bright with
''' an indigo underline, a hairline under the strip, and an optional count badge per tab (SetBadge — "3" in red on "Ürünler"
''' when three items are undercut).
''' Pages: each tab can own a sibling panel ("pageGeneral|pageGame|…") — the strip shows the selected one and hides the
''' rest itself, so the same happens in the Visual Studio designer: a click on a tab there opens that tab's page for editing.
''' a tab can be hidden (SetTabVisible); the indexes stay, a hidden tab is
''' neither drawn nor selectable.
''' </summary>
<ToolboxItem(True), DesignerCategory("Code"), DefaultEvent("SelectedIndexChanged"), Designer(GetType(KoTabStripDesigner))>
Public Class KoTabStrip
    Inherits Control

    Private _tabs As String() = {}
    Private _tabsText As String = ""
    Private _selected As Integer = 0
    Private _pagesText As String = ""
    Private _pageParent As Control
    Private _hover As Integer = -1
    Private ReadOnly _badges As New Dictionary(Of Integer, (Text As String, Tone As KoTone))()
    Private ReadOnly _hidden As New HashSet(Of Integer)()
    Private _hit As New List(Of Rectangle)()

    Public Event SelectedIndexChanged As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor, True)
        MyBase.BackColor = Color.Transparent
        MyBase.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        Size = New Size(400, 34)
    End Sub

    <Category("Ko"), DefaultValue(""), Description("Sekmeler, | ile ayrılmış: Ürünler|Pazar|Ayarlar")>
    Public Property Tabs As String
        Get
            Return _tabsText
        End Get
        Set(value As String)
            _tabsText = If(value, "")
            _tabs = _tabsText.Split({"|"c}, StringSplitOptions.RemoveEmptyEntries).Select(Function(t) t.Trim()).ToArray()
            If _selected >= _tabs.Length Then _selected = Math.Max(0, _tabs.Length - 1)
            Invalidate()
        End Set
    End Property

    <Category("Ko"), DefaultValue(""), Description("Her sekmenin paneli, | ile ve sekme sırasıyla: pageGeneral|pageGame — seçili sekmenin paneli görünür. Tasarımcıda sekmeye tıklayınca o panel açılır.")>
    Public Property Pages As String
        Get
            Return _pagesText
        End Get
        Set(value As String)
            _pagesText = If(value, "")
            ShowSelectedPage()
        End Set
    End Property

    ' Not written to the designer file: a tab clicked in the designer only picks the page being edited, the window still opens on
    ' the first tab.
    <Category("Ko"), DefaultValue(0), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property SelectedIndex As Integer
        Get
            Return _selected
        End Get
        Set(value As Integer)
            Dim v As Integer = Math.Max(0, Math.Min(Math.Max(0, _tabs.Length - 1), value))
            If v = _selected OrElse _hidden.Contains(v) Then Return
            _selected = v
            Invalidate()
            ShowSelectedPage()
            RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
        End Set
    End Property

    <Browsable(False)>
    Public ReadOnly Property SelectedTab As String
        Get
            Return If(_selected < _tabs.Length, _tabs(_selected), "")
        End Get
    End Property

    ''' <summary>Index of the tab under a point in this control's coordinates, -1 when none (also asked by the designer).</summary>
    Public Function TabAt(p As Point) As Integer
        Return _hit.FindIndex(Function(r) r.Contains(p))
    End Function

    ''' <summary>The panel a tab owns (from Pages); Nothing when the tab has none or it isn't next to the strip.</summary>
    Public Function PageOf(index As Integer) As Control
        Dim names As String() = PageNames()
        If Parent Is Nothing OrElse index < 0 OrElse index >= names.Length Then Return Nothing
        For Each c As Control In Parent.Controls
            If c IsNot Me AndAlso String.Equals(c.Name, names(index), StringComparison.OrdinalIgnoreCase) Then Return c
        Next
        Return Nothing
    End Function

    Private Function PageNames() As String()
        Return _pagesText.Split({"|"c}, StringSplitOptions.RemoveEmptyEntries).Select(Function(t) t.Trim()).ToArray()
    End Function

    ' Visible is set on the panel itself: in the designer that changes what is drawn but not what is saved (the designer keeps its
    ' own copy of Visible for the file), so the hidden pages stay "Visible = False" in the .Designer.vb.
    Private Sub ShowSelectedPage()
        If Parent Is Nothing OrElse _pagesText.Length = 0 Then Return
        Dim count As Integer = PageNames().Length
        Parent.SuspendLayout()
        Try
            For i As Integer = 0 To count - 1
                Dim page As Control = PageOf(i)
                If page IsNot Nothing Then page.Visible = i = _selected
            Next
        Finally
            Parent.ResumeLayout()
        End Try
    End Sub

    Protected Overrides Sub OnParentChanged(e As EventArgs)
        MyBase.OnParentChanged(e)
        If _pageParent IsNot Nothing Then RemoveHandler _pageParent.ControlAdded, AddressOf Parent_ControlAdded
        _pageParent = Parent
        If _pageParent IsNot Nothing Then AddHandler _pageParent.ControlAdded, AddressOf Parent_ControlAdded
        ShowSelectedPage()
    End Sub

    ' A page added after the strip (designer file order) is hidden or shown as soon as it arrives.
    Private Sub Parent_ControlAdded(sender As Object, e As ControlEventArgs)
        If _pagesText.Length > 0 AndAlso PageNames().Contains(e.Control.Name, StringComparer.OrdinalIgnoreCase) Then ShowSelectedPage()
    End Sub

    ''' <summary>shows / hides a tab. Hiding the selected one selects the next visible tab (the one before it when none follows).</summary>
    Public Sub SetTabVisible(index As Integer, visible As Boolean)
        If visible Then
            If Not _hidden.Remove(index) Then Return
        Else
            If Not _hidden.Add(index) Then Return
            If _selected = index Then
                Dim nextTab As Integer = Enumerable.Range(0, _tabs.Length).Where(Function(i) Not _hidden.Contains(i)).
                                         OrderBy(Function(i) If(i > index, i - index, _tabs.Length + index - i)).DefaultIfEmpty(index).First()
                If nextTab <> index Then
                    _selected = nextTab
                    ShowSelectedPage()
                    RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
                End If
            End If
        End If
        Invalidate()
    End Sub

    Public Function IsTabVisible(index As Integer) As Boolean
        Return index >= 0 AndAlso index < _tabs.Length AndAlso Not _hidden.Contains(index)
    End Function

    ''' <summary>A small count next to a tab's name ("" = none).</summary>
    Public Sub SetBadge(index As Integer, text As String, Optional tone As KoTone = KoTone.Bad)
        If String.IsNullOrEmpty(text) Then _badges.Remove(index) Else _badges(index) = (text, tone)
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim h As Integer = _hit.FindIndex(Function(r) r.Contains(e.Location))
        If h <> _hover Then _hover = h : Cursor = If(h >= 0, Cursors.Hand, Cursors.Default) : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hover = -1 : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        MyBase.OnMouseClick(e)
        Dim h As Integer = TabAt(e.Location)
        If h >= 0 Then SelectedIndex = h
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using p As New Pen(KoPaint.Hairline, 1)
            g.DrawLine(p, 0, Height - 1, Width, Height - 1)
        End Using
        _hit = New List(Of Rectangle)()
        Dim flags As TextFormatFlags = TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine Or TextFormatFlags.VerticalCenter
        Using badgeFont As New Font("Segoe UI", 7.5!, FontStyle.Bold)
            ' Each tab is text + badge with 10 px on both sides; when that doesn't fit (six tabs + the log badge in the 484 px main
            ' window, Oct 21) the side padding shrinks evenly down to 4 px instead of the last tab being cut off.
            Dim tws(_tabs.Length - 1) As Integer, bws(_tabs.Length - 1) As Integer
            Dim content As Integer = 0
            Dim shown As Integer = 0
            For i As Integer = 0 To _tabs.Length - 1
                If _hidden.Contains(i) Then Continue For
                shown += 1
                tws(i) = TextRenderer.MeasureText(_tabs(i), Font, Size.Empty, flags).Width
                If _badges.ContainsKey(i) Then bws(i) = KoPaint.PillWidth(_badges(i).Text, badgeFont, 16, dot:=False) + 6
                content += tws(i) + bws(i)
            Next
            Dim pad As Integer = 10
            Dim room As Integer = Width - Padding.Left - Padding.Right
            If shown > 0 AndAlso content + 2 * pad * shown > room Then pad = Math.Max(4, (room - content) \ (2 * shown))
            Dim inset As Integer = Math.Max(1, pad - 2)
            Dim x As Integer = Padding.Left
            For i As Integer = 0 To _tabs.Length - 1
                If _hidden.Contains(i) Then _hit.Add(Rectangle.Empty) : Continue For
                Dim tw As Integer = tws(i)
                Dim bw As Integer = bws(i)
                Dim cell As New Rectangle(x, 0, tw + bw + 2 * pad, Height)
                _hit.Add(cell)
                Dim selected As Boolean = i = _selected
                Dim fore As Color = If(selected, TextMain, If(i = _hover, Color.FromArgb(200, 204, 228), TextMuted))
                TextRenderer.DrawText(g, _tabs(i), Font, New Rectangle(cell.X + pad, 0, tw + 2, Height - 2), fore, flags)
                If bw > 0 Then
                    KoPaint.DrawPill(g, New Rectangle(cell.X + pad + tw + 6, (Height - 2 - 16) \ 2, bw - 6, 16), _badges(i).Text, _badges(i).Tone, badgeFont, dot:=False)
                End If
                If selected Then
                    Using path As GraphicsPath = KoPaint.RoundPath(New RectangleF(cell.X + inset, Height - 3, cell.Width - 2 * inset, 3), 1.5F)
                        Using b As New LinearGradientBrush(New Rectangle(cell.X + inset, Height - 3, Math.Max(2, cell.Width - 2 * inset), 3), Primary, Secondary, LinearGradientMode.Horizontal)
                            g.FillPath(b, path)
                        End Using
                    End Using
                End If
                x = cell.Right
            Next
        End Using
    End Sub

End Class

''' <summary>
''' Lets a click on a tab reach the strip in the Visual Studio designer ; without it
''' the designer only selects the control and the other pages can't be opened there.
''' </summary>
Public Class KoTabStripDesigner
    Inherits ControlDesigner

    Protected Overrides Function GetHitTest(point As Point) As Boolean
        Dim strip As KoTabStrip = TryCast(Control, KoTabStrip)
        Return strip IsNot Nothing AndAlso strip.TabAt(strip.PointToClient(point)) >= 0
    End Function

End Class
