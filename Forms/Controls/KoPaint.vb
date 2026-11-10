Imports System.Drawing.Drawing2D
Imports System.Globalization

''' <summary>Tone of a pill / status (KoPill, grid status cells, log rows) — one colour set per tone.</summary>
Public Enum KoTone
    Neutral
    Primary
    Ok
    Warn
    Bad
    Info
End Enum

''' <summary>KoButton look.</summary>
Public Enum KoButtonKind
    Primary
    Secondary
    Ghost
    Danger
    Success
End Enum

''' <summary>
''' Shared drawing for the KO designer controls: rounded paths, tone colours, pills, price text. One place, so a card, a
''' pill in a grid cell and a pill control look the same. Module without a namespace (like Theme / UiStyle).
''' </summary>
Friend Module KoPaint

    Friend ReadOnly CardFill As Color = Color.FromArgb(18, 21, 45)
    Friend ReadOnly CardBorder As Color = Color.FromArgb(40, 46, 84)
    Friend ReadOnly Hairline As Color = Color.FromArgb(28, 32, 64)
    Friend ReadOnly SecondaryFill As Color = Color.FromArgb(34, 39, 76)
    Friend ReadOnly SecondaryHover As Color = Color.FromArgb(46, 52, 98)
    Friend ReadOnly RowAlt As Color = Color.FromArgb(14, 17, 36)
    Friend ReadOnly RowSelect As Color = Color.FromArgb(40, 45, 92)
    Friend ReadOnly PriceGold As Color = Color.FromArgb(239, 198, 1)

    Friend Function RoundPath(r As RectangleF, radius As Single) As GraphicsPath
        Dim gp As New GraphicsPath()
        Dim d As Single = Math.Max(1.0F, Math.Min(radius * 2, Math.Min(r.Width, r.Height)))
        gp.AddArc(r.X, r.Y, d, d, 180, 90)
        gp.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        gp.CloseFigure()
        Return gp
    End Function

    ''' <summary>(fill, text, dot) of a tone — fills are dark tints so the text stays readable on the night background.</summary>
    Friend Function ToneColors(tone As KoTone) As (Fill As Color, Fore As Color, Dot As Color)
        Select Case tone
            Case KoTone.Primary : Return (Color.FromArgb(36, 38, 92), Color.FromArgb(165, 180, 252), Primary)
            Case KoTone.Ok : Return (Color.FromArgb(14, 52, 36), Color.FromArgb(134, 239, 172), OkGreen)
            Case KoTone.Warn : Return (Color.FromArgb(60, 44, 12), Color.FromArgb(252, 211, 77), WarnAmber)
            Case KoTone.Bad : Return (Color.FromArgb(68, 20, 26), Color.FromArgb(252, 165, 165), BadRed)
            Case KoTone.Info : Return (Color.FromArgb(14, 40, 66), Color.FromArgb(147, 197, 253), InfoBlue)
            Case Else : Return (Color.FromArgb(30, 34, 62), TextMuted, TextFaint)
        End Select
    End Function

    ''' <summary>A rounded pill with an optional dot — the same drawing in KoPill and in grid cells.</summary>
    Friend Sub DrawPill(g As Graphics, bounds As Rectangle, text As String, tone As KoTone, font As Font, Optional dot As Boolean = True)
        If bounds.Width < 8 OrElse bounds.Height < 8 Then Return
        Dim c = ToneColors(tone)
        Dim old As SmoothingMode = g.SmoothingMode
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using path As GraphicsPath = RoundPath(New RectangleF(bounds.X + 0.5F, bounds.Y + 0.5F, bounds.Width - 1, bounds.Height - 1), bounds.Height / 2.0F)
            Using b As New SolidBrush(c.Fill)
                g.FillPath(b, path)
            End Using
        End Using
        Dim textRect As Rectangle = bounds
        If dot Then
            Dim d As Integer = Math.Max(5, bounds.Height \ 3)
            Dim dx As Integer = bounds.X + Math.Max(7, bounds.Height \ 2 - 1)
            Using b As New SolidBrush(c.Dot)
                g.FillEllipse(b, dx - d \ 2, bounds.Y + (bounds.Height - d) \ 2, d, d)
            End Using
            textRect = Rectangle.FromLTRB(dx + d \ 2 + 3, bounds.Y, bounds.Right - 6, bounds.Bottom)
        End If
        g.SmoothingMode = old
        TextRenderer.DrawText(g, text, font, textRect, c.Fore,
                              TextFormatFlags.VerticalCenter Or If(dot, TextFormatFlags.Left, TextFormatFlags.HorizontalCenter) Or
                              TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine)
    End Sub

    ''' <summary>Width a pill needs for its text (+ dot).</summary>
    Friend Function PillWidth(text As String, font As Font, height As Integer, Optional dot As Boolean = True) As Integer
        Dim w As Integer = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine).Width
        Return w + If(dot, Math.Max(7, height \ 2 - 1) + Math.Max(5, height \ 3) \ 2 + 3 + 8, 16)
    End Function

    Private ReadOnly TurkishPrices As CultureInfo = CultureInfo.GetCultureInfo("tr-TR")

    ''' <summary>"7.000.000" — how a Turkish merchant writes a price (the game itself shows 7,000,000).</summary>
    Friend Function FormatPrice(value As Long) As String
        Return If(value < 0, "—", value.ToString("N0", TurkishPrices))
    End Function

    ''' <summary>"7m", "7,5m", "6.999k", "6999000", "7.000.000", "7,000,000" → 7000000. False for anything else.</summary>
    Friend Function TryParsePrice(text As String, ByRef value As Long) As Boolean
        value = 0
        Dim t As String = If(text, "").Trim().ToLowerInvariant().Replace(" ", "")
        If t = "" Then Return False
        Dim mult As Long = 1
        If t.EndsWith("m") Then
            mult = 1000000 : t = t.Substring(0, t.Length - 1)
        ElseIf t.EndsWith("k") Then
            mult = 1000 : t = t.Substring(0, t.Length - 1)
        End If
        If t = "" Then Return False
        If mult > 1 Then
            ' "7,5m" / "7.5m": a single separator followed by 1-2 digits is a decimal point.
            Dim m = System.Text.RegularExpressions.Regex.Match(t, "^(\d+)(?:[.,](\d{1,2}))?$")
            If Not m.Success Then
                t = t.Replace(".", "").Replace(",", "")
                Dim whole As Long
                If Not Long.TryParse(t, whole) Then Return False
                value = whole * mult : Return True
            End If
            Dim frac As String = m.Groups(2).Value
            Dim v As Decimal = Decimal.Parse(m.Groups(1).Value, CultureInfo.InvariantCulture) +
                               If(frac = "", 0D, Decimal.Parse(frac, CultureInfo.InvariantCulture) / CDec(Math.Pow(10, frac.Length)))
            value = CLng(Math.Round(v * mult))
            Return True
        End If
        t = t.Replace(".", "").Replace(",", "")
        Return Long.TryParse(t, value) AndAlso value >= 0
    End Function

End Module
