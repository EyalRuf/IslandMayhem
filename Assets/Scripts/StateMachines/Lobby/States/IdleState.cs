using Assets.Scripts.UI;
using CardboardCore.DI;
using CardboardCore.StateMachines;

public class IdleState : State
{
    [Inject] private MenuUIManager menuUIManager;

    protected override void OnEnter() => menuUIManager.ShowMainScreen();
    protected override void OnExit() { }
}
