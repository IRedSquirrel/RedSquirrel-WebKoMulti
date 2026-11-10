Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Newtonsoft.Json.Linq
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Services.Accounts

    ''' <summary>
    ''' Accounts (label, ID, password, character slot, enabled, useVpn) on disk: ..\Settings\multi-accounts.dat (next to the exe's folder).
    ''' File = "WMA1" + DPAPI CurrentUser(UTF8 JSON) — readable only by this Windows user on this PC. JSON is written / read by hand
    ''' (JObject; no reflection to POCO — reflection-based deserialization breaks under packers / obfuscators). Atomic write (.tmp + Replace). A corrupt file is moved aside
    ''' (multi-accounts.dat.bozuk-&lt;time&gt;) and Load returns an empty list. The log gets counts only — never an ID or a password.
    ''' </summary>
    Public Module AccountStore

        Private ReadOnly Magic As Byte() = Encoding.ASCII.GetBytes("WMA1")
        Private ReadOnly Entropy As Byte() = Encoding.UTF8.GetBytes("RedSquirrel.WebKoMulti.accounts.v1")
        Private ReadOnly _lock As New Object()

        ''' <summary>Test hook: another file instead of ..\Settings\multi-accounts.dat (self-test). Nothing = the real file.</summary>
        Friend PathOverride As String = Nothing

        Public Function FilePath() As String
            If Not String.IsNullOrEmpty(PathOverride) Then Return PathOverride
            Return Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Settings", "multi-accounts.dat"))
        End Function

        Public Function Load() As List(Of GameAccount)
            SyncLock _lock
                Dim file As String = FilePath()
                Dim list As New List(Of GameAccount)()
                If Not IO.File.Exists(file) Then Return list
                Try
                    Dim raw As Byte() = IO.File.ReadAllBytes(file)
                    list = FromJson(Unprotect(raw))
                    BotLog.Add("hesap", $"Hesaplar yüklendi: {list.Count} hesap")
                    Return list
                Catch ex As Exception
                    Dim aside As String = file & ".bozuk-" & DateTime.Now.ToString("yyyyMMdd-HHmmss")
                    Try
                        IO.File.Move(file, aside)
                    Catch
                    End Try
                    BotLog.Fail("hesap", $"Hesap dosyası okunamadı, kenara alındı ({Path.GetFileName(aside)}) — hesaplar boş başlıyor", ex)
                    Return New List(Of GameAccount)()
                End Try
            End SyncLock
        End Function

        Public Function Save(accounts As IList(Of GameAccount)) As Boolean
            SyncLock _lock
                Dim file As String = FilePath()
                Dim tmp As String = file & ".tmp"
                Try
                    Directory.CreateDirectory(Path.GetDirectoryName(file))
                    Dim data As Byte() = Protect(ToJson(If(accounts, New List(Of GameAccount)())))
                    IO.File.WriteAllBytes(tmp, data)
                    If IO.File.Exists(file) Then
                        IO.File.Replace(tmp, file, Nothing)
                    Else
                        IO.File.Move(tmp, file)
                    End If
                    BotLog.Add("hesap", $"Hesaplar kaydedildi: {If(accounts Is Nothing, 0, accounts.Count)} hesap")
                    Return True
                Catch ex As Exception
                    Try
                        If IO.File.Exists(tmp) Then IO.File.Delete(tmp)
                    Catch
                    End Try
                    BotLog.Fail("hesap", "Hesaplar kaydedilemedi", ex)
                    Return False
                End Try
            End SyncLock
        End Function

        ' ---- JSON by hand (side-effect free; Friend for the self-test) ----

        Friend Function ToJson(accounts As IList(Of GameAccount)) As String
            Dim arr As New JArray()
            For Each a In accounts
                If a Is Nothing Then Continue For
                arr.Add(New JObject From {
                    {"label", If(a.Label, "")},
                    {"userId", If(a.UserId, "")},
                    {"password", If(a.Password, "")},
                    {"characterSlot", a.CharacterSlot},
                    {"enabled", a.Enabled},
                    {"useVpn", a.UseVpn}
                })
            Next
            Return New JObject From {{"version", 1}, {"accounts", arr}}.ToString(Newtonsoft.Json.Formatting.None)
        End Function

        Friend Function FromJson(json As String) As List(Of GameAccount)
            Dim list As New List(Of GameAccount)()
            Dim root As JObject = JObject.Parse(json)
            Dim arr As JArray = TryCast(Get_(root, "accounts"), JArray)
            If arr Is Nothing Then Return list
            For Each t In arr
                Dim o As JObject = TryCast(t, JObject)
                If o Is Nothing Then Continue For
                Dim slot As Integer = 1
                Dim st = Get_(o, "characterSlot")
                If st IsNot Nothing AndAlso (st.Type = JTokenType.Integer OrElse st.Type = JTokenType.Float) Then slot = CInt(st)
                Dim vpn As Boolean = False   ' missing in files written before the field existed → False
                Dim vt = Get_(o, "useVpn")
                If vt IsNot Nothing AndAlso vt.Type = JTokenType.Boolean Then vpn = CBool(vt)
                Dim en As Boolean = True
                Dim et = Get_(o, "enabled")
                If et IsNot Nothing AndAlso et.Type = JTokenType.Boolean Then en = CBool(et)
                list.Add(New GameAccount With {
                    .Label = Str(o, "label"),
                    .UserId = Str(o, "userId"),
                    .Password = Str(o, "password"),
                    .CharacterSlot = Math.Max(1, Math.Min(4, slot)),
                    .Enabled = en,
                    .UseVpn = vpn
                })
            Next
            Return list
        End Function

        Private Function Get_(o As JObject, key As String) As JToken
            Return o.GetValue(key, StringComparison.OrdinalIgnoreCase)
        End Function

        Private Function Str(o As JObject, key As String) As String
            Dim t = Get_(o, key)
            If t Is Nothing OrElse t.Type = JTokenType.Null Then Return ""
            Return t.ToString()
        End Function

        Friend Function Protect(json As String) As Byte()
            Dim enc As Byte() = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), Entropy, DataProtectionScope.CurrentUser)
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
