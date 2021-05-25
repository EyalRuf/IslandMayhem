using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimations : NetworkBehaviour
{
    public float triggerResetTime = 0.1f;
    private const string anim_param_b_grounded = "isGrounded";
    private const string anim_param_b_falling = "isFalling";
    private const string anim_param_b_walk = "isWalking";
    private const string anim_param_b_sprint = "isSprinting";
    private const string anim_param_b_holding_item = "isHoldingAnItem";
    private const string anim_param_b_throwing = "isThrowing";
    private const string anim_param_b_aiming = "isAiming";
    private const string anim_param_b_crippled = "isCrippled";
    private const string anim_param_b_interacting = "isInteracting";
    private const string anim_param_t_beingHit = "beingHitTrigger";
    private const string anim_param_t_jump = "jumpTrigger";
    private const string anim_param_t_throw = "throwTrigger";
    private const string anim_param_t_punch = "punchTrigger";

    [Header("Skins")]
    public Skin chosenSkin = Skin.Random;
    [SyncVar]
    public int currentSkin = -1;
    public Animator[] skins;
    private bool selectedSkin;
    public float invunerabilityBlinkSpeed = 0.1f;

    [Header("References")]
    public Animator animator;
    public NetworkAnimator netAnimator;
    public ThirdPersonCharacterController cController;
    public PlayerCombat combat;

    [Header("Etc")]
    bool isJumping;
    bool isThrowing;

    [Header("Networking")]
    [SyncVar]
    public float netPlayerAnimatorSpeed;

    private void Start()
    {
        if (isServer)
        {
            currentSkin = chosenSkin != Skin.Random ? (int) chosenSkin : UnityEngine.Random.Range(0, skins.Length);
        }

        foreach (Animator skin in skins)
        {
            skin.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update ()
    {
        if (currentSkin >= 0 && !selectedSkin)
        {
            SetSkin();
        }

        //invunerablility stuff
        skins[currentSkin].gameObject.SetActive(combat.isInvulnerable ? Mathf.PingPong(Time.time, invunerabilityBlinkSpeed) > invunerabilityBlinkSpeed / 2 : true);

        if (isLocalPlayer)
        {
            //set variables
            animator.SetBool(anim_param_b_grounded, cController.isGrounded);
            animator.SetBool(anim_param_b_falling, !cController.isGrounded && cController.playerVelocity.y < -0.35f);
            animator.SetBool(anim_param_b_walk, !isJumping && cController.isMoving);
            animator.SetBool(anim_param_b_sprint, !isJumping && cController.isSprinting);
            animator.SetBool(anim_param_b_aiming, cController.isAiming);
            animator.SetBool(anim_param_b_holding_item, isThrowing || cController.playerItems.heldItem != null);
            animator.SetBool(anim_param_b_throwing, isThrowing);
            animator.SetBool(anim_param_b_crippled, cController.isCrippled);
        }
    }

    void SetSkin ()
    {
        skins[currentSkin].gameObject.SetActive(true);
        animator = skins[currentSkin];
        netAnimator.animator = animator;

        selectedSkin = true;
    }

    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            // Matching walking/sprinting animation speeds to actual movement speeds
            float animSpeed = animator.GetCurrentAnimatorStateInfo(0).IsTag("MovingOnFloor") ? (cController.currMoveSpeed / cController.baseMoveSpeed) : 1;
            animator.speed = animSpeed;
            CmdUpdatePlayerAnimSpeed(netId, animSpeed);
        } else
        {
            animator.speed = netPlayerAnimatorSpeed;
        }
    }

    [Command]
    void CmdUpdatePlayerAnimSpeed(uint playerNID, float animSpeed)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        PlayerAnimations pa = player.GetComponent<PlayerAnimations>();
        pa.netPlayerAnimatorSpeed = animSpeed;
    }

    IEnumerator ResetTriggerCR(Action resetTriggerFunc, float resetTimer)
    {
        yield return new WaitForSeconds(resetTimer);
        resetTriggerFunc();
    }

    public void JumpAnim ()
    {
        animator.SetTrigger(anim_param_t_jump);
        isJumping = true;
        StartCoroutine(ResetTriggerCR(ResetJumpTrigger, triggerResetTime));
    }

    public void ResetJumpTrigger ()
    {
        isJumping = false;
        animator.ResetTrigger(anim_param_t_jump);
    }

    public void ThrowAnim ()
    {
        isThrowing = true;
        animator.SetTrigger(anim_param_t_throw);
        StartCoroutine(ResetTriggerCR(ResetThrowTrigger, triggerResetTime));
    }

    void ResetThrowTrigger ()
    {
        isThrowing = false;
        animator.ResetTrigger(anim_param_t_throw);
    }

    public void StartInteractingAnim ()
    {
        animator.SetBool(anim_param_b_interacting, true);
    }

    public void StopInteractingAnim()
    {
        animator.SetBool(anim_param_b_interacting, false);
    }

    public void PunchAnim()
    {
        animator.SetTrigger(anim_param_t_punch);
        StartCoroutine(ResetTriggerCR(ResetPunchTrigger, triggerResetTime));
    }

    public void ResetPunchTrigger()
    {
        animator.ResetTrigger(anim_param_t_punch);
    }

    public void GetHitAnim()
    {
        animator.SetTrigger(anim_param_t_beingHit);
        StartCoroutine(ResetTriggerCR(ResetPunchTrigger, triggerResetTime));
    }

    public void ResetBeingHitTrigger()
    {
        animator.ResetTrigger(anim_param_t_beingHit);
    }
}

public enum Skin
{
    Random = -1,
    Tiger = 0,
    Frog = 1,
    Lemur = 2
}
