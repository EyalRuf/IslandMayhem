using Assets.Scripts.Networking;
using CardboardCore.DI;
using CardboardCore.StateMachines;
using Steamworks;

public class BrowsingLobbiesState : State<LobbyStateMachine>
{
    [Inject] private SteamLobby steamLobby;

    protected override void OnEnter()
    {
        steamLobby.LobbyListReadyEvent += OnLobbyListReady;
        steamLobby.LobbySelectedEvent += OnLobbySelected;
        SteamMatchmaking.AddRequestLobbyListStringFilter("game", "eyalgame", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    protected override void OnExit()
    {
        steamLobby.LobbyListReadyEvent -= OnLobbyListReady;
        steamLobby.LobbySelectedEvent -= OnLobbySelected;
    }

    private void OnLobbyListReady() { } // UI items created by SteamLobby.OnLobbyMatchList

    private void OnLobbySelected(CSteamID id)
    {
        owningStateMachine.SelectedLobbyId = id;
        owningStateMachine.ToNextState(); // static → JoiningLobbyState
    }
}
