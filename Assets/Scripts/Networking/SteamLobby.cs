using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Networking
{
    public class SteamLobby : MonoBehaviour
    {
        public NetworkManager networkManager;

        private string HostAdressKey = "HostAddress";

        #region Steam Callbacks
        protected Callback<LobbyCreated_t> lobbyCreated;
        protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
        protected Callback<LobbyEnter_t> lobbyEntered;
        protected Callback<LobbyMatchList_t> lobbyMatchList;
        #endregion

        public List<CSteamID> lobbyIds = new List<CSteamID>();
        public List<int> lobbyMemberCounts = new List<int>();

        private void Start()
        {
            networkManager = GetComponent<NetworkManager>();

            if (!SteamManager.Initialized) 
                return;

            #region Steam Callbacks Initialization
            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
            #endregion

        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                HostLobby();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                SteamMatchmaking.RequestLobbyList();
            }
        }

        public void HostLobby()
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);
        }

        public void OnLobbyCreated (LobbyCreated_t callback)
        {
            if (callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError("LobbyFailed");
                return;
            }
            CSteamID lobby = new CSteamID(callback.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(lobby, "owner", SteamUser.GetSteamID().ToString());
            SteamMatchmaking.SetLobbyData(lobby, "ownerName", SteamFriends.GetPersonaName().ToString());
            SteamMatchmaking.SetLobbyData(lobby, "plrAmount", "1");
            networkManager.StartHost();
            Debug.Log("started host");

            SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAdressKey, SteamUser.GetSteamID().ToString());
        }

        public void OnGameLobbyJoinRequested (GameLobbyJoinRequested_t callback)
        {
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        public void OnLobbyEntered (LobbyEnter_t callback)
        {
            if (NetworkServer.active) return;

            string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAdressKey);

            networkManager.networkAddress = hostAddress;
            networkManager.StartClient();

            Debug.Log("started client");
        }

        public void OnLobbyMatchList (LobbyMatchList_t callback)
        {
            string lobbyResults = "Found " + callback.m_nLobbiesMatching + " lobbies with the following Names:\n";
            for (int i = 0; i < callback.m_nLobbiesMatching; i++)
            {
                CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
                //int players = SteamMatchmaking.GetLobbyData(lobbyID, "plrAmount");
                lobbyResults += SteamMatchmaking.GetLobbyData(lobbyID, "ownerName") + "\n";
                lobbyIds.Add(lobbyID);
                lobbyMemberCounts.Add(SteamMatchmaking.GetNumLobbyMembers(lobbyID));
            }

            Debug.Log(lobbyResults);
        }
    }
}