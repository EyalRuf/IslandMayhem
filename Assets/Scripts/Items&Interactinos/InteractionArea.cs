using UnityEngine;
using System.Collections;
using Mirror;
using System;

public class InteractionArea : NetworkBehaviour
{
    [Header("InteractionArea")]
    [SyncVar]
    public bool canBeInteractedWith;
    [SyncVar]
    public bool beingInteractedWith;
    public bool isDisabledAfterInteraction;
    public float interactionDuration;

    public int restrictedToTeam = -1;

    public virtual IEnumerator Interact (Func<bool> onEndInteraction)
    {
        this.OnStartInteraction();
        yield return new WaitForSeconds(interactionDuration);
        bool flagShouldEndInteraction = this.OnBeforeEndInteraction(onEndInteraction);
        if (flagShouldEndInteraction)
            this.OnEndInteraction();
    }

    public virtual void OnStartInteraction()
    {
        beingInteractedWith = true;
        netIdentity.AssignClientAuthority(CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkIdentity>().connectionToClient);
    }

    public virtual bool OnBeforeEndInteraction(Func<bool> onEndInteraction)
    {
        beingInteractedWith = false;
        return onEndInteraction();
    }

    public virtual void OnEndInteraction()
    {
        canBeInteractedWith = !isDisabledAfterInteraction;
        netIdentity.RemoveClientAuthority();
    }
}
