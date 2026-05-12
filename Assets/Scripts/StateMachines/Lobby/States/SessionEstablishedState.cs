using CardboardCore.DI;
using CardboardCore.StateMachines;

public class SessionEstablishedState : State
{
    [Inject] private CustomNetworkManager networkManager;

    protected override void OnEnter()
    {
        networkManager.ClientStoppedEvent += OnClientStopped;
    }

    protected override void OnExit()
    {
        networkManager.ClientStoppedEvent -= OnClientStopped;
    }

    private void OnClientStopped() => owningStateMachine.ToState<ReconnectingState>();
}
