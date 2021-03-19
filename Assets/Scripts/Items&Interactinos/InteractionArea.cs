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

    public virtual IEnumerator Interact (Action onEndInteraction)
    {
        this.OnStartInteraction();
        yield return new WaitForSeconds(interactionDuration);
        this.OnBeforeEndInteraction(onEndInteraction);
        this.OnEndInteraction();
    }

    public virtual void OnStartInteraction()
    {
        beingInteractedWith = true;
        netIdentity.AssignClientAuthority(CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkIdentity>().connectionToClient);
    }

    public virtual void OnBeforeEndInteraction(Action onEndInteraction)
    {
        beingInteractedWith = false;
        onEndInteraction();
    }

    public virtual void OnEndInteraction()
    {
        canBeInteractedWith = !isDisabledAfterInteraction;
        netIdentity.RemoveClientAuthority();
    }
}
