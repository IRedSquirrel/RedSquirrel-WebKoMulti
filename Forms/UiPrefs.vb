Imports System.IO
Imports Newtonsoft.Json.Linq

''' <summary>
''' The UI's own small settings (not secret): which account row is the leader and the input mirror's mode.
''' ..\Settings\multi-ui.json, read-modify-write, atomic. Kept apart from AccountStore (GameAccount has no leader field).
''' </summary>
Friend Module UiPrefs

    Private ReadOnly _lock As New Object()

    Private Function FilePath() As String
        Return Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Settings", "multi-ui.json"))
    End Function

    Private Function Load() As JObject
        Try
            Dim p As String = FilePath()
            If Not File.Exists(p) Then Return New JObject()
            Return JObject.Parse(File.ReadAllText(p))
        Catch ex As Exception
            Services.Logging.BotLog.Warn("arayüz", "Arayüz ayarı okunamadı: " & ex.Message)
            Return New JObject()
        End Try
    End Function

    Private Sub SetValue(key As String, value As JToken)
        SyncLock _lock
            Try
                Dim o As JObject = Load()
                o(key) = value
                Dim p As String = FilePath()
                Directory.CreateDirectory(Path.GetDirectoryName(p))
                Dim tmp As String = p & ".tmp"
                File.WriteAllText(tmp, o.ToString())
                If File.Exists(p) Then File.Replace(tmp, p, Nothing) Else File.Move(tmp, p)
            Catch ex As Exception
                Services.Logging.BotLog.Warn("arayüz", "Arayüz ayarı yazılamadı: " & ex.Message)
            End Try
        End SyncLock
    End Sub

    Friend Function LeaderIndex(demo As Boolean) As Integer
        If demo Then Return -1
        Dim t As JToken = Load()("leader")
        Return If(t IsNot Nothing AndAlso t.Type = JTokenType.Integer, CInt(t), -1)
    End Function

    Friend Sub SaveLeaderIndex(index As Integer)
        SetValue("leader", index)
    End Sub

    ''' <summary>True = keyboard + mouse; False (default, also when missing / unreadable) = keyboard only.</summary>
    Friend Function MirrorMouse() As Boolean
        Dim t As JToken = Load()("mirrorMode")
        Return t IsNot Nothing AndAlso t.Type = JTokenType.String AndAlso CStr(t) = "keyboard_mouse"
    End Function

    Friend Sub SaveMirrorMouse(withMouse As Boolean)
        SetValue("mirrorMode", If(withMouse, "keyboard_mouse", "keyboard"))
    End Sub

End Module
