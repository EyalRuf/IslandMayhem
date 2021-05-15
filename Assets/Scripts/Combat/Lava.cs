using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    public int damage = 1;
    public Vector3 hitVector;

    private void OnTriggerStay(Collider other)
    {
        PlayerCombat pc = other.GetComponent<PlayerCombat>();

        if(pc != null)
        {
            if (!pc.isInvulnerable)
            {
                pc.CmdPlayerWasHit(pc.netId, hitVector, damage);
            }
        }
    }
}
