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
    public float moveSpeedWithItemMultiplyer;
    public float currMoveSpeed;
    private bool wasSprintingWhenJumped;
    private Vector3 playerLastPos;
    public Vector3 playerVelocity { get; private set; }

    [Header("Jumping")]
    public bool isGrounded;
    public Transform groundCheck;
    public float groundCheckDistance;
    public LayerMask groundCheckMask;
    public float jumpForce;
    public float jumpCD;
    private bool applyJump;
    private bool jumpCDFlag;

    [Header("OtherCharacterActions")]
    public bool isHoldingItem;
    public bool isAiming;
    public bool isLookingAround;
    public bool isAttacking;
    [SyncVar]
    public bool isCrippled;

    [Header("Audio")]
    public float playerVolume = 1f;
    public AudioClip[] walkClips;
    public AudioClip[] sprintClips;
    public AudioClip[] jumpClips;
    public float footstepDistance = 1f;

    [SerializeField]
    private AudioSource source;
    private Vector3 lastFootstep;

    [Header("Misc")]
    public float groundedDrag;
    public PhysicMaterial groundedMaterial;
    public PhysicMaterial airBorneMaterial;

    private void Start()
    {
        lastFootstep = transform.position;
    }

    void Update()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundCheckMask);
        isSprinting = ShouldApplySprint();
        isLookingAround = lpInput.lookAroundInput;
        isHoldingItem = playerItems.heldItem != null;

        // Jump if not crip & on floor & not currently jumping & pressing jump input
        applyJump = !isCrippled && isGrounded && lpInput.jumpInput && !jumpCDFlag;

        float moveSpeedBasedOnItem = isHoldingItem ? baseMoveSpeed * moveSpeedWithItemMultiplyer : baseMoveSpeed;
        currMoveSpeed = isCrippled ? baseMoveSpeed / 2 : isSprinting ? moveSpeedBasedOnItem * sprintSpeedMultiplyer : moveSpeedBasedOnItem;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + rb.rotation * (Vector3.ClampMagnitude(lpInput.moveInput, 1f) * currMoveSpeed * Time.fixedDeltaTime));

        if (applyJump)
        {
            wasSprintingWhenJumped = isSprinting;
            applyJump = false;
            jumpCDFlag = true;
            CmdJump(netId);
            StartCoroutine(JumpCDApplier());

            //jump sound
            CmdPlayJumpClip();
        }

        //if we're grounded, apply drag horizontally.
        if (isGrounded)
        {
            Vector3 newVelocity = rb.velocity * (1 - groundedDrag * Time.fixedDeltaTime);

            if (newVelocity.x == newVelocity.x && newVelocity.z == newVelocity.z)
                rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, newVelocity.z);
        } else if (rb.useGravity) // Whenever we're not grounded apply gravity forces
        {
            Vector3 vec = Vector3.up * Physics2D.gravity.y * rb.mass * Time.fixedDeltaTime;
            if (vec.x == vec.x && vec.y == vec.y && vec.z == vec.z)
                rb.velocity += vec;
        }

        // Set physics material
        playerCollider.material = isGrounded ? groundedMaterial : airBorneMaterial;

        playerVelocity = (rb.position - playerLastPos) / Time.fixedDeltaTime;
        var posOffset = rb.position - playerLastPos;
        playerLastPos = rb.position;
        var minMovementMagnitude = isCrippled ? 0.05f : 0.15f;

        // If traversed some non vertical ditance + horizontal movement is above a low point -> you're inputting movement + you actually moved a bit
        isMoving = new Vector3(posOffset.x, 0, posOffset.z).magnitude > minMovementMagnitude && 
            (Mathf.Abs(lpInput.moveInput.x) > 0.1f || Mathf.Abs(lpInput.moveInput.z) > 0.1f);

        //footsteps sounds
        if (Vector3.Distance(lastFootstep, transform.position) > footstepDistance && isMoving && isGrounded)
        {
            lastFootstep = transform.position;
            CmdPlayMovementClip(isSprinting);
        }
    }

    void Jump()
    {
        playerAnims.JumpAnim();

        if (rb.velocity.y < 0)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }

        rb.velocity += Vector3.up * jumpForce;
    }

    [ClientRpc]
    public void RpcJump()
    {
        Jump();
    }

    [Command]
    void CmdJump (uint playerNID)
    {
        NetworkIdentity player = CustomNetworkManager.GetPlayerByNetId(playerNID);
        ThirdPersonCharacterController pp = player.GetComponent<ThirdPersonCharacterController>();

        pp.RpcJump();
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

        return wasSprintingWhenJumped && forwardMovementInput && !isAiming && !isCrippled;
    }

    #region Audio

    [Command]
    private void CmdPlayJumpClip()
    {
        RpcPlayJumpClip();
    }

    [ClientRpc]
    private void RpcPlayJumpClip()
    {
        if(source == null)
        {
            source = GetComponent<AudioSource>();
        }

        source.PlayOneShot(jumpClips[Random.Range(0, jumpClips.Length)], playerVolume);
    }

    [Command]
    public void CmdPlayMovementClip(bool isSprinting)
    {
        RpcPlayMovementClip(isSprinting);
    }

    [ClientRpc]
    private void RpcPlayMovementClip(bool isSprinting)
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        source.PlayOneShot(
            isSprinting ? sprintClips[Random.Range(0, jumpClips.Length)] : walkClips[Random.Range(0, jumpClips.Length)], 
            playerVolume);
    }

    #endregion
}
