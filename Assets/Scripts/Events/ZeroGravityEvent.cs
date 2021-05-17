using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZeroGravityEvent : RandomEvent
{
    public GameObject zgravMessagePrefab;
    public Vector3 spawnOffset;

    public float duration;
    public float upwardsForce;
    [Range(0f, 10f)]
    public float downforce = 0.1f;

    public override void ServerEvent()
    {
        //make sure that host + client only gets called once?
        if (!isClient)
        {
            StartCoroutine(ZgravRoutine());
        }
    }

    public override void ClientEvent()
    {
        StartCoroutine(ZgravRoutine());
    }

    private IEnumerator ZgravRoutine()
    {
        //send message
        Instantiate(zgravMessagePrefab, CustomNetworkManager.GetLocalPlayer().transform.position + spawnOffset, Quaternion.identity);

        Rigidbody[] bodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in bodies)
        {
            rb.useGravity = !rb.useGravity;
            rb.AddForce(Vector3.up * upwardsForce, ForceMode.Acceleration);
        }

        //wait with downforce
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.fixedDeltaTime;

            foreach (Rigidbody rb in bodies)
            {
                if (rb != null)
                {
                    rb.AddForce(Physics.gravity * downforce * rb.mass, ForceMode.Acceleration);
                }
            }

            yield return new WaitForFixedUpdate();
        }

        //return to normal
        foreach (Rigidbody rb in bodies)
        {
            if(rb != null)
            {
                rb.useGravity = !rb.useGravity;
            }
        }

        yield return null;
    }
}
