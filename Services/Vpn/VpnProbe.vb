Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Net.Sockets
Imports System.Security.Authentication
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Namespace Services.Vpn

    ''' <summary>A SOCKS5 client (CONNECT) — the exit-IP check and the self-test talk to the bridge with it.</summary>
    Friend Module SocksClient

        ''' <summary>Connected stream to host:port through the SOCKS5 proxy on 127.0.0.1:proxyPort. Throws SocksException.</summary>
        Friend Async Function ConnectAsync(proxyPort As Integer, host As String, port As Integer, tok As CancellationToken) As Task(Of TcpClient)
            Dim c As New TcpClient()
            Try
                Await c.ConnectAsync(IPAddress.Loopback, proxyPort).ConfigureAwait(False)
                Dim s = c.GetStream()
                Await s.WriteAsync(New Byte() {5, 1, 0}, 0, 3, tok).ConfigureAwait(False)
                Dim m = Await SocksBridge.ReadExactAsync(s, 2, tok).ConfigureAwait(False)
                If m(0) <> 5 OrElse m(1) <> 0 Then Throw New SocksException(255)
                Dim req As New List(Of Byte) From {5, 1, 0}
                Dim lit As IPAddress = Nothing
                If IPAddress.TryParse(host, lit) AndAlso lit.AddressFamily = AddressFamily.InterNetwork Then
                    req.Add(1)
                    req.AddRange(lit.GetAddressBytes())
                Else
                    Dim hb = Encoding.ASCII.GetBytes(host)
                    req.Add(3)
                    req.Add(CByte(hb.Length))
                    req.AddRange(hb)
                End If
                req.Add(CByte((port >> 8) And &HFF))
                req.Add(CByte(port And &HFF))
                Await s.WriteAsync(req.ToArray(), 0, req.Count, tok).ConfigureAwait(False)
                Dim r = Await SocksBridge.ReadExactAsync(s, 10, tok).ConfigureAwait(False)
                If r(1) <> 0 Then Throw New SocksException(r(1))
                Return c
            Catch
                c.Close()
                Throw
            End Try
        End Function

    End Module

    Friend Class SocksException
        Inherits Exception
        Public ReadOnly Code As Byte
        Public Sub New(code As Byte)
            MyBase.New(Describe(code))
            Me.Code = code
        End Sub
        Friend Shared Function Describe(code As Byte) As String
            Select Case code
                Case 2 : Return "köprü bağlantıyı reddetti (VPN kapalı)"
                Case 3 : Return "VPN arayüzünden ağa ulaşılamadı (yol yok)"
                Case 4 : Return "hedef bulunamadı"
                Case 5 : Return "hedef bağlantıyı reddetti"
                Case 6 : Return "zaman aşımı"
                Case 7 : Return "komut desteklenmiyor"
                Case 8 : Return "adres türü desteklenmiyor"
                Case 255 : Return "köprü yanıtı tanınmadı"
                Case Else : Return $"köprü hatası {code}"
            End Select
        End Function
    End Class

    ''' <summary>
    ''' "What IP does the internet see?" — GET https://api.ipify.org once through the bridge (= through the VPN) and once
    ''' directly. Different = the bridge really leaves through the VPN. Only the IP comes back; nothing is sent but the request.
    ''' </summary>
    Friend Module ExitIpProbe

        Friend Const Host As String = "api.ipify.org"
        Friend Property TimeoutMs As Integer = 15000

        Friend Async Function ViaBridgeAsync(proxyPort As Integer, tok As CancellationToken) As Task(Of IPAddress)
            Using c = Await SocksClient.ConnectAsync(proxyPort, Host, 443, tok).ConfigureAwait(False)
                Return Await FetchAsync(c.GetStream(), tok).ConfigureAwait(False)
            End Using
        End Function

        Friend Async Function DirectAsync(tok As CancellationToken) As Task(Of IPAddress)
            Using c As New TcpClient(AddressFamily.InterNetwork)
                Await c.ConnectAsync(Host, 443).ConfigureAwait(False)
                Return Await FetchAsync(c.GetStream(), tok).ConfigureAwait(False)
            End Using
        End Function

        Private Async Function FetchAsync(raw As Stream, tok As CancellationToken) As Task(Of IPAddress)
            Using cts = CancellationTokenSource.CreateLinkedTokenSource(tok)
                cts.CancelAfter(TimeoutMs)
                Using reg = cts.Token.Register(Sub() raw.Dispose())
                    Using tls As New SslStream(raw, False)
                        Await tls.AuthenticateAsClientAsync(Host, Nothing, SslProtocols.Tls12, True).ConfigureAwait(False)
                        Dim req = Encoding.ASCII.GetBytes($"GET / HTTP/1.1{vbCrLf}Host: {Host}{vbCrLf}User-Agent: RedSquirrel.WebKoMulti{vbCrLf}Connection: close{vbCrLf}{vbCrLf}")
                        Await tls.WriteAsync(req, 0, req.Length, cts.Token).ConfigureAwait(False)
                        Dim ms As New MemoryStream()
                        Dim buf(4095) As Byte
                        Do
                            Dim n = Await tls.ReadAsync(buf, 0, buf.Length, cts.Token).ConfigureAwait(False)
                            If n <= 0 Then Exit Do
                            ms.Write(buf, 0, n)
                            If ms.Length > 65536 Then Exit Do
                        Loop
                        Return ParseHttpIp(Encoding.ASCII.GetString(ms.ToArray()))
                    End Using
                End Using
            End Using
        End Function

        ''' <summary>IPv4 in the body of an HTTP 200 (plain or chunked); Nothing otherwise.</summary>
        Friend Function ParseHttpIp(resp As String) As IPAddress
            If resp Is Nothing OrElse Not resp.StartsWith("HTTP/1.", StringComparison.Ordinal) Then Return Nothing
            Dim firstLine = resp.Substring(0, Math.Max(0, resp.IndexOf(vbCrLf, StringComparison.Ordinal)))
            If Not firstLine.Contains(" 200") Then Return Nothing
            Dim i = resp.IndexOf(vbCrLf & vbCrLf, StringComparison.Ordinal)
            If i < 0 Then Return Nothing
            For Each part In resp.Substring(i + 4).Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                Dim a As IPAddress = Nothing
                If IPAddress.TryParse(part.Trim(), a) AndAlso a.AddressFamily = AddressFamily.InterNetwork AndAlso part.Trim().Count(Function(ch) ch = "."c) = 3 Then Return a
            Next
            Return Nothing
        End Function

    End Module

End Namespace
