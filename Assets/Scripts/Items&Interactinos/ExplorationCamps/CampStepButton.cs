using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampStepButton : CampStep
{
    [Header("Button")]
    public float duration;
    public Transform overlapPosition;
    public float overlapRadius;
    public LayerMask overlapMask;

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        Collider[] colliders = Physics.OverlapSphere(overlapPosition.position, overlapRadius, overlapMask);

        if (!isCompleted && colliders.Length > 0)
        {
            StartCoroutine(PressButton());
        }
    }

    private IEnumerator PressButton()
    {
        CompleteStep();

        yield return new WaitForSeconds(duration);

        ResetStep();

        yield return null;
    }
}
