using UnityEngine;
using System.Collections;
using Mirror;
using Steamworks;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Network")]
    public bool isConnectedThroughSteam;
    [SyncVar]
    public string userName;
    [SyncVar]
    public Color teamColor = Color.white;

    [Header("References")]
    public Camera playerCamera;
    public Behaviour[] disableForNotLocal;

    [Header("UI")]
    public Text nameTag;
    public bool hideLocalNametag;

    void Start()
    {
        if (!isLocalPlayer)
        {
            foreach (Behaviour b in disableForNotLocal)
            {
                b.enabled = false;
            }

            //assign the localplayer's camera
            playerCamera = CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkPlayer>().playerCamera;
        } 
        else
        {
            if (isConnectedThroughSteam)
            {
                CmdSetUserName(SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID()));
            }
        }
    }

    private void Update()
    {
        if (isLocalPlayer && hideLocalNametag)
        {
            nameTag.text = "";
        }
        else
        {
            nameTag.text = userName;
            nameTag.color = teamColor;
        }
    }

    private void LateUpdate()
    {
        nameTag.transform.parent.LookAt(playerCamera.transform.position);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        CustomNetworkManager.RegisterPlayer(netId, gameObject);

        if (isLocalPlayer)
        {
            CustomNetworkManager.SetLocalPlayer(netId);
        }
    }

    public override void OnStopClient() {
        base.OnStopClient();

        CustomNetworkManager.UnregisterPlayer(netId);
    }

    [Command]
    public void CmdSetUserName(string userName)
    {
        this.userName = userName;
    }
}
