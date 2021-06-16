using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PalmTree : NetworkBehaviour
{
    public Vector3 punchBoxSize;
    public Vector3 punchBoxOffset;
    public Vector3 spawnOffset;
    public float spawnRandomness;
    public float spawnCooldown;
    public LayerMask overlapMask;

    [Header("References")]
    public GameObject coconut;
    public GameObject punchImpact;

    private float spawnTimer;

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        spawnTimer += Time.fixedDeltaTime;

        if(spawnTimer > spawnCooldown)
        {
            Collider[] colliders = Physics.OverlapBox(transform.position + punchBoxOffset, punchBoxSize / 2, Quaternion.identity, overlapMask, QueryTriggerInteraction.Collide);

            if (colliders.Length > 0)
            {
                NetworkServer.Spawn(Instantiate(coconut, transform.position + spawnOffset + Random.insideUnitSphere * spawnRandomness, Quaternion.identity));
                RpcPunchImpact(colliders[0].transform.position);
                spawnTimer = 0;
            }
        }
    }

    [ClientRpc]
    private void RpcPunchImpact(Vector3 pos)
    {
        Instantiate(punchImpact, pos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + punchBoxOffset, punchBoxSize);
    }
}
