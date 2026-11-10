Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Threading
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Native
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Services.Vpn

    Public Enum VpnState
        Off
        Connecting
        Connected
        Failed
    End Enum

    ''' <summary>
    ''' One VPN (Windows' own PPTP, no install, no admin) shared by every account marked "VPN ile gir". The PC's own traffic
    ''' stays direct; a local SOCKS5 bridge (127.0.0.1) sends only the marked views' traffic out through the VPN interface —
    ''' those views get <see cref="ProxyAddress"/> as GameSession.ProxyServer.
    '''
    ''' Flow of <see cref="ConnectAsync"/>: settings → own phonebook (VpnPhonebook) → RasDial (async, polled, 30 s) → VPN IP
    ''' (RasGetProjectionInfo) → its interface → route guard (the PC's default route must NOT be the VPN) → bridge pinned to the
    ''' interface → exit-IP check (through the bridge vs direct; must differ) → Connected. A watchdog checks the link every
    ''' 5 s; when it drops the bridge refuses everything (no direct fallback) and State = Failed. The bridge keeps its port across
    ''' reconnects (views built on it work again after a reconnect); <see cref="Disconnect"/> stops it.
    ''' Credentials go only into RasDial's parameter block — never to a command line, the log or the phonebook file.
    '''
    ''' PROXY MODE (<see cref="Mode"/> = Proxy; VPNBook stopped accepting PPTP): no RAS at all. The same bridge stays the only thing
    ''' the views talk to; its outbound leg becomes an <see cref="UpstreamProxyEgress"/> (SOCKS5 with optional RFC 1929 user / password,
    ''' or HTTP CONNECT with Basic auth) and target host names go to that proxy. Connect = settings check → bridge → exit-IP check
    ''' (through the bridge vs direct, must differ; wrong credentials are named) → Connected. The watchdog opens a TCP connection to
    ''' the proxy every 5 s; two misses in a row = LinkLost (bridge refuses, no direct fallback). Proxy credentials never reach a log.
    ''' </summary>
    Public Module VpnManager

        Public Property Server As String = ""
        Public Property UserName As String = ""
        Public Property Password As String = ""

        Public Property Mode As VpnMode = VpnMode.Pptp
        Public Property ProxyType As ProxyKind = ProxyKind.Socks5
        Public Property ProxyHost As String = ""
        Public Property ProxyPort As Integer = 0
        Public Property ProxyUser As String = ""
        Public Property ProxyPassword As String = ""

        ''' <summary>Exit-IP probes (test hook: the self-test replaces them so no internet host is contacted).</summary>
        Friend ViaProbe As Func(Of Integer, CancellationToken, Task(Of IPAddress)) = AddressOf ExitIpProbe.ViaBridgeAsync
        Friend DirectProbe As Func(Of CancellationToken, Task(Of IPAddress)) = AddressOf ExitIpProbe.DirectAsync

        ''' <summary>Raised (any thread) when State / StatusText changes.</summary>
        Public Event Changed()

        Private ReadOnly _gate As New SemaphoreSlim(1, 1)
        Private ReadOnly _sync As New Object()
        Private _state As VpnState = VpnState.Off
        Private _status As String = "VPN kapalı"
        Private _proxy As String = ""
        Private _hconn As IntPtr = IntPtr.Zero
        Private _bridge As SocksBridge
        Private _watch As System.Threading.Timer
        Private _connectedServer As String = ""
        Private _egress As VpnEgress
        Private _upstream As UpstreamProxyEgress
        Private _proxyMisses As Integer
        Private _watchBusy As Integer
        Private _connectCts As CancellationTokenSource

        Private Const DialTimeoutMs As Integer = 30000
        ''' <summary>How long "invalid handle" without a callback error still means "not registered yet".</summary>
        Private Const DialNoHandleGraceMs As Integer = 5000
        ''' <summary>Watchdog period (test hook: the self-test shortens it).</summary>
        Friend WatchMs As Integer = 5000
        ''' <summary>Route-guard probe target: only a route lookup (GetBestInterfaceEx), no packet is sent.</summary>
        Private ReadOnly RouteProbe As IPAddress = IPAddress.Parse("1.1.1.1")

        Public ReadOnly Property State As VpnState
            Get
                Return _state
            End Get
        End Property

        Public ReadOnly Property StatusText As String
            Get
                Return _status
            End Get
        End Property

        ''' <summary>"socks5://127.0.0.1:port" while Connected, otherwise "".</summary>
        Public ReadOnly Property ProxyAddress As String
            Get
                Return If(_state = VpnState.Connected, _proxy, "")
            End Get
        End Property

        Public Sub LoadSettings()
            Dim s = VpnStore.Load()
            Server = s.Server
            UserName = s.UserName
            Password = s.Password
            Mode = s.Mode
            ProxyType = s.ProxyType
            ProxyHost = s.ProxyHost
            ProxyPort = s.ProxyPort
            ProxyUser = s.ProxyUser
            ProxyPassword = s.ProxyPassword
        End Sub

        Public Function SaveSettings() As Boolean
            Return VpnStore.Save(New VpnSettings With {
                .Mode = Mode, .Server = If(Server, "").Trim(), .UserName = If(UserName, "").Trim(), .Password = If(Password, ""),
                .ProxyType = ProxyType, .ProxyHost = If(ProxyHost, "").Trim(), .ProxyPort = ProxyPort,
                .ProxyUser = If(ProxyUser, "").Trim(), .ProxyPassword = If(ProxyPassword, "")})
        End Function

        ''' <summary>Connects (or returns True at once if already connected). False + StatusText on failure.</summary>
        Public Async Function ConnectAsync(ct As CancellationToken) As Task(Of Boolean)
            If IsUp() Then Return True
            Await _gate.WaitAsync(ct).ConfigureAwait(False)
            Dim mine As CancellationTokenSource = Nothing
            Try
                If IsUp() Then Return True
                ' Disconnect ("Kes" while connecting) cancels this attempt through its own token.
                mine = CancellationTokenSource.CreateLinkedTokenSource(ct)
                Volatile.Write(_connectCts, mine)
                Try
                    Dim ok = Await ConnectCoreAsync(mine.Token).ConfigureAwait(False)
                    If Not AbortedBy(mine, ct) Then Return ok
                Catch ex As Exception When AbortedBy(mine, ct)
                End Try
                ' Aborted by Disconnect: nothing the attempt set up may stay (bridge / dialled line), and it is not a failure.
                TearDown()
                SetState(VpnState.Off, "VPN kapalı")
                BotLog.Add("vpn", "Bağlanma kesildi (Kes) — VPN kapalı")
                Return False
            Finally
                Interlocked.CompareExchange(_connectCts, Nothing, mine)
                mine?.Dispose()
                _gate.Release()
            End Try
        End Function

        Private Function AbortedBy(mine As CancellationTokenSource, ct As CancellationToken) As Boolean
            Return mine IsNot Nothing AndAlso mine.IsCancellationRequested AndAlso Not ct.IsCancellationRequested
        End Function

        ''' <summary>Stops the bridge and hangs up. Call it when the program closes (safe to call any time, any thread).</summary>
        Public Sub Disconnect()
            Dim wasUp = _state <> VpnState.Off
            Dim pending = Volatile.Read(_connectCts)
            If pending IsNot Nothing Then
                Try
                    pending.Cancel()
                Catch
                    ' Attempt already finished (disposed).
                End Try
            End If
            TearDown()
            SetState(VpnState.Off, "VPN kapalı")
            If wasUp Then BotLog.Add("vpn", "VPN kapatıldı")
        End Sub

        ''' <summary>Watchdog off, bridge stopped, own line hung up, link fields cleared (State untouched).</summary>
        Private Sub TearDown()
            StopWatch()
            Dim b = _bridge
            _bridge = Nothing
            If b IsNot Nothing Then
                b.Egress = Nothing
                b.Stop()
            End If
            HangUpOwn()
            _egress = Nothing
            _upstream = Nothing
            _connectedServer = ""
        End Sub

        ' ---------------------------------------------------------------------------------------------------------------

        Private Function IsUp() As Boolean
            Return _state = VpnState.Connected AndAlso _bridge IsNot Nothing AndAlso _bridge.Egress IsNot Nothing AndAlso
                   String.Equals(_connectedServer, CurrentKey(), StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>What the current settings would connect to (memory only, never logged): a change = reconnect.</summary>
        Private Function CurrentKey() As String
            If Mode = VpnMode.Proxy Then
                Dim h As String
                Using sha = System.Security.Cryptography.SHA256.Create()
                    h = BitConverter.ToString(sha.ComputeHash(Text.Encoding.UTF8.GetBytes(If(ProxyUser, "").Trim() & ChrW(0) & If(ProxyPassword, ""))))
                End Using
                Return $"proxy|{VpnStore.ProxyTypeText(ProxyType)}|{If(ProxyHost, "").Trim()}|{ProxyPort}|{h}"
            End If
            Return "pptp|" & If(Server, "").Trim()
        End Function

        ''' <summary>Proxy mode: "" if usable, else the reason (Turkish). Values are never part of the text.</summary>
        Friend Function ProxyProblem(host As String, port As Integer, user As String, pass As String) As String
            Dim h = If(host, "").Trim()
            If h = "" Then Return "Proxy adresi girilmedi — Ayarlar'dan proxy sağlayıcınızın verdiği adresi ve portu girin."
            Dim lit As IPAddress = Nothing
            Dim isV4 = IPAddress.TryParse(h, lit) AndAlso lit.AddressFamily = AddressFamily.InterNetwork AndAlso h.Count(Function(c) c = "."c) = 3
            If h.Length > 253 OrElse Not (isV4 OrElse SocksBridge.ValidHost(h)) Then
                Return "Proxy adresi geçersiz — yalnız ad ya da IP yazın (ör. 203.0.113.5); başına socks5:// / http:// koymayın, portu ayrı kutuya yazın."
            End If
            If port < 1 OrElse port > 65535 Then Return "Proxy portu geçersiz — 1 ile 65535 arasında bir sayı girin."
            Dim u = If(user, "").Trim(), p = If(pass, "")
            If (u = "") <> (p = "") Then Return "Proxy kullanıcı adı ile şifresi birlikte girilmeli (proxy kimlik istemiyorsa ikisini de boş bırakın)."
            If Text.Encoding.UTF8.GetByteCount(u) > 255 OrElse Text.Encoding.UTF8.GetByteCount(p) > 255 Then
                Return "Proxy kullanıcı adı ya da şifresi çok uzun (en çok 255 bayt)."
            End If
            Return ""
        End Function

        ''' <summary>"" if usable, else the reason (Turkish).</summary>
        Friend Function SettingsProblem(server As String, user As String, pass As String) As String
            Dim s = If(server, "").Trim()
            If s = "" Then Return "VPN sunucusu girilmedi — Ayarlar'dan bir VPNBook sunucusu girin (ör. us16.vpnbook.com)."
            If s.Length > 128 OrElse Not s.All(Function(c) (c < ChrW(128) AndAlso Char.IsLetterOrDigit(c)) OrElse c = "."c OrElse c = "-"c) Then
                Return "VPN sunucu adı geçersiz — yalnız ad yazın (ör. us16.vpnbook.com), başına http:// ya da boşluk koymayın."
            End If
            If If(user, "").Trim() = "" OrElse If(pass, "") = "" Then
                Return "VPN kullanıcı adı / şifresi girilmedi — VPNBook sayfasındaki güncel bilgileri Ayarlar'a girin."
            End If
            Return ""
        End Function

        Private Async Function ConnectCoreAsync(ct As CancellationToken) As Task(Of Boolean)
            If Mode = VpnMode.Proxy Then Return Await ConnectProxyAsync(ct).ConfigureAwait(False)
            Dim srv = If(Server, "").Trim()
            Dim problem = SettingsProblem(srv, UserName, Password)
            If problem <> "" Then Return Fail(problem, hangUp:=False)

            ' a previous link (other server, dropped, or left over from a crash) goes first
            StopWatch()
            If _bridge IsNot Nothing Then _bridge.Egress = Nothing
            HangUpOwn()

            SetState(VpnState.Connecting, $"VPN'e bağlanılıyor — {srv}")
            BotLog.Add("vpn", $"VPN'e bağlanılıyor — sunucu {srv}")

            Dim pbk As String
            Try
                pbk = VpnPhonebook.Write(srv)
            Catch ex As Exception
                BotLog.Fail("vpn", "VPN girdisi yazılamadı", ex)
                Return Fail("Programın VPN girdisi yazılamadı: " & ex.Message, hangUp:=False)
            End Try

            ' --- dial (asynchronous RasDial, state polled) ---
            Dim h As IntPtr
            Dim rc = RasNative.DialAsync(pbk, VpnPhonebook.EntryName, UserName.Trim(), Password, h)
            _hconn = h
            If rc <> 0 Then Return Fail(VpnErrors.Describe(rc), code:=rc)
            Dim t0 = Environment.TickCount
            Do
                If ct.IsCancellationRequested Then Return Fail("VPN bağlantısı iptal edildi.")
                Dim st, er As Integer
                Dim src = RasNative.GetStatus(h, st, er)
                If src = RasNative.ERROR_INVALID_HANDLE Then
                    ' Two meanings (measured 10.11.2026): right after RasDial returns, RAS has not registered the
                    ' connection yet (6 at 0 ms, callback not called) — keep polling; after a failure Windows frees the
                    ' handle and the real code is only in the callback.
                    Dim cb = RasNative.LastDialError()
                    If cb <> 0 Then
                        BotLog.Add("vpn", $"VPN araması düştü — Windows'un bildirdiği hata {cb}, son adım {RasNative.LastDialState()}")
                        Return Fail(VpnErrors.Describe(cb), code:=cb)
                    End If
                    If Environment.TickCount - t0 > DialNoHandleGraceMs Then
                        BotLog.Add("vpn", $"VPN araması {DialNoHandleGraceMs \ 1000} sn içinde başlamadı — son adım {RasNative.LastDialState()}")
                        Return Fail("VPN araması Windows tarafından sebep bildirilmeden kapatıldı. Sunucuya ulaşılamamış olabilir; başka bir sunucu deneyin.", code:=src)
                    End If
                    Await Task.Delay(100).ConfigureAwait(False)
                    Continue Do
                End If
                If src <> 0 Then Return Fail(VpnErrors.Describe(src), code:=src)
                If er <> 0 Then Return Fail(VpnErrors.Describe(er), code:=er)
                If st = RasNative.RASCS_Connected Then Exit Do
                If st = RasNative.RASCS_Disconnected Then Return Fail("VPN bağlantısı kurulamadan kapandı.")
                If Environment.TickCount - t0 > DialTimeoutMs Then
                    Return Fail("VPN 30 sn içinde bağlanamadı — sunucu yanıt vermiyor ya da PPTP engelli olabilir. Başka bir sunucu deneyin.")
                End If
                Await Task.Delay(200).ConfigureAwait(False)
            Loop
            BotLog.Add("vpn", $"VPN hattı kuruldu ({(Environment.TickCount - t0) \ 1000} sn) — sunucu {srv}")

            ' --- the VPN's address and interface ---
            Dim ipText = "", serverIp = ""
            Dim prc = RasNative.GetPppIp(h, ipText, serverIp)
            Dim vpnIp As IPAddress = Nothing
            If prc <> 0 OrElse Not IPAddress.TryParse(ipText, vpnIp) Then
                Return Fail("VPN bağlandı ama IP adresi okunamadı" & If(prc <> 0, $" ({prc})", "") & ".")
            End If
            Dim eg As VpnEgress = Nothing
            Dim t1 = Environment.TickCount
            Do
                eg = FindEgress(vpnIp)
                If eg IsNot Nothing OrElse Environment.TickCount - t1 > 4000 Then Exit Do
                Await Task.Delay(250).ConfigureAwait(False)
            Loop
            If eg Is Nothing Then Return Fail($"VPN bağlandı ama Windows'ta arayüzü bulunamadı (IP {vpnIp}).")
            BotLog.Add("vpn", $"VPN arayüzü: {eg} · DNS {If(eg.DnsServers.Count = 0, "yok", String.Join(", ", eg.DnsServers))}")

            ' --- route guard: the PC's own traffic must NOT have moved to the VPN ---
            Dim best = NetNative.BestInterfaceFor(RouteProbe)
            If best = eg.InterfaceIndex Then
                BotLog.Warn("vpn", "Bilgisayarın varsayılan yolu VPN'e geçmiş — güvenlik için VPN kapatılıyor")
                Return Fail("Bilgisayarın tüm trafiği VPN'e geçti (beklenmiyordu) — güvenlik için VPN kapatıldı. Bu durumu bildirin.")
            End If

            ' --- bridge ---
            If _bridge Is Nothing Then
                _bridge = New SocksBridge()
                Try
                    _bridge.Start()
                Catch ex As Exception
                    _bridge = Nothing
                    Return Fail("VPN köprüsü açılamadı: " & ex.Message)
                End Try
            End If
            _bridge.Egress = eg
            _egress = eg
            _upstream = Nothing

            ' --- exit IP: through the bridge vs direct ---
            Dim viaIp As IPAddress = Nothing, directIp As IPAddress = Nothing
            Dim viaErr = "", directErr = ""
            Dim viaT = ViaProbe(_bridge.Port, ct)
            Dim dirT = DirectProbe(ct)
            Try
                viaIp = Await viaT.ConfigureAwait(False)
            Catch ex As Exception
                viaErr = Inner(ex)
            End Try
            Try
                directIp = Await dirT.ConfigureAwait(False)
            Catch ex As Exception
                directErr = Inner(ex)
            End Try
            If viaIp Is Nothing Then
                Return Fail("VPN bağlandı ama köprüden internete çıkılamadı" & If(viaErr <> "", ": " & viaErr, "") & ".")
            End If
            If directIp IsNot Nothing AndAlso directIp.Equals(viaIp) Then
                BotLog.Warn("vpn", $"VPN çıkış IP'si doğrudan IP ile aynı ({viaIp}) — VPN kapatılıyor")
                Return Fail($"VPN çıkış IP'si doğrudan IP ile aynı ({viaIp}) — trafik VPN'den çıkmıyor, VPN kapatıldı.")
            End If

            Dim directText = If(directIp IsNot Nothing, directIp.ToString(), "ölçülemedi")
            _proxy = $"socks5://127.0.0.1:{_bridge.Port}"
            _connectedServer = CurrentKey()
            SetState(VpnState.Connected, $"VPN bağlı — {srv} · çıkış IP'si {viaIp} (doğrudan: {directText})")
            BotLog.Add("vpn", $"VPN çıkış IP'si {viaIp} (doğrudan: {directText}) — köprü 127.0.0.1:{_bridge.Port}")
            If directIp Is Nothing Then BotLog.Warn("vpn", "Doğrudan çıkış IP'si ölçülemedi" & If(directErr <> "", $" ({directErr})", "") & " — VPN'den çıkış karşılaştırılamadı")
            StartWatch()
            Return True
        End Function

        ''' <summary>Proxy mode connect: settings → bridge with the upstream egress → exit-IP check → Connected.</summary>
        Private Async Function ConnectProxyAsync(ct As CancellationToken) As Task(Of Boolean)
            Dim host = If(ProxyHost, "").Trim()
            Dim problem = ProxyProblem(host, ProxyPort, ProxyUser, ProxyPassword)
            If problem <> "" Then Return Fail(problem, hangUp:=False)

            ' a previous link (PPTP or another proxy) goes first
            StopWatch()
            If _bridge IsNot Nothing Then _bridge.Egress = Nothing
            HangUpOwn()
            _egress = Nothing
            _upstream = Nothing

            Dim up As New UpstreamProxyEgress(ProxyType, host, ProxyPort, If(ProxyUser, "").Trim(), If(ProxyPassword, ""))
            SetState(VpnState.Connecting, $"Proxy'ye bağlanılıyor — {up}")
            BotLog.Add("vpn", $"Proxy'ye bağlanılıyor — {up}" & If(up.HasCredentials, " (kimlikli)", " (kimliksiz)"))

            If _bridge Is Nothing Then
                _bridge = New SocksBridge()
                Try
                    _bridge.Start()
                Catch ex As Exception
                    _bridge = Nothing
                    Return Fail("Proxy köprüsü açılamadı: " & ex.Message)
                End Try
            End If
            _bridge.Egress = up
            _upstream = up

            Dim viaIp As IPAddress = Nothing, directIp As IPAddress = Nothing
            Dim viaErr = "", directErr = ""
            Dim viaT = ViaProbe(_bridge.Port, ct)
            Dim dirT = DirectProbe(ct)
            Try
                viaIp = Await viaT.ConfigureAwait(False)
            Catch ex As Exception
                viaErr = Inner(ex)
            End Try
            Try
                directIp = Await dirT.ConfigureAwait(False)
            Catch ex As Exception
                directErr = Inner(ex)
            End Try
            If viaIp Is Nothing Then Return Fail(ProxyFailText(up, viaErr))
            If directIp IsNot Nothing AndAlso directIp.Equals(viaIp) Then
                BotLog.Warn("vpn", $"Proxy çıkış IP'si doğrudan IP ile aynı ({viaIp}) — proxy kapatılıyor")
                Return Fail($"Proxy çıkış IP'si doğrudan IP ile aynı ({viaIp}) — trafik proxy'den çıkmıyor (proxy doğrudan bağlanıyor olabilir), bağlantı kapatıldı.")
            End If

            Dim directText = If(directIp IsNot Nothing, directIp.ToString(), "ölçülemedi")
            _proxy = $"socks5://127.0.0.1:{_bridge.Port}"
            _connectedServer = CurrentKey()
            Interlocked.Exchange(_proxyMisses, 0)
            SetState(VpnState.Connected, $"Proxy bağlı — {up} · çıkış IP'si {viaIp} (doğrudan: {directText})")
            BotLog.Add("vpn", $"Proxy bağlı — {up} · çıkış IP'si {viaIp} (doğrudan: {directText}) — köprü 127.0.0.1:{_bridge.Port}")
            If directIp Is Nothing Then BotLog.Warn("vpn", "Doğrudan çıkış IP'si ölçülemedi" & If(directErr <> "", $" ({directErr})", "") & " — proxy'den çıkış karşılaştırılamadı")
            StartWatch()
            Return True
        End Function

        ''' <summary>Why the exit-IP check through the proxy failed, in words the user can act on.</summary>
        Friend Function ProxyFailText(up As UpstreamProxyEgress, err As String) As String
            Select Case up.LastProblem
                Case UpstreamProblem.AuthFailed
                    Return "Proxy kullanıcı adı ya da şifresi yanlış — proxy sağlayıcınızın verdiği güncel bilgileri Ayarlar'a girin."
                Case UpstreamProblem.AuthRequired
                    Return "Proxy kullanıcı adı / şifre istiyor — Ayarlar'a proxy sağlayıcınızın verdiği bilgileri girin."
                Case UpstreamProblem.Unreachable
                    Return $"Proxy sunucusuna bağlanılamadı ({up.Host}:{up.Port}" & If(up.LastDetail <> "", " · " & up.LastDetail, "") &
                           ") — adres / port yanlış ya da proxy kapalı olabilir."
                Case UpstreamProblem.Protocol
                    Return $"Proxy'nin yanıtı tanınmadı ({up.LastDetail}) — proxy türü ({UpstreamProxyEgress.KindText(up.Kind)}) doğru mu? SOCKS5 / HTTP seçimini kontrol edin."
                Case UpstreamProblem.Target
                    Return $"Proxy bağlandı ama internete çıkamadı ({up.LastDetail})."
                Case Else
                    Return "Proxy'den internete çıkılamadı" & If(err <> "", ": " & err, "") & "."
            End Select
        End Function

        Private Function Inner(ex As Exception) As String
            Dim e = ex
            Do While (TypeOf e Is AggregateException OrElse TypeOf e Is IO.IOException) AndAlso e.InnerException IsNot Nothing
                e = e.InnerException
            Loop
            Dim se = TryCast(e, SocketException)
            If se IsNot Nothing Then
                If se.SocketErrorCode = SocketError.NetworkUnreachable Then Return "VPN arayüzünde yol yok (10051)"
                Return $"{se.SocketErrorCode} ({se.ErrorCode})"
            End If
            Return e.Message
        End Function

        ''' <summary>The interface carrying <paramref name="ip"/> (IPv4 index + its DNS servers), Nothing if not (yet) there.</summary>
        Friend Function FindEgress(ip As IPAddress) As VpnEgress
            For Each ni In NetworkInterface.GetAllNetworkInterfaces()
                Try
                    Dim props = ni.GetIPProperties()
                    If Not props.UnicastAddresses.Any(Function(u) u.Address.Equals(ip)) Then Continue For
                    Dim v4 = props.GetIPv4Properties()
                    If v4 Is Nothing Then Continue For
                    Dim dns = props.DnsAddresses.Where(Function(a) a.AddressFamily = AddressFamily.InterNetwork).ToList()
                    Return New VpnEgress(ip, v4.Index, dns)
                Catch
                End Try
            Next
            Return Nothing
        End Function

        ' --- watchdog ---------------------------------------------------------------------------------------------------

        Private Sub StartWatch()
            StopWatch()
            _watch = New System.Threading.Timer(AddressOf WatchTick, Nothing, WatchMs, WatchMs)
        End Sub

        Private Sub StopWatch()
            Dim w = Interlocked.Exchange(_watch, Nothing)
            w?.Dispose()
        End Sub

        Private Sub WatchTick(o As Object)
            ' Timer thread: an unhandled exception here would end the whole process.
            Try
                WatchCheck()
            Catch ex As Exception
                BotLog.FailOnce("vpn.watch", ex)
            End Try
        End Sub

        Private Sub WatchCheck()
            If _state <> VpnState.Connected Then Return
            Dim upw = _upstream
            If upw IsNot Nothing Then
                If Interlocked.Exchange(_watchBusy, 1) = 1 Then Return
                Try
                    If upw.ReachableAsync(4000).GetAwaiter().GetResult() Then
                        Interlocked.Exchange(_proxyMisses, 0)
                        Return
                    End If
                    If Interlocked.Increment(_proxyMisses) < 2 Then
                        BotLog.Add("vpn", $"Proxy'ye ulaşılamadı ({upw}) — bir kez daha bakılacak")
                        Return
                    End If
                Finally
                    Interlocked.Exchange(_watchBusy, 0)
                End Try
                If _upstream Is upw Then LinkLost($"proxy sunucusuna ulaşılamıyor — {upw}")
                Return
            End If
            Dim reason = ""
            Dim st, er As Integer
            Dim rc = RasNative.GetStatus(_hconn, st, er)
            If rc <> 0 Then
                reason = "hat kapandı"
            ElseIf er <> 0 Then
                reason = VpnErrors.Describe(er)
            ElseIf st <> RasNative.RASCS_Connected Then
                reason = "hat bağlı değil"
            Else
                Dim eg = _egress
                If eg Is Nothing OrElse FindEgress(eg.LocalIp) Is Nothing Then reason = "VPN arayüzü kayboldu"
            End If
            If reason = "" Then Return
            LinkLost(reason)
        End Sub

        Private Sub LinkLost(reason As String)
            SyncLock _sync
                If _state <> VpnState.Connected Then Return
                StopWatch()
                If _bridge IsNot Nothing Then _bridge.Egress = Nothing     ' refuse everything from now on, close open tunnels
                HangUpOwn()
                _egress = Nothing
                _upstream = Nothing
                _connectedServer = ""
            End SyncLock
            Dim what = If(Mode = VpnMode.Proxy, "Proxy", "VPN")
            BotLog.Warn("vpn", $"{what} bağlantısı koptu ({reason}) — VPN'li hesaplar bağlantısız (VPN'siz çıkış yok)")
            SetState(VpnState.Failed, $"{what} bağlantısı koptu ({reason}) — VPN'li hesaplar bağlantısız kaldı. Yeniden bağlanın.")
        End Sub

        ' --- helpers -------------------------------------------------------------------------------------------------------

        ''' <summary>Hangs up our handle and any leftover connection of our entry (e.g. after a crash).</summary>
        Private Sub HangUpOwn()
            Dim h = _hconn
            _hconn = IntPtr.Zero
            Try
                If h <> IntPtr.Zero Then RasNative.HangUp(h)
                Dim list As List(Of KeyValuePair(Of String, IntPtr)) = Nothing
                If RasNative.EnumConnections(list) = 0 Then
                    For Each c In list
                        If String.Equals(c.Key, VpnPhonebook.EntryName, StringComparison.OrdinalIgnoreCase) Then RasNative.HangUp(c.Value)
                    Next
                End If
            Catch ex As Exception
                BotLog.FailOnce("vpn.hangup", ex)
            End Try
        End Sub

        Private Function Fail(text As String, Optional hangUp As Boolean = True, Optional code As Integer = 0) As Boolean
            If hangUp Then
                If _bridge IsNot Nothing Then _bridge.Egress = Nothing
                HangUpOwn()
                _egress = Nothing
                _upstream = Nothing
                _connectedServer = ""
            End If
            BotLog.Warn("vpn", If(Mode = VpnMode.Proxy, "Proxy bağlanamadı: ", "VPN bağlanamadı: ") & text)
            SetState(VpnState.Failed, text)
            Return False
        End Function

        Private Sub SetState(s As VpnState, text As String)
            _state = s
            _status = text
            Try
                RaiseEvent Changed()
            Catch ex As Exception
                BotLog.FailOnce("vpn.changed", ex)
            End Try
        End Sub

    End Module

End Namespace
