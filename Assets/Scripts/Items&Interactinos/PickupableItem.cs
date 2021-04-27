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
    public bool isGrounded;
    public float groundedCheckDistance = 0.5f;
    public LayerMask groundCheckMask;

    [Header("PlayerUsage")]
    public PlayerItemInteractions currUsingPlayer;

    public virtual void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundedCheckDistance, groundCheckMask);

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

    public virtual void FixedUpdate ()
    {
        if (!isGrounded && rb.useGravity)
        {
            rb.velocity += Vector3.up * Physics2D.gravity.y * rb.mass * Time.fixedDeltaTime;
        }
    }


    public virtual void Pickup (PlayerItemInteractions player)
    {
        currUsingPlayer = player;
        transform.parent = currUsingPlayer.itemHoldParent;
        transform.localPosition = currUsingPlayer.itemHoldPos.localPosition;
        transform.localRotation = currUsingPlayer.itemHoldPos.localRotation;

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
