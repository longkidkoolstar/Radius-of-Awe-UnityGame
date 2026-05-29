using UnityEngine;

/// <summary>
/// A code-driven procedural aesthetics script attached to any WonderObject.
/// Fixes the physics jitter bug by automatically isolating visual bobs into a runtime child.
/// Supports:
///   1. Whimsical sine-wave bobbing when inside the Wonder zone.
///   2. Programmatic generation and animation of a pulsing neon radial outline glow.
///   3. Dynamic sprite & color swaps (morphs plain cubes into gorgeous glowing runic blocks).
/// </summary>
[RequireComponent(typeof(WonderObject))]
public class WonderObjectJuice : MonoBehaviour
{
    [Header("Visual bobbing")]
    [Tooltip("Enable weightless drifting/bobbing when inside the Wonder Radius.")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobSpeed = 2.8f;
    [SerializeField] private float bobRange = 0.15f;

    [Header("Procedural Neon Outline")]
    [Tooltip("Enable procedural glowing neon outline.")]
    [SerializeField] private bool enableGlow = true;
    [Tooltip("Neon color of the glowing outline.")]
    [SerializeField] private Color glowColor = new Color(0.9f, 0.2f, 1f, 0.75f); // Hot Neon Magenta
    [SerializeField] private float glowPulseSpeed = 4f;
    [SerializeField] private float glowSizeMultiplier = 1.3f;

    [Header("Dynamic Sprite Swap")]
    [Tooltip("Enable dynamic sprite and color swaps inside the Wonder Zone.")]
    [SerializeField] private bool enableVisualSwap = true;
    [Tooltip("Color to shift to inside the Wonder Zone.")]
    [SerializeField] private Color wonderColor = new Color(0.2f, 0.85f, 1.0f, 1.0f); // Electric Cyan
    [Tooltip("Optional custom wonder sprite. If null, a gorgeous magical runic grid texture will be generated programmatically!")]
    [SerializeField] private Sprite customWonderSprite;

    private WonderObject wonderObject;
    private Transform graphicsChild;
    private GameObject glowObj;
    private SpriteRenderer glowRenderer;
    private SpriteRenderer graphicsRenderer;
    
    // Original state cached for restoration
    private Vector3 originalGraphicsPos;
    private Sprite mundaneSprite;
    private Color mundaneColor;

    // Generated assets
    private Sprite glowSprite;
    private Sprite proceduralRuneSprite;

    private void Start()
    {
        wonderObject = GetComponent<WonderObject>();
        
        // --- Fix the Physics Bobbing Bug (Transform Isolation) ---
        // If there is no dedicated child named "Graphics" or "Visuals", we automatically create one!
        // This isolates the visual bobbing from the parent Rigidbody2D transform.
        graphicsChild = transform.Find("Graphics");
        if (graphicsChild == null) graphicsChild = transform.Find("Visuals");
        
        if (graphicsChild == null)
        {
            // Create runtime child "Graphics"
            GameObject go = new GameObject("Graphics");
            go.transform.SetParent(this.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            // Move the root SpriteRenderer to the new child if it exists
            var rootSr = GetComponent<SpriteRenderer>();
            if (rootSr != null)
            {
                graphicsRenderer = go.AddComponent<SpriteRenderer>();
                
                // Copy properties
                graphicsRenderer.sprite = rootSr.sprite;
                graphicsRenderer.color = rootSr.color;
                graphicsRenderer.material = rootSr.material;
                graphicsRenderer.sortingOrder = rootSr.sortingOrder;
                
                // Destroy root SpriteRenderer to keep physics clean
                Destroy(rootSr);
            }
            graphicsChild = go.transform;
        }
        else
        {
            graphicsRenderer = graphicsChild.GetComponent<SpriteRenderer>();
        }

        if (graphicsRenderer == null)
        {
            graphicsRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        originalGraphicsPos = graphicsChild.localPosition;

        // Cache mundane visuals
        if (graphicsRenderer != null)
        {
            mundaneSprite = graphicsRenderer.sprite;
            mundaneColor = graphicsRenderer.color;
        }

        // Generate procedural textures
        GenerateProceduralTextures();

        if (enableGlow)
        {
            SetupGlowOutline();
        }
    }

    /// <summary>
    /// Generates glow masks and a beautiful magical runic coordinate grid for visual swaps.
    /// </summary>
    private void GenerateProceduralTextures()
    {
        // 1. Soft circular glow mask
        Texture2D glowTex = new Texture2D(32, 32);
        glowTex.filterMode = FilterMode.Bilinear;
        glowTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                alpha = Mathf.Pow(alpha, 1.8f);
                glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        glowTex.Apply();
        glowSprite = Sprite.Create(glowTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);

        // 2. Retro magical rune block visual (crisp square border, diagonal cross, glowing core)
        Texture2D runeTex = new Texture2D(32, 32);
        runeTex.filterMode = FilterMode.Point; // Sharp grid lines
        runeTex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool isBorder = (x == 0 || x == 31 || y == 0 || y == 31);
                bool isInnerBorder = (x == 2 || x == 29 || y == 2 || y == 29);
                bool isDiagonal = (Mathf.Abs(x - y) <= 1 || Mathf.Abs(x - (31 - y)) <= 1);
                
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                bool isCenterCore = dist < 5.5f;

                float alpha = 0.05f; // Faint glow fill
                if (isBorder) alpha = 1.0f;
                else if (isInnerBorder) alpha = 0.75f;
                else if (isDiagonal) alpha = 0.55f;
                else if (isCenterCore) alpha = Mathf.Clamp01(1f - (dist / 5.5f)) * 0.95f;

                runeTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        runeTex.Apply();
        // Pack into sprite
        proceduralRuneSprite = Sprite.Create(runeTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    private void SetupGlowOutline()
    {
        glowObj = new GameObject("WonderGlowOutline");
        glowObj.transform.SetParent(graphicsChild, false);
        glowObj.transform.localPosition = new Vector3(0f, 0f, 0.05f);

        // Match local scale based on parent bounds
        if (graphicsRenderer != null && graphicsRenderer.sprite != null)
        {
            float boundsWidth = graphicsRenderer.sprite.bounds.size.x;
            float boundsHeight = graphicsRenderer.sprite.bounds.size.y;
            glowObj.transform.localScale = new Vector3(boundsWidth * glowSizeMultiplier, boundsHeight * glowSizeMultiplier, 1f);
        }
        else
        {
            glowObj.transform.localScale = new Vector3(glowSizeMultiplier, glowSizeMultiplier, 1f);
        }

        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = glowSprite;

        // Mask shader setup
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

        glowRenderer.color = Color.clear;
        glowRenderer.sortingOrder = (graphicsRenderer != null) ? graphicsRenderer.sortingOrder - 1 : -1;
    }

    private void Update()
    {
        if (wonderObject == null) return;

        bool inside = wonderObject.IsInWonderZone;

        // --- 1. Weightless Drifting/Bobbing ---
        if (enableBobbing && graphicsChild != null)
        {
            if (inside)
            {
                float offset = Mathf.Sin(Time.time * bobSpeed) * bobRange;
                graphicsChild.localPosition = originalGraphicsPos + new Vector3(0f, offset, 0f);
            }
            else
            {
                // Smoothly slide back to baseline position
                graphicsChild.localPosition = Vector3.Lerp(graphicsChild.localPosition, originalGraphicsPos, Time.deltaTime * 6f);
            }
        }

        // --- 2. Dynamic Sprite Renderer Swap ---
        if (enableVisualSwap && graphicsRenderer != null)
        {
            if (inside)
            {
                // Swap to runic sprite and colorful glow
                Sprite targetSprite = (customWonderSprite != null) ? customWonderSprite : proceduralRuneSprite;
                graphicsRenderer.sprite = targetSprite;
                graphicsRenderer.color = wonderColor;
            }
            else
            {
                // Revert to dull mundane sprite and color
                graphicsRenderer.sprite = mundaneSprite;
                graphicsRenderer.color = mundaneColor;
            }
        }

        // --- 3. Glow Outline Pulsate ---
        if (enableGlow && glowRenderer != null)
        {
            if (inside)
            {
                float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f;
                Color pulseColor = glowColor;
                pulseColor.a = Mathf.Lerp(0.3f, 0.8f, pulse);
                glowRenderer.color = pulseColor;

                float sizePulse = glowSizeMultiplier * (1f + 0.05f * Mathf.Sin(Time.time * glowPulseSpeed * 1.2f));
                if (graphicsRenderer != null && graphicsRenderer.sprite != null)
                {
                    float boundsWidth = graphicsRenderer.sprite.bounds.size.x;
                    float boundsHeight = graphicsRenderer.sprite.bounds.size.y;
                    glowObj.transform.localScale = new Vector3(boundsWidth * sizePulse, boundsHeight * sizePulse, 1f);
                }
            }
            else
            {
                glowRenderer.color = Color.Lerp(glowRenderer.color, Color.clear, Time.deltaTime * 8f);
            }
        }
    }
}
