Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Namespace Services.Vpn

    ''' <summary>Which upstream proxy protocol the bridge speaks in proxy mode.</summary>
    Public Enum ProxyKind
        Socks5
        Http
    End Enum

    ''' <summary>Why the last dial through the upstream proxy failed (the manager turns it into a Turkish reason).</summary>
    Friend Enum UpstreamProblem
        None
        ''' <summary>The proxy itself could not be reached (TCP connect / name / timeout).</summary>
        Unreachable
        ''' <summary>The proxy wants credentials and none were given.</summary>
        AuthRequired
        ''' <summary>The proxy rejected the user name / password.</summary>
        AuthFailed
        ''' <summary>The proxy's answer is not the protocol we expected (wrong type?).</summary>
        Protocol
        ''' <summary>The proxy is fine but could not reach the target (its own reply code).</summary>
        Target
    End Enum

    ''' <summary>Result of <see cref="UpstreamProxyEgress.DialAsync"/>: a connected socket (handshake done) or a SOCKS reply code.</summary>
    Friend Structure UpstreamDial
        Public Socket As Socket
        Public Reply As Byte
        Public Problem As UpstreamProblem
        Public Detail As String
    End Structure

    ''' <summary>
    ''' Proxy-mode egress of <see cref="SocksBridge"/>: every tunnel is opened THROUGH a user-supplied SOCKS5 (RFC 1928, with RFC 1929
    ''' user / password) or HTTP CONNECT (Proxy-Authorization: Basic) proxy. The target's host name is handed to the proxy as is —
    ''' no local DNS lookup for targets. The socket to the proxy itself is a normal (direct) socket; if the proxy cannot be reached
    ''' the tunnel fails, there is no direct path to the target. Credentials stay in memory, never in a log line or a message.
    ''' </summary>
    Friend NotInheritable Class UpstreamProxyEgress
        Implements IEgress

        Public ReadOnly Kind As ProxyKind
        Public ReadOnly Host As String
        Public ReadOnly Port As Integer
        Private ReadOnly _user As String
        Private ReadOnly _pass As String

        Public Property HandshakeTimeoutMs As Integer = 15000

        Private _lastProblem As Integer
        Private _lastDetail As String = ""

        Public Sub New(kind As ProxyKind, host As String, port As Integer, user As String, password As String)
            Me.Kind = kind
            Me.Host = If(host, "").Trim()
            Me.Port = port
            _user = If(user, "")
            _pass = If(password, "")
        End Sub

        Public ReadOnly Property HasCredentials As Boolean
            Get
                Return _user <> ""
            End Get
        End Property

        ''' <summary>Problem of the most recent failed dial (None after a success).</summary>
        Public ReadOnly Property LastProblem As UpstreamProblem
            Get
                Return CType(Volatile.Read(_lastProblem), UpstreamProblem)
            End Get
        End Property

        Public ReadOnly Property LastDetail As String
            Get
                Return Volatile.Read(_lastDetail)
            End Get
        End Property

        ''' <summary>"SOCKS5 host:port" — for the log / status (no credentials).</summary>
        Public Overrides Function ToString() As String
            Return $"{KindText(Kind)} {Host}:{Port}"
        End Function

        Friend Shared Function KindText(k As ProxyKind) As String
            Return If(k = ProxyKind.Http, "HTTP", "SOCKS5")
        End Function

        ''' <summary>Opens a tunnel to target:port through the proxy. Never throws for proxy / network failures.</summary>
        Public Async Function DialAsync(target As String, targetPort As Integer, tok As CancellationToken) As Task(Of UpstreamDial)
            Dim r As New UpstreamDial()
            Dim s As Socket = Nothing
            Using cts = CancellationTokenSource.CreateLinkedTokenSource(tok)
                cts.CancelAfter(HandshakeTimeoutMs)
                Try
                    s = Await ConnectProxyAsync(cts.Token).ConfigureAwait(False)
                Catch ex As Exception
                    Return Finish(r, Nothing, 1, UpstreamProblem.Unreachable, Describe(ex, cts.IsCancellationRequested))
                End Try
                Dim outcome As UpstreamDial
                Try
                    Using reg = cts.Token.Register(Sub()
                                                       Try
                                                           s.Close()
                                                       Catch
                                                       End Try
                                                   End Sub)
                        Using ns As New NetworkStream(s, False)
                            If Kind = ProxyKind.Http Then
                                outcome = Await HttpConnectAsync(ns, target, targetPort, cts.Token).ConfigureAwait(False)
                            Else
                                outcome = Await Socks5ConnectAsync(ns, target, targetPort, cts.Token).ConfigureAwait(False)
                            End If
                        End Using
                    End Using
                Catch ex As Exception
                    outcome = New UpstreamDial With {.Reply = If(cts.IsCancellationRequested, CByte(6), CByte(1)), .Problem = UpstreamProblem.Protocol,
                                                     .Detail = Describe(ex, cts.IsCancellationRequested)}
                End Try
                If outcome.Problem <> UpstreamProblem.None Then
                    Try
                        s.Close()
                    Catch
                    End Try
                    Return Finish(r, Nothing, outcome.Reply, outcome.Problem, outcome.Detail)
                End If
                Return Finish(r, s, 0, UpstreamProblem.None, "")
            End Using
        End Function

        ''' <summary>Watchdog: can a TCP connection to the proxy still be opened? (no handshake, nothing sent).</summary>
        Public Async Function ReachableAsync(timeoutMs As Integer) As Task(Of Boolean)
            Using cts As New CancellationTokenSource(timeoutMs)
                Try
                    Dim s = Await ConnectProxyAsync(cts.Token).ConfigureAwait(False)
                    s.Close()
                    Return True
                Catch
                    Return False
                End Try
            End Using
        End Function

        Private Function Finish(r As UpstreamDial, s As Socket, reply As Byte, problem As UpstreamProblem, detail As String) As UpstreamDial
            r.Socket = s
            r.Reply = reply
            r.Problem = problem
            r.Detail = If(detail, "")
            Volatile.Write(_lastProblem, CInt(problem))
            Volatile.Write(_lastDetail, r.Detail)
            Return r
        End Function

        Private Shared Function Describe(ex As Exception, timedOut As Boolean) As String
            If timedOut Then Return "zaman aşımı"
            Dim e = ex
            Do While (TypeOf e Is AggregateException OrElse TypeOf e Is IO.IOException) AndAlso e.InnerException IsNot Nothing
                e = e.InnerException
            Loop
            Dim se = TryCast(e, SocketException)
            If se IsNot Nothing Then Return $"{se.SocketErrorCode} ({se.ErrorCode})"
            Return e.Message
        End Function

        ''' <summary>A normal TCP socket connected to the proxy (IPv4 preferred). Throws on failure / cancellation.</summary>
        Private Async Function ConnectProxyAsync(tok As CancellationToken) As Task(Of Socket)
            Dim addr As IPAddress = Nothing
            If Not IPAddress.TryParse(Host, addr) Then
                Dim list = Await Dns.GetHostAddressesAsync(Host).ConfigureAwait(False)
                addr = list.FirstOrDefault(Function(a) a.AddressFamily = AddressFamily.InterNetwork)
                If addr Is Nothing Then addr = list.FirstOrDefault()
                If addr Is Nothing Then Throw New SocketException(CInt(SocketError.HostNotFound))
            End If
            tok.ThrowIfCancellationRequested()
            Dim s As New Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            Try
                s.NoDelay = True
                Using reg = tok.Register(Sub()
                                             Try
                                                 s.Close()
                                             Catch
                                             End Try
                                         End Sub)
                    Await s.ConnectAsync(New IPEndPoint(addr, Port)).ConfigureAwait(False)
                End Using
                tok.ThrowIfCancellationRequested()
                Return s
            Catch
                s.Close()
                Throw
            End Try
        End Function

        ' --- SOCKS5 (RFC 1928 / RFC 1929) ---------------------------------------------------------------------------------

        Private Async Function Socks5ConnectAsync(ns As NetworkStream, target As String, targetPort As Integer, tok As CancellationToken) As Task(Of UpstreamDial)
            Dim greet = If(HasCredentials, New Byte() {5, 2, 0, 2}, New Byte() {5, 1, 0})
            Await ns.WriteAsync(greet, 0, greet.Length, tok).ConfigureAwait(False)
            Dim m = Await SocksBridge.ReadExactAsync(ns, 2, tok).ConfigureAwait(False)
            If m(0) <> 5 Then Return Bad(UpstreamProblem.Protocol, 1, "SOCKS5 yanıtı değil")
            Select Case m(1)
                Case 0
                Case 2
                    If Not HasCredentials Then Return Bad(UpstreamProblem.AuthRequired, 2, "proxy kimlik istiyor")
                    Dim u = Encoding.UTF8.GetBytes(_user), p = Encoding.UTF8.GetBytes(_pass)
                    If u.Length > 255 OrElse p.Length > 255 Then Return Bad(UpstreamProblem.AuthFailed, 2, "kimlik çok uzun")
                    Dim auth As New List(Of Byte) From {1, CByte(u.Length)}
                    auth.AddRange(u)
                    auth.Add(CByte(p.Length))
                    auth.AddRange(p)
                    Await ns.WriteAsync(auth.ToArray(), 0, auth.Count, tok).ConfigureAwait(False)
                    Dim ar = Await SocksBridge.ReadExactAsync(ns, 2, tok).ConfigureAwait(False)
                    If ar(1) <> 0 Then Return Bad(UpstreamProblem.AuthFailed, 2, "kimlik reddedildi")
                Case &HFF
                    Return Bad(If(HasCredentials, UpstreamProblem.AuthFailed, UpstreamProblem.AuthRequired), 2,
                               If(HasCredentials, "kimlik yöntemi kabul edilmedi", "proxy kimlik istiyor"))
                Case Else
                    Return Bad(UpstreamProblem.Protocol, 1, $"bilinmeyen yöntem {m(1)}")
            End Select

            Dim req As New List(Of Byte) From {5, 1, 0}
            Dim lit As IPAddress = Nothing
            If IPAddress.TryParse(target, lit) AndAlso lit.AddressFamily = AddressFamily.InterNetwork Then
                req.Add(1)
                req.AddRange(lit.GetAddressBytes())
            Else
                Dim hb = Encoding.ASCII.GetBytes(target)
                If hb.Length = 0 OrElse hb.Length > 255 Then Return Bad(UpstreamProblem.Target, 4, "hedef adı geçersiz")
                req.Add(3)
                req.Add(CByte(hb.Length))
                req.AddRange(hb)
            End If
            req.Add(CByte((targetPort >> 8) And &HFF))
            req.Add(CByte(targetPort And &HFF))
            Await ns.WriteAsync(req.ToArray(), 0, req.Count, tok).ConfigureAwait(False)
            Dim h = Await SocksBridge.ReadExactAsync(ns, 4, tok).ConfigureAwait(False)
            If h(0) <> 5 Then Return Bad(UpstreamProblem.Protocol, 1, "SOCKS5 yanıtı değil")
            Dim rest As Integer
            Select Case h(3)
                Case 1 : rest = 4
                Case 4 : rest = 16
                Case 3 : rest = (Await SocksBridge.ReadExactAsync(ns, 1, tok).ConfigureAwait(False))(0)
                Case Else : Return Bad(UpstreamProblem.Protocol, 1, "SOCKS5 adres türü tanınmadı")
            End Select
            Await SocksBridge.ReadExactAsync(ns, rest + 2, tok).ConfigureAwait(False)
            If h(1) <> 0 Then Return Bad(UpstreamProblem.Target, h(1), SocksException.Describe(h(1)))
            Return New UpstreamDial()
        End Function

        ' --- HTTP CONNECT ------------------------------------------------------------------------------------------------

        Private Async Function HttpConnectAsync(ns As NetworkStream, target As String, targetPort As Integer, tok As CancellationToken) As Task(Of UpstreamDial)
            Dim authority = $"{target}:{targetPort}"
            Dim sb As New StringBuilder()
            sb.Append("CONNECT ").Append(authority).Append(" HTTP/1.1").Append(vbCrLf)
            sb.Append("Host: ").Append(authority).Append(vbCrLf)
            If HasCredentials Then
                sb.Append("Proxy-Authorization: Basic ").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(_user & ":" & _pass))).Append(vbCrLf)
            End If
            sb.Append(vbCrLf)
            Dim req = Encoding.ASCII.GetBytes(sb.ToString())
            Await ns.WriteAsync(req, 0, req.Length, tok).ConfigureAwait(False)

            ' header block, byte by byte so nothing of the tunnel after it is swallowed
            Dim head As New List(Of Byte)()
            Dim one(0) As Byte
            Do
                Dim n = Await ns.ReadAsync(one, 0, 1, tok).ConfigureAwait(False)
                If n <= 0 Then Return Bad(UpstreamProblem.Protocol, 1, "HTTP yanıtı yarım kaldı")
                head.Add(one(0))
                If head.Count >= 4 AndAlso head(head.Count - 4) = 13 AndAlso head(head.Count - 3) = 10 AndAlso
                   head(head.Count - 2) = 13 AndAlso head(head.Count - 1) = 10 Then Exit Do
                If head.Count > 16384 Then Return Bad(UpstreamProblem.Protocol, 1, "HTTP yanıtı çok uzun")
            Loop
            Dim status = HttpStatus(Encoding.ASCII.GetString(head.ToArray()))
            Select Case status
                Case 200 To 299 : Return New UpstreamDial()
                Case 407
                    Return Bad(If(HasCredentials, UpstreamProblem.AuthFailed, UpstreamProblem.AuthRequired), 2, "HTTP 407")
                Case -1 : Return Bad(UpstreamProblem.Protocol, 1, "HTTP yanıtı değil")
                Case 403, 405 : Return Bad(UpstreamProblem.Target, 2, $"HTTP {status}")
                Case 502, 503, 504 : Return Bad(UpstreamProblem.Target, If(status = 504, CByte(6), CByte(4)), $"HTTP {status}")
                Case Else : Return Bad(UpstreamProblem.Target, 1, $"HTTP {status}")
            End Select
        End Function

        ''' <summary>Status code of "HTTP/1.x NNN …"; -1 if not an HTTP status line.</summary>
        Friend Shared Function HttpStatus(head As String) As Integer
            If head Is Nothing OrElse Not head.StartsWith("HTTP/1.", StringComparison.Ordinal) Then Return -1
            Dim parts = head.Split({" "c}, 3)
            Dim code As Integer
            If parts.Length < 2 OrElse parts(1).Length < 3 OrElse Not Integer.TryParse(parts(1).Substring(0, 3), code) Then Return -1
            Return code
        End Function

        Private Shared Function Bad(p As UpstreamProblem, reply As Byte, detail As String) As UpstreamDial
            Return New UpstreamDial With {.Problem = p, .Reply = reply, .Detail = detail}
        End Function

    End Class

End Namespace
