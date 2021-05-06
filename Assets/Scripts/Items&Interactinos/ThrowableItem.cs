using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class ThrowableItem : PickupableItem
{
    [Header("Throwing")]
    public float throwForce;
    public float movementDirectionForceMultiplyer;
    public Transform throwTarget;

    [Header("Trajectory")]
    public bool drawTrajectory;
    public LineRenderer lineRenderer;
    public int lineMaxLength;
    public float trajectoryPointDist;
    private List<Vector3> linePoints = new List<Vector3>();
    public LayerMask trajectoryLayerMask;
    public Vector3 aimMultiplyerVec;
    public Vector3 aimAdditionVec;
    public GameObject trajectoryEndSphere;

    [Header("HitInflictor")]
    public HitInflictor hitInflictor;
    public float hitActiveDuration;

    [Header("Misc")]
    private Vector3 currObjectVelocity;
    private Vector3 lastPosition;

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        drawTrajectory = currUsingPlayer != null ? currUsingPlayer.lpInput.itemSecondaryUsage : false;
        lineRenderer.enabled = drawTrajectory;
        trajectoryEndSphere.SetActive(drawTrajectory);

        if (isGrounded) // after hit ground can't hit players anymore
        {
            hitInflictor.isActive = false;
        }

        if (isBeingHeld)
        {
            currObjectVelocity = transform.position - lastPosition;
            lastPosition = transform.position;

            if (drawTrajectory)
            {
                DrawThrowTrajectory();
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void Pickup(PlayerItemInteractions player)
    {
        base.Pickup(player);
        throwTarget = player.cameraController.targetTransform;
        hitInflictor.Deactivate();
    }

    public override void UseMain()
    {
        base.UseMain();
        PlayerItemInteractions pi = currUsingPlayer;

        pi.playerAnims.ThrowAnim();
        hitInflictor.ActivateInflictorForDuration(pi.netId, hitActiveDuration);
        Drop();

        if (pi.netId == CustomNetworkManager.GetLocalPlayer().netId)
        {
            rb.AddForce(CalcThrowVector());
            CmdUpdateTransform(transform.position, transform.rotation, rb.velocity);
        }
    }

    public override void Drop()
    {
        currUsingPlayer.StopAiming();
        base.Drop();
    }

    public override void UseSecondary()
    {
        base.UseSecondary();
    }

    void DrawThrowTrajectory()
    {
        Vector3 throwVec = CalcThrowVector();
        float objMass = rb.mass;
        Vector3 startPoint = transform.position;

        Vector3 throwVelocity = (throwVec / (objMass*2)) * Time.fixedDeltaTime;

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
        trajectoryEndSphere.transform.position = linePoints[linePoints.Count - 1];
        lineRenderer.SetPositions(linePoints.ToArray());
    }

    Vector3 CalcThrowVector()
    {
        float movementMultiplyer = movementDirectionForceMultiplyer;
        Vector3 dir = throwTarget.forward;
        Vector3 movement = currObjectVelocity;

        // Halfing the effect of the movement on the throwing if you're walking backwards
        //if (Mathf.Sign(dir.x) != Mathf.Sign(movement.x) && Mathf.Sign(dir.z) != Mathf.Sign(movement.z))
        //    movementMultiplyer *= 0.5f;

        Vector3 adjustedThrowVec = new Vector3(throwTarget.forward.x * aimMultiplyerVec.x,
            throwTarget.forward.y * aimMultiplyerVec.y,
            throwTarget.forward.z * aimMultiplyerVec.z);

        return (adjustedThrowVec * throwForce) + (currObjectVelocity * movementMultiplyer) + aimAdditionVec;
    }
}
