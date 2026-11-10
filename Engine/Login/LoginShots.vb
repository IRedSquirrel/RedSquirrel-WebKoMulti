Imports System.IO
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Session
Imports RedSquirrel.WebKoMulti.Core.Vision
Imports RedSquirrel.WebKoMulti.Services.Logging

Namespace Engine.Login

    ''' <summary>
    ''' Measurement frames of the login chain: &lt;exe&gt;\..\Logs\multi-login\&lt;N&gt;-&lt;time&gt;-&lt;tag&gt;.png (7 days, like the log).
    ''' The chain saves one at every screen it does not know and on every failure — the user's first live run brings the
    ''' frames LOGIN.md marks "ölçülmedi". Frames are NOT saved while the typed ID is visible in the box (the account name
    ''' stays out of files, like the log). <see cref="SaveAsync"/> is also the "login-shot" for a UI button.
    ''' </summary>
    Public Module LoginShots

        Private Const KeepDays As Integer = 7

        Public Function Folder() As String
            Return Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Logs", "multi-login"))
        End Function

        ''' <summary>Captures the view and saves it; returns the file path or "" (never throws).</summary>
        Public Async Function SaveAsync(session As GameSession, tag As String) As Task(Of String)
            Try
                If session Is Nothing OrElse Not session.Ready Then Return ""
                Using bmp = Await ViewCapture.FullAsync(session.Cw)
                    Return Save(bmp, session.Number, tag)
                End Using
            Catch ex As Exception
                BotLog.FailOnce("login-shot", ex)
                Return ""
            End Try
        End Function

        Public Function Save(bmp As Bitmap, sessionNumber As Integer, tag As String) As String
            Try
                If bmp Is Nothing Then Return ""
                Dim dir = Folder()
                Directory.CreateDirectory(dir)
                Cleanup(dir)
                Dim safe = New String(If(tag, "kare").Select(Function(c) If(Char.IsLetterOrDigit(c) OrElse c = "-"c, c, "_"c)).ToArray())
                Dim file = Path.Combine(dir, $"{sessionNumber}-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{safe}.png")
                bmp.Save(file, Imaging.ImageFormat.Png)
                BotLog.Add("giris", $"{sessionNumber}. görünüm: kare kaydedildi ({Path.GetFileName(file)}, {bmp.Width}×{bmp.Height})")
                Return file
            Catch ex As Exception
                BotLog.FailOnce("login-shot-save", ex)
                Return ""
            End Try
        End Function

        Private _cleaned As Boolean
        Private Sub Cleanup(dir As String)
            If _cleaned Then Return
            _cleaned = True
            Try
                For Each f In Directory.GetFiles(dir, "*.png")
                    If File.GetLastWriteTime(f) < DateTime.Now.AddDays(-KeepDays) Then File.Delete(f)
                Next
            Catch
            End Try
        End Sub

    End Module

End Namespace
