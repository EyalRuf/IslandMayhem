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
    public uint initiatorNetId;
    public float knockbackPower;
    public bool isStunning;
    [SyncVar]
    public int damage = 1;
    public bool singularDmgInstance;

    public virtual void Update()
    {
    }

    public virtual void HitInflicted ()
    {
        isActive = !singularDmgInstance;
    }

    public void ActivateInflictorForDuration (uint initiatorNetId, float durationInSeconds)
    {
        isActive = true;
        this.initiatorNetId = initiatorNetId;
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
