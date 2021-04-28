using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class TagItemSpawner : NetworkBehaviour
{
    public string targetTag;
    public GameObject[] spawnableObjects;
    public Vector3 spawnRange;
    public Vector3 spawnOffset;
    public Vector2Int minMaxSpawns;
    public bool destroyAfterSpawn;

    public void Spawn()
    {
        //get spawn positions
        List<GameObject> spawnPositions = new List<GameObject>();

        spawnPositions.AddRange(GameObject.FindGameObjectsWithTag(targetTag));

        spawnPositions.OrderBy(s => Random.value);

        //spread items over points
        int spawns = Random.Range(minMaxSpawns.x, minMaxSpawns.y);
        for (int s = 0; s < spawns; s++)
        {
            GameObject objectToSpawn = spawnableObjects[Random.Range(0, spawnableObjects.Length)];

            //spawn
            Vector3 offset = Random.insideUnitSphere;
            NetworkServer.Spawn(Instantiate(objectToSpawn, spawnPositions[Mathf.RoundToInt(Mathf.Repeat(s, spawnPositions.Count))].transform.position + new Vector3(spawnRange.x * offset.x, spawnRange.y * offset.y, spawnRange.z * offset.z) + spawnOffset, Quaternion.identity));
        }

        if (destroyAfterSpawn)
        {
            Destroy(this);
        }
    }
}
