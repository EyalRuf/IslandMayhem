using Assets.Scripts.Networking;
using CardboardCore.DI;
using CardboardCore.StateMachines;
using Steamworks;

public class JoiningLobbyState : State<LobbyStateMachine>
{
    [Inject] private SteamLobby steamLobby;

    protected override void OnEnter()
    {
        steamLobby.LobbyEnteredEvent += OnLobbyEntered;
        SteamMatchmaking.JoinLobby(owningStateMachine.SelectedLobbyId);
    }

    protected override void OnExit()
    {
        steamLobby.LobbyEnteredEvent -= OnLobbyEntered;
    }

    private void OnLobbyEntered() => owningStateMachine.ToNextState();
}
