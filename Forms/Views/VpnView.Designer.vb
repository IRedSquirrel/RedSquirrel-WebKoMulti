<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class VpnView
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.cardVpn = New RedSquirrel.WebKoMulti.KoCard()
        Me.pillState = New RedSquirrel.WebKoMulti.KoPill()
        Me.chPptp = New RedSquirrel.WebKoMulti.KoChoice()
        Me.chProxy = New RedSquirrel.WebKoMulti.KoChoice()
        Me.lblCapServer = New System.Windows.Forms.Label()
        Me.txtServer = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.cmbSuggest = New RedSquirrel.WebKoMulti.KoCombo()
        Me.lblCapUser = New System.Windows.Forms.Label()
        Me.txtUser = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapPassword = New System.Windows.Forms.Label()
        Me.txtPassword = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapProxyType = New System.Windows.Forms.Label()
        Me.cmbProxyType = New RedSquirrel.WebKoMulti.KoCombo()
        Me.lblCapProxyHost = New System.Windows.Forms.Label()
        Me.txtProxyHost = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapProxyPort = New System.Windows.Forms.Label()
        Me.txtProxyPort = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapProxyUser = New System.Windows.Forms.Label()
        Me.txtProxyUser = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapProxyPassword = New System.Windows.Forms.Label()
        Me.txtProxyPassword = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.btnSave = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnConnect = New RedSquirrel.WebKoMulti.KoButton()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.lblHint = New System.Windows.Forms.Label()
        Me.cardInfo = New RedSquirrel.WebKoMulti.KoCard()
        Me.lblInfo = New System.Windows.Forms.Label()
        Me.lblCount = New System.Windows.Forms.Label()
        Me.cardVpn.SuspendLayout()
        Me.cardInfo.SuspendLayout()
        Me.SuspendLayout()
        '
        'cardVpn
        '
        Me.cardVpn.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cardVpn.Controls.Add(Me.pillState)
        Me.cardVpn.Controls.Add(Me.chPptp)
        Me.cardVpn.Controls.Add(Me.chProxy)
        Me.cardVpn.Controls.Add(Me.lblCapServer)
        Me.cardVpn.Controls.Add(Me.txtServer)
        Me.cardVpn.Controls.Add(Me.cmbSuggest)
        Me.cardVpn.Controls.Add(Me.lblCapUser)
        Me.cardVpn.Controls.Add(Me.txtUser)
        Me.cardVpn.Controls.Add(Me.lblCapPassword)
        Me.cardVpn.Controls.Add(Me.txtPassword)
        Me.cardVpn.Controls.Add(Me.lblCapProxyType)
        Me.cardVpn.Controls.Add(Me.cmbProxyType)
        Me.cardVpn.Controls.Add(Me.lblCapProxyHost)
        Me.cardVpn.Controls.Add(Me.txtProxyHost)
        Me.cardVpn.Controls.Add(Me.lblCapProxyPort)
        Me.cardVpn.Controls.Add(Me.txtProxyPort)
        Me.cardVpn.Controls.Add(Me.lblCapProxyUser)
        Me.cardVpn.Controls.Add(Me.txtProxyUser)
        Me.cardVpn.Controls.Add(Me.lblCapProxyPassword)
        Me.cardVpn.Controls.Add(Me.txtProxyPassword)
        Me.cardVpn.Controls.Add(Me.btnSave)
        Me.cardVpn.Controls.Add(Me.btnConnect)
        Me.cardVpn.Controls.Add(Me.lblStatus)
        Me.cardVpn.Controls.Add(Me.lblHint)
        Me.cardVpn.Location = New System.Drawing.Point(14, 8)
        Me.cardVpn.Name = "cardVpn"
        Me.cardVpn.Size = New System.Drawing.Size(472, 246)
        Me.cardVpn.TabIndex = 0
        Me.cardVpn.Title = "BAĞLANTI"
        '
        'pillState
        '
        Me.pillState.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pillState.Location = New System.Drawing.Point(340, 8)
        Me.pillState.Name = "pillState"
        Me.pillState.Size = New System.Drawing.Size(120, 22)
        Me.pillState.TabIndex = 0
        Me.pillState.Text = "Bağlı değil"
        '
        'chPptp
        '
        Me.chPptp.Checked = True
        Me.chPptp.Location = New System.Drawing.Point(14, 32)
        Me.chPptp.Name = "chPptp"
        Me.chPptp.Size = New System.Drawing.Size(220, 34)
        Me.chPptp.TabIndex = 20
        Me.chPptp.Text = "PPTP VPN (Windows)"
        '
        'chProxy
        '
        Me.chProxy.Location = New System.Drawing.Point(244, 32)
        Me.chProxy.Name = "chProxy"
        Me.chProxy.Size = New System.Drawing.Size(216, 34)
        Me.chProxy.TabIndex = 21
        Me.chProxy.Text = "Proxy (SOCKS5 / HTTP)"
        '
        'lblCapServer
        '
        Me.lblCapServer.BackColor = System.Drawing.Color.Transparent
        Me.lblCapServer.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapServer.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapServer.Location = New System.Drawing.Point(14, 74)
        Me.lblCapServer.Name = "lblCapServer"
        Me.lblCapServer.Size = New System.Drawing.Size(250, 15)
        Me.lblCapServer.TabIndex = 1
        Me.lblCapServer.Text = "Sunucu"
        '
        'txtServer
        '
        Me.txtServer.Cue = "ör. us16.vpnbook.com"
        Me.txtServer.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtServer.Location = New System.Drawing.Point(14, 90)
        Me.txtServer.MaxLength = 120
        Me.txtServer.Name = "txtServer"
        Me.txtServer.Size = New System.Drawing.Size(250, 30)
        Me.txtServer.TabIndex = 2
        '
        'cmbSuggest
        '
        Me.cmbSuggest.FormattingEnabled = True
        Me.cmbSuggest.Location = New System.Drawing.Point(272, 92)
        Me.cmbSuggest.Name = "cmbSuggest"
        Me.cmbSuggest.Size = New System.Drawing.Size(188, 26)
        Me.cmbSuggest.TabIndex = 3
        '
        'lblCapUser
        '
        Me.lblCapUser.BackColor = System.Drawing.Color.Transparent
        Me.lblCapUser.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapUser.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapUser.Location = New System.Drawing.Point(14, 128)
        Me.lblCapUser.Name = "lblCapUser"
        Me.lblCapUser.Size = New System.Drawing.Size(220, 15)
        Me.lblCapUser.TabIndex = 4
        Me.lblCapUser.Text = "Kullanıcı adı"
        '
        'txtUser
        '
        Me.txtUser.Cue = "kullanıcı adı"
        Me.txtUser.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtUser.Location = New System.Drawing.Point(14, 144)
        Me.txtUser.MaxLength = 80
        Me.txtUser.Name = "txtUser"
        Me.txtUser.Size = New System.Drawing.Size(220, 30)
        Me.txtUser.TabIndex = 5
        '
        'lblCapPassword
        '
        Me.lblCapPassword.BackColor = System.Drawing.Color.Transparent
        Me.lblCapPassword.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapPassword.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapPassword.Location = New System.Drawing.Point(244, 128)
        Me.lblCapPassword.Name = "lblCapPassword"
        Me.lblCapPassword.Size = New System.Drawing.Size(216, 15)
        Me.lblCapPassword.TabIndex = 6
        Me.lblCapPassword.Text = "Şifre"
        '
        'txtPassword
        '
        Me.txtPassword.Cue = "şifre"
        Me.txtPassword.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtPassword.Location = New System.Drawing.Point(244, 144)
        Me.txtPassword.MaxLength = 80
        Me.txtPassword.Name = "txtPassword"
        Me.txtPassword.Password = True
        Me.txtPassword.Size = New System.Drawing.Size(216, 30)
        Me.txtPassword.TabIndex = 7
        '
        'lblCapProxyType
        '
        Me.lblCapProxyType.BackColor = System.Drawing.Color.Transparent
        Me.lblCapProxyType.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapProxyType.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapProxyType.Location = New System.Drawing.Point(14, 74)
        Me.lblCapProxyType.Name = "lblCapProxyType"
        Me.lblCapProxyType.Size = New System.Drawing.Size(90, 15)
        Me.lblCapProxyType.TabIndex = 22
        Me.lblCapProxyType.Text = "Tür"
        Me.lblCapProxyType.Visible = False
        '
        'cmbProxyType
        '
        Me.cmbProxyType.FormattingEnabled = True
        Me.cmbProxyType.Location = New System.Drawing.Point(14, 92)
        Me.cmbProxyType.Name = "cmbProxyType"
        Me.cmbProxyType.Size = New System.Drawing.Size(90, 26)
        Me.cmbProxyType.TabIndex = 23
        Me.cmbProxyType.Visible = False
        '
        'lblCapProxyHost
        '
        Me.lblCapProxyHost.BackColor = System.Drawing.Color.Transparent
        Me.lblCapProxyHost.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapProxyHost.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapProxyHost.Location = New System.Drawing.Point(112, 74)
        Me.lblCapProxyHost.Name = "lblCapProxyHost"
        Me.lblCapProxyHost.Size = New System.Drawing.Size(250, 15)
        Me.lblCapProxyHost.TabIndex = 24
        Me.lblCapProxyHost.Text = "Proxy adresi"
        Me.lblCapProxyHost.Visible = False
        '
        'txtProxyHost
        '
        Me.txtProxyHost.Cue = "ör. 203.0.113.5"
        Me.txtProxyHost.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtProxyHost.Location = New System.Drawing.Point(112, 90)
        Me.txtProxyHost.MaxLength = 253
        Me.txtProxyHost.Name = "txtProxyHost"
        Me.txtProxyHost.Size = New System.Drawing.Size(250, 30)
        Me.txtProxyHost.TabIndex = 25
        Me.txtProxyHost.Visible = False
        '
        'lblCapProxyPort
        '
        Me.lblCapProxyPort.BackColor = System.Drawing.Color.Transparent
        Me.lblCapProxyPort.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapProxyPort.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapProxyPort.Location = New System.Drawing.Point(370, 74)
        Me.lblCapProxyPort.Name = "lblCapProxyPort"
        Me.lblCapProxyPort.Size = New System.Drawing.Size(90, 15)
        Me.lblCapProxyPort.TabIndex = 26
        Me.lblCapProxyPort.Text = "Port"
        Me.lblCapProxyPort.Visible = False
        '
        'txtProxyPort
        '
        Me.txtProxyPort.Cue = "ör. 1080"
        Me.txtProxyPort.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtProxyPort.Location = New System.Drawing.Point(370, 90)
        Me.txtProxyPort.MaxLength = 5
        Me.txtProxyPort.Name = "txtProxyPort"
        Me.txtProxyPort.Size = New System.Drawing.Size(90, 30)
        Me.txtProxyPort.TabIndex = 27
        Me.txtProxyPort.Visible = False
        '
        'lblCapProxyUser
        '
        Me.lblCapProxyUser.BackColor = System.Drawing.Color.Transparent
        Me.lblCapProxyUser.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapProxyUser.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapProxyUser.Location = New System.Drawing.Point(14, 128)
        Me.lblCapProxyUser.Name = "lblCapProxyUser"
        Me.lblCapProxyUser.Size = New System.Drawing.Size(220, 15)
        Me.lblCapProxyUser.TabIndex = 28
        Me.lblCapProxyUser.Text = "Kullanıcı adı (isteğe bağlı)"
        Me.lblCapProxyUser.Visible = False
        '
        'txtProxyUser
        '
        Me.txtProxyUser.Cue = "kullanıcı adı"
        Me.txtProxyUser.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtProxyUser.Location = New System.Drawing.Point(14, 144)
        Me.txtProxyUser.MaxLength = 255
        Me.txtProxyUser.Name = "txtProxyUser"
        Me.txtProxyUser.Size = New System.Drawing.Size(220, 30)
        Me.txtProxyUser.TabIndex = 29
        Me.txtProxyUser.Visible = False
        '
        'lblCapProxyPassword
        '
        Me.lblCapProxyPassword.BackColor = System.Drawing.Color.Transparent
        Me.lblCapProxyPassword.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapProxyPassword.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapProxyPassword.Location = New System.Drawing.Point(244, 128)
        Me.lblCapProxyPassword.Name = "lblCapProxyPassword"
        Me.lblCapProxyPassword.Size = New System.Drawing.Size(216, 15)
        Me.lblCapProxyPassword.TabIndex = 30
        Me.lblCapProxyPassword.Text = "Şifre (isteğe bağlı)"
        Me.lblCapProxyPassword.Visible = False
        '
        'txtProxyPassword
        '
        Me.txtProxyPassword.Cue = "şifre"
        Me.txtProxyPassword.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtProxyPassword.Location = New System.Drawing.Point(244, 144)
        Me.txtProxyPassword.MaxLength = 255
        Me.txtProxyPassword.Name = "txtProxyPassword"
        Me.txtProxyPassword.Password = True
        Me.txtProxyPassword.Size = New System.Drawing.Size(216, 30)
        Me.txtProxyPassword.TabIndex = 31
        Me.txtProxyPassword.Visible = False
        '
        'btnSave
        '
        Me.btnSave.Glyph = Global.Microsoft.VisualBasic.ChrW(59214)
        Me.btnSave.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Success
        Me.btnSave.Location = New System.Drawing.Point(14, 184)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(100, 30)
        Me.btnSave.TabIndex = 8
        Me.btnSave.Text = "Kaydet"
        '
        'btnConnect
        '
        Me.btnConnect.Glyph = Global.Microsoft.VisualBasic.ChrW(59137)
        Me.btnConnect.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Primary
        Me.btnConnect.Location = New System.Drawing.Point(120, 184)
        Me.btnConnect.Name = "btnConnect"
        Me.btnConnect.Size = New System.Drawing.Size(114, 30)
        Me.btnConnect.TabIndex = 9
        Me.btnConnect.Text = "Bağlan"
        '
        'lblStatus
        '
        Me.lblStatus.AutoEllipsis = True
        Me.lblStatus.BackColor = System.Drawing.Color.Transparent
        Me.lblStatus.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblStatus.Location = New System.Drawing.Point(244, 184)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(216, 32)
        Me.lblStatus.TabIndex = 10
        Me.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'lblHint
        '
        Me.lblHint.BackColor = System.Drawing.Color.Transparent
        Me.lblHint.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblHint.ForeColor = System.Drawing.Color.FromArgb(CType(CType(251, Byte), Integer), CType(CType(191, Byte), Integer), CType(CType(36, Byte), Integer))
        Me.lblHint.Location = New System.Drawing.Point(14, 222)
        Me.lblHint.Name = "lblHint"
        Me.lblHint.Size = New System.Drawing.Size(446, 18)
        Me.lblHint.TabIndex = 11
        Me.lblHint.Text = "Şifre 1-2 haftada bir değişir — VPNBook sayfasından yenisini alın."
        '
        'cardInfo
        '
        Me.cardInfo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cardInfo.Controls.Add(Me.lblInfo)
        Me.cardInfo.Controls.Add(Me.lblCount)
        Me.cardInfo.Location = New System.Drawing.Point(14, 262)
        Me.cardInfo.Name = "cardInfo"
        Me.cardInfo.Size = New System.Drawing.Size(472, 104)
        Me.cardInfo.TabIndex = 1
        Me.cardInfo.Title = "HANGİ HESAPLAR"
        '
        'lblInfo
        '
        Me.lblInfo.BackColor = System.Drawing.Color.Transparent
        Me.lblInfo.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblInfo.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblInfo.Location = New System.Drawing.Point(14, 28)
        Me.lblInfo.Name = "lblInfo"
        Me.lblInfo.Size = New System.Drawing.Size(446, 46)
        Me.lblInfo.TabIndex = 0
        Me.lblInfo.Text = "Yalnız Hesaplar sekmesinde ""VPN ile gir"" işaretli hesaplar VPN'den geçer; bilgisayarın geri kalanı doğrudan bağlanır. İşaretli hesap VPN bağlanamazsa başlatılmaz."
        '
        'lblCount
        '
        Me.lblCount.BackColor = System.Drawing.Color.Transparent
        Me.lblCount.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCount.ForeColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(233, Byte), Integer), CType(CType(244, Byte), Integer))
        Me.lblCount.Location = New System.Drawing.Point(14, 78)
        Me.lblCount.Name = "lblCount"
        Me.lblCount.Size = New System.Drawing.Size(446, 18)
        Me.lblCount.TabIndex = 1
        Me.lblCount.Text = "VPN ile giren hesap yok"
        '
        'VpnView
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.Controls.Add(Me.cardVpn)
        Me.Controls.Add(Me.cardInfo)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Name = "VpnView"
        Me.Size = New System.Drawing.Size(500, 378)
        Me.cardVpn.ResumeLayout(False)
        Me.cardInfo.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents cardVpn As RedSquirrel.WebKoMulti.KoCard
    Friend WithEvents pillState As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents chPptp As RedSquirrel.WebKoMulti.KoChoice
    Friend WithEvents chProxy As RedSquirrel.WebKoMulti.KoChoice
    Friend WithEvents lblCapServer As System.Windows.Forms.Label
    Friend WithEvents txtServer As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents cmbSuggest As RedSquirrel.WebKoMulti.KoCombo
    Friend WithEvents lblCapUser As System.Windows.Forms.Label
    Friend WithEvents txtUser As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapPassword As System.Windows.Forms.Label
    Friend WithEvents txtPassword As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapProxyType As System.Windows.Forms.Label
    Friend WithEvents cmbProxyType As RedSquirrel.WebKoMulti.KoCombo
    Friend WithEvents lblCapProxyHost As System.Windows.Forms.Label
    Friend WithEvents txtProxyHost As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapProxyPort As System.Windows.Forms.Label
    Friend WithEvents txtProxyPort As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapProxyUser As System.Windows.Forms.Label
    Friend WithEvents txtProxyUser As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapProxyPassword As System.Windows.Forms.Label
    Friend WithEvents txtProxyPassword As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents btnSave As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnConnect As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents lblStatus As System.Windows.Forms.Label
    Friend WithEvents lblHint As System.Windows.Forms.Label
    Friend WithEvents cardInfo As RedSquirrel.WebKoMulti.KoCard
    Friend WithEvents lblInfo As System.Windows.Forms.Label
    Friend WithEvents lblCount As System.Windows.Forms.Label

End Class
