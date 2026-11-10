Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>
''' "Oyunlar": one row per account with its live state (the same pill as the game cells), the selected row's Başlat / Yeniden giriş,
''' Durdur and Lider yap, and the game windows' controls (open, leader full screen, close all games). The views themselves are in
''' GameWindow / LeaderWindow — they don't fit 500×580. Designer: GamesView.Designer.vb.
''' </summary>
Public Class GamesView

    Private Shared ReadOnly PillFont As New Font("Segoe UI", 8.0!, FontStyle.Bold)
    Private _hub As SessionHub
    Private _games As GameWindow
    Private _loading As Boolean

    Public Sub New()
        InitializeComponent()
        AddHandler grid.CellPainting, AddressOf PaintState
    End Sub

    Public Sub Bind(hub As SessionHub, games As GameWindow)
        _hub = hub
        _games = games
        AddHandler hub.SessionsRebuilt, Sub() Rebuild()
        AddHandler hub.AccountsChanged, Sub() Rebuild()
        AddHandler hub.LeaderChanged, Sub() Rebuild()
        AddHandler hub.SessionChanged, Sub(s) UpdateRow(s)
        Rebuild()
    End Sub

    Private ReadOnly Property SelectedSession As GameSession
        Get
            If _hub Is Nothing OrElse grid.CurrentRow Is Nothing Then Return Nothing
            Dim i As Integer = grid.CurrentRow.Index
            Return If(i >= 0 AndAlso i < _hub.Sessions.Count, _hub.Sessions(i), Nothing)
        End Get
    End Property

    ''' <summary>Every selected row (Ctrl / Shift + click), in list order; the current row if nothing else is selected.</summary>
    Private ReadOnly Property SelectedSessions As List(Of GameSession)
        Get
            Dim list As New List(Of GameSession)()
            If _hub Is Nothing Then Return list
            For Each r As DataGridViewRow In grid.SelectedRows
                If r.Index >= 0 AndAlso r.Index < _hub.Sessions.Count Then list.Add(_hub.Sessions(r.Index))
            Next
            If list.Count = 0 AndAlso SelectedSession IsNot Nothing Then list.Add(SelectedSession)
            Return list.OrderBy(Function(x) x.Index).ToList()
        End Get
    End Property

    Public Sub Rebuild()
        If _hub Is Nothing Then Return
        Dim keep As Integer = If(grid.CurrentRow IsNot Nothing, grid.CurrentRow.Index, 0)
        _loading = True
        grid.Rows.Clear()
        For Each s As GameSession In _hub.Sessions
            grid.Rows.Add(CStr(s.Number), RowName(s), "", s.StatusText)
        Next
        If grid.Rows.Count > 0 Then grid.CurrentCell = grid.Rows(Math.Max(0, Math.Min(grid.Rows.Count - 1, keep))).Cells(colName.Index)
        _loading = False
        lblEmpty.Visible = grid.Rows.Count = 0
        UpdateButtons()
    End Sub

    Private Function RowName(s As GameSession) As String
        Return If(_hub.Leader = s.Index, "★ ", "") & If(s.Account.Label.Trim() = "", $"{s.Number}. hesap", s.Account.Label)
    End Function

    Private Sub UpdateRow(s As GameSession)
        If s.Index < 0 OrElse s.Index >= grid.Rows.Count OrElse Not _hub.Sessions.Contains(s) Then Return
        Dim r As DataGridViewRow = grid.Rows(s.Index)
        r.Cells(colName.Index).Value = RowName(s)
        r.Cells(colText.Index).Value = s.StatusText
        grid.InvalidateRow(s.Index)
        If SelectedSessions.Contains(s) Then UpdateButtons()
    End Sub

    Private Sub UpdateButtons()
        Dim s As GameSession = SelectedSession
        Dim sel As List(Of GameSession) = SelectedSessions
        Dim startable As Integer = sel.Where(Function(x) Not _hub.IsBusy(x) AndAlso (x.Account.Enabled OrElse x.Ready)).Count()
        Dim manualable As Integer = sel.Where(Function(x) Not _hub.IsBusy(x) AndAlso x.State <> SessionState.InGame).Count()
        btnStart.Enabled = startable > 0
        If sel.Count > 1 Then
            btnStart.Text = $"Başlat ({startable})"
        Else
            btnStart.Text = If(s IsNot Nothing AndAlso s.Ready, "Yeniden giriş", "Başlat")
        End If
        btnManual.Enabled = manualable > 0
        btnManual.Text = If(sel.Count > 1, $"Elle giriş ({manualable})", "Elle giriş")
        btnStop.Enabled = sel.Any(Function(x) _hub.IsBusy(x))
        btnMakeLeader.Enabled = s IsNot Nothing AndAlso _hub.Leader <> s.Index
        btnCloseGames.Enabled = _hub IsNot Nothing AndAlso _hub.AnyViewOpen
    End Sub

    Private Sub grid_SelectionChanged(sender As Object, e As EventArgs) Handles grid.SelectionChanged
        If Not _loading Then UpdateButtons()
    End Sub

    Private Sub grid_CurrentCellChanged(sender As Object, e As EventArgs) Handles grid.CurrentCellChanged
        If Not _loading Then UpdateButtons()
    End Sub

    Private Sub PaintState(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> colState.Index OrElse _hub Is Nothing OrElse e.RowIndex >= _hub.Sessions.Count Then Return
        e.PaintBackground(e.CellBounds, (e.State And DataGridViewElementStates.Selected) <> 0)
        Dim s As GameSession = _hub.Sessions(e.RowIndex)
        Dim busy As Boolean = _hub.IsBusy(s)
        Dim text As String = SessionLook.StateText(s, busy)
        Dim w As Integer = Math.Min(e.CellBounds.Width - 8, KoPaint.PillWidth(text, PillFont, 18))
        KoPaint.DrawPill(e.Graphics, New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + (e.CellBounds.Height - 18) \ 2, w, 18), text,
                         SessionLook.StateTone(s, busy), PillFont)
        e.Handled = True
    End Sub

    Private Sub btnShow_Click(sender As Object, e As EventArgs) Handles btnShow.Click
        _games.ShowAll(FindForm())
        _hub.Log("Oyun pencereleri açıldı", "pencere")
    End Sub

    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        Await StartSelectedAsync(manual:=False)
    End Sub

    Private Async Sub btnManual_Click(sender As Object, e As EventArgs) Handles btnManual.Click
        Await StartSelectedAsync(manual:=True)
    End Sub

    ''' <summary>Başlat / Elle giriş for every selected row at once (manual: only the ones not already in the game).</summary>
    Private Async Function StartSelectedAsync(manual As Boolean) As Task
        Dim todo As List(Of GameSession) = SelectedSessions.Where(Function(x) Not _hub.IsBusy(x) AndAlso
            If(manual, x.State <> SessionState.InGame, x.Account.Enabled OrElse x.Ready)).ToList()
        If todo.Count = 0 Then Return
        _games.ShowAll(FindForm())
        If todo.Count > 1 Then _hub.Log($"{If(manual, "Elle giriş", "Başlat")}: seçilen {todo.Count} hesap — " & String.Join(", ", todo.Select(Function(x) CStr(x.Number))), "oturum")
        Dim t = Task.WhenAll(todo.Select(Function(x) _hub.StartAsync(x, relogin:=Not manual AndAlso x.Ready, manual:=manual)))
        UpdateButtons()
        Await t
        UpdateButtons()
    End Function

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        For Each s As GameSession In SelectedSessions
            If _hub.IsBusy(s) Then _hub.StopOne(s)
        Next
    End Sub

    Private Sub btnMakeLeader_Click(sender As Object, e As EventArgs) Handles btnMakeLeader.Click
        Dim s As GameSession = SelectedSession
        If s IsNot Nothing Then _hub.SetLeader(s.Index)
    End Sub

    Private Sub btnLeaderFull_Click(sender As Object, e As EventArgs) Handles btnLeaderFull.Click
        _games.ToggleLeaderFullScreen(FindForm())
    End Sub

    Private Async Sub btnClearData_Click(sender As Object, e As EventArgs) Handles btnClearData.Click
        If Not KoConfirm.Ask(FindForm(), "Bütün tarayıcı verileri silinsin mi?",
                             "Oyunlar kapanır; her hesabın çerezleri, önbelleği ve oturumu silinir. Hesaplar, şifreler ve ayarlar kalır. Sonraki girişte her hesap sıfırdan açılır.",
                             "Hepsini sil") Then Return
        btnClearData.Enabled = False
        Dim problem As String = ""
        Try
            problem = Await _hub.ClearBrowserDataAsync()
        Catch ex As Exception
            problem = ex.Message
        End Try
        btnClearData.Enabled = True
        _games.Relayout()
        Rebuild()
        If problem <> "" Then _hub.Warn(problem, "oturum")
    End Sub

    Private Async Sub btnCloseGames_Click(sender As Object, e As EventArgs) Handles btnCloseGames.Click
        If Not KoConfirm.Ask(FindForm(), "Bütün oyunlar kapatılsın mı?", "Açık oyun görünümlerinin hepsi kapanır; girişler ve takip durur.", "Kapat") Then Return
        Await _hub.CloseGamesAsync()
        _games.Relayout()
        Rebuild()
    End Sub

End Class
