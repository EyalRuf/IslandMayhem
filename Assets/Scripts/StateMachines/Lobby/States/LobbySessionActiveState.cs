using CardboardCore.DI;
using CardboardCore.StateMachines;

public class LobbySessionActiveState : State
{
    [Inject] private CustomNetworkManager networkManager;

    protected override void OnEnter()
    {
        networkManager.HostStoppedEvent += OnHostStopped;
        networkManager.ClientStoppedEvent += OnClientStopped;
    }

    protected override void OnExit()
    {
        networkManager.HostStoppedEvent -= OnHostStopped;
        networkManager.ClientStoppedEvent -= OnClientStopped;
    }

    // HostStoppedEvent fires first on host stop — transition out before ClientStoppedEvent is heard
    private void OnHostStopped() => owningStateMachine.ToNextState();
    private void OnClientStopped() => owningStateMachine.ToState<LobbyReconnectingState>();
}
