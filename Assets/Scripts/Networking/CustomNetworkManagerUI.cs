using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Mirror;

public class CustomNetworkManagerUI : MonoBehaviour
{
    public GameObject startUI;
    public GameObject friendsUI;
    public GameObject processingUI;

    public Text statusText;
    public Text[] friendsButtons;

    private CustomNetworkManager networkManager;

    private MenuState menuState;
    private string status;
    private Dictionary<int, string> friendsInGame = new Dictionary<int, string>();

    private void Update()
    {
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<CustomNetworkManager>();
        }

        if (NetworkServer.active || NetworkClient.isConnected)
        {
            //disable everything
            startUI.SetActive(false);
            friendsUI.SetActive(false);
            return;
        }

        //set status
        statusText.text = status;

        //set active the right visuals
        startUI.SetActive(menuState == MenuState.Start);
        friendsUI.SetActive(menuState == MenuState.Friends);
        processingUI.SetActive(menuState == MenuState.Processing);
    }

    public void GoBackToStart()
    {
        menuState = MenuState.Start;
        networkManager.StopClient();
    }

    public void Host()
    {
        status = "Hosting match...";

        menuState = MenuState.Processing;
        if (networkManager.isSteam)
            networkManager.networkAddress = SteamUser.GetSteamID().ToString();
        networkManager.StartHost();
    }

    public void JoinFriends()
    {
        menuState = MenuState.Friends;
        GetFriendsInGame();
    }

    public void JoinFriendAtIndex(int index)
    {
        if (index < friendsInGame.Count)
        {
            networkManager.networkAddress = SteamFriends.GetFriendRichPresence(SteamFriends.GetFriendByIndex(friendsInGame.ElementAt(index).Key, EFriendFlags.k_EFriendFlagAll), "room");
            networkManager.StartClient();
            menuState = MenuState.Processing;
            status = "Joining " + friendsInGame.ElementAt(index).Value + "...";
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

        for(int f = 0; f < friendsButtons.Length; f++)
        {
            friendsButtons[f].gameObject.SetActive(f < friendsInGame.Count);
            if(f < friendsInGame.Count)
            {
                friendsButtons[f].text = friendsInGame.ElementAt(f).Value;
            }
        }
    }
}

public enum MenuState
{
    Start,
    Friends,
    Processing
}