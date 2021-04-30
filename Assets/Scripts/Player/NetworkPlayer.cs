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

    [Header("Misc")]
    [SyncVar]
    public int playerTeam = -1;

    void Start()
    {
        if (!isLocalPlayer)
        {
            foreach (Behaviour b in disableForNotLocal)
            {
                b.enabled = false;
            }

            playerCamera = CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkPlayer>().playerCamera;
        } 
        else
        {
            if (isConnectedThroughSteam)
            {
                CmdSetUserName(SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID()));
            }

            FindObjectOfType<MatchManager>()?.overviewCam.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        nameTag.text = userName;
        nameTag.color = teamColor;

        if (isLocalPlayer && hideLocalNametag)
        {
            nameTag.text = "";
        }
    }

    private void LateUpdate()
    {
        nameTag.transform.parent.LookAt(playerCamera.transform.position);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        CustomNetworkManager.RegisterPlayer(netId, netIdentity);

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
