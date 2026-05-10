using Assets.Scripts.UI;
using CardboardCore.DI;
using CardboardCore.StateMachines;
using System.Collections;
using UnityEngine;

public class LoadingGameState : State<LobbyStateMachine>
{
    private const float MinLoadingDuration = 1.5f;

    [Inject] private CustomNetworkManager networkManager;
    [Inject] private MenuUIManager menuUIManager;
    [Inject] private AppManager appManager;

    private float enterTime;

    protected override void OnEnter()
    {
        enterTime = Time.time;
        menuUIManager.ShowLoadingScreen();
        if (owningStateMachine.IsHosting)
            networkManager.HostStartedEvent += OnSessionReady;
        else
            networkManager.ClientStartedEvent += OnSessionReady;
    }

    protected override void OnExit()
    {
        networkManager.HostStartedEvent -= OnSessionReady;
        networkManager.ClientStartedEvent -= OnSessionReady;
    }

    private void OnSessionReady()
    {
        networkManager.HostStartedEvent -= OnSessionReady;
        networkManager.ClientStartedEvent -= OnSessionReady;
        appManager.StartCoroutine(WaitAndTransition());
    }

    private IEnumerator WaitAndTransition()
    {
        float remaining = MinLoadingDuration - (Time.time - enterTime);
        if (remaining > 0)
            yield return new WaitForSeconds(remaining);
        owningStateMachine.ToNextState();
    }
}
