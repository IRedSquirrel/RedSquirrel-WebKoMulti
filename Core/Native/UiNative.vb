Imports System.Runtime.InteropServices

Namespace Core.Native
    ''' <summary>The few Win32 calls the Ko* controls need (cue banner). Single P/Invoke place of this project.</summary>
    Friend Module UiNative
        Private Const EM_SETCUEBANNER As Integer = &H1501
        <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
        Private Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As String) As IntPtr
        End Function
        Friend Sub SetCueBanner(textBox As IntPtr, text As String)
            If textBox = IntPtr.Zero Then Return
            SendMessage(textBox, EM_SETCUEBANNER, New IntPtr(1), text)
        End Sub
    End Module
End Namespace
