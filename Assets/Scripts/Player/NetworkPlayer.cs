using UnityEngine;
using System.Collections;
using Mirror;
using Steamworks;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Network")]
    public bool isConnectedThroughSteam;

    [Header("References")]
    public Camera playerCamera;
    public Behaviour[] disableForNotLocal;

    [Header("UI")]
    public Text nameTag;

    void Start()
    {
        if (!isLocalPlayer)
        {
            foreach (Behaviour b in disableForNotLocal)
            {
                b.enabled = false;
            }
        } else
        {
            if (isConnectedThroughSteam)
            {
                nameTag.text = SteamFriends.GetFriendPersonaName(SteamUser.GetSteamID());
            }
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
}
