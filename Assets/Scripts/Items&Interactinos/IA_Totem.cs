using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class IA_Totem : InteractionArea
{
    [Header("Totem")]
    public TotemPieceGroup group = TotemPieceGroup.Any;
    [SyncVar]
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
        PlayerPickupAndThrow player = CustomNetworkManager.GetLocalPlayer().GetComponent<PlayerPickupAndThrow>();
        TotemPiece piece = player.heldItem.GetComponent<TotemPiece>();
        if (player.heldItem != null && piece != null) //we can match this more specifically later on
        {
            if (group == TotemPieceGroup.Any || group == piece.group)
            {
                player.DestroyItem();
                currentVisualStage++;
            }
        }

        base.OnEndInteraction();
    }
}
