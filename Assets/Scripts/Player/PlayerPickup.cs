using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickup : NetworkBehaviour
{
    [Header("References")]
    public Transform itemHoldPos;
    public Transform itemHoldParent;
    public LocalPlayerInput lpInput;

    [Header("Pickup")]
    public PickupableItem heldItem = null;
    public LayerMask pickableLayerMask;
    public float pickupCheckRadius;

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (heldItem != null) // Holding something
        {
            if (lpInput.pickupInputDown) // Put it down
            {
                CmdDropItem(netId);
            }
        }
        else // Not Holding
        {
            // Checking for pickupables nearby
            List<Collider> pickupablesCols = new List<Collider>(Physics.OverlapSphere(transform.position, pickupCheckRadius, pickableLayerMask));
            if (pickupablesCols.Count > 0)
            {
                // Finding ones that are not held
                List<PickupableItem> pickupables = new List<PickupableItem>();
                pickupablesCols.ForEach(col => pickupables.Add(col.GetComponent<PickupableItem>()));
                pickupables = pickupables.FindAll(pu => !pu.isBeingHeld);

                if (pickupables.Count > 0)
                {
                    // Finding closest one
                    pickupables.Sort((a, b) =>
                    {
                        float distanceToA = Vector3.Distance(transform.position, a.transform.position);
                        float distanceToB = Vector3.Distance(transform.position, b.transform.position);
                        return distanceToA.CompareTo(distanceToB);
                    });

                    PickupableItem closestPickupable = pickupables[0];

                    // Turning off outline for the not closest pickupable
                    for (var i = 1; i < pickupables.Count; i++)
                    {
                        pickupables[i].Outline(false);
                    }

                    closestPickupable.Outline(true);
                    if (lpInput.pickupInputDown) // Pick up
                    {
                        CmdPickupItem(netId, closestPickupable.netId);
                    }
                }
            }
        }
        
    }

    public void Pickup(PickupableItem pickupable)
    {
        heldItem = pickupable;
        heldItem.Pickup(itemHoldParent, itemHoldPos);
    }

    [ClientRpc]
    public void RpcPickup(uint objectNID)
    {
        NetworkIdentity item;
        PickupableItem pickupable = null;
        if (NetworkIdentity.spawned.TryGetValue(objectNID, out item))
        {
            pickupable = item.GetComponent<PickupableItem>();
        }

        if (pickupable != null)
        {
            this.Pickup(pickupable);
        } else
        {
            Debug.LogError("Item not found in scene");
        }
    }

    [Command]
    void CmdPickupItem(uint playerNID, uint objectNID)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);

        PlayerPickup pp = player.GetComponent<PlayerPickup>();

        pp.RpcPickup(objectNID);
    }

    public void DropItem()
    {
        heldItem.Drop();
        heldItem = null;
    }

    public void DestroyItem()
    {
        CmdDestroyItem(heldItem);
        heldItem = null;
    }

    [Command]
    void CmdDestroyItem(PickupableItem item)
    {
        if(item != null)
        {
            NetworkServer.Destroy(item.gameObject);
        }
    }

    [ClientRpc]
    public void RpcDrop()
    {
        this.DropItem();
    }

    [Command]
    void CmdDropItem(uint playerNID)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);

        PlayerPickup pp = player.GetComponent<PlayerPickup>();

        pp.RpcDrop();
    }
}
