using Assets.Scripts.Networking;
using CardboardCore.DI;
using CardboardCore.StateMachines;

public class LobbyIdleState : State
{
    [Inject] private CustomNetworkManager networkManager;
    [Inject] private SteamLobby steamLobby;

    protected override void OnEnter()
    {
        networkManager?.ResetManager();
        steamLobby?.LeaveLobby();
    }

    protected override void OnExit() { }
}
