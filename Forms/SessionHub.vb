Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.WinForms
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Engine.Follow
Imports RedSquirrel.WebKoMulti.Engine.Mirror
Imports RedSquirrel.WebKoMulti.Services.Vpn
Imports RedSquirrel.WebKoMulti.Services.Logging

''' <summary>
''' The UI's single owner of the accounts, their game sessions, the leader and the follow controller. Every view of the main
''' window and the game windows read from here; every change made here is written to the log (passwords never are).
''' One session per ACCOUNT ROW (enabled or not): list position = GameSession.Index = FollowController.LeaderIndex, and the
''' profile folder exe\Sessions\N follows the row. Removing / moving rows is therefore allowed only while no game view is open.
''' Events are raised on the UI thread.
''' </summary>
Public NotInheritable Class SessionHub
    Implements IDisposable

    ''' <summary>Fixed view size: the game draws 1:1 and the screen readers measure pixels of this size.</summary>
    Public Shared ReadOnly ViewSize As New Size(1280, 800)

    Private ReadOnly _ui As Control
    Private ReadOnly _cts As New Dictionary(Of GameSession, CancellationTokenSource)()
    Private _leader As Integer = -1
    Private _follow As FollowController
    Private _followRunning As Boolean
    Private _mirror As InputMirror
    Private _mirrorDemoOn As Boolean
    Private _mirrorMouse As Boolean
    Private _vpnTask As Task(Of Boolean)
    Private _vpnHooked As Boolean
    Private _demoVpn As VpnState = VpnState.Off
    Private _demoVpnText As String = ""
    Private _mirrorStatus As String = ""
    Private _mirrorLast As String = ""

    Public ReadOnly Property Accounts As New List(Of GameAccount)()
    Public ReadOnly Property Sessions As New List(Of GameSession)()

    ''' <summary>Preview / render mode: nothing is written to disk or to the log.</summary>
    Public Property Demo As Boolean

    ''' <summary>Asked before a view's browser is created: the view must sit in a shown window by then.</summary>
    Public Property BeforeInit As Action

    ''' <summary>Rows added / removed / moved / edited (labels, slot, on-off).</summary>
    Public Event AccountsChanged As EventHandler
    ''' <summary>The session list was rebuilt (new views).</summary>
    Public Event SessionsRebuilt As EventHandler
    Public Event LeaderChanged As EventHandler
    Public Event SessionChanged(session As GameSession)
    Public Event FollowStatus(index As Integer, text As String)
    Public Event FollowRunningChanged As EventHandler
    Public Event DirtyChanged As EventHandler
    ''' <summary>Mirroring switched on / off, or its status text changed.</summary>
    Public Event MirrorChanged As EventHandler
    ''' <summary>One mirrored action (text = MirrorLast).</summary>
    Public Event MirrorAction As EventHandler
    ''' <summary>VpnManager's state / text changed (UI thread).</summary>
    Public Event VpnChanged As EventHandler

    Private _dirty As Boolean

    Public Sub New(ui As Control)
        _ui = ui
    End Sub

    ' ---------------------------------------------------------------- accounts

    Public Sub LoadAccounts()
        Accounts.Clear()
        If Not Demo Then
            Try
                Accounts.AddRange(Services.Accounts.AccountStore.Load())
            Catch ex As Exception
                BotLog.Fail("hesap", "Hesaplar okunamadı", ex)
            End Try
        End If
        _leader = UiPrefs.LeaderIndex(Demo)
        If Not Demo Then
            _mirrorMouse = UiPrefs.MirrorMouse()
            Try
                VpnManager.LoadSettings()
            Catch ex As Exception
                BotLog.Fail("vpn", "VPN ayarları okunamadı", ex)
            End Try
            If Not _vpnHooked Then
                AddHandler VpnManager.Changed, AddressOf OnVpnChanged
                _vpnHooked = True
            End If
        End If
        If _leader >= Accounts.Count Then _leader = If(Accounts.Count > 0, 0, -1)
        If _leader < 0 AndAlso Accounts.Count > 0 Then _leader = 0
        Log($"Hesaplar yüklendi: {Accounts.Count} hesap" & If(_leader >= 0, $", lider {Describe(_leader)}", ""))
        RebuildSessions()
        SetDirty(False)
    End Sub

    ''' <summary>Preview data (render): labelled rows without real IDs.</summary>
    Public Sub LoadDemo(list As IEnumerable(Of GameAccount), leader As Integer)
        Accounts.Clear()
        Accounts.AddRange(list)
        _leader = leader
        RebuildSessions()
    End Sub

    Public Function Save() As Boolean
        If Demo Then Return True
        Dim ok As Boolean
        Try
            ok = Services.Accounts.AccountStore.Save(Accounts)
        Catch ex As Exception
            BotLog.Fail("hesap", "Hesaplar kaydedilemedi", ex)
            Return False
        End Try
        UiPrefs.SaveLeaderIndex(_leader)
        If ok Then
            BotLog.Add("hesap", $"Hesaplar kaydedildi: {Accounts.Count} hesap")
            SetDirty(False)
        Else
            BotLog.Warn("hesap", "Hesaplar kaydedilemedi (kayıt yeri yanıt vermedi)")
        End If
        Return ok
    End Function

    Public ReadOnly Property Dirty As Boolean
        Get
            Return _dirty
        End Get
    End Property

    Public Sub MarkEdited()
        SetDirty(True)
        RaiseEvent AccountsChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub SetDirty(value As Boolean)
        If _dirty = value Then Return
        _dirty = value
        RaiseEvent DirtyChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>"2. hesap (Okçu)".</summary>
    Public Function Describe(index As Integer) As String
        If index < 0 OrElse index >= Accounts.Count Then Return "—"
        Dim lbl As String = Accounts(index).Label.Trim()
        Return $"{index + 1}. hesap" & If(lbl = "", "", $" ({lbl})")
    End Function

    ''' <summary>True while any game view has a browser (a game may be running in it).</summary>
    Public ReadOnly Property AnyViewOpen As Boolean
        Get
            Return Sessions.Any(Function(s) s.Ready OrElse _cts.ContainsKey(s))
        End Get
    End Property

    Public Function AddAccount() As Integer
        Dim acc As New GameAccount With {.Label = "", .CharacterSlot = 1, .Enabled = True}
        Accounts.Add(acc)
        Dim i As Integer = Accounts.Count - 1
        Sessions.Add(NewSession(i, acc))   ' appended: the other views (and their games) stay
        If _leader < 0 Then _leader = i
        Log($"{i + 1}. hesap eklendi")
        SetDirty(True)
        RaiseEvent SessionsRebuilt(Me, EventArgs.Empty)
        RaiseEvent AccountsChanged(Me, EventArgs.Empty)
        If _leader = i Then RaiseEvent LeaderChanged(Me, EventArgs.Empty)
        Return i
    End Function

    ''' <summary>"" = done, otherwise why not (shown to the user).</summary>
    Public Function RemoveAccount(index As Integer) As String
        If index < 0 OrElse index >= Accounts.Count Then Return "Önce bir hesap seçin."
        If AnyViewOpen Then Return "Hesap silmek için önce Oyunlar › Oyunları kapat."
        Dim what As String = Describe(index)
        Accounts.RemoveAt(index)
        If _leader = index Then
            _leader = If(Accounts.Count > 0, 0, -1)
        ElseIf _leader > index Then
            _leader -= 1
        End If
        Log($"{what} silindi" & If(_leader >= 0, $" · lider {Describe(_leader)}", ""))
        RebuildSessions()
        SetDirty(True)
        RaiseEvent AccountsChanged(Me, EventArgs.Empty)
        RaiseEvent LeaderChanged(Me, EventArgs.Empty)
        Return ""
    End Function

    Public Function MoveAccount(index As Integer, delta As Integer) As String
        Dim target As Integer = index + delta
        If index < 0 OrElse index >= Accounts.Count OrElse target < 0 OrElse target >= Accounts.Count Then Return "-"
        If AnyViewOpen Then Return "Sırayı değiştirmek için önce Oyunlar › Oyunları kapat (her sıra kendi tarayıcı profilini kullanır)."
        Dim acc As GameAccount = Accounts(index)
        Accounts.RemoveAt(index)
        Accounts.Insert(target, acc)
        If _leader = index Then
            _leader = target
        ElseIf _leader = target Then
            _leader = index
        End If
        Log($"{index + 1}. hesap {target + 1}. sıraya taşındı")
        RebuildSessions()
        SetDirty(True)
        RaiseEvent AccountsChanged(Me, EventArgs.Empty)
        RaiseEvent LeaderChanged(Me, EventArgs.Empty)
        Return ""
    End Function

    ''' <summary>Disposes every view (their games close) and builds fresh ones. Stops login and follow first.</summary>
    Public Async Function CloseGamesAsync() As Task
        StopAll()
        If _followRunning Then Await StopFollowAsync()
        Dim open As Integer = Sessions.Where(Function(s) s.Ready).Count()
        RebuildSessions()
        Log($"Oyunlar kapatıldı ({open} görünüm)", "oturum")
    End Function

    ''' <summary>
    ''' Closes every view and deletes ALL browser data (exe\Sessions: cookies, cache, local storage of every profile) so
    ''' each account starts like a fresh browser. Accounts, passwords, VPN and program settings (..\Settings) stay.
    ''' The one-time game options are forgotten too (a fresh profile may have lost them). "" = done, else the reason.
    ''' </summary>
    Public Async Function ClearBrowserDataAsync() As Task(Of String)
        If Demo Then Return ""
        Await CloseGamesAsync()
        Dim root As String = IO.Path.Combine(AppPaths.ExeDir(), "Sessions")
        If Not IO.Directory.Exists(root) Then
            Log("Tarayıcı verileri zaten boş", "oturum")
            Return ""
        End If
        Dim sizeMb As Long = 0
        Try
            sizeMb = New IO.DirectoryInfo(root).EnumerateFiles("*", IO.SearchOption.AllDirectories).Sum(Function(f) f.Length) \ (1024 * 1024)
        Catch
        End Try
        ' WebView2's browser processes exit a moment after the views are disposed; the folders stay locked until then.
        Dim lastError As String = ""
        Dim t0 As Integer = Environment.TickCount
        Do
            Try
                IO.Directory.Delete(root, True)
                lastError = ""
                Exit Do
            Catch ex As Exception When TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException
                lastError = ex.Message
            End Try
            If Environment.TickCount - t0 > 15000 Then Exit Do
            Await Task.Delay(500)
        Loop
        If lastError <> "" AndAlso IO.Directory.Exists(root) Then
            Warn($"Tarayıcı verileri tam silinemedi: {lastError}", "oturum")
            Return "Bazı tarayıcı dosyaları silinemedi (hâlâ kullanımda). Programı kapatıp açın ve yeniden deneyin."
        End If
        Dim forgotten As Integer = 0
        For Each a As GameAccount In Accounts
            If String.IsNullOrWhiteSpace(a.UserId) Then Continue For
            Try
                Engine.Login.GameSetup.Forget(a.UserId)
                forgotten += 1
            Catch
            End Try
        Next
        Log($"Bütün tarayıcı verileri silindi ({sizeMb} MB, {(Environment.TickCount - t0) \ 1000} sn) — her hesap ilk girişte temiz tarayıcıyla açılacak, oyun ayarları yeniden uygulanacak ({forgotten} hesap)", "oturum")
        Return ""
    End Function

    Private Sub RebuildSessions()
        For Each s As GameSession In Sessions
            RemoveHandler s.Changed, AddressOf OnSessionChanged
            Try
                s.View.Dispose()
            Catch
            End Try
        Next
        Sessions.Clear()
        For i As Integer = 0 To Accounts.Count - 1
            Sessions.Add(NewSession(i, Accounts(i)))
        Next
        RecreateMirror()
        RaiseEvent SessionsRebuilt(Me, EventArgs.Empty)
    End Sub

    Private Function NewSession(index As Integer, acc As GameAccount) As GameSession
        Dim view As New WebView2 With {.Location = Point.Empty, .Size = ViewSize, .DefaultBackgroundColor = Color.Black}
        Dim s As New GameSession(index, view, acc)
        If Not acc.Enabled Then s.StatusText = "Kapalı"
        AddHandler s.Changed, AddressOf OnSessionChanged
        Return s
    End Function

    Private Sub OnSessionChanged(s As GameSession)
        OnUi(Sub() RaiseEvent SessionChanged(s))
    End Sub

    ' ---------------------------------------------------------------- leader

    Public ReadOnly Property Leader As Integer
        Get
            Return _leader
        End Get
    End Property

    Public ReadOnly Property LeaderSession As GameSession
        Get
            Return If(_leader >= 0 AndAlso _leader < Sessions.Count, Sessions(_leader), Nothing)
        End Get
    End Property

    Public Sub SetLeader(index As Integer)
        If index < 0 OrElse index >= Accounts.Count OrElse index = _leader Then Return
        Dim was As String = Describe(_leader)
        _leader = index
        Log($"Lider: {Describe(index)}" & If(was <> "—", $" (önceki {was})", "") & If(_followRunning, " — takip yeni lidere döndü", ""))
        If _follow IsNot Nothing Then _follow.LeaderIndex = index
        If Not Demo Then UiPrefs.SaveLeaderIndex(index)
        RaiseEvent LeaderChanged(Me, EventArgs.Empty)
        RaiseEvent AccountsChanged(Me, EventArgs.Empty)
    End Sub

    ' ---------------------------------------------------------------- login

    Public Function IsBusy(s As GameSession) As Boolean
        Return _cts.ContainsKey(s)
    End Function

    Public Async Function StartAllAsync() As Task
        Dim todo As List(Of GameSession) = Sessions.Where(Function(s) s.Account.Enabled AndAlso Not IsBusy(s) AndAlso s.State <> SessionState.InGame).ToList()
        If todo.Count = 0 Then
            Log("Hepsini başlat: başlatılacak hesap yok (hepsi oyunda, çalışıyor ya da kapalı)", "oturum")
            Return
        End If
        Log($"Hepsini başlat: {todo.Count} hesap — " & String.Join(", ", todo.Select(Function(s) CStr(s.Number))), "oturum")
        Await Task.WhenAll(todo.Select(Function(s) StartAsync(s, relogin:=False)))
    End Function

    ''' <summary>Opens the view's browser (once) and runs the login chain. relogin = the user asked to log in again.</summary>
    Public Async Function StartAsync(s As GameSession, relogin As Boolean, Optional manual As Boolean = False) As Task
        If IsBusy(s) Then Return
        Dim cts As New CancellationTokenSource()
        _cts(s) = cts
        If manual Then
            Log($"{Describe(s.Index)}: elle giriş — tarayıcı açılıyor, girişi siz yapacaksınız", "oturum")
        Else
            Log($"{Describe(s.Index)}: " & If(relogin, "yeniden giriş başladı", "giriş başladı") & $" · karakter {s.Account.CharacterSlot}", "oturum")
        End If
        Try
            ' VPN first: a marked account never goes in without it (the user asked for the VPN).
            Dim desired As String = ""
            If s.Account.UseVpn Then
                s.SetStatus(SessionState.Loading, "VPN bağlanıyor")
                If Not Await EnsureVpnAsync(cts.Token) Then
                    s.SetStatus(SessionState.Failed, "VPN bağlanamadı: " & VpnText)
                    Warn($"{Describe(s.Index)}: başlatılmadı — VPN bağlanamadı", "oturum")
                    Return
                End If
                desired = VpnManager.ProxyAddress
                If desired = "" Then
                    s.SetStatus(SessionState.Failed, "VPN bağlanamadı: köprü adresi yok")
                    Warn($"{Describe(s.Index)}: başlatılmadı — VPN köprü adresi yok", "oturum")
                    Return
                End If
            End If
            ' A WebView2 can't change its proxy: an open view made with another one is rebuilt (its game closes; it logs in again).
            If s.Ready AndAlso s.ActiveProxy <> desired Then
                Dim fresh As GameSession = Await RecreateViewAsync(s)
                _cts.Remove(s)
                s = fresh
                _cts(s) = cts
            End If
            s.ProxyServer = desired
            If Not s.Ready Then
                s.SetStatus(SessionState.Loading, "Tarayıcı açılıyor")
                BeforeInit?.Invoke()
                Await s.InitAsync()
            End If
            cts.Token.ThrowIfCancellationRequested()
            Dim ok As Boolean = If(manual, Await Engine.Login.WebLogin.RunManualAsync(s, cts.Token), Await Engine.Login.WebLogin.RunAsync(s, cts.Token))
            If ok Then
                Log($"{Describe(s.Index)}: oyuna girildi" & If(manual, " (elle)", ""), "oturum")
            Else
                Warn($"{Describe(s.Index)}: giriş tamamlanamadı — {s.StatusText}", "oturum")
            End If
        Catch ex As OperationCanceledException
            If s.State <> SessionState.InGame Then s.SetStatus(SessionState.NotStarted, "Durduruldu")
            Log($"{Describe(s.Index)}: giriş durduruldu", "oturum")
        Catch ex As Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException
            s.SetStatus(SessionState.Failed, "WebView2 Runtime yüklü değil — kurup programı yeniden açın")
            Warn($"{Describe(s.Index)}: başlatılmadı — WebView2 Runtime bulunamadı", "oturum")
        Catch ex As Exception When cts.IsCancellationRequested
            ' Stopped / games closed while a browser call was running: the call fails with its own error — it is a stop.
            If s.State <> SessionState.InGame Then s.SetStatus(SessionState.NotStarted, "Durduruldu")
            Log($"{Describe(s.Index)}: giriş durduruldu", "oturum")
        Catch ex As Exception
            s.SetStatus(SessionState.Failed, "Hata: " & ex.Message)
            If Not Demo Then BotLog.Fail("oturum", $"{Describe(s.Index)}: giriş hatası", ex)
        Finally
            _cts.Remove(s)
            cts.Dispose()
            OnUi(Sub() RaiseEvent SessionChanged(s))
        End Try
    End Function

    ''' <summary>The in-game steps again (close windows, mute, close PMs, game options — forced), only for a session in the game.</summary>
    Public Async Function RunInGameStepsAsync(s As GameSession) As Task
        If IsBusy(s) OrElse s.State <> SessionState.InGame OrElse Not InGameSteps.Available Then Return
        Dim cts As New CancellationTokenSource()
        _cts(s) = cts
        Log($"{Describe(s.Index)}: oyun içi adımlar elle yeniden çalıştırıldı", "oturum")
        OnUi(Sub() RaiseEvent SessionChanged(s))
        Try
            Dim ok As Boolean = Await InGameSteps.Run(s, cts.Token)
            If ok Then
                Log($"{Describe(s.Index)}: oyun içi adımlar tamamlandı", "oturum")
            Else
                Warn($"{Describe(s.Index)}: oyun içi adımlar tamamlanamadı — {s.StatusText}", "oturum")
            End If
        Catch ex As OperationCanceledException
            Log($"{Describe(s.Index)}: oyun içi adımlar durduruldu", "oturum")
        Catch ex As Exception
            If Not Demo Then BotLog.Fail("oturum", $"{Describe(s.Index)}: oyun içi adımlar hata verdi", ex)
        Finally
            _cts.Remove(s)
            cts.Dispose()
            OnUi(Sub() RaiseEvent SessionChanged(s))
        End Try
    End Function

    ''' <summary>Forgets the account's one-time game-options mark: the options are applied again on its next login.</summary>
    Public Function ForgetGameSetup(index As Integer) As String
        If index < 0 OrElse index >= Accounts.Count Then Return "Önce bir hesap seçin."
        Dim id As String = Accounts(index).UserId
        If String.IsNullOrWhiteSpace(id) Then Return "Bu hesabın oyun ID'si boş."
        If Demo Then Return ""
        Engine.Login.GameSetup.Forget(id)
        Log($"{Describe(index)}: oyun ayarları bir sonraki girişte yeniden uygulanacak")
        Return ""
    End Function

    ' ---------------------------------------------------------------- VPN (engine: Services.Vpn.VpnManager)

    ''' <summary>One connection for every marked account: concurrent starts wait for the same attempt.</summary>
    Private Async Function EnsureVpnAsync(ct As CancellationToken) As Task(Of Boolean)
        If Demo Then Return _demoVpn = VpnState.Connected
        If VpnManager.State = VpnState.Connected Then Return True
        If _vpnTask Is Nothing OrElse _vpnTask.IsCompleted Then _vpnTask = VpnManager.ConnectAsync(CancellationToken.None)
        Dim t As Task(Of Boolean) = _vpnTask
        Await Task.WhenAny(t, Task.Delay(Timeout.Infinite, ct))
        ct.ThrowIfCancellationRequested()
        Return Await t
    End Function

    ''' <summary>VPN tab's Bağlan.</summary>
    Public Function ConnectVpnAsync() As Task(Of Boolean)
        Return EnsureVpnAsync(CancellationToken.None)
    End Function

    Public ReadOnly Property VpnNow As VpnState
        Get
            Return If(Demo, _demoVpn, VpnManager.State)
        End Get
    End Property

    Public ReadOnly Property VpnText As String
        Get
            Return If(Demo, _demoVpnText, VpnManager.StatusText)
        End Get
    End Property

    ''' <summary>The open view runs with another proxy than its account now asks for (applied on the next start / re-login).</summary>
    Public Function ProxyMismatch(s As GameSession) As Boolean
        If Not s.Ready Then Return False
        If Not s.Account.UseVpn Then Return s.ActiveProxy <> ""
        If s.ActiveProxy = "" Then Return True
        Return VpnNow = VpnState.Connected AndAlso Not Demo AndAlso VpnManager.ProxyAddress <> "" AndAlso s.ActiveProxy <> VpnManager.ProxyAddress
    End Function

    Friend Sub DemoVpn(state As VpnState, text As String)
        _demoVpn = state
        _demoVpnText = text
        RaiseEvent VpnChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnVpnChanged()
        OnUi(Sub() RaiseEvent VpnChanged(Me, EventArgs.Empty))
    End Sub

    ''' <summary>New view for the same row (same profile folder): the old browser is closed and awaited first.</summary>
    Private Async Function RecreateViewAsync(old As GameSession) As Task(Of GameSession)
        Dim pid As Integer = 0
        Try
            If old.Ready Then pid = CInt(old.Cw.BrowserProcessId)
        Catch
        End Try
        RemoveHandler old.Changed, AddressOf OnSessionChanged
        Try
            old.View.Dispose()
        Catch
        End Try
        If pid > 0 Then
            Try
                Dim p As Process = Process.GetProcessById(pid)
                Dim waited As Integer = 0
                While Not p.HasExited AndAlso waited < 5000
                    Await Task.Delay(100)
                    waited += 100
                End While
            Catch
                ' Already gone.
            End Try
        End If
        Dim fresh As GameSession = NewSession(old.Index, old.Account)
        Sessions(old.Index) = fresh
        Log($"{Describe(old.Index)}: VPN ayarı değişti — oyun görünümü yeniden kuruldu", "oturum")
        RaiseEvent SessionsRebuilt(Me, EventArgs.Empty)
        Return fresh
    End Function

    Public Sub StopOne(s As GameSession)
        Dim cts As CancellationTokenSource = Nothing
        If _cts.TryGetValue(s, cts) Then
            Log($"{Describe(s.Index)}: durdur istendi", "oturum")
            cts.Cancel()
        End If
    End Sub

    Public Sub StopAll()
        If _cts.Count = 0 Then Return
        Log($"Durdur: {_cts.Count} giriş durduruluyor", "oturum")
        For Each c As CancellationTokenSource In _cts.Values.ToList()
            c.Cancel()
        Next
    End Sub

    Public ReadOnly Property BusyCount As Integer
        Get
            Return _cts.Count
        End Get
    End Property

    ' ---------------------------------------------------------------- follow

    Public ReadOnly Property FollowRunning As Boolean
        Get
            Return _followRunning
        End Get
    End Property

    Public Async Function StartFollowAsync() As Task
        If _followRunning Then Return
        If _leader < 0 Then
            Warn("Takip başlatılamadı: lider seçilmedi", "takip")
            RaiseEvent FollowStatus(-1, "Önce bir lider seçin")
            Return
        End If
        _follow = New FollowController(Sessions) With {.LeaderIndex = _leader}
        AddHandler _follow.Status, AddressOf OnFollowStatus
        _followRunning = True
        Dim followers As Integer = Sessions.Where(Function(s) s.Index <> _leader AndAlso s.Account.Enabled).Count()
        Log($"Takip başladı: lider {Describe(_leader)}, {followers} takipçi", "takip")
        RaiseEvent FollowRunningChanged(Me, EventArgs.Empty)
        Dim failed As Exception = Nothing
        Try
            Await _follow.StartAsync()
        Catch ex As Exception
            failed = ex
        End Try
        If failed IsNot Nothing Then
            If Not Demo Then BotLog.Fail("takip", "Takip başlatılamadı", failed)
            Await StopFollowAsync()
        End If
    End Function

    Public Async Function StopFollowAsync() As Task
        If Not _followRunning Then Return
        Dim f As FollowController = _follow
        _followRunning = False
        _follow = Nothing
        Try
            If f IsNot Nothing Then Await f.StopAsync()
        Catch ex As Exception
            If Not Demo Then BotLog.Fail("takip", "Takip durdurulurken hata", ex)
        End Try
        If f IsNot Nothing Then RemoveHandler f.Status, AddressOf OnFollowStatus
        Log("Takip durduruldu", "takip")
        RaiseEvent FollowRunningChanged(Me, EventArgs.Empty)
    End Function

    Private Sub OnFollowStatus(index As Integer, text As String)
        OnUi(Sub() RaiseEvent FollowStatus(index, text))
    End Sub

    ' ---------------------------------------------------------------- helpers

    Private Sub OnUi(a As Action)
        If _ui.IsDisposed Then Return
        If _ui.InvokeRequired Then
            If _ui.IsHandleCreated Then _ui.BeginInvoke(a)
        Else
            a()
        End If
    End Sub

    ''' <summary>topic = the log's category: hesap (accounts / leader) · oturum (start / login / stop) · takip · pencere.</summary>
    Public Sub Log(message As String, Optional topic As String = "hesap")
        If Not Demo Then BotLog.Add(topic, message)
    End Sub

    Public Sub Warn(message As String, Optional topic As String = "hesap")
        If Not Demo Then BotLog.Warn(topic, message)
    End Sub

    ' ---------------------------------------------------------------- input mirror (engine: Engine.Mirror.InputMirror)

    ''' <summary>A new mirror for a new session list (views disposed / recreated). It starts OFF.</summary>
    Private Sub RecreateMirror()
        Dim wasOn As Boolean = MirrorOn
        DisposeMirror()
        If Demo Then Return
        _mirror = New InputMirror(Sessions, Function() _leader)
        AddHandler _mirror.Status, AddressOf OnMirrorStatus
        AddHandler _mirror.Mirrored, AddressOf OnMirrored
        ApplyMirrorMode()
        _mirrorLast = ""
        If wasOn Then _mirrorStatus = "Oyunlar yeniden kuruldu — yansıtma kapandı"
        RaiseEvent MirrorChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub DisposeMirror()
        Dim m As InputMirror = _mirror
        _mirror = Nothing
        If m Is Nothing Then Return
        RemoveHandler m.Status, AddressOf OnMirrorStatus
        RemoveHandler m.Mirrored, AddressOf OnMirrored
        Try
            m.Enabled = False
            m.Dispose()
        Catch
        End Try
    End Sub

    ''' <summary>On / off as the ENGINE reports it (what is really happening), not what was asked.</summary>
    Public ReadOnly Property MirrorOn As Boolean
        Get
            If Demo Then Return _mirrorDemoOn
            Return _mirror IsNot Nothing AndAlso _mirror.Enabled
        End Get
    End Property

    Public ReadOnly Property MirrorStatus As String
        Get
            Return _mirrorStatus
        End Get
    End Property

    Public ReadOnly Property MirrorLast As String
        Get
            Return _mirrorLast
        End Get
    End Property

    ''' <summary>Followers the leader's input reaches now (in game, not the leader).</summary>
    Public ReadOnly Property MirrorTargets As Integer
        Get
            Return Sessions.Where(Function(x) x.Index <> _leader AndAlso x.State = SessionState.InGame).Count()
        End Get
    End Property

    ''' <summary>Switch from the leader window or the Takip tab (one source). The engine writes the log; follow is left to it.</summary>
    Public Sub SetMirror(value As Boolean)
        If Demo Then
            _mirrorDemoOn = value
        ElseIf _mirror IsNot Nothing Then
            Try
                _mirror.Enabled = value
            Catch ex As Exception
                _mirrorStatus = "Yansıtma açılamadı: " & ex.Message
            End Try
        End If
        RaiseEvent MirrorChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>Mirror mode: False = keyboard only (default), True = keyboard + mouse. Kept in ..\Settings\multi-ui.json.</summary>
    Public ReadOnly Property MirrorMouse As Boolean
        Get
            Return _mirrorMouse
        End Get
    End Property

    ''' <summary>Picker in the leader window / the Takip tab (one source). Works while mirroring is on; the engine logs it.</summary>
    Public Sub SetMirrorMouse(withMouse As Boolean)
        If withMouse = _mirrorMouse Then Return
        _mirrorMouse = withMouse
        If Not Demo Then UiPrefs.SaveMirrorMouse(withMouse)
        ApplyMirrorMode()
        RaiseEvent MirrorChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub ApplyMirrorMode()
        If _mirror Is Nothing Then Return
        Try
            _mirror.Mode = If(_mirrorMouse, MirrorMode.KeyboardAndMouse, MirrorMode.KeyboardOnly)
        Catch ex As Exception
            _mirrorStatus = "Yansıtma kipi değiştirilemedi: " & ex.Message
        End Try
    End Sub

    ''' <summary>Render preview: a mirror that is on, with its texts.</summary>
    Friend Sub DemoMirror(isOn As Boolean, status As String, last As String, Optional withMouse As Boolean = False)
        _mirrorMouse = withMouse
        _mirrorDemoOn = isOn
        _mirrorStatus = status
        _mirrorLast = last
        RaiseEvent MirrorChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnMirrorStatus(text As String)
        OnUi(Sub()
                 _mirrorStatus = If(text, "")
                 RaiseEvent MirrorChanged(Me, EventArgs.Empty)
             End Sub)
    End Sub

    Private Sub OnMirrored(text As String)
        OnUi(Sub()
                 _mirrorLast = If(text, "")
                 RaiseEvent MirrorAction(Me, EventArgs.Empty)
             End Sub)
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        DisposeMirror()
        If _vpnHooked Then
            RemoveHandler VpnManager.Changed, AddressOf OnVpnChanged
            _vpnHooked = False
        End If
        If Not Demo Then
            Try
                VpnManager.Disconnect()
            Catch ex As Exception
                BotLog.Fail("vpn", "Kapanışta VPN kesilemedi", ex)
            End Try
        End If
        For Each c As CancellationTokenSource In _cts.Values.ToList()
            Try
                c.Cancel()
            Catch
            End Try
        Next
        For Each s As GameSession In Sessions
            Try
                s.View.Dispose()
            Catch
            End Try
        Next
    End Sub

End Class
