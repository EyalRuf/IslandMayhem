using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonCharacterController cController;

    [Header("Combat")]
    public float invulnerabilityDuration;
    private float invulnerabilityTimer;

    // Update is called once per frame
    void Update()
    {
        if (cController.isBeingHit)
        {
            invulnerabilityTimer -= Time.deltaTime;
            cController.isBeingHit = invulnerabilityTimer > 0;
        }
    }

    void ApplyHitOnSelf(Vector3 hitVector)
    {
        cController.rb.AddForce(hitVector, ForceMode.Impulse);
        invulnerabilityTimer = invulnerabilityDuration;
        cController.isBeingHit = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!cController.isBeingHit)
        {
            HitInflictor hit = other.GetComponent<HitInflictor>();

            if (hit != null && hit.isActive && hit.initiatorInstanceId != gameObject.GetInstanceID())
            {
                Vector3 knockbackDir = transform.position - hit.transform.position;
                ApplyHitOnSelf(knockbackDir * hit.knockbackPower);
                hit.HitInflicted();
            }
        }
    }
}
