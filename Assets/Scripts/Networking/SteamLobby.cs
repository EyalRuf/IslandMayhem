using Assets.Scripts.UI;
using Mirror;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Networking
{
    public class SteamLobby : MonoBehaviour
    {
        public NetworkManager networkManager;

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

            CSteamID lobby = new CSteamID(callback.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(lobby, "host", SteamUser.GetSteamID().ToString());
            SteamMatchmaking.SetLobbyData(lobby, "hostName", SteamFriends.GetPersonaName().ToString());
            SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());

            currLobby = lobby;
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

            if (NetworkServer.active)
            {
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                return;
            }

            string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HostAddressKey);

            networkManager.networkAddress = hostAddress;
            networkManager.StartClient();
            currLobby = lobbyId;

            Debug.Log("started client");
        }

        public void GetLobbies ()
        {
            ClearLobbyList();
            SteamMatchmaking.RequestLobbyList();
        }

        public void LeaveLobby()
        {
            SteamMatchmaking.LeaveLobby(currLobby);
        }

        public void OnLobbyMatchList (LobbyMatchList_t callback)
        {

            for (int i = 0; i < callback.m_nLobbiesMatching; i++)
            {
                CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
                lobbyIds.Add(lobbyID);

                string hostName = SteamMatchmaking.GetLobbyData(lobbyID, "hostName");
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);

                GameObject instance = Instantiate(LobbyListItem, LobbyListTransform);
                instance.GetComponent<LobbyListItem>().updateLobbyUI(hostName, playerCount, i, JoinLobby);
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

        private void JoinLobby(int lobbyIndex)
        {
            SteamMatchmaking.JoinLobby(lobbyIds[lobbyIndex]);
        }
    }
}