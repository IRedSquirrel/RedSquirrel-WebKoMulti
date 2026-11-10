''' <summary>
''' The leader's own window: movable to any screen, ⛶ = borderless full screen on the screen it is on (again = back to a window).
''' The game view stays 1280×800 (the game draws 1:1; the readers measure that size) — on a larger screen it sits in the middle.
''' Closing only hides it; the main window decides when it really goes. Designer: LeaderWindow.Designer.vb.
''' </summary>
Public Class LeaderWindow

    Private _full As Boolean
    Private _normalBounds As Rectangle

    ''' <summary>Set by the main window when the application closes.</summary>
    Public Property AllowClose As Boolean

    Public Event FullScreenChanged As EventHandler

    Public Sub New()
        InitializeComponent()
        UiStyle.ApplyAppIcon(Me)
        AddHandler cell.FullScreenClicked, Sub() ToggleFullScreen()
    End Sub

    Public ReadOnly Property IsFullScreen As Boolean
        Get
            Return _full
        End Get
    End Property

    Public Sub ShowNear(owner As IWin32Window)
        If Not Visible Then
            If Location = Point.Empty Then
                Dim wa As Rectangle = Screen.PrimaryScreen.WorkingArea
                Location = New Point(wa.X + 40, wa.Y + 30)
            End If
            Show(owner)
        End If
        If WindowState = FormWindowState.Minimized Then WindowState = FormWindowState.Normal
    End Sub

    Public Sub ToggleFullScreen()
        _full = Not _full
        If _full Then
            _normalBounds = Bounds
            Dim scr As Screen = Screen.FromControl(Me)
            WindowState = FormWindowState.Normal
            FormBorderStyle = FormBorderStyle.None
            Bounds = scr.Bounds
        Else
            FormBorderStyle = FormBorderStyle.Sizable
            Bounds = If(_normalBounds.IsEmpty, New Rectangle(Location, New Size(1300, 872)), _normalBounds)
        End If
        RaiseEvent FullScreenChanged(Me, EventArgs.Empty)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not AllowClose AndAlso e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Hide()
        End If
        MyBase.OnFormClosing(e)
    End Sub

End Class
