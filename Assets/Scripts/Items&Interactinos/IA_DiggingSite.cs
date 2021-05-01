using UnityEngine;
using System.Collections;
using Mirror;
using System;

public class IA_DiggingSite : InteractionArea
{
    [Header("DiggingSite")]
    public GameObject[] spawnableObjects;
    public int spawnPercentage;

    public override IEnumerator Interact(Action<InteractionArea> endInteractionListener)
    {
        yield return base.Interact(endInteractionListener);

        // Spawn object on server
        CmdSpawnObj();
        EndInteraction(endInteractionListener);
    }

    [Command(ignoreAuthority = true)]
    void CmdSpawnObj()
    {
        GameObject toSpawn = spawnableObjects[UnityEngine.Random.Range(0, spawnableObjects.Length)];
        GameObject spawned = Instantiate(toSpawn, transform.position + (Vector3.up * 3), transform.rotation);
        NetworkServer.Spawn(spawned);
    }
}