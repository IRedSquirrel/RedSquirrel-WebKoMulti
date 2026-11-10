Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>
''' "Takip": the leader picker (the same list and the same leader as Hesaplar — the hub's), Takibi başlat / durdur
''' (Engine.Follow.FollowController via the hub), and every follower's last follow message with its time. Changing the leader
''' while following is allowed: the controller's LeaderIndex changes live. Designer: FollowView.Designer.vb.
''' </summary>
Public Class FollowView

    Private Shared ReadOnly PillFont As New Font("Segoe UI", 8.0!, FontStyle.Bold)
    Private _hub As SessionHub
    Private _loading As Boolean
    Private ReadOnly _last As New Dictionary(Of Integer, (Text As String, Time As DateTime))()

    Public Sub New()
        InitializeComponent()
        AddHandler grid.CellPainting, AddressOf PaintState
    End Sub

    Public Sub Bind(hub As SessionHub)
        _hub = hub
        AddHandler hub.AccountsChanged, Sub() Rebuild()
        AddHandler hub.SessionsRebuilt, Sub()
                                            _last.Clear()
                                            Rebuild()
                                        End Sub
        AddHandler hub.LeaderChanged, Sub() Rebuild()
        AddHandler hub.SessionChanged, Sub(s) grid.Invalidate()
        AddHandler hub.FollowRunningChanged, Sub() UpdateRunning()
        AddHandler hub.FollowStatus, AddressOf OnStatus
        AddHandler hub.MirrorChanged, Sub() RefreshMirror()
        AddHandler hub.MirrorAction, Sub() RefreshMirror()
        Rebuild()
    End Sub

    ''' <summary>Preview text for the render command (a follower's last message).</summary>
    Friend Sub DemoStatus(index As Integer, text As String, at As DateTime)
        _last(index) = (text, at)
        Rebuild()
    End Sub

    Private Function Followers() As List(Of GameSession)
        Return _hub.Sessions.Where(Function(s) s.Index <> _hub.Leader AndAlso s.Account.Enabled).ToList()
    End Function

    Public Sub Rebuild()
        If _hub Is Nothing Then Return
        _loading = True
        cmbLeader.Items.Clear()
        For i As Integer = 0 To _hub.Accounts.Count - 1
            cmbLeader.Items.Add(_hub.Describe(i) & If(_hub.Accounts(i).Enabled, "", " — kapalı"))
        Next
        cmbLeader.SelectedIndex = If(_hub.Leader < cmbLeader.Items.Count, _hub.Leader, -1)
        _loading = False

        grid.Rows.Clear()
        For Each s As GameSession In Followers()
            Dim last As (Text As String, Time As DateTime) = Nothing
            Dim has As Boolean = _last.TryGetValue(s.Index, last)
            Dim r As Integer = grid.Rows.Add(CStr(s.Number), If(s.Account.Label.Trim() = "", $"{s.Number}. hesap", s.Account.Label), "",
                                             If(has, last.Text, "—"), If(has, last.Time.ToString("HH:mm:ss"), ""))
            grid.Rows(r).Tag = s
        Next
        lblEmpty.Visible = grid.Rows.Count = 0
        UpdateRunning()
        RefreshMirror()
    End Sub

    Private Sub UpdateRunning()
        Dim running As Boolean = _hub.FollowRunning
        pillFollow.Text = If(running, "Takip açık", "Takip kapalı")
        pillFollow.Tone = If(running, KoTone.Ok, KoTone.Neutral)
        btnFollow.Text = If(running, "Takibi durdur", "Takibi başlat")
        btnFollow.Kind = If(running, KoButtonKind.Danger, KoButtonKind.Primary)
        btnFollow.Glyph = If(running, ChrW(&HE71A), ChrW(&HE768))
        btnFollow.Enabled = running OrElse (_hub.Leader >= 0 AndAlso Followers().Count > 0)
    End Sub

    ''' <summary>The same switch as the leader window's (hub = one source). Follow is never switched off here — the engine decides.</summary>
    Private Sub RefreshMirror()
        Dim isOn As Boolean = _hub.MirrorOn
        _loading = True
        tglMirror.Checked = isOn
        cmbMirrorMode.SelectedIndex = If(_hub.MirrorMouse, 1, 0)
        _loading = False
        cardMirror.Accent = If(isOn, WarnOrange, Color.Empty)
        cardMirror.BorderColor = If(isOn, WarnOrange, KoPaint.CardBorder)
        Dim parts As New List(Of String)
        If isOn Then parts.Add(If(_hub.MirrorMouse, $"AÇIK — tık ve tuşlar {_hub.MirrorTargets} hesapta tekrarlanıyor", $"AÇIK — tuşlar {_hub.MirrorTargets} hesapta tekrarlanıyor"))
        If _hub.MirrorStatus <> "" Then parts.Add(_hub.MirrorStatus)
        If isOn AndAlso _hub.MirrorLast <> "" Then parts.Add("son: " & _hub.MirrorLast)
        lblMirror.Text = If(parts.Count = 0, If(_hub.MirrorMouse, "Kapalı · açılınca tık, sürükleme ve tuşlar gider", "Kapalı · açılınca yalnız tuşlar gider"), String.Join("  ·  ", parts))
        lblMirror.ForeColor = If(isOn, WarnOrange, TextFaint)
    End Sub

    Private Sub cmbMirrorMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMirrorMode.SelectedIndexChanged
        If _loading OrElse _hub Is Nothing OrElse cmbMirrorMode.SelectedIndex < 0 Then Return
        _hub.SetMirrorMouse(cmbMirrorMode.SelectedIndex = 1)
    End Sub

    Private Sub tglMirror_CheckedChanged(sender As Object, e As EventArgs) Handles tglMirror.CheckedChanged
        If _loading OrElse _hub Is Nothing Then Return
        _hub.SetMirror(tglMirror.Checked)
    End Sub

    Private Sub OnStatus(index As Integer, text As String)
        If index < 0 Then
            lblOverall.Text = text
            lblOverall.ForeColor = TextMuted
            Return
        End If
        _last(index) = (text, DateTime.Now)
        For Each r As DataGridViewRow In grid.Rows
            Dim s As GameSession = TryCast(r.Tag, GameSession)
            If s IsNot Nothing AndAlso s.Index = index Then
                r.Cells(colFollow.Index).Value = text
                r.Cells(colTime.Index).Value = DateTime.Now.ToString("HH:mm:ss")
            End If
        Next
    End Sub

    Private Sub PaintState(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex <> colState.Index Then Return
        Dim s As GameSession = TryCast(grid.Rows(e.RowIndex).Tag, GameSession)
        If s Is Nothing Then Return
        e.PaintBackground(e.CellBounds, (e.State And DataGridViewElementStates.Selected) <> 0)
        Dim busy As Boolean = _hub.IsBusy(s)
        Dim text As String = SessionLook.StateText(s, busy)
        Dim w As Integer = Math.Min(e.CellBounds.Width - 8, KoPaint.PillWidth(text, PillFont, 18))
        KoPaint.DrawPill(e.Graphics, New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + (e.CellBounds.Height - 18) \ 2, w, 18), text,
                         SessionLook.StateTone(s, busy), PillFont)
        e.Handled = True
    End Sub

    Private Sub cmbLeader_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbLeader.SelectedIndexChanged
        If _loading OrElse cmbLeader.SelectedIndex < 0 Then Return
        _hub.SetLeader(cmbLeader.SelectedIndex)
    End Sub

    Private Async Sub btnFollow_Click(sender As Object, e As EventArgs) Handles btnFollow.Click
        btnFollow.Enabled = False
        Try
            If _hub.FollowRunning Then
                Await _hub.StopFollowAsync()
            Else
                Dim notInGame As Integer = Followers().Where(Function(s) s.State <> SessionState.InGame).Count()
                lblOverall.Text = If(notInGame > 0, $"{notInGame} takipçi henüz oyunda değil — oyuna girince yürümeye başlar.", "Takipçiler lidere yürüyor.")
                Await _hub.StartFollowAsync()
            End If
        Finally
            UpdateRunning()
        End Try
    End Sub

End Class
