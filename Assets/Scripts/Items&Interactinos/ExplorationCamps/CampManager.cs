using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CampManager : NetworkBehaviour
{
    [SyncVar] public int currentMainCamp;
    public float swapDelay = 30f;
    [SyncVar] public float swapTimer;

    public Camp[] camps;

    private void Start()
    {
        camps = GetComponentsInChildren<Camp>(true);

        foreach (Camp camp in camps)
        {
            camp.campManager = this;
        }
    }

    private void Update()
    {
        if (!isServer)
            return;

        //swapTimer += Time.deltaTime;
        //if(swapTimer > swapDelay)
        //{
        //    camps[currentMainCamp].isTotemDispenser = true;
        //    camps[currentMainCamp].isCoolingDown = false;
        //}
    }

    public void SwapMainCamp()
    {
        swapTimer = 0;

        int newMainCamp = currentMainCamp;
        while (newMainCamp == currentMainCamp)
        {
            newMainCamp = Random.Range(0, camps.Length);
        }

        for (int c = 0; c < camps.Length; c++)
        {
            camps[c].isTotemDispenser = false;
        }

        camps[newMainCamp].SetMainCamp(swapDelay);
        currentMainCamp = newMainCamp;
    }
}
