Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Namespace Services.Vpn

    ''' <summary>
    ''' Offline self-test of the VPN layer (no VPN, no network, no write to rasphone.pbk): settings file round trip, the
    ''' phonebook entry text (PPTP / MS-CHAPv2 / routing fields) + Windows parsing it from a TEMP phonebook, RAS error texts,
    ''' DNS packets + a fake loopback DNS server, the SOCKS5 bridge end to end (loopback egress, fake target), the bridge
    ''' REFUSING when the VPN is down, and pinning on an interface without a route. Proxy mode: settings v1/v2, fake loopback SOCKS5
    ''' (with / without RFC 1929 auth) and HTTP CONNECT upstreams, bridge → upstream → echo target, host name handed to the proxy,
    ''' wrong credentials named, VpnManager.ConnectAsync in proxy mode with stubbed exit-IP probes (no internet host is contacted)
    ''' and the watchdog's LinkLost. Last line "SONUC: GECTI n/m" / "DUSTU".
    ''' </summary>
    Public Module VpnSelfTest

        Private _sb As StringBuilder
        Private _pass, _total As Integer

        Private Sub Check(name As String, ok As Boolean, Optional detail As String = "")
            _total += 1
            If ok Then _pass += 1
            _sb.AppendLine($"{If(ok, "OK  ", "FAIL")} {name}{If(detail <> "", " — " & detail, "")}")
        End Sub

        Private Sub Info(text As String)
            _sb.AppendLine("BILGI " & text)
        End Sub

        Public Function Run() As String
            _sb = New StringBuilder()
            _pass = 0 : _total = 0
            Dim dir = Path.Combine(Path.GetTempPath(), "wwkm-vpn-test-" & Guid.NewGuid().ToString("N").Substring(0, 8))
            Directory.CreateDirectory(dir)
            Try
                SettingsTests(dir)
                PhonebookTests(dir)
                ErrorTests()
                RasTests()
                DnsTests()
                BridgeTests()
                PinningTests()
                ProxySettingsTests(dir)
                UpstreamTests()
                ProxyManagerTests()
            Catch ex As Exception
                Check("beklenmeyen istisna", False, ex.GetType().Name & ": " & ex.Message)
            Finally
                VpnStore.PathOverride = Nothing
                VpnPhonebook.PathOverride = Nothing
                VpnDns.ServerOverride = Nothing
                ResetManager()
                Try
                    Directory.Delete(dir, True)
                Catch
                End Try
            End Try
            _sb.AppendLine(If(_pass = _total, $"SONUC: GECTI {_pass}/{_total}", $"SONUC: DUSTU {_pass}/{_total}"))
            Return _sb.ToString()
        End Function

        ' ---- 1. settings ----
        Private Sub SettingsTests(dir As String)
            Dim f = Path.Combine(dir, "multi-vpn.dat")
            VpnStore.PathOverride = f
            Dim s As New VpnSettings With {.Server = "us16.vpnbook.com", .UserName = "ornek-kullanici", .Password = "Ornek-Sifre-ÇğŞ"}
            Check("ayar kaydı", VpnStore.Save(s))
            Dim raw = File.ReadAllBytes(f)
            Check("ayar dosyası WMV1 başlıklı", raw.Length > 4 AndAlso Encoding.ASCII.GetString(raw, 0, 4) = "WMV1")
            Dim plain = Encoding.UTF8.GetString(raw) & Encoding.Unicode.GetString(raw)
            Check("şifre / kullanıcı adı dosyada düz yok", Not plain.Contains("Ornek-Sifre") AndAlso Not plain.Contains("ornek-kullanici"))
            Dim back = VpnStore.Load()
            Check("ayar gidip-gelme", back.Server = s.Server AndAlso back.UserName = s.UserName AndAlso back.Password = s.Password)
            Check("tmp dosyası kalmadı", Not File.Exists(f & ".tmp"))
            File.WriteAllBytes(f, Encoding.ASCII.GetBytes("WMV1bozuk-veri"))
            Dim bad = VpnStore.Load()
            Check("bozuk dosya → boş ayar + kenara alındı", bad.Server = "" AndAlso Not File.Exists(f) AndAlso Directory.GetFiles(dir, "multi-vpn.dat.bozuk-*").Length = 1)
            VpnStore.PathOverride = Path.Combine(dir, "yok.dat")
            Check("dosya yokken boş ayar", VpnStore.Load().Server = "")

            Check("ayar: sunucu boş → sebep", VpnManager.SettingsProblem("", "a", "b").Contains("sunucusu girilmedi"))
            Check("ayar: http:// → geçersiz", VpnManager.SettingsProblem("http://us16.vpnbook.com", "a", "b").Contains("geçersiz"))
            Check("ayar: boşluklu ad → geçersiz", VpnManager.SettingsProblem("us16 vpnbook.com", "a", "b").Contains("geçersiz"))
            Check("ayar: şifre boş → sebep", VpnManager.SettingsProblem("us16.vpnbook.com", "a", "").Contains("şifresi girilmedi"))
            Check("ayar: geçerli", VpnManager.SettingsProblem(" de220.vpnbook.com ", "a", "b") = "")
        End Sub

        ' ---- 2. phonebook ----
        Private Sub PhonebookTests(dir As String)
            Dim t = VpnPhonebook.BuildEntry("ca149.vpnbook.com", "VPN4-0")
            Check("girdi adı", t.StartsWith("[" & VpnPhonebook.EntryName & "]"))
            Check("PPTP yalnız (VpnStrategy=1)", VpnPhonebook.Field(t, "VpnStrategy") = "1")
            Check("MS-CHAPv2 yalnız (AuthRestrictions=512)", VpnPhonebook.Field(t, "AuthRestrictions") = "512")
            Check("şifreleme zorunlu (DataEncryption=256)", VpnPhonebook.Field(t, "DataEncryption") = "256")
            Check("sunucu PhoneNumber'da", VpnPhonebook.Field(t, "PhoneNumber") = "ca149.vpnbook.com")
            Check("yol: uzak ağ geçidi yalnız yüksek metrikli (IpPrioritizeRemote=1, metrik 9000)",
                  VpnPhonebook.Field(t, "IpPrioritizeRemote") = "1" AndAlso VpnPhonebook.Field(t, "IpInterfaceMetric") = CStr(VpnPhonebook.InterfaceMetric) AndAlso VpnPhonebook.InterfaceMetric >= 5000)
            Check("yol: sınıf tabanlı yol yok", VpnPhonebook.Field(t, "DisableClassBasedDefaultRoute") = "1")
            Check("IPv6 VPN'de yok (ExcludedProtocols=8, Ipv6PrioritizeRemote=0)", VpnPhonebook.Field(t, "ExcludedProtocols") = "8" AndAlso VpnPhonebook.Field(t, "Ipv6PrioritizeRemote") = "0")
            Check("dosya/yazıcı paylaşımı VPN'e kapalı", VpnPhonebook.Field(t, "ShareMsFilePrint") = "0" AndAlso VpnPhonebook.Field(t, "ms_server") = "0" AndAlso VpnPhonebook.Field(t, "ms_msclient") = "0")
            Check("kimlik saklanmaz (CacheCredentials=0, UseRasCredentials=0)", VpnPhonebook.Field(t, "CacheCredentials") = "0" AndAlso VpnPhonebook.Field(t, "UseRasCredentials") = "0")
            Check("PPTP aygıtı + port", VpnPhonebook.Field(t, "Device") = "WAN Miniport (PPTP)" AndAlso VpnPhonebook.Field(t, "Port") = "VPN4-0")
            Dim port = VpnPhonebook.PptpPortName()
            Check("PPTP port adı bu bilgisayarda bulundu", port.StartsWith("VPN") AndAlso port.EndsWith("-0"), port)

            Dim pbk = Path.Combine(dir, "test.pbk")
            VpnPhonebook.PathOverride = pbk
            Dim written = VpnPhonebook.Write("us178.vpnbook.com")
            Check("telefon defteri GEÇİCİ yola yazıldı", String.Equals(written, pbk, StringComparison.OrdinalIgnoreCase) AndAlso File.Exists(pbk))
            Dim content = File.ReadAllText(pbk)
            Check("defter metninde kimlik yok", Not content.Contains("Ornek-Sifre") AndAlso Not content.Contains("ornek-kullanici"))
            Dim v = Core.Native.RasNative.ValidateEntryName(pbk, VpnPhonebook.EntryName)
            Check("Windows girdiyi okuyor (RasValidateEntryName = 183 'var')", v = Core.Native.RasNative.ERROR_ALREADY_EXISTS, $"rc {v}")
            Dim v2 = Core.Native.RasNative.ValidateEntryName(pbk, "Olmayan Girdi")
            Check("olmayan girdi 'var' sayılmıyor", v2 <> Core.Native.RasNative.ERROR_ALREADY_EXISTS, $"rc {v2}")
            VpnPhonebook.Write("de20.vpnbook.com")
            Check("sunucu değişince girdi güncelleniyor", VpnPhonebook.Field(File.ReadAllText(pbk), "PhoneNumber") = "de20.vpnbook.com")
            Dim userPbk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\Network\Connections\Pbk\rasphone.pbk")
            Check("kullanıcının rasphone.pbk'sinde girdi yok", Not File.Exists(userPbk) OrElse Not File.ReadAllText(userPbk).Contains(VpnPhonebook.EntryName))
        End Sub

        ' ---- 3. error texts ----
        Private Sub ErrorTests()
            Dim e691 = VpnErrors.Describe(691)
            Check("691 → şifre yanlış + 1-2 hafta + VPNBook", e691.Contains("şifre yanlış") AndAlso e691.Contains("1-2 hafta") AndAlso e691.Contains("VPNBook"))
            Check("800 → ulaşılamadı + PPTP", VpnErrors.Describe(800).Contains("bağlanılamadı") AndAlso VpnErrors.Describe(800).Contains("PPTP"))
            Check("807 → koptu + PPTP", VpnErrors.Describe(807).Contains("koptu") AndAlso VpnErrors.Describe(807).Contains("PPTP"))
            Check("806 → GRE engelli", VpnErrors.Describe(806).Contains("GRE"))
            Check("868 → sunucu adı", VpnErrors.Describe(868).Contains("Sunucu adı"))
            Dim unk = VpnErrors.Describe(9876)
            Check("bilinmeyen kod numarasıyla", unk.Contains("9876"), unk)
            Dim w = VpnErrors.Describe(678)
            Check("678 → zaman aşımı metni", w.Contains("yanıt vermedi"))
        End Sub

        ' ---- 4. RAS structures accepted by Windows (no dial) ----
        Private Sub RasTests()
            Dim list As List(Of KeyValuePair(Of String, IntPtr)) = Nothing
            Dim rc = Core.Native.RasNative.EnumConnections(list)
            Check("RasEnumConnections yapı boyutu kabul", rc = 0, $"rc {rc}, {If(list Is Nothing, 0, list.Count)} bağlantı")
            Dim st, er As Integer
            Dim g = Core.Native.RasNative.GetStatus(New IntPtr(&H7FFF1234), st, er)
            Check("geçersiz tutamaç → 6 (bağlantı yok sayılır)", g = Core.Native.RasNative.ERROR_INVALID_HANDLE, $"rc {g}")
            Check("RasGetErrorString çalışıyor", Core.Native.RasNative.ErrorText(691) <> "")
            Check("RASDIALPARAMS boyutu (x64, 0x601, paket 4) 2120", Runtime.InteropServices.Marshal.SizeOf(GetType(Core.Native.RasNative.RASDIALPARAMS)) = 2120,
                  CStr(Runtime.InteropServices.Marshal.SizeOf(GetType(Core.Native.RasNative.RASDIALPARAMS))))
            Check("RASCONN boyutu (0x600) 1388", Runtime.InteropServices.Marshal.SizeOf(GetType(Core.Native.RasNative.RASCONN)) = 1388)
            Check("RASPPPIP boyutu 80", Runtime.InteropServices.Marshal.SizeOf(GetType(Core.Native.RasNative.RASPPPIP)) = 80)
            ' RasDial on an entry that does not exist in a TEMP phonebook: no server is contacted; 632 would mean "wrong size".
            Dim tmpPbk = VpnPhonebook.FilePath()
            Dim h As IntPtr
            Dim d = Core.Native.RasNative.DialAsync(tmpPbk, "Olmayan Girdi", "x", "y", h)
            If h <> IntPtr.Zero Then Core.Native.RasNative.HangUp(h)
            Check("RasDial parametre yapısını kabul ediyor (olmayan girdi → 623, 632 değil)", d <> 632, $"rc {d}")
        End Sub

        ' ---- 5. DNS ----
        Private Sub DnsTests()
            Dim q = VpnDns.BuildQuery(&H1234, "oyun.ardream.net")
            Check("DNS sorgusu biçimi", q.Length = 12 + 18 + 4 AndAlso q(0) = &H12 AndAlso q(1) = &H34 AndAlso q(2) = 1 AndAlso q(12) = 4)
            Dim resp = VpnDns.BuildAResponse(q, IPAddress.Parse("10.20.30.40"))
            Dim parsed = VpnDns.ParseAResponse(resp, &H1234)
            Check("DNS yanıtı ayrıştırma", parsed.Count = 1 AndAlso parsed(0).ToString() = "10.20.30.40")
            Check("yanlış kimlikli yanıt atılıyor", VpnDns.ParseAResponse(resp, &H1235).Count = 0)
            Dim nx = CType(resp.Clone(), Byte())
            nx(3) = &H83
            Check("NXDOMAIN → boş", VpnDns.ParseAResponse(nx, &H1234).Count = 0)
            Check("kısa paket → boş", VpnDns.ParseAResponse(New Byte() {1, 2, 3}, 1).Count = 0)

            ' fake DNS server on loopback, query through a pinned (loopback) socket
            Using udp As New UdpClient(New IPEndPoint(IPAddress.Loopback, 0))
                Dim ep = CType(udp.Client.LocalEndPoint, IPEndPoint)
                Dim srvTask = Task.Run(Sub()
                                           Dim from As New IPEndPoint(IPAddress.Any, 0)
                                           Dim req = udp.Receive(from)
                                           Dim a = VpnDns.BuildAResponse(req, IPAddress.Parse("10.9.8.7"))
                                           udp.Send(a, a.Length, from)
                                       End Sub)
                VpnDns.ServerOverride = New List(Of IPEndPoint) From {ep}
                Dim before = VpnDns.SystemFallbacks
                Dim r = VpnDns.ResolveAsync("sahte.oyun.test", LoopEgress(), CancellationToken.None).Result
                Check("ad VPN DNS'iyle çözüldü (sahte sunucu)", r IsNot Nothing AndAlso r.ToString() = "10.9.8.7", If(r Is Nothing, "yok", r.ToString()))
                Check("sistem DNS'ine düşülmedi", VpnDns.SystemFallbacks = before)
                srvTask.Wait(2000)
            End Using
            VpnDns.ServerOverride = Nothing
            Dim lit = VpnDns.ResolveAsync("5.6.7.8", LoopEgress(), CancellationToken.None).Result
            Check("IP yazılı hedef sorgusuz", lit IsNot Nothing AndAlso lit.ToString() = "5.6.7.8")
        End Sub

        Private Function LoopEgress() As VpnEgress
            Return New VpnEgress(IPAddress.Loopback, NetworkInterface.LoopbackInterfaceIndex, Nothing)
        End Function

        ' ---- 6. bridge end to end ----
        Private Sub BridgeTests()
            Check("IP_UNICAST_IF değeri ağ bayt sırasında", VpnEgress.UnicastIfValue(1).SequenceEqual(New Byte() {0, 0, 0, 1}) AndAlso
                  VpnEgress.UnicastIfValue(&H102).SequenceEqual(New Byte() {0, 0, 1, 2}))

            Dim target As New TcpListener(IPAddress.Loopback, 0)
            target.Start()
            Dim tPort = CType(target.LocalEndpoint, IPEndPoint).Port
            Dim accepts As Integer = 0
            Dim lastRemote As String = ""
            Dim stopTarget As New CancellationTokenSource()
            Dim echo = Task.Run(Async Function()
                                    Do While Not stopTarget.IsCancellationRequested
                                        Dim c As TcpClient
                                        Try
                                            c = Await target.AcceptTcpClientAsync()
                                        Catch
                                            Exit Do
                                        End Try
                                        Interlocked.Increment(accepts)
                                        lastRemote = c.Client.RemoteEndPoint.ToString()
                                        Dim __ = Task.Run(Async Function()
                                                              Using c
                                                                  Dim s = c.GetStream()
                                                                  Dim buf(1023) As Byte
                                                                  Do
                                                                      Dim n = Await s.ReadAsync(buf, 0, buf.Length)
                                                                      If n <= 0 Then Exit Do
                                                                      Dim outp = Encoding.ASCII.GetBytes("ECHO:" & Encoding.ASCII.GetString(buf, 0, n))
                                                                      Await s.WriteAsync(outp, 0, outp.Length)
                                                                  Loop
                                                              End Using
                                                          End Function)
                                    Loop
                                End Function)

            Dim bridge As New SocksBridge()
            bridge.Start()
            Try
                Check("köprü yalnız 127.0.0.1'de", bridge.Running AndAlso bridge.Port > 0)
                bridge.Egress = LoopEgress()
                bridge.Resolver = Function(h, eg, tok) Task.FromResult(If(h = "hedef.oyun.test", IPAddress.Loopback, CType(Nothing, IPAddress)))

                Dim got = RoundTrip(bridge.Port, "127.0.0.1", tPort, "merhaba")
                Check("SOCKS5 CONNECT (IPv4) uçtan uca", got = "ECHO:merhaba", got)
                Check("hedef bağlantıyı köprünün sabitlenmiş adresinden gördü", lastRemote.StartsWith("127.0.0.1:"), lastRemote)
                Dim got2 = RoundTrip(bridge.Port, "hedef.oyun.test", tPort, "ad")
                Check("SOCKS5 CONNECT (alan adı) uçtan uca", got2 = "ECHO:ad", got2)
                Check("çözülemeyen ad → 4", SocksCode(bridge.Port, "yok.oyun.test", tPort) = 4)

                Dim closed As New TcpListener(IPAddress.Loopback, 0)
                closed.Start()
                Dim cPort = CType(closed.LocalEndpoint, IPEndPoint).Port
                closed.Stop()
                Check("kapalı port → 5 (reddedildi)", SocksCode(bridge.Port, "127.0.0.1", cPort) = 5)
                Check("BIND komutu → 7", RawReply(bridge.Port, New Byte() {5, 2, 0, 1, 127, 0, 0, 1, 0, 80}) = 7)
                Check("IPv6 hedef → 8", RawReply(bridge.Port, New Byte() {5, 1, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 80}) = 8)
                Check("kimlik isteyen istemci → FF (yöntem yok)", MethodReply(bridge.Port, New Byte() {5, 1, 2}) = &HFF)

                ' open tunnel is closed when the VPN drops
                Using c = SocksClient.ConnectAsync(bridge.Port, "127.0.0.1", tPort, CancellationToken.None).Result
                    Dim s = c.GetStream()
                    s.ReadTimeout = 3000
                    bridge.Egress = Nothing
                    Dim closedOk As Boolean
                    Try
                        Dim b(15) As Byte
                        closedOk = s.Read(b, 0, b.Length) <= 0
                    Catch
                        closedOk = True
                    End Try
                    Check("VPN düşünce açık tünel kapatıldı", closedOk)
                End Using

                ' VPN down: refused, nothing dialled
                Dim acc0 = Volatile.Read(accepts)
                Dim code = SocksCode(bridge.Port, "127.0.0.1", tPort)
                Thread.Sleep(200)
                Check("VPN kapalıyken köprü REDDEDİYOR (02)", code = 2, $"kod {code}")
                Check("VPN kapalıyken hedefe hiç bağlanılmadı (sızıntı yok)", Volatile.Read(accepts) = acc0)
                Check("alan adıyla da reddediyor", SocksCode(bridge.Port, "hedef.oyun.test", tPort) = 2)

                bridge.Egress = LoopEgress()
                Check("VPN geri gelince aynı portta yeniden çalışıyor", RoundTrip(bridge.Port, "127.0.0.1", tPort, "geri") = "ECHO:geri")
            Finally
                bridge.Stop()
                stopTarget.Cancel()
                target.Stop()
            End Try
            Dim refusedAfterStop As Boolean
            Try
                Using c As New TcpClient()
                    c.Connect(IPAddress.Loopback, bridge.Port)
                End Using
                refusedAfterStop = False
            Catch
                refusedAfterStop = True
            End Try
            Check("Disconnect sonrası köprü portu kapalı", refusedAfterStop OrElse bridge.Port = 0)

            Check("çıkış IP'si: düz yanıt", Equals(ExitIpProbe.ParseHttpIp("HTTP/1.1 200 OK" & vbCrLf & "Content-Type: text/plain" & vbCrLf & vbCrLf & "203.0.113.7")?.ToString(), "203.0.113.7"))
            Check("çıkış IP'si: chunked yanıt", Equals(ExitIpProbe.ParseHttpIp("HTTP/1.1 200 OK" & vbCrLf & "Transfer-Encoding: chunked" & vbCrLf & vbCrLf & "b" & vbCrLf & "203.0.113.8" & vbCrLf & "0" & vbCrLf & vbCrLf)?.ToString(), "203.0.113.8"))
            Check("çıkış IP'si: 500 → yok", ExitIpProbe.ParseHttpIp("HTTP/1.1 500 X" & vbCrLf & vbCrLf & "1.2.3.4") Is Nothing)
        End Sub

        Private Function RoundTrip(proxyPort As Integer, host As String, port As Integer, msg As String) As String
            Try
                Using c = SocksClient.ConnectAsync(proxyPort, host, port, CancellationToken.None).Result
                    Dim s = c.GetStream()
                    s.ReadTimeout = 3000
                    Dim b = Encoding.ASCII.GetBytes(msg)
                    s.Write(b, 0, b.Length)
                    Dim buf(255) As Byte
                    Dim n = s.Read(buf, 0, buf.Length)
                    Return Encoding.ASCII.GetString(buf, 0, Math.Max(0, n))
                End Using
            Catch ex As Exception
                Return "hata: " & If(ex.InnerException, ex).Message
            End Try
        End Function

        Private Function SocksCode(proxyPort As Integer, host As String, port As Integer) As Integer
            Try
                Using c = SocksClient.ConnectAsync(proxyPort, host, port, CancellationToken.None).Result
                    Return 0
                End Using
            Catch ex As AggregateException
                Dim se = TryCast(ex.InnerException, SocksException)
                Return If(se IsNot Nothing, CInt(se.Code), -1)
            Catch
                Return -1
            End Try
        End Function

        Private Function RawReply(proxyPort As Integer, request As Byte()) As Integer
            Try
                Using c As New TcpClient()
                    c.Connect(IPAddress.Loopback, proxyPort)
                    Dim s = c.GetStream()
                    s.ReadTimeout = 3000
                    s.Write(New Byte() {5, 1, 0}, 0, 3)
                    Dim m(1) As Byte
                    s.Read(m, 0, 2)
                    s.Write(request, 0, request.Length)
                    Dim r(9) As Byte
                    Dim n = s.Read(r, 0, r.Length)
                    Return If(n >= 2, CInt(r(1)), -1)
                End Using
            Catch
                Return -1
            End Try
        End Function

        Private Function MethodReply(proxyPort As Integer, greeting As Byte()) As Integer
            Try
                Using c As New TcpClient()
                    c.Connect(IPAddress.Loopback, proxyPort)
                    Dim s = c.GetStream()
                    s.ReadTimeout = 3000
                    s.Write(greeting, 0, greeting.Length)
                    Dim m(1) As Byte
                    Dim n = s.Read(m, 0, 2)
                    Return If(n = 2, CInt(m(1)), -1)
                End Using
            Catch
                Return -1
            End Try
        End Function

        ' ---- 7. pinning on an interface WITHOUT a route (the split-tunnel case): must fail, never use another line ----
        Private Sub PinningTests()
            Dim cand As VpnEgress = Nothing
            Dim candName = ""
            For Each ni In NetworkInterface.GetAllNetworkInterfaces()
                Try
                    If ni.OperationalStatus <> OperationalStatus.Up OrElse ni.NetworkInterfaceType = NetworkInterfaceType.Loopback Then Continue For
                    Dim p = ni.GetIPProperties()
                    If p.GatewayAddresses.Any(Function(g) g.Address.AddressFamily = AddressFamily.InterNetwork AndAlso Not g.Address.Equals(IPAddress.Any)) Then Continue For
                    Dim ua = p.UnicastAddresses.FirstOrDefault(Function(u) u.Address.AddressFamily = AddressFamily.InterNetwork)
                    If ua Is Nothing Then Continue For
                    cand = New VpnEgress(ua.Address, p.GetIPv4Properties().Index, Nothing)
                    candName = ni.Name
                    Exit For
                Catch
                End Try
            Next
            If cand Is Nothing Then
                Info("yolsuz arayüz yok — sabitleme sınaması atlandı (canlıda çıkış IP'si satırı kanıtlar)")
                Return
            End If
            Dim result = ""
            Using s = cand.NewSocket(SocketType.Stream, ProtocolType.Tcp)
                Try
                    Dim t = s.ConnectAsync(New IPEndPoint(IPAddress.Parse("192.0.2.1"), 9))
                    If Not t.Wait(3000) Then
                        result = "zaman aşımı"
                    Else
                        result = "bağlandı"
                    End If
                Catch ex As AggregateException
                    Dim se = TryCast(ex.InnerException, SocketException)
                    result = If(se IsNot Nothing, se.SocketErrorCode.ToString(), ex.InnerException.Message)
                End Try
            End Using
            Check($"yolsuz arayüze sabitlenen soket başka hatta gitmiyor ({candName})", result = "NetworkUnreachable", result)
        End Sub

        ' ---- 8. proxy settings ----
        Private Const FakeUser As String = "deneme-kullanici"
        Private Const FakePass As String = "Deneme-Sifre-ÇğŞ"

        Private Sub ProxySettingsTests(dir As String)
            Dim f = Path.Combine(dir, "multi-vpn-proxy.dat")
            VpnStore.PathOverride = f
            Dim s As New VpnSettings With {.Mode = VpnMode.Proxy, .Server = "us16.vpnbook.com", .UserName = "a", .Password = "b",
                                           .ProxyType = ProxyKind.Http, .ProxyHost = "proxy.ornek.test", .ProxyPort = 8080,
                                           .ProxyUser = FakeUser, .ProxyPassword = FakePass}
            Check("proxy ayarı kaydı", VpnStore.Save(s))
            Dim raw = File.ReadAllBytes(f)
            Dim plain = Encoding.UTF8.GetString(raw) & Encoding.Unicode.GetString(raw)
            Check("proxy kimliği dosyada düz yok", Not plain.Contains("Deneme-Sifre") AndAlso Not plain.Contains(FakeUser))
            Dim b = VpnStore.Load()
            Check("proxy ayarı gidip-gelme", b.Mode = VpnMode.Proxy AndAlso b.ProxyType = ProxyKind.Http AndAlso b.ProxyHost = s.ProxyHost AndAlso
                  b.ProxyPort = 8080 AndAlso b.ProxyUser = FakeUser AndAlso b.ProxyPassword = FakePass AndAlso b.Server = s.Server)
            Dim sum = VpnStore.Summary(b)
            Check("ayar özeti kip + adres:port, kimlik yok", sum.Contains("proxy") AndAlso sum.Contains("proxy.ornek.test:8080") AndAlso
                  Not sum.Contains(FakeUser) AndAlso Not sum.Contains("Deneme-Sifre"), sum)

            ' a v1 file (before proxy mode): no "mode" → PPTP, proxy fields empty
            Dim v1 = VpnStore.FromJson("{""version"":1,""server"":""de20.vpnbook.com"",""userName"":""u"",""password"":""p""}")
            Check("eski (v1) dosya: kip yok → PPTP", v1.Mode = VpnMode.Pptp AndAlso v1.Server = "de20.vpnbook.com" AndAlso v1.ProxyHost = "" AndAlso
                  v1.ProxyPort = 0 AndAlso v1.ProxyType = ProxyKind.Socks5)
            File.WriteAllBytes(f, VpnStore.Protect("{""version"":1,""server"":""de20.vpnbook.com"",""userName"":""u"",""password"":""p""}"))
            Dim lv1 = VpnStore.Load()
            Check("eski (v1) şifreli dosya okunuyor", lv1.Mode = VpnMode.Pptp AndAlso lv1.Server = "de20.vpnbook.com" AndAlso lv1.Password = "p")
            Dim odd = VpnStore.FromJson("{""mode"":""garip"",""proxyType"":""x"",""proxyPort"":""99999""}")
            Check("bilinmeyen kip / tür / port → varsayılan", odd.Mode = VpnMode.Pptp AndAlso odd.ProxyType = ProxyKind.Socks5 AndAlso odd.ProxyPort = 0)

            Check("proxy: adres boş → sebep", VpnManager.ProxyProblem("", 1080, "", "").Contains("adresi girilmedi"))
            Check("proxy: socks5:// → geçersiz", VpnManager.ProxyProblem("socks5://1.2.3.4", 1080, "", "").Contains("geçersiz"))
            Check("proxy: adres:port tek kutuda → geçersiz", VpnManager.ProxyProblem("1.2.3.4:1080", 1080, "", "").Contains("geçersiz"))
            Check("proxy: port 0 → sebep", VpnManager.ProxyProblem("1.2.3.4", 0, "", "").Contains("portu geçersiz"))
            Check("proxy: port 70000 → sebep", VpnManager.ProxyProblem("1.2.3.4", 70000, "", "").Contains("portu geçersiz"))
            Check("proxy: yalnız kullanıcı adı → sebep", VpnManager.ProxyProblem("1.2.3.4", 1080, "u", "").Contains("birlikte"))
            Check("proxy: geçerli (kimliksiz)", VpnManager.ProxyProblem(" 203.0.113.5 ", 1080, "", "") = "")
            Check("proxy: geçerli (ad + kimlik)", VpnManager.ProxyProblem("proxy.ornek.test", 8080, "u", "p") = "")
            Dim msg = VpnManager.ProxyProblem("1.2.3.4", 1080, FakeUser, "")
            Check("sebep metninde kullanıcı adı yok", Not msg.Contains(FakeUser), msg)
            VpnStore.PathOverride = Nothing
        End Sub

        ' ---- 9. upstream proxies (fake, loopback only) ----
        Private Sub UpstreamTests()
            Check("HTTP durum satırı", UpstreamProxyEgress.HttpStatus("HTTP/1.1 200 Connection established" & vbCrLf) = 200 AndAlso
                  UpstreamProxyEgress.HttpStatus("HTTP/1.0 407 Proxy Authentication Required") = 407 AndAlso UpstreamProxyEgress.HttpStatus("SSH-2.0") = -1)
            Using echo As New EchoTarget(), socksOpen As New FakeUpstream(ProxyKind.Socks5, "", ""),
                  socksAuth As New FakeUpstream(ProxyKind.Socks5, FakeUser, FakePass), httpOpen As New FakeUpstream(ProxyKind.Http, "", ""),
                  httpAuth As New FakeUpstream(ProxyKind.Http, FakeUser, FakePass)
                Dim bridge As New SocksBridge()
                Dim resolverCalls As Integer = 0
                bridge.Resolver = Function(h, eg, tok)
                                      Interlocked.Increment(resolverCalls)
                                      Return Task.FromResult(IPAddress.Loopback)
                                  End Function
                bridge.Start()
                Try
                    ' SOCKS5, no auth
                    Dim up = New UpstreamProxyEgress(ProxyKind.Socks5, "127.0.0.1", socksOpen.Port, "", "")
                    bridge.Egress = up
                    Dim got = RoundTrip(bridge.Port, "hedef.proxy.test", echo.Port, "s5")
                    Check("köprü → SOCKS5 (kimliksiz) → hedef uçtan uca", got = "ECHO:s5", got)
                    Check("hedef ADI proxy'ye gitti (yerel DNS yok)", socksOpen.LastTarget = "hedef.proxy.test:" & echo.Port AndAlso Volatile.Read(resolverCalls) = 0,
                          $"{socksOpen.LastTarget} · çözücü {resolverCalls}")
                    Check("IPv4 hedef de proxy'den", RoundTrip(bridge.Port, "127.0.0.1", echo.Port, "ip") = "ECHO:ip" AndAlso socksOpen.LastTarget = "127.0.0.1:" & echo.Port)
                    Check("proxy kimlik istemiyorsa yöntem 0 önerildi", socksOpen.LastMethods = "0", socksOpen.LastMethods)

                    ' SOCKS5 with RFC 1929
                    bridge.Egress = New UpstreamProxyEgress(ProxyKind.Socks5, "127.0.0.1", socksAuth.Port, FakeUser, FakePass)
                    got = RoundTrip(bridge.Port, "hedef.proxy.test", echo.Port, "s5a")
                    Check("köprü → SOCKS5 (kullanıcı/şifre) → hedef uçtan uca", got = "ECHO:s5a", got)
                    Check("RFC 1929 kimliği proxy'ye ulaştı", socksAuth.AuthOk = 1)
                    Dim wrong = New UpstreamProxyEgress(ProxyKind.Socks5, "127.0.0.1", socksAuth.Port, FakeUser, "yanlis")
                    bridge.Egress = wrong
                    Dim echo0 = echo.Accepts
                    Dim code = SocksCode(bridge.Port, "hedef.proxy.test", echo.Port)
                    Check("SOCKS5 yanlış şifre → reddedildi (02) + AuthFailed", code = 2 AndAlso wrong.LastProblem = UpstreamProblem.AuthFailed, $"kod {code} · {wrong.LastProblem}")
                    Check("yanlış şifrede hedefe gidilmedi", echo.Accepts = echo0)
                    Dim none = New UpstreamProxyEgress(ProxyKind.Socks5, "127.0.0.1", socksAuth.Port, "", "")
                    bridge.Egress = none
                    code = SocksCode(bridge.Port, "hedef.proxy.test", echo.Port)
                    Check("SOCKS5 kimlik istiyor, verilmedi → AuthRequired", code = 2 AndAlso none.LastProblem = UpstreamProblem.AuthRequired, $"kod {code} · {none.LastProblem}")

                    ' HTTP CONNECT
                    bridge.Egress = New UpstreamProxyEgress(ProxyKind.Http, "127.0.0.1", httpOpen.Port, "", "")
                    got = RoundTrip(bridge.Port, "hedef.proxy.test", echo.Port, "h")
                    Check("köprü → HTTP CONNECT (kimliksiz) → hedef uçtan uca", got = "ECHO:h", got)
                    Check("HTTP CONNECT hedefi ad:port", httpOpen.LastTarget = "hedef.proxy.test:" & echo.Port, httpOpen.LastTarget)
                    bridge.Egress = New UpstreamProxyEgress(ProxyKind.Http, "127.0.0.1", httpAuth.Port, FakeUser, FakePass)
                    got = RoundTrip(bridge.Port, "hedef.proxy.test", echo.Port, "ha")
                    Check("köprü → HTTP CONNECT (Basic) → hedef uçtan uca", got = "ECHO:ha", got)
                    Check("Proxy-Authorization proxy'ye ulaştı", httpAuth.AuthOk = 1)
                    Dim hw = New UpstreamProxyEgress(ProxyKind.Http, "127.0.0.1", httpAuth.Port, FakeUser, "yanlis")
                    bridge.Egress = hw
                    code = SocksCode(bridge.Port, "hedef.proxy.test", echo.Port)
                    Check("HTTP 407 (yanlış şifre) → AuthFailed", code = 2 AndAlso hw.LastProblem = UpstreamProblem.AuthFailed, $"kod {code} · {hw.LastProblem}")
                    Dim hn = New UpstreamProxyEgress(ProxyKind.Http, "127.0.0.1", httpAuth.Port, "", "")
                    bridge.Egress = hn
                    code = SocksCode(bridge.Port, "hedef.proxy.test", echo.Port)
                    Check("HTTP 407 (kimliksiz) → AuthRequired", code = 2 AndAlso hn.LastProblem = UpstreamProblem.AuthRequired, $"kod {code} · {hn.LastProblem}")

                    ' wrong type: SOCKS5 egress at an HTTP proxy
                    Dim mism = New UpstreamProxyEgress(ProxyKind.Socks5, "127.0.0.1", httpOpen.Port, "", "")
                    mism.HandshakeTimeoutMs = 2000
                    bridge.Egress = mism
                    code = SocksCode(bridge.Port, "hedef.proxy.test", echo.Port)
                    Check("tür yanlış (SOCKS5 → HTTP proxy) → Protocol", code <> 0 AndAlso mism.LastProblem = UpstreamProblem.Protocol, $"kod {code} · {mism.LastProblem}")

                    ' upstream down
                    Dim closed As New TcpListener(IPAddress.Loopback, 0)
                    closed.Start()
                    Dim cPort = CType(closed.LocalEndpoint, IPEndPoint).Port
                    closed.Stop()
                    Dim dead = New UpstreamProxyEgress(ProxyKind.Socks5, "127.0.0.1", cPort, "", "")
                    bridge.Egress = dead
                    echo0 = echo.Accepts
                    code = SocksCode(bridge.Port, "hedef.proxy.test", echo.Port)
                    Check("proxy kapalı → hata + Unreachable, hedefe doğrudan gidilmedi", code = 1 AndAlso dead.LastProblem = UpstreamProblem.Unreachable AndAlso echo.Accepts = echo0,
                          $"kod {code} · {dead.LastProblem}")
                    Check("proxy kapalı → ulaşılamıyor (gözcü)", Not dead.ReachableAsync(2000).Result)
                    Check("proxy açık → ulaşılıyor (gözcü)", up.ReachableAsync(2000).Result)

                    ' open tunnel closed + refused when the egress goes away
                    bridge.Egress = up
                    Using c = SocksClient.ConnectAsync(bridge.Port, "hedef.proxy.test", echo.Port, CancellationToken.None).Result
                        Dim st = c.GetStream()
                        st.ReadTimeout = 3000
                        bridge.Egress = Nothing
                        Dim closedOk As Boolean
                        Try
                            Dim bb(15) As Byte
                            closedOk = st.Read(bb, 0, bb.Length) <= 0
                        Catch
                            closedOk = True
                        End Try
                        Check("proxy kipinde çıkış kalkınca açık tünel kapatıldı", closedOk)
                    End Using
                    Dim upAcc = socksOpen.Accepts
                    echo0 = echo.Accepts
                    code = SocksCode(bridge.Port, "hedef.proxy.test", echo.Port)
                    Thread.Sleep(150)
                    Check("çıkış yok → REDDEDİYOR (02), proxy'ye de hedefe de gidilmedi",
                          code = 2 AndAlso socksOpen.Accepts = upAcc AndAlso echo.Accepts = echo0, $"kod {code}")
                Finally
                    bridge.Stop()
                End Try
            End Using
        End Sub

        ' ---- 10. VpnManager in proxy mode (exit-IP probes stubbed: no internet host) ----
        Private Sub ProxyManagerTests()
            Using echo As New EchoTarget(), socksAuth As New FakeUpstream(ProxyKind.Socks5, FakeUser, FakePass)
                Dim directIp = IPAddress.Parse("198.51.100.1")
                VpnManager.DirectProbe = Function(tok) Task.FromResult(directIp)
                ' "via": a real round trip through the bridge → fake upstream → echo target, then a documentation-range IP
                VpnManager.ViaProbe = Function(bp, tok) Task.Run(Function()
                                                                        Dim r = RoundTrip(bp, "ip.proxy.test", echo.Port, "ip?")
                                                                        If r <> "ECHO:ip?" Then Throw New IOException("köprüden yanıt yok: " & r)
                                                                        Return IPAddress.Parse("203.0.113.9")
                                                                    End Function)
                VpnManager.WatchMs = 300
                VpnManager.Mode = VpnMode.Proxy
                VpnManager.ProxyType = ProxyKind.Socks5
                VpnManager.ProxyHost = "127.0.0.1"
                VpnManager.ProxyPort = socksAuth.Port
                VpnManager.ProxyUser = FakeUser
                VpnManager.ProxyPassword = "yanlis"
                Dim ok = VpnManager.ConnectAsync(CancellationToken.None).Result
                Check("yönetici: yanlış şifre → bağlanmadı + 'şifresi yanlış'", Not ok AndAlso VpnManager.State = VpnState.Failed AndAlso
                      VpnManager.StatusText.Contains("şifresi yanlış") AndAlso VpnManager.ProxyAddress = "", VpnManager.StatusText)
                Check("durum metninde kimlik yok", Not VpnManager.StatusText.Contains(FakeUser) AndAlso Not VpnManager.StatusText.Contains("yanlis"))

                VpnManager.ProxyPassword = FakePass
                ok = VpnManager.ConnectAsync(CancellationToken.None).Result
                Check("yönetici: proxy kipinde bağlandı", ok AndAlso VpnManager.State = VpnState.Connected, VpnManager.StatusText)
                Check("ProxyAddress = yerel köprü", VpnManager.ProxyAddress.StartsWith("socks5://127.0.0.1:"), VpnManager.ProxyAddress)
                Check("durum metni adres:port + çıkış IP'si, kimlik yok", VpnManager.StatusText.Contains("127.0.0.1:" & socksAuth.Port) AndAlso
                      VpnManager.StatusText.Contains("203.0.113.9") AndAlso Not VpnManager.StatusText.Contains(FakeUser) AndAlso Not VpnManager.StatusText.Contains("Deneme-Sifre"))
                Dim addr = VpnManager.ProxyAddress
                Check("ayar değişmeden ikinci bağlan = aynı bağlantı", VpnManager.ConnectAsync(CancellationToken.None).Result AndAlso VpnManager.ProxyAddress = addr)

                ' watchdog: proxy goes away → LinkLost, no direct fallback
                socksAuth.Dispose()
                Dim t0 = Environment.TickCount
                Do While VpnManager.State = VpnState.Connected AndAlso Environment.TickCount - t0 < 8000
                    Thread.Sleep(50)
                Loop
                Check("gözcü: proxy düşünce bağlantı koptu (Failed)", VpnManager.State = VpnState.Failed AndAlso VpnManager.StatusText.Contains("koptu"),
                      $"{VpnManager.State} · {VpnManager.StatusText}")
                Check("koptuktan sonra ProxyAddress boş", VpnManager.ProxyAddress = "")
                Dim port = CInt(addr.Substring(addr.LastIndexOf(":"c) + 1))
                Check("koptuktan sonra köprü REDDEDİYOR (doğrudan çıkış yok)", SocksCode(port, "ip.proxy.test", echo.Port) = 2)
                VpnManager.Disconnect()

                ' same exit IP through the proxy as direct → refused
                Using open As New FakeUpstream(ProxyKind.Http, "", "")
                    VpnManager.DirectProbe = Function(tok) Task.FromResult(IPAddress.Parse("203.0.113.9"))
                    VpnManager.ProxyType = ProxyKind.Http
                    VpnManager.ProxyPort = open.Port
                    VpnManager.ProxyUser = ""
                    VpnManager.ProxyPassword = ""
                    ok = VpnManager.ConnectAsync(CancellationToken.None).Result
                    Check("yönetici: çıkış IP'si doğrudanla aynı → bağlanmadı", Not ok AndAlso VpnManager.StatusText.Contains("aynı") AndAlso VpnManager.ProxyAddress = "", VpnManager.StatusText)

                    ' "Kes" while connecting: the attempt is abandoned — Off (not Failed), no bridge left, no exception
                    VpnManager.DirectProbe = Function(tok) Task.FromResult(directIp)
                    VpnManager.ViaProbe = Function(bp, tok) Task.Run(Function()
                                                                            Thread.Sleep(800)   ' a slow probe that does not watch the token
                                                                            Return IPAddress.Parse("203.0.113.7")
                                                                        End Function)
                    Dim pending = VpnManager.ConnectAsync(CancellationToken.None)
                    Thread.Sleep(200)
                    VpnManager.Disconnect()
                    Dim abortOk As Boolean = True, abortErr As String = ""
                    Try
                        abortOk = pending.Wait(5000) AndAlso Not pending.Result
                    Catch ex As Exception
                        abortOk = False
                        abortErr = ex.GetBaseException().GetType().Name
                    End Try
                    Check("Kes bağlanırken: deneme bırakıldı (Off, köprü yok, hata yok)", abortOk AndAlso VpnManager.State = VpnState.Off AndAlso
                          VpnManager.ProxyAddress = "", $"{VpnManager.State} · {VpnManager.StatusText} {abortErr}")
                End Using
                VpnManager.ProxyHost = ""
                ok = VpnManager.ConnectAsync(CancellationToken.None).Result
                Check("yönetici: adres boş → ayar sebebi", Not ok AndAlso VpnManager.StatusText.Contains("adresi girilmedi"), VpnManager.StatusText)
                VpnManager.Disconnect()
            End Using
        End Sub

        Private Sub ResetManager()
            Try
                VpnManager.Disconnect()
            Catch
            End Try
            VpnManager.ViaProbe = AddressOf ExitIpProbe.ViaBridgeAsync
            VpnManager.DirectProbe = AddressOf ExitIpProbe.DirectAsync
            VpnManager.WatchMs = 5000
            VpnManager.Mode = VpnMode.Pptp
            VpnManager.ProxyType = ProxyKind.Socks5
            VpnManager.ProxyHost = ""
            VpnManager.ProxyPort = 0
            VpnManager.ProxyUser = ""
            VpnManager.ProxyPassword = ""
        End Sub

        ''' <summary>Loopback echo server ("ECHO:" + what it got).</summary>
        Private NotInheritable Class EchoTarget
            Implements IDisposable
            Private ReadOnly _l As New TcpListener(IPAddress.Loopback, 0)
            Private _accepts As Integer
            Public ReadOnly Port As Integer

            Public ReadOnly Property Accepts As Integer
                Get
                    Return Volatile.Read(_accepts)
                End Get
            End Property

            Public Sub New()
                _l.Start()
                Port = CType(_l.LocalEndpoint, IPEndPoint).Port
                Task.Run(AddressOf Loop_)
            End Sub

            Private Async Function Loop_() As Task
                Do
                    Dim c As TcpClient
                    Try
                        c = Await _l.AcceptTcpClientAsync()
                    Catch
                        Exit Do
                    End Try
                    Interlocked.Increment(_accepts)
                    Dim __ = Task.Run(Async Function()
                                          Using c
                                              Try
                                                  Dim s = c.GetStream()
                                                  Dim buf(1023) As Byte
                                                  Do
                                                      Dim n = Await s.ReadAsync(buf, 0, buf.Length)
                                                      If n <= 0 Then Exit Do
                                                      Dim outp = Encoding.ASCII.GetBytes("ECHO:" & Encoding.ASCII.GetString(buf, 0, n))
                                                      Await s.WriteAsync(outp, 0, outp.Length)
                                                  Loop
                                              Catch
                                              End Try
                                          End Using
                                      End Function)
                Loop
            End Function

            Public Sub Dispose() Implements IDisposable.Dispose
                Try
                    _l.Stop()
                Catch
                End Try
            End Sub
        End Class

        ''' <summary>
        ''' Fake upstream proxy on 127.0.0.1 (SOCKS5 with optional RFC 1929, or HTTP CONNECT with optional Basic). Every target
        ''' (name or IP) is dialled as 127.0.0.1:port — it never resolves or contacts anything else. Records what it was asked.
        ''' </summary>
        Private NotInheritable Class FakeUpstream
            Implements IDisposable
            Private ReadOnly _l As New TcpListener(IPAddress.Loopback, 0)
            Private ReadOnly _kind As ProxyKind
            Private ReadOnly _user As String, _pass As String
            Private ReadOnly _open As New List(Of TcpClient)()
            Private _accepts As Integer, _authOk As Integer
            Public ReadOnly Port As Integer
            Public LastTarget As String = ""
            Public LastMethods As String = ""

            Public ReadOnly Property Accepts As Integer
                Get
                    Return Volatile.Read(_accepts)
                End Get
            End Property

            Public ReadOnly Property AuthOk As Integer
                Get
                    Return Volatile.Read(_authOk)
                End Get
            End Property

            Public Sub New(kind As ProxyKind, user As String, pass As String)
                _kind = kind : _user = user : _pass = pass
                _l.Start()
                Port = CType(_l.LocalEndpoint, IPEndPoint).Port
                Task.Run(AddressOf Loop_)
            End Sub

            Private Async Function Loop_() As Task
                Do
                    Dim c As TcpClient
                    Try
                        c = Await _l.AcceptTcpClientAsync()
                    Catch
                        Exit Do
                    End Try
                    Interlocked.Increment(_accepts)
                    SyncLock _open
                        _open.Add(c)
                    End SyncLock
                    Dim __ = Task.Run(Function() Handle(c))
                Loop
            End Function

            Private Async Function Handle(c As TcpClient) As Task
                Dim tok = CancellationToken.None
                Dim outC As TcpClient = Nothing
                Try
                    Dim s = c.GetStream()
                    Dim host As String, port As Integer
                    If _kind = ProxyKind.Socks5 Then
                        Dim g = Await SocksBridge.ReadExactAsync(s, 2, tok)
                        Dim methods = Await SocksBridge.ReadExactAsync(s, g(1), tok)
                        LastMethods = String.Join(",", methods)
                        If _user <> "" Then
                            If Not methods.Contains(CByte(2)) Then
                                Await s.WriteAsync(New Byte() {5, &HFF}, 0, 2)
                                Return
                            End If
                            Await s.WriteAsync(New Byte() {5, 2}, 0, 2)
                            Dim v = Await SocksBridge.ReadExactAsync(s, 2, tok)
                            Dim u = Encoding.UTF8.GetString(Await SocksBridge.ReadExactAsync(s, v(1), tok))
                            Dim pl = (Await SocksBridge.ReadExactAsync(s, 1, tok))(0)
                            Dim p = Encoding.UTF8.GetString(Await SocksBridge.ReadExactAsync(s, pl, tok))
                            Dim good = u = _user AndAlso p = _pass
                            Await s.WriteAsync(New Byte() {1, If(good, CByte(0), CByte(1))}, 0, 2)
                            If Not good Then Return
                            Interlocked.Increment(_authOk)
                        Else
                            Await s.WriteAsync(New Byte() {5, 0}, 0, 2)
                        End If
                        Dim req = Await SocksBridge.ReadRequestAsync(s, tok)
                        host = If(req.Host, req.Address?.ToString())
                        port = req.Port
                    Else
                        Dim head As New StringBuilder()
                        Dim one(0) As Byte
                        Do While Not head.ToString().EndsWith(vbCrLf & vbCrLf)
                            If Await s.ReadAsync(one, 0, 1) <= 0 Then Return
                            head.Append(ChrW(one(0)))
                        Loop
                        Dim lines = head.ToString().Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
                        Dim auth = lines.FirstOrDefault(Function(l) l.StartsWith("Proxy-Authorization: Basic ", StringComparison.OrdinalIgnoreCase))
                        If _user <> "" Then
                            Dim want = Convert.ToBase64String(Encoding.UTF8.GetBytes(_user & ":" & _pass))
                            If auth Is Nothing OrElse auth.Substring("Proxy-Authorization: Basic ".Length).Trim() <> want Then
                                Dim r407 = Encoding.ASCII.GetBytes("HTTP/1.1 407 Proxy Authentication Required" & vbCrLf & "Content-Length: 0" & vbCrLf & vbCrLf)
                                Await s.WriteAsync(r407, 0, r407.Length)
                                Return
                            End If
                            Interlocked.Increment(_authOk)
                        End If
                        Dim parts = lines(0).Split(" "c)
                        If parts.Length < 2 OrElse parts(0) <> "CONNECT" Then Return
                        Dim authority = parts(1)
                        host = authority.Substring(0, authority.LastIndexOf(":"c))
                        port = Integer.Parse(authority.Substring(authority.LastIndexOf(":"c) + 1))
                    End If
                    LastTarget = host & ":" & port
                    outC = New TcpClient()
                    Dim failed = False
                    Try
                        Await outC.ConnectAsync(IPAddress.Loopback, port)
                    Catch
                        failed = True
                    End Try
                    If _kind = ProxyKind.Socks5 Then
                        Dim rep = New Byte() {5, If(failed, CByte(5), CByte(0)), 0, 1, 127, 0, 0, 1, 0, 0}
                        Await s.WriteAsync(rep, 0, rep.Length)
                    Else
                        Dim r = Encoding.ASCII.GetBytes(If(failed, "HTTP/1.1 502 Bad Gateway", "HTTP/1.1 200 Connection established") & vbCrLf & vbCrLf)
                        Await s.WriteAsync(r, 0, r.Length)
                    End If
                    If failed Then Return
                    Dim os = outC.GetStream()
                    Dim a = os.CopyToAsync(s), b = s.CopyToAsync(os)
                    Await Task.WhenAny(a, b)
                Catch
                Finally
                    outC?.Close()
                    c.Close()
                End Try
            End Function

            Public Sub Dispose() Implements IDisposable.Dispose
                Try
                    _l.Stop()
                Catch
                End Try
                SyncLock _open
                    For Each c In _open
                        Try
                            c.Close()
                        Catch
                        End Try
                    Next
                    _open.Clear()
                End SyncLock
            End Sub
        End Class

    End Module

End Namespace
