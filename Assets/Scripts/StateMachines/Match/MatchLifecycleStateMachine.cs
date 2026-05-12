using CardboardCore.StateMachines;

public class MatchLifecycleStateMachine : StateMachine
{
    public MatchService MatchService { get; set; }
    public MatchNetworkSync MatchNetworkSync { get; set; }

    public MatchLifecycleStateMachine() : base(enableDebugging: true)
    {
        SetInitialState<SetupPhaseState>();

        // Linear flow
        AddStaticTransition<SetupPhaseState, CountdownState>();
        AddStaticTransition<CountdownState, InProgressState>();
        AddStaticTransition<InProgressState, GameOverState>();
    }
}
