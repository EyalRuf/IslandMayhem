using UnityEngine;
using System.Collections;
using Mirror;

public class IA_DiggingSite : InteractionArea
{
    [Header("DiggingSite")]
    public GameObject[] spawnableObjects;
    public int spawnPercentage;

    public override void OnEndInteraction()
    {
        // Spawn object on server
        CmdSpawnObj();

        base.OnEndInteraction();
    }

    [Command]
    void CmdSpawnObj()
    {
        GameObject toSpawn = spawnableObjects[Random.Range(0, spawnableObjects.Length)];
        GameObject spawned = Instantiate(toSpawn, transform.position + (Vector3.up * 3), transform.rotation);
        NetworkServer.Spawn(spawned);
    }
}