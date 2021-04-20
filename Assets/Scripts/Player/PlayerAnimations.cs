using Mirror;
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

    [Header("References")]
    public Animator animator;
    public NetworkAnimator networkAnimator;
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

    public void JumpAnim ()
    {
        networkAnimator.SetTrigger(anim_param_t_jump);
    }

    public void ResetJumpTrigger ()
    {
        networkAnimator.ResetTrigger(anim_param_t_jump);
    }

    public void ThrowAnim ()
    {
        isThrowing = true;
        networkAnimator.SetTrigger(anim_param_t_throw);
        StartCoroutine(ResetThrowTrigger());
    }

    IEnumerator ResetThrowTrigger ()
    {
        yield return new WaitForSeconds(0.1f);
        networkAnimator.ResetTrigger(anim_param_t_throw);
        isThrowing = false;
    }
}
