<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LeaderWindow
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
        Me.cell = New RedSquirrel.WebKoMulti.GameCell()
        Me.SuspendLayout()
        '
        'cell
        '
        Me.cell.BackColor = System.Drawing.Color.Black
        Me.cell.Dock = System.Windows.Forms.DockStyle.Fill
        Me.cell.Location = New System.Drawing.Point(0, 0)
        Me.cell.Margin = New System.Windows.Forms.Padding(0)
        Me.cell.Name = "cell"
        Me.cell.Size = New System.Drawing.Size(1280, 832)
        Me.cell.TabIndex = 0
        '
        'LeaderWindow
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(1280, 832)
        Me.Controls.Add(Me.cell)
        Me.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.MinimumSize = New System.Drawing.Size(480, 320)
        Me.Name = "LeaderWindow"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Lider"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents cell As RedSquirrel.WebKoMulti.GameCell

End Class
