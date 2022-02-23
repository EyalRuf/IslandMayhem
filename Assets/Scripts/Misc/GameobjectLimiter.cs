using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameobjectLimiter : NetworkBehaviour
{
    public GameObject smokePoof;
    public GameobjectLimit[] limits;

    private void Start()
    {
        if (!isServer)
            return;

        for (int l = 0; l < limits.Length; l++)
        {
            limits[l].currentlyInScene = GameObject.FindGameObjectsWithTag(limits[l].name).Length;
        }
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        for (int l = 0; l < limits.Length; l++)
        {
            GameObject[] objectsInScene = GameObject.FindGameObjectsWithTag(limits[l].name);
            limits[l].currentlyInScene = objectsInScene.Length;

            int overLimit = limits[l].currentlyInScene - limits[l].limit;
            if (overLimit > 0)
            {
                for(int o = 0; o < overLimit; o++)
                {
                    GameObject go = objectsInScene[o];
                    PickupableItem pi = go.GetComponent<PickupableItem>();
                    if (pi != null)
                    {
                        if (pi.isBeingHeld)
                            continue;
                    }

                    RpcSpawnSmokePoof(objectsInScene[o].transform.position, Quaternion.identity);
                    NetworkServer.Destroy(objectsInScene[o]);
                }
            }

        }
    }

    [ClientRpc]
    private void RpcSpawnSmokePoof(Vector3 position, Quaternion rotation)
    {
        Instantiate(smokePoof, position, rotation);
    }
}

[System.Serializable]
public class GameobjectLimit
{
    public string name;
    public int limit;
    public int currentlyInScene;
}