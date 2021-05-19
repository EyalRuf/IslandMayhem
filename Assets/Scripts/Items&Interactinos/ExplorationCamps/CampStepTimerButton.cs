using UnityEngine;

public class CampStepTimerButton : TimerCampStep
{
    [Header("Button")]
    public Transform overlapPosition;
    public float overlapRadius;
    public LayerMask overlapMask;

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!isServer)
            return;

        Collider[] colliders = Physics.OverlapSphere(overlapPosition.position, overlapRadius, overlapMask);

        if (!isCompleted && colliders.Length > 0)
        {
            PressButton();
        }
    }

    private void PressButton()
    {
        CompleteStep();
    }
}