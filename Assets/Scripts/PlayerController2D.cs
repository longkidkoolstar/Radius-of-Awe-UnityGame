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
    [SerializeField] private float acceleration = 95f;
    [SerializeField] private float deceleration = 85f;
    [SerializeField] private float airAcceleration = 55f;
    [SerializeField] private float airDeceleration = 45f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 12.2f;
    [SerializeField] private float gravityScale = 3.5f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float fallGravityMultiplier = 2.2f;
    [SerializeField] private float maxFallSpeed = 25f;
    [Tooltip("Secondary jump key for standard gamepads (Xbox A, PS Cross, etc.).")]
    [SerializeField] private KeyCode controllerJumpKey = KeyCode.JoystickButton0;


    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.05f);
    [SerializeField] private Transform groundCheckPoint;

    [Header("Visual Juice")]
    [Tooltip("Reference to the player's graphics child Transform used for squash & stretch.")]
    [SerializeField] private Transform graphicsTransform;
    [Tooltip("How quickly squash & stretch shapes return to normal.")]
    [SerializeField] private float squashStretchSpeed = 7.5f;
    [Tooltip("How much the player leans forward when running.")]
    [SerializeField] private float runLeanMultiplier = 1.4f;
    [Tooltip("Amount of vertical step-bobbing when running.")]
    [SerializeField] private float runBobAmount = 0.05f;
    [Tooltip("Speed of running step bobbing.")]
    [SerializeField] private float runBobSpeed = 18f;

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
    private PlayerJuiceEffects juiceEffects;
    private float lastNonGroundedVelocityY;

    // Visual juice state
    private Vector3 currentScale = new Vector3(0.5f, 0.5f, 1f);
    private Vector3 defaultScale = new Vector3(0.5f, 0.5f, 1f);
    private float lastBobSin = 0f;

    /// <summary>True when the character is on the ground.</summary>
    public bool IsGrounded => isGrounded;

    /// <summary>Current horizontal velocity.</summary>
    public float HorizontalVelocity => rb != null ? rb.velocity.x : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        
        // Enforce the tight gravity settings
        rb.gravityScale = gravityScale;
        defaultGravityScale = gravityScale;

        // Freeze rotation so the character doesn't topple over
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Auto-detect or attach PlayerJuiceEffects
        juiceEffects = GetComponent<PlayerJuiceEffects>();
        if (juiceEffects == null)
        {
            juiceEffects = gameObject.AddComponent<PlayerJuiceEffects>();
        }

        // Auto-find child named "Graphics" if not manually assigned
        if (graphicsTransform == null)
        {
            graphicsTransform = transform.Find("Graphics");
        }

        if (graphicsTransform != null)
        {
            defaultScale = graphicsTransform.localScale;
            currentScale = defaultScale;
        }
    }

    private void Update()
    {
        // --- Restart Level Input ---
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            Time.timeScale = 1.0f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // --- Gather Input ---
        moveInput = Input.GetAxisRaw("Horizontal");

        // Track timers
        lastGroundedTime -= Time.deltaTime;
        lastJumpInputTime -= Time.deltaTime;

        // --- Ground Check ---
        CheckGrounded();

        // --- Jump Input ---
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(controllerJumpKey))
        {
            lastJumpInputTime = jumpBufferTime;
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

        // --- Update visual squash and stretch ---
        UpdateScaleJuice();
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

        // Cache vertical velocity right before impact so we know the force of the land
        if (!isGrounded)
        {
            lastNonGroundedVelocityY = rb.velocity.y;
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

        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(
            checkPos,
            groundCheckSize,
            0f,
            groundLayer
        );

        if (isGrounded)
        {
            lastGroundedTime = coyoteTime;
            
            // If just landed!
            if (!wasGrounded)
            {
                OnLand();
            }
            
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

        float accelRate;
        if (isGrounded)
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        }
        else
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? airAcceleration : airDeceleration;
        }

        // Celeste-style Mathf.MoveTowards horizontal velocity adjustment
        float newX = Mathf.MoveTowards(rb.velocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.velocity = new Vector2(newX, rb.velocity.y);
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

            // Reset timers to prevent double-jumps
            lastJumpInputTime = 0;
            lastGroundedTime = 0;

            // Stretch the player capsule vertically!
            if (graphicsTransform != null)
            {
                currentScale = new Vector3(defaultScale.x * 0.65f, defaultScale.y * 1.35f, defaultScale.z);
            }

            // Emit visual jump smoke/sparks burst
            if (juiceEffects != null)
            {
                juiceEffects.EmitJumpBurst();
            }

            // Play procedural jump sound
            AudioManager.PlayJump();
        }

        // Variable jump height: cut velocity when button is released early (continuous check)
        if (isJumping && rb.velocity.y > 0 && !Input.GetButton("Jump") && !Input.GetKey(controllerJumpKey))
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            isJumping = false; // Prevent multiple cuts in the same jump
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
    /// Triggered when the player changes from airborne to grounded state.
    /// Applies a horizontal squish to the visuals based on impact velocity.
    /// </summary>
    private void OnLand()
    {
        float fallVelocity = Mathf.Abs(lastNonGroundedVelocityY);
        
        // Only squish if we were falling with some velocity
        if (fallVelocity > 1f)
        {
            // Softened: Calculate a squish factor (max out at 22% of original size)
            float squishFactor = Mathf.Clamp(fallVelocity * 0.012f, 0.04f, 0.22f);
            currentScale = new Vector3(defaultScale.x * (1f + squishFactor), defaultScale.y * (1f - squishFactor), defaultScale.z);
        }

        // Emit landing dust/spark ring
        if (juiceEffects != null && fallVelocity > 1.5f)
        {
            juiceEffects.EmitLandBurst(fallVelocity);
        }

        // Play landing impact sound
        if (fallVelocity > 1.2f)
        {
            AudioManager.PlayLand(fallVelocity);
        }

        // Trigger camera landing zoom bump on heavy falls
        if (fallVelocity > 1.5f && CameraController2D.Instance != null)
        {
            CameraController2D.Instance.TriggerLandingZoomBump(fallVelocity);
        }

        // Trigger camera landing shake ONLY if it was a high, heavy fall (threshold raised from 12 to 17)
        if (fallVelocity > 17f && CameraController2D.Instance != null)
        {
            CameraController2D.Instance.TriggerShake(0.18f, fallVelocity * 0.022f);
        }
    }

    /// <summary>
    /// Smoothly lerps the visual scale and rotation back to default or updates mid-air stretching/running tilt.
    /// </summary>
    private void UpdateScaleJuice()
    {
        if (graphicsTransform == null) return;

        // --- 1. Running Tilt / Lean ---
        float targetAngle = 0f;
        if (rb != null)
        {
            // Lean forward based on horizontal velocity (direction of travel)
            // Compensate for local scale X mirroring when flipped left
            targetAngle = -rb.velocity.x * runLeanMultiplier * (facingRight ? 1f : -1f);
        }
        // Smoothly interpolate tilt rotation
        graphicsTransform.localRotation = Quaternion.Lerp(graphicsTransform.localRotation, Quaternion.Euler(0f, 0f, targetAngle), Time.deltaTime * 10f);

        // --- 2. Squash and Stretch & Running Step-Bobbing ---
        if (!isGrounded)
        {
            // Mid-air visual stretching based on current vertical velocity
            float velY = rb.velocity.y;
            // Stretch when rising, compress/neutral when falling
            float stretchFactor = Mathf.Clamp(velY * 0.008f, -0.1f, 0.18f);
            
            Vector3 airTargetScale = new Vector3(
                defaultScale.x * (1f - stretchFactor),
                defaultScale.y * (1f + stretchFactor),
                defaultScale.z
            );
            
            // Lerp slower in the air for floatier feels
            currentScale = Vector3.Lerp(currentScale, airTargetScale, Time.deltaTime * squashStretchSpeed * 0.5f);
        }
        else
        {
            // Grounded running step-bob (breathe steps dynamically based on speed)
            float bob = 0f;
            float hVel = Mathf.Abs(rb.velocity.x);
            if (hVel > 0.15f)
            {
                // Bob dynamically based on speed
                float currentBobSin = Mathf.Sin(Time.time * runBobSpeed);
                bob = currentBobSin * runBobAmount * (hVel / moveSpeed);

                // Trigger footstep sound on step bob zero-crossings
                if (isGrounded && hVel > 1.6f)
                {
                    if ((lastBobSin < 0f && currentBobSin >= 0f) || (lastBobSin > 0f && currentBobSin <= 0f))
                    {
                        AudioManager.PlayFootstep(hVel / moveSpeed);
                    }
                }
                lastBobSin = currentBobSin;
            }

            Vector3 groundedTargetScale = new Vector3(
                defaultScale.x * (1f + bob * 0.5f), // Stretch slightly wide when squished down
                defaultScale.y * (1f - bob),        // Bob height
                defaultScale.z
            );

            // Grounded: lerp back using rubbery spring speed
            currentScale = Vector3.Lerp(currentScale, groundedTargetScale, Time.deltaTime * squashStretchSpeed);
        }

        graphicsTransform.localScale = currentScale;
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