using Assets.Scripts.Networking;
using CardboardCore.DI;
using CardboardCore.StateMachines;
using Steamworks;
using System.Collections;
using UnityEngine;

public class LobbyLoadingState : State<LobbyStateMachine>
{
    private const float MinLoadingDuration = 1.5f;

    [Inject] private CustomNetworkManager networkManager;
    [Inject] private MenuManager menuManager;
    [Inject] private AppManager appManager;

    private float enterTime;

    protected override void OnEnter()
    {
        enterTime = Time.time;
        menuManager.ShowLoadingScreen();

        if (owningStateMachine.IsHosting)
        {
            networkManager.HostStartedEvent += OnSessionReady;
            networkManager.StartHost();
        }
        else
        {
            networkManager.ClientStartedEvent += OnSessionReady;
            if (networkManager.isSteam && owningStateMachine.SelectedLobbyId.IsValid())
            {
                string hostAddress = SteamMatchmaking.GetLobbyData(
                    owningStateMachine.SelectedLobbyId, SteamLobby.HostAddressKey);
                networkManager.networkAddress = hostAddress;
            }
            networkManager.StartClient();
        }
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
