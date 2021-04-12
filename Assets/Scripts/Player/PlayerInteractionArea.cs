using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionArea : NetworkBehaviour
{
    public int playerTeam = -1;

    [Header("InteractionArea")]
    public LayerMask interactionAreaLayerMask;

    [Header("References")]
    public LocalPlayerInput lpInput;
    public Behaviour[] disableWhileInteracting;

    private MatchManager matchManager;

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer)
            return;

        if(matchManager == null)
        {
            matchManager = FindObjectOfType<MatchManager>();
        }

        if (lpInput.interactInputDown)
        {
            RaycastHit hitInfo;
            // Perhaps change to overlap sphere + checking what is infront of the player
            if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, 3f, interactionAreaLayerMask))
            {
                InteractionArea area = hitInfo.collider.GetComponent<InteractionArea>();
                if (area.canBeInteractedWith && !area.beingInteractedWith)
                {
                    playerTeam = matchManager.GetTeamIndexByPlayer(gameObject);

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
        // stop moving + animation + call area function
        foreach (Behaviour b in disableWhileInteracting)
        {
            b.enabled = false;
        }

        StartCoroutine(area.Interact(EndInteractionWithArea));
    }

    void EndInteractionWithArea ()
    {
        foreach (Behaviour b in disableWhileInteracting)
        {
            b.enabled = true;
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
