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
                Vector3 throwVec = (heldItem as ThrowableItem).CalcThrowVector();
                if (lpInput.itemMainUseDown)
                {
                    CmdUseItem(netId, heldItem.transform.position, heldItem.transform.rotation, heldItem.rb.velocity, throwVec, true);
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
                //if (lpInput.itemMainUseDown)
                //{
                //    CmdUseItem(netId, heldItem.transform.position, heldItem.transform.rotation, heldItem.rb.velocity, true);
                //}
                //if (lpInput.itemSecondaryUsage)
                //{
                //    CmdUseItem(netId, heldItem.transform.position, heldItem.transform.rotation, heldItem.rb.velocity, false);
                //}
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
                pickupablesCols.ForEach(col => {
                    var pi = col.GetComponent<PickupableItem>();
                    if (pi != null)
                        pickupables.Add(col.GetComponent<PickupableItem>());
                });
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
                    if (lpInput.pickupInputDown && !playerController.isCrippled) // Pick up & not crippled
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
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
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

    public void DropItemIfHeld()
    {
        if (heldItem != null)
        {
            CmdDropItem(netId);
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
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerItemInteractions pp = player.GetComponent<PlayerItemInteractions>();

        pp.RpcDrop();
    }

    public void StartAiming()
    {
        playerController.isAiming = true;
        cameraController.ToggleCameraAim(true);
    }

    public void StopAiming()
    {
        playerController.isAiming = false;
        cameraController.ToggleCameraAim(false);
    }

    void UseItem(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 throwVec, bool isItemMainUse)
    {
        if (isItemMainUse)
        {
            heldItem.UseMain(pos, rot, vel, throwVec);
        } else
        {
            heldItem.UseSecondary(pos, rot, vel);
        }
    }

    [ClientRpc]
    public void RpcUseItem(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 throwVec, bool isItemMainUse)
    {
        UseItem(pos, rot, vel, throwVec, isItemMainUse);
    }

    [Command]
    void CmdUseItem(uint playerNID, Vector3 pos, Quaternion rot, Vector3 vel, Vector3 throwVec, bool isItemMainUse)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerItemInteractions pp = player.GetComponent<PlayerItemInteractions>();

        pp.RpcUseItem(pos, rot, vel, throwVec, isItemMainUse);
    }
}
