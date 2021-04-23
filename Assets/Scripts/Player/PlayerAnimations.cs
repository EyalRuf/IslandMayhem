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
    private const string anim_param_t_interacting = "isInteracting";
    private const string anim_param_b_isHit = "isHit";
    private const string anim_param_t_jump = "jumpTrigger";
    private const string anim_param_t_throw = "throwTrigger";

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
        animator.SetBool(anim_param_b_isHit, cController.isBeingHit);
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
        animator.SetBool(anim_param_t_interacting, true);
    }

    public void StopInteractingAnim()
    {
        animator.SetBool(anim_param_t_interacting, false);
    }

}
