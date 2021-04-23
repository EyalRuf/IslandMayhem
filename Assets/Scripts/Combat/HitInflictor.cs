using UnityEngine;
using System.Collections;
using Mirror;
using System;

[RequireComponent(typeof(Collider))]
public class HitInflictor : NetworkBehaviour
{
    [Header("Hit")]
    [SyncVar]
    public bool isActive;
    [SyncVar]
    public int initiatorInstanceId;
    public float knockbackPower;
    public bool singularDmgInstance;

    public virtual void Update()
    {
    }

    public virtual void HitInflicted ()
    {
        isActive = !singularDmgInstance || isActive;
    }

    public void ActivateInflictorForDuration (int initiatorInstanceId, float durationInSeconds)
    {
        isActive = true;
        this.initiatorInstanceId = initiatorInstanceId;
        StartCoroutine(DeactivateInflictor(durationInSeconds));
    }

    IEnumerator DeactivateInflictor (float durationInSeconds)
    {
        yield return new WaitForSeconds(durationInSeconds);
        Deactivate();
    }

    public void Deactivate()
    {
        StopCoroutine("DeactivateInflictor");
        isActive = false;
    }
}
