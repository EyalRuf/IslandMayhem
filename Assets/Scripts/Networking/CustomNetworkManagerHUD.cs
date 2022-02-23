using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Steamworks;

/// <summary>
/// An extension for the NetworkManager that displays a default HUD for controlling the network state of the game.
/// <para>This component also shows useful internal state for the networking system in the inspector window of the editor. It allows users to view connections, networked objects, message handlers, and packet statistics. This information can be helpful when debugging networked games.</para>
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager), typeof(CustomNetworkManager))]
public class CustomNetworkManagerHUD : MonoBehaviour
{
    public CustomNetworkManager customManager;

    /// <summary>
    /// Whether to show the default control HUD at runtime.
    /// </summary>
    public bool showGUI = true;

    /// <summary>
    /// The horizontal offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    public int offsetX;

    /// <summary>
    /// The vertical offset in pixels to draw the HUD runtime GUI at.
    /// </summary>
    public int offsetY;

    private bool joiningFriends;
    private Dictionary<int, string> friendsInGame = new Dictionary<int, string>();
    private string targetScene;

    void Awake()
    {
        customManager = GetComponent<CustomNetworkManager>();
    }

    void OnGUI()
    {
        if (!showGUI)
            return;

        GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            StartButtons();

            MatchMakingButtons();
        }
        else
        {
            StatusLabels();
        }

        // client ready
        if (NetworkClient.isConnected && !ClientScene.ready)
        {
            if (GUILayout.Button("Client Ready"))
            {
                ClientScene.Ready(NetworkClient.connection);

                if (ClientScene.localPlayer == null)
                {
                    ClientScene.AddPlayer(NetworkClient.connection);
                }
            }
        }

        StopButtons();

        GUILayout.EndArea();
    }

    void MatchMakingButtons()
    {
        //if (!customManager.matchmakingSearching)
        //{
        //    if (GUILayout.Button("Start Matchmaking"))
        //    {
        //        customManager.StartMatchmaking();
        //    }
        //}
        //else
        //{
        //    if (GUILayout.Button("Stop Matchmaking"))
        //    {
        //        customManager.StopMatchmaking();
        //    }
        //}
    }

    void StartButtons()
    {
        if (!NetworkClient.active)
        {
            if (!joiningFriends)
            {
                // Server + Client
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    if (GUILayout.Button("Host (Server + Client)"))
                    {
                        if (customManager.isSteam)
                            customManager.networkAddress = SteamUser.GetSteamID().ToString();
                        customManager.StartHost();
                    }
                }

                // Client + IP
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Client"))
                {
                    customManager.StartClient();
                }
                customManager.networkAddress = GUILayout.TextField(customManager.networkAddress);
                GUILayout.EndHorizontal();

                // Server Only
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    // cant be a server in webgl build
                    GUILayout.Box("(  WebGL cannot be server  )");
                }
                else
                {
                    if (GUILayout.Button("Server Only")) customManager.StartServer();
                }

                if (GUILayout.Button("Join Friends"))
                {
                    joiningFriends = true;
                    GetFriendsInGame();
                }
            }
            else
            {
                if (GUILayout.Button("Cancel"))
                {
                    joiningFriends = false;
                }

                if (GUILayout.Button("Refresh"))
                {
                    GetFriendsInGame();
                }

                GUILayout.Space(10f);

                //buttons for all friends
                if(friendsInGame.Count > 0)
                {
                    foreach (KeyValuePair<int, string> friend in friendsInGame)
                    {
                        if (GUILayout.Button("Join " + friend.Value))
                        {
                            customManager.networkAddress = SteamFriends.GetFriendRichPresence(SteamFriends.GetFriendByIndex(friend.Key, EFriendFlags.k_EFriendFlagAll), "room");
                            customManager.StartClient();
                        }
                    }
                }
                else
                {
                    GUILayout.Label("No friends are in game.");
                }
            }
        }
        else
        {
            // Connecting
            GUILayout.Label("Connecting to " + customManager.networkAddress + "..");
            if (GUILayout.Button("Cancel Connection Attempt"))
            {
                customManager.StopClient();
            }
        }
    }

    void StatusLabels()
    {
        // server / client status message
        if (NetworkServer.active)
        {
            GUILayout.Label("Server: active. Transport: " + Transport.activeTransport);
        }
        if (NetworkClient.isConnected)
        {
            GUILayout.Label("Client: address=" + customManager.networkAddress);
        }
    }

    void StopButtons()
    {
        // stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            if (GUILayout.Button("Stop Host"))
            {
                customManager.StopHost();
            }

            // Client + IP
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Switch Scene"))
            {
                NetworkManager.singleton.ServerChangeScene(targetScene);
            }
            targetScene = GUILayout.TextField(targetScene);
            GUILayout.EndHorizontal();
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            if (GUILayout.Button("Stop Client"))
            {
                customManager.StopClient();
            }
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            if (GUILayout.Button("Stop Server"))
            {
                customManager.StopServer();
            }
        }
    }

    private void GetFriendsInGame()
    {
        friendsInGame = new Dictionary<int, string>();

        for (int f = 0; f < SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll); f++)
        {
            CSteamID friendID = SteamFriends.GetFriendByIndex(f, EFriendFlags.k_EFriendFlagAll);
            string friendStatus = SteamFriends.GetFriendRichPresence(friendID, "status");

            if (friendStatus == "In Game")
            {
                friendsInGame.Add(f, SteamFriends.GetFriendPersonaName(friendID));
            }
        }
    }
}