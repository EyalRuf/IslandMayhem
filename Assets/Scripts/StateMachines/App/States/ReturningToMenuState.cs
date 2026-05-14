using CardboardCore.DI;
using CardboardCore.StateMachines;

public class ReturningToMenuState : State
{
    [Inject] private AppManager appManager;
    [Inject] private MenuManager menuManager;

    protected override void OnEnter()
    {
        appManager.ShowLoadingScreen();
        menuManager.UIReady += OnMenuUIReady;
        menuManager.NotifyIfReady();
    }

    protected override void OnExit()
    {
        menuManager.UIReady -= OnMenuUIReady;
    }

    private void OnMenuUIReady()
    {
        appManager.HideLoadingScreen();
        owningStateMachine.ToNextState();
    }
}
