Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>How a session's state is shown everywhere (pill text + tone) — one table, so the game cells, the leader window and the
''' Oyunlar / Takip lists say the same word for the same state.</summary>
Friend Module SessionLook

    Friend Function StateText(s As GameSession, busy As Boolean) As String
        If Not s.Account.Enabled AndAlso Not s.Ready Then Return "Kapalı"
        Select Case s.State
            Case SessionState.Loading : Return "Yükleniyor"
            Case SessionState.LoggingIn : Return "Giriş yapıyor"
            Case SessionState.InGame : Return "Oyunda"
            Case SessionState.Failed : Return "Hata"
            Case Else : Return If(busy, "Başlıyor", If(s.Ready, "Açık", "Başlamadı"))
        End Select
    End Function

    Friend Function StateTone(s As GameSession, busy As Boolean) As KoTone
        If Not s.Account.Enabled AndAlso Not s.Ready Then Return KoTone.Neutral
        Select Case s.State
            Case SessionState.Loading, SessionState.LoggingIn : Return KoTone.Info
            Case SessionState.InGame : Return KoTone.Ok
            Case SessionState.Failed : Return KoTone.Bad
            Case Else : Return If(busy, KoTone.Info, KoTone.Neutral)
        End Select
    End Function

    ''' <summary>"2 · Okçu" — number and label (label empty = number only).</summary>
    Friend Function Title(s As GameSession) As String
        Dim lbl As String = s.Account.Label.Trim()
        Return If(lbl = "", $"{s.Number}. hesap", $"{s.Number} · {lbl}")
    End Function

End Module
