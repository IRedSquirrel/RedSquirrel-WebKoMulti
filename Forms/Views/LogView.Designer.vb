<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LogView
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
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Me.tabsFilter = New RedSquirrel.WebKoMulti.KoTabStrip()
        Me.btnFolder = New RedSquirrel.WebKoMulti.KoButton()
        Me.txtSearch = New RedSquirrel.WebKoMulti.KoTextBox()
        Me.grid = New RedSquirrel.WebKoMulti.KoGrid()
        Me.colTime = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colKind = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colTopic = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.colText = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.lblEmpty = New System.Windows.Forms.Label()
        Me.tmrTail = New System.Windows.Forms.Timer(Me.components)
        CType(Me.grid, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'tabsFilter
        '
        Me.tabsFilter.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.tabsFilter.Location = New System.Drawing.Point(8, 6)
        Me.tabsFilter.Name = "tabsFilter"
        Me.tabsFilter.Size = New System.Drawing.Size(380, 32)
        Me.tabsFilter.TabIndex = 0
        Me.tabsFilter.Tabs = "Tümü|Hesap|Giriş|Takip|Uyarı"
        '
        'btnFolder
        '
        Me.btnFolder.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnFolder.Font = New System.Drawing.Font("Segoe UI", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.btnFolder.Glyph = Global.Microsoft.VisualBasic.ChrW(59448)
        Me.btnFolder.Kind = RedSquirrel.WebKoMulti.KoButtonKind.Ghost
        Me.btnFolder.Location = New System.Drawing.Point(398, 8)
        Me.btnFolder.Name = "btnFolder"
        Me.btnFolder.Size = New System.Drawing.Size(88, 26)
        Me.btnFolder.TabIndex = 1
        Me.btnFolder.Text = "Klasör"
        '
        'txtSearch
        '
        Me.txtSearch.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtSearch.Cue = "Kayıtlarda ara: hesap, kelime…"
        Me.txtSearch.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.txtSearch.Location = New System.Drawing.Point(14, 42)
        Me.txtSearch.Name = "txtSearch"
        Me.txtSearch.Size = New System.Drawing.Size(472, 28)
        Me.txtSearch.TabIndex = 2
        '
        'grid
        '
        Me.grid.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.grid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells
        Me.grid.ColumnHeadersVisible = False
        Me.grid.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.colTime, Me.colKind, Me.colTopic, Me.colText})
        Me.grid.Location = New System.Drawing.Point(14, 76)
        Me.grid.Name = "grid"
        Me.grid.Size = New System.Drawing.Size(472, 290)
        Me.grid.TabIndex = 3
        '
        'colTime
        '
        DataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.colTime.DefaultCellStyle = DataGridViewCellStyle1
        Me.colTime.HeaderText = "SAAT"
        Me.colTime.Name = "colTime"
        Me.colTime.ReadOnly = True
        Me.colTime.Width = 62
        '
        'colKind
        '
        Me.colKind.HeaderText = "TÜR"
        Me.colKind.Name = "colKind"
        Me.colKind.ReadOnly = True
        Me.colKind.Width = 66
        '
        'colTopic
        '
        DataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(154, Byte), Integer), CType(CType(161, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.colTopic.DefaultCellStyle = DataGridViewCellStyle2
        Me.colTopic.HeaderText = "KONU"
        Me.colTopic.Name = "colTopic"
        Me.colTopic.ReadOnly = True
        Me.colTopic.Width = 62
        '
        'colText
        '
        Me.colText.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        DataGridViewCellStyle3.Padding = New System.Windows.Forms.Padding(0, 5, 4, 5)
        DataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.colText.DefaultCellStyle = DataGridViewCellStyle3
        Me.colText.HeaderText = "AYRINTI"
        Me.colText.Name = "colText"
        Me.colText.ReadOnly = True
        '
        'lblEmpty
        '
        Me.lblEmpty.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblEmpty.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(12, Byte), Integer), CType(CType(28, Byte), Integer))
        Me.lblEmpty.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.lblEmpty.ForeColor = System.Drawing.Color.FromArgb(CType(CType(93, Byte), Integer), CType(CType(100, Byte), Integer), CType(CType(132, Byte), Integer))
        Me.lblEmpty.Location = New System.Drawing.Point(24, 150)
        Me.lblEmpty.Name = "lblEmpty"
        Me.lblEmpty.Size = New System.Drawing.Size(452, 50)
        Me.lblEmpty.TabIndex = 4
        Me.lblEmpty.Text = "Bugün henüz kayıt yok." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Hesap değişiklikleri, girişler ve takip burada saat sırasıyla görünür."
        Me.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'tmrTail
        '
        Me.tmrTail.Interval = 1000
        '
        'LogView
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(5, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.Controls.Add(Me.lblEmpty)
        Me.Controls.Add(Me.txtSearch)
        Me.Controls.Add(Me.grid)
        Me.Controls.Add(Me.btnFolder)
        Me.Controls.Add(Me.tabsFilter)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Name = "LogView"
        Me.Size = New System.Drawing.Size(500, 378)
        CType(Me.grid, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents tabsFilter As RedSquirrel.WebKoMulti.KoTabStrip
    Friend WithEvents btnFolder As RedSquirrel.WebKoMulti.KoButton
    Friend WithEvents txtSearch As RedSquirrel.WebKoMulti.KoTextBox
    Friend WithEvents grid As RedSquirrel.WebKoMulti.KoGrid
    Friend WithEvents colTime As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colKind As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colTopic As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colText As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents lblEmpty As System.Windows.Forms.Label
    Friend WithEvents tmrTail As System.Windows.Forms.Timer

End Class
