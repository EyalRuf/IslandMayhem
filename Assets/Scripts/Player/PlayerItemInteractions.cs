using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemInteractions : NetworkBehaviour
{
    [Header("References")]
    public Transform itemHoldPos;
    public Transform itemHoldParent;
    public LocalPlayerInput lpInput;
    public ThirdPersonCharacterController playerController;
    public PlayerAnimations playerAnims;

    [Header("Pickup")]
    public PickupableItem heldItem = null;
    public LayerMask pickableLayerMask;
    public float pickupCheckRadius;

    [Header("Aiming")]
    public ThirdPersonCameraController cameraController;

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
                return;
            } 

            if (heldItem is ThrowableItem)
            {
                if (lpInput.itemMainUseDown)
                {
                    StopAiming();
                    CmdUseItem(netId, true);
                }
                if (lpInput.itemSecondaryUsageDown)
                {
                    StartAiming();
                } else if (lpInput.itemSecondaryUsageUp)
                {
                    StopAiming();
                }
            } else
            {
                if (lpInput.itemMainUseDown)
                {
                    CmdUseItem(netId, true);
                }
                if (lpInput.itemSecondaryUsage)
                {
                    CmdUseItem(netId, false);
                }
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

    void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;
    }

    public void Pickup(PickupableItem pickupable)
    {
        heldItem = pickupable;
        heldItem.Pickup(this);
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
            Pickup(pickupable);
        } else
        {
            Debug.LogError("Item not found in scene");
        }
    }

    [Command]
    void CmdPickupItem(uint playerNID, uint objectNID)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);

        PlayerItemInteractions pp = player.GetComponent<PlayerItemInteractions>();

        pp.RpcPickup(objectNID);
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
    public void DropItem()
    {
        heldItem.Drop();
        heldItem = null;
    }

    [ClientRpc]
    public void RpcDrop()
    {
        DropItem();
    }

    [Command]
    void CmdDropItem(uint playerNID)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerItemInteractions pp = player.GetComponent<PlayerItemInteractions>();

        pp.RpcDrop();
    }

    void StartAiming()
    {
        playerController.isAiming = true;
        cameraController.ToggleCameraAim(true);
    }

    void StopAiming()
    {
        playerController.isAiming = false;
        cameraController.ToggleCameraAim(false);
    }

    void UseItem(bool isItemMainUse)
    {
        //playerAnims.ThrowAnim();
        if (isItemMainUse)
        {
            heldItem.UseMain();
        } else
        {
            heldItem.UseSecondary();
        }
    }

    [ClientRpc]
    public void RpcUseItem(bool isItemMainUse)
    {
        UseItem(isItemMainUse);
    }

    [Command]
    void CmdUseItem(uint playerNID, bool isItemMainUse)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerItemInteractions pp = player.GetComponent<PlayerItemInteractions>();

        pp.RpcUseItem(isItemMainUse);
    }
}
