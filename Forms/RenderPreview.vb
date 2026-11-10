#If DEBUG Then
Imports System.Drawing.Imaging
Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>
''' Debug: `RedSquirrel.WebKoMulti.exe render &lt;png&gt; [hesaplar|oyunlar|takip|kayit|vpn|vpn-hata|vpn-proxy|vpn-proxy-hata|pencere|lider|lider-yansitma|pencere-yansitma|takip-yansitma (+ -fare)]` — opens the window OFF SCREEN with sample
''' accounts (nothing read from or written to disk, nothing logged, no browser created) and draws it with DrawToBitmap.
''' No screen copy (only the app's own window is drawn). "pencere" = the followers' game window, "lider" = the leader window.
''' </summary>
Friend Module RenderPreview

    Private Function DemoAccounts() As List(Of GameAccount)
        Return New List(Of GameAccount) From {
            New GameAccount With {.Label = "Okçu", .UserId = "ornek_okcu", .Password = "x", .CharacterSlot = 1, .Enabled = True},
            New GameAccount With {.Label = "Priest", .UserId = "ornek_priest", .Password = "x", .CharacterSlot = 2, .Enabled = True, .UseVpn = True},
            New GameAccount With {.Label = "Warrior", .UserId = "ornek_war", .Password = "x", .CharacterSlot = 1, .Enabled = True, .UseVpn = True},
            New GameAccount With {.Label = "Mage", .UserId = "ornek_mage", .Password = "x", .CharacterSlot = 3, .Enabled = True},
            New GameAccount With {.Label = "Yedek", .UserId = "ornek_yedek", .Password = "", .CharacterSlot = 1, .Enabled = False}}
    End Function

    Private Sub DemoStates(hub As SessionHub)
        Dim s = hub.Sessions
        s(0).SetStatus(SessionState.InGame, "Oyunda · 812, 541")
        s(1).SetStatus(SessionState.InGame, "Oyunda · 806, 538")
        s(2).SetStatus(SessionState.LoggingIn, "Karakter seçiliyor (1.)")
        s(3).SetStatus(SessionState.Failed, "Giriş reddedildi: şifre yanlış")
    End Sub

    Friend Function Run(png As String, page As String) As Integer
        Try
            Dim main As New Main(demo:=True)
            main.Hub.LoadDemo(DemoAccounts(), leader:=0)
            DemoStates(main.Hub)
            main.Hub.DemoVpn(Services.Vpn.VpnState.Connected, "Bağlı · ornek.vpnbook.com")
            main.VpnPage.DemoFields("ornek.vpnbook.com", "ornek", "ornek-sifre")
            main.FollowPage.DemoStatus(1, "Lidere yürüyor · 14 birim", DateTime.Today.AddHours(21).AddMinutes(4).AddSeconds(12))
            main.FollowPage.DemoStatus(2, "Oyunda değil — bekliyor", DateTime.Today.AddHours(21).AddMinutes(4).AddSeconds(9))
            main.LogPage.AddDemo("21:02:41", "INFO", "hesap", "Hesaplar yüklendi: 5 hesap, lider 1. hesap (Okçu)")
            main.LogPage.AddDemo("21:03:02", "INFO", "oturum", "Hepsini başlat: 4 hesap — 1, 2, 3, 4")
            main.LogPage.AddDemo("21:03:40", "INFO", "oturum", "1. hesap (Okçu): oyuna girildi")
            main.LogPage.AddDemo("21:03:44", "WARN", "oturum", "4. hesap (Mage): giriş tamamlanamadı — Giriş reddedildi: şifre yanlış")
            main.LogPage.AddDemo("21:04:01", "INFO", "takip", "Takip başladı: lider 1. hesap (Okçu), 3 takipçi")
            main.LogPage.AddDemo("21:04:20", "INFO", "hesap", "Lider: 1. hesap (Okçu) (önceki 2. hesap (Priest)) — takip yeni lidere döndü")

            Dim target As Form = main
            Select Case If(page, "").ToLowerInvariant()
                Case "oyunlar" : main.SelectTab(1)
                Case "takip" : main.SelectTab(2)
                Case "kayit", "kayıt" : main.SelectTab(4)
                Case "vpn" : main.SelectTab(3)
                Case "vpn-hata"
                    main.Hub.DemoVpn(Services.Vpn.VpnState.Failed, "Bağlanamadı: kullanıcı adı ya da şifre yanlış (691)")
                    main.SelectTab(3)
                Case "vpn-proxy"
                    main.Hub.DemoVpn(Services.Vpn.VpnState.Connected, "Proxy bağlı — SOCKS5 203.0.113.5:1080 · çıkış IP'si 203.0.113.5 (doğrudan: 198.51.100.7)")
                    main.VpnPage.DemoProxy(Services.Vpn.ProxyKind.Socks5, "203.0.113.5", 1080, "ornek", "ornek-sifre")
                    main.SelectTab(3)
                Case "vpn-proxy-hata"
                    main.Hub.DemoVpn(Services.Vpn.VpnState.Failed, "Proxy kullanıcı adı ya da şifresi yanlış — proxy sağlayıcınızın verdiği güncel bilgileri Ayarlar'a girin.")
                    main.VpnPage.DemoProxy(Services.Vpn.ProxyKind.Http, "proxy.ornek.test", 8080, "", "")
                    main.SelectTab(3)
                Case "pencere" : target = main.Games
                Case "lider" : target = main.Games.LeaderWin
                Case "lider-yansitma", "pencere-yansitma", "takip-yansitma", "lider-yansitma-fare", "pencere-yansitma-fare", "takip-yansitma-fare"
                    ' Keyboard only (default): follow keeps running. Keyboard + mouse: the engine may pause follow (its own text).
                    If page.EndsWith("-fare") Then
                        main.Hub.DemoMirror(True, "Yansıtma açık: 1 takipçi · takip duraklatıldı", "Tık 640,412 → 1 hesap", withMouse:=True)
                    Else
                        main.Hub.DemoMirror(True, "Yansıtma açık: 1 takipçi", "Tuş I → 1 hesap", withMouse:=False)
                    End If
                    If page.StartsWith("lider") Then
                        target = main.Games.LeaderWin
                    ElseIf page.StartsWith("pencere") Then
                        target = main.Games
                    Else
                        main.SelectTab(2)
                    End If
                Case Else : main.SelectTab(0)
            End Select

            main.StartPosition = FormStartPosition.Manual
            main.Location = New Point(-6000, -6000)
            main.ShowInTaskbar = False
            main.Show()
            If target IsNot main Then
                target.StartPosition = FormStartPosition.Manual
                target.ShowInTaskbar = False
                target.Location = New Point(-6000, -3000)
                If target Is main.Games Then target.Size = New Size(1284, 900)
                target.Show(main)
                main.Games.Relayout()
            End If
            For i As Integer = 1 To 10
                Application.DoEvents()
                Threading.Thread.Sleep(30)
            Next
            Using bmp As New Bitmap(target.Width, target.Height)
                target.DrawToBitmap(bmp, New Rectangle(Point.Empty, target.Size))
                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(IO.Path.GetFullPath(png)))
                bmp.Save(png, ImageFormat.Png)
            End Using
            main.Close()
            Console.WriteLine("OK " & png)
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine("render: " & ex.ToString())
            Return 1
        End Try
    End Function

End Module
#End If
