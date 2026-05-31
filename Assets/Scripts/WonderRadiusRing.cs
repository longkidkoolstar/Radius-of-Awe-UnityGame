using UnityEngine;

/// <summary>
/// Dynamically draws a glowing neon circle boundary around the active Wonder Radius.
/// Integrates:
///   1. A soft, semi-transparent reality background bubble (bioluminescent glow field).
///   2. An expanding shockwave energy ripple when toggled ON (procedurally generated in memory).
///   3. Sleek elastic scaling pop animation when the radius expands.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class WonderRadiusRing : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The controller managing the Wonder Radius. If left blank, will try to find one in parents/children.")]
    [SerializeField] private WonderRadiusController controller;

    [Header("Visual Settings")]
    [Tooltip("Number of segments in the circle. Higher is smoother but heavier.")]
    [SerializeField] private int segments = 64;
    [Tooltip("The default width of the boundary line.")]
    [SerializeField] private float width = 0.06f;
    [Tooltip("Color of the ring. A gradient looks extremely high-quality and dynamic.")]
    [SerializeField] private Color startColor = new Color(0.2f, 0.85f, 1f, 0.85f); // Electric Blue/Cyan
    [SerializeField] private Color endColor = new Color(0.9f, 0.2f, 1f, 0.85f);   // Neon Magenta

    [Header("Pulse Animation")]
    [Tooltip("Speed at which the neon glow pulses.")]
    [SerializeField] private float glowPulseSpeed = 3f;
    [Tooltip("Minimum alpha opacity of the glow.")]
    [SerializeField] private float minAlpha = 0.45f;
    [Tooltip("Maximum alpha opacity of the glow.")]
    [SerializeField] private float maxAlpha = 0.95f;

    [Header("Reality Bubble Background")]
    [Tooltip("Enable the glowing background reality bubble field inside the circle.")]
    [SerializeField] private bool enableBubbleBg = true;
    [Tooltip("Soft, semi-transparent color for the radial background bubble.")]
    [SerializeField] private Color bubbleColor = new Color(0.6f, 0.2f, 1f, 0.08f); // Faint violet glow

    [Header("Expansion Shockwave Ripple")]
    [Tooltip("Enable the expanding energy shockwave ring when the Wonder Zone is activated.")]
    [SerializeField] private bool enableShockwave = true;
    [SerializeField] private Color shockwaveColor = new Color(0.2f, 0.85f, 1f, 0.9f); // Bright Cyan
    [SerializeField] private float shockwaveDuration = 0.35f;

    private LineRenderer lineRenderer;
    
    // Bubble background state
    private GameObject bubbleObj;
    private SpriteRenderer bubbleRenderer;
    private Sprite bubbleSprite;

    // Shockwave ripple state
    private GameObject shockObj;
    private SpriteRenderer shockRenderer;
    private Sprite hollowRingSprite;
    private float shockTimer = 0f;
    private float targetShockScale = 0f;

    // Latching state for toggle
    private bool wasControllerActive = false;
    private float popOpenTimer = 0f;
    private const float popOpenDuration = 0.38f;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (controller == null)
        {
            controller = GetComponentInParent<WonderRadiusController>();
            if (controller == null)
            {
                controller = FindFirstObjectByType<WonderRadiusController>();
            }
        }

        // Configure LineRenderer properties
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        
        Shader defaultSpriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (defaultSpriteShader != null)
        {
            lineRenderer.material = new Material(defaultSpriteShader);
        }

        GenerateProceduralSprites();
        
        if (enableBubbleBg) SetupBubbleBackground();
        if (enableShockwave) SetupShockwaveRing();

        if (controller != null)
        {
            wasControllerActive = controller.IsActive;
        }
    }

    /// <summary>
    /// Generates the radial gradient background and the thin hollow ring texture programmatically.
    /// </summary>
    private void GenerateProceduralSprites()
    {
        // 1. Soft radial gradient texture
        Texture2D bubbleTex = new Texture2D(32, 32);
        bubbleTex.filterMode = FilterMode.Bilinear;
        bubbleTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                alpha = Mathf.Pow(alpha, 2.0f); // Soft edge blending
                bubbleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        bubbleTex.Apply();
        bubbleSprite = Sprite.Create(bubbleTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);

        // 2. Hollow circular ring texture (extremely sharp boundary line)
        Texture2D ringTex = new Texture2D(32, 32);
        ringTex.filterMode = FilterMode.Bilinear;
        ringTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                // Hollow ring width centered at radius 14.5
                float val = 1f - Mathf.Abs(dist - 14.2f) / 1.6f;
                float alpha = Mathf.Clamp01(val);
                alpha = Mathf.Pow(alpha, 1.5f);
                ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        ringTex.Apply();
        hollowRingSprite = Sprite.Create(ringTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
    }

    private void SetupBubbleBackground()
    {
        bubbleObj = new GameObject("WonderBubbleBackground");
        bubbleObj.transform.SetParent(this.transform, false);
        bubbleObj.transform.localPosition = new Vector3(0f, 0f, 0.2f); // Render behind player sprite

        bubbleRenderer = bubbleObj.AddComponent<SpriteRenderer>();
        bubbleRenderer.sprite = bubbleSprite;
        bubbleRenderer.color = Color.clear;
        bubbleRenderer.sortingOrder = -5; // Render very deep in background
    }

    private void SetupShockwaveRing()
    {
        shockObj = new GameObject("WonderExpansionShockwave");
        shockObj.transform.SetParent(this.transform, false);
        shockObj.transform.localPosition = new Vector3(0f, 0f, -0.05f); // Render slightly in front

        shockRenderer = shockObj.AddComponent<SpriteRenderer>();
        shockRenderer.sprite = hollowRingSprite;
        shockRenderer.color = Color.clear;
        shockRenderer.sortingOrder = 14; // In front of main visual characters
    }

    private void LateUpdate()
    {
        if (controller == null || lineRenderer == null) return;

        float radius = controller.Radius;
        bool isWonderActive = controller.IsActive;

        // --- 1. Toggle Latch & Elastic Pop Trigger ---
        if (isWonderActive && !wasControllerActive)
        {
            // Pop open trigger!
            popOpenTimer = popOpenDuration;
            
            // Trigger shockwave
            if (enableShockwave)
            {
                shockTimer = shockwaveDuration;
                targetShockScale = controller.Radius * 2.1f; // Expand slightly beyond final boundary
            }
        }
        wasControllerActive = isWonderActive;

        // Decent timers
        if (popOpenTimer > 0) popOpenTimer -= Time.deltaTime;
        if (shockTimer > 0) shockTimer -= Time.deltaTime;

        // Hide visuals if radius is tiny
        if (radius <= 0.02f)
        {
            lineRenderer.enabled = false;
            if (bubbleRenderer != null) bubbleRenderer.color = Color.clear;
            if (shockRenderer != null) shockRenderer.color = Color.clear;
            return;
        }

        lineRenderer.enabled = true;

        // --- 2. Elastic Pop Size Modifier ---
        float elasticRadius = radius;
        if (popOpenTimer > 0)
        {
            float progress = 1f - (popOpenTimer / popOpenDuration);
            // Elastic pop curve: rises fast, overshoots, settles
            float overshoot = Mathf.Sin(progress * Mathf.PI * 1.5f) * 0.12f * (1f - progress);
            elasticRadius = radius * (progress + overshoot);
        }

        // --- 3. Animate LineRenderer circle ---
        float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f;
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
        
        Color currentStart = startColor;
        currentStart.a = currentAlpha;
        Color currentEnd = endColor;
        currentEnd.a = currentAlpha;

        lineRenderer.startColor = currentStart;
        lineRenderer.endColor = currentEnd;
        
        float currentWidth = width * (1f + 0.15f * Mathf.Sin(Time.time * glowPulseSpeed * 1.5f));
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;

        Vector3 center = controller.Center;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                center.x + Mathf.Cos(angle) * elasticRadius,
                center.y + Mathf.Sin(angle) * elasticRadius,
                center.z
            );
            lineRenderer.SetPosition(i, pos);
        }

        // --- 4. Animate Bubble Background ---
        if (enableBubbleBg && bubbleRenderer != null)
        {
            // Match current elastic radius (radius maps to scale diameter: radius * 2)
            bubbleObj.transform.localScale = new Vector3(elasticRadius * 2f, elasticRadius * 2f, 1f);
            
            // Keep centered in world space (handles mouse follow or player drift)
            bubbleObj.transform.position = new Vector3(center.x, center.y, bubbleObj.transform.position.z);

            // Fade alpha based on whether the controller is active
            Color targetColor = isWonderActive ? bubbleColor : Color.clear;
            bubbleRenderer.color = Color.Lerp(bubbleRenderer.color, targetColor, Time.deltaTime * 6f);
        }

        // --- 5. Animate Shockwave Ripple ---
        if (enableShockwave && shockRenderer != null)
        {
            if (shockTimer > 0)
            {
                float progress = 1f - (shockTimer / shockwaveDuration);
                
                // Scale expands rapidly
                float currentShockScale = Mathf.Lerp(0f, targetShockScale, progress);
                shockObj.transform.localScale = new Vector3(currentShockScale, currentShockScale, 1f);
                shockObj.transform.position = new Vector3(center.x, center.y, shockObj.transform.position.z);

                // Alpha fades out to 0
                Color currentShockColor = shockwaveColor;
                currentShockColor.a = Mathf.Lerp(1.0f, 0.0f, progress);
                shockRenderer.color = currentShockColor;
            }
            else
            {
                shockRenderer.color = Color.clear;
            }
        }
    }
}
