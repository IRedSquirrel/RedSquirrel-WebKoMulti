Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Services.Vpn

    ''' <summary>
    ''' The bridge's outbound leg: VpnEgress (PPTP: sockets pinned to the VPN interface, the bridge resolves names) or
    ''' UpstreamProxyEgress (proxy mode: tunnels opened through a SOCKS5 / HTTP CONNECT proxy, the proxy resolves names).
    ''' SocksBridge.Egress = Nothing in either mode = every request refused, never a direct connection.
    ''' </summary>
    Friend Interface IEgress
    End Interface

    ''' <summary>
    ''' Where the bridge's outgoing sockets leave: the VPN interface's IPv4 + index. Every socket is pinned twice — IP_UNICAST_IF
    ''' (the send interface) and Bind (the source address). Measured on this PC: a pinned socket on an interface without a route
    ''' gets 10051 and is NOT sent elsewhere, so a pinned socket either uses the VPN or fails — never the normal line.
    ''' </summary>
    Friend NotInheritable Class VpnEgress
        Implements IEgress

        Friend Const IP_UNICAST_IF As Integer = 31

        Public ReadOnly LocalIp As IPAddress
        Public ReadOnly InterfaceIndex As Integer
        Public ReadOnly DnsServers As IReadOnlyList(Of IPAddress)

        Public Sub New(localIp As IPAddress, interfaceIndex As Integer, dnsServers As IEnumerable(Of IPAddress))
            Me.LocalIp = localIp
            Me.InterfaceIndex = interfaceIndex
            Me.DnsServers = If(dnsServers, Enumerable.Empty(Of IPAddress)()).ToList()
        End Sub

        ''' <summary>A socket that can only leave through this interface; throws if it can't be pinned.</summary>
        Public Function NewSocket(type As SocketType, proto As ProtocolType) As Socket
            Dim s As New Socket(AddressFamily.InterNetwork, type, proto)
            Try
                s.SetSocketOption(SocketOptionLevel.IP, CType(IP_UNICAST_IF, SocketOptionName), UnicastIfValue(InterfaceIndex))
                s.Bind(New IPEndPoint(LocalIp, 0))
                Return s
            Catch
                s.Close()
                Throw
            End Try
        End Function

        ''' <summary>IP_UNICAST_IF wants the index in network byte order (4 bytes).</summary>
        Friend Shared Function UnicastIfValue(index As Integer) As Byte()
            Return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(index))
        End Function

        Public Overrides Function ToString() As String
            Return $"{LocalIp} (arayüz {InterfaceIndex})"
        End Function
    End Class

    ''' <summary>
    ''' SOCKS5 server on 127.0.0.1:&lt;random&gt; for the "VPN ile gir" views (Chromium sends host names to a socks5 proxy, so
    ''' DNS happens here — PPTP mode; in proxy mode the name goes on to the upstream proxy). CONNECT only; IPv4 and domain targets;
    ''' no auth; loopback clients only. With <see cref="Egress"/> = Nothing (VPN / proxy down) every request is refused (reply 02)
    ''' and nothing is dialled — there is no direct fallback.
    ''' </summary>
    Friend NotInheritable Class SocksBridge

        Private _listener As TcpListener
        Private _cts As CancellationTokenSource
        Private ReadOnly _open As New HashSet(Of Socket)()
        Private _egress As IEgress

        ''' <summary>Resolves a host name for an egress. Default: VpnDns (VPN's DNS, falls back to system DNS).</summary>
        Public Property Resolver As Func(Of String, VpnEgress, CancellationToken, Task(Of IPAddress)) = AddressOf VpnDns.ResolveAsync

        Public Property ConnectTimeoutMs As Integer = 15000

        Public ReadOnly Property Port As Integer
            Get
                Dim l = _listener
                Return If(l Is Nothing, 0, CType(l.LocalEndpoint, IPEndPoint).Port)
            End Get
        End Property

        Public ReadOnly Property Running As Boolean
            Get
                Return _listener IsNot Nothing
            End Get
        End Property

        ''' <summary>Counters for the self-test / log.</summary>
        Public Accepted As Integer, Refused As Integer, Relayed As Integer

        Public Property Egress As IEgress
            Get
                Return Volatile.Read(_egress)
            End Get
            Set(value As IEgress)
                Volatile.Write(_egress, value)
                If value Is Nothing Then CloseAll()
            End Set
        End Property

        Public Sub Start()
            If _listener IsNot Nothing Then Return
            _cts = New CancellationTokenSource()
            _listener = New TcpListener(IPAddress.Loopback, 0)
            _listener.Start(64)
            Dim l = _listener, tok = _cts.Token
            Task.Run(Function() AcceptLoop(l, tok))
        End Sub

        Public Sub [Stop]()
            Try
                _cts?.Cancel()
            Catch
            End Try
            Try
                _listener?.Stop()
            Catch
            End Try
            _listener = Nothing
            CloseAll()
        End Sub

        ''' <summary>Closes every open tunnel (VPN dropped / stopping).</summary>
        Public Sub CloseAll()
            Dim list As List(Of Socket)
            SyncLock _open
                list = _open.ToList()
                _open.Clear()
            End SyncLock
            For Each s In list
                Try
                    s.Close()
                Catch
                End Try
            Next
        End Sub

        Private Sub Track(s As Socket)
            SyncLock _open
                _open.Add(s)
            End SyncLock
        End Sub

        Private Sub Untrack(s As Socket)
            SyncLock _open
                _open.Remove(s)
            End SyncLock
            Try
                s.Close()
            Catch
            End Try
        End Sub

        Private Async Function AcceptLoop(l As TcpListener, tok As CancellationToken) As Task
            Do While Not tok.IsCancellationRequested
                Dim c As Socket
                Try
                    c = Await l.AcceptSocketAsync().ConfigureAwait(False)
                Catch
                    Exit Do
                End Try
                Dim rep = TryCast(c.RemoteEndPoint, IPEndPoint)
                If rep Is Nothing OrElse Not IPAddress.IsLoopback(rep.Address) Then
                    c.Close()
                    Continue Do
                End If
                Interlocked.Increment(Accepted)
                Dim __ = Task.Run(Function() HandleClient(c, tok))
            Loop
        End Function

        Private Async Function HandleClient(client As Socket, tok As CancellationToken) As Task
            Track(client)
            Dim outbound As Socket = Nothing
            Try
                client.NoDelay = True
                Using cs As New NetworkStream(client, False)
                    ' greeting: VER NMETHODS METHODS…
                    Dim g = Await ReadExactAsync(cs, 2, tok)
                    If g(0) <> 5 Then Return
                    Dim methods = Await ReadExactAsync(cs, g(1), tok)
                    If Not methods.Contains(CByte(0)) Then
                        Await cs.WriteAsync(New Byte() {5, &HFF}, 0, 2, tok)
                        Return
                    End If
                    Await cs.WriteAsync(New Byte() {5, 0}, 0, 2, tok)

                    Dim req = Await ReadRequestAsync(cs, tok)
                    If req.Reply <> 0 Then
                        Await SendReplyAsync(cs, req.Reply, Nothing, tok)
                        Return
                    End If

                    Dim eg = Egress
                    If eg Is Nothing Then
                        Interlocked.Increment(Refused)
                        Await SendReplyAsync(cs, 2, Nothing, tok)   ' "not allowed": VPN is down, never go direct
                        Return
                    End If

                    Dim code As Byte = 0
                    Dim up = TryCast(eg, UpstreamProxyEgress)
                    Dim vpn = TryCast(eg, VpnEgress)
                    If up IsNot Nothing Then
                        ' proxy mode: the target host name goes to the upstream proxy as is (no local DNS for targets)
                        Dim d = Await up.DialAsync(If(req.Address IsNot Nothing, req.Address.ToString(), req.Host), req.Port, tok).ConfigureAwait(False)
                        If d.Socket IsNot Nothing Then
                            outbound = d.Socket
                            Track(outbound)
                        Else
                            code = If(d.Reply = 0, CByte(1), d.Reply)
                        End If
                    ElseIf vpn IsNot Nothing Then
                        Dim target As IPAddress = req.Address
                        If target Is Nothing Then
                            Try
                                target = Await Resolver(req.Host, vpn, tok).ConfigureAwait(False)
                            Catch
                                target = Nothing
                            End Try
                            If target Is Nothing Then
                                Await SendReplyAsync(cs, 4, Nothing, tok)
                                Return
                            End If
                        End If
                        If Egress IsNot eg Then                          ' VPN changed / dropped meanwhile
                            Interlocked.Increment(Refused)
                            Await SendReplyAsync(cs, 2, Nothing, tok)
                            Return
                        End If
                        Try
                            outbound = vpn.NewSocket(SocketType.Stream, ProtocolType.Tcp)
                            Track(outbound)
                            outbound.NoDelay = True
                            Dim ct As Task = outbound.ConnectAsync(New IPEndPoint(target, req.Port))
                            If Await Task.WhenAny(ct, Task.Delay(ConnectTimeoutMs, tok)).ConfigureAwait(False) IsNot ct Then
                                code = 6
                            Else
                                Await ct.ConfigureAwait(False)
                            End If
                        Catch ex As SocketException
                            code = ReplyFor(ex.SocketErrorCode)
                        Catch
                            code = 1
                        End Try
                    Else
                        code = 2                                         ' unknown egress: refuse, never go direct
                    End If
                    If code <> 0 Then
                        If code = 2 Then Interlocked.Increment(Refused)
                        Await SendReplyAsync(cs, code, Nothing, tok)
                        Return
                    End If
                    If Egress IsNot eg Then                              ' mode / link changed while dialling
                        Interlocked.Increment(Refused)
                        Await SendReplyAsync(cs, 2, Nothing, tok)
                        Return
                    End If
                    Await SendReplyAsync(cs, 0, TryCast(outbound.LocalEndPoint, IPEndPoint), tok)
                    Interlocked.Increment(Relayed)

                    Using os As New NetworkStream(outbound, False)
                        Dim a = PumpAsync(cs, os, outbound, tok)
                        Dim b = PumpAsync(os, cs, client, tok)
                        Await Task.WhenAny(a, b).ConfigureAwait(False)
                    End Using
                End Using
            Catch
            Finally
                If outbound IsNot Nothing Then Untrack(outbound)
                Untrack(client)
            End Try
        End Function

        Private Shared Async Function PumpAsync(src As NetworkStream, dst As NetworkStream, dstSocket As Socket, tok As CancellationToken) As Task
            Dim buf(32767) As Byte
            Try
                Do
                    Dim n = Await src.ReadAsync(buf, 0, buf.Length, tok).ConfigureAwait(False)
                    If n <= 0 Then Exit Do
                    Await dst.WriteAsync(buf, 0, n, tok).ConfigureAwait(False)
                Loop
                Try
                    dstSocket.Shutdown(SocketShutdown.Send)
                Catch
                End Try
            Catch
            End Try
        End Function

        Friend Shared Function ReplyFor(e As SocketError) As Byte
            Select Case e
                Case SocketError.ConnectionRefused : Return 5
                Case SocketError.HostUnreachable, SocketError.HostNotFound : Return 4
                Case SocketError.NetworkUnreachable, SocketError.NetworkDown : Return 3
                Case SocketError.TimedOut : Return 6
                Case Else : Return 1
            End Select
        End Function

        ''' <summary>Parsed CONNECT request. Reply ≠ 0 = refuse with that code.</summary>
        Friend Structure SocksRequest
            Public Reply As Byte
            Public Address As IPAddress
            Public Host As String
            Public Port As Integer
        End Structure

        Friend Shared Async Function ReadRequestAsync(s As IO.Stream, tok As CancellationToken) As Task(Of SocksRequest)
            Dim r As New SocksRequest()
            Dim h = Await ReadExactAsync(s, 4, tok)               ' VER CMD RSV ATYP
            If h(0) <> 5 Then
                r.Reply = 1
                Return r
            End If
            Select Case h(3)
                Case 1
                    r.Address = New IPAddress(Await ReadExactAsync(s, 4, tok))
                Case 3
                    Dim len = (Await ReadExactAsync(s, 1, tok))(0)
                    r.Host = Text.Encoding.ASCII.GetString(Await ReadExactAsync(s, len, tok))
                    Dim lit As IPAddress = Nothing
                    If IPAddress.TryParse(r.Host, lit) AndAlso lit.AddressFamily = AddressFamily.InterNetwork Then r.Address = lit
                Case 4
                    Await ReadExactAsync(s, 16 + 2, tok)
                    r.Reply = 8                                    ' IPv6: not over this bridge
                    Return r
                Case Else
                    r.Reply = 8
                    Return r
            End Select
            Dim p = Await ReadExactAsync(s, 2, tok)
            r.Port = (CInt(p(0)) << 8) Or p(1)
            If h(1) <> 1 Then r.Reply = 7                           ' only CONNECT
            If r.Reply = 0 AndAlso r.Address Is Nothing AndAlso Not ValidHost(r.Host) Then r.Reply = 4
            Return r
        End Function

        Friend Shared Function ValidHost(h As String) As Boolean
            If String.IsNullOrEmpty(h) OrElse h.Length > 253 Then Return False
            Return h.All(Function(c) (c < ChrW(128) AndAlso Char.IsLetterOrDigit(c)) OrElse c = "."c OrElse c = "-"c OrElse c = "_"c) AndAlso
                   h.Any(Function(c) Char.IsLetter(c))
        End Function

        Private Shared Async Function SendReplyAsync(s As IO.Stream, code As Byte, bound As IPEndPoint, tok As CancellationToken) As Task
            Dim b(9) As Byte
            b(0) = 5 : b(1) = code : b(2) = 0 : b(3) = 1
            If bound IsNot Nothing AndAlso bound.AddressFamily = AddressFamily.InterNetwork Then
                Buffer.BlockCopy(bound.Address.GetAddressBytes(), 0, b, 4, 4)
                b(8) = CByte((bound.Port >> 8) And &HFF) : b(9) = CByte(bound.Port And &HFF)
            End If
            Try
                Await s.WriteAsync(b, 0, b.Length, tok).ConfigureAwait(False)
            Catch
            End Try
        End Function

        Friend Shared Async Function ReadExactAsync(s As IO.Stream, n As Integer, tok As CancellationToken) As Task(Of Byte())
            Dim buf(n - 1) As Byte
            Dim got = 0
            Do While got < n
                Dim k = Await s.ReadAsync(buf, got, n - got, tok).ConfigureAwait(False)
                If k <= 0 Then Throw New IO.EndOfStreamException()
                got += k
            Loop
            Return buf
        End Function

    End Class

End Namespace
