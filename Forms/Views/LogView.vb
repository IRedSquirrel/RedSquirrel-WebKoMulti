Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' "Kayıt": today's log (Services.Logging.BotLog, Logs\multi-yyyy-MM-dd.log), newest first, with a filter strip and a search box.
''' BotLog has no event yet, so the view follows the file (1 s, only the new bytes) — every session's lines show up here, the
''' login and follow engines' too. Designer: LogView.Designer.vb.
''' </summary>
Public Class LogView

    Private Shared ReadOnly PillFont As New Font("Segoe UI", 8.0!, FontStyle.Bold)
    Private Shared ReadOnly LineRx As New Regex("^(\d\d:\d\d:\d\d)(?:\.\d+)? \[(\w+)\] \[([^\]]*)\] (.*)$", RegexOptions.Compiled)
    Private Const MaxEntries As Integer = 600

    Private ReadOnly _entries As New List(Of (Time As String, Level As String, Topic As String, Text As String))()
    Private _file As String = ""
    Private _pos As Long
    Private _partial As String = ""

    ''' <summary>Warnings + errors among the entries changed (Main shows it on the tab).</summary>
    Public Event ProblemsChanged As EventHandler

    Public Sub New()
        InitializeComponent()
        AddHandler grid.CellPainting, AddressOf PaintKind
    End Sub

    Public ReadOnly Property ProblemCount As Integer
        Get
            Return _entries.Where(Function(e) e.Level <> "INFO").Count()
        End Get
    End Property

    ''' <summary>Starts following the log file (not in render / demo).</summary>
    Public Sub StartTail()
        Poll()
        tmrTail.Start()
    End Sub

    Friend Sub AddDemo(time As String, level As String, topic As String, text As String)
        _entries.Insert(0, (time, level, topic, text))
        Rebuild()
    End Sub

    Private Sub tmrTail_Tick(sender As Object, e As EventArgs) Handles tmrTail.Tick
        Poll()
    End Sub

    Private Sub Poll()
        Try
            Dim path As String = Services.Logging.BotLog.DosyaYolu()
            If path <> _file Then
                _file = path
                _pos = 0
                _partial = ""
                _entries.Clear()
            End If
            If Not File.Exists(path) Then
                Rebuild()
                Return
            End If
            Dim added As Boolean = False
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
                If fs.Length < _pos Then _pos = 0 : _entries.Clear()
                If fs.Length = _pos Then Return
                fs.Seek(_pos, SeekOrigin.Begin)
                Dim buf(CInt(Math.Min(fs.Length - _pos, 4 * 1024 * 1024)) - 1) As Byte
                Dim n As Integer = fs.Read(buf, 0, buf.Length)
                _pos += n
                Dim text As String = _partial & Encoding.UTF8.GetString(buf, 0, n)
                Dim lines As String() = text.Split({ChrW(10)})
                _partial = lines(lines.Length - 1)
                For i As Integer = 0 To lines.Length - 2
                    Dim m As Match = LineRx.Match(lines(i).TrimEnd(ChrW(13)))
                    If Not m.Success Then Continue For
                    _entries.Insert(0, (m.Groups(1).Value, m.Groups(2).Value, m.Groups(3).Value, m.Groups(4).Value))
                    added = True
                Next
                While _entries.Count > MaxEntries
                    _entries.RemoveAt(_entries.Count - 1)
                End While
            End Using
            If added Then Rebuild()
        Catch
            ' The log is being written; the next tick reads the rest.
        End Try
    End Sub

    Private Shared Function InFilter(topic As String, level As String, filter As String) As Boolean
        Dim t As String = topic.ToLowerInvariant()
        Select Case filter
            Case "Hesap" : Return t = "hesap"
            Case "Giriş" : Return t = "oturum" OrElse t.StartsWith("giriş") OrElse t.StartsWith("giris") OrElse t.StartsWith("login") OrElse t = "web"
            Case "Takip" : Return t = "takip" OrElse t.StartsWith("follow")
            Case "Uyarı" : Return level <> "INFO"
            Case Else : Return True
        End Select
    End Function

    Private Shared Function Matches(line As String, search As String) As Boolean
        Dim tr As Globalization.CompareInfo = Globalization.CultureInfo.GetCultureInfo("tr-TR").CompareInfo
        Return If(search, "").Split({" "c}, StringSplitOptions.RemoveEmptyEntries).All(Function(w) tr.IndexOf(line, w, Globalization.CompareOptions.IgnoreCase) >= 0)
    End Function

    Private Sub Rebuild()
        Dim filter As String = tabsFilter.SelectedTab
        Dim search As String = txtSearch.Text.Trim()
        grid.SuspendLayout()
        grid.Rows.Clear()
        For Each en In _entries
            If Not InFilter(en.Topic, en.Level, filter) Then Continue For
            If Not Matches(en.Topic & " " & en.Text, search) Then Continue For
            grid.Rows.Add(en.Time, en.Level, en.Topic, en.Text)
            If grid.Rows.Count >= 300 Then Exit For
        Next
        grid.ResumeLayout()
        grid.ClearSelection()
        lblEmpty.Visible = grid.Rows.Count = 0
        lblEmpty.Text = If(_entries.Count = 0, "Bugün henüz kayıt yok." & Environment.NewLine & "Hesap değişiklikleri, girişler ve takip burada saat sırasıyla görünür.",
                           If(search <> "", $"'{search}' ile eşleşen kayıt yok.", $"'{filter}' türünde kayıt yok."))
        RaiseEvent ProblemsChanged(Me, EventArgs.Empty)
    End Sub

    Private Shared Function KindText(level As String) As String
        Select Case level
            Case "WARN" : Return "Uyarı"
            Case "FAIL" : Return "Hata"
            Case Else : Return "Bilgi"
        End Select
    End Function

    Private Sub PaintKind(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> colKind.Index Then Return
        e.PaintBackground(e.CellBounds, (e.State And DataGridViewElementStates.Selected) <> 0)
        Dim level As String = If(e.Value?.ToString(), "")
        Dim text As String = KindText(level)
        Dim tone As KoTone = If(level = "FAIL", KoTone.Bad, If(level = "WARN", KoTone.Warn, KoTone.Info))
        Dim w As Integer = Math.Min(e.CellBounds.Width - 6, KoPaint.PillWidth(text, PillFont, 18))
        KoPaint.DrawPill(e.Graphics, New Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + (e.CellBounds.Height - 18) \ 2, w, 18), text, tone, PillFont)
        e.Handled = True
    End Sub

    Private Sub tabsFilter_SelectedIndexChanged(sender As Object, e As EventArgs) Handles tabsFilter.SelectedIndexChanged
        Rebuild()
    End Sub

    Private Sub txtSearch_TextChanged(sender As Object, e As EventArgs) Handles txtSearch.TextChanged
        Rebuild()
    End Sub

    Private Sub btnFolder_Click(sender As Object, e As EventArgs) Handles btnFolder.Click
        Try
            Dim dir As String = Path.GetDirectoryName(Services.Logging.BotLog.DosyaYolu())
            Directory.CreateDirectory(dir)
            Process.Start("explorer.exe", """" & dir & """")
        Catch ex As Exception
            Services.Logging.BotLog.Warn("arayüz", "Kayıt klasörü açılamadı: " & ex.Message)
        End Try
    End Sub

    Protected Overrides Sub OnHandleDestroyed(e As EventArgs)
        tmrTail.Stop()
        MyBase.OnHandleDestroyed(e)
    End Sub

End Class
