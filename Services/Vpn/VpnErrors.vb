Namespace Services.Vpn

    ''' <summary>RAS error code → what the user should know, in Turkish (side-effect free).</summary>
    Friend Module VpnErrors

        Friend Function Describe(code As Integer) As String
            Select Case code
                Case 691
                    Return "Kullanıcı adı ya da şifre yanlış (691). VPNBook şifresi 1-2 haftada bir değişiyor — VPNBook sayfasından yenisini alıp Ayarlar'a girin."
                Case 800
                    Return "VPN sunucusuna bağlanılamadı (800): sunucu kapalı olabilir ya da ağınız / modeminiz PPTP'yi engelliyor olabilir. Başka bir sunucu deneyin."
                Case 807
                    Return "VPN bağlantısı kurulurken koptu (807): ağ gecikmesi ya da PPTP'nin engellenmesi olabilir. Başka bir sunucu deneyin."
                Case 806
                    Return "Sunucuya ulaşıldı ama PPTP'nin veri yolu (GRE) geçemedi (806): modeminiz ya da internet sağlayıcınız PPTP'yi engelliyor."
                Case 619
                    Return "VPN bağlantı noktası açılamadı (619): güvenlik duvarı ya da modem PPTP'yi (GRE) engelliyor olabilir."
                Case 809
                    Return "VPN sunucusu yanıt vermedi (809): sunucu kapalı olabilir ya da ağınız VPN'i engelliyor. Başka bir sunucu deneyin."
                Case 868
                    Return "Sunucu adı bulunamadı (868): sunucu adını kontrol edin (ör. us16.vpnbook.com) ve internet bağlantınızı doğrulayın."
                Case 718, 638, 678
                    Return $"VPN sunucusu zamanında yanıt vermedi ({code}). Biraz sonra ya da başka bir sunucuyla deneyin."
                Case 720
                    Return "VPN bağlantısı kuruldu ama IP adresi alınamadı (720). Başka bir sunucu deneyin."
                Case 741, 742
                    Return $"Sunucu ile şifreleme ayarı uyuşmadı ({code}). Başka bir sunucu deneyin."
                Case 812, 919
                    Return $"Sunucu oturum açma yöntemini reddetti ({code}). Kullanıcı adı / şifreyi ve sunucuyu kontrol edin."
                Case 629, 734
                    Return $"Bağlantı sunucu tarafından kapatıldı ({code}). Şifre değişmiş ya da sunucu dolu olabilir; VPNBook'tan güncel şifreyi kontrol edin."
                Case 623
                    Return "Programın VPN girdisi bulunamadı (623). Yeniden deneyin; sürerse programı yeniden başlatın."
                Case 633, 756
                    Return $"VPN aygıtı şu an meşgul ({code}) — başka bir VPN bağlantısı açık olabilir. Onu kapatıp yeniden deneyin."
                Case 651, 797
                    Return $"Windows'un VPN aygıtı (WAN Miniport) çalışmadı ({code}). Bilgisayarı yeniden başlatıp deneyin."
                Case 631
                    Return "Bağlantı iptal edildi (631)."
                Case Else
                    Dim w = ""
                    Try
                        w = Core.Native.RasNative.ErrorText(code)
                    Catch
                    End Try
                    Return If(w <> "", $"VPN bağlanamadı ({code}): {w}", $"VPN bağlanamadı (hata {code}).")
            End Select
        End Function

    End Module

End Namespace
