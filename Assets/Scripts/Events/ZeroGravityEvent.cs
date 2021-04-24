using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZeroGravityEvent : RandomEvent
{
    public GameObject zgravMessagePrefab;
    public Vector3 spawnOffset;

    public float duration;
    public Vector3 zGravity;

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
        }

        yield return new WaitForSeconds(duration);

        foreach (Rigidbody rb in bodies)
        {
            rb.useGravity = !rb.useGravity;
        }

        yield return null;
    }
}
