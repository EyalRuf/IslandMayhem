using UnityEngine;
using System.Collections;
using Mirror;

public class PickupableItem : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    [SerializeField] Collider col;
    [SerializeField] Transform originalParent;
    public Outline outline;

    [Header("Pickupable")]
    [SyncVar]
    public bool isBeingHeld;
    public float outlineDistance;

    [Header("PlayerUsage")]
    public PlayerItemInteractions currUsingPlayer;

    public virtual void Update()
    {
        // Un-outlining item if local player is not close anymore or item is being held
        if (CustomNetworkManager.localPlayerInitialized)
        {
            // Disable outline if player is not close anymore or im being held
            GameObject localPlayer = CustomNetworkManager.GetLocalPlayer();
            if (isBeingHeld || Vector3.Distance(transform.position, localPlayer.transform.position) > outlineDistance)
            {
                Outline(false);
            }
        }
    }

    public virtual void Pickup (PlayerItemInteractions player, Transform newParent, Transform newOrientation)
    {
        currUsingPlayer = player;
        transform.parent = newParent;
        transform.localPosition = newOrientation.localPosition;
        transform.localRotation = newOrientation.localRotation;

        outline.enabled = false;
        isBeingHeld = true;
        rb.isKinematic = true;
        col.enabled = false;
    }

    public virtual void Drop ()
    {
        currUsingPlayer.heldItem = null;
        currUsingPlayer = null;
        transform.parent = originalParent;
        isBeingHeld = false;
        rb.isKinematic = false;
        col.enabled = true;
    }

    public virtual void UseMain ()
    {
    }

    public virtual void UseSecondary()
    {
    }

    public void Outline(bool flag)
    {
        outline.enabled = flag;
    }
}
