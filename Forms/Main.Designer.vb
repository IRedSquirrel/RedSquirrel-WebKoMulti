<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Main
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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
        Me.pnlHeader = New System.Windows.Forms.Panel()
        Me.picLogo = New RedSquirrel.WebKoMulti.BrandLogo()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.pillProduct = New RedSquirrel.WebKoMulti.KoPill()
        Me.btnMinimize = New System.Windows.Forms.Label()
        Me.btnClose = New System.Windows.Forms.Label()
        Me.pnlAccent = New System.Windows.Forms.Panel()
        Me.cardSummary = New RedSquirrel.WebKoMulti.KoCard()
        Me.pillLeader = New RedSquirrel.WebKoMulti.KoPill()
        Me.lblSummary = New System.Windows.Forms.Label()
        Me.lblMirror = New System.Windows.Forms.Label()
        Me.lblFollow = New System.Windows.Forms.Label()
        Me.lblFollowCaption = New System.Windows.Forms.Label()
        Me.tabs = New RedSquirrel.WebKoMulti.KoTabStrip()
        Me.pnlHost = New System.Windows.Forms.Panel()
        Me.pnlFooter = New System.Windows.Forms.Panel()
        Me.btnStartAll = New RedSquirrel.WebKoMulti.KoButton()
        Me.lblFooterMain = New System.Windows.Forms.Label()
        Me.lblFooterStatus = New System.Windows.Forms.Label()
        Me.btnGames = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnStopAll = New RedSquirrel.WebKoMulti.KoButton()
        Me.pnlFooterLine = New System.Windows.Forms.Panel()
        Me.tmrRefresh = New System.Windows.Forms.Timer(Me.components)
        Me.pnlHeader.SuspendLayout()
        Me.cardSummary.SuspendLayout()
        Me.pnlFooter.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlHeader
        '
        Me.pnlHeader.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(12, Byte), Integer), CType(CType(28, Byte), Integer))
        Me.pnlHeader.Controls.Add(Me.picLogo)
        Me.pnlHeader.Controls.Add(Me.lblTitle)
        Me.pnlHeader.Controls.Add(Me.pillProduct)
        Me.pnlHeader.Controls.Add(Me.btnMinimize)
        Me.pnlHeader.Controls.Add(Me.btnClose)
        Me.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlHeader.Location = New System.Drawing.Point(0, 0)
        Me.pnlHeader.Name = "pnlHeader"
        Me.pnlHeader.Size = New System.Drawing.Size(500, 44)
        Me.pnlHeader.TabIndex = 0
        '
        'picLogo
        '
        Me.picLogo.Location = New System.Drawing.Point(14, 10)
        Me.picLogo.Name = "picLogo"
        Me.picLogo.Size = New System.Drawing.Size(24, 24)
        Me.picLogo.TabIndex = 5
        '
        'lblTitle
        '
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 10.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(129, Byte), Integer), CType(CType(140, Byte), Integer), CType(CType(248, Byte), Integer))
        Me.lblTitle.Location = New System.Drawing.Point(42, 12)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(86, 19)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "RedSquirrel"
        '
        'pillProduct
        '
        Me.pillProduct.Font = New System.Drawing.Font("Segoe UI", 7.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.pillProduct.Location = New System.Drawing.Point(134, 12)
        Me.pillProduct.Name = "pillProduct"
        Me.pillProduct.ShowDot = False
        Me.pillProduct.Size = New System.Drawing.Size(86, 20)
        Me.pillProduct.TabIndex = 1
        Me.pillProduct.Text = "WebKo Multi"
        Me.pillProduct.Tone = RedSquirrel.WebKoMulti.KoTone.Primary
        '
        'btnMinimize
        '
        Me.btnMinimize.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnMinimize.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnMinimize.Font = New System.Drawing.Font("Segoe MDL2 Assets", 9.0!)
        Me.btnMinimize.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.btnMinimize.Location = New System.Drawing.Point(430, 0)
        Me.btnMinimize.Name = "btnMinimize"
        Me.btnMinimize.Size = New System.Drawing.Size(35, 44)
        Me.btnMinimize.TabIndex = 4
        Me.btnMinimize.Text = Global.Microsoft.VisualBasic.ChrW(59681)
        Me.btnMinimize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btnClose
        '
        Me.btnClose.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnClose.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnClose.Font = New System.Drawing.Font("Segoe MDL2 Assets", 9.0!)
        Me.btnClose.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.btnClose.Location = New System.Drawing.Point(465, 0)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(35, 44)
        Me.btnClose.TabIndex = 5
        Me.btnClose.Text = Global.Microsoft.VisualBasic.ChrW(59579)
        Me.btnClose.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'pnlAccent
        '
        Me.pnlAccent.BackColor = System.Drawing.Color.FromArgb(CType(CType(129, Byte), Integer), CType(CType(140, Byte), Integer), CType(CType(248, Byte), Integer))
        Me.pnlAccent.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlAccent.Location = New System.Drawing.Point(0, 44)
        Me.pnlAccent.Name = "pnlAccent"
        Me.pnlAccent.Size = New System.Drawing.Size(500, 2)
        Me.pnlAccent.TabIndex = 1
        '
        'cardSummary
        '
        Me.cardSummary.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cardSummary.Controls.Add(Me.pillLeader)
        Me.cardSummary.Controls.Add(Me.lblSummary)
        Me.cardSummary.Controls.Add(Me.lblMirror)
        Me.cardSummary.Controls.Add(Me.lblFollow)
        Me.cardSummary.Controls.Add(Me.lblFollowCaption)
        Me.cardSummary.Location = New System.Drawing.Point(14, 56)
        Me.cardSummary.Name = "cardSummary"
        Me.cardSummary.Size = New System.Drawing.Size(472, 50)
        Me.cardSummary.TabIndex = 2
        '
        'pillLeader
        '
        Me.pillLeader.Location = New System.Drawing.Point(12, 14)
        Me.pillLeader.Name = "pillLeader"
        Me.pillLeader.Size = New System.Drawing.Size(150, 22)
        Me.pillLeader.TabIndex = 0
        Me.pillLeader.Text = "Lider: —"
        Me.pillLeader.Tone = RedSquirrel.WebKoMulti.KoTone.Warn
        '
        'lblSummary
        '
        Me.lblSummary.BackColor = System.Drawing.Color.Transparent
        Me.lblSummary.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblSummary.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblSummary.Location = New System.Drawing.Point(172, 7)
        Me.lblSummary.Name = "lblSummary"
        Me.lblSummary.Size = New System.Drawing.Size(200, 19)
        Me.lblSummary.TabIndex = 1
        Me.lblSummary.Text = "0 hesap · 0 oyunda"
        Me.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'lblMirror
        '
        Me.lblMirror.BackColor = System.Drawing.Color.Transparent
        Me.lblMirror.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblMirror.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblMirror.Location = New System.Drawing.Point(172, 27)
        Me.lblMirror.Name = "lblMirror"
        Me.lblMirror.Size = New System.Drawing.Size(200, 16)
        Me.lblMirror.TabIndex = 4
        Me.lblMirror.Text = "Yansıtma: kapalı"
        Me.lblMirror.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'lblFollow
        '
        Me.lblFollow.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblFollow.BackColor = System.Drawing.Color.Transparent
        Me.lblFollow.Font = New System.Drawing.Font("Segoe UI", 13.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblFollow.ForeColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(233, Byte), Integer), CType(CType(244, Byte), Integer))
        Me.lblFollow.Location = New System.Drawing.Point(370, 4)
        Me.lblFollow.Name = "lblFollow"
        Me.lblFollow.Size = New System.Drawing.Size(90, 24)
        Me.lblFollow.TabIndex = 2
        Me.lblFollow.Text = "Kapalı"
        Me.lblFollow.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'lblFollowCaption
        '
        Me.lblFollowCaption.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblFollowCaption.BackColor = System.Drawing.Color.Transparent
        Me.lblFollowCaption.Font = New System.Drawing.Font("Segoe UI", 7.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblFollowCaption.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblFollowCaption.Location = New System.Drawing.Point(356, 29)
        Me.lblFollowCaption.Name = "lblFollowCaption"
        Me.lblFollowCaption.Size = New System.Drawing.Size(104, 14)
        Me.lblFollowCaption.TabIndex = 3
        Me.lblFollowCaption.Text = "takip"
        Me.lblFollowCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tabs
        '
        Me.tabs.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tabs.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.tabs.Location = New System.Drawing.Point(8, 112)
        Me.tabs.Name = "tabs"
        Me.tabs.Size = New System.Drawing.Size(484, 36)
        Me.tabs.TabIndex = 3
        Me.tabs.Tabs = "Hesaplar|Oyunlar|Takip|VPN|Kayıt"
        '
        'pnlHost
        '
        Me.pnlHost.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pnlHost.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.pnlHost.Location = New System.Drawing.Point(0, 150)
        Me.pnlHost.Name = "pnlHost"
        Me.pnlHost.Size = New System.Drawing.Size(500, 378)
        Me.pnlHost.TabIndex = 4
        '
        'pnlFooter
        '
        Me.pnlFooter.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(12, Byte), Integer), CType(CType(28, Byte), Integer))
        Me.pnlFooter.Controls.Add(Me.btnStartAll)
        Me.pnlFooter.Controls.Add(Me.lblFooterMain)
        Me.pnlFooter.Controls.Add(Me.lblFooterStatus)
        Me.pnlFooter.Controls.Add(Me.btnGames)
        Me.pnlFooter.Controls.Add(Me.btnStopAll)
        Me.pnlFooter.Controls.Add(Me.pnlFooterLine)
        Me.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.pnlFooter.Location = New System.Drawing.Point(0, 528)
        Me.pnlFooter.Name = "pnlFooter"
        Me.pnlFooter.Size = New System.Drawing.Size(500, 52)
        Me.pnlFooter.TabIndex = 5
        '
        'btnStartAll
        '
        Me.btnStartAll.Glyph = Global.Microsoft.VisualBasic.ChrW(59240)
        Me.btnStartAll.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Primary
        Me.btnStartAll.Location = New System.Drawing.Point(14, 10)
        Me.btnStartAll.Name = "btnStartAll"
        Me.btnStartAll.Size = New System.Drawing.Size(132, 32)
        Me.btnStartAll.TabIndex = 0
        Me.btnStartAll.Text = "Hepsini başlat"
        '
        'lblFooterMain
        '
        Me.lblFooterMain.BackColor = System.Drawing.Color.Transparent
        Me.lblFooterMain.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblFooterMain.ForeColor = System.Drawing.Color.FromArgb(CType(CType(200, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(228, Byte), Integer))
        Me.lblFooterMain.Location = New System.Drawing.Point(156, 9)
        Me.lblFooterMain.Name = "lblFooterMain"
        Me.lblFooterMain.Size = New System.Drawing.Size(150, 17)
        Me.lblFooterMain.TabIndex = 1
        Me.lblFooterMain.Text = "0 hesap açık"
        '
        'lblFooterStatus
        '
        Me.lblFooterStatus.BackColor = System.Drawing.Color.Transparent
        Me.lblFooterStatus.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblFooterStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblFooterStatus.Location = New System.Drawing.Point(156, 27)
        Me.lblFooterStatus.Name = "lblFooterStatus"
        Me.lblFooterStatus.Size = New System.Drawing.Size(150, 16)
        Me.lblFooterStatus.TabIndex = 2
        Me.lblFooterStatus.Text = "Giriş yapan yok"
        '
        'btnGames
        '
        Me.btnGames.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnGames.Glyph = Global.Microsoft.VisualBasic.ChrW(59559)
        Me.btnGames.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnGames.Location = New System.Drawing.Point(312, 10)
        Me.btnGames.Name = "btnGames"
        Me.btnGames.Size = New System.Drawing.Size(84, 32)
        Me.btnGames.TabIndex = 3
        Me.btnGames.Text = "Oyunlar"
        '
        'btnStopAll
        '
        Me.btnStopAll.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnStopAll.Glyph = Global.Microsoft.VisualBasic.ChrW(59162)
        Me.btnStopAll.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Danger
        Me.btnStopAll.Location = New System.Drawing.Point(402, 10)
        Me.btnStopAll.Name = "btnStopAll"
        Me.btnStopAll.Size = New System.Drawing.Size(84, 32)
        Me.btnStopAll.TabIndex = 4
        Me.btnStopAll.Text = "Durdur"
        '
        'pnlFooterLine
        '
        Me.pnlFooterLine.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(32, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.pnlFooterLine.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlFooterLine.Location = New System.Drawing.Point(0, 0)
        Me.pnlFooterLine.Name = "pnlFooterLine"
        Me.pnlFooterLine.Size = New System.Drawing.Size(500, 1)
        Me.pnlFooterLine.TabIndex = 5
        '
        'tmrRefresh
        '
        Me.tmrRefresh.Interval = 1000
        '
        'Main
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(500, 580)
        Me.Controls.Add(Me.pnlHost)
        Me.Controls.Add(Me.tabs)
        Me.Controls.Add(Me.cardSummary)
        Me.Controls.Add(Me.pnlFooter)
        Me.Controls.Add(Me.pnlAccent)
        Me.Controls.Add(Me.pnlHeader)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.ForeColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(233, Byte), Integer), CType(CType(244, Byte), Integer))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "Main"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "RedSquirrel - WebKo Multi"
        Me.pnlHeader.ResumeLayout(False)
        Me.pnlHeader.PerformLayout()
        Me.cardSummary.ResumeLayout(False)
        Me.pnlFooter.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents pnlHeader As System.Windows.Forms.Panel
    Friend WithEvents picLogo As RedSquirrel.WebKoMulti.BrandLogo
    Friend WithEvents lblTitle As System.Windows.Forms.Label
    Friend WithEvents pillProduct As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents btnMinimize As System.Windows.Forms.Label
    Friend WithEvents btnClose As System.Windows.Forms.Label
    Friend WithEvents pnlAccent As System.Windows.Forms.Panel
    Friend WithEvents cardSummary As RedSquirrel.WebKoMulti.KoCard
    Friend WithEvents pillLeader As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents lblSummary As System.Windows.Forms.Label
    Friend WithEvents lblMirror As System.Windows.Forms.Label
    Friend WithEvents lblFollow As System.Windows.Forms.Label
    Friend WithEvents lblFollowCaption As System.Windows.Forms.Label
    Friend WithEvents tabs As RedSquirrel.WebKoMulti.KoTabStrip
    Friend WithEvents pnlHost As System.Windows.Forms.Panel
    Friend WithEvents pnlFooter As System.Windows.Forms.Panel
    Friend WithEvents btnStartAll As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents lblFooterMain As System.Windows.Forms.Label
    Friend WithEvents lblFooterStatus As System.Windows.Forms.Label
    Friend WithEvents btnGames As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnStopAll As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents pnlFooterLine As System.Windows.Forms.Panel
    Friend WithEvents tmrRefresh As System.Windows.Forms.Timer

End Class
