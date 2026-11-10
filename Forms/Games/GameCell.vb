Imports Microsoft.Web.WebView2.WinForms
Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>
''' One game on screen: a status strip (name, state pill, the session's own status line, ★ make leader, ▶ start / log in again,
''' ■ stop; in the leader window ⛶ full screen instead of ★) over a scrolling host that holds the session's 1280×800 view.
''' The view is MOVED between cells (Attach), never recreated — the game in it keeps running. Designer: GameCell.Designer.vb.
''' </summary>
Public Class GameCell

    Private _hub As SessionHub
    Private _session As GameSession
    Private _leaderMode As Boolean
    Private _settingMirror As Boolean

    ''' <summary>The leader window's ⛶ button.</summary>
    Public Event FullScreenClicked As EventHandler

    Public Sub New()
        InitializeComponent()
    End Sub

    Public ReadOnly Property Session As GameSession
        Get
            Return _session
        End Get
    End Property

    ''' <summary>Puts the session's view into this cell (taking it out of wherever it was).</summary>
    Public Sub Attach(hub As SessionHub, s As GameSession, leaderMode As Boolean)
        _hub = hub
        _leaderMode = leaderMode
        btnLead.Visible = Not leaderMode
        btnFull.Visible = leaderMode
        If _session IsNot s Then
            _session = s
            If s IsNot Nothing Then
                Dim v As WebView2 = s.View
                If v.Parent IsNot pnlView Then
                    v.Location = Point.Empty
                    v.Size = SessionHub.ViewSize
                    pnlView.Controls.Add(v)
                End If
            End If
        End If
        pnlView_Resize(Me, EventArgs.Empty)
        RefreshState()
    End Sub

    ''' <summary>Leaves the view where it is (the next cell takes it) and forgets the session.</summary>
    Public Sub Release()
        _session = Nothing
    End Sub

    Public Sub RefreshState()
        Dim s As GameSession = _session
        If s Is Nothing OrElse _hub Is Nothing Then
            lblName.Text = "—"
            pillState.Text = "Boş"
            pillState.Tone = KoTone.Neutral
            lblStatus.Text = ""
            lblPlaceholder.Visible = True
            lblPlaceholder.BringToFront()
            Return
        End If
        Dim busy As Boolean = _hub.IsBusy(s)
        lblName.Text = If(_leaderMode, "★ ", "") & SessionLook.Title(s)
        pillState.Text = SessionLook.StateText(s, busy)
        pillState.Tone = SessionLook.StateTone(s, busy)
        pillState.FitWidth()
        ' VPN: plain "VPN" while fine, red when the VPN dropped (its traffic is cut), amber when the open view was made with
        ' another proxy setting (the hub rebuilds the view on the next Başlat / Yeniden giriş).
        Dim vpnShown As Boolean = s.Account.UseVpn OrElse s.ActiveProxy <> ""
        pillVpn.Visible = vpnShown
        If vpnShown Then
            If _hub.ProxyMismatch(s) Then
                pillVpn.Text = "VPN: yeniden girişte" : pillVpn.Tone = KoTone.Warn
                tips.SetToolTip(pillVpn, "VPN ayarı bu oyun açıldıktan sonra değişti — yeniden başlatınca uygulanır (oyun yeniden açılır)")
            ElseIf s.Account.UseVpn AndAlso s.Ready AndAlso _hub.VpnNow = Services.Vpn.VpnState.Failed Then
                pillVpn.Text = "VPN koptu" : pillVpn.Tone = KoTone.Bad
                tips.SetToolTip(pillVpn, "VPN koptu — bu hesabın trafiği kesildi. " & _hub.VpnText)
            Else
                pillVpn.Text = "VPN" : pillVpn.Tone = KoTone.Info
                tips.SetToolTip(pillVpn, "Bu hesap VPN ile girer")
            End If
            pillVpn.FitWidth()
            pillVpn.Left = pillState.Right + 6
        End If
        lblStatus.Left = If(vpnShown, pillVpn.Right, pillState.Right) + 6
        lblStatus.Text = s.StatusText
        tips.SetToolTip(lblStatus, s.StatusText)
        btnStart.Enabled = Not busy
        tips.SetToolTip(btnStart, If(s.Ready, "Yeniden giriş", "Başlat"))
        btnStop.Enabled = busy
        btnSteps.Enabled = Not busy AndAlso s.State = SessionState.InGame AndAlso InGameSteps.Available
        tips.SetToolTip(btnSteps, If(InGameSteps.Available, "Oyun içi adımları yeniden çalıştır: pencereleri kapat, sesi kapat, PM'leri kapat, oyun ayarını uygula" & If(s.State = SessionState.InGame, "", " (yalnız oyundayken)"),
                                     "Oyun içi adımları yeniden çalıştır — giriş motorunda henüz yok"))
        btnLead.Enabled = _hub.Leader <> s.Index
        lblPlaceholder.Visible = Not s.Ready AndAlso Not busy
        lblPlaceholder.Text = If(s.Account.Enabled, "Oyun başlatılınca burada görünür (1280×800, kaydırılabilir).", "Hesap kapalı — Hesaplar sekmesinden açın.")
        If lblPlaceholder.Visible Then lblPlaceholder.BringToFront() Else s.View.BringToFront()
        RefreshMirror()
    End Sub

    ''' <summary>
    ''' Mirroring (hub = one source): the leader cell shows the switch and, while on, an ORANGE strip under its bar with the last
    ''' mirrored action; a follower cell that receives the leader's input shows a "yansıtılıyor" badge.
    ''' </summary>
    Public Sub RefreshMirror()
        Dim s As GameSession = _session
        Dim isOn As Boolean = _hub IsNot Nothing AndAlso _hub.MirrorOn
        _settingMirror = True
        tglMirror.Visible = _leaderMode
        tglMirror.Checked = isOn
        cmbMirrorMode.Visible = _leaderMode
        If _hub IsNot Nothing Then cmbMirrorMode.SelectedIndex = If(_hub.MirrorMouse, 1, 0)
        _settingMirror = False
        If _hub IsNot Nothing Then tips.SetToolTip(tglMirror, "Açıkken liderde tıkladığın / sürüklediğin / bastığın her şey oyundaki takipçilerde tekrarlanır" &
                                                   If(_hub.MirrorStatus = "", "", Environment.NewLine & _hub.MirrorStatus))
        pillMirror.Visible = Not _leaderMode AndAlso isOn AndAlso s IsNot Nothing AndAlso s.State = SessionState.InGame
        pnlMirror.Visible = _leaderMode AndAlso isOn
        If pnlMirror.Visible Then
            Dim n As Integer = _hub.MirrorTargets
            Dim last As String = _hub.MirrorLast
            lblMirror.Text = If(_hub.MirrorMouse, $"YANSITMA AÇIK — tıkladığın ve bastığın her şey {n} hesapta tekrarlanıyor",
                                $"YANSITMA AÇIK — tuşların {n} hesapta tekrarlanıyor (fare tıkları gitmiyor)") & If(last = "", "", "   ·   " & last)
            tips.SetToolTip(lblMirror, If(_hub.MirrorStatus = "", lblMirror.Text, _hub.MirrorStatus))
        End If
        Dim right As Integer = If(cmbMirrorMode.Visible, cmbMirrorMode.Left, If(pillMirror.Visible, pillMirror.Left, btnSteps.Left))
        lblStatus.Width = Math.Max(10, right - lblStatus.Left - 6)
    End Sub

    Private Sub cmbMirrorMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMirrorMode.SelectedIndexChanged
        If _settingMirror OrElse _hub Is Nothing OrElse cmbMirrorMode.SelectedIndex < 0 Then Return
        _hub.SetMirrorMouse(cmbMirrorMode.SelectedIndex = 1)
    End Sub

    Private Sub tglMirror_CheckedChanged(sender As Object, e As EventArgs) Handles tglMirror.CheckedChanged
        If _settingMirror OrElse _hub Is Nothing Then Return
        _hub.SetMirror(tglMirror.Checked)
    End Sub

    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        If _session Is Nothing Then Return
        Dim s As GameSession = _session
        Await _hub.StartAsync(s, relogin:=s.Ready)
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        If _session IsNot Nothing Then _hub.StopOne(_session)
    End Sub

    Private Sub btnLead_Click(sender As Object, e As EventArgs) Handles btnLead.Click
        If _session IsNot Nothing Then _hub.SetLeader(_session.Index)
    End Sub

    Private Async Sub btnSteps_Click(sender As Object, e As EventArgs) Handles btnSteps.Click
        If _session Is Nothing Then Return
        Dim s As GameSession = _session
        btnSteps.Enabled = False
        Await _hub.RunInGameStepsAsync(s)
        RefreshState()
    End Sub

    Private Sub btnFull_Click(sender As Object, e As EventArgs) Handles btnFull.Click
        RaiseEvent FullScreenClicked(Me, EventArgs.Empty)
    End Sub

    ''' <summary>A screen larger than the view (full screen leader): the view sits in the middle instead of the top-left.</summary>
    Private Sub pnlView_Resize(sender As Object, e As EventArgs) Handles pnlView.Resize
        If _session Is Nothing Then Return
        Dim v As WebView2 = _session.View
        If v.Parent IsNot pnlView Then Return
        Dim x As Integer = Math.Max(0, (pnlView.ClientSize.Width - v.Width) \ 2)
        Dim y As Integer = Math.Max(0, (pnlView.ClientSize.Height - v.Height) \ 2)
        If pnlView.ClientSize.Width < v.Width OrElse pnlView.ClientSize.Height < v.Height Then
            v.Location = New Point(pnlView.AutoScrollPosition.X + x, pnlView.AutoScrollPosition.Y + y)
        Else
            v.Location = New Point(x, y)
        End If
    End Sub

End Class
