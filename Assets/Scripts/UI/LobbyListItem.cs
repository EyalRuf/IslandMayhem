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

        private int lobbyIndex;
        private Action<int> onLobbyClicked; 

        public void updateLobbyUI (string ownerTxt, int playerCount, int lobbyIndex, Action<int> onLobbyClicked)
        {
            ownerNameText.text = ownerTxt;
            playerCountText.text = playerCount + "/8";
            this.lobbyIndex = lobbyIndex;
            this.onLobbyClicked = onLobbyClicked;
            joinLobbyBtn.onClick.AddListener(ClickedLobby);
        }

        public void ClickedLobby()
        {
            onLobbyClicked(lobbyIndex);
        }
    }
}