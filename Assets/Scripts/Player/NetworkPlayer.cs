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
    private MatchManager matchManager;

    [Header("UI")]
    public Text nameTag;
    public bool hideLocalNametag;

    [Header("Misc")]
    [SyncVar]
    public int playerTeam = -1;
    [SyncVar]
    public Color teamColor = Color.white;

    [Header("Networking")]
    [SyncVar]
    public Vector3 netPlayerPos = Vector3.zero;
    [SyncVar]
    public Quaternion netPlayerRot = Quaternion.identity;
    [SyncVar]
    public Vector3 netPlayerVel = Vector3.zero;
    public float lerpFactor;

    void Start()
    {
        matchManager = FindObjectOfType<MatchManager>();

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
        if (isLocalPlayer && hideLocalNametag)
        {
            nameTag.text = "";
        }

        if (matchManager.gameStarted && !isLocalPlayer)
        {
            nameTag.text = userName;
            nameTag.color = teamColor;

            if (matchManager is SDMatchManager)
            {
                NetworkPlayer localPlayer = CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkPlayer>();
                if (localPlayer.playerTeam != -1)
                {
                    nameTag.color = localPlayer.playerTeam == 0 ? teamColor : Color.white;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            CmdUpdatePlayerTransform(netId, transform.position, transform.rotation, rb.velocity);
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
    void CmdUpdatePlayerTransform(uint playerNID, Vector3 pos, Quaternion rot, Vector3 vel)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        NetworkPlayer np = player.GetComponent<NetworkPlayer>();
        np.netPlayerPos = pos;
        np.netPlayerRot = rot;
        np.netPlayerVel = vel;
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
