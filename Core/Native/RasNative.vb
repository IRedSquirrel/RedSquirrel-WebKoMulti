Imports System.Runtime.InteropServices
Imports System.Text

Namespace Core.Native

    ''' <summary>
    ''' Windows Remote Access (RAS) — the PPTP dialer built into Windows (rasapi32.dll, no install, no admin for a user
    ''' phonebook). Only Services/Vpn calls this. Structures are the Unicode x64 layouts of ras.h, PACKED TO 4 (ras.h uses pshpack4 — measured: Windows accepts RASCONN sizes 1344/1360/1372/1388 only, i.e. a 4-byte packed HRASCONN); RAS accepts the older
    ''' (WINVER 0x401 / 0x500) sizes, so the smaller versions are used where the newer fields are not needed.
    ''' </summary>
    Friend Module RasNative

        Friend Const RAS_MaxEntryName As Integer = 256
        Friend Const RAS_MaxPhoneNumber As Integer = 128
        Friend Const RAS_MaxCallbackNumber As Integer = 128
        Friend Const RAS_MaxDeviceType As Integer = 16
        Friend Const RAS_MaxDeviceName As Integer = 128
        Friend Const RAS_MaxIpAddress As Integer = 15
        Friend Const UNLEN As Integer = 256
        Friend Const PWLEN As Integer = 256
        Friend Const DNLEN As Integer = 15
        Friend Const MAX_PATH As Integer = 260

        Friend Const RASCS_Connected As Integer = &H2000
        Friend Const RASCS_Disconnected As Integer = &H2001
        Friend Const RASP_PppIp As Integer = &H8021

        Friend Const ERROR_INVALID_HANDLE As Integer = 6
        Friend Const ERROR_BUFFER_TOO_SMALL As Integer = 603
        Friend Const ERROR_CANNOT_FIND_PHONEBOOK_ENTRY As Integer = 623
        Friend Const ERROR_ALREADY_EXISTS As Integer = 183

        ''' <summary>RASDIALPARAMSW, WINVER 0x601 (dwIfIndex + szEncPassword; szEncPassword stays NULL).</summary>
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode, Pack:=4)>
        Friend Structure RASDIALPARAMS
            Public dwSize As Integer
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxEntryName + 1)> Public szEntryName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxPhoneNumber + 1)> Public szPhoneNumber As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxCallbackNumber + 1)> Public szCallbackNumber As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=UNLEN + 1)> Public szUserName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=PWLEN + 1)> Public szPassword As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=DNLEN + 1)> Public szDomain As String
            Public dwSubEntry As Integer
            Public dwCallbackId As IntPtr
            Public dwIfIndex As Integer
            Public szEncPassword As IntPtr
        End Structure

        ''' <summary>RASCONNSTATUSW, WINVER 0x401.</summary>
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode, Pack:=4)>
        Friend Structure RASCONNSTATUS
            Public dwSize As Integer
            Public rasconnstate As Integer
            Public dwError As Integer
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxDeviceType + 1)> Public szDeviceType As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxDeviceName + 1)> Public szDeviceName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxPhoneNumber + 1)> Public szPhoneNumber As String
        End Structure

        ''' <summary>RASCONNW, WINVER 0x601 (Win11 rejected the 0x500 size with 632 — measured).</summary>
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode, Pack:=4)>
        Friend Structure RASCONN
            Public dwSize As Integer
            Public hrasconn As IntPtr
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxEntryName + 1)> Public szEntryName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxDeviceType + 1)> Public szDeviceType As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxDeviceName + 1)> Public szDeviceName As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_PATH)> Public szPhonebook As String
            Public dwSubEntry As Integer
            Public guidEntry As Guid
            Public dwFlags As Integer
            Public luidLow As Integer
            Public luidHigh As Integer
            Public guidCorrelationId As Guid
        End Structure

        ''' <summary>RASPPPIPW, WINVER 0x500.</summary>
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode, Pack:=4)>
        Friend Structure RASPPPIP
            Public dwSize As Integer
            Public dwError As Integer
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxIpAddress + 1)> Public szIpAddress As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=RAS_MaxIpAddress + 1)> Public szServerIpAddress As String
            Public dwOptions As Integer
            Public dwServerOptions As Integer
        End Structure

        ''' <summary>RasDialFunc (notifier type 0). Its presence alone makes RasDial asynchronous; state is polled.</summary>
        <UnmanagedFunctionPointer(CallingConvention.StdCall)>
        Friend Delegate Sub RasDialFunc(unMsg As Integer, state As Integer, dwError As Integer)

        <DllImport("rasapi32.dll", CharSet:=CharSet.Unicode, EntryPoint:="RasDialW")>
        Private Function RasDialW(lpRasDialExtensions As IntPtr, lpszPhonebook As String, ByRef lpRasDialParams As RASDIALPARAMS,
                                  dwNotifierType As Integer, lpvNotifier As RasDialFunc, ByRef lphRasConn As IntPtr) As Integer
        End Function

        <DllImport("rasapi32.dll", CharSet:=CharSet.Unicode, EntryPoint:="RasGetConnectStatusW")>
        Private Function RasGetConnectStatusW(hrasconn As IntPtr, ByRef lprasconnstatus As RASCONNSTATUS) As Integer
        End Function

        <DllImport("rasapi32.dll", CharSet:=CharSet.Unicode, EntryPoint:="RasHangUpW")>
        Private Function RasHangUpW(hrasconn As IntPtr) As Integer
        End Function

        <DllImport("rasapi32.dll", CharSet:=CharSet.Unicode, EntryPoint:="RasGetProjectionInfoW")>
        Private Function RasGetProjectionInfoW(hrasconn As IntPtr, rasprojection As Integer, ByRef lpprojection As RASPPPIP, ByRef lpcb As Integer) As Integer
        End Function

        <DllImport("rasapi32.dll", CharSet:=CharSet.Unicode, EntryPoint:="RasEnumConnectionsW")>
        Private Function RasEnumConnectionsW(<[In], Out> lprasconn As RASCONN(), ByRef lpcb As Integer, ByRef lpcConnections As Integer) As Integer
        End Function

        <DllImport("rasapi32.dll", CharSet:=CharSet.Unicode, EntryPoint:="RasGetErrorStringW")>
        Private Function RasGetErrorStringW(uErrorValue As Integer, lpszErrorString As StringBuilder, cBufSize As Integer) As Integer
        End Function

        <DllImport("rasapi32.dll", CharSet:=CharSet.Unicode, EntryPoint:="RasValidateEntryNameW")>
        Private Function RasValidateEntryNameW(lpszPhonebook As String, lpszEntry As String) As Integer
        End Function

        ' The real reason of a failed asynchronous dial arrives ONLY here: on Win11 RAS frees the handle right after the
        ' failure, so RasGetConnectStatus then answers 6 (invalid handle) and the cause is lost (measured 10.11.2026,
        ' dial to 127.0.0.1: state 2 for ~2 s, then rc 6 with dwError 0).
        Private _lastDialError As Integer
        Private _lastDialState As Integer

        ' Kept alive for as long as any asynchronous dial may call back.
        Private ReadOnly _notifier As RasDialFunc = Sub(m, s, e)
                                                       Threading.Volatile.Write(_lastDialState, s)
                                                       If e <> 0 Then Threading.Volatile.Write(_lastDialError, e)
                                                   End Sub

        ''' <summary>Error the current dial reported through its callback (0 = none yet).</summary>
        Friend Function LastDialError() As Integer
            Return Threading.Volatile.Read(_lastDialError)
        End Function

        ''' <summary>Last connection state the current dial reported through its callback.</summary>
        Friend Function LastDialState() As Integer
            Return Threading.Volatile.Read(_lastDialState)
        End Function

        ''' <summary>Starts an asynchronous dial; handle in <paramref name="handle"/> (may be non-zero even on error → hang up).</summary>
        Friend Function DialAsync(phonebook As String, entry As String, user As String, password As String, ByRef handle As IntPtr) As Integer
            Dim p As New RASDIALPARAMS With {
                .szEntryName = entry, .szPhoneNumber = "", .szCallbackNumber = "",
                .szUserName = If(user, ""), .szPassword = If(password, ""), .szDomain = ""
            }
            p.dwSize = Marshal.SizeOf(GetType(RASDIALPARAMS))
            handle = IntPtr.Zero
            Threading.Volatile.Write(_lastDialError, 0)
            Threading.Volatile.Write(_lastDialState, 0)
            Dim rc = RasDialW(IntPtr.Zero, phonebook, p, 0, _notifier, handle)
            p.szPassword = ""
            Return rc
        End Function

        ''' <summary>0 + state/error, or a RAS error (6 = handle no longer valid → the connection is gone).</summary>
        Friend Function GetStatus(handle As IntPtr, ByRef state As Integer, ByRef lastError As Integer) As Integer
            Dim s As New RASCONNSTATUS()
            s.dwSize = Marshal.SizeOf(GetType(RASCONNSTATUS))
            Dim rc = RasGetConnectStatusW(handle, s)
            state = s.rasconnstate
            lastError = s.dwError
            Return rc
        End Function

        ''' <summary>Hangs up and waits (≤ 3 s) until RAS releases the port, as the docs require.</summary>
        Friend Function HangUp(handle As IntPtr) As Integer
            If handle = IntPtr.Zero Then Return 0
            Dim rc = RasHangUpW(handle)
            Dim t = Environment.TickCount
            Do While Environment.TickCount - t < 3000
                Dim st, er As Integer
                If GetStatus(handle, st, er) = ERROR_INVALID_HANDLE Then Exit Do
                Threading.Thread.Sleep(50)
            Loop
            Return rc
        End Function

        ''' <summary>The address the VPN server gave us ("" + RAS error on failure).</summary>
        Friend Function GetPppIp(handle As IntPtr, ByRef localIp As String, ByRef serverIp As String) As Integer
            Dim p As New RASPPPIP()
            p.dwSize = Marshal.SizeOf(GetType(RASPPPIP))
            Dim cb = p.dwSize
            Dim rc = RasGetProjectionInfoW(handle, RASP_PppIp, p, cb)
            localIp = If(rc = 0 AndAlso p.dwError = 0, If(p.szIpAddress, ""), "")
            serverIp = If(rc = 0, If(p.szServerIpAddress, ""), "")
            If rc = 0 AndAlso p.dwError <> 0 Then Return p.dwError
            Return rc
        End Function

        ''' <summary>Open RAS connections (entry name, handle). rc ≠ 0 = RAS error.</summary>
        Friend Function EnumConnections(ByRef list As List(Of KeyValuePair(Of String, IntPtr))) As Integer
            list = New List(Of KeyValuePair(Of String, IntPtr))()
            Dim one = Marshal.SizeOf(GetType(RASCONN))
            Dim arr(15) As RASCONN
            arr(0).dwSize = one
            Dim cb = one * arr.Length, n As Integer = 0
            Dim rc = RasEnumConnectionsW(arr, cb, n)
            If rc = ERROR_BUFFER_TOO_SMALL Then
                ReDim arr(Math.Max(1, cb \ one) - 1)
                arr(0).dwSize = one
                cb = one * arr.Length
                rc = RasEnumConnectionsW(arr, cb, n)
            End If
            If rc <> 0 Then Return rc
            For i = 0 To Math.Min(n, arr.Length) - 1
                list.Add(New KeyValuePair(Of String, IntPtr)(If(arr(i).szEntryName, ""), arr(i).hrasconn))
            Next
            Return 0
        End Function

        ''' <summary>Windows' own text for a RAS error ("" if unknown).</summary>
        Friend Function ErrorText(code As Integer) As String
            Dim sb As New StringBuilder(512)
            If RasGetErrorStringW(code, sb, sb.Capacity) <> 0 Then Return ""
            Return sb.ToString().Trim()
        End Function

        ''' <summary>183 (exists) / 623 (not found) / 0 (valid new name) — tells whether Windows parsed our phonebook entry.</summary>
        Friend Function ValidateEntryName(phonebook As String, entry As String) As Integer
            Return RasValidateEntryNameW(phonebook, entry)
        End Function

    End Module

End Namespace
