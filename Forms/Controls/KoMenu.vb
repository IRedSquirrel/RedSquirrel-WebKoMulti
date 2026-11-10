''' <summary>a right-click menu in the app's dark look (the system's light one sat oddly on these screens).</summary>
Friend NotInheritable Class KoMenu

    Private Sub New()
    End Sub

    Private NotInheritable Class Colors
        Inherits ProfessionalColorTable

        Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
            Get
                Return KoPaint.CardFill
            End Get
        End Property
        Public Overrides ReadOnly Property MenuBorder As Color
            Get
                Return KoPaint.CardBorder
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemBorder As Color
            Get
                Return Primary
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelected As Color
            Get
                Return Color.FromArgb(40, 44, 92)
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
            Get
                Return KoPaint.CardFill
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
            Get
                Return KoPaint.CardFill
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
            Get
                Return KoPaint.CardFill
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorDark As Color
            Get
                Return KoPaint.CardBorder
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorLight As Color
            Get
                Return KoPaint.CardFill
            End Get
        End Property
    End Class

    Private NotInheritable Class Renderer
        Inherits ToolStripProfessionalRenderer

        Public Sub New()
            MyBase.New(New Colors())
            RoundedEdges = False
        End Sub

        ' Disabled lines stay readable on the dark card (the default grey is drawn for a light menu).
        Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
            e.TextColor = If(e.Item.Enabled, e.Item.ForeColor, TextFaint)
            MyBase.OnRenderItemText(e)
        End Sub

        Protected Overrides Sub OnRenderArrow(e As ToolStripArrowRenderEventArgs)
            e.ArrowColor = TextMuted
            MyBase.OnRenderArrow(e)
        End Sub
    End Class

    Public Shared Function Create() As ContextMenuStrip
        Return New ContextMenuStrip With {.Renderer = New Renderer(), .ShowImageMargin = False, .Font = New Font("Segoe UI", 9.0!), .BackColor = KoPaint.CardFill}
    End Function

    Public Shared Function Add(items As ToolStripItemCollection, text As String, action As Action, Optional enabled As Boolean = True, Optional danger As Boolean = False) As ToolStripMenuItem
        Dim item As New ToolStripMenuItem(text) With {.ForeColor = If(danger, BadRed, TextMain), .Enabled = enabled}
        If action IsNot Nothing Then AddHandler item.Click, Sub() action()
        items.Add(item)
        Return item
    End Function

    ''' <summary>A line that opens more lines.</summary>
    Public Shared Function AddGroup(items As ToolStripItemCollection, text As String) As ToolStripMenuItem
        Dim item As New ToolStripMenuItem(text) With {.ForeColor = TextMain}
        item.DropDown.BackColor = KoPaint.CardFill
        DirectCast(item.DropDown, ToolStripDropDownMenu).ShowImageMargin = False
        items.Add(item)
        Return item
    End Function

End Class
