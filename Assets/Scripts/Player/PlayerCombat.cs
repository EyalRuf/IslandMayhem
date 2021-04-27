using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("References")]
    public LocalPlayerInput lpInput;
    public ThirdPersonCharacterController cController;
    public PlayerAnimations pAnims;

    [Header("Combat")]
    public PlayerPunch punchObj;
    public float punchCD = 0.5f;
    private float punchCDTimer;
    public float invulnerabilityDuration;
    private float invulnerabilityTimer;

    void Start()
    {
        punchObj.initiatorInstanceId = gameObject.GetInstanceID();
    }

    // Update is called once per frame
    void Update()
    {
        if (cController.isBeingHit)
        {
            invulnerabilityTimer -= Time.deltaTime;
            cController.isBeingHit = invulnerabilityTimer > 0;
        }

        if (punchCDTimer > 0)
        {
            punchCDTimer -= Time.deltaTime;
        } else if (!cController.isHoldingItem && lpInput.attackInputDown)
        {
            CmdPlayerPunch(netId);
        }
    }

    void Punch ()
    {
        punchObj.gameObject.SetActive(true);
        pAnims.PunchAnim();
    }

    [ClientRpc]
    void RpcPunch()
    {
        Punch();
    }

    [Command]
    void CmdPlayerPunch (uint playerNID)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerCombat pc = player.GetComponent<PlayerCombat>();
        pc.RpcPunch();
    }

    void ApplyHitOnSelf(Vector3 hitVector)
    {
        cController.rb.AddForce(hitVector, ForceMode.Impulse);
        invulnerabilityTimer = invulnerabilityDuration;
        cController.isBeingHit = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!cController.isBeingHit)
        {
            HitInflictor hit = other.GetComponent<HitInflictor>();

            if (hit != null && hit.isActive && hit.initiatorInstanceId != gameObject.GetInstanceID())
            {
                Vector3 knockbackDir = transform.position - hit.transform.position;
                ApplyHitOnSelf(knockbackDir * hit.knockbackPower);
                hit.HitInflicted();
            }
        }
    }
}
