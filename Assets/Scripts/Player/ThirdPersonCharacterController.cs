using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThirdPersonCharacterController : MonoBehaviour
{
    private Rigidbody rb;
    private LocalPlayerInput lpInput;

    [Header("Movement")]
    public bool isMoving;
    public float baseMoveSpeed;
    private float currMoveSpeed;
    public bool isSprinting;
    public float sprintSpeedMultiplyer;
    private bool wasSprintingWhenJumped;
    private Vector3 playerLastPos;
    public Vector3 playerVelocity;

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

    [Header("Misc")]
    public float groundedDrag;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lpInput = GetComponent<LocalPlayerInput>();
    }

    void Update()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundCheckMask);

        isSprinting = ShouldApplySprint();
        
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
            rb.velocity += Vector3.up * jumpForce;
            applyJump = false;
            jumpCDFlag = true;
            StartCoroutine(JumpCDApplier());
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

        playerVelocity = (rb.position - playerLastPos) / Time.deltaTime;
        playerLastPos = rb.position;
        isMoving = playerVelocity != Vector3.zero;
    }

    IEnumerator JumpCDApplier ()
    {
        yield return new WaitForSeconds(jumpCD);
        jumpCDFlag = false;
    }

    bool ShouldApplySprint()
    {
        if (!isGrounded)
        {
            if (!wasSprintingWhenJumped)
            {
                return false;
            } else
            {
                wasSprintingWhenJumped = lpInput.sprintInput;
            }
        }

        bool forwardMovementInput = lpInput.moveInput.z > 0;

        // Going Forward && not aiming && sprinting
        return forwardMovementInput && !lpInput.aimInput && lpInput.sprintInput;
    }
}
