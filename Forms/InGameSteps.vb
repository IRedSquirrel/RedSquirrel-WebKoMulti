Imports System.Threading
Imports System.Threading.Tasks
Imports RedSquirrel.WebKoMulti.Core.Session

''' <summary>
''' The UI's way to Engine.Login.WebLogin.RunInGameStepsAsync: close windows, mute, close PMs, apply the game options
''' (forceSetup = True — the one-time mark is ignored) on a session that is already in game. No login.
''' </summary>
Friend Module InGameSteps

    Friend ReadOnly Property Available As Boolean
        Get
            Return True
        End Get
    End Property

    Friend Function Run(session As GameSession, ct As CancellationToken) As Task(Of Boolean)
        Return Engine.Login.WebLogin.RunInGameStepsAsync(session, ct, True)
    End Function

End Module
