using UnityEngine;

/// <summary>
/// A polished, physics-based 2D character controller with coyote time,
/// jump buffering, and smooth acceleration/deceleration.
/// Designed to feel responsive and modern.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 50f;
    [SerializeField] private float airAcceleration = 30f;
    [SerializeField] private float airDeceleration = 20f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [SerializeField] private float maxFallSpeed = 25f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.05f);
    [SerializeField] private Transform groundCheckPoint;

    // Internal state
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private float moveInput;
    private bool isGrounded;
    private float lastGroundedTime;
    private float lastJumpInputTime;
    private bool isJumping;
    private bool jumpInputReleased;
    private float defaultGravityScale;
    private bool facingRight = true;

    /// <summary>True when the character is on the ground.</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>Current horizontal velocity.</summary>
    public float HorizontalVelocity => rb != null ? rb.velocity.x : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        defaultGravityScale = rb.gravityScale;

        // Freeze rotation so the character doesn't topple over
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        // --- Gather Input ---
        moveInput = Input.GetAxisRaw("Horizontal");

        // Track timers
        lastGroundedTime -= Time.deltaTime;
        lastJumpInputTime -= Time.deltaTime;

        // --- Ground Check ---
        CheckGrounded();

        // --- Jump Input ---
        if (Input.GetButtonDown("Jump"))
        {
            lastJumpInputTime = jumpBufferTime;
        }

        if (Input.GetButtonUp("Jump"))
        {
            jumpInputReleased = true;
        }

        // --- Flip Sprite ---
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        // --- Horizontal Movement ---
        ApplyMovement();

        // --- Jumping ---
        HandleJump();

        // --- Enhanced Gravity ---
        ApplyGravityModifiers();

        // --- Clamp Fall Speed ---
        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    /// <summary>
    /// Uses a box overlap at the character's feet to check for ground.
    /// If no groundCheckPoint is assigned, falls back to the bottom of the capsule collider.
    /// </summary>
    private void CheckGrounded()
    {
        Vector2 checkPos;
        if (groundCheckPoint != null)
        {
            checkPos = groundCheckPoint.position;
        }
        else
        {
            // Fallback: bottom of the capsule collider
            checkPos = (Vector2)transform.position + capsuleCollider.offset + Vector2.down * (capsuleCollider.size.y * 0.5f);
        }

        isGrounded = Physics2D.OverlapBox(
            checkPos,
            groundCheckSize,
            0f,
            groundLayer
        );

        if (isGrounded)
        {
            lastGroundedTime = coyoteTime;
            isJumping = false;
        }
    }

    /// <summary>
    /// Applies smooth acceleration and deceleration to horizontal movement.
    /// Uses different rates for grounded vs. airborne.
    /// </summary>
    private void ApplyMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;

        float accelRate;
        if (isGrounded)
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        }
        else
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? airAcceleration : airDeceleration;
        }

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.AddForce(Vector2.right * movement, ForceMode2D.Force);
    }

    /// <summary>
    /// Handles jump initiation with coyote time and jump buffering.
    /// Also handles variable jump height via early release.
    /// </summary>
    private void HandleJump()
    {
        // Can jump if: jump was buffered AND (grounded OR within coyote time)
        if (lastJumpInputTime > 0 && lastGroundedTime > 0 && !isJumping)
        {
            // Perform jump
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isJumping = true;
            jumpInputReleased = false;

            // Reset timers to prevent double-jumps
            lastJumpInputTime = 0;
            lastGroundedTime = 0;
        }

        // Variable jump height: cut velocity when button is released early
        if (jumpInputReleased && isJumping && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            jumpInputReleased = false;
        }
    }

    /// <summary>
    /// Applies heavier gravity when falling for snappier, more satisfying arcs.
    /// </summary>
    private void ApplyGravityModifiers()
    {
        if (rb.velocity.y < 0)
        {
            // Falling — apply enhanced gravity
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    /// <summary>
    /// Flips the character's local scale to face the direction of movement.
    /// </summary>
    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    /// <summary>
    /// Draws the ground check gizmo in the editor for easy tuning.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector2 checkPos;
        if (groundCheckPoint != null)
        {
            checkPos = groundCheckPoint.position;
        }
        else if (capsuleCollider != null)
        {
            checkPos = (Vector2)transform.position + capsuleCollider.offset + Vector2.down * (capsuleCollider.size.y * 0.5f);
        }
        else
        {
            checkPos = (Vector2)transform.position + Vector2.down * 0.5f;
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
}