Imports System.Threading
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Input
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Core.Vision
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Engine.Login

    ''' <summary>
    ''' Constants of the chain in ONE place. "ölçülmedi" ones are guesses until the user's first live run (frames in
    ''' Logs\multi-login) — LOGIN.md lists them.
    ''' </summary>
    Public Module LoginPlan
        ''' <summary>Home page → game frame shown. First visit downloads ~135 MB.</summary>
        Public Const LauncherTimeoutMs As Integer = 10 * 60 * 1000
        ''' <summary>Download progress unchanged this long → give up.</summary>
        Public Const DownloadStallMs As Integer = 120 * 1000
        Public Const MaxStartPresses As Integer = 3
        ''' <summary>Game frame shown → login box seen.</summary>
        Public Const LoginScreenTimeoutMs As Integer = 90 * 1000
        ''' <summary>Typed text must show in the box within this.</summary>
        Public Const TypedVisibleMs As Integer = 2000
        ''' <summary>OK → the login box must be gone.</summary>
        Public Const LoginAcceptMs As Integer = 30 * 1000
        ''' <summary>After login, whole way to the world (server, character, loading — the first run downloads ~425 MB more).</summary>
        Public Const ToWorldTimeoutMs As Integer = 10 * 60 * 1000
        ''' <summary>A screen counts as "settled" when two frames this far apart barely differ.</summary>
        Public Const SettleGapMs As Integer = 700
        Public Const SettleMaxDiff As Double = 0.06
        ''' <summary>A press counts when this share of the scene samples moved (server Enter measured live: 73-88 %, 0,6-2,5 s).</summary>
        Public Const ChangeMinDiff As Double = 0.2
        Public Const StepChangeWaitMs As Integer = 6000
        ''' <summary>Character switch: after the scene changed, the arrows must be back within this. (ölçülmedi)</summary>
        Public Const CharacterBackMs As Integer = 8000
        ''' <summary>Enter / Start Game → the character screen must be gone within this. (ölçülmedi)</summary>
        Public Const CharacterLeaveMs As Integer = 6000
        Public Const MaxServerTries As Integer = 3
        Public Const MaxCharacterTries As Integer = 2
        Public Const MaxCharacters As Integer = 4
        ''' <summary>Unknown screen after login: frame at first sight, then this often (max 5); NO keys are sent on it.</summary>
        Public Const UnknownShotEveryMs As Integer = 60 * 1000
    End Module

    ''' <summary>
    ''' One account's own way into the game in its own view (see LOGIN.md for the measured flow):
    ''' 1) ardream.net home → START (read-only DOM probe on the HOME page only, click through DevTools) → the site shows the game frame;
    ''' 2) the game's ID / password box in the canvas (frames only, no script) → ID, password — each box must SHOW the typed text
    '''    before going on → Enter (OK as fallback) → the box must go;
    ''' 3) server list / character screen (ölçülmedi: keys, every press measured by a scene change) → HP / MP bars seen = in game.
    ''' Every step has a timeout and a way out: Failed with the reason (status + log + a frame in Logs\multi-login).
    ''' The password never goes to the log or a frame file; the ID only as its length.
    ''' </summary>
    Public Module WebLogin

        ''' <summary>TEST HOOK (LoginProbe only): stop after both boxes show the typed text, empty them, never press Enter / OK.</summary>
        Friend StopBeforeSubmit As Boolean = False
        ''' <summary>TEST HOOK: text columns of password / ID box after the test-mode clear.</summary>
        Friend ReadOnly LastTestClearColumns As New List(Of Integer)()

        ''' <summary>
        ''' The in-game steps ALONE on a session that is already in the world (UI button, so the user can test them without logging in
        ''' again): screen must read as InGame, then AfterEnter (windows / sound / PMs) and GameSetup. <paramref name="forceSetup"/> =
        ''' forget the one-time mark first (apply the Option settings again). Returns GameSetup's result; False if not in game.
        ''' </summary>
        Public Async Function RunInGameStepsAsync(session As GameSession, ct As CancellationToken, forceSetup As Boolean) As Task(Of Boolean)
            If session Is Nothing OrElse Not session.Ready Then Return False
            Dim n = session.Number
            Try
                Dim r = Await ReadAsync(session)
                If r.Kind <> ScreenKind.InGame Then
                    BotLog.Warn("giris", $"{n}. görünüm: oyun içi adımlar istendi ama oyunda değil ({r.Kind})")
                    session.SetStatus(session.State, "Oyun içi adımlar: oyunda değil")
                    Return False
                End If
                BotLog.Add("giris", $"{n}. görünüm: oyun içi adımlar elle başlatıldı{If(forceSetup, " (oyun ayarı yeniden)", "")}")
                Await AfterEnter.RunAsync(session, ct)
                If forceSetup Then GameSetup.Forget(session.Account?.UserId)
                Return Await GameSetup.RunAsync(session, ct)
            Catch ex As OperationCanceledException
                BotLog.Add("giris", $"{n}. görünüm: oyun içi adımlar durduruldu")
                session.SetStatus(SessionState.InGame, "Oyunda")
                Return False
            End Try
        End Function

        ''' <summary>ardream's own ID rule (hesap.js): [a-z0-9_] 3-20; capitals accepted (the site lowercases). Side-effect free.</summary>
        Public Function IdLooksValid(id As String) As Boolean
            Return id IsNot Nothing AndAlso Text.RegularExpressions.Regex.IsMatch(id, "^[A-Za-z0-9_]{3,20}$")
        End Function

        ''' <summary>
        ''' MANUAL entry: opens the site and sends NOTHING to the game — the user logs in by hand in the view. The screen is
        ''' read every 2 s (frames only, no input); when the game HUD shows, the session becomes InGame so follow / mirror
        ''' include it. Runs until the HUD is seen or the user presses Durdur.
        ''' </summary>
        Public Async Function RunManualAsync(session As GameSession, ct As CancellationToken) As Task(Of Boolean)
            If session Is Nothing Then Return False
            Dim n = session.Number
            Try
                If Not session.Ready Then Return Fail(session, "Görünüm hazır değil")
                Dim cw = session.Cw
                Dim src = If(cw.Source, "").ToLowerInvariant()
                If Not src.StartsWith("https://ardream.net") Then cw.Navigate(LauncherPage.HomeUrl)
                Status(session, SessionState.LoggingIn, "Elle giriş bekleniyor — pencereden giriş yapın")
                BotLog.Add("giris", $"{n}. görünüm: elle giriş — site açıldı, oyuna tuş / tık gönderilmiyor")
                Dim sw = Stopwatch.StartNew()
                While True
                    ct.ThrowIfCancellationRequested()
                    Dim r As ScreenRead = Nothing
                    Try
                        r = Await ReadAsync(session)
                    Catch
                    End Try
                    If r IsNot Nothing AndAlso r.Kind = ScreenKind.InGame Then
                        session.SetStatus(SessionState.InGame, "Oyunda · elle girildi")
                        BotLog.Add("giris", $"{n}. görünüm: elle girildi — oyun ekranı görüldü ({sw.Elapsed.TotalMinutes:0.0} dk)")
                        Return True
                    End If
                    Await Task.Delay(2000, ct)
                End While
                Return False
            Catch ex As OperationCanceledException
                session.SetStatus(SessionState.NotStarted, "Elle giriş durduruldu")
                BotLog.Add("giris", $"{n}. görünüm: elle giriş durduruldu")
                Return False
            Catch ex As Exception When ct.IsCancellationRequested
                session.SetStatus(SessionState.NotStarted, "Elle giriş durduruldu")
                BotLog.Add("giris", $"{n}. görünüm: elle giriş durduruldu")
                Return False
            Catch ex As Exception
                BotLog.Fail("giris", $"{n}. görünüm: elle giriş hatası", ex)
                Return Fail(session, "Beklenmeyen hata: " & ex.Message)
            End Try
        End Function

        Public Async Function RunAsync(session As GameSession, ct As CancellationToken) As Task(Of Boolean)
            If session Is Nothing Then Return False
            Dim n = session.Number
            Dim acc = session.Account
            Try
                If Not session.Ready Then Return Fail(session, "Görünüm hazır değil")
                If acc Is Nothing OrElse String.IsNullOrWhiteSpace(acc.UserId) OrElse String.IsNullOrEmpty(acc.Password) Then
                    Return Fail(session, "Hesap ID / şifre boş")
                End If
                ' ardream has AUTO-REGISTRATION (the login box says so): a mistyped ID opens a NEW account instead of failing.
                ' The site's own rule for an ID is [a-z0-9_] 3-20 → anything else is refused here, before a single key goes out.
                If Not IdLooksValid(acc.UserId) Then Return Fail(session, "ID geçersiz (3-20 karakter: harf, rakam, _) — yeni hesap açılmasın diye yazılmadı")
                BotLog.Add("giris", $"{n}. görünüm: giriş başladı (ID {acc.UserId.Length} karakter, karakter {acc.CharacterSlot})")

                ' 1) launcher
                Dim launch = Await LauncherAsync(session, ct)
                If launch IsNot Nothing Then Return Fail(session, launch)

                ' 2) login box (or already in game)
                Status(session, SessionState.LoggingIn, "Giriş ekranı bekleniyor")
                Dim first = Await WaitScreenAsync(session, ct, LoginPlan.LoginScreenTimeoutMs, {ScreenKind.Login, ScreenKind.InGame})
                If first Is Nothing Then
                    Await LoginShots.SaveAsync(session, "giris-ekrani-yok")
                    Return Fail(session, "Oyunun giriş ekranı tanınmadı (kare kaydedildi)")
                End If
                If first.Kind = ScreenKind.InGame Then Return Done(session, "zaten oyunda")
                Await LoginShots.SaveAsync(session, "giris-ekrani")
                BotLog.Add("giris", $"{n}. görünüm: giriş ekranı bulundu — {first.Detail}")

                Dim cred = Await CredentialsAsync(session, first.Login, ct)
                If cred IsNot Nothing Then Return Fail(session, cred)
                If StopBeforeSubmit Then
                    session.SetStatus(SessionState.NotStarted, "Sınama: Enter'dan önce durdu")
                    BotLog.Add("giris", $"{n}. görünüm: sınama kipi — Enter'a basılmadı")
                    Return True
                End If

                ' 3) after login → world
                Dim world = Await ToWorldAsync(session, ct)
                If world IsNot Nothing Then Return Fail(session, world)
                Await AfterEnter.RunAsync(session, ct)
                Await GameSetup.RunAsync(session, ct)   ' once per account; never fails the login
                Return Done(session, "oyunda")
            Catch ex As OperationCanceledException
                session.SetStatus(SessionState.NotStarted, "Giriş durduruldu")
                BotLog.Add("giris", $"{n}. görünüm: giriş durduruldu")
                Return False
            Catch ex As Exception When ct.IsCancellationRequested
                ' Stopped while a browser call was running (view closed): a stop, not an error.
                session.SetStatus(SessionState.NotStarted, "Giriş durduruldu")
                BotLog.Add("giris", $"{n}. görünüm: giriş durduruldu")
                Return False
            Catch ex As Exception
                BotLog.Fail("giris", $"{n}. görünüm: giriş hatası", ex)
                Return Fail(session, "Beklenmeyen hata: " & ex.Message)
            End Try
        End Function

        ' ---------------- 1) launcher ----------------

        ''' <summary>Nothing = the game frame is shown; otherwise the failure reason.</summary>
        Private Async Function LauncherAsync(session As GameSession, ct As CancellationToken) As Task(Of String)
            Dim cw = session.Cw
            Status(session, SessionState.Loading, "Site açılıyor")
            Dim src = If(cw.Source, "").ToLowerInvariant()
            If Not (src.StartsWith("https://ardream.net") AndAlso Not src.StartsWith("https://ardream.net/play")) Then
                cw.Navigate(LauncherPage.HomeUrl)
                Await Task.Delay(1500, ct)
            End If
            Dim sw = Stopwatch.StartNew()
            Dim pressed = 0
            Dim lastProgress As Double = -1, lastMove As Long = 0
            Dim lastStep As LauncherPage.LauncherStep = CType(-1, LauncherPage.LauncherStep)
            While sw.ElapsedMilliseconds < LoginPlan.LauncherTimeoutMs
                ct.ThrowIfCancellationRequested()
                Dim st As LauncherPage.LauncherState = Nothing
                Try
                    st = Await LauncherPage.ReadAsync(cw)
                Catch ex As Exception
                    st = Nothing   ' navigation in progress — try again below
                End Try
                If st Is Nothing Then
                    Await Task.Delay(700, ct)
                    Continue While
                End If
                Dim stp = LauncherPage.Decide(st, pressed > 0)
                If stp <> lastStep Then
                    BotLog.Add("giris", $"{session.Number}. görünüm: site — {stp} ('{Clip(st.StatusText)}')")
                    lastStep = stp
                End If
                Select Case stp
                    Case LauncherPage.LauncherStep.GameShown
                        Status(session, SessionState.Loading, "Oyun açılıyor")
                        Return Nothing
                    Case LauncherPage.LauncherStep.WrongPage
                        Return $"Site açılmadı ({Clip(st.Url)})"
                    Case LauncherPage.LauncherStep.DirectPlay
                        Await LoginShots.SaveAsync(session, "dogrudan-play")
                        Return "Site oyunu çerçevede açamadı (doğrudan /play) — bu yol desteklenmiyor"
                    Case LauncherPage.LauncherStep.PressStart, LauncherPage.LauncherStep.RetryStart
                        If pressed >= LoginPlan.MaxStartPresses Then
                            Await LoginShots.SaveAsync(session, "start-tutmadi")
                            Return $"START {pressed} kez basıldı, oyun açılmadı ('{Clip(st.StatusText)}')"
                        End If
                        If st.StartPoint.X <= 0 OrElse st.StartPoint.Y <= 0 OrElse st.StartPoint.X >= st.ViewSize.Width OrElse st.StartPoint.Y >= st.ViewSize.Height Then
                            Return "START düğmesi görünümün dışında (görünüm çok küçük olabilir)"
                        End If
                        Await CdpInput.ClickAsync(cw, st.StartPoint.X, st.StartPoint.Y)
                        pressed += 1
                        BotLog.Add("giris", $"{session.Number}. görünüm: START'a basıldı ({pressed}. kez, {st.StartPoint.X},{st.StartPoint.Y})")
                        Status(session, SessionState.Loading, "START'a basıldı")
                        lastMove = sw.ElapsedMilliseconds
                        Await Task.Delay(1200, ct)
                        Continue While
                    Case LauncherPage.LauncherStep.Downloading
                        If Math.Abs(st.Progress - lastProgress) > 0.0005 Then lastProgress = st.Progress : lastMove = sw.ElapsedMilliseconds
                        Status(session, SessionState.Loading, If(st.Progress > 0, $"Oyun dosyaları iniyor %{Math.Floor(st.Progress * 100)}", "Oyun dosyaları hazırlanıyor"))
                        If sw.ElapsedMilliseconds - lastMove > LoginPlan.DownloadStallMs Then
                            Return $"İndirme {LoginPlan.DownloadStallMs \ 1000} sn ilerlemedi ('{Clip(st.StatusText)}')"
                        End If
                    Case LauncherPage.LauncherStep.Wait
                        Status(session, SessionState.Loading, "Site yükleniyor")
                End Select
                Await Task.Delay(700, ct)
            End While
            Return "Site / oyun yüklenmesi zaman aşımına uğradı"
        End Function

        ' ---------------- 2) credentials ----------------

        ''' <summary>Nothing = the login box went after OK; otherwise the failure reason. No frame files here (the ID is visible).</summary>
        Private Async Function CredentialsAsync(session As GameSession, box As LoginBox, ct As CancellationToken) As Task(Of String)
            Dim cw = session.Cw
            Dim acc = session.Account

            Status(session, SessionState.LoggingIn, "ID yazılıyor")
            Dim idOk = Await TypeIntoAsync(session, box.IdBox, acc.UserId, ct)
            If Not idOk Then Return "ID kutusuna yazı geçmedi"

            Status(session, SessionState.LoggingIn, "Şifre yazılıyor")
            Dim pwOk = Await TypeIntoAsync(session, box.PasswordBox, acc.Password, ct)
            If Not pwOk Then Return "Şifre kutusuna yazı geçmedi"

            ' the ID box must still hold its text (a missed click on the password box could have typed into the ID box)
            Using f = Await ViewCapture.FullAsync(cw)
                Dim cols = LoginScreens.TextColumns(f, box.IdBox)
                If cols >= 0 AndAlso cols < LoginScreens.TextColumnThreshold Then Return "ID kutusu şifreden sonra boş görünüyor — giriş yapılmadı"
            End Using
            If StopBeforeSubmit Then
                ' test mode: nothing is sent — (dummy text only: frame allowed) save, empty both boxes again and report
                Await LoginShots.SaveAsync(session, "sinama-yazildi")
                For Each b In {box.PasswordBox, box.IdBox}
                    Dim c = LoginBox.Center(b)
                    Await CdpInput.ClickAsync(cw, c.X, c.Y)
                    Await Task.Delay(200, ct)
                    Await LoginKeys.EndAsync(cw)
                    For i = 1 To 24
                        Await LoginKeys.BackspaceAsync(cw)
                    Next
                    Await Task.Delay(300, ct)
                    LastTestClearColumns.Add(Await ColumnsAsync(cw, b))
                Next
                Return Nothing
            End If

            Status(session, SessionState.LoggingIn, "Giriş yapılıyor")
            Dim before As Integer()
            Using f = Await ViewCapture.FullAsync(cw)
                before = LoginScreens.Signature(f)
            End Using
            Await LoginKeys.EnterAsync(cw)
            BotLog.Add("giris", $"{session.Number}. görünüm: ID ve şifre yazıldı, Enter")
            Dim sw = Stopwatch.StartNew()
            Dim okClicked = False
            While sw.ElapsedMilliseconds < LoginPlan.LoginAcceptMs
                Await Task.Delay(600, ct)
                Dim r = Await ReadAsync(session)
                If r.Kind <> ScreenKind.Login Then
                    BotLog.Add("giris", $"{session.Number}. görünüm: giriş kutusu kapandı ({sw.ElapsedMilliseconds} ms, ekran: {r.Kind})")
                    Return Nothing
                End If
                If Not okClicked AndAlso sw.ElapsedMilliseconds > 3000 Then
                    ' Enter did not take → the OK button once
                    okClicked = True
                    BotLog.Add("giris", $"{session.Number}. görünüm: Enter tutmadı, OK'a basılıyor ({r.Login.OkButton.X},{r.Login.OkButton.Y})")
                    Await CdpInput.ClickAsync(cw, r.Login.OkButton.X, r.Login.OkButton.Y)
                End If
            End While
            ' still the login box: wrong password / server message — the box is visible, the ID too → clear nothing, save no frame
            Return "Giriş kabul edilmedi (şifre yanlış ya da oyunun uyarısı var)"
        End Function

        ''' <summary>Click the box, clear what is in it, type, and SEE the text. One retry with a new click.</summary>
        Private Async Function TypeIntoAsync(session As GameSession, box As Rectangle, text As String, ct As CancellationToken) As Task(Of Boolean)
            Dim cw = session.Cw
            Dim c = LoginBox.Center(box)
            For attempt = 1 To 2
                ct.ThrowIfCancellationRequested()
                Await CdpInput.ClickAsync(cw, c.X, c.Y)
                Await Task.Delay(250, ct)
                ' clear: only the clicked box, only as long as it still shows text (bounded)
                Dim cols = Await ColumnsAsync(cw, box)
                If cols >= LoginScreens.TextColumnThreshold Then
                    Await LoginKeys.EndAsync(cw)
                    For i = 1 To 24
                        Await LoginKeys.BackspaceAsync(cw)
                        If i Mod 4 = 0 Then
                            cols = Await ColumnsAsync(cw, box)
                            If cols < LoginScreens.TextColumnThreshold Then Exit For
                        End If
                    Next
                    If cols >= LoginScreens.TextColumnThreshold Then
                        BotLog.Warn("giris", $"{session.Number}. görünüm: kutudaki eski yazı silinemedi ({attempt}. deneme)")
                        Continue For
                    End If
                End If
                Await LoginKeys.TypeAsync(cw, text)
                Dim sw = Stopwatch.StartNew()
                While sw.ElapsedMilliseconds < LoginPlan.TypedVisibleMs
                    cols = Await ColumnsAsync(cw, box)
                    If cols >= LoginScreens.TextColumnThreshold Then Return True
                    Await Task.Delay(200, ct)
                End While
                BotLog.Warn("giris", $"{session.Number}. görünüm: yazı kutuda görünmedi ({attempt}. deneme, {cols} sütun)")
            Next
            Return False
        End Function

        Private Async Function ColumnsAsync(cw As Microsoft.Web.WebView2.Core.CoreWebView2, box As Rectangle) As Task(Of Integer)
            Using f = Await ViewCapture.FullAsync(cw)
                Return LoginScreens.TextColumns(f, box)
            End Using
        End Function

        ' ---------------- 3) to the world ----------------

        ''' <summary>
        ''' Login accepted → world. Acts ONLY on screens it recognises (measured on the user's live run, 9 Nov 23:44):
        ''' Server ("Select Server" + Connect) → Enter, Connect if Enter did not change the scene;
        ''' Character (◄ ► + panel) → arrows to CharacterSlot, Enter, Start Game if Enter did not leave the screen;
        ''' InGame (HP / MP frame) → done. Unknown screens (loading, nation select of an account without a character, notices)
        ''' get NO keys — only a frame and waiting (a blind Enter on the nation screen could pick a nation).
        ''' </summary>
        Private Async Function ToWorldAsync(session As GameSession, ct As CancellationToken) As Task(Of String)
            Dim cw = session.Cw
            Dim sw = Stopwatch.StartNew()
            Dim serverTries = 0, characterTries = 0
            Dim unknownSince As Long = -1, lastUnknownShot As Long = -LoginPlan.UnknownShotEveryMs, unknownShots = 0
            Status(session, SessionState.LoggingIn, "Sunucu ekranı bekleniyor")
            While sw.ElapsedMilliseconds < LoginPlan.ToWorldTimeoutMs
                ct.ThrowIfCancellationRequested()
                Dim r = Await SettledAsync(session, ct)
                If r Is Nothing Then Continue While
                If r.Kind <> ScreenKind.Unknown Then unknownSince = -1
                Select Case r.Kind
                    Case ScreenKind.InGame
                        BotLog.Add("giris", $"{session.Number}. görünüm: oyunda — {r.Detail}")
                        Return Nothing
                    Case ScreenKind.Login
                        ' only a REAL ID / password layout (centred boxes + red OK at the ardream ratio) gets here
                        Await LoginShots.SaveAsync(session, "giris-geri-geldi")
                        Return "Giriş ekranına geri dönüldü (oturum kabul edilmedi)"
                    Case ScreenKind.Server
                        If characterTries > 0 Then
                            ' after the character step the server list does not come back: an orange in-game line read as
                            ' "Connect" (user's 2nd run: Enter + a click went into the world) → no keys, wait like Unknown
                            Await Task.Delay(1000, ct)
                            Continue While
                        End If
                        serverTries += 1
                        If serverTries > LoginPlan.MaxServerTries Then
                            Await LoginShots.SaveAsync(session, "sunucu-gecilemedi")
                            Return "Sunucu ekranı geçilemedi (kare kaydedildi)"
                        End If
                        Status(session, SessionState.LoggingIn, "Sunucuya bağlanılıyor")
                        BotLog.Add("giris", $"{session.Number}. görünüm: sunucu ekranı ({r.Detail}), {serverTries}. deneme")
                        If Not Await PressMeasuredAsync(session, Function() LoginKeys.EnterAsync(cw), "sunucu: Enter", ct, attempts:=1) Then
                            Dim c = r.Connect
                            If Not Await PressMeasuredAsync(session, Function() HoverClickAsync(cw, c), $"sunucu: Connect ({c.X},{c.Y})", ct, attempts:=1) Then
                                Await LoginShots.SaveAsync(session, "sunucu-tutmadi")
                            End If
                        End If
                        Status(session, SessionState.LoggingIn, "Karakter ekranı bekleniyor")
                    Case ScreenKind.Character
                        characterTries += 1
                        If characterTries > LoginPlan.MaxCharacterTries Then
                            Await LoginShots.SaveAsync(session, "karakter-gecilemedi")
                            Return "Karakter ekranında oyuna girilemedi — Start Game'e kendiniz basın (kare kaydedildi)"
                        End If
                        BotLog.Add("giris", $"{session.Number}. görünüm: karakter ekranı — {r.Detail}")
                        Dim why = Await SelectCharacterAsync(session, r.Character, ct)
                        If why IsNot Nothing Then
                            Await LoginShots.SaveAsync(session, "karakter-secilemedi")
                            Return why
                        End If
                        Status(session, SessionState.LoggingIn, "Oyun yükleniyor")
                    Case ScreenKind.Blank
                        Status(session, SessionState.LoggingIn, "Yükleniyor")
                        Await Task.Delay(800, ct)
                    Case ScreenKind.Unknown
                        If unknownSince < 0 Then unknownSince = sw.ElapsedMilliseconds
                        If unknownShots < 5 AndAlso sw.ElapsedMilliseconds - lastUnknownShot >= LoginPlan.UnknownShotEveryMs Then
                            unknownShots += 1
                            lastUnknownShot = sw.ElapsedMilliseconds
                            Await LoginShots.SaveAsync(session, $"taninmayan{unknownShots}")
                            BotLog.Add("giris", $"{session.Number}. görünüm: tanınmayan ekran — tuş gönderilmiyor, bekleniyor ({Clip(r.Detail)})")
                        End If
                        Status(session, SessionState.LoggingIn, $"Bekleniyor (tanınmayan ekran, {(sw.ElapsedMilliseconds - unknownSince) \ 1000} sn)")
                        Await Task.Delay(1000, ct)
                End Select
            End While
            Await LoginShots.SaveAsync(session, "dunya-zaman-asimi")
            Return "Oyuna girilemedi — zaman aşımı (kare kaydedildi)"
        End Function

        ''' <summary>
        ''' Character N (1-based): ► until it is grey (= first character), then ◄ N-1 times (each must be gold); every press is
        ''' measured (scene changes, arrows come back). Then Enter; if the screen stays, Start Game. Nothing = left the screen.
        ''' </summary>
        Private Async Function SelectCharacterAsync(session As GameSession, layout As CharacterLayout, ct As CancellationToken) As Task(Of String)
            Dim cw = session.Cw
            Dim slot = Math.Max(1, Math.Min(LoginPlan.MaxCharacters, session.Account.CharacterSlot))
            Dim moves = 0
            While layout.RightEnabled AndAlso moves < LoginPlan.MaxCharacters
                Status(session, SessionState.LoggingIn, "1. karaktere dönülüyor")
                layout = Await ArrowStepAsync(session, layout, toLeft:=False, ct)
                If layout Is Nothing Then Return "Karakter oku (►) tıkı almadı — karakteri kendiniz seçin"
                moves += 1
            End While
            If layout.RightEnabled Then Return "1. karaktere dönülemedi — karakteri kendiniz seçin"
            For i = 2 To slot
                If Not layout.LeftEnabled Then
                    BotLog.Warn("giris", $"{session.Number}. görünüm: {slot}. karakter yok — {i - 1} karakter var")
                    Return $"{slot}. karakter yok — bu hesapta {i - 1} karakter var"
                End If
                Status(session, SessionState.LoggingIn, $"{i}. karaktere geçiliyor")
                layout = Await ArrowStepAsync(session, layout, toLeft:=True, ct)
                If layout Is Nothing Then Return $"{i}. karaktere geçilemedi (◄ tıkı ekranı değiştirmedi)"
            Next
            BotLog.Add("giris", $"{session.Number}. görünüm: {slot}. karakterdeyiz ({layout})")

            Status(session, SessionState.LoggingIn, $"{slot}. karakterle oyuna giriliyor")
            Await LoginKeys.EnterAsync(cw)
            If Await LeftCharacterAsync(session, ct) Then
                BotLog.Add("giris", $"{session.Number}. görünüm: karakter ekranı Enter ile geçildi")
                Return Nothing
            End If
            If layout.StartGame.IsEmpty Then Return "Enter karakter ekranını geçmedi ve Start Game bulunamadı — Start Game'e kendiniz basın"
            BotLog.Add("giris", $"{session.Number}. görünüm: Enter tutmadı, Start Game'e basılıyor ({layout.StartGame.X},{layout.StartGame.Y})")
            Await HoverClickAsync(cw, layout.StartGame)
            If Await LeftCharacterAsync(session, ct) Then
                BotLog.Add("giris", $"{session.Number}. görünüm: karakter ekranı Start Game ile geçildi")
                Return Nothing
            End If
            ' still there: the outer loop sees the character screen again and tries once more (MaxCharacterTries)
            BotLog.Warn("giris", $"{session.Number}. görünüm: Enter ve Start Game karakter ekranını geçmedi")
            Return Nothing
        End Function

        ''' <summary>One arrow press, measured: the scene must change, then the arrows must be back. Up to 3 presses. New layout or Nothing.</summary>
        Private Async Function ArrowStepAsync(session As GameSession, layout As CharacterLayout, toLeft As Boolean, ct As CancellationToken) As Task(Of CharacterLayout)
            Dim cw = session.Cw
            Dim arrow = If(toLeft, "◄", "►")
            For attempt = 1 To 3
                Dim before As Integer()
                Using f = Await ViewCapture.FullAsync(cw)
                    before = LoginScreens.Signature(f)
                End Using
                Await HoverClickAsync(cw, If(toLeft, layout.LeftArrow, layout.RightArrow))
                Dim changed = False
                Dim sw = Stopwatch.StartNew()
                While sw.ElapsedMilliseconds < LoginPlan.StepChangeWaitMs
                    Await Task.Delay(300, ct)
                    Using f = Await ViewCapture.FullAsync(cw)
                        If LoginScreens.Difference(before, LoginScreens.Signature(f)) >= LoginPlan.ChangeMinDiff Then changed = True : Exit While
                    End Using
                End While
                If changed Then
                    sw.Restart()
                    While sw.ElapsedMilliseconds < LoginPlan.CharacterBackMs
                        Dim r = Await SettledAsync(session, ct)
                        If r IsNot Nothing AndAlso r.Kind = ScreenKind.Character Then
                            BotLog.Add("giris", $"{session.Number}. görünüm: karakter {arrow} — geçildi ({r.Character})")
                            Return r.Character
                        End If
                    End While
                    BotLog.Warn("giris", $"{session.Number}. görünüm: karakter {arrow} — ekran değişti ama oklar geri gelmedi")
                    Return Nothing
                End If
                BotLog.Warn("giris", $"{session.Number}. görünüm: karakter {arrow} — ekran değişmedi ({attempt}. deneme)")
                Dim again = Await ReadAsync(session)
                If again.Kind <> ScreenKind.Character Then Return Nothing
                layout = again.Character
                If If(toLeft, Not layout.LeftEnabled, Not layout.RightEnabled) Then Return Nothing   ' that way closed without a move
            Next
            Return Nothing
        End Function

        ''' <summary>True when the character screen is gone in two reads in a row within CharacterLeaveMs.</summary>
        Private Async Function LeftCharacterAsync(session As GameSession, ct As CancellationToken) As Task(Of Boolean)
            Dim sw = Stopwatch.StartNew()
            Dim gone = 0
            While sw.ElapsedMilliseconds < LoginPlan.CharacterLeaveMs
                Await Task.Delay(400, ct)
                Dim r = Await ReadAsync(session)
                If r.Kind = ScreenKind.Character Then gone = 0 Else gone += 1
                If gone >= 2 Then Return True
            End While
            Return False
        End Function

        ''' <summary>Move onto the button first (the game took clicks that came ONTO a button — desktop KO), then click.</summary>
        Private Async Function HoverClickAsync(cw As Microsoft.Web.WebView2.Core.CoreWebView2, p As Point) As Task
            Await CdpInput.Mouse(cw, "mouseMoved", p.X, p.Y)
            Await Task.Delay(250)
            Await CdpInput.ClickAsync(cw, p.X, p.Y)
        End Function

        ''' <summary>Press, then the scene must change within StepChangeWaitMs; one more press if not.</summary>
        Private Async Function PressMeasuredAsync(session As GameSession, press As Func(Of Task), what As String, ct As CancellationToken,
                                                  Optional attempts As Integer = 2) As Task(Of Boolean)
            Dim cw = session.Cw
            For attempt = 1 To attempts
                Dim before As Integer()
                Using f = Await ViewCapture.FullAsync(cw)
                    before = LoginScreens.Signature(f)
                End Using
                Await press()
                Dim sw = Stopwatch.StartNew()
                While sw.ElapsedMilliseconds < LoginPlan.StepChangeWaitMs
                    Await Task.Delay(400, ct)
                    Using f = Await ViewCapture.FullAsync(cw)
                        Dim d = LoginScreens.Difference(before, LoginScreens.Signature(f))
                        If d >= LoginPlan.ChangeMinDiff Then
                            BotLog.Add("giris", $"{session.Number}. görünüm: {what} — ekran değişti ({sw.ElapsedMilliseconds} ms, %{Math.Round(d * 100)})")
                            Return True
                        End If
                    End Using
                End While
                BotLog.Warn("giris", $"{session.Number}. görünüm: {what} — ekran değişmedi ({attempt}. deneme)")
            Next
            Return False
        End Function

        ''' <summary>Two frames SettleGapMs apart that barely differ → classify the second. Nothing if still moving (caller loops).</summary>
        Private Async Function SettledAsync(session As GameSession, ct As CancellationToken) As Task(Of ScreenRead)
            Dim cw = session.Cw
            Dim a As Integer()
            Using f = Await ViewCapture.FullAsync(cw)
                a = LoginScreens.Signature(f)
            End Using
            Await Task.Delay(LoginPlan.SettleGapMs, ct)
            Using f = Await ViewCapture.FullAsync(cw)
                If f Is Nothing Then Return Nothing
                Dim r = Await Task.Run(Function() LoginScreens.Classify(f))
                If r.Kind = ScreenKind.InGame Then Return r   ' the world moves; the HUD is enough
                If LoginScreens.Difference(a, LoginScreens.Signature(f)) > LoginPlan.SettleMaxDiff Then Return Nothing
                Return r
            End Using
        End Function

        Private Async Function ReadAsync(session As GameSession) As Task(Of ScreenRead)
            Using f = Await ViewCapture.FullAsync(session.Cw)
                If f Is Nothing Then Return New ScreenRead With {.Kind = ScreenKind.Blank}
                Return Await Task.Run(Function() LoginScreens.Classify(f))
            End Using
        End Function

        Private Async Function WaitScreenAsync(session As GameSession, ct As CancellationToken, timeoutMs As Integer, kinds As ScreenKind()) As Task(Of ScreenRead)
            Dim sw = Stopwatch.StartNew()
            Dim last As ScreenKind = CType(-1, ScreenKind)
            While sw.ElapsedMilliseconds < timeoutMs
                ct.ThrowIfCancellationRequested()
                Dim r = Await ReadAsync(session)
                If kinds.Contains(r.Kind) Then Return r
                If r.Kind <> last Then
                    BotLog.Add("giris", $"{session.Number}. görünüm: ekran {r.Kind} ({Clip(r.Detail)})")
                    last = r.Kind
                End If
                Await Task.Delay(700, ct)
            End While
            Return Nothing
        End Function

        ' ---------------- helpers ----------------

        Private Sub Status(session As GameSession, st As SessionState, text As String)
            If session.State = st AndAlso session.StatusText = text Then Return
            session.SetStatus(st, text)
        End Sub

        Private Function Fail(session As GameSession, reason As String) As Boolean
            session.SetStatus(SessionState.Failed, reason)
            BotLog.Warn("giris", $"{session.Number}. görünüm: giriş olmadı — {reason}")
            Return False
        End Function

        Private Function Done(session As GameSession, how As String) As Boolean
            session.SetStatus(SessionState.InGame, "Oyunda")
            BotLog.Add("giris", $"{session.Number}. görünüm: giriş tamam ({how})")
            Return True
        End Function

        Private Function Clip(s As String) As String
            s = If(s, "").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
            Return If(s.Length > 80, s.Substring(0, 80) & "…", s)
        End Function

    End Module

End Namespace
