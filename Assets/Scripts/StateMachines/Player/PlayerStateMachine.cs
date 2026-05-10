using CardboardCore.StateMachines;

public class PlayerStateMachine : StateMachine
{
    public PlayerStateMachine() : base(enableDebugging: true)
    {
        SetInitialState<AliveState>();

        // Downed cycle
        AddStaticTransition<AliveState, DownedState>();
        AddStaticTransition<DownedState, AliveState>();
    }
}
