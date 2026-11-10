Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>
''' "Hesaplar": the game accounts (label, ID, password, character slot, on / off) and the LEADER (★, one of them). The list is the
''' hub's; edits change the account in place and light "kaydedilmedi" until Kaydet (Services.Accounts.AccountStore). Every
''' edit goes to the log — WHAT changed, never the password or the ID's value. Designer: AccountsView.Designer.vb.
''' </summary>
Public Class AccountsView

    Private Shared ReadOnly StarFont As New Font("Segoe MDL2 Assets", 10.0!)
    Private Shared ReadOnly PillFont As New Font("Segoe UI", 8.0!, FontStyle.Bold)
    Private Shared ReadOnly Gold As Color = Color.FromArgb(251, 191, 36)

    Private _hub As SessionHub
    Private _loading As Boolean
    Private _shown As Integer = -1

    Public Sub New()
        InitializeComponent()
        AddHandler grid.CellPainting, AddressOf PaintCell
    End Sub

    Public Sub Bind(hub As SessionHub)
        _hub = hub
        AddHandler hub.AccountsChanged, Sub() Rebuild()
        AddHandler hub.SessionsRebuilt, Sub() Rebuild()
        AddHandler hub.LeaderChanged, Sub() grid.Invalidate()
        AddHandler hub.DirtyChanged, Sub() pillDirty.Visible = hub.Dirty
        Rebuild()
    End Sub

    Private ReadOnly Property Selected As Integer
        Get
            Return If(grid.CurrentRow IsNot Nothing, grid.CurrentRow.Index, -1)
        End Get
    End Property

    Private ReadOnly Property SelectedAccount As GameAccount
        Get
            Dim i As Integer = Selected
            Return If(_hub IsNot Nothing AndAlso i >= 0 AndAlso i < _hub.Accounts.Count, _hub.Accounts(i), Nothing)
        End Get
    End Property

    ''' <summary>Rows from the hub; the selection stays on the same row number.</summary>
    Public Sub Rebuild(Optional selectRow As Integer = -1)
        If _hub Is Nothing Then Return
        Dim keep As Integer = If(selectRow >= 0, selectRow, Selected)
        _loading = True
        grid.Rows.Clear()
        For i As Integer = 0 To _hub.Accounts.Count - 1
            Dim a As GameAccount = _hub.Accounts(i)
            grid.Rows.Add("", CStr(i + 1), If(a.Label.Trim() = "", "—", a.Label), If(a.UserId = "", "(boş)", a.UserId),
                          $"{a.CharacterSlot}.", If(a.Enabled, "Açık", "Kapalı"))
        Next
        If grid.Rows.Count > 0 Then
            keep = Math.Max(0, Math.Min(grid.Rows.Count - 1, keep))
            grid.CurrentCell = grid.Rows(keep).Cells(colLabel.Index)
        End If
        _loading = False
        lblEmpty.Visible = grid.Rows.Count = 0
        pillDirty.Visible = _hub.Dirty
        ShowEditor()
    End Sub

    Private Sub ShowEditor()
        Dim a As GameAccount = SelectedAccount
        _shown = Selected
        _loading = True
        cardEdit.Enabled = a IsNot Nothing
        btnRemove.Enabled = a IsNot Nothing
        btnUp.Enabled = a IsNot Nothing AndAlso Selected > 0
        btnDown.Enabled = a IsNot Nothing AndAlso Selected < _hub.Accounts.Count - 1
        cardEdit.Title = If(a Is Nothing, "SEÇİLİ HESAP", $"{Selected + 1}. HESAP")
        txtLabel.Text = If(a?.Label, "")
        txtId.Text = If(a?.UserId, "")
        txtPassword.Text = If(a?.Password, "")
        cmbSlot.SelectedIndex = If(a Is Nothing, -1, Math.Max(0, Math.Min(3, a.CharacterSlot - 1)))
        tglEnabled.Checked = a IsNot Nothing AndAlso a.Enabled
        tglVpn.Checked = a IsNot Nothing AndAlso a.UseVpn
        Dim isLeader As Boolean = a IsNot Nothing AndAlso _hub.Leader = Selected
        btnLeader.Text = If(isLeader, "Lider bu hesap", "Lider yap")
        btnLeader.Enabled = a IsNot Nothing AndAlso Not isLeader
        btnReapply.Enabled = a IsNot Nothing AndAlso a.UserId.Trim() <> ""
        _loading = False
    End Sub

    Private Sub grid_CurrentCellChanged(sender As Object, e As EventArgs) Handles grid.CurrentCellChanged
        If _loading OrElse Selected = _shown Then Return
        ShowEditor()
    End Sub

    Private Sub grid_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles grid.CellClick
        If _hub Is Nothing OrElse e.RowIndex < 0 OrElse e.RowIndex >= _hub.Accounts.Count Then Return
        If e.ColumnIndex = colLead.Index Then
            _hub.SetLeader(e.RowIndex)
        ElseIf e.ColumnIndex = colOn.Index Then
            SetEnabled(e.RowIndex, Not _hub.Accounts(e.RowIndex).Enabled)
        ElseIf e.ColumnIndex = colVpn.Index Then
            SetVpn(e.RowIndex, Not _hub.Accounts(e.RowIndex).UseVpn)
        End If
    End Sub

    Private Sub PaintCell(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse _hub Is Nothing OrElse e.RowIndex >= _hub.Accounts.Count Then Return
        If e.ColumnIndex = colLead.Index Then
            e.PaintBackground(e.CellBounds, (e.State And DataGridViewElementStates.Selected) <> 0)
            Dim lead As Boolean = _hub.Leader = e.RowIndex
            ' Filled star (E735) for the leader, outline (E734) for the rest.
            TextRenderer.DrawText(e.Graphics, If(lead, ChrW(&HE735), ChrW(&HE734)), StarFont, e.CellBounds,
                                  If(lead, Gold, TextFaint), TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPrefix)
            e.Handled = True
        ElseIf e.ColumnIndex = colVpn.Index Then
            e.PaintBackground(e.CellBounds, (e.State And DataGridViewElementStates.Selected) <> 0)
            If _hub.Accounts(e.RowIndex).UseVpn Then
                KoPaint.DrawPill(e.Graphics, New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + (e.CellBounds.Height - 18) \ 2, Math.Min(e.CellBounds.Width - 8, 40), 18),
                                 "VPN", KoTone.Info, PillFont, dot:=False)
            Else
                TextRenderer.DrawText(e.Graphics, "—", grid.Font, e.CellBounds, TextFaint, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
            End If
            e.Handled = True
        ElseIf e.ColumnIndex = colOn.Index Then
            e.PaintBackground(e.CellBounds, (e.State And DataGridViewElementStates.Selected) <> 0)
            Dim isOn As Boolean = _hub.Accounts(e.RowIndex).Enabled
            Dim text As String = If(isOn, "Açık", "Kapalı")
            Dim w As Integer = Math.Min(e.CellBounds.Width - 8, KoPaint.PillWidth(text, PillFont, 18))
            KoPaint.DrawPill(e.Graphics, New Rectangle(e.CellBounds.X + 4, e.CellBounds.Y + (e.CellBounds.Height - 18) \ 2, w, 18), text,
                             If(isOn, KoTone.Ok, KoTone.Neutral), PillFont)
            e.Handled = True
        End If
    End Sub

    ' ---------------------------------------------------------------- toolbar

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Dim i As Integer = _hub.AddAccount()
        Rebuild(i)
        txtLabel.Focus()
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        Dim i As Integer = Selected
        If i < 0 Then Return
        If Not KoConfirm.Ask(Me, $"{_hub.Describe(i)} silinsin mi?", "Hesap listeden kalkar; kayıtlı şifresi de silinir (Kaydet ile).", "Sil") Then Return
        Dim why As String = _hub.RemoveAccount(i)
        If why <> "" Then Say(why, bad:=True) Else Rebuild(Math.Max(0, i - 1))
    End Sub

    Private Sub btnUp_Click(sender As Object, e As EventArgs) Handles btnUp.Click
        MoveSelected(-1)
    End Sub

    Private Sub btnDown_Click(sender As Object, e As EventArgs) Handles btnDown.Click
        MoveSelected(1)
    End Sub

    Private Sub MoveSelected(delta As Integer)
        Dim i As Integer = Selected
        Dim why As String = _hub.MoveAccount(i, delta)
        If why = "" Then
            Rebuild(i + delta)
        ElseIf why <> "-" Then
            Say(why, bad:=True)
        End If
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        CommitTexts()
        If _hub.Save() Then
            Say($"Kaydedildi: {_hub.Accounts.Count} hesap.", bad:=False)
        Else
            Say("Kaydedilemedi — kayıt sekmesine bakın.", bad:=True)
        End If
    End Sub

    Private Sub btnReapply_Click(sender As Object, e As EventArgs) Handles btnReapply.Click
        CommitId()
        Dim why As String = _hub.ForgetGameSetup(Selected)
        If why = "" Then Say("Oyun ayarları bu hesabın bir sonraki girişinde yeniden uygulanacak.", bad:=False) Else Say(why, bad:=True)
    End Sub

    Private Sub btnLeader_Click(sender As Object, e As EventArgs) Handles btnLeader.Click
        If Selected >= 0 Then _hub.SetLeader(Selected)
        ShowEditor()
    End Sub

    ''' <summary>Short message in the card's hint line (red for problems).</summary>
    Private Sub Say(text As String, bad As Boolean)
        lblHint.Text = text
        lblHint.ForeColor = If(bad, BadRed, OkGreen)
    End Sub

    ' ---------------------------------------------------------------- editor

    ''' <summary>Text boxes commit on Enter / leaving; Kaydet commits the focused one first.</summary>
    Private Sub CommitTexts()
        CommitLabel()
        CommitId()
        CommitPassword()
    End Sub

    Private Sub txtLabel_Committed(sender As Object, e As EventArgs) Handles txtLabel.Committed
        CommitLabel()
    End Sub

    Private Sub txtId_Committed(sender As Object, e As EventArgs) Handles txtId.Committed
        CommitId()
    End Sub

    Private Sub txtPassword_Committed(sender As Object, e As EventArgs) Handles txtPassword.Committed
        CommitPassword()
    End Sub

    Private Sub CommitLabel()
        Dim a As GameAccount = SelectedAccount
        If _loading OrElse a Is Nothing Then Return
        Dim v As String = txtLabel.Text.Trim()
        If v = a.Label Then Return
        Dim was As String = a.Label
        a.Label = v
        _hub.Log($"{Selected + 1}. hesabın etiketi '{was}' → '{v}'")
        _hub.MarkEdited()
    End Sub

    Private Sub CommitId()
        Dim a As GameAccount = SelectedAccount
        If _loading OrElse a Is Nothing Then Return
        Dim v As String = txtId.Text.Trim()
        If v = a.UserId Then Return
        a.UserId = v
        _hub.Log($"{_hub.Describe(Selected)}: oyun ID'si değişti")
        _hub.MarkEdited()
    End Sub

    Private Sub CommitPassword()
        Dim a As GameAccount = SelectedAccount
        If _loading OrElse a Is Nothing Then Return
        If txtPassword.Text = a.Password Then Return
        a.Password = txtPassword.Text
        _hub.Log($"{_hub.Describe(Selected)}: şifre değişti")
        _hub.MarkEdited()
    End Sub

    Private Sub cmbSlot_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSlot.SelectedIndexChanged
        Dim a As GameAccount = SelectedAccount
        If _loading OrElse a Is Nothing OrElse cmbSlot.SelectedIndex < 0 Then Return
        Dim v As Integer = cmbSlot.SelectedIndex + 1
        If v = a.CharacterSlot Then Return
        Dim was As Integer = a.CharacterSlot
        a.CharacterSlot = v
        _hub.Log($"{_hub.Describe(Selected)}: karakter {was} → {v}")
        _hub.MarkEdited()
    End Sub

    Private Sub tglEnabled_CheckedChanged(sender As Object, e As EventArgs) Handles tglEnabled.CheckedChanged
        If _loading OrElse SelectedAccount Is Nothing Then Return
        SetEnabled(Selected, tglEnabled.Checked)
    End Sub

    Private Sub tglVpn_CheckedChanged(sender As Object, e As EventArgs) Handles tglVpn.CheckedChanged
        If _loading OrElse SelectedAccount Is Nothing Then Return
        SetVpn(Selected, tglVpn.Checked)
    End Sub

    Private Sub SetVpn(index As Integer, value As Boolean)
        Dim a As GameAccount = _hub.Accounts(index)
        If a.UseVpn = value Then Return
        a.UseVpn = value
        Dim s As GameSession = If(index < _hub.Sessions.Count, _hub.Sessions(index), Nothing)
        Dim note As String = If(s IsNot Nothing AndAlso s.Ready, " — açık oyunda yeniden girişte uygulanır", "")
        _hub.Log($"{_hub.Describe(index)}: VPN ile gir " & If(value, "açıldı", "kapatıldı") & note)
        _hub.MarkEdited()
        If index = Selected Then
            _loading = True
            tglVpn.Checked = value
            _loading = False
        End If
    End Sub

    Private Sub SetEnabled(index As Integer, value As Boolean)
        Dim a As GameAccount = _hub.Accounts(index)
        If a.Enabled = value Then Return
        a.Enabled = value
        _hub.Log($"{_hub.Describe(index)} " & If(value, "açıldı", "kapatıldı (başlatılmaz)"))
        _hub.MarkEdited()
    End Sub

End Class
