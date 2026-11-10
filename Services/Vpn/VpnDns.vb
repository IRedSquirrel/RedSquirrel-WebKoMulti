Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Services.Vpn

    ''' <summary>
    ''' Host-name lookup for the bridge: an A query over UDP sent THROUGH the VPN (pinned socket) to the VPN's own DNS servers,
    ''' so the name doesn't go to the local provider either. If the VPN gave no DNS or none answers, the system resolver is used
    ''' (the connection itself still leaves through the VPN) — said once in the log and kept in <see cref="SystemFallbacks"/>.
    ''' </summary>
    Friend Module VpnDns

        Friend Property TimeoutMs As Integer = 2500
        Friend SystemFallbacks As Integer
        Private _fallbackLogged As Integer

        ''' <summary>Test hook: DNS servers to use instead of the egress's (Nothing = egress.DnsServers); port too.</summary>
        Friend ServerOverride As IList(Of IPEndPoint) = Nothing

        Friend Async Function ResolveAsync(host As String, eg As VpnEgress, tok As CancellationToken) As Task(Of IPAddress)
            Dim lit As IPAddress = Nothing
            If IPAddress.TryParse(host, lit) Then Return If(lit.AddressFamily = AddressFamily.InterNetwork, lit, Nothing)
            Dim servers As IList(Of IPEndPoint) = If(ServerOverride, eg.DnsServers.Select(Function(a) New IPEndPoint(a, 53)).ToList())
            For Each srv In servers
                Try
                    Dim r = Await QueryAsync(host, srv, eg, tok).ConfigureAwait(False)
                    If r IsNot Nothing Then Return r
                Catch
                End Try
            Next
            Interlocked.Increment(SystemFallbacks)
            If Interlocked.Exchange(_fallbackLogged, 1) = 0 Then
                BotLog.Warn("vpn", If(servers.Count = 0, "VPN DNS sunucusu vermedi", "VPN'in DNS sunucusu yanıt vermedi") &
                            " — adlar Windows DNS'iyle çözülüyor (bağlantılar yine VPN'den çıkıyor)")
            End If
            Try
                Dim all = Await Dns.GetHostAddressesAsync(host).ConfigureAwait(False)
                Return all.FirstOrDefault(Function(a) a.AddressFamily = AddressFamily.InterNetwork)
            Catch
                Return Nothing
            End Try
        End Function

        Private Async Function QueryAsync(host As String, srv As IPEndPoint, eg As VpnEgress, tok As CancellationToken) As Task(Of IPAddress)
            Dim id = CUShort(New Random().Next(1, 65535))
            Dim q = BuildQuery(id, host)
            Using s = eg.NewSocket(SocketType.Dgram, ProtocolType.Udp)
                s.SendTo(q, srv)
                Dim buf(1499) As Byte
                Dim recv = Task.Run(Function() s.Receive(buf), tok)
                If Await Task.WhenAny(recv, Task.Delay(TimeoutMs, tok)).ConfigureAwait(False) IsNot recv Then
                    s.Close()
                    Return Nothing
                End If
                Dim n = Await recv.ConfigureAwait(False)
                Dim resp(n - 1) As Byte
                Buffer.BlockCopy(buf, 0, resp, 0, n)
                Return ParseAResponse(resp, id).FirstOrDefault()
            End Using
        End Function

        ''' <summary>Standard query, RD set, one question: host IN A.</summary>
        Friend Function BuildQuery(id As UShort, host As String) As Byte()
            Dim b As New List(Of Byte) From {CByte(id >> 8), CByte(id And &HFF), 1, 0, 0, 1, 0, 0, 0, 0, 0, 0}
            For Each label In host.TrimEnd("."c).Split("."c)
                Dim bytes = Text.Encoding.ASCII.GetBytes(label)
                If bytes.Length = 0 OrElse bytes.Length > 63 Then Throw New ArgumentException("geçersiz ad")
                b.Add(CByte(bytes.Length))
                b.AddRange(bytes)
            Next
            b.AddRange(New Byte() {0, 0, 1, 0, 1})
            Return b.ToArray()
        End Function

        ''' <summary>A records of a response to query <paramref name="id"/> (empty on mismatch / error / no answer).</summary>
        Friend Function ParseAResponse(r As Byte(), id As UShort) As List(Of IPAddress)
            Dim list As New List(Of IPAddress)()
            If r Is Nothing OrElse r.Length < 12 Then Return list
            If ((CInt(r(0)) << 8) Or r(1)) <> id Then Return list
            If (r(2) And &H80) = 0 Then Return list                ' not a response
            If (r(3) And &HF) <> 0 Then Return list                 ' RCODE ≠ 0
            Dim qd = (CInt(r(4)) << 8) Or r(5), an = (CInt(r(6)) << 8) Or r(7)
            Dim p = 12
            For i = 1 To qd
                p = SkipName(r, p) + 4
                If p > r.Length Then Return list
            Next
            For i = 1 To an
                p = SkipName(r, p)
                If p + 10 > r.Length Then Return list
                Dim type = (CInt(r(p)) << 8) Or r(p + 1)
                Dim cls = (CInt(r(p + 2)) << 8) Or r(p + 3)
                Dim len = (CInt(r(p + 8)) << 8) Or r(p + 9)
                p += 10
                If p + len > r.Length Then Return list
                If type = 1 AndAlso cls = 1 AndAlso len = 4 Then
                    Dim a(3) As Byte
                    Buffer.BlockCopy(r, p, a, 0, 4)
                    list.Add(New IPAddress(a))
                End If
                p += len
            Next
            Return list
        End Function

        Private Function SkipName(r As Byte(), p As Integer) As Integer
            Do While p < r.Length
                Dim l = r(p)
                If l = 0 Then Return p + 1
                If (l And &HC0) = &HC0 Then Return p + 2
                p += l + 1
            Loop
            Return r.Length + 1
        End Function

        ''' <summary>A minimal answer (for the self-test's fake DNS server).</summary>
        Friend Function BuildAResponse(query As Byte(), addr As IPAddress) As Byte()
            Dim b As New List(Of Byte)(query)
            b(2) = &H81 : b(3) = &H80                              ' QR, RD, RA, RCODE 0
            b(6) = 0 : b(7) = 1                                    ' ANCOUNT 1
            b.AddRange(New Byte() {&HC0, 12, 0, 1, 0, 1, 0, 0, 0, 60, 0, 4})
            b.AddRange(addr.GetAddressBytes())
            Return b.ToArray()
        End Function

    End Module

End Namespace
