using CardboardCore.StateMachines;
using Steamworks;

public class LobbyStateMachine : StateMachine
{
    public CSteamID SelectedLobbyId { get; set; }
    public bool IsHosting { get; set; }

    public LobbyStateMachine() : base(enableDebugging: true)
    {
        SetInitialState<LobbyIdleState>();

        // Happy path flow
        AddStaticTransition<CreatingLobbyState, InLobbyState>();
        AddStaticTransition<BrowsingLobbiesState, JoiningLobbyState>();
        AddStaticTransition<JoiningLobbyState, InLobbyState>();
        AddStaticTransition<InLobbyState, LobbyLoadingState>();
        AddStaticTransition<LobbyLoadingState, LobbySessionActiveState>();
        AddStaticTransition<LobbySessionActiveState, LobbyIdleState>();
        AddStaticTransition<LobbyReconnectingState, LobbySessionActiveState>();

        // User choices from Idle
        AddFreeFlowTransition<LobbyIdleState, CreatingLobbyState>();
        AddFreeFlowTransition<LobbyIdleState, BrowsingLobbiesState>();
        AddFreeFlowTransition<LobbyIdleState, LobbyReconnectingState>();
        AddFreeFlowTransition<LobbyIdleState, LobbyLoadingState>(); // local/direct — no lobby needed

        // Exceptional exits
        AddFreeFlowTransition<CreatingLobbyState, LobbyIdleState>();
        AddFreeFlowTransition<BrowsingLobbiesState, LobbyIdleState>();
        AddFreeFlowTransition<JoiningLobbyState, LobbyIdleState>();
        AddFreeFlowTransition<InLobbyState, LobbyIdleState>();
        AddFreeFlowTransition<LobbyLoadingState, LobbyIdleState>();
        AddFreeFlowTransition<LobbySessionActiveState, LobbyReconnectingState>();
        AddFreeFlowTransition<LobbyReconnectingState, LobbyIdleState>();
    }
}
