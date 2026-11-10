Imports System.Runtime.InteropServices
Imports System.Text

Namespace Core.Native

    ''' <summary>
    ''' Low-level input hooks and the window / keyboard queries the input mirror needs (Engine/Mirror). Standard
    ''' SetWindowsHookEx low-level hooks. Hooks are installed on the UI thread, which pumps messages.
    ''' </summary>
    Friend Module HookNative

        Friend Const WH_KEYBOARD_LL As Integer = 13
        Friend Const WH_MOUSE_LL As Integer = 14

        Friend Const WM_MOUSEMOVE As Integer = &H200
        Friend Const WM_LBUTTONDOWN As Integer = &H201
        Friend Const WM_LBUTTONUP As Integer = &H202
        Friend Const WM_RBUTTONDOWN As Integer = &H204
        Friend Const WM_RBUTTONUP As Integer = &H205
        Friend Const WM_MBUTTONDOWN As Integer = &H207
        Friend Const WM_MBUTTONUP As Integer = &H208
        Friend Const WM_MOUSEWHEEL As Integer = &H20A
        Friend Const WM_KEYDOWN As Integer = &H100
        Friend Const WM_KEYUP As Integer = &H101
        Friend Const WM_SYSKEYDOWN As Integer = &H104
        Friend Const WM_SYSKEYUP As Integer = &H105

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure POINTAPI
            Public X As Integer
            Public Y As Integer
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure MSLLHOOKSTRUCT
            Public Pt As POINTAPI
            Public MouseData As UInteger
            Public Flags As UInteger
            Public Time As UInteger
            Public ExtraInfo As IntPtr
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure KBDLLHOOKSTRUCT
            Public VkCode As UInteger
            Public ScanCode As UInteger
            Public Flags As UInteger
            Public Time As UInteger
            Public ExtraInfo As IntPtr
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure RECTAPI
            Public Left, Top, Right, Bottom As Integer
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Friend Structure GUITHREADINFO
            Public CbSize As Integer
            Public Flags As Integer
            Public HwndActive As IntPtr
            Public HwndFocus As IntPtr
            Public HwndCapture As IntPtr
            Public HwndMenuOwner As IntPtr
            Public HwndMoveSize As IntPtr
            Public HwndCaret As IntPtr
            Public RcCaret As RECTAPI
        End Structure

        Friend Delegate Function HookProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

        <DllImport("user32.dll", SetLastError:=True)>
        Private Function SetWindowsHookExW(idHook As Integer, lpfn As HookProc, hmod As IntPtr, dwThreadId As UInteger) As IntPtr
        End Function
        <DllImport("user32.dll", SetLastError:=True)>
        Private Function UnhookWindowsHookEx(hhk As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function
        <DllImport("user32.dll")>
        Private Function CallNextHookEx(hhk As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        End Function
        <DllImport("kernel32.dll", CharSet:=CharSet.Unicode)>
        Private Function GetModuleHandleW(lpModuleName As String) As IntPtr
        End Function
        <DllImport("user32.dll")>
        Private Function WindowFromPoint(p As POINTAPI) As IntPtr
        End Function
        <DllImport("user32.dll")>
        Private Function GetAncestor(hwnd As IntPtr, flags As UInteger) As IntPtr
        End Function
        <DllImport("user32.dll")>
        Private Function GetForegroundWindow() As IntPtr
        End Function
        <DllImport("user32.dll")>
        Private Function GetWindowThreadProcessId(hwnd As IntPtr, ByRef pid As UInteger) As UInteger
        End Function
        <DllImport("user32.dll")>
        Private Function GetGUIThreadInfo(threadId As UInteger, ByRef info As GUITHREADINFO) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function
        <DllImport("user32.dll")>
        Private Function IsChild(parent As IntPtr, child As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function
        <DllImport("user32.dll")>
        Private Function GetKeyboardLayout(threadId As UInteger) As IntPtr
        End Function
        <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
        Private Function ToUnicodeEx(vk As UInteger, scan As UInteger, keyState As Byte(), buf As StringBuilder, bufSize As Integer, flags As UInteger, hkl As IntPtr) As Integer
        End Function

        Private Const GA_ROOT As UInteger = 2

        Friend Function SetHook(kind As Integer, proc As HookProc) As IntPtr
            Return SetWindowsHookExW(kind, proc, GetModuleHandleW(Nothing), 0UI)
        End Function

        Friend Function RemoveHook(hook As IntPtr) As Boolean
            If hook = IntPtr.Zero Then Return False
            Return UnhookWindowsHookEx(hook)
        End Function

        Friend Function NextHook(hook As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
            Return CallNextHookEx(hook, nCode, wParam, lParam)
        End Function

        Friend Function MouseData(lParam As IntPtr) As MSLLHOOKSTRUCT
            Return Marshal.PtrToStructure(Of MSLLHOOKSTRUCT)(lParam)
        End Function

        Friend Function KeyData(lParam As IntPtr) As KBDLLHOOKSTRUCT
            Return Marshal.PtrToStructure(Of KBDLLHOOKSTRUCT)(lParam)
        End Function

        ''' <summary>Top-level window under a screen point.</summary>
        Friend Function RootAt(x As Integer, y As Integer) As IntPtr
            Dim h = WindowFromPoint(New POINTAPI With {.X = x, .Y = y})
            Return If(h = IntPtr.Zero, IntPtr.Zero, GetAncestor(h, GA_ROOT))
        End Function

        Friend Function Foreground() As IntPtr
            Return GetForegroundWindow()
        End Function

        ''' <summary>The window holding keyboard focus in the foreground thread (may be a child of another process, as WebView2's).</summary>
        Friend Function ForegroundFocus() As IntPtr
            Dim fg = GetForegroundWindow()
            If fg = IntPtr.Zero Then Return IntPtr.Zero
            Dim pid As UInteger
            Dim tid = GetWindowThreadProcessId(fg, pid)
            Dim info As New GUITHREADINFO With {.CbSize = Marshal.SizeOf(GetType(GUITHREADINFO))}
            If Not GetGUIThreadInfo(tid, info) Then Return IntPtr.Zero
            Return info.HwndFocus
        End Function

        Friend Function IsSameOrChild(parent As IntPtr, h As IntPtr) As Boolean
            Return h <> IntPtr.Zero AndAlso (h = parent OrElse IsChild(parent, h))
        End Function

        ''' <summary>
        ''' The character a key gives with the given modifier state in the foreground keyboard layout; "" for none / dead keys.
        ''' Flag 4 = do not change the keyboard state (Windows 10 1607+), so dead keys of the user's typing are not eaten.
        ''' </summary>
        Friend Function KeyText(vk As Integer, scan As Integer, shift As Boolean, altGr As Boolean, caps As Boolean) As String
            Dim state(255) As Byte
            If shift Then state(&H10) = &H80 : state(&HA0) = &H80
            If altGr Then state(&H11) = &H80 : state(&HA2) = &H80 : state(&H12) = &H80 : state(&HA5) = &H80
            If caps Then state(&H14) = &H1
            Dim pid As UInteger
            Dim tid = GetWindowThreadProcessId(GetForegroundWindow(), pid)
            Dim sb As New StringBuilder(8)
            Dim n = ToUnicodeEx(CUInt(vk), CUInt(scan), state, sb, sb.Capacity, 4UI, GetKeyboardLayout(tid))
            If n <= 0 Then Return ""
            Dim s = sb.ToString(0, n)
            If s.Length = 1 AndAlso Char.IsControl(s(0)) Then Return ""
            Return s
        End Function

    End Module

End Namespace
