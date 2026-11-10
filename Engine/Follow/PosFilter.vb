Namespace Engine.Follow

    ''' <summary>
    ''' Smooths one character's coordinate reads for following:
    '''  • an unreadable frame keeps the last good position for up to 1.5 s (a single miss must not stop the walk);
    '''  • a jump of more than 25 units is accepted only when the next read agrees (one misread digit must not look like a teleport).
    ''' </summary>
    Public NotInheritable Class PosFilter

        Private Const HoldMs As Long = 1500
        Private Const JumpUnits As Integer = 25

        Private _good As Point?
        Private _goodT As Long
        Private _pending As Point?

        Public Function Update(raw As Point?) As Point?
            Return Update(raw, FollowClock.NowMs)
        End Function

        ''' <summary>Same, with the caller's clock (the self-test runs on virtual time).</summary>
        Public Function Update(raw As Point?, nowMs As Long) As Point?
            If raw.HasValue Then
                If Not _good.HasValue OrElse Dist(raw.Value, _good.Value) <= JumpUnits Then
                    _good = raw : _goodT = nowMs : _pending = Nothing
                ElseIf _pending.HasValue AndAlso Dist(raw.Value, _pending.Value) <= 6 Then
                    _good = raw : _goodT = nowMs : _pending = Nothing     ' confirmed jump (town, teleport)
                Else
                    _pending = raw                                           ' wait for a second read
                End If
            End If
            If _good.HasValue AndAlso nowMs - _goodT <= HoldMs Then Return _good
            Return Nothing
        End Function

        Public Sub Reset()
            _good = Nothing : _pending = Nothing
        End Sub

        Private Shared Function Dist(a As Point, b As Point) As Double
            Return Math.Sqrt((a.X - b.X) ^ 2 + (a.Y - b.Y) ^ 2)
        End Function

    End Class

End Namespace
