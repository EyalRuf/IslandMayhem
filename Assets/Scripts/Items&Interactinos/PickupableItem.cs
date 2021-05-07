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
    public float dropCDDuration = 0.75f;
    public float outlineDistance;
    public bool isGrounded;
    public float groundedCheckDistance = 0.5f;
    public LayerMask groundCheckMask;

    [Header("PlayerUsage")]
    public PlayerItemInteractions currUsingPlayer;

    [Header("Networking")]
    [SyncVar]
    public Vector3 netPos;
    [SyncVar]
    public Quaternion netRot;
    [SyncVar]
    public Vector3 netVel;
    public float lerpFactor;

    public void Start()
    {
        netPos = transform.position;
        netRot = transform.rotation;
        netVel = rb.velocity;
    }

    public virtual void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundedCheckDistance, groundCheckMask);

        // Un-outlining item if local player is not close anymore or item is being held
        if (CustomNetworkManager.localPlayerInitialized)
        {
            // Disable outline if player is not close anymore or im being held
            if (isBeingHeld || Vector3.Distance(transform.position, CustomNetworkManager.GetLocalPlayer().transform.position) > outlineDistance)
            {
                Outline(false);
            }
        }
    }

    public virtual void FixedUpdate ()
    {
        if (!isGrounded && rb.useGravity)
        {
            Vector3 vec = Vector3.up * Physics2D.gravity.y * rb.mass * Time.fixedDeltaTime;
            rb.velocity += vec;
        }

        if (isServer)
        {
            netPos = transform.position;
            netRot = transform.rotation;
            netVel = rb.velocity;
        } else if (!isBeingHeld)
        {
            UpdateTransform(netPos, netRot, netVel);
        }
    }

    public void UpdateTransform(Vector3 pos, Quaternion rot, Vector3 vel)
    {
        // teleport if too far away
        if (Vector3.Distance(transform.position, pos) > 10f)
            transform.position = pos;
        if (Quaternion.Angle(transform.rotation, rot) > 10f)
            transform.rotation = rot;
        if (Vector3.Distance(rb.velocity, vel) > 5f)
            rb.velocity = vel;

        // smooth closer if not very very close already
        if (Vector3.Distance(transform.position, pos) > 0.01f)
            transform.position = Vector3.Lerp(transform.position, pos, lerpFactor);
        if (Quaternion.Angle(transform.rotation, rot) > 0.1f)
            transform.rotation = Quaternion.Lerp(transform.rotation, rot, lerpFactor);
        if (Vector3.Distance(rb.velocity, vel) > 0.01f)
            rb.velocity = Vector3.Lerp(rb.velocity, vel, lerpFactor);
    }

    //[Command]
    //public void CmdUpdateServerTransform(Vector3 pos, Quaternion rot, Vector3 vel)
    //{
    //    transform.position = pos;
    //    transform.rotation = rot;
    //    rb.velocity = vel;
    //    netPos = pos;
    //    netRot = rot;
    //    netVel = vel;
    //}

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

        StartCoroutine(HoldCD());
    }

    public virtual void UseMain (Vector3 pos, Quaternion rot, Vector3 vel, Vector3 throwVec)
    {
        transform.position = pos;
        transform.rotation = rot;
        rb.velocity = vel;
    }

    public virtual void UseSecondary(Vector3 pos, Quaternion rot, Vector3 vel)
    {
        transform.position = pos;
        transform.rotation = rot;
        rb.velocity = vel;
    }

    public void Outline(bool flag)
    {
        outline.enabled = flag;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isServer && !isBeingHeld)
        {
            HitInflictor hit = other.GetComponent<HitInflictor>();

            if (hit != null && hit.isActive && hit.initiatorNetId != netId)
            {
                Vector3 knockbackDir = transform.position - hit.transform.position;
                rb.AddForce(knockbackDir * hit.knockbackPower / 3, ForceMode.Impulse);
                hit.HitInflicted();
            }
        }
    }

    protected IEnumerator HoldCD ()
    {
        isBeingHeld = true;
        yield return new WaitForSeconds(dropCDDuration);
        isBeingHeld = false;
    }
}
