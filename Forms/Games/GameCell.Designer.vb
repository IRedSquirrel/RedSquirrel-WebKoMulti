<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class GameCell
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
        Me.components = New System.ComponentModel.Container()
        Me.pnlBar = New System.Windows.Forms.Panel()
        Me.lblName = New System.Windows.Forms.Label()
        Me.pillState = New RedSquirrel.WebKoMulti.KoPill()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.btnLead = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnStart = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnStop = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnFull = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnSteps = New RedSquirrel.WebKoMulti.KoButton()
        Me.tglMirror = New RedSquirrel.WebKoMulti.KoToggle()
        Me.cmbMirrorMode = New RedSquirrel.WebKoMulti.KoCombo()
        Me.pillMirror = New RedSquirrel.WebKoMulti.KoPill()
        Me.pillVpn = New RedSquirrel.WebKoMulti.KoPill()
        Me.pnlMirror = New System.Windows.Forms.Panel()
        Me.lblMirror = New System.Windows.Forms.Label()
        Me.pnlView = New System.Windows.Forms.Panel()
        Me.lblPlaceholder = New System.Windows.Forms.Label()
        Me.tips = New System.Windows.Forms.ToolTip(Me.components)
        Me.pnlBar.SuspendLayout()
        Me.pnlMirror.SuspendLayout()
        Me.pnlView.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlBar
        '
        Me.pnlBar.BackColor = System.Drawing.Color.FromArgb(CType(CType(16, Byte), Integer), CType(CType(19, Byte), Integer), CType(CType(42, Byte), Integer))
        Me.pnlBar.Controls.Add(Me.lblName)
        Me.pnlBar.Controls.Add(Me.pillState)
        Me.pnlBar.Controls.Add(Me.lblStatus)
        Me.pnlBar.Controls.Add(Me.btnLead)
        Me.pnlBar.Controls.Add(Me.btnStart)
        Me.pnlBar.Controls.Add(Me.btnStop)
        Me.pnlBar.Controls.Add(Me.btnFull)
        Me.pnlBar.Controls.Add(Me.btnSteps)
        Me.pnlBar.Controls.Add(Me.tglMirror)
        Me.pnlBar.Controls.Add(Me.cmbMirrorMode)
        Me.pnlBar.Controls.Add(Me.pillMirror)
        Me.pnlBar.Controls.Add(Me.pillVpn)
        Me.pnlBar.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlBar.Location = New System.Drawing.Point(0, 0)
        Me.pnlBar.Name = "pnlBar"
        Me.pnlBar.Size = New System.Drawing.Size(640, 32)
        Me.pnlBar.TabIndex = 0
        '
        'lblName
        '
        Me.lblName.AutoEllipsis = True
        Me.lblName.BackColor = System.Drawing.Color.Transparent
        Me.lblName.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblName.ForeColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(233, Byte), Integer), CType(CType(244, Byte), Integer))
        Me.lblName.Location = New System.Drawing.Point(8, 7)
        Me.lblName.Name = "lblName"
        Me.lblName.Size = New System.Drawing.Size(120, 18)
        Me.lblName.TabIndex = 0
        Me.lblName.Text = "2 · Okçu"
        '
        'pillState
        '
        Me.pillState.Location = New System.Drawing.Point(132, 5)
        Me.pillState.Name = "pillState"
        Me.pillState.Size = New System.Drawing.Size(100, 22)
        Me.pillState.TabIndex = 1
        Me.pillState.Text = "Başlamadı"
        '
        'lblStatus
        '
        Me.lblStatus.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblStatus.AutoEllipsis = True
        Me.lblStatus.BackColor = System.Drawing.Color.Transparent
        Me.lblStatus.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblStatus.Location = New System.Drawing.Point(238, 8)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(266, 16)
        Me.lblStatus.TabIndex = 2
        '
        'btnLead
        '
        Me.btnLead.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnLead.Glyph = Global.Microsoft.VisualBasic.ChrW(59188)
        Me.btnLead.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnLead.Location = New System.Drawing.Point(542, 4)
        Me.btnLead.Name = "btnLead"
        Me.btnLead.Radius = 5
        Me.btnLead.Size = New System.Drawing.Size(28, 24)
        Me.btnLead.TabIndex = 3
        Me.tips.SetToolTip(Me.btnLead, "Lider yap — görünüm lider penceresine taşınır")
        '
        'btnStart
        '
        Me.btnStart.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnStart.Glyph = Global.Microsoft.VisualBasic.ChrW(59240)
        Me.btnStart.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnStart.Location = New System.Drawing.Point(574, 4)
        Me.btnStart.Name = "btnStart"
        Me.btnStart.Radius = 5
        Me.btnStart.Size = New System.Drawing.Size(28, 24)
        Me.btnStart.TabIndex = 4
        Me.tips.SetToolTip(Me.btnStart, "Başlat / yeniden giriş")
        '
        'btnStop
        '
        Me.btnStop.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnStop.Glyph = Global.Microsoft.VisualBasic.ChrW(59162)
        Me.btnStop.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnStop.Location = New System.Drawing.Point(606, 4)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Radius = 5
        Me.btnStop.Size = New System.Drawing.Size(28, 24)
        Me.btnStop.TabIndex = 5
        Me.tips.SetToolTip(Me.btnStop, "Girişi durdur")
        '
        'btnFull
        '
        Me.btnFull.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnFull.Glyph = Global.Microsoft.VisualBasic.ChrW(59200)
        Me.btnFull.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnFull.Location = New System.Drawing.Point(542, 4)
        Me.btnFull.Name = "btnFull"
        Me.btnFull.Radius = 5
        Me.btnFull.Size = New System.Drawing.Size(28, 24)
        Me.btnFull.TabIndex = 6
        Me.btnFull.Visible = False
        Me.tips.SetToolTip(Me.btnFull, "Tam ekran / pencere")
        '
        'btnSteps
        '
        Me.btnSteps.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSteps.Enabled = False
        Me.btnSteps.Glyph = Global.Microsoft.VisualBasic.ChrW(59541)
        Me.btnSteps.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnSteps.Location = New System.Drawing.Point(510, 4)
        Me.btnSteps.Name = "btnSteps"
        Me.btnSteps.Radius = 5
        Me.btnSteps.Size = New System.Drawing.Size(28, 24)
        Me.btnSteps.TabIndex = 7
        Me.tips.SetToolTip(Me.btnSteps, "Oyun içi adımları yeniden çalıştır: pencereleri kapat, sesi kapat, PM'leri kapat, oyun ayarını uygula")
        '
        'tglMirror
        '
        Me.tglMirror.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tglMirror.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.tglMirror.Location = New System.Drawing.Point(398, 6)
        Me.tglMirror.Name = "tglMirror"
        Me.tglMirror.Size = New System.Drawing.Size(104, 22)
        Me.tglMirror.TabIndex = 8
        Me.tglMirror.Text = "Yansıtma"
        Me.tglMirror.Visible = False
        Me.tips.SetToolTip(Me.tglMirror, "Açıkken liderde tıkladığın / sürüklediğin / bastığın her şey oyundaki takipçilerde tekrarlanır")
        '
        'cmbMirrorMode
        '
        Me.cmbMirrorMode.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmbMirrorMode.FormattingEnabled = True
        Me.cmbMirrorMode.Items.AddRange(New Object() {"Yalnız klavye", "Klavye + fare"})
        Me.cmbMirrorMode.Location = New System.Drawing.Point(276, 4)
        Me.cmbMirrorMode.Name = "cmbMirrorMode"
        Me.cmbMirrorMode.Size = New System.Drawing.Size(116, 26)
        Me.cmbMirrorMode.TabIndex = 10
        Me.cmbMirrorMode.Visible = False
        Me.tips.SetToolTip(Me.cmbMirrorMode, "Yalnız klavye: yalnız tuşlar takipçilere gider · Klavye + fare: tık, sürükleme ve tekerlek de gider")
        '
        'pillMirror
        '
        Me.pillMirror.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pillMirror.Location = New System.Drawing.Point(410, 5)
        Me.pillMirror.Name = "pillMirror"
        Me.pillMirror.Size = New System.Drawing.Size(94, 22)
        Me.pillMirror.TabIndex = 9
        Me.pillMirror.Text = "yansıtılıyor"
        Me.pillMirror.Tone = RedSquirrel.WebKoMulti.KoTone.Warn
        Me.pillMirror.Visible = False
        '
        'pillVpn
        '
        Me.pillVpn.Location = New System.Drawing.Point(238, 5)
        Me.pillVpn.Name = "pillVpn"
        Me.pillVpn.Size = New System.Drawing.Size(60, 22)
        Me.pillVpn.TabIndex = 11
        Me.pillVpn.Text = "VPN"
        Me.pillVpn.Tone = RedSquirrel.WebKoMulti.KoTone.Info
        Me.pillVpn.Visible = False
        '
        'pnlMirror
        '
        Me.pnlMirror.BackColor = System.Drawing.Color.FromArgb(CType(CType(234, Byte), Integer), CType(CType(118, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.pnlMirror.Controls.Add(Me.lblMirror)
        Me.pnlMirror.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlMirror.Location = New System.Drawing.Point(0, 32)
        Me.pnlMirror.Name = "pnlMirror"
        Me.pnlMirror.Size = New System.Drawing.Size(640, 26)
        Me.pnlMirror.TabIndex = 2
        Me.pnlMirror.Visible = False
        '
        'lblMirror
        '
        Me.lblMirror.AutoEllipsis = True
        Me.lblMirror.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lblMirror.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblMirror.ForeColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(14, Byte), Integer), CType(CType(4, Byte), Integer))
        Me.lblMirror.Location = New System.Drawing.Point(0, 0)
        Me.lblMirror.Name = "lblMirror"
        Me.lblMirror.Padding = New System.Windows.Forms.Padding(8, 0, 8, 0)
        Me.lblMirror.Size = New System.Drawing.Size(640, 26)
        Me.lblMirror.TabIndex = 0
        Me.lblMirror.Text = "YANSITMA AÇIK"
        Me.lblMirror.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'pnlView
        '
        Me.pnlView.AutoScroll = True
        Me.pnlView.BackColor = System.Drawing.Color.Black
        Me.pnlView.Controls.Add(Me.lblPlaceholder)
        Me.pnlView.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlView.Location = New System.Drawing.Point(0, 32)
        Me.pnlView.Name = "pnlView"
        Me.pnlView.Size = New System.Drawing.Size(640, 400)
        Me.pnlView.TabIndex = 1
        '
        'lblPlaceholder
        '
        Me.lblPlaceholder.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lblPlaceholder.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblPlaceholder.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblPlaceholder.Location = New System.Drawing.Point(0, 0)
        Me.lblPlaceholder.Name = "lblPlaceholder"
        Me.lblPlaceholder.Size = New System.Drawing.Size(640, 400)
        Me.lblPlaceholder.TabIndex = 0
        Me.lblPlaceholder.Text = "Oyun başlatılınca burada görünür (1280×800, kaydırılabilir)."
        Me.lblPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'GameCell
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.Black
        Me.Controls.Add(Me.pnlView)
        Me.Controls.Add(Me.pnlMirror)
        Me.Controls.Add(Me.pnlBar)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Margin = New System.Windows.Forms.Padding(1)
        Me.Name = "GameCell"
        Me.Size = New System.Drawing.Size(640, 432)
        Me.pnlBar.ResumeLayout(False)
        Me.pnlMirror.ResumeLayout(False)
        Me.pnlView.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents pnlBar As System.Windows.Forms.Panel
    Friend WithEvents lblName As System.Windows.Forms.Label
    Friend WithEvents pillState As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents lblStatus As System.Windows.Forms.Label
    Friend WithEvents btnLead As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnStart As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnStop As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnFull As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnSteps As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents tglMirror As RedSquirrel.WebKoMulti.KoToggle
    Friend WithEvents cmbMirrorMode As RedSquirrel.WebKoMulti.KoCombo
    Friend WithEvents pillMirror As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents pillVpn As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents pnlMirror As System.Windows.Forms.Panel
    Friend WithEvents lblMirror As System.Windows.Forms.Label
    Friend WithEvents pnlView As System.Windows.Forms.Panel
    Friend WithEvents lblPlaceholder As System.Windows.Forms.Label
    Friend WithEvents tips As System.Windows.Forms.ToolTip

End Class
