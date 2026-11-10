Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms
Imports RedSquirrel.WebKoMulti.Core.Input
Imports RedSquirrel.WebKoMulti.Core.Vision

Namespace Engine.Login

    ''' <summary>
    ''' MEASUREMENT harness (no account, nothing typed, nothing submitted): opens one view on its own profile folder, presses START
    ''' with the production launcher code, and once the game frame is shown saves a frame every <c>everyMs</c> to <c>outDir</c>.
    ''' Used to measure the game's own login screen (LOGIN.md). Call from a test host (PowerShell 5.1 loading the exe as a library):
    ''' Engine.Login.LoginProbe.Run(profileDir, outDir, seconds). Blocks; runs its own message loop.
    ''' </summary>
    Public Module LoginProbe

        ''' <summary>
        ''' The PRODUCTION chain (WebLogin.RunAsync) with a DUMMY account and the StopBeforeSubmit hook: launcher → login box → dummy
        ''' ID + dummy password typed and SEEN → frame saved → both boxes emptied → returns. Enter / OK are never pressed.
        ''' </summary>
        Public Function RunChain(profileDir As String, seconds As Integer, Optional viewWidth As Integer = 1280, Optional viewHeight As Integer = 720) As String
            Dim report As New Text.StringBuilder()
            Dim f As New Form With {.Text = "login-chain-probe", .ShowInTaskbar = False, .FormBorderStyle = FormBorderStyle.FixedToolWindow,
                                    .StartPosition = FormStartPosition.Manual}
            f.ClientSize = New Size(viewWidth, viewHeight)
            f.Location = New Point(-viewWidth - 200, 40)
            Dim view As New WebView2 With {.Dock = DockStyle.Fill}
            f.Controls.Add(view)
            AddHandler f.Shown, Async Sub(s, e)
                                    Dim cts As New CancellationTokenSource(seconds * 1000)
                                    Try
                                        Directory.CreateDirectory(profileDir)
                                        Dim opts As New CoreWebView2EnvironmentOptions("--disable-background-timer-throttling --disable-renderer-backgrounding --disable-backgrounding-occluded-windows")
                                        Dim env = Await CoreWebView2Environment.CreateAsync(Nothing, profileDir, opts)
                                        Await view.EnsureCoreWebView2Async(env)
                                        Dim acc As New Core.Session.GameAccount With {.Label = "sınama", .UserId = "probe_x9", .Password = "Abc12345", .CharacterSlot = 1}
                                        Dim session As New Core.Session.GameSession(97, view, acc)
                                        AddHandler session.Changed, Sub(ss) report.AppendLine($"durum: {ss.State} — {ss.StatusText}")
                                        WebLogin.StopBeforeSubmit = True
                                        WebLogin.LastTestClearColumns.Clear()
                                        Dim ok = Await WebLogin.RunAsync(session, cts.Token)
                                        report.AppendLine($"sonuç: {ok} · silindikten sonra sütun (şifre, ID): {String.Join(",", WebLogin.LastTestClearColumns)}")
                                    Catch ex As Exception
                                        report.AppendLine("HATA " & ex.ToString())
                                    Finally
                                        WebLogin.StopBeforeSubmit = False
                                        f.Close()
                                    End Try
                                End Sub
            Application.Run(f)
            Return report.ToString()
        End Function

        Public Function Run(profileDir As String, outDir As String, seconds As Integer, Optional viewWidth As Integer = 1280, Optional viewHeight As Integer = 720,
                            Optional everyMs As Integer = 2000, Optional onScreen As Boolean = False) As String
            Dim report As New Text.StringBuilder()
            Dim f As New Form With {.Text = "login-probe", .ShowInTaskbar = False, .FormBorderStyle = FormBorderStyle.FixedToolWindow,
                                    .StartPosition = FormStartPosition.Manual}
            f.ClientSize = New Size(viewWidth, viewHeight)
            f.Location = If(onScreen, New Point(40, 40), New Point(-viewWidth - 200, 40))
            Dim view As New WebView2 With {.Dock = DockStyle.Fill}
            f.Controls.Add(view)
            Directory.CreateDirectory(outDir)
            AddHandler f.Shown, Async Sub(s, e)
                                    Try
                                        Await ProbeAsync(view, profileDir, outDir, seconds, everyMs, report)
                                    Catch ex As Exception
                                        report.AppendLine("HATA " & ex.ToString())
                                    Finally
                                        f.Close()
                                    End Try
                                End Sub
            Application.Run(f)
            Return report.ToString()
        End Function

        Private Async Function ProbeAsync(view As WebView2, profileDir As String, outDir As String, seconds As Integer, everyMs As Integer,
                                          report As Text.StringBuilder) As Task
            Directory.CreateDirectory(profileDir)
            Dim opts As New CoreWebView2EnvironmentOptions("--disable-background-timer-throttling --disable-renderer-backgrounding --disable-backgrounding-occluded-windows")
            Dim env = Await CoreWebView2Environment.CreateAsync(Nothing, profileDir, opts)
            Await view.EnsureCoreWebView2Async(env)
            Dim cw = view.CoreWebView2
            cw.Navigate(LauncherPage.HomeUrl)
            Dim sw = Stopwatch.StartNew()
            Dim pressed As Boolean = False
            Dim shownAt As Long = -1
            Dim n As Integer = 0
            Dim lastShot As Long = -everyMs
            While sw.ElapsedMilliseconds < seconds * 1000L
                Await Task.Delay(500)
                If shownAt < 0 Then
                    Dim st As LauncherPage.LauncherState = Nothing
                    Try
                        st = Await LauncherPage.ReadAsync(cw)
                    Catch ex As Exception
                        report.AppendLine($"{sw.ElapsedMilliseconds} okuma hatası {ex.Message}")
                        Continue While
                    End Try
                    Dim stp = LauncherPage.Decide(st, pressed)
                    report.AppendLine($"{sw.ElapsedMilliseconds} {stp} start={st.HasStart}/{st.StartEnabled}@{st.StartPoint} p={st.Progress:0.00} '{st.StatusText}' view={st.ViewSize}")
                    Select Case stp
                        Case LauncherPage.LauncherStep.PressStart, LauncherPage.LauncherStep.RetryStart
                            Await CdpInput.ClickAsync(cw, st.StartPoint.X, st.StartPoint.Y)
                            pressed = True
                            Await Task.Delay(1000)
                        Case LauncherPage.LauncherStep.GameShown
                            shownAt = sw.ElapsedMilliseconds
                        Case LauncherPage.LauncherStep.DirectPlay, LauncherPage.LauncherStep.WrongPage
                            report.AppendLine("çıkış: " & stp.ToString())
                            Return
                    End Select
                    If shownAt < 0 Then Continue While
                End If
                If sw.ElapsedMilliseconds - lastShot >= everyMs Then
                    lastShot = sw.ElapsedMilliseconds
                    Using bmp = Await ViewCapture.FullAsync(cw)
                        If bmp IsNot Nothing Then
                            n += 1
                            Dim file = Path.Combine(outDir, $"probe-{n:000}-{(sw.ElapsedMilliseconds - shownAt) \ 1000}s.png")
                            bmp.Save(file, Imaging.ImageFormat.Png)
                            Dim kind = LoginScreens.Classify(bmp)
                            report.AppendLine($"{sw.ElapsedMilliseconds} kare {Path.GetFileName(file)} {bmp.Width}x{bmp.Height} → {kind.Kind} {kind.Detail}")
                        End If
                    End Using
                End If
            End While
        End Function

    End Module

End Namespace
