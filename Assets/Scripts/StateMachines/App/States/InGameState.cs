using CardboardCore.DI;
using CardboardCore.StateMachines;
using UnityEngine;

public class InGameState : State
{
    [Inject] private AppManager appManager;
    [Inject] private MatchService matchService;

    protected override void OnEnter()
    {
        // MatchNetworkSync is a scene object — one intentional lookup after scene load
        // MatchService is DDOL and provided via injection above
        var matchNetworkSync = Object.FindObjectOfType<MatchNetworkSync>();
        matchNetworkSync?.Initialize(matchService);

        appManager.MatchLifecycleStateMachine.MatchService = matchService;
        appManager.MatchLifecycleStateMachine.MatchNetworkSync = matchNetworkSync;
        appManager.MatchLifecycleStateMachine.Start();
    }

    protected override void OnExit() { }
}
