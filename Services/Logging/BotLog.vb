Imports System.IO
Imports System.Text

' Services/Logging — uygulamanin yerel defteri (log).
'
' KALICI KURAL: her yeni ozellik deftere baglanir — botun yaptigi is, kullanicinin
' degistirdigi ayar, botun kendi karari. Defter yalniz yerel dosyadir; hicbir yere gonderilmez.
'
' Dosya: <exe>\..\Logs\multi-yyyy-MM-dd.log, 7 gun saklanir. Yazim senkron ve kilitli
' (giris akisinda satir sayisi az); yazilamayan satir uygulamayi DUSURMEZ.
Namespace Services.Logging

    Public Module BotLog

        Private Const SaklamaGun As Integer = 7
        Private ReadOnly _kilit As New Object()
        Private _temizlendi As Boolean = False
        Private ReadOnly _birKezYazilan As New HashSet(Of String)(StringComparer.Ordinal)

        ''' <summary>Bilgi satiri (botun isi / karar / kullanici eylemi).</summary>
        Public Sub Add(kategori As String, mesaj As String)
            Yaz("INFO", kategori, mesaj)
        End Sub

        ''' <summary>Uyari — is surdu ama beklenen olmadi.</summary>
        Public Sub Warn(kategori As String, mesaj As String)
            Yaz("WARN", kategori, mesaj)
        End Sub

        ''' <summary>Hata — istisna varsa turu ve mesaji satira eklenir.</summary>
        Public Sub Fail(kategori As String, mesaj As String, Optional ex As Exception = Nothing)
            Dim m As String = mesaj
            If ex IsNot Nothing Then m &= $" — {ex.GetType().Name}: {ex.Message}"
            Yaz("FAIL", kategori, m)
        End Sub

        ''' <summary>Ayni ad icin hatayi YALNIZ BIR KEZ yazar (tekrarlayan dongu defteri doldurmasin).</summary>
        Public Sub FailOnce(ad As String, ex As Exception)
            SyncLock _kilit
                If Not _birKezYazilan.Add(If(ad, "")) Then Return
            End SyncLock
            Fail("error", ad, ex)
        End Sub

        ''' <summary>Gunun defter dosyasi.</summary>
        Public Function DosyaYolu() As String
            Return Path.Combine(Klasor(), $"multi-{DateTime.Now:yyyy-MM-dd}.log")
        End Function

        Private Function Klasor() As String
            Return Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Logs"))
        End Function

        Private Sub Yaz(seviye As String, kategori As String, mesaj As String)
            Try
                Dim satir As String = $"{DateTime.Now:HH:mm:ss.fff} [{seviye}] [{If(kategori, "-")}] {If(mesaj, "")}"
                SyncLock _kilit
                    Dim dir As String = Klasor()
                    If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
                    If Not _temizlendi Then
                        _temizlendi = True
                        EskileriSil(dir)
                    End If
                    File.AppendAllText(DosyaYolu(), satir & Environment.NewLine, New UTF8Encoding(False))
                End SyncLock
                Debug.WriteLine(satir)
            Catch
                ' Defter hicbir kosulda uygulamayi dusurmez.
            End Try
        End Sub

        Private Sub EskileriSil(dir As String)
            Try
                Dim sinir As DateTime = DateTime.Now.AddDays(-SaklamaGun)
                For Each f As String In Directory.GetFiles(dir, "multi-*.log")
                    If File.GetLastWriteTime(f) < sinir Then File.Delete(f)
                Next
            Catch
            End Try
        End Sub

    End Module

End Namespace
