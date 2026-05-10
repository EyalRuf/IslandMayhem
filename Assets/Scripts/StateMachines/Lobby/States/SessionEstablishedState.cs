using Assets.Scripts.UI;
using CardboardCore.DI;
using CardboardCore.StateMachines;

public class SessionEstablishedState : State
{
    [Inject] private CustomNetworkManager networkManager;
    [Inject] private MenuUIManager menuUIManager;

    protected override void OnEnter()
    {
        menuUIManager.HideAll();
        networkManager.ClientStoppedEvent += OnClientStopped;
    }

    protected override void OnExit()
    {
        networkManager.ClientStoppedEvent -= OnClientStopped;
    }

    private void OnClientStopped() => owningStateMachine.ToState<ReconnectingState>();
}
