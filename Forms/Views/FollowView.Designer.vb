<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FollowView
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
        Dim DataGridViewCellStyle3 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.cardLeader = New RedSquirrel.WebKoMulti.KoCard()
        Me.pillFollow = New RedSquirrel.WebKoMulti.KoPill()
        Me.lblCap = New System.Windows.Forms.Label()
        Me.cmbLeader = New RedSquirrel.WebKoMulti.KoCombo()
        Me.btnFollow = New RedSquirrel.WebKoMulti.KoButton()
        Me.lblOverall = New System.Windows.Forms.Label()
        Me.cardMirror = New RedSquirrel.WebKoMulti.KoCard()
        Me.tglMirror = New RedSquirrel.WebKoMulti.KoToggle()
        Me.cmbMirrorMode = New RedSquirrel.WebKoMulti.KoCombo()
        Me.lblMirror = New System.Windows.Forms.Label()
        Me.grid = New RedSquirrel.WebKoMulti.KoGrid()
        Me.colNo = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colName = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colState = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colFollow = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colTime = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.lblEmpty = New System.Windows.Forms.Label()
        Me.cardLeader.SuspendLayout()
        Me.cardMirror.SuspendLayout()
        CType(Me.grid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'cardLeader
        '
        Me.cardLeader.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cardLeader.Controls.Add(Me.pillFollow)
        Me.cardLeader.Controls.Add(Me.lblCap)
        Me.cardLeader.Controls.Add(Me.cmbLeader)
        Me.cardLeader.Controls.Add(Me.btnFollow)
        Me.cardLeader.Controls.Add(Me.lblOverall)
        Me.cardLeader.Location = New System.Drawing.Point(14, 8)
        Me.cardLeader.Name = "cardLeader"
        Me.cardLeader.Size = New System.Drawing.Size(472, 106)
        Me.cardLeader.TabIndex = 0
        Me.cardLeader.Title = "LİDER"
        '
        'pillFollow
        '
        Me.pillFollow.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.pillFollow.Location = New System.Drawing.Point(368, 8)
        Me.pillFollow.Name = "pillFollow"
        Me.pillFollow.Size = New System.Drawing.Size(92, 22)
        Me.pillFollow.TabIndex = 0
        Me.pillFollow.Text = "Takip kapalı"
        '
        'lblCap
        '
        Me.lblCap.BackColor = System.Drawing.Color.Transparent
        Me.lblCap.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCap.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCap.Location = New System.Drawing.Point(12, 30)
        Me.lblCap.Name = "lblCap"
        Me.lblCap.Size = New System.Drawing.Size(280, 15)
        Me.lblCap.TabIndex = 1
        Me.lblCap.Text = "Takipçiler bu hesabın koordinatına yürür"
        '
        'cmbLeader
        '
        Me.cmbLeader.FormattingEnabled = True
        Me.cmbLeader.Location = New System.Drawing.Point(12, 48)
        Me.cmbLeader.Name = "cmbLeader"
        Me.cmbLeader.Size = New System.Drawing.Size(270, 26)
        Me.cmbLeader.TabIndex = 2
        '
        'btnFollow
        '
        Me.btnFollow.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnFollow.Glyph = Global.Microsoft.VisualBasic.ChrW(59240)
        Me.btnFollow.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Primary
        Me.btnFollow.Location = New System.Drawing.Point(300, 44)
        Me.btnFollow.Name = "btnFollow"
        Me.btnFollow.Size = New System.Drawing.Size(160, 32)
        Me.btnFollow.TabIndex = 3
        Me.btnFollow.Text = "Takibi başlat"
        '
        'lblOverall
        '
        Me.lblOverall.AutoEllipsis = True
        Me.lblOverall.BackColor = System.Drawing.Color.Transparent
        Me.lblOverall.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblOverall.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblOverall.Location = New System.Drawing.Point(12, 82)
        Me.lblOverall.Name = "lblOverall"
        Me.lblOverall.Size = New System.Drawing.Size(448, 16)
        Me.lblOverall.TabIndex = 4
        Me.lblOverall.Text = "Yalnız oyunda olan takipçiler yürür."
        '
        'cardMirror
        '
        Me.cardMirror.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cardMirror.Controls.Add(Me.tglMirror)
        Me.cardMirror.Controls.Add(Me.cmbMirrorMode)
        Me.cardMirror.Controls.Add(Me.lblMirror)
        Me.cardMirror.Location = New System.Drawing.Point(14, 120)
        Me.cardMirror.Name = "cardMirror"
        Me.cardMirror.Size = New System.Drawing.Size(472, 56)
        Me.cardMirror.TabIndex = 3
        '
        'tglMirror
        '
        Me.tglMirror.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.tglMirror.Location = New System.Drawing.Point(12, 8)
        Me.tglMirror.Name = "tglMirror"
        Me.tglMirror.Size = New System.Drawing.Size(300, 22)
        Me.tglMirror.TabIndex = 0
        Me.tglMirror.Text = "Girdi yansıtma — liderden takipçilere"
        '
        'cmbMirrorMode
        '
        Me.cmbMirrorMode.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cmbMirrorMode.FormattingEnabled = True
        Me.cmbMirrorMode.Items.AddRange(New Object() {"Yalnız klavye", "Klavye + fare"})
        Me.cmbMirrorMode.Location = New System.Drawing.Point(330, 6)
        Me.cmbMirrorMode.Name = "cmbMirrorMode"
        Me.cmbMirrorMode.Size = New System.Drawing.Size(130, 26)
        Me.cmbMirrorMode.TabIndex = 2
        '
        'lblMirror
        '
        Me.lblMirror.AutoEllipsis = True
        Me.lblMirror.BackColor = System.Drawing.Color.Transparent
        Me.lblMirror.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblMirror.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblMirror.Location = New System.Drawing.Point(54, 35)
        Me.lblMirror.Name = "lblMirror"
        Me.lblMirror.Size = New System.Drawing.Size(406, 16)
        Me.lblMirror.TabIndex = 1
        Me.lblMirror.Text = "Kapalı"
        '
        'grid
        '
        Me.grid.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grid.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.colNo, Me.colName, Me.colState, Me.colFollow, Me.colTime})
        Me.grid.Location = New System.Drawing.Point(14, 180)
        Me.grid.Name = "grid"
        Me.grid.Size = New System.Drawing.Size(472, 186)
        Me.grid.TabIndex = 1
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
        Me.colName.HeaderText = "TAKİPÇİ"
        Me.colName.Name = "colName"
        Me.colName.ReadOnly = True
        Me.colName.Width = 104
        '
        'colState
        '
        Me.colState.HeaderText = "OYUN"
        Me.colState.Name = "colState"
        Me.colState.ReadOnly = True
        Me.colState.Width = 96
        '
        'colFollow
        '
        Me.colFollow.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        DataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(200, Byte), Integer), CType(CType(204, Byte), Integer), CType(CType(228, Byte), Integer))
        Me.colFollow.DefaultCellStyle = DataGridViewCellStyle2
        Me.colFollow.HeaderText = "TAKİP"
        Me.colFollow.Name = "colFollow"
        Me.colFollow.ReadOnly = True
        '
        'colTime
        '
        DataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.colTime.DefaultCellStyle = DataGridViewCellStyle3
        Me.colTime.HeaderText = "SAAT"
        Me.colTime.Name = "colTime"
        Me.colTime.ReadOnly = True
        Me.colTime.Width = 62
        '
        'lblEmpty
        '
        Me.lblEmpty.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(12, Byte), Integer), CType(CType(28, Byte), Integer))
        Me.lblEmpty.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblEmpty.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblEmpty.Location = New System.Drawing.Point(24, 246)
        Me.lblEmpty.Name = "lblEmpty"
        Me.lblEmpty.Size = New System.Drawing.Size(452, 50)
        Me.lblEmpty.TabIndex = 2
        Me.lblEmpty.Text = "Takipçi yok." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Liderden başka en az bir açık hesap gerekir."
        Me.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'FollowView
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.Controls.Add(Me.lblEmpty)
        Me.Controls.Add(Me.cardLeader)
        Me.Controls.Add(Me.cardMirror)
        Me.Controls.Add(Me.grid)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Name = "FollowView"
        Me.Size = New System.Drawing.Size(500, 378)
        Me.cardLeader.ResumeLayout(False)
        Me.cardMirror.ResumeLayout(False)
        CType(Me.grid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents cardLeader As RedSquirrel.WebKoMulti.KoCard
    Friend WithEvents pillFollow As RedSquirrel.WebKoMulti.KoPill
    Friend WithEvents lblCap As System.Windows.Forms.Label
    Friend WithEvents cmbLeader As RedSquirrel.WebKoMulti.KoCombo
    Friend WithEvents btnFollow As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents lblOverall As System.Windows.Forms.Label
    Friend WithEvents cardMirror As RedSquirrel.WebKoMulti.KoCard
    Friend WithEvents tglMirror As RedSquirrel.WebKoMulti.KoToggle
    Friend WithEvents cmbMirrorMode As RedSquirrel.WebKoMulti.KoCombo
    Friend WithEvents lblMirror As System.Windows.Forms.Label
    Friend WithEvents grid As RedSquirrel.WebKoMulti.KoGrid
    Friend WithEvents colNo As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colName As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colState As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colFollow As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colTime As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents lblEmpty As System.Windows.Forms.Label

End Class
