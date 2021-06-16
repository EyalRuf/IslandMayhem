using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombat : NetworkBehaviour
{
    [Header("References")]
    public LocalPlayerInput lpInput;
    public ThirdPersonCharacterController cController;
    public PlayerItemInteractions pItems;
    public PlayerAnimations pAnims;
    public CampManager cm;
    public MatchManager matchManager;
    public PlayerBuffs pBuffs;

    [Header("Combat")]
    public PlayerPunch punchObj;
    public float punchCD = 0.75f;
    public int baseMaxHp;
    [SyncVar]
    public int maxHp;
    [SyncVar]
    public int currHp;
    public Image hpBarLocal;
    public Image hpBarRemote;
    public bool isInvulnerable;
    public float invulnerabilityDuration;
    public float miniStunDuration;
    public float crippleDuration;
    public float knockDownDuration;
    [Range(0f, 1f)]
    public float knockDownChance;
    private float punchCDTimer;
    private float reviveTimer;

    [Header("Regeneration")]
    public int regenAmount;
    public Vector2 regenMinMax;
    public float currRegenInterval;
    public float regenIntervalInc;
    public float timeBeforeRegen;
    private float regenTimer;
    private float timerBeforeRegen;

    [Header("Particles")]
    public GameObject hitParticles;
    public Vector3 hitParticlesOffset;
    public GameObject stunnedParticles;
    public GameObject deathParticles;
    public GameObject spawnParticles;

    [Header("Death&Respawn")]
    public List<Behaviour> turnOffWhenDead;
    public float deathAnimTime;
    public float fadeTime;
    public float deathTime;
    public DeathFade deathFade;

    void Start()
    {
        punchObj.initiatorNetId = netId;
        cm = FindObjectOfType<CampManager>();
        matchManager = FindObjectOfType<MatchManager>();
    }

    // Update is called once per frame
    void Update()
    {
        hpBarLocal.fillAmount = Mathf.Lerp(hpBarLocal.fillAmount, (float) currHp / maxHp, 0.05f);
        hpBarRemote.fillAmount = Mathf.Lerp(hpBarRemote.fillAmount, (float) currHp / maxHp, 0.05f);

        if (isLocalPlayer && !cController.isDead)
        {
            if (punchCDTimer > 0)
            {
                punchCDTimer -= Time.deltaTime;
            } else if (!cController.isCrippled && !cController.isHoldingItem && lpInput.attackInputDown)
            {
                CmdPlayerPunch(netId);
            }

            if (currHp < maxHp)
            {
                if (cController.isCrippled)
                {
                    reviveTimer -= Time.deltaTime;
                    if (reviveTimer <= 0)
                    {
                        CmdRevive(netId);
                    }
                } else
                {
                    timerBeforeRegen -= Time.deltaTime;
                    if (timerBeforeRegen <= 0)
                    {
                        regenTimer -= Time.deltaTime;
                        if (regenTimer <= 0)
                        {
                            CmdRegen();
                            currRegenInterval -= regenIntervalInc;
                            currRegenInterval = Mathf.Clamp(currRegenInterval, regenMinMax.x, regenMinMax.y);
                            regenTimer = currRegenInterval;
                        }
                    }
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

    private void ApplyHitOnSelf(Vector3 hitVector, int damage, bool isStun)
    {
        Instantiate(hitParticles, transform.position + hitParticlesOffset, hitParticles.transform.rotation);
        cController.rb.AddForce(hitVector, ForceMode.Impulse);
        pItems.DropItemIfHeld();
        pAnims.GetHitAnim();

        isInvulnerable = true;
        StartCoroutine(InvulnerabilityTime());

        if (matchManager.gameStarted)
        {
            stunnedParticles.SetActive(true);

            currRegenInterval = regenMinMax.y;
            regenTimer = currRegenInterval;
            timerBeforeRegen = timeBeforeRegen;

            currHp -= damage;

            if (currHp <= 0)
            {
                // Death sequence
                StartCoroutine(DeathSequence());
            }
            else if (!cController.isCrippled) {
               if (isStun)
                    Cripple();
                else 
                    Ministun();
            }
        }
    }

    IEnumerator DeathSequence()
    {
        // Turn off behaviors + Death animation
        stunnedParticles.SetActive(false);
        cController.isDead = true;
        cController.isLookingAround = true;
        deathParticles.SetActive(true);
        pAnims.DeathAnim();

        yield return new WaitForSeconds(deathAnimTime);

        // Fadeout 
        deathFade.FadeOut(fadeTime);

        yield return new WaitForSeconds(fadeTime);

        deathParticles.SetActive(false);
        pBuffs.PurgeBuffs();

        // Teleport player
        int randomCamp = UnityEngine.Random.Range(0, cm.camps.Length);
        while (cm.camps.Length > 1 && randomCamp == cm.currentMainCamp) { randomCamp = UnityEngine.Random.Range(0, cm.camps.Length); }

        Camp spawnCamp = cm.camps[randomCamp];
        Transform spawnPos = spawnCamp.spawnPositions[UnityEngine.Random.Range(0, spawnCamp.spawnPositions.Length)];

        transform.position = spawnPos.position;

        yield return new WaitForSeconds(deathTime);

        // Fadein
        deathFade.FadeIn(fadeTime);
        
        // Turn behaviors on
        cController.isDead = false;
        cController.isCrippled = false;
        cController.isLookingAround = false;
        currHp = maxHp;
        spawnParticles.SetActive(true);

        yield return new WaitForSeconds(fadeTime);

        // after finishing revive
        spawnParticles.SetActive(false);
    }

    IEnumerator InvulnerabilityTime()
    {
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    [ClientRpc]
    public void RpcPlayerWasHit(Vector3 hitVec, int damage, bool isStun)
    {
        ApplyHitOnSelf(hitVec, damage, isStun);
    }

    [Command]
    public void CmdPlayerWasHit(uint playerNID, Vector3 hitVec, int damage, bool isStun)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerCombat pc = player.GetComponent<PlayerCombat>();
        pc.RpcPlayerWasHit(hitVec, damage, isStun);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLocalPlayer && !isInvulnerable && !cController.isDead)
        {
            HitInflictor hit = other.GetComponent<HitInflictor>();

            if (hit != null && hit.isActive && hit.initiatorNetId != netId)
            {
                Vector3 knockbackDir = (other.GetComponent<Lava>() != null) ? Vector3.up : (transform.position - hit.transform.position).normalized;
                CmdPlayerWasHit(netId, knockbackDir * hit.knockbackPower, hit.damage, hit.isStunning);

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
        reviveTimer = crippleDuration;
        cController.isCrippled = true;
    }

    void Knockdown ()
    {
        reviveTimer = knockDownDuration;
        cController.isCrippled = true;
        cController.isKnockedDown = true;
    }

    void Revive ()
    {
        cController.isKnockedDown = false;
        cController.isCrippled = false;
        stunnedParticles.SetActive(false);
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

    [Command]
    void CmdRegen()
    {
        currHp = Mathf.Clamp(currHp + regenAmount, 0, maxHp);
    }
}
