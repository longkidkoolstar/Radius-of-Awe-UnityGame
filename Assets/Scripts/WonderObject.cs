using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach this to any GameObject that should change behavior when inside the Wonder Radius.
/// Highly modular: configure physics swaps, collider toggles, sprite swaps,
/// and custom UnityEvents entirely from the Inspector.
/// </summary>
public class WonderObject : MonoBehaviour
{
    [System.Serializable]
    public class PhysicsOverride
    {
        [Tooltip("Override gravity scale when inside Wonder Zone.")]
        public bool overrideGravity = true;
        public float wonderGravityScale = -0.5f;

        [Tooltip("Override mass when inside Wonder Zone.")]
        public bool overrideMass = false;
        public float wonderMass = 0.2f;

        [Tooltip("Override linear drag when inside Wonder Zone.")]
        public bool overrideDrag = true;
        public float wonderDrag = 3f;

        [Tooltip("Override angular drag when inside Wonder Zone.")]
        public bool overrideAngularDrag = false;
        public float wonderAngularDrag = 0.5f;
    }

    [Header("Physics Swapping")]
    [Tooltip("Enable to swap Rigidbody2D properties when inside the Wonder Zone.")]
    [SerializeField] private bool swapPhysics = true;
    [SerializeField] private PhysicsOverride wonderPhysics = new PhysicsOverride();

    [Header("Collider Management")]
    [Tooltip("Collider to DISABLE when inside Wonder Zone (e.g., a solid wall becomes passable).")]
    [SerializeField] private Collider2D mundaneCollider;
    [Tooltip("Collider to ENABLE when inside Wonder Zone (e.g., bouncy terrain appears).")]
    [SerializeField] private Collider2D wonderCollider;

    [Header("Visual Swap")]
    [Tooltip("SpriteRenderer to show in Mundane world (hidden in Wonder).")]
    [SerializeField] private SpriteRenderer mundaneSprite;
    [Tooltip("SpriteRenderer to show in Wonder world (hidden in Mundane).")]
    [SerializeField] private SpriteRenderer wonderSprite;

    [Header("Events")]
    [SerializeField] private UnityEvent onEnterWonder;
    [SerializeField] private UnityEvent onExitWonder;

    [Header("Detection Settings")]
    [Tooltip("How often (in seconds) to check Wonder Zone overlap. Lower = more responsive, higher = more performant.")]
    [SerializeField] private float checkInterval = 0.05f;
    [Tooltip("Offset from the transform position for the detection point.")]
    [SerializeField] private Vector2 detectionOffset = Vector2.zero;

    // Cached mundane (original) physics values
    private float mundaneGravityScale;
    private float mundaneMass;
    private float mundaneDrag;
    private float mundaneAngularDrag;

    // State tracking
    private Rigidbody2D rb;
    private bool isInWonderZone = false;
    private float checkTimer;

    /// <summary>True if this object is currently inside the Wonder Zone.</summary>
    public bool IsInWonderZone => isInWonderZone;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Cache the original mundane physics values
        if (rb != null)
        {
            mundaneGravityScale = rb.gravityScale;
            mundaneMass = rb.mass;
            mundaneDrag = rb.drag;
            mundaneAngularDrag = rb.angularDrag;
        }

        // Ensure wonder-only visuals and colliders start disabled
        if (wonderCollider != null) wonderCollider.enabled = false;
        if (wonderSprite != null) wonderSprite.enabled = false;
    }

    private void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            EvaluateWonderZone();
        }
    }

    /// <summary>
    /// Checks whether this object's detection point is inside the Wonder Radius
    /// and triggers state transitions accordingly.
    /// </summary>
    private void EvaluateWonderZone()
    {
        Vector3 detectionPoint = transform.position + (Vector3)detectionOffset;
        bool insideNow = WonderRadiusController.IsInsideWonderZone(detectionPoint);

        if (insideNow && !isInWonderZone)
        {
            EnterWonder();
        }
        else if (!insideNow && isInWonderZone)
        {
            ExitWonder();
        }
    }

    /// <summary>
    /// Called once when the object enters the Wonder Zone.
    /// Applies wonder physics, enables wonder colliders/sprites, fires events.
    /// </summary>
    private void EnterWonder()
    {
        isInWonderZone = true;

        // --- Physics Swap ---
        if (swapPhysics && rb != null)
        {
            if (wonderPhysics.overrideGravity)
                rb.gravityScale = wonderPhysics.wonderGravityScale;
            if (wonderPhysics.overrideMass)
                rb.mass = wonderPhysics.wonderMass;
            if (wonderPhysics.overrideDrag)
                rb.drag = wonderPhysics.wonderDrag;
            if (wonderPhysics.overrideAngularDrag)
                rb.angularDrag = wonderPhysics.wonderAngularDrag;
        }

        // --- Collider Swap ---
        if (mundaneCollider != null) mundaneCollider.enabled = false;
        if (wonderCollider != null) wonderCollider.enabled = true;

        // --- Visual Swap ---
        if (mundaneSprite != null) mundaneSprite.enabled = false;
        if (wonderSprite != null) wonderSprite.enabled = true;

        // --- Fire Event ---
        onEnterWonder?.Invoke();
    }

    /// <summary>
    /// Called once when the object exits the Wonder Zone.
    /// Restores mundane physics, disables wonder colliders/sprites, fires events.
    /// </summary>
    private void ExitWonder()
    {
        isInWonderZone = false;

        // --- Restore Physics ---
        if (swapPhysics && rb != null)
        {
            if (wonderPhysics.overrideGravity)
                rb.gravityScale = mundaneGravityScale;
            if (wonderPhysics.overrideMass)
                rb.mass = mundaneMass;
            if (wonderPhysics.overrideDrag)
                rb.drag = mundaneDrag;
            if (wonderPhysics.overrideAngularDrag)
                rb.angularDrag = mundaneAngularDrag;
        }

        // --- Collider Swap ---
        if (mundaneCollider != null) mundaneCollider.enabled = true;
        if (wonderCollider != null) wonderCollider.enabled = false;

        // --- Visual Swap ---
        if (mundaneSprite != null) mundaneSprite.enabled = true;
        if (wonderSprite != null) wonderSprite.enabled = false;

        // --- Fire Event ---
        onExitWonder?.Invoke();
    }

    /// <summary>
    /// Visualizes the detection point in the Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 point = transform.position + (Vector3)detectionOffset;
        Gizmos.color = isInWonderZone ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(point, 0.15f);
    }
}