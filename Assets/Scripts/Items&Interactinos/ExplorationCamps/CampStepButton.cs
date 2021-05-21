using UnityEngine;
using System.Collections;

public class CampStepButton : CampStep
{
    [Header("Button")]
    public Transform overlapPosition;
    public float overlapRadius;
    public LayerMask overlapMask;

    public override void FixedUpdate()
    {
        if (!isServer)
            return;

        Collider[] colliders = Physics.OverlapSphere(overlapPosition.position, overlapRadius, overlapMask);

        if (!isCompleted && colliders.Length > 0)
        {
            PressButton();
        }
    }

    public virtual void PressButton()
    {
        if (isEnabled)
        {
            CompleteStep();
        }
    }
}