using Assets.Scripts.Networking;
using CardboardCore.DI;
using CardboardCore.StateMachines;
using Steamworks;

public class CreatingLobbyState : State
{
    [Inject] private SteamLobby steamLobby;

    protected override void OnEnter()
    {
        steamLobby.LobbyCreatedEvent += OnLobbyCreated;
        steamLobby.LobbyCreateFailedEvent += OnFailed;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
    }

    protected override void OnExit()
    {
        steamLobby.LobbyCreatedEvent -= OnLobbyCreated;
        steamLobby.LobbyCreateFailedEvent -= OnFailed;
    }

    private void OnLobbyCreated() => owningStateMachine.ToNextState();
    private void OnFailed() => owningStateMachine.ToState<IdleState>();
}
