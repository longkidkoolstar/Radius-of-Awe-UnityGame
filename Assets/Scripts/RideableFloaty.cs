using UnityEngine;

/// <summary>
/// Attached to a floating platform or crate to convert it into a rideable hoverboard!
/// Features:
///   1. Detects if the player is standing on top of it.
///   2. Tracks and applies displacement to the player in FixedUpdate to enable perfect, slide-free riding.
///   3. Floats upward smoothly at a constant speed inside the Wonder Zone, carrying the rider like an elevator!
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(WonderObject))]
public class RideableFloaty : MonoBehaviour
{
    [Header("Hover Physics")]
    [Tooltip("Upward speed of the hoverboard when inside the active Wonder Zone.")]
    [SerializeField] private float hoverSpeed = 2.4f;
    [Tooltip("Linear drag applied when active to stabilize horizontal floating.")]
    [SerializeField] private float activeDrag = 4f;

    private Rigidbody2D rb;
    private WonderObject wonderObject;
    private Vector2 previousPosition;

    // Rider tracking state
    private Rigidbody2D playerRb;
    private bool isPlayerRiding = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        wonderObject = GetComponent<WonderObject>();
        previousPosition = rb.position;

        // Ensure stable physics
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        bool inside = wonderObject.IsInWonderZone;

        // --- 1. Wonder Hover Lift ---
        if (inside)
        {
            // Lift smoothly upward, ignoring player's gravity drag
            rb.velocity = new Vector2(rb.velocity.x, hoverSpeed);
            rb.drag = activeDrag;
        }
        else
        {
            // Restore normal linear drag in the mundane world
            rb.drag = 0.8f;
        }

        // --- 2. Rider Displacement Matching (Zero Jitter/Slide) ---
        Vector2 platformDisplacement = rb.position - previousPosition;
        previousPosition = rb.position;

        if (isPlayerRiding && playerRb != null)
        {
            // Translate the player exactly by the platform's movement
            playerRb.position += platformDisplacement;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckRider(collision, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckRider(collision, true);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        CheckRider(collision, false);
    }

    /// <summary>
    /// Checks contact normals to determine if the player is standing directly on top of this object.
    /// </summary>
    private void CheckRider(Collision2D collision, bool isColliding)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var pRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (pRb != null)
            {
                if (isColliding)
                {
                    // Check contacts to make sure player is standing on the top surface
                    foreach (var contact in collision.contacts)
                    {
                        if (contact.normal.y < -0.55f) // Contact normal points downward relative to player
                        {
                            playerRb = pRb;
                            isPlayerRiding = true;
                            return;
                        }
                    }
                }
                else
                {
                    playerRb = null;
                    isPlayerRiding = false;
                }
            }
        }
    }
}
