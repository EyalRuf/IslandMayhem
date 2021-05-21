using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Lava : NetworkBehaviour
{
    public int damage = 1;
    public Vector3 hitVector;
    public float cooldown;

    private float cooldownTimer;

    private void FixedUpdate()
    {
        if (isClient)
        {
            cooldownTimer += Time.fixedDeltaTime;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isClient)
        {
            if (cooldownTimer > cooldown)
            {
                PlayerCombat pc = other.GetComponent<PlayerCombat>();

                if (pc != null)
                {
                    if (!pc.isInvulnerable && pc.isLocalPlayer)
                    {
                        pc.CmdPlayerWasHit(pc.netId, hitVector, damage);
                        cooldownTimer = 0;
                    }
                }
            }
        }
    }
}
