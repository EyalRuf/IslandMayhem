using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CampManager : NetworkBehaviour
{
    [SyncVar] public int currentMainCamp;
    public float swapDelay = 30f;
    [SyncVar] public float swapTimer;

    [HideInInspector] public Camp[] camps;

    private void Start()
    {
        camps = GetComponentsInChildren<Camp>();

        foreach (Camp camp in camps)
        {
            camp.campManager = this;
        }

        if (isServer)
        {
            SwapMainCamp();
        }
    }

    private void Update()
    {
        //toggle visuals for all camps
        for (int c = 0; c < camps.Length; c++)
        {
            camps[c].totemDispenserVisual.SetActive(c == currentMainCamp && !camps[c].isCoolingDown);
        }

        if (!isServer)
            return;

        swapTimer += Time.deltaTime;
        if(swapTimer > swapDelay)
        {
            camps[currentMainCamp].isTotemDispenser = true;
        }

        if (camps[currentMainCamp].isCoolingDown)
        {
            swapTimer = 0;

            SwapMainCamp();
        }
    }

    private void SwapMainCamp()
    {
        currentMainCamp = Random.Range(0, camps.Length);

        for(int c = 0; c < camps.Length; c++)
        {
            camps[c].isTotemDispenser = false;
        }
    }
}
