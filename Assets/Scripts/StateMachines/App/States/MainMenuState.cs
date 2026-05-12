using Assets.Scripts.UI;
using CardboardCore.DI;
using CardboardCore.StateMachines;

public class MainMenuState : State
{
    [Inject] private CustomNetworkManager networkManager;
    [Inject] private MenuUIManager menuUIManager;

    protected override void OnEnter()
    {
        menuUIManager.ShowMainScreen();
        networkManager.GameSceneReadyEvent += OnGameSceneReady;
    }

    protected override void OnExit()
    {
        networkManager.GameSceneReadyEvent -= OnGameSceneReady;
    }

    private void OnGameSceneReady()
    {
        networkManager.GameSceneReadyEvent -= OnGameSceneReady;
        owningStateMachine.ToNextState();
    }
}
