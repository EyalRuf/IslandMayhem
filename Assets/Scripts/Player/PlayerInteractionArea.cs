using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionArea : NetworkBehaviour
{
    [Header("References")]
    public LocalPlayerInput lpInput;
    public PlayerAnimations playerAnims;
    public Behaviour[] disableWhileInteracting;

    [Header("InteractionArea")]
    public LayerMask interactionAreaLayerMask;
    public Transform areaCheckTransform;
    public float areaCheckSphereRadius;

    [Header("Misc")]
    public int playerTeam = -1;

    private MatchManager matchManager;

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer)
            return;

        if(matchManager == null)
        {
            matchManager = FindObjectOfType<MatchManager>();
            playerTeam = matchManager.GetTeamIndexByPlayer(gameObject);
        }

        if (lpInput.interactInputDown)
        {
            Collider[] cols = Physics.OverlapSphere(areaCheckTransform.position, areaCheckSphereRadius, interactionAreaLayerMask);
            if (cols.Length > 0)
            {
                InteractionArea area = cols[0].GetComponent<InteractionArea>();
                if (area.canBeInteractedWith && !area.beingInteractedWith)
                {
                    if (area.restrictedToTeam >= 0 ? area.restrictedToTeam == playerTeam : true)
                    {
                        CmdInteractWithArea(netId, area.netId);
                    }
                }
            }
        }
    }

    void InteractWithArea(InteractionArea area)
    {
        playerAnims.StartInteractingAnim();
        EnableOrDisableBehaviors(false);
        StartCoroutine(area.Interact(EndInteractionWithArea));
    }

    bool EndInteractionWithArea ()
    {
        EnableOrDisableBehaviors(true);
        playerAnims.StopInteractingAnim();
        return true;
    }

    void EnableOrDisableBehaviors (bool isEnabled)
    {
        foreach (Behaviour b in disableWhileInteracting)
        {
            b.enabled = isEnabled;
        }
    }

    [ClientRpc]
    void RpcInteractWithArea(uint areaNID)
    {
        NetworkIdentity areaNI;
        InteractionArea area = null;
        if (NetworkIdentity.spawned.TryGetValue(areaNID, out areaNI))
        {
            area = areaNI.GetComponent<InteractionArea>();
        }

        if (area != null)
        {
            this.InteractWithArea(area);
        }
        else
        {
            Debug.LogError("Item not found in scene");
        }
    }

    [Command]
    void CmdInteractWithArea(uint playerNID, uint areaNID)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerInteractionArea pia = player.GetComponent<PlayerInteractionArea>();

        pia.RpcInteractWithArea(areaNID);
    }
}
