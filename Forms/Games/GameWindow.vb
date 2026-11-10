Imports Microsoft.Web.WebView2.WinForms
Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>
''' "Oyunlar" window: every shown account except the leader in a grid (1 → 1×1, 2 → 2×1, 3-4 → 2×2, 5-6 → 3×2, …), each cell a
''' GameCell with its status strip; the leader in its own LeaderWindow (owned here). On a leader change the views are MOVED —
''' the new leader's view goes to the leader window, the old one back into the grid; no game is reloaded.
''' Separate from the 500×580 main window on purpose: 1280×800 views don't fit it, and the user puts the games on the screen
''' they want. Closing it only hides it. Designer: GameWindow.Designer.vb.
''' </summary>
Public Class GameWindow

    Private _hub As SessionHub
    Private ReadOnly _cells As New Dictionary(Of GameSession, GameCell)()
    Private ReadOnly _leader As New LeaderWindow()
    Private _allowClose As Boolean
    Private _lastLayout As String = Nothing

    Public Sub New()
        InitializeComponent()
        UiStyle.ApplyAppIcon(Me)
        AddHandler _leader.cell.FullScreenClicked, Sub() _hub?.Log("Lider penceresi: " & If(_leader.IsFullScreen, "tam ekran", "pencere"), "pencere")
    End Sub

    Public Sub Bind(hub As SessionHub)
        _hub = hub
        AddHandler hub.SessionsRebuilt, Sub() Relayout()
        AddHandler hub.LeaderChanged, Sub() Relayout()
        AddHandler hub.AccountsChanged, Sub() Relayout()
        AddHandler hub.SessionChanged, AddressOf OnSessionChanged
        AddHandler hub.MirrorChanged, Sub() RefreshMirror()
        AddHandler hub.VpnChanged, Sub()
                                       _leader.cell.RefreshState()
                                       For Each c As GameCell In _cells.Values
                                           c.RefreshState()
                                       Next
                                   End Sub
        AddHandler hub.MirrorAction, Sub() _leader.cell.RefreshMirror()
        Relayout()
    End Sub

    Public ReadOnly Property LeaderWin As LeaderWindow
        Get
            Return _leader
        End Get
    End Property

    ''' <summary>Shows both windows (the views need a shown window before their browser is created).</summary>
    Public Sub ShowAll(owner As IWin32Window)
        Relayout()
        If Not Visible Then
            If Location = Point.Empty Then
                Dim wa As Rectangle = Screen.PrimaryScreen.WorkingArea
                Size = New Size(Math.Min(Width, wa.Width - 60), Math.Min(Height, wa.Height - 40))
                Location = New Point(wa.Right - Width - 20, wa.Y + 20)
            End If
            Show(owner)
        End If
        If WindowState = FormWindowState.Minimized Then WindowState = FormWindowState.Normal
        _leader.ShowNear(owner)
    End Sub

    Public Sub ShowLeader(owner As IWin32Window)
        Relayout()
        _leader.ShowNear(owner)
        _leader.Activate()
    End Sub

    Public Sub ToggleLeaderFullScreen(owner As IWin32Window)
        ShowLeader(owner)
        _leader.ToggleFullScreen()
        _hub?.Log("Lider penceresi: " & If(_leader.IsFullScreen, "tam ekran", "pencere"), "pencere")
    End Sub

    ''' <summary>Which sessions get a grid cell: not the leader, and enabled — or already holding a game.</summary>
    Private Function InGrid(s As GameSession) As Boolean
        Return s.Index <> _hub.Leader AndAlso (s.Account.Enabled OrElse s.Ready OrElse _hub.IsBusy(s))
    End Function

    ''' <summary>Grid columns for n cells: 1 · 2 · 2 · 2 · 3 · 3 · 3 · 3 · 3 · 4 …</summary>
    Friend Shared Function Columns(n As Integer) As Integer
        If n <= 1 Then Return 1
        If n <= 4 Then Return 2
        If n <= 9 Then Return 3
        Return 4
    End Function

    Public Sub Relayout()
        If _hub Is Nothing OrElse IsDisposed Then Return
        Dim leader As GameSession = _hub.LeaderSession
        ' 1) the leader first: its view leaves its grid cell before that cell can be dropped
        _leader.cell.Attach(_hub, leader, leaderMode:=True)
        _leader.Text = If(leader Is Nothing, "Lider — seçilmedi", $"Lider — {SessionLook.Title(leader)}")

        ' 2) follower cells (kept per session, so a cell's scroll position survives)
        Dim followers As List(Of GameSession) = _hub.Sessions.Where(AddressOf InGrid).ToList()
        Dim layoutKey As String = String.Join(",", followers.Select(Function(f) f.GetHashCode().ToString()))
        If layoutKey = _lastLayout AndAlso followers.All(Function(f) _cells.ContainsKey(f)) Then
            ' Same cells in the same places (an edit, a label change): refresh only — no re-parenting, no flicker.
            For Each f As GameSession In followers
                _cells(f).Attach(_hub, f, leaderMode:=False)
            Next
            UpdateCount(followers)
            Return
        End If
        _lastLayout = layoutKey
        grid.SuspendLayout()
        grid.Controls.Clear()
        For Each stale As GameSession In _cells.Keys.Where(Function(k) Not followers.Contains(k)).ToList()
            Dim c As GameCell = _cells(stale)
            DetachView(c, stale)
            _cells.Remove(stale)
            c.Dispose()
        Next
        Dim n As Integer = followers.Count
        Dim cols As Integer = Columns(n)
        Dim rows As Integer = Math.Max(1, CInt(Math.Ceiling(n / CDbl(cols))))
        grid.ColumnStyles.Clear()
        grid.RowStyles.Clear()
        grid.ColumnCount = cols
        grid.RowCount = rows
        For i As Integer = 1 To cols
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / cols))
        Next
        For i As Integer = 1 To rows
            grid.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F / rows))
        Next
        For i As Integer = 0 To n - 1
            Dim s As GameSession = followers(i)
            Dim c As GameCell = Nothing
            If Not _cells.TryGetValue(s, c) Then
                c = New GameCell With {.Dock = DockStyle.Fill, .Margin = New Padding(1)}
                _cells(s) = c
            End If
            grid.Controls.Add(c, i Mod cols, i \ cols)
            c.Attach(_hub, s, leaderMode:=False)
        Next
        grid.ResumeLayout(True)
        lblEmpty.Visible = n = 0
        If n = 0 Then lblEmpty.BringToFront()

        ' 3) a view nobody shows (account switched off while not started) is parked outside any cell
        For Each s As GameSession In _hub.Sessions
            If s IsNot leader AndAlso Not _cells.ContainsKey(s) AndAlso s.View.Parent IsNot Nothing AndAlso Not s.View.IsDisposed Then
                s.View.Parent.Controls.Remove(s.View)
            End If
        Next

        UpdateCount(followers)
    End Sub

    Private Sub UpdateCount(followers As IEnumerable(Of GameSession))
        Dim n As Integer = followers.Count()
        Dim inGame As Integer = followers.Count(Function(f) f.State = SessionState.InGame)
        lblCount.Text = $"{n} takipçi · {inGame} oyunda · lider ayrı pencerede"
    End Sub

    Private Shared Sub DetachView(c As GameCell, s As GameSession)
        c.Release()
        If s.View IsNot Nothing AndAlso Not s.View.IsDisposed AndAlso s.View.Parent IsNot Nothing AndAlso
           c.Contains(s.View) Then
            s.View.Parent.Controls.Remove(s.View)
        End If
    End Sub

    Private Sub RefreshMirror()
        _leader.cell.RefreshMirror()
        For Each c As GameCell In _cells.Values
            c.RefreshMirror()
        Next
    End Sub

    Private Sub OnSessionChanged(s As GameSession)
        Dim c As GameCell = Nothing
        If _cells.TryGetValue(s, c) Then c.RefreshState()
        If _leader.cell.Session Is s Then _leader.cell.RefreshState()
        UpdateCount(_cells.Keys)
    End Sub

    Private Sub btnLeader_Click(sender As Object, e As EventArgs) Handles btnLeader.Click
        ShowLeader(Owner)
    End Sub

    Private Async Sub btnStartAll_Click(sender As Object, e As EventArgs) Handles btnStartAll.Click
        Await _hub.StartAllAsync()
    End Sub

    Private Sub btnStopAll_Click(sender As Object, e As EventArgs) Handles btnStopAll.Click
        _hub.StopAll()
    End Sub

    ''' <summary>The main window closes the game windows for real (application exit).</summary>
    Public Sub CloseForGood()
        _allowClose = True
        _leader.AllowClose = True
        ' The views belong to the hub (it disposes them): take them out first so closing these windows doesn't.
        For Each kv In _cells.ToList()
            DetachView(kv.Value, kv.Key)
        Next
        Dim ls As GameSession = _leader.cell.Session
        If ls IsNot Nothing AndAlso Not ls.View.IsDisposed AndAlso ls.View.Parent IsNot Nothing Then ls.View.Parent.Controls.Remove(ls.View)
        _leader.Close()
        Close()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not _allowClose AndAlso e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Hide()
        End If
        MyBase.OnFormClosing(e)
    End Sub

End Class
