Imports RedSquirrel.WebKoMulti.Services.Vpn

''' <summary>
''' "VPN": how every "VPN ile gir" account leaves (Services.Vpn.VpnManager). Two modes, one choice: PPTP VPN (Windows, split
''' tunnel — server + suggestions, user name, password) or Proxy (SOCKS5 / HTTP CONNECT — type, address, port, optional user name /
''' password). Kaydet, Bağlan / Kes, the engine's state as a pill. Credentials are never logged (proxy host:port is); the engine
''' writes the connection lines. Designer: VpnView.Designer.vb.
''' </summary>
Public Class VpnView

    ''' <summary>VPNBook's PPTP servers (suggestions only — any address can be typed).</summary>
    Private Shared ReadOnly Suggestions As String() =
        {"us16", "us178", "ca149", "ca196", "ca225", "uk205", "uk68", "de20", "de220", "fr200", "fr231", "pl134", "pl140"}

    Private Const PptpHint As String = "Şifre 1-2 haftada bir değişir — VPNBook sayfasından yenisini alın."
    Private Const ProxyHint As String = "Kimlik isteğe bağlı. Proxy düşerse VPN'li hesaplar doğrudan bağlanmaz, bekler."

    Private _hub As SessionHub
    Private _loading As Boolean

    Public Sub New()
        InitializeComponent()
        cmbSuggest.Items.Add("Öneriler…")
        For Each s As String In Suggestions
            cmbSuggest.Items.Add(s & ".vpnbook.com")
        Next
        cmbSuggest.SelectedIndex = 0
        cmbProxyType.Items.Add("SOCKS5")
        cmbProxyType.Items.Add("HTTP")
        cmbProxyType.SelectedIndex = 0
        ApplyMode()
    End Sub

    ''' <summary>The engine's current values as one settings object (for LoadFields).</summary>
    Private Shared Function EngineSettings() As VpnSettings
        Return New VpnSettings With {
            .Mode = VpnManager.Mode, .Server = VpnManager.Server, .UserName = VpnManager.UserName, .Password = VpnManager.Password,
            .ProxyType = VpnManager.ProxyType, .ProxyHost = VpnManager.ProxyHost, .ProxyPort = VpnManager.ProxyPort,
            .ProxyUser = VpnManager.ProxyUser, .ProxyPassword = VpnManager.ProxyPassword}
    End Function

    Public Sub Bind(hub As SessionHub)
        _hub = hub
        AddHandler hub.VpnChanged, Sub() RefreshState()
        AddHandler hub.AccountsChanged, Sub() RefreshCount()
        AddHandler hub.SessionsRebuilt, Sub() RefreshCount()
        If Not hub.Demo Then LoadFields(EngineSettings())
        RefreshState()
        RefreshCount()
    End Sub

    ''' <summary>After the hub loaded the settings (Main.OnLoad).</summary>
    Public Sub ReloadFromEngine()
        If _hub Is Nothing OrElse _hub.Demo Then Return
        LoadFields(EngineSettings())
        RefreshState()
    End Sub

    ''' <summary>Render preview: sample values (never real ones).</summary>
    Friend Sub DemoFields(server As String, user As String, password As String)
        LoadFields(New VpnSettings With {.Server = server, .UserName = user, .Password = password})
    End Sub

    ''' <summary>Render preview: proxy mode with sample values (never real ones).</summary>
    Friend Sub DemoProxy(kind As ProxyKind, host As String, port As Integer, user As String, password As String)
        LoadFields(New VpnSettings With {.Mode = VpnMode.Proxy, .ProxyType = kind, .ProxyHost = host, .ProxyPort = port,
                                         .ProxyUser = user, .ProxyPassword = password})
    End Sub

    Private Sub LoadFields(s As VpnSettings)
        _loading = True
        txtServer.Text = If(s.Server, "")
        txtUser.Text = If(s.UserName, "")
        txtPassword.Text = If(s.Password, "")
        cmbProxyType.SelectedIndex = If(s.ProxyType = ProxyKind.Http, 1, 0)
        txtProxyHost.Text = If(s.ProxyHost, "")
        txtProxyPort.Text = If(s.ProxyPort > 0, CStr(s.ProxyPort), "")
        txtProxyUser.Text = If(s.ProxyUser, "")
        txtProxyPassword.Text = If(s.ProxyPassword, "")
        If s.Mode = VpnMode.Proxy Then chProxy.Checked = True Else chPptp.Checked = True
        ApplyMode()
        _loading = False
    End Sub

    Private ReadOnly Property SelectedMode As VpnMode
        Get
            Return If(chProxy.Checked, VpnMode.Proxy, VpnMode.Pptp)
        End Get
    End Property

    Private ReadOnly Property SelectedProxyType As ProxyKind
        Get
            Return If(cmbProxyType.SelectedIndex = 1, ProxyKind.Http, ProxyKind.Socks5)
        End Get
    End Property

    ''' <summary>Typed port, 0 if empty / not a number.</summary>
    Private ReadOnly Property TypedPort As Integer
        Get
            Dim p As Integer
            Return If(Integer.TryParse(txtProxyPort.Text.Trim(), p), p, 0)
        End Get
    End Property

    ''' <summary>Shows the fields of the chosen mode (both sets share the same rows).</summary>
    Private Sub ApplyMode()
        Dim proxy As Boolean = chProxy.Checked
        For Each c As Control In New Control() {lblCapServer, txtServer, cmbSuggest, lblCapUser, txtUser, lblCapPassword, txtPassword}
            c.Visible = Not proxy
        Next
        For Each c As Control In New Control() {lblCapProxyType, cmbProxyType, lblCapProxyHost, txtProxyHost, lblCapProxyPort, txtProxyPort,
                                                lblCapProxyUser, txtProxyUser, lblCapProxyPassword, txtProxyPassword}
            c.Visible = proxy
        Next
        lblHint.Text = If(proxy, ProxyHint, PptpHint)
    End Sub

    Private Sub Mode_CheckedChanged(sender As Object, e As EventArgs) Handles chPptp.CheckedChanged, chProxy.CheckedChanged
        If Not CType(sender, KoChoice).Checked Then Return
        ApplyMode()
        If _loading OrElse _hub Is Nothing OrElse _hub.Demo Then Return
        _hub.Log("VPN kipi seçildi: " & If(chProxy.Checked, "Proxy", "PPTP VPN") & " (Kaydet ile kalıcı olur)", "vpn")
    End Sub

    Private Sub RefreshState()
        If _hub Is Nothing Then Return
        Dim st As VpnState = _hub.VpnNow
        Select Case st
            Case VpnState.Connected : pillState.Text = "Bağlı" : pillState.Tone = KoTone.Ok
            Case VpnState.Connecting : pillState.Text = "Bağlanıyor" : pillState.Tone = KoTone.Info
            Case VpnState.Failed : pillState.Text = "Bağlanamadı" : pillState.Tone = KoTone.Bad
            Case Else : pillState.Text = "Bağlı değil" : pillState.Tone = KoTone.Neutral
        End Select
        pillState.FitWidth()
        pillState.Left = cardVpn.Width - 12 - pillState.Width
        lblStatus.Text = _hub.VpnText
        lblStatus.ForeColor = If(st = VpnState.Failed, BadRed, If(st = VpnState.Connected, OkGreen, TextMuted))
        Dim live As Boolean = st = VpnState.Connected OrElse st = VpnState.Connecting
        btnConnect.Text = If(live, "Kes", "Bağlan")
        btnConnect.Kind = If(live, KoButtonKind.Danger, KoButtonKind.Primary)
        btnConnect.Glyph = If(live, ChrW(&HE711), ChrW(&HE701))
    End Sub

    Private Sub RefreshCount()
        If _hub Is Nothing Then Return
        Dim marked As List(Of String) = _hub.Accounts.Select(Function(a, i) New With {a, i}).
                                        Where(Function(x) x.a.UseVpn).Select(Function(x) _hub.Describe(x.i)).ToList()
        lblCount.Text = If(marked.Count = 0, "VPN ile giren hesap yok", $"{marked.Count} hesap VPN ile girer: " & String.Join(", ", marked))
    End Sub

    Private Sub cmbSuggest_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSuggest.SelectedIndexChanged
        If cmbSuggest.SelectedIndex <= 0 Then Return
        txtServer.Text = CStr(cmbSuggest.SelectedItem)
        cmbSuggest.SelectedIndex = 0
    End Sub

    ''' <summary>The typed values go to the engine (memory) — Kaydet also writes them to disk; Bağlan uses them either way.</summary>
    Private Sub ApplyFields()
        VpnManager.Mode = SelectedMode
        VpnManager.Server = txtServer.Text.Trim()
        VpnManager.UserName = txtUser.Text.Trim()
        VpnManager.Password = txtPassword.Text
        VpnManager.ProxyType = SelectedProxyType
        VpnManager.ProxyHost = txtProxyHost.Text.Trim()
        VpnManager.ProxyPort = TypedPort
        VpnManager.ProxyUser = txtProxyUser.Text.Trim()
        VpnManager.ProxyPassword = txtProxyPassword.Text
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If _hub.Demo Then Return
        Dim serverChanged As Boolean = txtServer.Text.Trim() <> VpnManager.Server
        Dim userChanged As Boolean = txtUser.Text.Trim() <> VpnManager.UserName
        Dim passChanged As Boolean = txtPassword.Text <> VpnManager.Password
        Dim oldMode As VpnMode = VpnManager.Mode
        Dim oldType As ProxyKind = VpnManager.ProxyType
        Dim oldAddr As String = $"{VpnManager.ProxyHost}:{VpnManager.ProxyPort}"
        Dim newAddr As String = $"{txtProxyHost.Text.Trim()}:{TypedPort}"
        Dim pUserChanged As Boolean = txtProxyUser.Text.Trim() <> VpnManager.ProxyUser
        Dim pPassChanged As Boolean = txtProxyPassword.Text <> VpnManager.ProxyPassword
        ApplyFields()
        Dim ok As Boolean
        Try
            ok = VpnManager.SaveSettings()
        Catch ex As Exception
            Services.Logging.BotLog.Fail("vpn", "VPN ayarları kaydedilemedi", ex)
        End Try
        If ok Then
            Dim what As New List(Of String)
            If oldMode <> SelectedMode Then what.Add($"kip {ModeText(oldMode)} → {ModeText(SelectedMode)}")
            If serverChanged Then what.Add("sunucu")
            If userChanged Then what.Add("kullanıcı adı")
            If passChanged Then what.Add("şifre")
            If oldType <> SelectedProxyType Then what.Add($"proxy türü {Services.Vpn.UpstreamProxyEgress.KindText(oldType)} → {Services.Vpn.UpstreamProxyEgress.KindText(SelectedProxyType)}")
            If oldAddr <> newAddr Then what.Add($"proxy adresi {If(oldAddr = ":0", "boş", oldAddr)} → {If(newAddr = ":0", "boş", newAddr)}")
            If pUserChanged Then what.Add("proxy kullanıcı adı")
            If pPassChanged Then what.Add("proxy şifresi")
            _hub.Log("VPN ayarları kaydedildi" & If(what.Count = 0, "", " (" & String.Join(", ", what) & " değişti)"), "vpn")
            lblStatus.Text = "Kaydedildi."
            lblStatus.ForeColor = OkGreen
        Else
            _hub.Warn("VPN ayarları kaydedilemedi", "vpn")
            lblStatus.Text = "Kaydedilemedi."
            lblStatus.ForeColor = BadRed
        End If
    End Sub

    Private Shared Function ModeText(m As VpnMode) As String
        Return If(m = VpnMode.Proxy, "Proxy", "PPTP")
    End Function

    Private Async Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        If _hub.Demo Then Return
        Dim st As VpnState = _hub.VpnNow
        If st = VpnState.Connected OrElse st = VpnState.Connecting Then
            VpnManager.Disconnect()
            RefreshState()
            Return
        End If
        If SelectedMode = VpnMode.Proxy Then
            If txtProxyHost.Text.Trim() = "" OrElse TypedPort < 1 OrElse TypedPort > 65535 Then
                lblStatus.Text = "Proxy adresi ve portu (1-65535) gerekli."
                lblStatus.ForeColor = BadRed
                Return
            End If
        ElseIf txtServer.Text.Trim() = "" OrElse txtUser.Text.Trim() = "" Then
            lblStatus.Text = "Sunucu ve kullanıcı adı gerekli."
            lblStatus.ForeColor = BadRed
            Return
        End If
        ApplyFields()
        btnConnect.Enabled = False
        Try
            Await _hub.ConnectVpnAsync()
        Catch ex As Exception
            lblStatus.Text = "Bağlanamadı: " & ex.Message
        Finally
            btnConnect.Enabled = True
            RefreshState()
        End Try
    End Sub

End Class
