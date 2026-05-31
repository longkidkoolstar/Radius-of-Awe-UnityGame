using UnityEngine;

/// <summary>
/// A high-quality 2D camera controller featuring smooth target tracking, horizontal look-ahead,
/// dynamic zoom-effects when active in the Wonder Zone, and a fading screenshake solver.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    public static CameraController2D Instance { get; private set; }

    [Header("Follow Settings")]
    [Tooltip("Target transform to follow. If unassigned, will search for a GameObject tagged 'Player'.")]
    [SerializeField] private Transform target;
    [Tooltip("How smoothly the camera catches up to the target. Lower = faster, higher = smoother.")]
    [SerializeField] private float smoothTime = 0.18f;
    [Tooltip("Camera offset relative to the target.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.8f, -10f);
    [Tooltip("Additional Y offset applied to the target tracking.")]
    [SerializeField] private float yOffset = 0f;

    public float YOffset
    {
        get => yOffset;
        set => yOffset = value;
    }



    [Header("Wonder Zoom Effects")]
    [Tooltip("Enable smooth zoom scaling when the Wonder Zone is activated.")]
    [SerializeField] private bool enableZoomEffects = true;
    [Tooltip("Orthographic camera size when in the gray mundane world.")]
    [SerializeField] private float defaultZoom = 7.0f;
    [Tooltip("Orthographic camera size when the Wonder Zone is active (reveals more of the world).")]
    [SerializeField] private float wonderActiveZoom = 7.8f;
    [Tooltip("Speed of the smooth orthographic camera zoom transition.")]
    [SerializeField] private float zoomSpeed = 3.5f;

    [Tooltip("Speed of decay back from impact landing zoom drops.")]
    [SerializeField] private float landingZoomRecoverySpeed = 6.2f;

    private Vector3 currentVelocity;
    private Camera cam;

    // Camera dynamic state
    private float currentZoomOffset = 0f;
    
    // Screenshake state variables
    private float shakeTimeRemaining;
    private float shakeMagnitude;
    private float totalShakeDuration;
    private Vector3 shakeOffset;

    // Reality distortion tracking
    private WonderRadiusController radiusController;
    private bool wasWonderActive = true;

    private void Awake()
    {
        // Singleton initialization
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Start()
    {
        // Auto-detect player if target is missing
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target != null)
        {
            radiusController = target.GetComponent<WonderRadiusController>();
            if (radiusController != null)
            {
                wasWonderActive = radiusController.IsActive;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // --- 1. Wonder Radius Toggle Detection ---
        if (radiusController != null)
        {
            bool isWonderActive = radiusController.IsActive;
            if (isWonderActive != wasWonderActive)
            {
                wasWonderActive = isWonderActive;
                // High-frequency reality shift ripple screenshake!
                TriggerShake(0.16f, isWonderActive ? 0.24f : 0.14f);
            }
        }

        // Decay landing zoom offset back to 0
        currentZoomOffset = Mathf.Lerp(currentZoomOffset, 0f, Time.deltaTime * landingZoomRecoverySpeed);

        // --- 2. Camera Zoom Scaling ---
        if (enableZoomEffects && cam != null && radiusController != null)
        {
            float targetZoom = radiusController.IsActive ? wonderActiveZoom : defaultZoom;
            
            // Subtract current zoom offset (makes the camera zoom in temporarily on impact)
            targetZoom -= currentZoomOffset;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        }

        // --- 3. Smooth Follow Calculation ---
        Vector3 targetPos = target.position + offset;
        targetPos.y += yOffset;

        // Dynamic smooth time vertical lag: increase smoothTime slightly during high vertical velocity
        float activeSmoothTime = smoothTime;
        var rb = target.GetComponent<Rigidbody2D>();
        if (rb != null && Mathf.Abs(rb.velocity.y) > 3.0f)
        {
            float vLag = Mathf.Min(Mathf.Abs(rb.velocity.y) * 0.025f, 0.5f);
            activeSmoothTime = smoothTime * (1f + vLag);
        }

        // Clamp camera Z depth to original Z
        Vector3 newPos = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, activeSmoothTime);
        newPos.z = offset.z;

        // --- 4. Screenshake Solver ---
        if (shakeTimeRemaining > 0)
        {
            shakeTimeRemaining -= Time.deltaTime;
            
            // Fade out the shake magnitude over its duration
            float damping = Mathf.Clamp01(shakeTimeRemaining / totalShakeDuration);
            float currentMag = shakeMagnitude * damping;
            
            // Random shake offsets in world space
            Vector2 randomShake = Random.insideUnitCircle * currentMag;
            shakeOffset = new Vector3(randomShake.x, randomShake.y, 0f);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // Apply calculated coordinates
        transform.position = newPos + shakeOffset;
    }

    /// <summary>
    /// Triggers a screen shake with the specified duration and intensity.
    /// </summary>
    /// <param name="duration">Duration of the shake in seconds.</param>
    /// <param name="magnitude">Starting displacement force of the camera.</param>
    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        totalShakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    /// <summary>
    /// Triggers an elastic camera zoom compression bump based on the landing impact velocity.
    /// </summary>
    public void TriggerLandingZoomBump(float fallVelocity)
    {
        // Clamp zoom impact to prevent complete viewport collapse
        float targetOffset = Mathf.Clamp(fallVelocity * 0.038f, 0.08f, 0.65f);
        currentZoomOffset = targetOffset;
    }
}
