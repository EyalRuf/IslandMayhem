using CardboardCore.StateMachines;

public class InProgressState : State
{
    private MatchService matchService;

    protected override void OnEnter()
    {
        matchService = (owningStateMachine as MatchLifecycleStateMachine).MatchService;
        matchService.GameOverEvent += OnGameOver;
    }

    protected override void OnExit()
    {
        matchService.GameOverEvent -= OnGameOver;
    }

    private void OnGameOver(int teamIndex) => owningStateMachine.ToNextState();
}
