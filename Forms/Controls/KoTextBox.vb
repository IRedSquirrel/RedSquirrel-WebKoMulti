Imports System.ComponentModel
Imports System.Drawing.Drawing2D

''' <summary>
''' Dark rounded text field (Oct 20, settings › Oyun: game account ID / password, server name): an inner borderless TextBox in a
''' KoItemPicker-style frame (focus → indigo edge), a grey hint while empty (Cue), and for a password an eye on the right that
''' shows / hides it (Password + Reveal). Draws itself → the designer shows the real look. "Committed" = the user left the box or
''' pressed Enter — what the settings save on.
''' </summary>
<ToolboxItem(True), DesignerCategory("Code"), DefaultEvent("Committed"), DefaultProperty("Text")>
Public Class KoTextBox
    Inherits Control

    Private ReadOnly _box As New TextBox()
    Private _cue As String = ""
    Private _password As Boolean
    Private _revealed As Boolean
    Private _hoverEye As Boolean
    Private Const EyeSize As Integer = 26

    ''' <summary>The user left the box or pressed Enter (text may be unchanged).</summary>
    Public Event Committed As EventHandler

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor Or ControlStyles.Selectable, True)
        MyBase.BackColor = Color.Transparent
        MyBase.Font = New Font("Segoe UI", 10.0!)
        _box.BorderStyle = BorderStyle.None
        _box.BackColor = NightDeep
        _box.ForeColor = TextMain
        _box.Font = MyBase.Font
        Controls.Add(_box)
        AddHandler _box.TextChanged, Sub() OnTextChanged(EventArgs.Empty)
        AddHandler _box.GotFocus, Sub() Invalidate()
        AddHandler _box.LostFocus, Sub()
                                       Invalidate()
                                       RaiseEvent Committed(Me, EventArgs.Empty)
                                   End Sub
        AddHandler _box.KeyDown, Sub(s, e)
                                     OnKeyDown(e)
                                     If e.KeyCode = Keys.Enter AndAlso Not e.Handled Then
                                         e.SuppressKeyPress = True
                                         RaiseEvent Committed(Me, EventArgs.Empty)
                                     End If
                                 End Sub
        AddHandler _box.HandleCreated, Sub() Core.Native.UiNative.SetCueBanner(_box.Handle, _cue)
        Size = New Size(200, 30)
    End Sub

    <Browsable(True), EditorBrowsable(EditorBrowsableState.Always), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
    Public Overrides Property Text As String
        Get
            Return _box.Text
        End Get
        Set(value As String)
            _box.Text = If(value, "")
        End Set
    End Property

    ''' <summary>Grey hint shown while the box is empty (also while focused).</summary>
    <Category("Ko"), DefaultValue("")>
    Public Property Cue As String
        Get
            Return _cue
        End Get
        Set(value As String)
            _cue = If(value, "")
            If _box.IsHandleCreated Then Core.Native.UiNative.SetCueBanner(_box.Handle, _cue)
        End Set
    End Property

    ''' <summary>A password: shown as dots, with the eye on the right to show / hide it.</summary>
    <Category("Ko"), DefaultValue(False)>
    Public Property Password As Boolean
        Get
            Return _password
        End Get
        Set(value As Boolean)
            _password = value
            _revealed = False
            _box.UseSystemPasswordChar = value
            PerformLayout()
            Invalidate()
        End Set
    End Property

    <Category("Ko"), DefaultValue(32767)>
    Public Property MaxLength As Integer
        Get
            Return _box.MaxLength
        End Get
        Set(value As Integer)
            _box.MaxLength = value
        End Set
    End Property

    <Category("Ko"), DefaultValue(GetType(CharacterCasing), "Normal")>
    Public Property CharacterCasing As CharacterCasing
        Get
            Return _box.CharacterCasing
        End Get
        Set(value As CharacterCasing)
            _box.CharacterCasing = value
        End Set
    End Property

    Public Overrides Property Font As Font
        Get
            Return MyBase.Font
        End Get
        Set(value As Font)
            MyBase.Font = value
            _box.Font = value
            PerformLayout()
        End Set
    End Property

    Public Shadows Sub Focus()
        _box.Focus()
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        _box.Enabled = Enabled
        _box.ForeColor = If(Enabled, TextMain, TextFaint)
        Invalidate()
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        MyBase.OnGotFocus(e)
        _box.Focus()
    End Sub

    Private Function EyeRect() As Rectangle
        Return New Rectangle(Width - EyeSize - 4, (Height - EyeSize) \ 2, EyeSize, EyeSize)
    End Function

    Protected Overrides Sub OnLayout(levent As LayoutEventArgs)
        MyBase.OnLayout(levent)
        Dim h As Integer = _box.PreferredHeight
        Dim right As Integer = If(_password, EyeSize + 8, 10)
        _box.Bounds = New Rectangle(10, Math.Max(0, (Height - h) \ 2), Math.Max(10, Width - 10 - right), h)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim over As Boolean = _password AndAlso EyeRect().Contains(e.Location)
        If over <> _hoverEye Then _hoverEye = over : Cursor = If(over, Cursors.Hand, Cursors.IBeam) : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        If _hoverEye Then _hoverEye = False : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If _password AndAlso Enabled AndAlso EyeRect().Contains(e.Location) Then
            _revealed = Not _revealed
            _box.UseSystemPasswordChar = Not _revealed
            Invalidate()
        End If
        _box.Focus()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim focused As Boolean = _box.Focused
        Using path As GraphicsPath = KoPaint.RoundPath(New RectangleF(0.5F, 0.5F, Width - 1.0F, Height - 1.0F), 7)
            Using b As New SolidBrush(NightDeep)
                g.FillPath(b, path)
            End Using
            Using p As New Pen(If(focused, PrimarySoft, KoPaint.CardBorder), If(focused, 1.4F, 1.0F))
                g.DrawPath(p, path)
            End Using
        End Using
        If Not _password Then Return
        Dim r As Rectangle = EyeRect()
        If _hoverEye Then
            Using b As New SolidBrush(KoPaint.SecondaryHover)
                g.FillEllipse(b, r)
            End Using
        End If
        ' An eye: an almond outline and a pupil; crossed out while hidden.
        Dim c As New PointF(r.X + r.Width / 2.0F, r.Y + r.Height / 2.0F)
        Dim col As Color = If(_revealed, PrimarySoft, If(_hoverEye, TextMain, TextMuted))
        Using p As New Pen(col, 1.4F)
            Using eye As New GraphicsPath()
                eye.AddBezier(c.X - 8, c.Y, c.X - 4, c.Y - 5.5F, c.X + 4, c.Y - 5.5F, c.X + 8, c.Y)
                eye.AddBezier(c.X + 8, c.Y, c.X + 4, c.Y + 5.5F, c.X - 4, c.Y + 5.5F, c.X - 8, c.Y)
                g.DrawPath(p, eye)
            End Using
            g.DrawEllipse(p, c.X - 2.2F, c.Y - 2.2F, 4.4F, 4.4F)
            If Not _revealed Then g.DrawLine(p, c.X - 7, c.Y + 6, c.X + 7, c.Y - 6)
        End Using
    End Sub

End Class
