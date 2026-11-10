Friend Module Program

    <STAThread>
    Function Main(args As String()) As Integer
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
#If DEBUG Then
        ' Debug: RedSquirrel.WebKoMulti.exe render <png> [hesaplar|oyunlar|takip|kayit|pencere|lider] — off-screen picture of a page.
        If args IsNot Nothing AndAlso args.Length >= 2 AndAlso String.Equals(args(0), "render", StringComparison.OrdinalIgnoreCase) Then
            Return RenderPreview.Run(args(1), If(args.Length >= 3, args(2), ""))
        End If
        ' Debug: offline self-tests (no network, no input to any game).
        If args IsNot Nothing AndAlso args.Length >= 1 AndAlso String.Equals(args(0), "follow-selftest", StringComparison.OrdinalIgnoreCase) Then
            Return Engine.Follow.FollowSelfTest.Run(args.Skip(1).ToArray())
        End If
        If args IsNot Nothing AndAlso args.Length >= 1 AndAlso String.Equals(args(0), "vpn-selftest", StringComparison.OrdinalIgnoreCase) Then
            Dim vpnReport = Services.Vpn.VpnSelfTest.Run()
            Console.WriteLine(vpnReport)
            Return If(vpnReport.Contains("SONUC: GECTI"), 0, 1)
        End If
        If args IsNot Nothing AndAlso args.Length >= 1 AndAlso String.Equals(args(0), "login-selftest", StringComparison.OrdinalIgnoreCase) Then
            Dim report = Engine.Login.LoginSelfTest.Run(If(args.Length >= 2, args(1), Nothing))
            Console.WriteLine(report)
            Return If(report.Contains("SONUC: GECTI"), 0, 1)
        End If
#End If
        AddHandler Application.ThreadException, Sub(s, e) Services.Logging.BotLog.Warn("hata", "Yakalanmayan arayüz hatası: " & e.Exception.Message)
        ' A background-thread exception still ends the process; it must at least leave a line in the log.
        AddHandler AppDomain.CurrentDomain.UnhandledException,
            Sub(s, e) Services.Logging.BotLog.Fail("hata", "Yakalanmayan hata — program kapanıyor", TryCast(e.ExceptionObject, Exception))
        Application.Run(New Main())
        Return 0
    End Function

End Module
