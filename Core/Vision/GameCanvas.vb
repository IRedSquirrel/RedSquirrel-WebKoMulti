Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Namespace Core.Vision

    ''' <summary>
    ''' Where the game canvas sits inside a view. In a view larger than the game the browser KO client does NOT scale: it draws
    ''' its 1280×800 canvas in the middle with black around it (measured 9 Nov, 1920×1032 full-screen leader: canvas
    ''' 320..1599 × 143..942, a light page strip on top). Every canvas-relative position (coordinate text rows 77..86,
    ''' the walking circle) must be offset by this rectangle. When the view IS the canvas the answer is the whole view.
    ''' Pure function of one whole-view frame; callers cache it until the view size changes.
    ''' </summary>
    Public Module GameCanvas

        ''' <summary>A pixel counts as border when its brightest channel is at most this.</summary>
        Private Const BlackMax As Integer = 12
        ''' <summary>A column / row is border when at least this share of it is black.</summary>
        Private Const BorderShare As Double = 0.9
        ''' <summary>Width / height of the game canvas (1280×800).</summary>
        Private Const Aspect As Double = 1.6
        Private Const MinBorderPx As Integer = 8

        ''' <summary>Canvas rectangle in frame pixels; the whole frame when no black border is found.</summary>
        Public Function Locate(frame As Bitmap) As Rectangle
            Dim full As New Rectangle(0, 0, frame.Width, frame.Height)
            If frame.Width < 100 OrElse frame.Height < 100 Then Return full
            Dim w = frame.Width, h = frame.Height
            Dim black(h - 1, w - 1) As Boolean
            Dim data = frame.LockBits(full, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
            Dim buf(data.Stride * h - 1) As Byte
            Marshal.Copy(data.Scan0, buf, 0, buf.Length)
            frame.UnlockBits(data)
            For y = 0 To h - 1
                Dim o = y * data.Stride
                For x = 0 To w - 1
                    Dim i = o + x * 4
                    black(y, x) = buf(i) <= BlackMax AndAlso buf(i + 1) <= BlackMax AndAlso buf(i + 2) <= BlackMax
                Next
            Next

            ' Columns: the canvas is the span between the black side bands.
            Dim colBorder(w - 1) As Boolean
            For x = 0 To w - 1
                Dim n = 0
                For y = 0 To h - 1
                    If black(y, x) Then n += 1
                Next
                colBorder(x) = n >= BorderShare * h
            Next
            Dim left = 0, right = w - 1
            While left < w AndAlso colBorder(left) : left += 1 : End While
            While right > left AndAlso colBorder(right) : right -= 1 : End While
            If left >= right Then Return full
            Dim cw = right - left + 1

            ' Rows, looked at only inside the canvas columns.
            Dim rowBorder(h - 1) As Boolean
            For y = 0 To h - 1
                Dim n = 0
                For x = left To right
                    If black(y, x) Then n += 1
                Next
                rowBorder(y) = n >= BorderShare * cw
            Next
            Dim sideBands = left >= MinBorderPx OrElse w - 1 - right >= MinBorderPx
            Dim ch As Integer
            If sideBands Then
                ch = CInt(Math.Round(cw / Aspect))   ' the canvas keeps its shape; dark scenes inside must not cut it
            Else
                ' No side bands: the canvas spans the width; its height is the longest run of non-border rows.
                Dim best = 0, run = 0
                For y = 0 To h - 1
                    run = If(rowBorder(y), 0, run + 1)
                    best = Math.Max(best, run)
                Next
                If best >= h - MinBorderPx Then Return full
                ch = best
            End If
            If ch >= h Then Return New Rectangle(left, 0, cw, h)

            ' Top: the window of ch rows with the most canvas rows inside, starting / ending exactly where black turns into
            ' canvas. The canvas' own first / last row can be dark — only a real black → canvas transition counts as an edge
            ' (measured: rewarding "black just outside" alone put the top one row too high, and the reader needs exact rows).
            Dim prefix(h) As Integer
            For y = 0 To h - 1
                prefix(y + 1) = prefix(y) + If(rowBorder(y), 0, 1)
            Next
            Dim bestTop = 0, bestScore = Integer.MinValue
            For t = 0 To h - ch
                Dim inside = prefix(t + ch) - prefix(t)
                Dim edge = 0
                If t > 0 AndAlso rowBorder(t - 1) AndAlso Not rowBorder(t) Then edge += 50
                If t + ch < h AndAlso rowBorder(t + ch) AndAlso Not rowBorder(t + ch - 1) Then edge += 50
                Dim score = inside + edge
                If score > bestScore Then bestScore = score : bestTop = t
            Next
            Return New Rectangle(left, bestTop, cw, ch)
        End Function

        ''' <summary>
        ''' Locate + settle the canvas' exact top row by reading the coordinate text: the border edge can be one row off (a
        ''' partly dark first row; measured on two real full-screen frames) and the reader needs exact rows. Tries ±3 rows
        ''' around the located top and keeps the one where the text reads; unchanged when the text is not readable anywhere.
        ''' </summary>
        Public Function LocateForCoords(frame As Bitmap) As Rectangle
            Dim r = Locate(frame)
            If ReadAt(frame, r).HasValue Then Return r
            For Each dy In {1, -1, 2, -2, 3, -3}
                Dim t = New Rectangle(r.X, r.Y + dy, r.Width, r.Height)
                If t.Y < 0 Then Continue For
                If ReadAt(frame, t).HasValue Then Return t
            Next
            Return r
        End Function

        Private Function ReadAt(frame As Bitmap, canvas As Rectangle) As Point?
            Dim c = WebCoordReader.RegionIn(canvas)
            c.Intersect(New Rectangle(0, 0, frame.Width, frame.Height))
            If c.Width < 60 OrElse c.Height < 96 Then Return Nothing
            Using crop = frame.Clone(c, frame.PixelFormat)
                Return WebCoordReader.Read(crop)
            End Using
        End Function

    End Module

End Namespace
