<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class AccountsView
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
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.grid = New RedSquirrel.WebKoMulti.KoGrid()
        Me.colLead = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colNo = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colLabel = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colId = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colSlot = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colOn = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.lblEmpty = New System.Windows.Forms.Label()
        Me.btnAdd = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnRemove = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnUp = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnDown = New RedSquirrel.WebKoMulti.KoButton()
        Me.pillDirty = New RedSquirrel.WebKoMulti.KoPill()
        Me.btnSave = New RedSquirrel.WebKoMulti.KoButton()
        Me.cardEdit = New RedSquirrel.WebKoMulti.KoCard()
        Me.lblCapLabel = New System.Windows.Forms.Label()
        Me.txtLabel = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapId = New System.Windows.Forms.Label()
        Me.txtId = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapPassword = New System.Windows.Forms.Label()
        Me.txtPassword = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.lblCapSlot = New System.Windows.Forms.Label()
        Me.cmbSlot = New RedSquirrel.WebKoMulti.KoCombo()
        Me.tglEnabled = New RedSquirrel.WebKoMulti.KoToggle()
        Me.btnLeader = New RedSquirrel.WebKoMulti.KoButton()
        Me.lblHint = New System.Windows.Forms.Label()
        Me.btnReapply = New RedSquirrel.WebKoMulti.KoButton()
        Me.tglVpn = New RedSquirrel.WebKoMulti.KoToggle()
        Me.colVpn = New System.Windows.Forms.DataGridViewTextBoxColumn()
        CType(Me.grid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.cardEdit.SuspendLayout()
        Me.SuspendLayout()
        '
        'grid
        '
        Me.grid.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grid.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.colLead, Me.colNo, Me.colLabel, Me.colId, Me.colSlot, Me.colVpn, Me.colOn})
        Me.grid.Location = New System.Drawing.Point(14, 8)
        Me.grid.Name = "grid"
        Me.grid.Size = New System.Drawing.Size(472, 140)
        Me.grid.TabIndex = 0
        '
        'colLead
        '
        Me.colLead.HeaderText = "LİDER"
        Me.colLead.Name = "colLead"
        Me.colLead.ReadOnly = True
        Me.colLead.ToolTipText = "Yıldıza tıklayın: bu hesap lider olur, diğerleri onu takip eder."
        Me.colLead.Width = 50
        '
        'colNo
        '
        DataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.colNo.DefaultCellStyle = DataGridViewCellStyle1
        Me.colNo.HeaderText = "#"
        Me.colNo.Name = "colNo"
        Me.colNo.ReadOnly = True
        Me.colNo.Width = 30
        '
        'colLabel
        '
        Me.colLabel.HeaderText = "ETİKET"
        Me.colLabel.Name = "colLabel"
        Me.colLabel.ReadOnly = True
        Me.colLabel.Width = 100
        '
        'colId
        '
        Me.colId.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        DataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.colId.DefaultCellStyle = DataGridViewCellStyle2
        Me.colId.HeaderText = "OYUN ID"
        Me.colId.Name = "colId"
        Me.colId.ReadOnly = True
        '
        'colSlot
        '
        Me.colSlot.HeaderText = "KAR."
        Me.colSlot.Name = "colSlot"
        Me.colSlot.ReadOnly = True
        Me.colSlot.Width = 52
        '
        'colVpn
        '
        Me.colVpn.HeaderText = "VPN"
        Me.colVpn.Name = "colVpn"
        Me.colVpn.ReadOnly = True
        Me.colVpn.ToolTipText = "VPN ile girer (VPN sekmesindeki bağlantı)"
        Me.colVpn.Width = 50
        '
        'colOn
        '
        Me.colOn.HeaderText = "DURUM"
        Me.colOn.Name = "colOn"
        Me.colOn.ReadOnly = True
        Me.colOn.ToolTipText = "Tıklayın: hesabı aç / kapat (kapalı hesap başlatılmaz)."
        Me.colOn.Width = 72
        '
        'lblEmpty
        '
        Me.lblEmpty.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(12, Byte), Integer), CType(CType(28, Byte), Integer))
        Me.lblEmpty.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblEmpty.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblEmpty.Location = New System.Drawing.Point(24, 52)
        Me.lblEmpty.Name = "lblEmpty"
        Me.lblEmpty.Size = New System.Drawing.Size(452, 50)
        Me.lblEmpty.TabIndex = 1
        Me.lblEmpty.Text = "Henüz hesap yok." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Ekle ile oyun hesaplarınızı girin; biri lider olur, diğerleri onu takip eder."
        Me.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btnAdd
        '
        Me.btnAdd.Glyph = Global.Microsoft.VisualBasic.ChrW(59152)
        Me.btnAdd.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Primary
        Me.btnAdd.Location = New System.Drawing.Point(14, 156)
        Me.btnAdd.Name = "btnAdd"
        Me.btnAdd.Size = New System.Drawing.Size(88, 28)
        Me.btnAdd.TabIndex = 2
        Me.btnAdd.Text = "Ekle"
        '
        'btnRemove
        '
        Me.btnRemove.Glyph = Global.Microsoft.VisualBasic.ChrW(59213)
        Me.btnRemove.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnRemove.Location = New System.Drawing.Point(108, 156)
        Me.btnRemove.Name = "btnRemove"
        Me.btnRemove.Size = New System.Drawing.Size(72, 28)
        Me.btnRemove.TabIndex = 3
        Me.btnRemove.Text = "Sil"
        '
        'btnUp
        '
        Me.btnUp.Glyph = Global.Microsoft.VisualBasic.ChrW(59150)
        Me.btnUp.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnUp.Location = New System.Drawing.Point(186, 156)
        Me.btnUp.Name = "btnUp"
        Me.btnUp.Size = New System.Drawing.Size(32, 28)
        Me.btnUp.TabIndex = 4
        '
        'btnDown
        '
        Me.btnDown.Glyph = Global.Microsoft.VisualBasic.ChrW(59149)
        Me.btnDown.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnDown.Location = New System.Drawing.Point(222, 156)
        Me.btnDown.Name = "btnDown"
        Me.btnDown.Size = New System.Drawing.Size(32, 28)
        Me.btnDown.TabIndex = 5
        '
        'pillDirty
        '
        Me.pillDirty.Location = New System.Drawing.Point(296, 159)
        Me.pillDirty.Name = "pillDirty"
        Me.pillDirty.Size = New System.Drawing.Size(100, 22)
        Me.pillDirty.TabIndex = 6
        Me.pillDirty.Text = "kaydedilmedi"
        Me.pillDirty.Tone = RedSquirrel.WebKoMulti.KoTone.Warn
        Me.pillDirty.Visible = False
        '
        'btnSave
        '
        Me.btnSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSave.Glyph = Global.Microsoft.VisualBasic.ChrW(59214)
        Me.btnSave.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Success
        Me.btnSave.Location = New System.Drawing.Point(402, 156)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(84, 28)
        Me.btnSave.TabIndex = 7
        Me.btnSave.Text = "Kaydet"
        '
        'cardEdit
        '
        Me.cardEdit.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cardEdit.Controls.Add(Me.lblCapLabel)
        Me.cardEdit.Controls.Add(Me.txtLabel)
        Me.cardEdit.Controls.Add(Me.lblCapId)
        Me.cardEdit.Controls.Add(Me.txtId)
        Me.cardEdit.Controls.Add(Me.lblCapPassword)
        Me.cardEdit.Controls.Add(Me.txtPassword)
        Me.cardEdit.Controls.Add(Me.lblCapSlot)
        Me.cardEdit.Controls.Add(Me.cmbSlot)
        Me.cardEdit.Controls.Add(Me.tglEnabled)
        Me.cardEdit.Controls.Add(Me.btnLeader)
        Me.cardEdit.Controls.Add(Me.lblHint)
        Me.cardEdit.Controls.Add(Me.btnReapply)
        Me.cardEdit.Controls.Add(Me.tglVpn)
        Me.cardEdit.Location = New System.Drawing.Point(14, 192)
        Me.cardEdit.Name = "cardEdit"
        Me.cardEdit.Size = New System.Drawing.Size(472, 180)
        Me.cardEdit.TabIndex = 8
        Me.cardEdit.Title = "SEÇİLİ HESAP"
        '
        'lblCapLabel
        '
        Me.lblCapLabel.BackColor = System.Drawing.Color.Transparent
        Me.lblCapLabel.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapLabel.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapLabel.Location = New System.Drawing.Point(14, 30)
        Me.lblCapLabel.Name = "lblCapLabel"
        Me.lblCapLabel.Size = New System.Drawing.Size(140, 15)
        Me.lblCapLabel.TabIndex = 0
        Me.lblCapLabel.Text = "Etiket (yalnız sizin için)"
        '
        'txtLabel
        '
        Me.txtLabel.Cue = "ör. Okçu"
        Me.txtLabel.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtLabel.Location = New System.Drawing.Point(14, 46)
        Me.txtLabel.MaxLength = 24
        Me.txtLabel.Name = "txtLabel"
        Me.txtLabel.Size = New System.Drawing.Size(140, 30)
        Me.txtLabel.TabIndex = 1
        '
        'lblCapId
        '
        Me.lblCapId.BackColor = System.Drawing.Color.Transparent
        Me.lblCapId.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapId.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapId.Location = New System.Drawing.Point(164, 30)
        Me.lblCapId.Name = "lblCapId"
        Me.lblCapId.Size = New System.Drawing.Size(140, 15)
        Me.lblCapId.TabIndex = 2
        Me.lblCapId.Text = "Oyun ID"
        '
        'txtId
        '
        Me.txtId.Cue = "hesap adı"
        Me.txtId.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtId.Location = New System.Drawing.Point(164, 46)
        Me.txtId.MaxLength = 40
        Me.txtId.Name = "txtId"
        Me.txtId.Size = New System.Drawing.Size(140, 30)
        Me.txtId.TabIndex = 3
        '
        'lblCapPassword
        '
        Me.lblCapPassword.BackColor = System.Drawing.Color.Transparent
        Me.lblCapPassword.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapPassword.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapPassword.Location = New System.Drawing.Point(314, 30)
        Me.lblCapPassword.Name = "lblCapPassword"
        Me.lblCapPassword.Size = New System.Drawing.Size(144, 15)
        Me.lblCapPassword.TabIndex = 4
        Me.lblCapPassword.Text = "Şifre"
        '
        'txtPassword
        '
        Me.txtPassword.Cue = "şifre"
        Me.txtPassword.Font = New System.Drawing.Font("Segoe UI", 9.5!)
        Me.txtPassword.Location = New System.Drawing.Point(314, 46)
        Me.txtPassword.MaxLength = 40
        Me.txtPassword.Name = "txtPassword"
        Me.txtPassword.Password = True
        Me.txtPassword.Size = New System.Drawing.Size(144, 30)
        Me.txtPassword.TabIndex = 5
        '
        'lblCapSlot
        '
        Me.lblCapSlot.BackColor = System.Drawing.Color.Transparent
        Me.lblCapSlot.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCapSlot.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCapSlot.Location = New System.Drawing.Point(14, 82)
        Me.lblCapSlot.Name = "lblCapSlot"
        Me.lblCapSlot.Size = New System.Drawing.Size(140, 15)
        Me.lblCapSlot.TabIndex = 6
        Me.lblCapSlot.Text = "Girilecek karakter"
        '
        'cmbSlot
        '
        Me.cmbSlot.FormattingEnabled = True
        Me.cmbSlot.Items.AddRange(New Object() {"1. karakter", "2. karakter", "3. karakter", "4. karakter"})
        Me.cmbSlot.Location = New System.Drawing.Point(14, 98)
        Me.cmbSlot.Name = "cmbSlot"
        Me.cmbSlot.Size = New System.Drawing.Size(140, 26)
        Me.cmbSlot.TabIndex = 7
        '
        'tglEnabled
        '
        Me.tglEnabled.Location = New System.Drawing.Point(164, 100)
        Me.tglEnabled.Name = "tglEnabled"
        Me.tglEnabled.Size = New System.Drawing.Size(140, 22)
        Me.tglEnabled.TabIndex = 8
        Me.tglEnabled.Text = "Hesap açık"
        '
        'btnLeader
        '
        Me.btnLeader.Glyph = Global.Microsoft.VisualBasic.ChrW(59188)
        Me.btnLeader.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnLeader.Location = New System.Drawing.Point(314, 96)
        Me.btnLeader.Name = "btnLeader"
        Me.btnLeader.Size = New System.Drawing.Size(144, 30)
        Me.btnLeader.TabIndex = 9
        Me.btnLeader.Text = "Lider yap"
        '
        'lblHint
        '
        Me.lblHint.BackColor = System.Drawing.Color.Transparent
        Me.lblHint.Font = New System.Drawing.Font("Segoe UI", 7.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblHint.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblHint.Location = New System.Drawing.Point(14, 160)
        Me.lblHint.Name = "lblHint"
        Me.lblHint.Size = New System.Drawing.Size(444, 15)
        Me.lblHint.TabIndex = 10
        Me.lblHint.Text = "Şifre bu bilgisayarda şifreli saklanır, kayda yazılmaz."
        '
        'btnReapply
        '
        Me.btnReapply.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.btnReapply.Glyph = Global.Microsoft.VisualBasic.ChrW(59541)
        Me.btnReapply.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnReapply.Location = New System.Drawing.Point(14, 130)
        Me.btnReapply.Name = "btnReapply"
        Me.btnReapply.Size = New System.Drawing.Size(220, 26)
        Me.btnReapply.TabIndex = 11
        Me.btnReapply.Text = "Oyun ayarlarını yeniden uygula"
        '
        'tglVpn
        '
        Me.tglVpn.Location = New System.Drawing.Point(250, 132)
        Me.tglVpn.Name = "tglVpn"
        Me.tglVpn.Size = New System.Drawing.Size(208, 22)
        Me.tglVpn.TabIndex = 12
        Me.tglVpn.Text = "VPN ile gir"
        '
        'AccountsView
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.Controls.Add(Me.lblEmpty)
        Me.Controls.Add(Me.grid)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.btnRemove)
        Me.Controls.Add(Me.btnUp)
        Me.Controls.Add(Me.btnDown)
        Me.Controls.Add(Me.pillDirty)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.cardEdit)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Name = "AccountsView"
        Me.Size = New System.Drawing.Size(500, 378)
        CType(Me.grid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.cardEdit.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents grid As RedSquirrel.WebKoMulti.KoGrid
    Friend WithEvents colLead As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colNo As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colLabel As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colId As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colSlot As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colOn As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents lblEmpty As System.Windows.Forms.Label
    Friend WithEvents btnAdd As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnRemove As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnUp As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnDown As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents pillDirty As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents btnSave As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents cardEdit As RedSquirrel.WebKoMulti.KoCard
    Friend WithEvents lblCapLabel As System.Windows.Forms.Label
    Friend WithEvents txtLabel As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapId As System.Windows.Forms.Label
    Friend WithEvents txtId As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapPassword As System.Windows.Forms.Label
    Friend WithEvents txtPassword As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents lblCapSlot As System.Windows.Forms.Label
    Friend WithEvents cmbSlot As RedSquirrel.WebKoMulti.KoCombo
    Friend WithEvents tglEnabled As RedSquirrel.WebKoMulti.KoToggle
    Friend WithEvents btnLeader As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents lblHint As System.Windows.Forms.Label
    Friend WithEvents btnReapply As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents tglVpn As RedSquirrel.WebKoMulti.KoToggle
    Friend WithEvents colVpn As System.Windows.Forms.DataGridViewTextBoxColumn

End Class
