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

    [Header("Look-Ahead (Offset in Movement Direction)")]
    [Tooltip("How far ahead the camera looks based on target's velocity.")]
    [SerializeField] private float lookAheadDistance = 1.8f;
    [Tooltip("How fast the camera shifts horizontally to look ahead.")]
    [SerializeField] private float lookAheadSpeed = 2.5f;

    [Header("Wonder Zoom Effects")]
    [Tooltip("Enable smooth zoom scaling when the Wonder Zone is activated.")]
    [SerializeField] private bool enableZoomEffects = true;
    [Tooltip("Orthographic camera size when in the gray mundane world.")]
    [SerializeField] private float defaultZoom = 7.0f;
    [Tooltip("Orthographic camera size when the Wonder Zone is active (reveals more of the world).")]
    [SerializeField] private float wonderActiveZoom = 7.8f;
    [Tooltip("Speed of the smooth orthographic camera zoom transition.")]
    [SerializeField] private float zoomSpeed = 3.5f;

    private Vector3 currentVelocity;
    private Camera cam;
    
    // Screenshake state variables
    private float shakeTimeRemaining;
    private float shakeMagnitude;
    private float totalShakeDuration;
    private Vector3 shakeOffset;

    // Reality distortion tracking
    private WonderRadiusController radiusController;
    private bool wasWonderActive = true;
    private float currentLookAheadX = 0f;

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

        // --- 2. Camera Zoom Scaling ---
        if (enableZoomEffects && cam != null && radiusController != null)
        {
            float targetZoom = radiusController.IsActive ? wonderActiveZoom : defaultZoom;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        }

        // --- 3. Look-Ahead Offset (Horizontal Shift) ---
        float targetLookAhead = 0f;
        var rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float hVel = rb.velocity.x;
            if (Mathf.Abs(hVel) > 0.15f)
            {
                targetLookAhead = Mathf.Sign(hVel) * lookAheadDistance;
            }
        }
        
        // Smoothly interpolate the look-ahead value to prevent camera jerking
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAhead, Time.deltaTime * lookAheadSpeed);

        // --- 4. Smooth Follow Calculation ---
        Vector3 targetPos = target.position + offset;
        targetPos.x += currentLookAheadX;

        // Clamp camera Z depth to original Z
        Vector3 newPos = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);
        newPos.z = offset.z;

        // --- 5. Screenshake Solver ---
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
}
