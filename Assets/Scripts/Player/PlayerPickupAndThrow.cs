using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickupAndThrow : NetworkBehaviour
{
    [Header("References")]
    public Transform itemHoldPos;
    public Transform itemHoldParent;
    public LocalPlayerInput lpInput;
    public ThirdPersonCharacterController playerController;

    [Header("Pickup")]
    public PickupableItem heldItem = null;
    public LayerMask pickableLayerMask;
    public float pickupCheckRadius;

    [Header("Throwing")]
    public float throwForce;
    public float movementDirectionForceMultiplyer;
    public Transform throwTarget;

    [Header("Aiming")]
    public ThirdPersonCameraController cameraController;

    [Header("Trajectory")]
    public LineRenderer lineRenderer;
    public int lineMaxLength;
    public float trajectoryPointDist;
    private List<Vector3> linePoints = new List<Vector3>();
    public LayerMask trajectoryLayerMask;
    public Vector3 aimAdjustVec;

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
            else if (lpInput.throwInputDown)
            {
                StopAiming();
                CmdThrowItem(netId);
            }

            if (lpInput.aimInputDown)
            {
                StartAiming();
            } else if (lpInput.aimInputUp)
            {
                StopAiming();
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

        if (heldItem != null && lpInput.aimInput)
        {
            DrawThrowTrajectory(heldItem);
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

        PlayerPickupAndThrow pp = player.GetComponent<PlayerPickupAndThrow>();

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
        PlayerPickupAndThrow pp = player.GetComponent<PlayerPickupAndThrow>();

        pp.RpcDrop();
    }

    #region Throwing

    void StartAiming()
    {
        cameraController.ToggleCameraAim(true);
        lineRenderer.enabled = true;
    }

    void StopAiming()
    {
        cameraController.ToggleCameraAim(false);
        lineRenderer.enabled = false;
    }

    void DrawThrowTrajectory(PickupableItem item)
    {
        Vector3 throwVec = CalcThrowVector();
        float objMass = item.rb.mass;
        Vector3 startPoint = item.transform.position;

        Vector3 throwVelocity = throwVec / objMass * Time.fixedDeltaTime;

        linePoints.Clear();
        linePoints.Add(startPoint);

        Vector3 currentPosition = startPoint;
        Vector3 currentVelocity = throwVelocity;

        RaycastHit hit;
        Ray ray = new Ray(currentPosition, currentVelocity.normalized);
        while (!Physics.Raycast(ray, out hit, trajectoryPointDist) && Vector3.Distance(startPoint, currentPosition) < lineMaxLength)
        {
            // Time to travel distance of trajectoryVertDist
            var t = trajectoryPointDist / currentVelocity.magnitude;
            // Update position and velocity
            currentVelocity = currentVelocity + t * Physics.gravity;

            currentPosition = currentPosition + t * currentVelocity;
            currentPosition = new Vector3(currentPosition.x, 
                currentPosition.y - (0.5f * Physics.gravity.y * t * t), 
                currentPosition.z);

            linePoints.Add(currentPosition);
            ray = new Ray(currentPosition, currentVelocity.normalized);
        }

        // If something was hit, add last point there
        if (hit.transform)
        {
            linePoints.Add(hit.point);
        }

        lineRenderer.positionCount = linePoints.Count;
        lineRenderer.SetPositions(linePoints.ToArray());
    }

    Vector3 CalcThrowVector()
    {
        float movementMultiplyer = movementDirectionForceMultiplyer;
        Vector3 dir = throwTarget.forward;
        Vector3 movement = playerController.playerVelocity * 0.75f;
        if (Mathf.Sign(dir.x) != Mathf.Sign(movement.x) && Mathf.Sign(dir.z) != Mathf.Sign(movement.z))
            movementMultiplyer *= 0.5f;

        Vector3 adjustedThrowVec = new Vector3(throwTarget.forward.x * aimAdjustVec.x, 
            throwTarget.forward.y * aimAdjustVec.y, 
            throwTarget.forward.z * aimAdjustVec.z);

        return  (adjustedThrowVec * throwForce) + (playerController.playerVelocity * movementMultiplyer);
    }

    void ThrowItem()
    {
        heldItem.Drop();

        heldItem.rb.AddForce(CalcThrowVector());
        heldItem = null;
    }

    [ClientRpc]
    public void RpcThrow()
    {
        ThrowItem();
    }

    [Command]
    void CmdThrowItem(uint playerNID)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerPickupAndThrow pp = player.GetComponent<PlayerPickupAndThrow>();

        pp.RpcThrow();
    }

    #endregion
}
