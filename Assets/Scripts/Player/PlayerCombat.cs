using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [Header("References")]
    public LocalPlayerInput lpInput;
    public ThirdPersonCharacterController cController;
    public PlayerItemInteractions pItems;
    public PlayerAnimations pAnims;

    [Header("Combat")]
    public PlayerPunch punchObj;
    public float punchCD = 0.5f;
    public int maxHp = 4;
    public int currHp = 4;
    public float regenerationDuration;
    private float regenerationTimer;
    private float punchCDTimer;
    public float invulnerabilityDuration;
    private float invulnerabilityTimer;

    void Start()
    {
        punchObj.initiatorNetId = netId;
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
        } else if (!cController.isCrippled && !cController.isHoldingItem && lpInput.attackInputDown)
        {
            CmdPlayerPunch(netId);
        }

        if (currHp < maxHp)
        {
            regenerationTimer -= Time.deltaTime;
            if (regenerationTimer <= 0)
            {
                cController.isCrippled = false;
                currHp = maxHp;
            }
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

    void ApplyHitOnSelf(Vector3 hitVector, int damage)
    {
        cController.rb.AddForce(hitVector, ForceMode.Impulse);
        cController.isBeingHit = true;
        invulnerabilityTimer = invulnerabilityDuration;

        if (!cController.isCrippled)
        {
            currHp -= damage;
            regenerationTimer = regenerationDuration;
            
            if (currHp <= 0)
            {
                Cripple();
            }
        }
    }

    [ClientRpc]
    void RpcPlayerWasHit(Vector3 hitVec, int damage)
    {
        ApplyHitOnSelf(hitVec, damage);
    }

    [Command]
    void CmdPlayerWasHit(uint playerNID, Vector3 hitVec, int damage)
    {
        GameObject player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerCombat pc = player.GetComponent<PlayerCombat>();
        pc.RpcPlayerWasHit(hitVec, damage);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLocalPlayer)
        {
            if (!cController.isBeingHit)
            {
                HitInflictor hit = other.GetComponent<HitInflictor>();

                if (hit != null && hit.isActive && hit.initiatorNetId != netId)
                {
                    Vector3 knockbackDir = transform.position - hit.transform.position;
                    CmdPlayerWasHit(netId, knockbackDir * hit.knockbackPower, hit.damage);
                    hit.HitInflicted();
                }
            }
        }
    }

    void Cripple ()
    {
        cController.isCrippled = true;
        pItems.DropItemIfHeld();
    }
}
