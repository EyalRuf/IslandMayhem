using CardboardCore.DI;
using CardboardCore.StateMachines;
using UnityEngine;

public class InGameState : State
{
    [Inject] private AppManager appManager;
    [Inject] private MatchService matchService;
    [Inject] private CustomNetworkManager networkManager;

    protected override void OnEnter()
    {
        var matchNetworkSync = Object.FindObjectOfType<MatchNetworkSync>();
        matchNetworkSync?.Initialize(matchService);

        appManager.MatchLifecycleStateMachine.MatchService = matchService;
        appManager.MatchLifecycleStateMachine.MatchNetworkSync = matchNetworkSync;
        appManager.MatchLifecycleStateMachine.Start();

        networkManager.HostStoppedEvent += OnNetworkStopped;
        networkManager.ClientStoppedEvent += OnNetworkStopped;
    }

    protected override void OnExit()
    {
        networkManager.HostStoppedEvent -= OnNetworkStopped;
        networkManager.ClientStoppedEvent -= OnNetworkStopped;
    }

    private void OnNetworkStopped() => owningStateMachine.ToNextState(); // static → MainMenuState
}
