<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class GamesView
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
        Me.cardInfo = New RedSquirrel.WebKoMulti.KoCard()
        Me.lblInfo = New System.Windows.Forms.Label()
        Me.btnShow = New RedSquirrel.WebKoMulti.KoButton()
        Me.grid = New RedSquirrel.WebKoMulti.KoGrid()
        Me.colNo = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colName = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colState = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colText = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.lblEmpty = New System.Windows.Forms.Label()
        Me.btnStart = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnStop = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnMakeLeader = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnLeaderFull = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnCloseGames = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnClearData = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnManual = New RedSquirrel.WebKoMulti.KoButton()
        Me.cardInfo.SuspendLayout()
        CType(Me.grid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'cardInfo
        '
        Me.cardInfo.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cardInfo.Controls.Add(Me.lblInfo)
        Me.cardInfo.Controls.Add(Me.btnShow)
        Me.cardInfo.Location = New System.Drawing.Point(14, 8)
        Me.cardInfo.Name = "cardInfo"
        Me.cardInfo.Size = New System.Drawing.Size(472, 62)
        Me.cardInfo.TabIndex = 0
        Me.cardInfo.Title = "OYUN PENCERELERİ"
        '
        'lblInfo
        '
        Me.lblInfo.BackColor = System.Drawing.Color.Transparent
        Me.lblInfo.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblInfo.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblInfo.Location = New System.Drawing.Point(12, 26)
        Me.lblInfo.Name = "lblInfo"
        Me.lblInfo.Size = New System.Drawing.Size(316, 32)
        Me.lblInfo.TabIndex = 0
        Me.lblInfo.Text = "Lider kendi penceresinde, takipçiler oyun penceresinde ızgarada. Her görünüm 1280×800, kaydırılabilir."
        '
        'btnShow
        '
        Me.btnShow.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnShow.Glyph = Global.Microsoft.VisualBasic.ChrW(59559)
        Me.btnShow.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Primary
        Me.btnShow.Location = New System.Drawing.Point(334, 18)
        Me.btnShow.Name = "btnShow"
        Me.btnShow.Size = New System.Drawing.Size(126, 30)
        Me.btnShow.TabIndex = 1
        Me.btnShow.Text = "Pencereleri aç"
        '
        'grid
        '
        Me.grid.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grid.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.colNo, Me.colName, Me.colState, Me.colText})
        Me.grid.Location = New System.Drawing.Point(14, 78)
        Me.grid.Name = "grid"
        Me.grid.Size = New System.Drawing.Size(472, 210)
        Me.grid.TabIndex = 1
        Me.grid.MultiSelect = True
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
        'colName
        '
        Me.colName.HeaderText = "HESAP"
        Me.colName.Name = "colName"
        Me.colName.ReadOnly = True
        Me.colName.Width = 120
        '
        'colState
        '
        Me.colState.HeaderText = "DURUM"
        Me.colState.Name = "colState"
        Me.colState.ReadOnly = True
        Me.colState.Width = 104
        '
        'colText
        '
        Me.colText.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        DataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.colText.DefaultCellStyle = DataGridViewCellStyle2
        Me.colText.HeaderText = "AYRINTI"
        Me.colText.Name = "colText"
        Me.colText.ReadOnly = True
        '
        'lblEmpty
        '
        Me.lblEmpty.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(12, Byte), Integer), CType(CType(28, Byte), Integer))
        Me.lblEmpty.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblEmpty.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblEmpty.Location = New System.Drawing.Point(24, 150)
        Me.lblEmpty.Name = "lblEmpty"
        Me.lblEmpty.Size = New System.Drawing.Size(452, 50)
        Me.lblEmpty.TabIndex = 2
        Me.lblEmpty.Text = "Hesap yok." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Önce Hesaplar sekmesinden oyun hesaplarını ekleyin."
        Me.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btnStart
        '
        Me.btnStart.Glyph = Global.Microsoft.VisualBasic.ChrW(59240)
        Me.btnStart.Location = New System.Drawing.Point(14, 296)
        Me.btnStart.Name = "btnStart"
        Me.btnStart.Size = New System.Drawing.Size(128, 28)
        Me.btnStart.TabIndex = 3
        Me.btnStart.Text = "Başlat"
        '
        'btnStop
        '
        Me.btnStop.Glyph = Global.Microsoft.VisualBasic.ChrW(59162)
        Me.btnStop.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnStop.Location = New System.Drawing.Point(148, 296)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New System.Drawing.Size(96, 28)
        Me.btnStop.TabIndex = 4
        Me.btnStop.Text = "Durdur"
        '
        'btnMakeLeader
        '
        Me.btnMakeLeader.Glyph = Global.Microsoft.VisualBasic.ChrW(59188)
        Me.btnMakeLeader.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnMakeLeader.Location = New System.Drawing.Point(250, 296)
        Me.btnMakeLeader.Name = "btnMakeLeader"
        Me.btnMakeLeader.Size = New System.Drawing.Size(110, 28)
        Me.btnMakeLeader.TabIndex = 5
        Me.btnMakeLeader.Text = "Lider yap"
        '
        'btnManual
        '
        Me.btnManual.Glyph = Global.Microsoft.VisualBasic.ChrW(59259)
        Me.btnManual.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnManual.Location = New System.Drawing.Point(366, 296)
        Me.btnManual.Name = "btnManual"
        Me.btnManual.Size = New System.Drawing.Size(120, 28)
        Me.btnManual.TabIndex = 9
        Me.btnManual.Text = "Elle giriş"
        '
        'btnLeaderFull
        '
        Me.btnLeaderFull.Glyph = Global.Microsoft.VisualBasic.ChrW(59200)
        Me.btnLeaderFull.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnLeaderFull.Location = New System.Drawing.Point(14, 334)
        Me.btnLeaderFull.Name = "btnLeaderFull"
        Me.btnLeaderFull.Size = New System.Drawing.Size(170, 28)
        Me.btnLeaderFull.TabIndex = 6
        Me.btnLeaderFull.Text = "Lider: tam ekran"
        '
        'btnClearData
        '
        Me.btnClearData.Glyph = Global.Microsoft.VisualBasic.ChrW(59213)
        Me.btnClearData.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnClearData.Location = New System.Drawing.Point(190, 334)
        Me.btnClearData.Name = "btnClearData"
        Me.btnClearData.Size = New System.Drawing.Size(150, 28)
        Me.btnClearData.TabIndex = 8
        Me.btnClearData.Text = "Tarayıcıyı sıfırla"
        '
        'btnCloseGames
        '
        Me.btnCloseGames.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCloseGames.Glyph = Global.Microsoft.VisualBasic.ChrW(59579)
        Me.btnCloseGames.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Danger
        Me.btnCloseGames.Location = New System.Drawing.Point(346, 334)
        Me.btnCloseGames.Name = "btnCloseGames"
        Me.btnCloseGames.Size = New System.Drawing.Size(140, 28)
        Me.btnCloseGames.TabIndex = 7
        Me.btnCloseGames.Text = "Oyunları kapat"
        '
        'GamesView
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.Controls.Add(Me.lblEmpty)
        Me.Controls.Add(Me.cardInfo)
        Me.Controls.Add(Me.grid)
        Me.Controls.Add(Me.btnStart)
        Me.Controls.Add(Me.btnStop)
        Me.Controls.Add(Me.btnMakeLeader)
        Me.Controls.Add(Me.btnLeaderFull)
        Me.Controls.Add(Me.btnCloseGames)
        Me.Controls.Add(Me.btnClearData)
        Me.Controls.Add(Me.btnManual)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Name = "GamesView"
        Me.Size = New System.Drawing.Size(500, 378)
        Me.cardInfo.ResumeLayout(False)
        CType(Me.grid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents cardInfo As RedSquirrel.WebKoMulti.KoCard
    Friend WithEvents lblInfo As System.Windows.Forms.Label
    Friend WithEvents btnShow As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents grid As RedSquirrel.WebKoMulti.KoGrid
    Friend WithEvents colNo As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colName As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colState As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colText As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents lblEmpty As System.Windows.Forms.Label
    Friend WithEvents btnStart As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnStop As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnMakeLeader As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnLeaderFull As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnCloseGames As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnClearData As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnManual As RedSquirrel.WebKoMulti.KoButton

End Class
