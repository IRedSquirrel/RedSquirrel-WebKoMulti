Imports System.IO
Imports System.Text
Imports Microsoft.Win32

Namespace Services.Vpn

    ''' <summary>
    ''' The program's OWN phonebook file ..\Settings\multi-vpn.pbk (RasDial takes a phonebook path) — the user's rasphone.pbk
    ''' and Windows' VPN list are never touched. Entry "RedSquirrel WebKoMulti VPN": PPTP only, MS-CHAPv2 only, encryption required,
    ''' IPv6 not negotiated, no file/print sharing over the VPN, no class-based route.
    '''
    ''' ROUTING (measured 10.11.2026 on this PC): a socket bound to an interface that has NO route (bind to its IP, IP_UNICAST_IF,
    ''' or both) fails with 10051 NetworkUnreachable — Windows does not fall back to another interface (good: no leak), but it
    ''' also means a pure split tunnel ("use default gateway on remote network" off, no routes) gives the bridge nowhere to go.
    ''' A 0.0.0.0/0 custom VPN route is refused by Windows (Add-VpnConnectionRoute: "Invalid parameter"), and 0/1 + 128/1 would
    ''' beat the real default route by prefix length (= whole PC through the VPN). So the entry asks for the remote default
    ''' gateway with a HIGH interface metric (<see cref="InterfaceMetric"/>): the VPN gets a default route that always loses to
    ''' the PC's own, so ordinary traffic stays direct and only sockets pinned to the VPN interface use it. VpnManager VERIFIES
    ''' this after every dial (GetBestInterfaceEx to the test host must not be the VPN interface) and hangs up otherwise.
    ''' </summary>
    Friend Module VpnPhonebook

        Friend Const EntryName As String = "RedSquirrel WebKoMulti VPN"
        Friend Const InterfaceMetric As Integer = 9000

        ''' <summary>Test hook: another phonebook file. Nothing = ..\Settings\multi-vpn.pbk.</summary>
        Friend PathOverride As String = Nothing

        Friend Function FilePath() As String
            If Not String.IsNullOrEmpty(PathOverride) Then Return PathOverride
            Return Path.GetFullPath(Path.Combine(AppPaths.ExeDir(), "..", "Settings", "multi-vpn.pbk"))
        End Function

        ''' <summary>Writes the phonebook (only our entry) for <paramref name="server"/>; atomic. Returns the path.</summary>
        Friend Function Write(server As String) As String
            Dim file = FilePath()
            Directory.CreateDirectory(Path.GetDirectoryName(file))
            Dim tmp = file & ".tmp"
            IO.File.WriteAllText(tmp, BuildEntry(server, PptpPortName()), New UTF8Encoding(False))
            If IO.File.Exists(file) Then
                IO.File.Replace(tmp, file, Nothing)
            Else
                IO.File.Move(tmp, file)
            End If
            Return file
        End Function

        ''' <summary>
        ''' "VPN&lt;n&gt;-0": n = rank of the PPTP miniport among the WAN miniports that have endpoints (this PC: SSTP, IKEv2,
        ''' L2TP, PPTP → VPN4-0, the port Windows itself wrote for a PowerShell-made PPTP entry). Read-only registry; default 4.
        ''' </summary>
        Friend Function PptpPortName() As String
            Try
                Using cls = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}")
                    If cls IsNot Nothing Then
                        Dim rank = 0
                        For Each name In cls.GetSubKeyNames().Where(Function(n) n.All(AddressOf Char.IsDigit)).OrderBy(Function(n) n)
                            Using k = cls.OpenSubKey(name)
                                Dim desc = TryCast(k?.GetValue("DriverDesc"), String)
                                If desc Is Nothing OrElse Not desc.StartsWith("WAN Miniport", StringComparison.OrdinalIgnoreCase) Then Continue For
                                Dim ep = k.GetValue("WanEndpoints")
                                If ep Is Nothing Then Continue For
                                If desc.IndexOf("PPPOE", StringComparison.OrdinalIgnoreCase) >= 0 Then Continue For
                                rank += 1
                                If desc.IndexOf("PPTP", StringComparison.OrdinalIgnoreCase) >= 0 Then Return $"VPN{rank}-0"
                            End Using
                        Next
                    End If
                End Using
            Catch
            End Try
            Return "VPN4-0"
        End Function

        ''' <summary>The phonebook text (side-effect free; the self-test reads the routing fields from it).</summary>
        Friend Function BuildEntry(server As String, port As String) As String
            Dim host = If(server, "").Trim()
            Dim sb As New StringBuilder()
            Dim L = Sub(s As String) sb.Append(s).Append(vbCrLf)
            L($"[{EntryName}]")
            L("Encoding=1")
            L("PBVersion=8")
            L("Type=2")
            L("AutoLogon=0")
            L("UseRasCredentials=0")
            L("DialParamsUID=0")
            L("Guid=" & Guid.NewGuid().ToString("N").ToUpperInvariant())
            L("VpnStrategy=1")                       ' PPTP only
            L("ExcludedProtocols=8")                 ' no IPv6 over the VPN
            L("LcpExtensions=1")
            L("DataEncryption=256")                  ' "require encryption" (what Windows writes for -EncryptionLevel Required)
            L("SwCompression=0")
            L("NegotiateMultilinkAlways=0")
            L("SkipDoubleDialDialog=0")
            L("DialMode=0")
            L("OverridePref=15")
            L("RedialAttempts=0")
            L("RedialSeconds=60")
            L("IdleDisconnectSeconds=0")
            L("RedialOnLinkFailure=0")
            L("CallbackMode=0")
            L("CustomDialDll=")
            L("CustomDialFunc=")
            L("CustomRasDialDll=")
            L("ForceSecureCompartment=0")
            L("DisableIKENameEkuCheck=0")
            L("AuthenticateServer=0")
            L("ShareMsFilePrint=0")                  ' nothing of this PC is shared towards the VPN
            L("BindMsNetClient=0")
            L("SharedPhoneNumbers=0")
            L("GlobalDeviceSettings=0")
            L("PrerequisiteEntry=")
            L("PrerequisitePbk=")
            L("PreferredPort=" & port)
            L("PreferredDevice=WAN Miniport (PPTP)")
            L("PreferredBps=0")
            L("PreferredHwFlow=0")
            L("PreferredProtocol=0")
            L("PreferredCompression=0")
            L("PreferredSpeaker=0")
            L("PreferredMdmProtocol=0")
            L("PreviewUserPw=0")
            L("PreviewDomain=0")
            L("PreviewPhoneNumber=0")
            L("ShowDialingProgress=0")
            L("ShowMonitorIconInTaskBar=0")
            L("CustomAuthKey=0")
            L("AuthRestrictions=512")                ' MS-CHAPv2 only
            L("IpPrioritizeRemote=1")                ' see the ROUTING note: a default route that always loses …
            L("IpInterfaceMetric=" & InterfaceMetric) ' … because of this metric
            L("IpHeaderCompression=0")
            L("IpAddress=0.0.0.0")
            L("IpDnsAddress=0.0.0.0")
            L("IpDns2Address=0.0.0.0")
            L("IpWinsAddress=0.0.0.0")
            L("IpWins2Address=0.0.0.0")
            L("IpAssign=1")
            L("IpNameAssign=1")
            L("IpDnsFlags=0")
            L("IpNBTFlags=0")
            L("TcpWindowSize=0")
            L("UseFlags=2")
            L("IpSecFlags=0")
            L("IpDnsSuffix=")
            L("Ipv6Assign=1")
            L("Ipv6Address=::")
            L("Ipv6PrefixLength=0")
            L("Ipv6PrioritizeRemote=0")
            L("Ipv6InterfaceMetric=0")
            L("Ipv6NameAssign=1")
            L("Ipv6DnsAddress=::")
            L("Ipv6Dns2Address=::")
            L("Ipv6Prefix=0000000000000000")
            L("Ipv6InterfaceId=0000000000000000")
            L("DisableClassBasedDefaultRoute=1")     ' no 10.0.0.0/8-style route that could catch LAN traffic
            L("DisableMobility=0")
            L("NetworkOutageTime=0")
            L("CacheCredentials=0")
            L("NumCustomPolicy=0")
            L("NumEku=0")
            L("UseMachineRootCert=0")
            L("NumServers=0")
            L("NumRoutes=0")
            L("NumNrptRules=0")
            L("AutoTiggerCapable=0")
            L("NumAppIds=0")
            L("NumClassicAppIds=0")
            L("DisableDefaultDnsSuffixes=1")
            L("NumTrustedNetworks=0")
            L("NumDnsSearchSuffixes=0")
            L("ProxyFlags=0")
            L("AlwaysOnCapable=0")
            L("DeviceTunnel=0")
            L("PrivateNetwork=0")
            L("")
            L("NETCOMPONENTS=")
            L("ms_msclient=0")
            L("ms_server=0")
            L("")
            L("MEDIA=rastapi")
            L("Port=" & port)
            L("Device=WAN Miniport (PPTP)")
            L("")
            L("DEVICE=vpn")
            L("PhoneNumber=" & host)
            L("AreaCode=")
            L("CountryCode=0")
            L("CountryID=0")
            L("UseDialingRules=0")
            L("Comment=")
            L("FriendlyName=")
            L("LastSelectedPhone=0")
            L("PromoteAlternates=0")
            L("TryNextAlternateOnFail=1")
            Return sb.ToString()
        End Function

        ''' <summary>Value of key=value in our entry text ("" if absent) — self-test helper.</summary>
        Friend Function Field(text As String, key As String) As String
            For Each ln As String In text.Split({vbCrLf, vbLf}, StringSplitOptions.None)
                If ln.StartsWith(key & "=", StringComparison.Ordinal) Then Return ln.Substring(key.Length + 1)
            Next
            Return ""
        End Function

    End Module

End Namespace
