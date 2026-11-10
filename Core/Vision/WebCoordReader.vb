Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Namespace Core.Vision

    ''' <summary>
    ''' Reads "El Morad 1594, 428" under the HP/MP bars of a 1:1 browser KO view (top-left = view origin).
    ''' Text rows = view rows 77..86, x 0..240 (CSS pixels). A pixel is ink when it is at least 70 % of the brightest pixel
    ''' there (measured over the text area only — a bright scene to the right must not raise the bar). Glyphs are column
    ''' runs, read from the right: digits and one comma; the zone name stops the read.
    ''' Measured on 225 recorded frames (223 read, none wrong). Works on a whole view frame OR on the
    ''' <see cref="Region"/> crop — the reader only ever looks at x 0..239, y 77..86, so both give the same answer.
    ''' </summary>
    Public Module WebCoordReader

        Private Const Top As Integer = 77
        Private Const Rows As Integer = 10
        Private Const MaxX As Integer = 240
        Private Const InkShare As Double = 0.7
        Private Const MinMatch As Double = 0.86

        ''' <summary>The only part of the view the reader needs: capture this (ViewCapture.RegionAsync) instead of the whole view.</summary>
        Public ReadOnly Region As New Rectangle(0, 0, MaxX, 96)

        ''' <summary><see cref="Region"/> moved onto a game canvas that does not start at the view's corner (GameCanvas.Locate).</summary>
        Public Function RegionIn(canvas As Rectangle) As Rectangle
            Return New Rectangle(canvas.X + Region.X, canvas.Y + Region.Y, Region.Width, Region.Height)
        End Function

        Private ReadOnly Templates As List(Of KeyValuePair(Of Char, Boolean(,))) = Load()

        Private Function Load() As List(Of KeyValuePair(Of Char, Boolean(,)))
            Dim list As New List(Of KeyValuePair(Of Char, Boolean(,)))
            For Each entry In WebCoordGlyphs.Data
                Dim parts = entry.Split("|"c)
                Dim w = parts(1).Length
                Dim g(Rows - 1, w - 1) As Boolean
                For y = 0 To Rows - 1
                    For x = 0 To w - 1
                        g(y, x) = parts(1 + y)(x) = "1"c
                    Next
                Next
                list.Add(New KeyValuePair(Of Char, Boolean(,))(parts(0)(0), g))
            Next
            Return list
        End Function

        ''' <summary>(x, y) world coordinate, or Nothing when the text can't be read.</summary>
        Public Function Read(view As Bitmap) As Point?
            If view Is Nothing OrElse view.Width < 60 OrElse view.Height < Top + Rows Then Return Nothing
            Dim w = Math.Min(MaxX, view.Width)
            Dim data = view.LockBits(New Rectangle(0, Top, w, Rows), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
            Dim buf(data.Stride * Rows - 1) As Byte
            Marshal.Copy(data.Scan0, buf, 0, buf.Length)
            view.UnlockBits(data)
            Dim lum(Rows - 1, w - 1) As Integer
            Dim lums As New List(Of Integer)
            For y = 0 To Rows - 1
                For x = 0 To w - 1
                    Dim i = y * data.Stride + x * 4
                    Dim l = (CInt(buf(i)) + buf(i + 1) + buf(i + 2)) \ 3
                    lum(y, x) = l
                    lums.Add(l)
                Next
            Next
            lums.Sort()
            Dim hi = lums(Math.Max(0, lums.Count - Math.Max(1, lums.Count \ 50)))
            Dim thr = Math.Max(60, CInt(hi * InkShare))
            Dim ink(Rows - 1, w - 1) As Boolean
            Dim colHas(w) As Boolean
            For y = 0 To Rows - 1
                For x = 0 To w - 1
                    ink(y, x) = lum(y, x) >= thr
                    If ink(y, x) Then colHas(x) = True
                Next
            Next
            ' column runs = glyphs
            Dim runs As New List(Of (A As Integer, B As Integer))
            Dim s = -1
            For x = 0 To w
                If colHas(x) AndAlso s < 0 Then s = x
                If Not colHas(x) AndAlso s >= 0 Then runs.Add((s, x - 1)) : s = -1
            Next
            Dim chars As New List(Of Char)
            For k = runs.Count - 1 To 0 Step -1
                Dim c = Classify(ink, runs(k).A, runs(k).B)
                ' Bright scene / HUD edge right of the numbers (x 228..239 on 1280×800, measured) is skipped until the
                ' first digit; after that an unknown glyph is the zone name and ends the read.
                If c = ChrW(0) AndAlso chars.Count = 0 Then Continue For
                If c = ChrW(0) Then Exit For
                chars.Insert(0, c)
                If chars.Count > 11 Then Exit For
            Next
            Dim txt As New String(chars.ToArray())
            Dim parts = txt.Split(","c)
            If parts.Length <> 2 Then Return Nothing
            Dim a, b As Integer
            If parts(0).Length = 0 OrElse parts(0).Length > 4 OrElse parts(1).Length = 0 OrElse parts(1).Length > 4 Then Return Nothing
            If Not Integer.TryParse(parts(0), a) OrElse Not Integer.TryParse(parts(1), b) Then Return Nothing
            Return New Point(a, b)
        End Function

        Private Function Classify(ink As Boolean(,), a As Integer, b As Integer) As Char
            Dim wa = b - a + 1
            Dim best = ChrW(0)
            Dim bs = 0.0
            For Each t In Templates
                Dim wb = t.Value.GetLength(1)
                If Math.Abs(wa - wb) > 1 Then Continue For
                Dim w = Math.Max(wa, wb)
                For oa = 0 To w - wa
                    For ob = 0 To w - wb
                        Dim eq = 0
                        For y = 0 To Rows - 1
                            For x = 0 To w - 1
                                Dim va = x - oa >= 0 AndAlso x - oa < wa AndAlso ink(y, a + x - oa)
                                Dim vb = x - ob >= 0 AndAlso x - ob < wb AndAlso t.Value(y, x - ob)
                                If va = vb Then eq += 1
                            Next
                        Next
                        Dim sc = eq / (Rows * w)
                        If sc > bs Then bs = sc : best = t.Key
                    Next
                Next
            Next
            Return If(bs >= MinMatch, best, ChrW(0))
        End Function

    End Module

End Namespace
