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
    public float punchCD = 0.75f;
    public int maxHp = 4;
    public int currHp = 4;
    public bool isInvulnerable;
    public float invulnerabilityDuration;
    public float miniStunDuration;
    public float crippleDuration;
    public float knockDownDuration;
    [Range(0f, 1f)]
    public float knockDownChance;

    private float regenerationTimer;
    private float punchCDTimer;

    [Header("Particles")]
    public GameObject hitParticles;
    public Vector3 hitParticlesOffset;
    public GameObject stunnedParticles;

    void Start()
    {
        punchObj.initiatorNetId = netId;
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            if (punchCDTimer > 0)
            {
                punchCDTimer -= Time.deltaTime;
            } else if (!cController.isCrippled && !cController.isHoldingItem && lpInput.attackInputDown)
            {
                CmdPlayerPunch(netId);
            }

            if (currHp < maxHp && cController.isCrippled)
            {
                regenerationTimer -= Time.deltaTime;
                if (regenerationTimer <= 0)
                {
                    CmdRevive(netId);
                }
            }
        }
    }

    void Punch ()
    {
        punchCDTimer = punchCD;
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
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerCombat pc = player.GetComponent<PlayerCombat>();
        pc.RpcPunch();
    }

    private void ApplyHitOnSelf(Vector3 hitVector, int damage)
    {
        cController.rb.AddForce(hitVector, ForceMode.Impulse);
        pItems.DropItemIfHeld();
        
        pAnims.GetHitAnim();
        stunnedParticles.SetActive(true);

        isInvulnerable = true;
        StartCoroutine(InvulnerabilityTime());
        
        if (!cController.isCrippled)
        {
            currHp -= damage;
            
            if (currHp <= 0)
            {
                if(Random.value < knockDownChance)
                {
                    Knockdown();
                }
                else
                {
                    Cripple();
                }
            }
            else
            {
                Ministun();
            }
        }
    }

    IEnumerator InvulnerabilityTime()
    {
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    [ClientRpc]
    void RpcPlayerWasHit(Vector3 hitVec, int damage)
    {
        //spawn hit particles
        Instantiate(hitParticles, transform.position + hitParticlesOffset, hitParticles.transform.rotation);
        ApplyHitOnSelf(hitVec, damage);
    }

    [Command]
    public void CmdPlayerWasHit(uint playerNID, Vector3 hitVec, int damage)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerCombat pc = player.GetComponent<PlayerCombat>();
        pc.RpcPlayerWasHit(hitVec, damage);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLocalPlayer && !isInvulnerable)
        {
            HitInflictor hit = other.GetComponent<HitInflictor>();

            if (hit != null && hit.isActive && hit.initiatorNetId != netId)
            {
                Vector3 knockbackDir = (other.GetComponent<Lava>() != null) ? Vector3.up : (transform.position - hit.transform.position).normalized;
                CmdPlayerWasHit(netId, knockbackDir * hit.knockbackPower, hit.damage);

                hit.HitInflicted();
            }
        }
    }

    void Ministun ()
    {
        StartCoroutine(MinistunTime());
    }

    IEnumerator MinistunTime()
    {
        yield return new WaitForSeconds(miniStunDuration);
        stunnedParticles.SetActive(false);
    }

    void Cripple ()
    {
        regenerationTimer = crippleDuration;
        cController.isCrippled = true;
    }

    void Knockdown ()
    {
        regenerationTimer = knockDownDuration;
        cController.isCrippled = true;
        cController.isKnockedDown = true;
    }

    void Revive ()
    {
        cController.isKnockedDown = false;
        cController.isCrippled = false;
        stunnedParticles.SetActive(false);

        if (currHp <= 0)
        {
            currHp = maxHp;
        }
    }

    [ClientRpc]
    void RpcRevive()
    {
        Revive();
    }

    [Command]
    void CmdRevive(uint playerNID)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerCombat pc = player.GetComponent<PlayerCombat>();
        pc.RpcRevive();
    }
}
