using UnityEngine;

/// <summary>
/// Controls the position and size of the "Wonder Radius" zone.
/// Feeds center coordinates and radius to global shader properties so that
/// all materials using the WonderMask shader update in real-time.
/// Also exposes a static API for other scripts to query zone membership.
/// </summary>
public class WonderRadiusController : MonoBehaviour
{
    public enum RadiusMode
    {
        /// <summary>The Wonder Radius is centered on this GameObject's position.</summary>
        FollowPlayer,
        /// <summary>The Wonder Radius follows the mouse cursor in world space.</summary>
        FollowMouse
    }

    [Header("Mode")]
    [SerializeField] private RadiusMode mode = RadiusMode.FollowPlayer;

    [Header("Radius Settings")]
    [SerializeField] private float radius = 3.5f;
    [SerializeField] private float minRadius = 1f;
    [SerializeField] private float maxRadius = 8f;
    [SerializeField] private float radiusChangeSpeed = 2f;
    [SerializeField] private bool allowScrollResize = true;

    [Header("Transition")]
    [Tooltip("How quickly the shader radius lerps to the target radius.")]
    [SerializeField] private float radiusSmoothSpeed = 8f;
    [Tooltip("Feather softness at the edge of the radius (shader property).")]
    [SerializeField] private float feather = 0.5f;

    [Header("Toggle")]
    [Tooltip("Allow the player to toggle the Wonder Radius on/off.")]
    [SerializeField] private bool allowToggle = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.E;

    [Header("Controller Mappings")]
    [Tooltip("Controller button to toggle the Wonder Radius (Xbox X, PS Square, etc.).")]
    [SerializeField] private KeyCode controllerToggleKey = KeyCode.JoystickButton2;
    [Tooltip("Maximum offset distance the Right Stick can push the radius center.")]
    [SerializeField] private float controllerAimOffsetRange = 5f;
    [Tooltip("How quickly the radius offset responds to Right Stick inputs.")]
    [SerializeField] private float controllerAimSmoothSpeed = 10f;

    // Controller Aiming States
    private Vector3 currentAimOffset = Vector3.zero;
    private Vector3 virtualCursorPos;


    // Shader property IDs (cached for performance)
    private static readonly int WonderCenterID = Shader.PropertyToID("_WonderCenter");
    private static readonly int WonderRadiusID = Shader.PropertyToID("_WonderRadius");
    private static readonly int WonderFeatherID = Shader.PropertyToID("_WonderFeather");

    // Static singleton reference for easy querying
    private static WonderRadiusController instance;
    public static WonderRadiusController Instance => instance;

    // Drift Mode state
    private bool isDrifting = false;
    private float driftTargetRadius;
    private float driftExpansionSpeed;

    /// <summary>Enables manual drift mode centered at a custom position with a starting radius.</summary>
    public void EnableDriftMode(Vector3 customCenter, float startRadius)
    {
        isDrifting = true;
        currentCenter = customCenter;
        currentRadius = startRadius;
    }

    /// <summary>Sets the target drift radius and speed for smooth wave expansion.</summary>
    public void SetDriftRadius(float targetRadius, float speed)
    {
        driftTargetRadius = targetRadius;
        driftExpansionSpeed = speed;
    }

    // Runtime state
    private Camera mainCamera;
    private Vector3 currentCenter;
    private float currentRadius;
    private bool isActive = true;

    /// <summary>Current world-space center of the Wonder Zone.</summary>
    public Vector3 Center => currentCenter;

    /// <summary>Current effective radius of the Wonder Zone.</summary>
    public float Radius => currentRadius;

    /// <summary>Whether the Wonder Zone is currently active.</summary>
    public bool IsActive => isActive;

    /// <summary>
    /// Static helper: returns true if a given world position is inside the active Wonder Zone.
    /// </summary>
    public static bool IsInsideWonderZone(Vector3 worldPosition)
    {
        if (instance == null || !instance.isActive) return false;
        float dist = Vector2.Distance(worldPosition, instance.currentCenter);
        return dist <= instance.currentRadius;
    }

    /// <summary>
    /// Static helper: returns the normalized distance (0 at center, 1 at edge) of a
    /// world position relative to the Wonder Zone. Returns float.MaxValue if zone is inactive.
    /// </summary>
    public static float GetNormalizedDistance(Vector3 worldPosition)
    {
        if (instance == null || !instance.isActive || instance.currentRadius <= 0)
            return float.MaxValue;

        float dist = Vector2.Distance(worldPosition, instance.currentCenter);
        return dist / instance.currentRadius;
    }

    private void Awake()
    {
        // Singleton setup (non-destructive — last spawned wins)
        instance = this;
        mainCamera = Camera.main;
        currentRadius = radius;
        virtualCursorPos = transform.position;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            // Reset shader globals so Wonder visuals disappear cleanly
            Shader.SetGlobalFloat(WonderRadiusID, 0f);
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateCenter();
        UpdateRadius();
        PushShaderGlobals();
    }

    /// <summary>
    /// Handles toggle and scroll-wheel resize input.
    /// </summary>
    private void HandleInput()
    {
        // Toggle on/off (keyboard or controller button)
        if (allowToggle && (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(controllerToggleKey)))
        {
            isActive = !isActive;
            if (isActive)
            {
                AudioManager.PlayWonderToggleOn();
            }
            else
            {
                AudioManager.PlayWonderToggleOff();
            }
        }

        // Scroll wheel or Controller Triggers to resize radius
        if (allowScrollResize && isActive)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            // Continuous trigger axis sizing (analog trigger values go from 0 to 1)
            float controllerSizeDelta = 0f;
            float lt = 0f;
            float rt = 0f;
            try
            {
                lt = Input.GetAxis("LeftTrigger");
                rt = Input.GetAxis("RightTrigger");
            }
            catch (System.ArgumentException)
            {
                // Fallback in case axes are somehow not set up in InputManager
            }

            // Clamping deadzone at 0.1 to avoid stick drift / initial trigger offsets
            if (lt > 0.1f)
            {
                controllerSizeDelta -= lt * radiusChangeSpeed * Time.deltaTime;
            }
            if (rt > 0.1f)
            {
                controllerSizeDelta += rt * radiusChangeSpeed * Time.deltaTime;
            }

            if (Mathf.Abs(scroll) > 0.01f)
            {
                radius += scroll * radiusChangeSpeed;
                radius = Mathf.Clamp(radius, minRadius, maxRadius);
            }
            else if (Mathf.Abs(controllerSizeDelta) > 0.001f)
            {
                radius += controllerSizeDelta;
                radius = Mathf.Clamp(radius, minRadius, maxRadius);
            }
        }
    }

    /// <summary>
    /// Updates the center position based on the current mode.
    /// </summary>
    private void UpdateCenter()
    {
        if (isDrifting) return; // Freeze position during dimensional drift

        // Attempt to read Right Stick input for analog aiming
        float rx = 0f;
        float ry = 0f;
        try
        {
            rx = Input.GetAxis("RightStickX");
            ry = Input.GetAxis("RightStickY");
        }
        catch (System.ArgumentException)
        {
            // Fallback in case axes are somehow not set up in InputManager
        }

        switch (mode)
        {
            case RadiusMode.FollowPlayer:
                Vector3 targetOffset = Vector3.zero;

                if (Mathf.Abs(rx) > 0.05f || Mathf.Abs(ry) > 0.05f)
                {
                    // Controller: Smoothly aim/offset using the Right Stick
                    targetOffset = new Vector3(rx, ry, 0f) * controllerAimOffsetRange;
                }
                else if (mainCamera != null)
                {
                    // KBM: Tethered Aim Pull - Mouse pulls the radius in its direction, clamped to the range
                    Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorld.z = 0f;
                    Vector3 playerToMouse = mouseWorld - transform.position;
                    targetOffset = Vector3.ClampMagnitude(playerToMouse, controllerAimOffsetRange);
                }

                currentAimOffset = Vector3.Lerp(currentAimOffset, targetOffset, Time.deltaTime * controllerAimSmoothSpeed);
                currentCenter = transform.position + currentAimOffset;
                break;

            case RadiusMode.FollowMouse:
                if (mainCamera != null)
                {
                    // Check if Right Stick is actively aiming
                    if (Mathf.Abs(rx) > 0.05f || Mathf.Abs(ry) > 0.05f)
                    {
                        // Move a virtual world-space cursor via the stick
                        virtualCursorPos += new Vector3(rx, ry, 0f) * (radiusChangeSpeed * 6f) * Time.deltaTime;
                        currentCenter = virtualCursorPos;
                    }
                    else if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f)
                    {
                        // Active mouse movement overrides the virtual cursor
                        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                        mouseWorld.z = 0f;
                        virtualCursorPos = mouseWorld;
                        currentCenter = mouseWorld;
                    }
                    else
                    {
                        // If no active input, keep current center
                        currentCenter = virtualCursorPos;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Smoothly lerps the actual shader radius toward the target.
    /// When inactive, the radius shrinks to zero.
    /// </summary>
    private void UpdateRadius()
    {
        if (isDrifting)
        {
            currentRadius = Mathf.MoveTowards(currentRadius, driftTargetRadius, Time.deltaTime * driftExpansionSpeed);
            return;
        }

        float targetRadius = isActive ? radius : 0f;
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * radiusSmoothSpeed);

        // Snap to zero when very close to avoid perpetual tiny circles
        if (!isActive && currentRadius < 0.01f)
        {
            currentRadius = 0f;
        }
    }

    /// <summary>
    /// Writes the current center, radius, and feather values to global shader properties.
    /// Every material using _WonderCenter / _WonderRadius / _WonderFeather will automatically update.
    /// </summary>
    private void PushShaderGlobals()
    {
        Shader.SetGlobalVector(WonderCenterID, new Vector4(currentCenter.x, currentCenter.y, 0, 0));
        Shader.SetGlobalFloat(WonderRadiusID, currentRadius);
        Shader.SetGlobalFloat(WonderFeatherID, feather);
    }

    /// <summary>
    /// Draws the Wonder Radius gizmo in the Scene view for easy visual tuning.
    /// Green when active, dim red when inactive.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? new Color(0.2f, 1f, 0.6f, 0.35f) : new Color(1f, 0.2f, 0.2f, 0.15f);
        Vector3 center = Application.isPlaying ? currentCenter : transform.position;
        float drawRadius = Application.isPlaying ? currentRadius : radius;

        // Draw filled circle approximation
        DrawWireCircle(center, drawRadius, 64);

        // Draw feather edge
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
        DrawWireCircle(center, drawRadius + feather, 64);
    }

    private void DrawWireCircle(Vector3 center, float circleRadius, int segments)
    {
        if (circleRadius <= 0) return;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(circleRadius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * circleRadius, Mathf.Sin(angle) * circleRadius, 0);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}