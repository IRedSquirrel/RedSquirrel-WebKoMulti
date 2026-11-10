Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Newtonsoft.Json.Linq
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Services.Vpn

    ''' <summary>How the "VPN ile gir" accounts leave: Windows PPTP VPN (default, old files) or a user-supplied upstream proxy.</summary>
    Public Enum VpnMode
        Pptp
        Proxy
    End Enum

    ''' <summary>VPN settings: PPTP (server, user name, password) + upstream proxy (type, host, port, user name, password) + mode.</summary>
    Public Class VpnSettings
        Public Property Mode As VpnMode = VpnMode.Pptp
        Public Property Server As String = ""
        Public Property UserName As String = ""
        Public Property Password As String = ""
        Public Property ProxyType As ProxyKind = ProxyKind.Socks5
        Public Property ProxyHost As String = ""
        Public Property ProxyPort As Integer = 0
        Public Property ProxyUser As String = ""
        Public Property ProxyPassword As String = ""
    End Class

    ''' <summary>
    ''' ..\Settings\multi-vpn.dat = "WMV1" + DPAPI CurrentUser(UTF8 JSON), JSON by hand, atomic write (.tmp + Replace) —
    ''' the AccountStore pattern. The log gets the mode + server / proxy host:port only, never a user name or a password.
    ''' </summary>
    Public Module VpnStore

        Private ReadOnly Magic As Byte() = Encoding.ASCII.GetBytes("WMV1")
        Private ReadOnly Entropy As Byte() = Encoding.UTF8.GetBytes("RedSquirrel.WebKoMulti.vpn.v1")
        Private ReadOnly _lock As New Object()

        ''' <summary>Test hook: another file instead of ..\Settings\multi-vpn.dat. Nothing = the real file.</summary>
        Friend PathOverride As String = Nothing

        Public Function FilePath() As String
            If Not String.IsNullOrEmpty(PathOverride) Then Return PathOverride
            Return Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Settings", "multi-vpn.dat"))
        End Function

        Public Function Load() As VpnSettings
            SyncLock _lock
                Dim file = FilePath()
                If Not IO.File.Exists(file) Then Return New VpnSettings()
                Try
                    Dim s = FromJson(Unprotect(IO.File.ReadAllBytes(file)))
                    BotLog.Add("vpn", "VPN ayarları yüklendi — " & Summary(s))
                    Return s
                Catch ex As Exception
                    Dim aside = file & ".bozuk-" & DateTime.Now.ToString("yyyyMMdd-HHmmss")
                    Try
                        IO.File.Move(file, aside)
                    Catch
                    End Try
                    BotLog.Fail("vpn", $"VPN ayar dosyası okunamadı, kenara alındı ({Path.GetFileName(aside)}) — ayarlar boş başlıyor", ex)
                    Return New VpnSettings()
                End Try
            End SyncLock
        End Function

        Public Function Save(s As VpnSettings) As Boolean
            SyncLock _lock
                Dim file = FilePath()
                Dim tmp = file & ".tmp"
                Try
                    Directory.CreateDirectory(Path.GetDirectoryName(file))
                    IO.File.WriteAllBytes(tmp, Protect(ToJson(If(s, New VpnSettings()))))
                    If IO.File.Exists(file) Then
                        IO.File.Replace(tmp, file, Nothing)
                    Else
                        IO.File.Move(tmp, file)
                    End If
                    BotLog.Add("vpn", "VPN ayarları kaydedildi — " & Summary(If(s, New VpnSettings())))
                    Return True
                Catch ex As Exception
                    Try
                        If IO.File.Exists(tmp) Then IO.File.Delete(tmp)
                    Catch
                    End Try
                    BotLog.Fail("vpn", "VPN ayarları kaydedilemedi", ex)
                    Return False
                End Try
            End SyncLock
        End Function

        ''' <summary>Log text: mode + server / proxy host:port only — never a user name or a password.</summary>
        Friend Function Summary(s As VpnSettings) As String
            If s.Mode = VpnMode.Proxy Then
                Return "kip proxy" & If(s.ProxyHost <> "", $" — {UpstreamProxyEgress.KindText(s.ProxyType)} {s.ProxyHost}:{s.ProxyPort}", " — proxy adresi boş")
            End If
            Return "kip PPTP" & If(s.Server <> "", $" — sunucu {s.Server}", " — sunucu boş")
        End Function

        Friend Function ModeText(m As VpnMode) As String
            Return If(m = VpnMode.Proxy, "proxy", "pptp")
        End Function

        Friend Function ProxyTypeText(k As ProxyKind) As String
            Return If(k = ProxyKind.Http, "http", "socks5")
        End Function

        Friend Function ToJson(s As VpnSettings) As String
            Return New JObject From {
                {"version", 2},
                {"mode", ModeText(s.Mode)},
                {"server", If(s.Server, "")},
                {"userName", If(s.UserName, "")},
                {"password", If(s.Password, "")},
                {"proxyType", ProxyTypeText(s.ProxyType)},
                {"proxyHost", If(s.ProxyHost, "")},
                {"proxyPort", s.ProxyPort},
                {"proxyUser", If(s.ProxyUser, "")},
                {"proxyPassword", If(s.ProxyPassword, "")}
            }.ToString(Newtonsoft.Json.Formatting.None)
        End Function

        ''' <summary>Reads v1 (PPTP only — no "mode" = PPTP) and v2 files; unknown / missing values fall back to the defaults.</summary>
        Friend Function FromJson(json As String) As VpnSettings
            Dim o = JObject.Parse(json)
            Dim port As Integer = 0
            Dim pt = o.GetValue("proxyPort", StringComparison.OrdinalIgnoreCase)
            If pt IsNot Nothing AndAlso pt.Type <> JTokenType.Null Then Integer.TryParse(pt.ToString(), port)
            Return New VpnSettings With {
                .Mode = If(String.Equals(Str(o, "mode"), "proxy", StringComparison.OrdinalIgnoreCase), VpnMode.Proxy, VpnMode.Pptp),
                .Server = Str(o, "server"), .UserName = Str(o, "userName"), .Password = Str(o, "password"),
                .ProxyType = If(String.Equals(Str(o, "proxyType"), "http", StringComparison.OrdinalIgnoreCase), ProxyKind.Http, ProxyKind.Socks5),
                .ProxyHost = Str(o, "proxyHost"), .ProxyPort = If(port >= 0 AndAlso port <= 65535, port, 0),
                .ProxyUser = Str(o, "proxyUser"), .ProxyPassword = Str(o, "proxyPassword")}
        End Function

        Private Function Str(o As JObject, key As String) As String
            Dim t = o.GetValue(key, StringComparison.OrdinalIgnoreCase)
            If t Is Nothing OrElse t.Type = JTokenType.Null Then Return ""
            Return t.ToString()
        End Function

        Friend Function Protect(json As String) As Byte()
            Dim enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), Entropy, DataProtectionScope.CurrentUser)
            Dim result(Magic.Length + enc.Length - 1) As Byte
            Buffer.BlockCopy(Magic, 0, result, 0, Magic.Length)
            Buffer.BlockCopy(enc, 0, result, Magic.Length, enc.Length)
            Return result
        End Function

        Friend Function Unprotect(raw As Byte()) As String
            If raw Is Nothing OrElse raw.Length <= Magic.Length Then Throw New InvalidDataException("dosya kısa")
            For i = 0 To Magic.Length - 1
                If raw(i) <> Magic(i) Then Throw New InvalidDataException("tanınmayan dosya başlığı")
            Next
            Dim enc(raw.Length - Magic.Length - 1) As Byte
            Buffer.BlockCopy(raw, Magic.Length, enc, 0, enc.Length)
            Return Encoding.UTF8.GetString(ProtectedData.Unprotect(enc, Entropy, DataProtectionScope.CurrentUser))
        End Function

    End Module

End Namespace
