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
        SteamMatchmaking.AddRequestLobbyListStringFilter("game", "eyalgame", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    protected override void OnExit()
    {
        steamLobby.LobbyListReadyEvent -= OnLobbyListReady;
    }

    private void OnLobbyListReady() { } // UI wired in Chunk 4

    public void SelectLobby(CSteamID id)
    {
        owningStateMachine.SelectedLobbyId = id;
        owningStateMachine.ToNextState();
    }
}
