using Assets.Scripts.UI;
using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Networking
{
    public class SteamLobby : MonoBehaviour
    {
        public NetworkManager networkManager;
        public MatchManager matchManager;
        public MenuUIManager menuUIManager;

        private string HostAddressKey = "HostAddress";

        #region Steam Callbacks
        protected Callback<LobbyCreated_t> lobbyCreated;
        protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
        protected Callback<LobbyEnter_t> lobbyEntered;
        protected Callback<LobbyMatchList_t> lobbyMatchList;
        #endregion

        private CSteamID currLobby;
        public List<CSteamID> lobbyIds = new List<CSteamID>();

        [Header("UI")]
        [SerializeField] private Transform LobbyListTransform;
        [SerializeField] private ScrollRect ScrollRect;
        [SerializeField] private GameObject LobbyListItem;

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
                GetLobbies();
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

            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            var x = SteamUser.GetSteamID().ToString();
            var y = SteamFriends.GetPersonaName().ToString();
            SteamMatchmaking.SetLobbyData(lobbyId, "host", x);
            SteamMatchmaking.SetLobbyData(lobbyId, "hostName", y);
            SteamMatchmaking.SetLobbyData(lobbyId, HostAddressKey, x);
            SteamMatchmaking.SetLobbyData(lobbyId, "game", "eyalgame");

            currLobby = lobbyId;
            networkManager.StartHost();
            Debug.Log("started host");
        }

        public void OnGameLobbyJoinRequested (GameLobbyJoinRequested_t callback)
        {
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        public void OnLobbyEntered (LobbyEnter_t callback)
        {
            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        
            if (NetworkServer.active) // I'm hosting and someone joined my lobby
            {
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                if (playerCount >= matchManager.numberOfPlayersNeededToStart)
                {
                    SteamMatchmaking.SetLobbyJoinable(lobbyId, false);
                }

                return;
            }
            else // I joined the lobby
            {
                string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HostAddressKey);

                networkManager.networkAddress = hostAddress;
                networkManager.StartClient();
                currLobby = lobbyId;

                menuUIManager.JoinedGame();

                Debug.Log("started client");
            }
        }

        public void GetLobbies ()
        {
            ClearLobbyList();
            SteamMatchmaking.AddRequestLobbyListStringFilter("game", "eyalgame", ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.RequestLobbyList();
        }

        public void LeaveLobby()
        {
            SteamMatchmaking.LeaveLobby(currLobby);
            currLobby = new CSteamID();
        }

        // Lobbies fetched
        public void OnLobbyMatchList(LobbyMatchList_t callback)
        {
            List<string> hostNames = new List<string>();
            List<int> playercounts = new List<int>();
            List<CSteamID> tempLobbyIds = new List<CSteamID>();

            for (int i = 0; i < callback.m_nLobbiesMatching; i++)
            {
                CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

                string hostId = SteamMatchmaking.GetLobbyData(lobbyId, "host");
                string hostName = SteamMatchmaking.GetLobbyData(lobbyId, "hostName");
                string game = SteamMatchmaking.GetLobbyData(lobbyId, "game");
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);

                // Don't need empty or local player's own lobbies
                // only need the game when using the 480 appid
                if (game.Equals("eyalgame"))
                {
                    hostNames.Add(hostName);
                    playercounts.Add(playerCount);
                    tempLobbyIds.Add(lobbyId);
                }
            }

            for (var i = 0; i < hostNames.Count; i++)
            {
                GameObject instance = Instantiate(LobbyListItem, LobbyListTransform);
                instance.GetComponent<LobbyListItem>().updateLobbyUI(hostNames[i], playercounts[i] + "/" + matchManager.numberOfPlayersNeededToStart, tempLobbyIds[i], JoinLobby);
                lobbyIds.Add(tempLobbyIds[i]);
            }
        }

        private void ClearLobbyList()
        {
            for (int i = 0; i < LobbyListTransform.childCount; i++)
            {
                Destroy(LobbyListTransform.GetChild(i).gameObject);
            }

            lobbyIds.Clear();
        }

        private void JoinLobby(CSteamID lobbyId)
        {
            menuUIManager.ClickedLobby();
            SteamMatchmaking.JoinLobby(lobbyId);
        }
    }
}