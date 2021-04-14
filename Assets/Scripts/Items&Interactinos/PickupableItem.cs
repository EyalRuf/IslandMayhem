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

    void Update()
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

    public void Pickup (Transform newParent, Transform newOrientation)
    {
        transform.parent = newParent;
        transform.localPosition = newOrientation.localPosition;
        transform.localRotation = newOrientation.localRotation;

        outline.enabled = false;
        isBeingHeld = true;
        rb.isKinematic = true;
        col.enabled = false;
    }

    public void Drop ()
    {
        transform.parent = originalParent;
        isBeingHeld = false;
        rb.isKinematic = false;
        col.enabled = true;
    }

    public void Outline(bool flag)
    {
        outline.enabled = flag;
    }
}
