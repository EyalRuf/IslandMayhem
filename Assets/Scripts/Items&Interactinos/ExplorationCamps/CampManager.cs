using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CampManager : NetworkBehaviour
{
    [SyncVar] public int currentMainCamp;
    public float swapDelay = 30f;

    public Camp[] camps;

    private void Start()
    {
        if (camps == null || camps.Length == 0)
        {
            camps = GetComponentsInChildren<Camp>(true);

            foreach (Camp camp in camps)
            {
                camp.campManager = this;
            }
        }
    }

    public void SwapMainCamp()
    {
        if (isServer)
        {
            int newMainCamp = currentMainCamp;
            while (newMainCamp == currentMainCamp)
            {
                newMainCamp = Random.Range(0, camps.Length);
            }

            RpcSwapMainCamp(newMainCamp);
        }
    }

    [ClientRpc]
    public void RpcSwapMainCamp(int newMainCamp)
    {
        for (int c = 0; c < camps.Length; c++)
        {
            camps[c].isTotemDispenser = false;
        }

        camps[newMainCamp].SetMainCamp(swapDelay);
        currentMainCamp = newMainCamp;
    }
}
