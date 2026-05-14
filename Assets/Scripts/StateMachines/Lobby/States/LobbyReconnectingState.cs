using CardboardCore.DI;
using CardboardCore.StateMachines;

public class LobbyReconnectingState : State
{
    [Inject] private SessionData sessionData;
    [Inject] private CustomNetworkManager networkManager;

    protected override void OnEnter()
    {
        networkManager.ClientStartedEvent += OnClientStarted;
        // Full reconnect attempt logic — Chunk 7
    }

    protected override void OnExit()
    {
        networkManager.ClientStartedEvent -= OnClientStarted;
    }

    private void OnClientStarted() => owningStateMachine.ToNextState();
}
