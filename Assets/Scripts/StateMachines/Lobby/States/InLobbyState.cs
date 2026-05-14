using CardboardCore.StateMachines;

public class InLobbyState : State<LobbyStateMachine>
{
    protected override void OnEnter() => owningStateMachine.ToNextState(); // static → LobbyLoadingState
    protected override void OnExit() { }
}
