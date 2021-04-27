using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThirdPersonCharacterController : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Collider playerCollider;
    public LocalPlayerInput lpInput;
    public PlayerAnimations playerAnims;
    public PlayerItemInteractions playerItems;

    [Header("Movement")]
    public bool isMoving;
    public float baseMoveSpeed;
    public bool isSprinting;
    public float sprintSpeedMultiplyer;
    private float currMoveSpeed;
    private bool wasSprintingWhenJumped;
    private Vector3 playerLastPos;
    public Vector3 playerVelocity { get; private set; }

    [Header("Jumping")]
    public bool isGrounded;
    public Transform groundCheck;
    public float groundCheckDistance;
    public LayerMask groundCheckMask;
    public float jumpForce;
    public float fallMultiplier;
    public float lowJumpMultiplier;
    public float jumpCD;
    private bool applyJump;
    private bool jumpCDFlag;

    [Header("OtherCharacterActions")]
    public bool isHoldingItem;
    public bool isAiming;
    public bool isLookingAround;
    public bool isAttacking;
    public bool isBeingHit;
    public bool isCrippled;

    [Header("Misc")]
    public float groundedDrag;
    public PhysicMaterial groundedMaterial;
    public PhysicMaterial airBorneMaterial;

    void Update()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundCheckMask);
        isSprinting = ShouldApplySprint();
        isLookingAround = lpInput.lookAroundInput;
        isHoldingItem = playerItems.heldItem != null;

        if (isGrounded && lpInput.jumpInput && !jumpCDFlag)
        {
            applyJump = true;
            wasSprintingWhenJumped = isSprinting;
        }

        currMoveSpeed = isCrippled ? baseMoveSpeed / 2 : isSprinting ? baseMoveSpeed * sprintSpeedMultiplyer : baseMoveSpeed;
    }

    private void FixedUpdate()
    {
        //move
        rb.MovePosition(rb.position + rb.rotation * (lpInput.moveInput * currMoveSpeed * Time.fixedDeltaTime));

        if (!isCrippled && applyJump)
        {
            Jump();
        }

        // Applying additional falling physics
        //if (rb.velocity.y < 0)
        //{
        //    rb.velocity += Vector3.up * Physics2D.gravity.y * rb.mass * fallMultiplier * Time.fixedDeltaTime;
        //}
        //else if (rb.velocity.y > 0 && !lpInput.jumpInput)
        //{
        //    rb.velocity += Vector3.up * Physics2D.gravity.y * rb.mass * lowJumpMultiplier * Time.fixedDeltaTime;
        //} else if (rb.velocity.y > 0)
        //{
        //    rb.velocity += Vector3.up * Physics2D.gravity.y * rb.mass * Time.fixedDeltaTime;
        //}

        //if we're grounded, apply drag horizontally.
        if (isGrounded)
        {
            Vector3 newVelocity = rb.velocity * (1 - groundedDrag * Time.fixedDeltaTime);
            rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, newVelocity.z);
        } else if(rb.useGravity) // Whenever we're not grounded apply gravity forces
        {
            rb.velocity += Vector3.up * Physics2D.gravity.y * rb.mass * Time.fixedDeltaTime;
        }

        //set physics material
        playerCollider.material = isGrounded ? groundedMaterial : airBorneMaterial;

        playerVelocity = (rb.position - playerLastPos) / Time.fixedDeltaTime;
        var posOffset = rb.position - playerLastPos;
        isMoving = new Vector3(posOffset.x, 0, posOffset.z).magnitude > 0.2f;
        playerLastPos = rb.position;
    }

    void Jump()
    {
        playerAnims.JumpAnim();
        rb.velocity += Vector3.up * jumpForce;
        applyJump = false;
        jumpCDFlag = true;
        StartCoroutine(JumpCDApplier());
    }

    IEnumerator JumpCDApplier ()
    {
        yield return new WaitForSeconds(jumpCD);
        jumpCDFlag = false;
    }

    bool ShouldApplySprint()
    {
        if (isGrounded)
        {
            wasSprintingWhenJumped = lpInput.sprintInput;
        }

        bool forwardMovementInput = lpInput.moveInput.z > 0;

        // Going Forward && not aiming && sprinting
        return wasSprintingWhenJumped && forwardMovementInput && !isAiming && !isBeingHit && !isCrippled;
    }
}
