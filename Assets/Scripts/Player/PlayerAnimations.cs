using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
    private const string anim_param_b_grounded = "isGrounded";
    private const string anim_param_b_falling = "isFalling";
    private const string anim_param_b_walk = "isWalking";
    private const string anim_param_b_sprint = "isSprinting";
    private const string anim_param_b_holding_item = "isHoldingAnItem";
    private const string anim_param_t_jump = "jumpTrigger";
    private const string anim_param_t_throw = "throwTrigger";
    private const string anim_param_t_start_interacting = "startInteractionTrigger";
    private const string anim_param_t_stop_interacting = "stopInteractionTrigger";

    [Header("References")]
    public Animator animator;
    public ThirdPersonCharacterController cController;

    [Header("Etc")]
    bool isThrowing;

    // Update is called once per frame
    void Update ()
    {
        animator.SetBool(anim_param_b_grounded, cController.isGrounded);
        animator.SetBool(anim_param_b_falling, !cController.isGrounded && cController.playerVelocity.y < -0.25f);
        animator.SetBool(anim_param_b_walk, cController.isMoving);
        animator.SetBool(anim_param_b_sprint, cController.isSprinting);
        animator.SetBool(anim_param_b_holding_item, isThrowing || cController.playerItems.heldItem != null);
    }

    IEnumerator ResetTriggerCR(Action resetTriggerFunc, float resetTimer)
    {
        yield return new WaitForSeconds(resetTimer);
        resetTriggerFunc();
    }

    public void JumpAnim ()
    {
        animator.SetTrigger(anim_param_t_jump);
        StartCoroutine(ResetTriggerCR(ResetJumpTrigger, 0.1f));
    }

    public void ResetJumpTrigger ()
    {
        animator.ResetTrigger(anim_param_t_jump);
    }

    public void ThrowAnim ()
    {
        isThrowing = true;
        animator.SetTrigger(anim_param_t_throw);
        StartCoroutine(ResetTriggerCR(ResetThrowTrigger, 0.1f));
    }

    void ResetThrowTrigger ()
    {
        isThrowing = false;
        animator.ResetTrigger(anim_param_t_throw);
    }

    public void StartInteractingAnim ()
    {
        animator.SetTrigger(anim_param_t_start_interacting);
        StartCoroutine(ResetTriggerCR(ResetStartInteractionTrigger, 0.1f));
    }

    void ResetStartInteractionTrigger()
    {
        animator.ResetTrigger(anim_param_t_start_interacting);
    }

    public void StopInteractingAnim()
    {
        animator.SetTrigger(anim_param_t_stop_interacting);
        StartCoroutine(ResetTriggerCR(ResetStopInteractionTrigger, 0.1f));
    }

    void ResetStopInteractionTrigger()
    {
        animator.ResetTrigger(anim_param_t_stop_interacting);
    }
}
