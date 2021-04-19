using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class ItemSpawner : NetworkBehaviour
{
    public GameObject[] spawnableObjects;
    public Vector3 spawnRange;
    public Vector2Int minMaxSpawns;

    public void Spawn()
    {
        if(transform.childCount > 0)
        {
            //get spawn positions
            List<Transform> spawnPositions = new List<Transform>();

            foreach (Transform child in transform)
            {
                spawnPositions.Add(child);
            }

            spawnPositions.OrderBy(s => Random.value);

            //spread items over points
            int spawns = Random.Range(minMaxSpawns.x, minMaxSpawns.y);
            for (int s = 0; s < spawns; s++)
            {
                GameObject objectToSpawn = spawnableObjects[Random.Range(0, spawnableObjects.Length)];

                //spawn
                Vector3 offset = Random.insideUnitSphere;
                NetworkServer.Spawn(Instantiate(objectToSpawn, spawnPositions[Mathf.RoundToInt(Mathf.Repeat(s, spawnPositions.Count))].position + new Vector3(spawnRange.x * offset.x, spawnRange.y * offset.y, spawnRange.z * offset.z), Quaternion.identity));
            }
        }
        else
        {
            int spawns = Random.Range(minMaxSpawns.x, minMaxSpawns.y);
            for (int s = 0; s < spawns; s++)
            {
                GameObject objectToSpawn = spawnableObjects[Random.Range(0, spawnableObjects.Length)];

                //spawn
                Vector3 offset = Random.insideUnitSphere;
                NetworkServer.Spawn(Instantiate(objectToSpawn, transform.position + new Vector3(spawnRange.x * offset.x, spawnRange.y * offset.y, spawnRange.z * offset.z), Quaternion.identity));
            }
        }

        Destroy(this);
    }
}
