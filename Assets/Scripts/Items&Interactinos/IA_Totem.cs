using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class IA_Totem : InteractionArea
{
    [Header("Totem"), SyncVar]
    public int currentVisualStage;
    public bool maxVisualStageReached;
    public GameObject[] visuals;

    private void Start()
    {
        foreach(GameObject visual in visuals)
        {
            visual.SetActive(false);
        }
    }

    private void Update()
    {
        //set visuals based on stage
        for (int v = 0; v < visuals.Length; v++)
        {
            visuals[v].SetActive(v < currentVisualStage);
        }

        maxVisualStageReached = currentVisualStage >= visuals.Length;
        canBeInteractedWith = !maxVisualStageReached;
    }

    public override void OnEndInteraction()
    {
        PlayerPickup player = CustomNetworkManager.GetLocalPlayer().GetComponent<PlayerPickup>();
        if (player.heldItem != null) //we can match this more specifically later on
        {
            player.DestroyItem();
            currentVisualStage++;
        }

        base.OnEndInteraction();
    }
}
