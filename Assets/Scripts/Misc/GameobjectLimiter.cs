using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameobjectLimiter : MonoBehaviour
{
    public GameObject smokePoof;
    public GameobjectLimit[] limits;

    private void Start()
    {
        for (int l = 0; l < limits.Length; l++)
        {
            limits[l].currentlyInScene = GameObject.FindGameObjectsWithTag(limits[l].name).Length;
        }
    }

    private void FixedUpdate()
    {
        for (int l = 0; l < limits.Length; l++)
        {
            GameObject[] objectsInScene = GameObject.FindGameObjectsWithTag(limits[l].name);
            limits[l].currentlyInScene = objectsInScene.Length;

            int overLimit = limits[l].currentlyInScene - limits[l].limit;
            if (overLimit > 0)
            {
                for(int o = 0; o < overLimit; o++)
                {
                    Instantiate(smokePoof, objectsInScene[o].transform.position, Quaternion.identity);
                    Destroy(objectsInScene[o]);
                }
            }

        }
    }
}

[System.Serializable]
public class GameobjectLimit
{
    public string name;
    public int limit;
    public int currentlyInScene;
}