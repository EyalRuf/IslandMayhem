using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThirdPersonCharacterController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public LocalPlayerInput lpInput;
    public Collider playerCollider;
    public PlayerAnimations playerAnims;

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
    public bool isAiming;
    public bool isLookingAround;

    [Header("Misc")]
    public float groundedDrag;
    public PhysicMaterial groundedMaterial;
    public PhysicMaterial airBorneMaterial;

    void Update()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundCheckMask);
        isSprinting = ShouldApplySprint();
        isLookingAround = lpInput.lookAroundInput;

        if (isGrounded && lpInput.jumpInput && !jumpCDFlag)
        {
            applyJump = true;
            wasSprintingWhenJumped = isSprinting;
        }

        currMoveSpeed = isSprinting ? baseMoveSpeed * sprintSpeedMultiplyer : baseMoveSpeed;
    }

    private void FixedUpdate()
    {
        rb.transform.Translate((lpInput.moveInput * currMoveSpeed * Time.fixedDeltaTime), Space.Self);

        if (applyJump)
        {
            Jump();
        }

        // Applying additional falling physics
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics2D.gravity.y * fallMultiplier * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !lpInput.jumpInput)
        {
            rb.velocity += Vector3.up * Physics2D.gravity.y * lowJumpMultiplier * Time.fixedDeltaTime;
        }

        //if we're grounded, apply drag horizontally.
        if (isGrounded)
        {
            Vector3 newVelocity = rb.velocity * (1 - groundedDrag * Time.fixedDeltaTime);
            rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, newVelocity.z);
        }

        //set physics material
        playerCollider.material = isGrounded ? groundedMaterial : airBorneMaterial;

        playerVelocity = (rb.position - playerLastPos) / Time.deltaTime;
        isMoving = (rb.position - playerLastPos).magnitude > 0.2f;
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
        playerAnims.ResetJumpTrigger();
    }

    bool ShouldApplySprint()
    {
        if (isGrounded)
        {
            wasSprintingWhenJumped = lpInput.sprintInput;
        }

        bool forwardMovementInput = lpInput.moveInput.z > 0;

        // Going Forward && not aiming && sprinting
        return wasSprintingWhenJumped && forwardMovementInput && !isAiming;
    }
}
