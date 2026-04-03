using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

public class ThirdPersonCharacterController : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Collider playerCollider;
    public LocalPlayerInput lpInput;
    public PlayerAnimations playerAnims;
    public PlayerItemInteractions playerItems;
    public PlayerBuffs pBuffs;

    [Header("Movement")]
    public bool isMoving;
    public float baseMoveSpeed;
    public bool isSprinting;
    public float sprintSpeedMultiplyer;
    public float moveSpeedWithItemMultiplyer;
    public float currMoveSpeed;
    private bool wasSprintingWhenJumped;
    private Vector3 playerLastPos;
    public Vector3 playerVelocity;

    [Header("Jumping")]
    public bool isGrounded;
    public Transform groundCheck;
    public float groundCheckDistance;
    public LayerMask groundCheckMask;
    public float jumpForce;
    public float jumpCD;
    private bool applyJump;
    private bool jumpCDFlag;
    public GameObject jumpParticles;
    public Transform jumpParticlesTransform;

    [Header("OtherCharacterActions")]
    public bool isDead;
    public bool isHoldingItem;
    public bool isAiming;
    public bool isLookingAround;
    public bool isAttacking;
    [SyncVar]
    public bool isCrippled;
    [SyncVar]
    public bool isKnockedDown = false;

    [Header("Audio")]
    public float playerVolume = 1f;
    public AudioClip[] walkClips;
    public AudioClip[] sprintClips;
    public AudioClip[] jumpClips;
    public AudioClip[] landingClips;
    public float footstepDistance = 1f;
    public Vector2 pitchRange;

    [SerializeField]
    private AudioSource source;
    private Vector3 lastFootstep;

    [Header("Misc")]
    public float groundedDrag;
    public PhysicsMaterial groundedMaterial;
    public PhysicsMaterial airBorneMaterial;


    private void Start()
    {
        lastFootstep = transform.position;
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundCheckMask);
            isSprinting = ShouldApplySprint();
            isLookingAround = lpInput.lookAroundInput;
            isHoldingItem = playerItems.heldItem != null;

            // Jump if not crip & on floor & not currently jumping & pressing jump input
            applyJump = !isKnockedDown && !isCrippled && isGrounded && lpInput.jumpInput && !jumpCDFlag;

            float moveSpeedBasedOnItem = (isHoldingItem ? baseMoveSpeed * moveSpeedWithItemMultiplyer : baseMoveSpeed) * (pBuffs.msBuffActive ? pBuffs.moveSpeedMultiplyer : 1);
            currMoveSpeed = isKnockedDown ? 0 : (isCrippled ? baseMoveSpeed / 2 : isSprinting ? moveSpeedBasedOnItem * sprintSpeedMultiplyer : moveSpeedBasedOnItem);
        }
    }

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            if (!isDead)
            {
                Vector3 vec = rb.position + rb.rotation * (Vector3.ClampMagnitude(lpInput.moveInput, 1f) * currMoveSpeed * Time.fixedDeltaTime);
                rb.MovePosition(vec);

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
            }

            //if we're grounded, apply drag horizontally.
            if (isGrounded)
            {
                Vector3 newVelocity = rb.linearVelocity * (1 - groundedDrag * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector3(newVelocity.x, rb.linearVelocity.y, newVelocity.z);

            } else if (rb.useGravity) // Whenever we're not grounded apply gravity forces
            {
                Vector3 vec2 = Vector3.up * Physics2D.gravity.y * rb.mass * Time.fixedDeltaTime;
                rb.linearVelocity += vec2;
            }

            // Set physics material
            playerCollider.material = isGrounded ? groundedMaterial : airBorneMaterial;

            playerVelocity = (rb.position - playerLastPos) / Time.fixedDeltaTime;
            var posOffset = rb.position - playerLastPos;
            var minMovementMagnitude = isCrippled ? 0.05f : 0.15f;

            // If traversed some non vertical ditance + horizontal movement is above a low point -> you're inputting movement + you actually moved a bit
            isMoving = new Vector3(posOffset.x, 0, posOffset.z).magnitude > minMovementMagnitude && 
                (Mathf.Abs(lpInput.moveInput.x) > 0.1f || Mathf.Abs(lpInput.moveInput.z) > 0.1f);

            // Footsteps sounds
            if (Vector3.Distance(lastFootstep, transform.position) > footstepDistance && isMoving && isGrounded)
            {
                lastFootstep = transform.position;
                CmdPlayMovementClip(isSprinting);
            }
        }

        playerVelocity = (rb.position - playerLastPos) / Time.fixedDeltaTime;
        playerLastPos = rb.position;
    }

    void Jump()
    {
        playerAnims.JumpAnim();
        Instantiate(jumpParticles, jumpParticlesTransform.position, Quaternion.Euler(-90, 0, 0));

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }

        rb.linearVelocity += Vector3.up * jumpForce * (pBuffs.jumpBuffActive ? pBuffs.jumpForceMultiplyer : 1);
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

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
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
        PlayMovementClip(isSprinting);
    }

    void PlayMovementClip(bool isSprinting)
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(
            isSprinting ? sprintClips[Random.Range(0, jumpClips.Length)] : walkClips[Random.Range(0, jumpClips.Length)],
            playerVolume);
    }

    [Command]
    public void CmdPlayLandClip()
    {
        RpcPlayLandClip();
    }

    [ClientRpc]
    private void RpcPlayLandClip()
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(landingClips[Random.Range(0, landingClips.Length)], playerVolume);
    }

    #endregion
}
