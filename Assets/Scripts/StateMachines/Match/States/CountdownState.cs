using CardboardCore.StateMachines;

public class CountdownState : State
{
    private MatchService matchService;

    protected override void OnEnter()
    {
        matchService = (owningStateMachine as MatchLifecycleStateMachine).MatchService;
        matchService.GameStartedEvent += OnGameStarted;
    }

    protected override void OnExit()
    {
        matchService.GameStartedEvent -= OnGameStarted;
    }

    // GameStartedEvent fires after countdown completes and game is live
    private void OnGameStarted() => owningStateMachine.ToNextState();
}
