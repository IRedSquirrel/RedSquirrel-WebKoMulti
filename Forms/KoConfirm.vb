Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' Forms/KoConfirm — a small themed confirm popover ("are you sure?" instead of acting on the first click). Modal, centered on the owner; returns True on confirm. Ko* look (KoPaint palette + KoButton).
Public NotInheritable Class KoConfirm

    Private Sub New()
    End Sub

    Private Shared ReadOnly TitleColor As Color = Color.FromArgb(231, 233, 244)
    Private Shared ReadOnly MsgColor As Color = Color.FromArgb(154, 161, 192)
    Private Shared ReadOnly DangerStrip As Color = Color.FromArgb(176, 40, 52)
    Private Shared ReadOnly PrimaryStrip As Color = Color.FromArgb(99, 102, 241)

    ''' <summary>Shows "title / message" with a confirm (danger) and a Vazgeç button. Returns True only when confirmed.</summary>
    Public Shared Function Ask(owner As IWin32Window, title As String, message As String, confirmText As String,
                               Optional danger As Boolean = True) As Boolean
        Using dlg As New Form()
            dlg.FormBorderStyle = FormBorderStyle.None
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.ShowInTaskbar = False
            dlg.ShowIcon = False
            dlg.MinimizeBox = False
            dlg.MaximizeBox = False
            dlg.KeyPreview = True
            dlg.BackColor = KoPaint.CardFill
            dlg.ForeColor = TitleColor
            dlg.Font = New Font("Segoe UI", 9.0F)
            ' a message with long item names grows the window, it is not cut.
            Dim messageFont As New Font("Segoe UI", 9.5F)
            Dim messageHeight As Integer = Math.Max(42, TextRenderer.MeasureText(message, messageFont, New Size(276, 0), TextFormatFlags.WordBreak Or TextFormatFlags.NoPrefix).Height + 4)
            Dim grown As Integer = messageHeight - 42
            dlg.ClientSize = New Size(316, 156 + grown)

            AddHandler dlg.Paint, Sub(s, e)
                                      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                      Using p As New Pen(KoPaint.CardBorder, 1)
                                          e.Graphics.DrawRectangle(p, New Rectangle(0, 0, dlg.ClientSize.Width - 1, dlg.ClientSize.Height - 1))
                                      End Using
                                      Using b As New SolidBrush(If(danger, DangerStrip, PrimaryStrip))
                                          e.Graphics.FillRectangle(b, New Rectangle(1, 1, dlg.ClientSize.Width - 2, 3))
                                      End Using
                                  End Sub

            dlg.Controls.Add(New Label With {.Text = title, .ForeColor = TitleColor, .AutoSize = False,
                                             .Font = New Font("Segoe UI", 11.5F, FontStyle.Bold), .Bounds = New Rectangle(20, 22, 276, 24)})
            dlg.Controls.Add(New Label With {.Text = message, .ForeColor = MsgColor, .AutoSize = False,
                                             .Font = messageFont, .UseMnemonic = False, .Bounds = New Rectangle(20, 52, 276, messageHeight)})

            Dim btnYes As New KoButton With {.Text = confirmText, .Kind = If(danger, KoButtonKind.Danger, KoButtonKind.Primary),
                                             .Bounds = New Rectangle(20, 108 + grown, 170, 34)}
            AddHandler btnYes.Click, Sub(s, e)
                                         dlg.DialogResult = DialogResult.OK
                                         dlg.Close()
                                     End Sub
            dlg.Controls.Add(btnYes)

            Dim btnNo As New KoButton With {.Text = "Vazgeç", .Kind = KoButtonKind.Ghost, .Bounds = New Rectangle(200, 108 + grown, 96, 34)}
            AddHandler btnNo.Click, Sub(s, e)
                                        dlg.DialogResult = DialogResult.Cancel
                                        dlg.Close()
                                    End Sub
            dlg.Controls.Add(btnNo)

            AddHandler dlg.KeyDown, Sub(s, e)
                                        If e.KeyCode = Keys.Escape Then
                                            dlg.DialogResult = DialogResult.Cancel
                                            dlg.Close()
                                        End If
                                    End Sub

            Return dlg.ShowDialog(owner) = DialogResult.OK
        End Using
    End Function

    Private Shared Function Shell(title As String, message As String, bodyHeight As Integer, ByRef messageBottom As Integer) As Form
        Dim dlg As New Form() With {.FormBorderStyle = FormBorderStyle.None, .StartPosition = FormStartPosition.CenterParent, .ShowInTaskbar = False, .ShowIcon = False,
                                    .MinimizeBox = False, .MaximizeBox = False, .KeyPreview = True, .BackColor = KoPaint.CardFill, .ForeColor = TitleColor,
                                    .Font = New Font("Segoe UI", 9.0F)}
        Dim messageFont As New Font("Segoe UI", 9.5F)
        Dim height As Integer = TextRenderer.MeasureText(message, messageFont, New Size(276, 0), TextFormatFlags.WordBreak Or TextFormatFlags.NoPrefix).Height + 4
        messageBottom = 52 + height + 12
        dlg.ClientSize = New Size(316, messageBottom + bodyHeight + 16)
        AddHandler dlg.Paint, Sub(s, e)
                                  Using p As New Pen(KoPaint.CardBorder, 1)
                                      e.Graphics.DrawRectangle(p, New Rectangle(0, 0, dlg.ClientSize.Width - 1, dlg.ClientSize.Height - 1))
                                  End Using
                                  Using br As New SolidBrush(PrimaryStrip)
                                      e.Graphics.FillRectangle(br, New Rectangle(1, 1, dlg.ClientSize.Width - 2, 3))
                                  End Using
                              End Sub
        dlg.Controls.Add(New Label With {.Text = title, .ForeColor = TitleColor, .AutoSize = False, .UseMnemonic = False,
                                         .Font = New Font("Segoe UI", 11.5F, FontStyle.Bold), .Bounds = New Rectangle(20, 22, 276, 24)})
        dlg.Controls.Add(New Label With {.Text = message, .ForeColor = MsgColor, .AutoSize = False, .UseMnemonic = False, .Font = messageFont,
                                         .Bounds = New Rectangle(20, 52, 276, height)})
        AddHandler dlg.KeyDown, Sub(s, e)
                                    If e.KeyCode = Keys.Escape Then dlg.DialogResult = DialogResult.Cancel : dlg.Close()
                                End Sub
        Return dlg
    End Function

    ''' <summary>one of several answers, one under the other, then Vazgeç. Returns the answer's index, -1 = Vazgeç; Enter takes the first.</summary>
    Public Shared Function Choose(owner As IWin32Window, title As String, message As String, options As String()) As Integer
        Dim picked(0) As Integer
        Using dlg As Form = BuildChoose(title, message, options, picked)
            dlg.ShowDialog(owner)
            Return picked(0)
        End Using
    End Function

    ''' <summary>The window of Choose; picked(0) = the answer (a gate draws it without showing it).</summary>
    Friend Shared Function BuildChoose(title As String, message As String, options As String(), picked As Integer()) As Form
        Dim top As Integer = 0
        Const Pitch As Integer = 38
        picked(0) = -1
        Dim dlg As Form = Shell(title, message, Pitch * (options.Length + 1) - 4, top)
        For i As Integer = 0 To options.Length - 1
            Dim index As Integer = i
            Dim b As New KoButton With {.Text = options(i), .Kind = If(i = 0, KoButtonKind.Primary, KoButtonKind.Secondary), .Bounds = New Rectangle(20, top + Pitch * i, 276, 34)}
            AddHandler b.Click, Sub()
                                    picked(0) = index
                                    dlg.DialogResult = DialogResult.OK
                                    dlg.Close()
                                End Sub
            dlg.Controls.Add(b)
        Next
        Dim no As New KoButton With {.Text = "Vazgeç", .Kind = KoButtonKind.Ghost, .Bounds = New Rectangle(20, top + Pitch * options.Length, 276, 34)}
        AddHandler no.Click, Sub()
                                 dlg.DialogResult = DialogResult.Cancel
                                 dlg.Close()
                             End Sub
        dlg.Controls.Add(no)
        AddHandler dlg.KeyDown, Sub(s, e)
                                    If e.KeyCode = Keys.Enter Then
                                        picked(0) = 0
                                        dlg.DialogResult = DialogResult.OK
                                        dlg.Close()
                                    End If
                                End Sub
        Return dlg
    End Function

    ''' <summary>a short text from the user. Nothing = Vazgeç or an empty box.</summary>
    Public Shared Function AskText(owner As IWin32Window, title As String, message As String, initial As String, maxLength As Integer, confirmText As String) As String
        Dim top As Integer = 0
        Using dlg As Form = Shell(title, message, 76, top)
            Dim box As New KoTextBox With {.Text = If(initial, ""), .MaxLength = maxLength, .Bounds = New Rectangle(20, top, 276, 30)}
            dlg.Controls.Add(box)
            Dim yes As New KoButton With {.Text = confirmText, .Kind = KoButtonKind.Primary, .Bounds = New Rectangle(20, top + 42, 170, 34)}
            AddHandler yes.Click, Sub()
                                      dlg.DialogResult = DialogResult.OK
                                      dlg.Close()
                                  End Sub
            dlg.Controls.Add(yes)
            Dim no As New KoButton With {.Text = "Vazgeç", .Kind = KoButtonKind.Ghost, .Bounds = New Rectangle(200, top + 42, 96, 34)}
            AddHandler no.Click, Sub()
                                     dlg.DialogResult = DialogResult.Cancel
                                     dlg.Close()
                                 End Sub
            dlg.Controls.Add(no)
            AddHandler dlg.KeyDown, Sub(s, e)
                                        If e.KeyCode = Keys.Enter Then e.SuppressKeyPress = True : dlg.DialogResult = DialogResult.OK : dlg.Close()
                                    End Sub
            If dlg.ShowDialog(owner) <> DialogResult.OK OrElse box.Text.Trim() = "" Then Return Nothing
            Return box.Text.Trim()
        End Using
    End Function

End Class
