Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms

Namespace Core.Session

    ''' <summary>One game account the user typed in (ID, password, which character to enter).</summary>
    Public NotInheritable Class GameAccount
        Public Property Label As String = ""
        Public Property UserId As String = ""
        ''' <summary>Plain text in memory only; on disk through Services/Accounts/AccountStore (DPAPI).</summary>
        Public Property Password As String = ""
        ''' <summary>Character to enter on the character screen, 1-based.</summary>
        Public Property CharacterSlot As Integer = 1
        Public Property Enabled As Boolean = True
        ''' <summary>"VPN ile gir": this account's view goes out through the VPN (Services.Vpn.VpnManager), the others direct.</summary>
        Public Property UseVpn As Boolean = False
    End Class

    Public Enum SessionState
        NotStarted
        Loading
        LoggingIn
        InGame
        Failed
    End Enum

    ''' <summary>
    ''' CONTRACT shared by UI / Login / Follow. One browser game view = one WebView2 with its own profile folder
    ''' (exe\Sessions\N, cookies separate) + the account typed for it. Input and screenshots go through the view
    ''' (Core.Input.CdpInput / Core.Vision.ViewCapture) — the view never needs to be in front.
    ''' Shared contract used by every engine — change with care.
    ''' </summary>
    Public NotInheritable Class GameSession
        Public ReadOnly Property Index As Integer
        Public ReadOnly Property View As WebView2
        Public Property Account As GameAccount
        Public Property State As SessionState = SessionState.NotStarted
        Public Property StatusText As String = ""

        ''' <summary>Raised (any thread) when State / StatusText changes.</summary>
        Public Event Changed(sender As GameSession)

        Public Sub New(index As Integer, view As WebView2, account As GameAccount)
            Me.Index = index
            Me.View = view
            Me.Account = account
        End Sub

        Public ReadOnly Property Number As Integer
            Get
                Return Index + 1
            End Get
        End Property

        Public ReadOnly Property Cw As CoreWebView2
            Get
                Return View.CoreWebView2
            End Get
        End Property

        Public ReadOnly Property Ready As Boolean
            Get
                Return View.CoreWebView2 IsNot Nothing
            End Get
        End Property

        Public Sub SetStatus(newState As SessionState, text As String)
            State = newState
            StatusText = text
            RaiseEvent Changed(Me)
        End Sub

        ''' <summary>
        ''' Proxy for this view, e.g. "socks5://127.0.0.1:51234" (the VPN bridge) — Nothing / "" = direct. Read ONCE by InitAsync:
        ''' a WebView2 environment can't change its proxy later, so switching it means a new view (the hub rebuilds it).
        ''' </summary>
        Public Property ProxyServer As String = ""

        ''' <summary>The proxy the running view was created with ("" = direct).</summary>
        Public ReadOnly Property ActiveProxy As String
            Get
                Return _activeProxy
            End Get
        End Property
        Private _activeProxy As String = ""

        ''' <summary>Creates the WebView2 environment in exe\Sessions\N; call on the UI thread once.</summary>
        Public Async Function InitAsync() As Task
            Dim folder = IO.Path.Combine(AppPaths.ExeDir(), "Sessions", CStr(Number))
            IO.Directory.CreateDirectory(folder)
            Dim args = "--disable-background-timer-throttling --disable-renderer-backgrounding --disable-backgrounding-occluded-windows"
            Dim proxy = If(ProxyServer, "").Trim()
            ' WebRTC would otherwise reach the network around the proxy; the game uses WebSocket, so nothing is lost.
            If proxy <> "" Then args &= $" --proxy-server=""{proxy}"" --force-webrtc-ip-handling-policy=disable_non_proxied_udp"
            Dim opts As New CoreWebView2EnvironmentOptions(args)
            Dim env = Await CoreWebView2Environment.CreateAsync(Nothing, folder, opts)
            Await View.EnsureCoreWebView2Async(env)
            _activeProxy = proxy
        End Function
    End Class

End Namespace
