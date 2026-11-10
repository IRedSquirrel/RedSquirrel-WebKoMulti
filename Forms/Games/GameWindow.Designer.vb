<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class GameWindow
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
        Me.pnlTop = New System.Windows.Forms.Panel()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.lblCount = New System.Windows.Forms.Label()
        Me.btnLeader = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnStartAll = New RedSquirrel.WebKoMulti.KoButton()
        Me.btnStopAll = New RedSquirrel.WebKoMulti.KoButton()
        Me.pnlTopLine = New System.Windows.Forms.Panel()
        Me.grid = New System.Windows.Forms.TableLayoutPanel()
        Me.lblEmpty = New System.Windows.Forms.Label()
        Me.pnlTop.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlTop
        '
        Me.pnlTop.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(12, Byte), Integer), CType(CType(28, Byte), Integer))
        Me.pnlTop.Controls.Add(Me.lblTitle)
        Me.pnlTop.Controls.Add(Me.lblCount)
        Me.pnlTop.Controls.Add(Me.btnLeader)
        Me.pnlTop.Controls.Add(Me.btnStartAll)
        Me.pnlTop.Controls.Add(Me.btnStopAll)
        Me.pnlTop.Controls.Add(Me.pnlTopLine)
        Me.pnlTop.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlTop.Location = New System.Drawing.Point(0, 0)
        Me.pnlTop.Name = "pnlTop"
        Me.pnlTop.Size = New System.Drawing.Size(1284, 42)
        Me.pnlTop.TabIndex = 0
        '
        'lblTitle
        '
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(129, Byte), Integer), CType(CType(140, Byte), Integer), CType(CType(248, Byte), Integer))
        Me.lblTitle.Location = New System.Drawing.Point(12, 11)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(84, 19)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "Takipçiler"
        '
        'lblCount
        '
        Me.lblCount.AutoSize = True
        Me.lblCount.Font = New System.Drawing.Font("Segoe UI", 8.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblCount.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.lblCount.Location = New System.Drawing.Point(104, 13)
        Me.lblCount.Name = "lblCount"
        Me.lblCount.Size = New System.Drawing.Size(200, 15)
        Me.lblCount.TabIndex = 1
        Me.lblCount.Text = "0 takipçi · lider ayrı pencerede"
        '
        'btnLeader
        '
        Me.btnLeader.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnLeader.Glyph = Global.Microsoft.VisualBasic.ChrW(59188)
        Me.btnLeader.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnLeader.Location = New System.Drawing.Point(894, 7)
        Me.btnLeader.Name = "btnLeader"
        Me.btnLeader.Size = New System.Drawing.Size(140, 28)
        Me.btnLeader.TabIndex = 2
        Me.btnLeader.Text = "Lider penceresi"
        '
        'btnStartAll
        '
        Me.btnStartAll.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnStartAll.Glyph = Global.Microsoft.VisualBasic.ChrW(59240)
        Me.btnStartAll.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Primary
        Me.btnStartAll.Location = New System.Drawing.Point(1040, 7)
        Me.btnStartAll.Name = "btnStartAll"
        Me.btnStartAll.Size = New System.Drawing.Size(132, 28)
        Me.btnStartAll.TabIndex = 3
        Me.btnStartAll.Text = "Hepsini başlat"
        '
        'btnStopAll
        '
        Me.btnStopAll.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnStopAll.Glyph = Global.Microsoft.VisualBasic.ChrW(59162)
        Me.btnStopAll.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Danger
        Me.btnStopAll.Location = New System.Drawing.Point(1178, 7)
        Me.btnStopAll.Name = "btnStopAll"
        Me.btnStopAll.Size = New System.Drawing.Size(94, 28)
        Me.btnStopAll.TabIndex = 4
        Me.btnStopAll.Text = "Durdur"
        '
        'pnlTopLine
        '
        Me.pnlTopLine.BackColor = System.Drawing.Color.FromArgb(CType(CType(129, Byte), Integer), CType(CType(140, Byte), Integer), CType(CType(248, Byte), Integer))
        Me.pnlTopLine.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.pnlTopLine.Location = New System.Drawing.Point(0, 40)
        Me.pnlTopLine.Name = "pnlTopLine"
        Me.pnlTopLine.Size = New System.Drawing.Size(1284, 2)
        Me.pnlTopLine.TabIndex = 5
        '
        'grid
        '
        Me.grid.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(32, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.grid.ColumnCount = 1
        Me.grid.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.grid.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grid.Location = New System.Drawing.Point(0, 42)
        Me.grid.Margin = New System.Windows.Forms.Padding(0)
        Me.grid.Name = "grid"
        Me.grid.Padding = New System.Windows.Forms.Padding(1)
        Me.grid.RowCount = 1
        Me.grid.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.grid.Size = New System.Drawing.Size(1284, 818)
        Me.grid.TabIndex = 1
        '
        'lblEmpty
        '
        Me.lblEmpty.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.lblEmpty.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lblEmpty.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblEmpty.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblEmpty.Location = New System.Drawing.Point(0, 42)
        Me.lblEmpty.Name = "lblEmpty"
        Me.lblEmpty.Size = New System.Drawing.Size(1284, 818)
        Me.lblEmpty.TabIndex = 2
        Me.lblEmpty.Text = "Takipçi yok." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Lider dışındaki açık hesaplar burada ızgarada görünür."
        Me.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.lblEmpty.Visible = False
        '
        'GameWindow
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(1284, 860)
        Me.Controls.Add(Me.lblEmpty)
        Me.Controls.Add(Me.grid)
        Me.Controls.Add(Me.pnlTop)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.ForeColor = System.Drawing.Color.FromArgb(CType(CType(231, Byte), Integer), CType(CType(233, Byte), Integer), CType(CType(244, Byte), Integer))
        Me.MinimumSize = New System.Drawing.Size(720, 420)
        Me.Name = "GameWindow"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "RedSquirrel - WebKo Multi · Oyunlar"
        Me.pnlTop.ResumeLayout(False)
        Me.pnlTop.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents pnlTop As System.Windows.Forms.Panel
    Friend WithEvents lblTitle As System.Windows.Forms.Label
    Friend WithEvents lblCount As System.Windows.Forms.Label
    Friend WithEvents btnLeader As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnStartAll As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents btnStopAll As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents pnlTopLine As System.Windows.Forms.Panel
    Friend WithEvents grid As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents lblEmpty As System.Windows.Forms.Label

End Class
