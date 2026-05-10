using CardboardCore.StateMachines;
using Steamworks;

public class LobbyStateMachine : StateMachine
{
    public CSteamID SelectedLobbyId { get; set; }
    public bool IsHosting { get; set; }

    public LobbyStateMachine() : base(enableDebugging: true)
    {
        SetInitialState<IdleState>();

        // Happy path flow
        AddStaticTransition<CreatingLobbyState, InLobbyState>();
        AddStaticTransition<BrowsingLobbiesState, JoiningLobbyState>();
        AddStaticTransition<JoiningLobbyState, InLobbyState>();
        AddStaticTransition<InLobbyState, LoadingGameState>();
        AddStaticTransition<LoadingGameState, SessionEstablishedState>();
        AddStaticTransition<SessionEstablishedState, IdleState>();
        AddStaticTransition<ReconnectingState, SessionEstablishedState>();

        // User choices from Idle
        AddFreeFlowTransition<IdleState, CreatingLobbyState>();
        AddFreeFlowTransition<IdleState, BrowsingLobbiesState>();
        AddFreeFlowTransition<IdleState, ReconnectingState>();
        AddFreeFlowTransition<IdleState, LoadingGameState>(); // local/direct connection — no lobby needed

        // Exceptional exits
        AddFreeFlowTransition<CreatingLobbyState, IdleState>();
        AddFreeFlowTransition<BrowsingLobbiesState, IdleState>();
        AddFreeFlowTransition<JoiningLobbyState, IdleState>();
        AddFreeFlowTransition<InLobbyState, IdleState>();
        AddFreeFlowTransition<LoadingGameState, IdleState>();
        AddFreeFlowTransition<SessionEstablishedState, ReconnectingState>();
        AddFreeFlowTransition<ReconnectingState, IdleState>();
    }
}
