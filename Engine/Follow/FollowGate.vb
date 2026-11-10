Namespace Engine.Follow

    ''' <summary>
    ''' Shared switch between following and input mirroring. While the mirror is on, FollowController lets every follower's
    ''' mouse go and skips its rounds: a held walking press would mix with the mirrored clicks / drags in the same view.
    ''' Set / read on the UI thread.
    ''' </summary>
    Public Module FollowGate
        Public Property MirrorActive As Boolean
    End Module

End Namespace
