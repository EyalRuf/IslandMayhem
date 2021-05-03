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

    [Header("References")]
    public Camera localPlayerCamera;
    public Behaviour[] disableForNotLocal;
    public GameObject[] gameObjectsToDisableForNotLocal;
    [HideInInspector]
    public TeamAndObjectivesUI localPlayer_TNO_UI;

    [Header("UI")]
    public Text nameTag;
    public bool hideLocalNametag;

    [Header("Misc")]
    [SyncVar]
    public int playerTeam = -1;
    [SyncVar]
    public Color teamColor = Color.white;

    void Start()
    {
        if (!isLocalPlayer)
        {
            foreach (Behaviour b in disableForNotLocal)
            {
                b.enabled = false;
            }
            
            foreach (GameObject go in gameObjectsToDisableForNotLocal)
            {
                go.SetActive(false);
            }

            localPlayerCamera = CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkPlayer>().localPlayerCamera;
        } 
        else
        {
            if (isConnectedThroughSteam)
            {
                CmdSetUserName(SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID()));
            }

            FindObjectOfType<MatchManager>().overviewCam.gameObject.SetActive(false);
            localPlayer_TNO_UI = FindObjectOfType<TeamAndObjectivesUI>();
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
        nameTag.transform.parent.LookAt(localPlayerCamera.transform.position);
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
