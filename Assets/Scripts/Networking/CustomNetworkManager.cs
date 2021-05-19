using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections.Generic;
using Steamworks;
using System.Linq;
using VivoxUnity;
using System.Collections;

/*
	Documentation: https://mirror-networking.com/docs/Components/NetworkManager.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class CustomNetworkManager : NetworkManager
{
    private const string PLAYER_ID_PREFIX = "Player_";
    private static Dictionary<string, NetworkIdentity> playersDic = new Dictionary<string, NetworkIdentity>();
    private static string localPlayerId;
    public static bool localPlayerInitialized { get; private set; }

    [Header("CustomManagerProperties")]
    public bool isSteam;

    [Header("References")]
    public MatchManager matchManager;

    void OnLevelWasLoaded(int level)
    {
        ResetManager();
    }

    public void ResetManager()
    {
        matchManager.ResetMatch();
        playersDic = new Dictionary<string, NetworkIdentity>();
        localPlayerId = null;
        localPlayerInitialized = false;
    }

    public static void RegisterPlayer(uint netId, NetworkIdentity go)
    {
        string id = PLAYER_ID_PREFIX + netId;
        playersDic.Add(id, go);
        go.transform.name = id;
    }

    public static void SetLocalPlayer(uint netId)
    {
        localPlayerInitialized = true;
        localPlayerId = PLAYER_ID_PREFIX + netId;
    }

    public static List<NetworkIdentity> GetAllPlayers() 
    {
        return playersDic.Values.ToList();
    }

    public static NetworkIdentity GetLocalPlayer()
    {
        if (localPlayerId == null)
            return null;

        if (playersDic.ContainsKey(localPlayerId))
            return playersDic[localPlayerId];
        return null;
    }

    public static void UnregisterPlayer(uint netId)
    {
        string id = PLAYER_ID_PREFIX + netId;
        playersDic.Remove(id);
    }

    public static NetworkIdentity GetPlayerByNetId (uint netId)
    {
        string id = PLAYER_ID_PREFIX + netId;
        return playersDic[id];
    }

    #region Matchmaking and Steamworks

    [Header("Matchmaking")]
    public float lobbysearchInterval = 1f;
    public List<CSteamID> lobbies = new List<CSteamID>();
    [HideInInspector]
    public bool matchmakingSearching = false;

    private bool updatedLobbies = false;

    private protected Callback<LobbyMatchList_t> Callback_lobbyList;
    private protected Callback<LobbyCreated_t> Callback_lobbyCreated;
    private CSteamID lobby = CSteamID.Nil;

    public void StartLobby()
    {
        Callback_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, matchManager.numberOfPlayersNeededToStart);
    }

    public void StopLobby()
    {
        Debug.Log("Left lobby with the following ID: " + lobby);

        lobby = CSteamID.Nil;
        SteamMatchmaking.LeaveLobby(lobby);
    }

    public void StartMatchmaking()
    {
        StartCoroutine(FindMatchProcess());
    }

    public void StopMatchmaking()
    {
        matchmakingSearching = false;
    }

    private IEnumerator FindMatchProcess()
    {
        matchmakingSearching = true;

        //run until found
        while (true)
        {
            //wait a lil bit
            yield return new WaitForSeconds(lobbysearchInterval);

            //request lobbies
            updatedLobbies = false;
            Callback_lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);
            SteamAPICall_t lobbyRequest = SteamMatchmaking.RequestLobbyList();

            //wait until we have lobbies
            //yield return new WaitUntil(() => updatedLobbies);

            if (matchmakingSearching)
            {
                if(lobbies.Count > 0)
                {
                    try
                    {
                        ulong lobbyOwnerAsInt = 0;

                        if (ulong.TryParse(SteamMatchmaking.GetLobbyData(lobby, "owner"), out lobbyOwnerAsInt))
                        {
                            CSteamID lobbyOwner = (CSteamID)lobbyOwnerAsInt;
                            networkAddress = lobbyOwner.ToString();
                            StartClient();
                            matchmakingSearching = false;
                        }
                    }
                    catch(System.Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                    yield return null;
                }
            }
            else
            {
                //stop searching
                yield return null;
            }
        }
    }

    private void OnGetLobbiesList(LobbyMatchList_t result)
    {
        lobbies.Clear();

        string lobbyResults = "Found " + result.m_nLobbiesMatching + " lobbies with the following IDs:\n";
        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbies.Add(lobbyID);
            lobbyResults += lobbyID.ToString() + "\n";
        }

        if (result.m_nLobbiesMatching != 0)
        {
            Debug.Log(lobbyResults);
        }
        updatedLobbies = true;
    }

    private void OnLobbyCreated(LobbyCreated_t result)
    {
        if(result.m_eResult == EResult.k_EResultOK && matchmakingSearching)
        {
            lobby = (CSteamID)result.m_ulSteamIDLobby;
            SteamMatchmaking.SetLobbyOwner(lobby, SteamUser.GetSteamID());
            SteamMatchmaking.SetLobbyData(lobby, "owner", SteamUser.GetSteamID().ToString());

            Debug.Log("Created a lobby with the following ID: " + (CSteamID)result.m_ulSteamIDLobby);
        }
    }

    #endregion

    #region Unity Callbacks

    public override void OnValidate()
    {
        base.OnValidate();
    }

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Start()
    {
        base.Start();
        SteamAPI.Init();
        SteamFriends.SetRichPresence("status", "In Menu");
        StartCoroutine(VivoxLogin());
    }

    IEnumerator VivoxLogin ()
    {
        yield return new WaitForSeconds(3);
        //_vivoxVoiceManager.Login(Time.deltaTime.ToString());
    }

    /// <summary>
    /// Runs on both Server and Client
    /// </summary>
    public override void LateUpdate()
    {
        SteamAPI.RunCallbacks();
        base.LateUpdate();
    }

    /// <summary>
    /// Runs on both Server and Client
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region Start & Stop

    /// <summary>
    /// Set the frame rate for a headless server.
    /// <para>Override if you wish to disable the behavior or set your own tick rate.</para>
    /// </summary>
    public override void ConfigureServerFrameRate()
    {
        base.ConfigureServerFrameRate();
    }

    /// <summary>
    /// called when quitting the application by closing the window / pressing stop in the editor
    /// </summary>
    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// This causes the server to switch scenes and sets the networkSceneName.
    /// <para>Clients that connect to this server will automatically switch to this scene. This is called automatically if onlineScene or offlineScene are set, but it can be called from user code to switch scenes again while the game is in progress. This automatically sets clients to be not-ready. The clients must call NetworkClient.Ready() again to participate in the new scene.</para>
    /// </summary>
    /// <param name="newSceneName"></param>
    public override void ServerChangeScene(string newSceneName)
    {
        base.ServerChangeScene(newSceneName);
    }

    /// <summary>
    /// Called from ServerChangeScene immediately before SceneManager.LoadSceneAsync is executed
    /// <para>This allows server to do work / cleanup / prep before the scene changes.</para>
    /// </summary>
    /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
    public override void OnServerChangeScene(string newSceneName) { base.OnServerChangeScene(newSceneName); }

    /// <summary>
    /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
    /// </summary>
    /// <param name="sceneName">The name of the new scene.</param>
    public override void OnServerSceneChanged(string sceneName) { base.OnServerSceneChanged(sceneName); }

    /// <summary>
    /// Called from ClientChangeScene immediately before SceneManager.LoadSceneAsync is executed
    /// <para>This allows client to do work / cleanup / prep before the scene changes.</para>
    /// </summary>
    /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
    /// <param name="sceneOperation">Scene operation that's about to happen</param>
    /// <param name="customHandling">true to indicate that scene loading will be handled through overrides</param>
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) { }

    /// <summary>
    /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
    /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
    /// </summary>
    /// <param name="conn">The network connection that the scene change message arrived on.</param>
    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);
    }

    #endregion

    #region Server System Callbacks

    /// <summary>
    /// Called on the server when a new client connects.
    /// <para>Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerConnect(NetworkConnection conn) 
    {
        base.OnServerConnect(conn);
    }

    /// <summary>
    /// Called on the server when a client is ready.
    /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerReady(NetworkConnection conn)
    {
        //sync info when player is ready
        matchManager.RpcSyncTeamInfo(JsonUtility.ToJson(new TeamInfo(matchManager.teams)));

        base.OnServerReady(conn);
    }

    /// <summary>
    /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
    /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);
    }

    /// <summary>
    /// Called on the server when a client disconnects.
    /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// Called on the server when a network error occurs for a client connection.
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    /// <param name="errorCode">Error code.</param>
    public override void OnServerError(NetworkConnection conn, int errorCode) { base.OnServerError(conn, errorCode); }

    #endregion

    #region Client System Callbacks

    /// <summary>
    /// Called on the client when connected to a server.
    /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        //var lobbychannel = _vivoxVoiceManager.ActiveChannels.FirstOrDefault(ac => ac.Channel.Name == "A");
        //if ((_vivoxVoiceManager && _vivoxVoiceManager.ActiveChannels.Count == 0)
        //    || lobbychannel == null)
        //{
        //    _vivoxVoiceManager.JoinChannel("A", ChannelType.Positional, VivoxVoiceManager.ChatCapability.AudioOnly);
        //}
        //else
        //{
        //    if (lobbychannel.AudioState == ConnectionState.Disconnected)
        //    {
        //        // Ask for hosts since we're already in the channel and part added won't be triggered.

        //        lobbychannel.BeginSetAudioConnected(true, true, ar =>
        //        {
        //            Debug.Log("Now transmitting into lobby channel");
        //        });
        //    }

        //}

    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
    }

    /// <summary>
    /// Called on clients when a network error occurs.
    /// </summary>
    /// <param name="conn">Connection to a server.</param>
    /// <param name="errorCode">Error code.</param>
    public override void OnClientError(NetworkConnection conn, int errorCode) { base.OnClientError(conn, errorCode); }

    /// <summary>
    /// Called on clients when a servers tells the client it is no longer ready.
    /// <para>This is commonly used when switching scenes.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientNotReady(NetworkConnection conn) { base.OnClientNotReady(conn); }

    #endregion

    #region Start & Stop Callbacks

    // Since there are multiple versions of StartServer, StartClient and StartHost, to reliably customize
    // their functionality, users would need override all the versions. Instead these callbacks are invoked
    // from all versions, so users only need to implement this one case.

    /// <summary>
    /// This is invoked when a host is started.
    /// <para>StartHost has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartHost() 
    {
        StartLobby();

        base.OnStartHost();
    }

    /// <summary>
    /// This is invoked when a server is started - including when a host is started.
    /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartServer() 
    {
        base.OnStartServer();
    }


    private OnClientStartStop[] onClientStartStops;

    /// <summary>
    /// This is invoked when the client is started.
    /// </summary>
    public override void OnStartClient() 
    {
        SteamFriends.SetRichPresence("status", "In Game");

        if(mode == NetworkManagerMode.Host)
        {
            SteamFriends.SetRichPresence("room", SteamUser.GetSteamID().ToString());
        }
        else
        {
            SteamFriends.SetRichPresence("room", networkAddress);
        }

        onClientStartStops = FindObjectsOfType<OnClientStartStop>();

        foreach(OnClientStartStop onClientStartStop in onClientStartStops)
        {
            onClientStartStop.OnClientStart();
        }
    }

    /// <summary>
    /// This is called when a host is stopped.
    /// </summary>
    public override void OnStopHost() 
    {
        StopLobby();

        base.OnStopHost();
        ResetManager();
    }

    /// <summary>
    /// This is called when a server is stopped - including when a host is stopped.
    /// </summary>
    public override void OnStopServer() 
    {
        base.OnStopServer();
    }

    /// <summary>
    /// This is called when a client is stopped.
    /// </summary>
    public override void OnStopClient() 
    {
        base.OnStopClient();
        SteamFriends.SetRichPresence("status", "In Menu");
        SteamFriends.SetRichPresence("room", "");

        ResetManager();

        foreach (OnClientStartStop onClientStartStop in onClientStartStops)
        {
            onClientStartStop.OnClientStart();
        }
    }

    #endregion
}
