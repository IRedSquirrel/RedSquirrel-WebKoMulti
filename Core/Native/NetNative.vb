Imports System.Net
Imports System.Runtime.InteropServices

Namespace Core.Native

    ''' <summary>IP Helper: which interface Windows would use for a destination (no admin, read only).</summary>
    Friend Module NetNative

        <DllImport("iphlpapi.dll")>
        Private Function GetBestInterfaceEx(pDestAddr As Byte(), ByRef pdwBestIfIndex As Integer) As Integer
        End Function

        ''' <summary>Interface index Windows routes <paramref name="destination"/> through by default; -1 on error.</summary>
        Friend Function BestInterfaceFor(destination As IPAddress) As Integer
            If destination Is Nothing OrElse destination.AddressFamily <> Sockets.AddressFamily.InterNetwork Then Return -1
            Dim sa(15) As Byte                        ' sockaddr_in: family (LE), port (0), address, zero
            sa(0) = 2 : sa(1) = 0
            Buffer.BlockCopy(destination.GetAddressBytes(), 0, sa, 4, 4)
            Dim idx As Integer = 0
            If GetBestInterfaceEx(sa, idx) <> 0 Then Return -1
            Return idx
        End Function

    End Module

End Namespace
