using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class Totem : NetworkBehaviour
{
    [Header("References")]
    public MatchManager matchManager;

    [Header("Totem")]
    public TotemPieceGroup group = TotemPieceGroup.Any;
    [SyncVar]
    public int currentVisualStage;
    public bool maxVisualStageReached;
    public GameObject[] visuals;
    public GameObject smokePoof;

    [Header("UI")]
    public Text totemText;

    private void Start()
    {
        foreach (GameObject visual in visuals)
        {
            visual.SetActive(false);
        }

        if (matchManager == null)
        {
            matchManager = FindObjectOfType<MatchManager>();
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

        //update ui
        totemText.text = maxVisualStageReached ? "Completed!" : currentVisualStage + "/" + visuals.Length;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        if (maxVisualStageReached)
            return;

        PickupableItem item = other.GetComponent<PickupableItem>();
        TotemPiece piece = other.GetComponent<TotemPiece>();

        if(item != null)
        {
            if (piece != null && !piece.insertedToTotem && !item.isBeingHeld)
            {
                if (group == TotemPieceGroup.Any || group == piece.group)
                {
                    piece.group = TotemPieceGroup.None;
                    piece.insertedToTotem = true;
                    NetworkServer.Destroy(other.gameObject);
                    BuildTotem();
                }
            }
        }
    }

    //[Command]
    public void BuildTotem()
    {
        RpcBuildTotem(currentVisualStage);

        currentVisualStage++;
    }

    [ClientRpc]
    public void RpcBuildTotem(int stage)
    {
        Instantiate(smokePoof, visuals[Mathf.RoundToInt(Mathf.Clamp(stage, 0, visuals.Length - 1))].transform.position, Quaternion.identity);

        if (stage >= visuals.Length)
        {
            matchManager.UpdateTeamAndObjectiveUI();
        }
    }

    public void ResetTotem()
    {
        currentVisualStage = 0;
    }
}
