using CardboardCore.DI;
using CardboardCore.StateMachines;

public class BootFailedState : State
{
    [Inject] private BootFailedUI bootFailedUI;

    protected override void OnEnter() => bootFailedUI.ShowError();
    protected override void OnExit() { }
}
