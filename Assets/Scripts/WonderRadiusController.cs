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
    [SerializeField] private KeyCode toggleKey = KeyCode.Space;

    // Shader property IDs (cached for performance)
    private static readonly int WonderCenterID = Shader.PropertyToID("_WonderCenter");
    private static readonly int WonderRadiusID = Shader.PropertyToID("_WonderRadius");
    private static readonly int WonderFeatherID = Shader.PropertyToID("_WonderFeather");

    // Static singleton reference for easy querying
    private static WonderRadiusController instance;

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
        // Toggle on/off
        if (allowToggle && Input.GetKeyDown(toggleKey))
        {
            isActive = !isActive;
        }

        // Scroll wheel to resize radius
        if (allowScrollResize && isActive)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                radius += scroll * radiusChangeSpeed;
                radius = Mathf.Clamp(radius, minRadius, maxRadius);
            }
        }
    }

    /// <summary>
    /// Updates the center position based on the current mode.
    /// </summary>
    private void UpdateCenter()
    {
        switch (mode)
        {
            case RadiusMode.FollowPlayer:
                currentCenter = transform.position;
                break;

            case RadiusMode.FollowMouse:
                if (mainCamera != null)
                {
                    Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorld.z = 0f;
                    currentCenter = mouseWorld;
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