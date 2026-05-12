using CardboardCore.StateMachines;

public class SetupPhaseState : State
{
    private MatchService matchService;

    protected override void OnEnter()
    {
        matchService = (owningStateMachine as MatchLifecycleStateMachine).MatchService;
        matchService.CountdownTickEvent += OnCountdownTick;
    }

    protected override void OnExit()
    {
        matchService.CountdownTickEvent -= OnCountdownTick;
    }

    // First countdown tick signals game is starting — transition to CountdownState
    private void OnCountdownTick(string tick) => owningStateMachine.ToNextState();
}
