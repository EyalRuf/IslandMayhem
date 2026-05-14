using Assets.Scripts.UI;
using CardboardCore.DI;
using Mirror;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Networking
{
    [Injectable]
    public class SteamLobby : MonoBehaviour
    {
        public event Action LobbyCreatedEvent;
        public event Action LobbyCreateFailedEvent;
        public event Action LobbyEnteredEvent;
        public event Action LobbyListReadyEvent;
        public event Action LobbyLeftEvent;
        public event Action<CSteamID> LobbySelectedEvent;

        public NetworkManager networkManager;

        public const string HostAddressKey = "HostAddress";

        private MatchService matchService;

        #region Steam Callbacks
        protected Callback<LobbyCreated_t> lobbyCreated;
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
            matchService = GetComponent<MatchService>();

            if (!SteamManager.Initialized)
                return;

            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        }

        public void HostLobby()
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);
        }

        public void OnLobbyCreated(LobbyCreated_t callback)
        {
            if (callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError("LobbyFailed");
                LobbyCreateFailedEvent?.Invoke();
                return;
            }

            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(lobbyId, "host", SteamUser.GetSteamID().ToString());
            SteamMatchmaking.SetLobbyData(lobbyId, "hostName", SteamFriends.GetPersonaName().ToString());
            SteamMatchmaking.SetLobbyData(lobbyId, HostAddressKey, SteamUser.GetSteamID().ToString());
            SteamMatchmaking.SetLobbyData(lobbyId, "game", "eyalgame");

            currLobby = lobbyId;
            LobbyCreatedEvent?.Invoke();
        }

        public void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            //SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        public void OnLobbyEntered(LobbyEnter_t callback)
        {
            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

            if (NetworkServer.active) // I'm hosting and someone joined my lobby
            {
                int playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                if (playerCount >= matchService.numberOfPlayersNeededToStart)
                {
                    SteamMatchmaking.SetLobbyJoinable(lobbyId, false);
                }

                return;
            }
            else // I joined the lobby
            {
                currLobby = lobbyId;
                LobbyEnteredEvent?.Invoke();
            }
        }

        public void GetLobbies()
        {
            ClearLobbyList();
            SteamMatchmaking.AddRequestLobbyListStringFilter("game", "eyalgame", ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.RequestLobbyList();
        }

        public void LeaveLobby()
        {
            SteamMatchmaking.LeaveLobby(currLobby);
            currLobby = new CSteamID();
            LobbyLeftEvent?.Invoke();
        }

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
                instance.GetComponent<LobbyListItem>().updateLobbyUI(hostNames[i], playercounts[i] + "/" + matchService.numberOfPlayersNeededToStart, tempLobbyIds[i], JoinLobby);
                lobbyIds.Add(tempLobbyIds[i]);
            }

            LobbyListReadyEvent?.Invoke();
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
            LobbySelectedEvent?.Invoke(lobbyId);
        }
    }
}
