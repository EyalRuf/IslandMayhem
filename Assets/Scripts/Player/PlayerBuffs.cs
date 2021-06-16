using UnityEngine;
using System.Collections;
using Mirror;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerBuffs : NetworkBehaviour
{
    [Header("References")]
    public PlayerCombat pCombat;
    public PlayerPunch pp;

    [Header("Props")]
    [SyncVar]
    public bool msBuffActive = false;
    public float moveSpeedMultiplyer = 1.5f;
    Coroutine msCR;

    public float scaleMultiplyer = 1.5f;
    Coroutine sCR;

    [SyncVar]
    public bool jumpBuffActive = false;
    public float jumpForceMultiplyer = 1.75f;
    Coroutine jCR;

    public float hpMultiplyer = 1.45f;
    Coroutine hpCR;

    public float punchPowerMultiplyer = 1.75f;
    public float punchKBMultiplyer = 1.45f;
    Coroutine ppCR;

    public float buffDecayTime = 30;

    [Header("ParticleEffects")]
    public Transform buffTransform;
    public GameObject moveSpeedBuffParticle;
    public GameObject scaleBuffParticle;
    public GameObject jumpBuffParticle;
    public GameObject healthBuffParticle;
    public GameObject punchBuffParticle;

    [Header("UI")]
    public GameObject msBuffIcon;
    public GameObject scaleBuffIcon;
    public GameObject jumpBuffIcon;
    public GameObject hpBuffIcon;
    public GameObject punchBuffIcon;
    public Color hpBaseColor;
    public Color hpBuffColor;
    public Image hpBar;

    [ClientRpc]
    public void RpcGiveBuff(Buff buff)
    {
        Coroutine c = StartCoroutine(BuffDecay(buff));

        switch (buff)
        {
            case Buff.MoveSpeed:
            {
                    msBuffActive = true;
                    Instantiate(moveSpeedBuffParticle, buffTransform);
                    msBuffIcon.SetActive(true);

                    if (msCR != null)
                    {
                        StopCoroutine(msCR);
                        msCR = c;
                    }

                    break;
            }
            case Buff.SizeUp:
            {
                    transform.localScale = scaleMultiplyer * Vector3.one;
                    Instantiate(scaleBuffParticle, buffTransform);
                    scaleBuffIcon.SetActive(true);

                    if (sCR != null)
                    {
                        StopCoroutine(sCR);
                        sCR = c;
                    }

                    break;
            }
            case Buff.JumpForce:
            {
                    jumpBuffActive = true;
                    Instantiate(jumpBuffParticle, buffTransform);
                    jumpBuffIcon.SetActive(true);

                    if (jCR != null)
                    {
                        StopCoroutine(jCR);
                        jCR = c;
                    }

                    break;
            }
            case Buff.Health:
            {
                    pCombat.maxHp = (int)(pCombat.baseMaxHp * hpMultiplyer);
                    pCombat.currHp = pCombat.maxHp;

                    Instantiate(healthBuffParticle, buffTransform);
                    hpBuffIcon.SetActive(true);
                    hpBar.color = hpBuffColor;

                    if (hpCR != null)
                    {
                        StopCoroutine(hpCR);
                        hpCR = c;
                    }

                    break;
            }
            case Buff.PunchPower:
            {
                    pp.damage = (int) (pp.punchBaseDmg * punchPowerMultiplyer);
                    pp.knockbackPower = pp.punchBaseKP * punchKBMultiplyer;

                    Instantiate(punchBuffParticle, buffTransform);
                    punchBuffIcon.SetActive(true);

                    if (ppCR != null)
                    {
                        StopCoroutine(ppCR);
                        ppCR = c;
                    }

                    break;
            }
            default:
            {
                break;
            }
        }

    }

    IEnumerator BuffDecay (Buff buff)
    {
        yield return new WaitForSeconds(buffDecayTime);

        switch (buff)
        {
            case Buff.MoveSpeed:
                {
                    msBuffActive = false;
                    msBuffIcon.SetActive(false);
                    break;
                }
            case Buff.SizeUp:
                {
                    transform.localScale = Vector3.one;
                    scaleBuffIcon.SetActive(false);
                    break;
                }
            case Buff.JumpForce:
                {
                    jumpBuffActive = false;
                    jumpBuffIcon.SetActive(false);
                    break;
                }
            case Buff.Health:
                {
                    pCombat.maxHp = pCombat.baseMaxHp;
                    pCombat.currHp = pCombat.currHp > pCombat.maxHp ? pCombat.maxHp : pCombat.currHp;
                    hpBuffIcon.SetActive(false);
                    hpBar.color = hpBaseColor;

                    break;
                }
            case Buff.PunchPower:
                {
                    pp.damage = pp.punchBaseDmg;
                    pp.knockbackPower = pp.punchBaseKP;
                    punchBuffIcon.SetActive(false);

                    break;
                }
            default:
                {
                    break;
                }
        }
    }
}
