using UnityEngine;

/// <summary>
/// A code-driven procedural aesthetics script attached to any WonderObject.
/// When inside the active Wonder Zone:
///   1. Gently bobs the object's visuals in a weightless sine wave.
///   2. Programmatically creates and animates a pulsing bioluminescent glow outline.
/// </summary>
[RequireComponent(typeof(WonderObject))]
public class WonderObjectJuice : MonoBehaviour
{
    [Header("Bobbing Animation")]
    [Tooltip("Enable weightless drifting/bobbing when inside the Wonder Radius.")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobSpeed = 2.8f;
    [SerializeField] private float bobRange = 0.15f;

    [Header("Bioluminescent Glow")]
    [Tooltip("Enable procedural glowing neon outline.")]
    [SerializeField] private bool enableGlow = true;
    [Tooltip("Neon color of the glowing outline.")]
    [SerializeField] private Color glowColor = new Color(0.9f, 0.2f, 1f, 0.75f); // Hot Neon Magenta
    [SerializeField] private float glowPulseSpeed = 4f;
    [SerializeField] private float glowSizeMultiplier = 1.3f;

    private WonderObject wonderObject;
    private Transform graphicsChild;
    private GameObject glowObj;
    private SpriteRenderer glowRenderer;
    private Vector3 originalGraphicsPos;
    private Sprite glowSprite;

    private void Start()
    {
        wonderObject = GetComponent<WonderObject>();
        
        // Find graphics child
        graphicsChild = transform.Find("Graphics");
        if (graphicsChild == null) graphicsChild = transform.Find("Visuals");
        if (graphicsChild == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.transform != transform) graphicsChild = sr.transform;
        }
        if (graphicsChild == null) graphicsChild = transform;

        originalGraphicsPos = graphicsChild.localPosition;

        if (enableGlow)
        {
            SetupGlowOutline();
        }
    }

    /// <summary>
    /// Programmatically generates a radial glow sprite in-memory and sets up the child glow visual.
    /// </summary>
    private void SetupGlowOutline()
    {
        // 1. Generate soft circular radial texture
        Texture2D tex = new Texture2D(32, 32);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                alpha = Mathf.Pow(alpha, 1.8f); // Soft exponential falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);

        // 2. Create outline child
        glowObj = new GameObject("WonderGlowOutline");
        glowObj.transform.SetParent(graphicsChild, false);
        glowObj.transform.localPosition = new Vector3(0f, 0f, 0.05f); // Render slightly in front or back

        // Scale to wrap bounds of the parent sprite
        var parentSr = graphicsChild.GetComponent<SpriteRenderer>();
        if (parentSr == null) parentSr = GetComponentInChildren<SpriteRenderer>();
        if (parentSr != null && parentSr.sprite != null)
        {
            float boundsWidth = parentSr.sprite.bounds.size.x;
            float boundsHeight = parentSr.sprite.bounds.size.y;
            glowObj.transform.localScale = new Vector3(boundsWidth * glowSizeMultiplier, boundsHeight * glowSizeMultiplier, 1f);
        }
        else
        {
            glowObj.transform.localScale = new Vector3(glowSizeMultiplier, glowSizeMultiplier, 1f);
        }

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = glowSprite;

        // Use custom WonderMask shader so it is only visible inside the Wonder Zone
        Shader wonderShader = Shader.Find("Sprites/WonderMask");
        if (wonderShader != null)
        {
            glowRenderer.material = new Material(wonderShader);
            glowRenderer.material.SetFloat("_Feather", 0.35f);
        }
        else
        {
            glowRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        glowRenderer.color = Color.clear; // Hide initially
        
        // Render right behind the parent sprite so it looks like an outline
        if (parentSr != null)
        {
            glowRenderer.sortingOrder = parentSr.sortingOrder - 1;
        }
        else
        {
            glowRenderer.sortingOrder = -1;
        }
    }

    private void Update()
    {
        if (wonderObject == null) return;

        bool inside = wonderObject.IsInWonderZone;

        // --- 1. Gentle Weightless Bobbing ---
        if (enableBobbing && graphicsChild != null)
        {
            if (inside)
            {
                float offset = Mathf.Sin(Time.time * bobSpeed) * bobRange;
                graphicsChild.localPosition = originalGraphicsPos + new Vector3(0f, offset, 0f);
            }
            else
            {
                // Smoothly return to mundane resting position
                graphicsChild.localPosition = Vector3.Lerp(graphicsChild.localPosition, originalGraphicsPos, Time.deltaTime * 5f);
            }
        }

        // --- 2. Outline Pulsate ---
        if (enableGlow && glowRenderer != null)
        {
            if (inside)
            {
                // Pulse alpha glow
                float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f;
                Color pulseColor = glowColor;
                pulseColor.a = Mathf.Lerp(0.35f, 0.85f, pulse);
                glowRenderer.color = pulseColor;

                // Gentle breathing scale pulsate
                float sizePulse = glowSizeMultiplier * (1f + 0.05f * Mathf.Sin(Time.time * glowPulseSpeed * 1.2f));
                var parentSr = graphicsChild.GetComponent<SpriteRenderer>();
                if (parentSr == null) parentSr = GetComponentInChildren<SpriteRenderer>();
                
                if (parentSr != null && parentSr.sprite != null)
                {
                    float boundsWidth = parentSr.sprite.bounds.size.x;
                    float boundsHeight = parentSr.sprite.bounds.size.y;
                    glowObj.transform.localScale = new Vector3(boundsWidth * sizePulse, boundsHeight * sizePulse, 1f);
                }
            }
            else
            {
                // Fade out outline cleanly
                glowRenderer.color = Color.Lerp(glowRenderer.color, Color.clear, Time.deltaTime * 8f);
            }
        }
    }
}
