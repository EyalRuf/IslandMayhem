using CardboardCore.StateMachines;

public class AppStateMachine : StateMachine
{
    public AppStateMachine() : base(enableDebugging: true)
    {
        SetInitialState<BootingState>();

        // Happy path flow
        AddStaticTransition<BootingState, MainMenuState>();
        AddStaticTransition<MainMenuState, InGameState>();
        AddStaticTransition<InGameState, MainMenuState>();

        // Exceptional exits
        AddFreeFlowTransition<BootingState, BootFailedState>();
    }
}
