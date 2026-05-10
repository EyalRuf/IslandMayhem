using CardboardCore.DI;
using UnityEngine;

[Injectable]
public class AppManager : MonoBehaviour
{
    public AppStateMachine AppStateMachine { get; private set; }
    public LobbyStateMachine LobbyStateMachine { get; private set; }
    public MatchLifecycleStateMachine MatchLifecycleStateMachine { get; private set; }
    public PlayerStateMachine PlayerStateMachine { get; private set; }

    private void Awake()
    {
        AppStateMachine = new AppStateMachine();
        LobbyStateMachine = new LobbyStateMachine();
        MatchLifecycleStateMachine = new MatchLifecycleStateMachine();
        PlayerStateMachine = new PlayerStateMachine();

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AppStateMachine.Start();
        LobbyStateMachine.Start();
    }

    public void RequestHostLocal(CustomNetworkManager networkManager)
    {
        LobbyStateMachine.IsHosting = true;
        LobbyStateMachine.ToState<LoadingGameState>();
        networkManager.StartHost();
    }

    public void RequestJoinLocal(CustomNetworkManager networkManager, string address = "localhost")
    {
        LobbyStateMachine.IsHosting = false;
        LobbyStateMachine.ToState<LoadingGameState>();
        networkManager.networkAddress = address;
        networkManager.StartClient();
    }
}
