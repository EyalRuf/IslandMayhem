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
    public Rigidbody rb;
    public Camera localPlayerCamera;
    public Behaviour[] disableForNotLocal;
    public GameObject[] gameObjectsToDisableForNotLocal;
    [HideInInspector]
    public TeamAndObjectivesUI localPlayer_TNO_UI;
    MatchManager matchManager;

    [Header("UI")]
    public GameObject localPlayerUI;
    public GameObject remotePlayerUI;
    public Text nameTag;
    public bool hideLocalNametag;

    [Header("Misc")]
    [SyncVar]
    public int playerTeam = -1;
    [SyncVar]
    public Color teamColor = Color.white;

    [Header("Networking")]
    [SyncVar]
    public Vector3 netPlayerPos;
    [SyncVar]
    public Quaternion netPlayerRot;
    [SyncVar]
    public Vector3 netPlayerVel;
    public float lerpFactor;

    void Start()
    {
        matchManager = FindObjectOfType<MatchManager>();
        netPlayerPos = transform.position;
        netPlayerRot = transform.rotation;
        netPlayerVel = rb.velocity;
        localPlayerUI.SetActive(isLocalPlayer);
        remotePlayerUI.SetActive(!isLocalPlayer);

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
        nameTag.text = (isLocalPlayer && hideLocalNametag) ? "" : userName;
        nameTag.color = teamColor;
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            CmdUpdatePlayerTransform(netId, transform.position, transform.rotation, rb.velocity, transform.localScale);
        }
        else 
        {
            if (Vector3.Distance(transform.position, netPlayerPos) > 0.01f)
                transform.position = Vector3.Lerp(transform.position, netPlayerPos, lerpFactor);
            if (Quaternion.Angle(transform.rotation, netPlayerRot) > 0.1f)
                transform.rotation = Quaternion.Lerp(transform.rotation, netPlayerRot, lerpFactor);
            if (Vector3.Distance(rb.velocity, netPlayerVel) > 0.01f)
                rb.velocity = Vector3.Lerp(rb.velocity, netPlayerVel, lerpFactor);
        }
    }

    [Command]
    void CmdUpdatePlayerTransform (uint playerNID, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 size)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        NetworkPlayer np = player.GetComponent<NetworkPlayer>();
        np.netPlayerPos = pos;
        np.netPlayerRot = rot;
        np.netPlayerVel = vel;
        np.transform.localScale = size;
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
