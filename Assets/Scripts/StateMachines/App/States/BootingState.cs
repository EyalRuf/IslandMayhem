using CardboardCore.DI;
using CardboardCore.StateMachines;

public class BootingState : State
{
    [Inject] private SessionData sessionData;
    [Inject] private MenuManager menuManager;

    protected override void OnEnter()
    {
        if (SteamManager.Initialized)
        {
            sessionData.Load();
            menuManager.UIReady += OnUIReady;
            menuManager.NotifyIfReady();
        }
        else
        {
            owningStateMachine.ToState<BootFailedState>();
        }
    }

    protected override void OnExit()
    {
        menuManager.UIReady -= OnUIReady;
    }

    private void OnUIReady()
    {
        owningStateMachine.ToNextState();
    }
}
