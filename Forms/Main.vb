Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Services.Logging

''' <summary>
''' Main window (500×580, borderless, the KO look): header + summary card (leader · accounts in game · follow on / off) + five tabs
''' — Hesaplar · Oyunlar · Takip · VPN · Kayıt — and a footer with Hepsini başlat / Oyunlar / Durdur. The game views live in GameWindow
''' (followers' grid) and LeaderWindow (the leader) — they are 1280×800 each and don't fit here. State is the SessionHub's.
''' Designer: Main.Designer.vb.
''' </summary>
Public Class Main

    ' Form cosmetics (dragging the borderless window), kept local like the other KO windows.
    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    Private ReadOnly _demo As Boolean
    Private ReadOnly _hub As SessionHub
    Private ReadOnly _accounts As New AccountsView()
    Private ReadOnly _gamesView As New GamesView()
    Private ReadOnly _followView As New FollowView()
    Private ReadOnly _vpn As New VpnView()
    Private ReadOnly _log As New LogView()
    Private ReadOnly _views As Control()
    Private ReadOnly _games As New GameWindow()
    Private _closing As Boolean
    Private Shared ReadOnly MirrorOnFont As New Font("Segoe UI", 8.0!, FontStyle.Bold)
    Private Shared ReadOnly MirrorOffFont As New Font("Segoe UI", 8.0!)

    Public Sub New()
        Me.New(demo:=False)
    End Sub

    ''' <param name="demo">Render / preview: nothing is read from or written to disk, nothing is logged.</param>
    Public Sub New(demo As Boolean)
        InitializeComponent()
        UiStyle.ApplyAppIcon(Me)   ' taskbar / Alt+Tab icon (the borderless window draws no title bar)
        _demo = demo
        _hub = New SessionHub(Me) With {.Demo = demo}
        _hub.BeforeInit = Sub() _games.ShowAll(Me)
        _views = {_accounts, _gamesView, _followView, _vpn, _log}
        pnlHost.SuspendLayout()
        For Each v As Control In _views
            v.Dock = DockStyle.Fill
            v.Visible = False
            pnlHost.Controls.Add(v)
        Next
        pnlHost.ResumeLayout(False)
        _accounts.Visible = True
        _accounts.Bind(_hub)
        _gamesView.Bind(_hub, _games)
        _followView.Bind(_hub)
        _vpn.Bind(_hub)
        _games.Bind(_hub)
        AddHandler _hub.AccountsChanged, Sub() RefreshSummary()
        AddHandler _hub.SessionsRebuilt, Sub() RefreshSummary()
        AddHandler _hub.LeaderChanged, Sub() RefreshSummary()
        AddHandler _hub.SessionChanged, Sub(s) RefreshSummary()
        AddHandler _hub.FollowRunningChanged, Sub() RefreshSummary()
        AddHandler _hub.MirrorChanged, Sub() RefreshSummary()
        AddHandler _hub.DirtyChanged, Sub() tabs.SetBadge(0, If(_hub.Dirty, "•", ""), KoTone.Warn)
        AddHandler _log.ProblemsChanged, Sub()
                                             Dim n As Integer = _log.ProblemCount
                                             tabs.SetBadge(4, If(n > 0, n.ToString(), ""), KoTone.Warn)
                                         End Sub
    End Sub

    Friend ReadOnly Property Hub As SessionHub
        Get
            Return _hub
        End Get
    End Property

    Friend ReadOnly Property Games As GameWindow
        Get
            Return _games
        End Get
    End Property

    Friend ReadOnly Property FollowPage As FollowView
        Get
            Return _followView
        End Get
    End Property

    Friend ReadOnly Property VpnPage As VpnView
        Get
            Return _vpn
        End Get
    End Property

    Friend ReadOnly Property LogPage As LogView
        Get
            Return _log
        End Get
    End Property

    Friend Sub SelectTab(index As Integer)
        tabs.SelectedIndex = index
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        If _demo Then
            RefreshSummary()
            Return
        End If
        BotLog.Add("app", "Uygulama açıldı")
        _hub.LoadAccounts()
        _vpn.ReloadFromEngine()
        _log.StartTail()
        tmrRefresh.Start()
        RefreshSummary()
    End Sub

    Friend Const WebView2DownloadUrl As String = "https://developer.microsoft.com/microsoft-edge/webview2/"

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _demo Then Return
        ' The game views need the Microsoft Edge WebView2 Runtime; without it every Başlat would fail with an English exception.
        Dim version As String = Nothing
        Try
            version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString()
        Catch ex As Exception
            version = Nothing
        End Try
        If Not String.IsNullOrEmpty(version) Then
            BotLog.Add("app", "WebView2 Runtime " & version)
            Return
        End If
        BotLog.Warn("app", "WebView2 Runtime bulunamadı — oyun görünümleri açılamaz; kullanıcıya indirme sayfası önerildi")
        If KoConfirm.Ask(Me, "WebView2 Runtime bulunamadı",
                         "Oyun görünümleri için Microsoft Edge WebView2 Runtime gerekir. Kurduktan sonra programı yeniden açın.",
                         "İndirme sayfasını aç", danger:=False) Then
            Try
                Process.Start(WebView2DownloadUrl)
                BotLog.Add("app", "WebView2 indirme sayfası açıldı")
            Catch ex As Exception
                BotLog.Warn("app", "WebView2 indirme sayfası açılamadı: " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub tabs_SelectedIndexChanged(sender As Object, e As EventArgs) Handles tabs.SelectedIndexChanged
        pnlHost.SuspendLayout()
        For i As Integer = 0 To _views.Length - 1
            _views(i).Visible = i = tabs.SelectedIndex
        Next
        pnlHost.ResumeLayout()
    End Sub

    Public Sub RefreshSummary()
        Dim total As Integer = _hub.Accounts.Count
        Dim enabled As Integer = _hub.Accounts.Where(Function(a) a.Enabled).Count()
        Dim inGame As Integer = _hub.Sessions.Where(Function(s) s.State = SessionState.InGame).Count()
        Dim busy As Integer = _hub.BusyCount
        Dim leader As GameSession = _hub.LeaderSession
        pillLeader.Text = If(leader Is Nothing, "Lider: seçilmedi", "Lider: " & SessionLook.Title(leader))
        pillLeader.Tone = If(leader Is Nothing, KoTone.Warn, KoTone.Primary)
        pillLeader.Width = Math.Min(190, KoPaint.PillWidth(pillLeader.Text, pillLeader.Font, pillLeader.Height))
        lblSummary.Left = pillLeader.Right + 10
        lblMirror.Left = lblSummary.Left
        lblMirror.Text = If(_hub.MirrorOn, "Yansıtma: açık · " & If(_hub.MirrorMouse, "klavye + fare", "yalnız klavye"), "Yansıtma: kapalı")
        lblMirror.ForeColor = If(_hub.MirrorOn, WarnOrange, TextFaint)
        lblMirror.Font = If(_hub.MirrorOn, MirrorOnFont, MirrorOffFont)
        lblSummary.Text = $"{total} hesap · {inGame} oyunda"
        lblFollow.Text = If(_hub.FollowRunning, "Açık", "Kapalı")
        lblFollow.ForeColor = If(_hub.FollowRunning, OkGreen, TextMain)
        lblFooterMain.Text = $"{enabled} hesap açık"
        lblFooterStatus.Text = If(busy > 0, $"{busy} giriş sürüyor", If(inGame > 0, $"{inGame} hesap oyunda", "Giriş yapan yok"))
        btnStopAll.Enabled = busy > 0
        btnStartAll.Enabled = enabled > 0
    End Sub

    Private Sub tmrRefresh_Tick(sender As Object, e As EventArgs) Handles tmrRefresh.Tick
        RefreshSummary()
    End Sub

    Private Async Sub btnStartAll_Click(sender As Object, e As EventArgs) Handles btnStartAll.Click
        _games.ShowAll(Me)
        Dim t As Task = _hub.StartAllAsync()
        RefreshSummary()
        Await t
        RefreshSummary()
    End Sub

    Private Sub btnStopAll_Click(sender As Object, e As EventArgs) Handles btnStopAll.Click
        _hub.StopAll()
        RefreshSummary()
    End Sub

    Private Sub btnGames_Click(sender As Object, e As EventArgs) Handles btnGames.Click
        _games.ShowAll(Me)
        _games.Activate()
    End Sub

#Region "Borderless window"

    Private Sub btnMinimize_Click(sender As Object, e As EventArgs) Handles btnMinimize.Click
        WindowState = FormWindowState.Minimized
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Close()
    End Sub

    Private Sub btnClose_MouseEnter(sender As Object, e As EventArgs) Handles btnClose.MouseEnter
        btnClose.BackColor = Color.FromArgb(200, 35, 35)
    End Sub

    Private Sub btnClose_MouseLeave(sender As Object, e As EventArgs) Handles btnClose.MouseLeave
        btnClose.BackColor = Color.Transparent
    End Sub

    Private Sub btnMinimize_MouseEnter(sender As Object, e As EventArgs) Handles btnMinimize.MouseEnter
        btnMinimize.BackColor = Color.FromArgb(40, 46, 84)
    End Sub

    Private Sub btnMinimize_MouseLeave(sender As Object, e As EventArgs) Handles btnMinimize.MouseLeave
        btnMinimize.BackColor = Color.Transparent
    End Sub

    Private Sub Header_MouseDown(sender As Object, e As MouseEventArgs) Handles pnlHeader.MouseDown, lblTitle.MouseDown, picLogo.MouseDown
        If e.Button <> MouseButtons.Left Then Return
        ReleaseCapture()
        SendMessage(Handle, &HA1, &H2, 0)
    End Sub

#End Region

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Using pen As New Pen(Color.FromArgb(26, 30, 62), 1)
            Using p = UiStyle.RoundedRect(New Rectangle(0, 0, Width - 1, Height - 1), 10)
                e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                e.Graphics.DrawPath(pen, p)
            End Using
        End Using
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not _demo AndAlso Not _closing AndAlso _hub.AnyViewOpen AndAlso e.CloseReason = CloseReason.UserClosing Then
            If Not KoConfirm.Ask(Me, "Program kapatılsın mı?", "Açık oyunların hepsi kapanır.", "Kapat") Then
                e.Cancel = True
                Return
            End If
        End If
        _closing = True
        MyBase.OnFormClosing(e)
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        tmrRefresh.Stop()
        If Not _demo Then
            If _hub.Dirty Then
                ' Edits not saved yet: kept rather than lost (the user typed them into this list).
                If _hub.Save() Then BotLog.Add("hesap", "Kapanışta kaydedilmemiş hesap değişiklikleri kaydedildi")
            End If
            _hub.StopAll()
        End If
        _games.CloseForGood()
        _hub.Dispose()
        If Not _demo Then BotLog.Add("app", "Uygulama kapatıldı")
        MyBase.OnFormClosed(e)
    End Sub

End Class
