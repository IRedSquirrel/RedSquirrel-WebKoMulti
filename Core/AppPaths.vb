Imports System.IO
Imports System.Reflection

' Core/AppPaths — exe dizini (namespace YOK → nitelemesiz erisim).
' Not: bir paketleyici/koruyucu assembly'yi bellekten yukleyince
' Assembly.Location / Application.StartupPath bos ya da yanlis doner. Once surecin
' ana modulu (diskteki gercek exe) sorulur, sonra digerleri.
Public Module AppPaths

    Private _exeDir As String

    ''' <summary>Exe'nin bulundugu dizin — ayar/log yollari buna gore. Bir kez bulunur.</summary>
    Public Function ExeDir() As String
        Dim cur As String = _exeDir
        If Not String.IsNullOrEmpty(cur) Then Return cur

        Dim adaylar As New List(Of Func(Of String)) From {
            Function() Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
            Function() AppDomain.CurrentDomain.BaseDirectory,
            Function() Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
            Function() Application.StartupPath
        }
        For Each uret In adaylar
            Try
                Dim d As String = uret()
                If Not String.IsNullOrWhiteSpace(d) Then
                    _exeDir = Path.GetFullPath(d.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                    Return _exeDir
                End If
            Catch
            End Try
        Next
        _exeDir = Directory.GetCurrentDirectory()
        Return _exeDir
    End Function

End Module
