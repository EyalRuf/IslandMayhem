using CardboardCore.DI;
using CardboardCore.StateMachines;
using System.IO;
using UnityEngine.SceneManagement;

public class MainMenuState : State
{
    [Inject] private MenuManager menuManager;
    [Inject] private CustomNetworkManager networkManager;

    protected override void OnEnter()
    {
        menuManager.ShowMainScreen();
        networkManager.GameSceneReadyEvent += OnGameSceneReady;
    }

    protected override void OnExit()
    {
        networkManager.GameSceneReadyEvent -= OnGameSceneReady;
    }

    private void OnGameSceneReady()
    {
        if (SceneManager.GetActiveScene().name == Path.GetFileNameWithoutExtension(networkManager.onlineScene))
        {
            networkManager.GameSceneReadyEvent -= OnGameSceneReady;
            owningStateMachine.ToNextState();
        }
    }
}
