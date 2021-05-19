using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampStepPressurePlate : CampStep
{
    [Header("Pressure plate")]
    public float overlapRadius;
    public LayerMask overlapMask;

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, overlapRadius, overlapMask);
        
        if(!isCompleted && colliders.Length > 0)
        {
            CompleteStep();
        }
        else if (isCompleted && colliders.Length <= 0)
        {
            ResetStep();
        }
    }
}
