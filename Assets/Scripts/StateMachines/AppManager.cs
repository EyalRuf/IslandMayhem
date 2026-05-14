using CardboardCore.DI;
using UnityEngine;

[Injectable]
public class AppManager : MonoBehaviour
{
    public AppStateMachine AppStateMachine { get; private set; }
    public LobbyStateMachine LobbyStateMachine { get; private set; }
    public MatchLifecycleStateMachine MatchLifecycleStateMachine { get; private set; }
    public PlayerStateMachine PlayerStateMachine { get; private set; }

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private CustomNetworkManager networkManager;

    private static AppManager _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _instance = this;
        }

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

    public void ShowLoadingScreen() => loadingScreen.SetActive(true);
    public void HideLoadingScreen() => loadingScreen.SetActive(false);

    // TODO: remove — temporary back-navigation shortcut for testing leave-match flow.
    // F12 → StopHost/StopClient → HostStoppedEvent → InGameState.OnNetworkStopped → ReturningToMenuState → MainMenuState.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            if (networkManager.mode == Mirror.NetworkManagerMode.Host) networkManager.StopHost();
            else if (networkManager.mode == Mirror.NetworkManagerMode.ClientOnly) networkManager.StopClient();
        }
    }

    public void RequestHostSteam()
    {
        LobbyStateMachine.IsHosting = true;
        LobbyStateMachine.ToState<CreatingLobbyState>();
    }

    public void RequestBrowseLobbies()
    {
        LobbyStateMachine.IsHosting = false;
        LobbyStateMachine.ToState<BrowsingLobbiesState>();
    }

    public void RequestLeave()
    {
        LobbyStateMachine.ToState<LobbyIdleState>();
    }

    public void RequestHostLocal()
    {
        LobbyStateMachine.IsHosting = true;
        LobbyStateMachine.ToState<LobbyLoadingState>();
    }

    public void RequestJoinLocal(string address = "localhost")
    {
        LobbyStateMachine.IsHosting = false;
        networkManager.networkAddress = address;
        LobbyStateMachine.ToState<LobbyLoadingState>();
    }
}
