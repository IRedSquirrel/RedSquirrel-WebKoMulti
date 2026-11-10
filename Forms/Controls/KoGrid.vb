Imports System.ComponentModel

''' <summary>
''' Dark read-only table: night rows with a faint alternate row, hairline row borders, muted bold headers, full-row
''' selection, no row headers. The look is set here once, so every view's designer only lists its columns. Cells that should
''' show a pill (status) are drawn by the view's CellPainting with KoPaint.DrawPill.
''' </summary>
<ToolboxItem(True), DesignerCategory("Code")>
Public Class KoGrid
    Inherits DataGridView

    Public Sub New()
        MyBase.New()
        BackgroundColor = Night
        BorderStyle = BorderStyle.None
        GridColor = KoPaint.Hairline
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        EnableHeadersVisualStyles = False
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        ColumnHeadersHeight = 28
        ColumnHeadersDefaultCellStyle.BackColor = KoPaint.CardFill
        ColumnHeadersDefaultCellStyle.ForeColor = TextFaint
        ColumnHeadersDefaultCellStyle.SelectionBackColor = KoPaint.CardFill
        ColumnHeadersDefaultCellStyle.SelectionForeColor = TextFaint
        ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 8.0!, FontStyle.Bold)
        ColumnHeadersDefaultCellStyle.Padding = New Padding(4, 0, 4, 0)
        ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False
        DefaultCellStyle.BackColor = Night
        DefaultCellStyle.ForeColor = TextMain
        DefaultCellStyle.SelectionBackColor = KoPaint.RowSelect
        DefaultCellStyle.SelectionForeColor = Color.White
        DefaultCellStyle.Font = New Font("Segoe UI", 9.0!)
        DefaultCellStyle.Padding = New Padding(4, 0, 4, 0)
        AlternatingRowsDefaultCellStyle.BackColor = KoPaint.RowAlt
        RowHeadersVisible = False
        AllowUserToAddRows = False
        AllowUserToDeleteRows = False
        AllowUserToResizeRows = False
        AllowUserToResizeColumns = False
        Me.ReadOnly = True
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
        MultiSelect = False
        RowTemplate.Height = 26
        ScrollBars = ScrollBars.Vertical
        DoubleBuffered = True
    End Sub

    ''' <summary>A sortable column reserves room for the sort arrow and its header wraps ("ALT / LİMİT") — the views order the rows.</summary>
    Protected Overrides Sub OnColumnAdded(e As DataGridViewColumnEventArgs)
        MyBase.OnColumnAdded(e)
        e.Column.SortMode = DataGridViewColumnSortMode.NotSortable
    End Sub

End Class
