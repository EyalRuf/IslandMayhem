using CardboardCore.StateMachines;

public class MatchLifecycleStateMachine : StateMachine
{
    public MatchLifecycleStateMachine() : base(enableDebugging: true)
    {
        SetInitialState<SetupPhaseState>();

        // Linear flow
        AddStaticTransition<SetupPhaseState, CountdownState>();
        AddStaticTransition<CountdownState, InProgressState>();
        AddStaticTransition<InProgressState, GameOverState>();
    }
}
