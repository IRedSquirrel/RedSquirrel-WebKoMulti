Namespace Engine.Follow

    ''' <summary>
    ''' How long the follow loop waits before the next round (side-effect-free, follow-selftest checks it). Every round is a
    ''' whole-view capture of every in-game view, so a calm group is read seldom:
    '''  • fast (120 ms) while the leader's coordinate changes, any follower is walking / can't read, or the leader is unknown;
    '''  • slow (450 ms) once the leader has stood still AND every follower has been beside it (or out of range) for 1,5 s;
    '''  • back to fast on the very first round the leader's coordinate changes.
    ''' Seen from the leader: it is read seldom only while it is not moving; its first step is noticed within one slow round.
    ''' </summary>
    Public NotInheritable Class FollowPace
        Public Const FastMs As Integer = 120
        Public Const SlowMs As Integer = 450
        Public Const CalmAfterMs As Long = 1500

        Private _leader As Point?
        Private _calmSince As Long = Long.MinValue

        ''' <summary>True when the last answer was the slow interval.</summary>
        Public ReadOnly Property IsSlow As Boolean

        ''' <param name="leader">The leader's (filtered) coordinate this round; Nothing = not readable.</param>
        ''' <param name="followersSettled">Every follower's step this round was "beside the leader" or "out of range".</param>
        Public Function NextDelay(leader As Point?, followersSettled As Boolean, nowMs As Long) As Integer
            Dim moved = Not leader.HasValue OrElse Not _leader.HasValue OrElse leader.Value <> _leader.Value
            _leader = leader
            If moved OrElse Not followersSettled Then
                _calmSince = nowMs
                _IsSlow = False
                Return FastMs
            End If
            If _calmSince = Long.MinValue Then _calmSince = nowMs
            _IsSlow = nowMs - _calmSince >= CalmAfterMs
            Return If(_IsSlow, SlowMs, FastMs)
        End Function

        Public Sub Reset()
            _leader = Nothing
            _calmSince = Long.MinValue
            _IsSlow = False
        End Sub
    End Class

End Namespace
