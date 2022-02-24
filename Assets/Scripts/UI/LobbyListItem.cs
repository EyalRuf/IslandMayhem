using Steamworks;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class LobbyListItem : MonoBehaviour
    {
        [SerializeField] private Text ownerNameText;
        [SerializeField] private Text playerCountText;
        [SerializeField] private Button joinLobbyBtn;

        private CSteamID lobbyId;
        private Action<CSteamID> onLobbyClicked; 

        public void updateLobbyUI (string ownerTxt, string playerCountTxt, CSteamID lobbyId, Action<CSteamID> onLobbyClicked)
        {
            ownerNameText.text = ownerTxt;
            playerCountText.text = playerCountTxt;
            this.lobbyId = lobbyId;
            this.onLobbyClicked = onLobbyClicked;
            joinLobbyBtn.onClick.AddListener(ClickedLobby);
        }

        public void ClickedLobby()
        {
            onLobbyClicked(lobbyId);
        }
    }
}