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

    public virtual IEnumerator Interact (Action<InteractionArea> endInteractionListener)
    {
        beingInteractedWith = true;
        yield return new WaitForSeconds(interactionDuration);
    }

    protected virtual void EndInteraction(Action<InteractionArea> endInteractionListener)
    {
        beingInteractedWith = false;
        canBeInteractedWith = !isDisabledAfterInteraction;
        endInteractionListener(this);
    }
}
