using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThirdPersonCharacterController : MonoBehaviour
{
    private Rigidbody rb;
    private LocalPlayerInput lpInput;
    
    [Header("Movement")]
    public float speed;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lpInput = GetComponent<LocalPlayerInput>();
    }

    void Update()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundCheckMask);
        if (isGrounded && lpInput.jumpInput && !jumpCDFlag)
        {
            applyJump = true;
        }
    }

    private void FixedUpdate()
    {
        rb.transform.Translate((lpInput.moveInput * speed * Time.deltaTime), Space.Self);

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
            rb.velocity += Vector3.up * Physics2D.gravity.y * fallMultiplier * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !lpInput.jumpInput)
        {
            rb.velocity += Vector3.up * Physics2D.gravity.y * lowJumpMultiplier * Time.deltaTime;
        }
    }

    IEnumerator JumpCDApplier ()
    {
        yield return new WaitForSeconds(jumpCD);
        jumpCDFlag = false;
    }
}
