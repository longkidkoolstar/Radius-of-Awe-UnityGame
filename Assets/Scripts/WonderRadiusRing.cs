using UnityEngine;

/// <summary>
/// Dynamically draws a glowing neon circle boundary around the active Wonder Radius.
/// Uses a LineRenderer and animates its alpha and width to create a pulse effect.
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

    private LineRenderer lineRenderer;

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
        
        // Use the built-in Sprites/Default shader so it supports coloring and transparency out of the box
        Shader defaultSpriteShader = Shader.Find("Sprites/Default");
        if (defaultSpriteShader != null)
        {
            lineRenderer.material = new Material(defaultSpriteShader);
        }
    }

    private void LateUpdate()
    {
        if (controller == null || lineRenderer == null) return;

        float radius = controller.Radius;
        
        // Hide the ring if the radius is near zero (inactive)
        if (radius <= 0.02f)
        {
            lineRenderer.enabled = false;
            return;
        }
        
        lineRenderer.enabled = true;

        // Calculate pulsing alpha using sine wave
        float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f;
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
        
        Color currentStart = startColor;
        currentStart.a = currentAlpha;
        Color currentEnd = endColor;
        currentEnd.a = currentAlpha;

        lineRenderer.startColor = currentStart;
        lineRenderer.endColor = currentEnd;
        
        // Pulse the line width slightly to enhance the neon "buzzing" feel
        float currentWidth = width * (1f + 0.15f * Mathf.Sin(Time.time * glowPulseSpeed * 1.5f));
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;

        // Generate coordinates for a perfect circle around the Wonder center
        Vector3 center = controller.Center;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius,
                center.z // Keep camera z-depth alignment
            );
            lineRenderer.SetPosition(i, pos);
        }
    }
}
