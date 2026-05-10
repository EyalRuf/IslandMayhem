using CardboardCore.DI;
using CardboardCore.StateMachines;

public class BootingState : State
{
    [Inject] private SessionData sessionData;

    protected override void OnEnter()
    {
        if (SteamManager.Initialized)
        {
            sessionData.Load();
            owningStateMachine.ToNextState();
        }
        else
        {
            owningStateMachine.ToState<BootFailedState>();
        }
    }

    protected override void OnExit() { }
}
