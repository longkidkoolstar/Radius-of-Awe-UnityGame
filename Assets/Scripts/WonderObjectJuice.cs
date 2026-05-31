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
    private Sprite mundaneHoverboardSprite;
    private Sprite wonderHoverboardSprite;

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

        // Check if this object is the hoverboard and generate dynamic sprites
        bool isHoverboard = GetComponent<RideableFloaty>() != null;
        if (isHoverboard)
        {
            GenerateHoverboardSprites();
            if (graphicsRenderer != null)
            {
                graphicsRenderer.sprite = mundaneHoverboardSprite;
                graphicsRenderer.color = Color.white;
            }
            customWonderSprite = wonderHoverboardSprite;
        }

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
            glowRenderer.material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
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

    /// <summary>
    /// Generates high-fidelity mechanical slate and cyber-runic hoverboard textures.
    /// Perfectly maps pixel aspect ratios to the actual BoxCollider2D bounds!
    /// </summary>
    private void GenerateHoverboardSprites()
    {
        Vector2 colSize = new Vector2(2.4f, 0.4f);
        var boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            colSize = boxCol.size;
        }

        float ppu = 100f;
        int width = Mathf.Max(32, Mathf.RoundToInt(colSize.x * ppu));
        int height = Mathf.Max(16, Mathf.RoundToInt(colSize.y * ppu));

        // 1. Generate Mundane Hoverboard Sprite
        Texture2D mundaneTex = new Texture2D(width, height);
        mundaneTex.filterMode = FilterMode.Bilinear;
        mundaneTex.wrapMode = TextureWrapMode.Clamp;

        // Slate metallic slate
        Color baseGrey = new Color(0.28f, 0.31f, 0.35f, 1f);
        // Dark industrial steel bezel
        Color borderDark = new Color(0.16f, 0.18f, 0.2f, 1f);
        // Light metallic highlight
        Color borderLight = new Color(0.48f, 0.52f, 0.58f, 1f);
        // Black/charcoal grip pad
        Color gripColor = new Color(0.12f, 0.13f, 0.15f, 1f);
        // Deep shadow/groove
        Color mechanicalDark = new Color(0.08f, 0.09f, 0.1f, 1f);
        // Inactive glass red bumper
        Color bumperOff = new Color(0.45f, 0.1f, 0.1f, 1f);
        // Exhaust nozzle
        Color thrusterMetal = new Color(0.2f, 0.22f, 0.25f, 1f);

        float cornerRadius = Mathf.Min(8f, height * 0.25f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Corner rounding check
                if (IsOutsideRoundedCorners(x, y, width, height, cornerRadius))
                {
                    mundaneTex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Check if in borders
                bool isBorder = (x < 4 || x >= width - 4 || y < 4 || y >= height - 4);
                bool isTopHighlight = (y >= height - 2 && x >= 4 && x < width - 4);
                bool isBottomShadow = (y < 2 && x >= 4 && x < width - 4);

                // Grip traction pads: Left and Right sections
                bool inLeftPad = (x >= width * 0.12f && x < width * 0.42f && y >= 6 && y < height - 6);
                bool inRightPad = (x >= width * 0.58f && x < width * 0.88f && y >= 6 && y < height - 6);

                // Inactive bumper lights: Left end and Right end (centered vertically)
                float leftBumperDist = Vector2.Distance(new Vector2(x, y), new Vector2(10f, height * 0.5f));
                float rightBumperDist = Vector2.Distance(new Vector2(x, y), new Vector2(width - 10f, height * 0.5f));
                bool isBumper = (leftBumperDist < 3.5f || rightBumperDist < 3.5f);

                // Thruster nozzles at bottom
                bool inLeftThruster = (x >= width * 0.25f - 8 && x < width * 0.25f + 8 && y < 3);
                bool inRightThruster = (x >= width * 0.75f - 8 && x < width * 0.75f + 8 && y < 3);

                // Vertical mechanical panel cuts
                bool isPanelLine = (Mathf.Abs(x - width * 0.5f) <= 0.5f || Mathf.Abs(x - width * 0.08f) <= 0.5f || Mathf.Abs(x - width * 0.92f) <= 0.5f);

                // Corner rivets (screws)
                bool isRivet = false;
                if (!isBorder)
                {
                    if ((Mathf.Abs(x - 16) < 1.5f || Mathf.Abs(x - (width - 16)) < 1.5f) &&
                        (Mathf.Abs(y - 8) < 1.5f || Mathf.Abs(y - (height - 8)) < 1.5f))
                    {
                        isRivet = true;
                    }
                }

                // Apply paint logic
                if (isBumper)
                {
                    mundaneTex.SetPixel(x, y, bumperOff);
                }
                else if (inLeftThruster || inRightThruster)
                {
                    mundaneTex.SetPixel(x, y, thrusterMetal);
                }
                else if (isRivet)
                {
                    mundaneTex.SetPixel(x, y, Color.white * 0.75f);
                }
                else if (isPanelLine)
                {
                    mundaneTex.SetPixel(x, y, mechanicalDark);
                }
                else if (inLeftPad || inRightPad)
                {
                    // Draw grip tape with noise pattern for rich texture
                    float noise = Random.value * 0.08f;
                    Color finalGrip = gripColor + new Color(noise, noise, noise, 0f);
                    // Add mechanical traction lines
                    if (x % 6 == 0 || y % 6 == 0) finalGrip = mechanicalDark;
                    mundaneTex.SetPixel(x, y, finalGrip);
                }
                else if (isTopHighlight)
                {
                    mundaneTex.SetPixel(x, y, borderLight);
                }
                else if (isBottomShadow)
                {
                    mundaneTex.SetPixel(x, y, borderDark);
                }
                else if (isBorder)
                {
                    mundaneTex.SetPixel(x, y, borderDark);
                }
                else
                {
                    mundaneTex.SetPixel(x, y, baseGrey);
                }
            }
        }
        mundaneTex.Apply();
        mundaneHoverboardSprite = Sprite.Create(mundaneTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);

        // 2. Generate Wonder Hoverboard Sprite
        Texture2D wonderTex = new Texture2D(width, height);
        wonderTex.filterMode = FilterMode.Bilinear;
        wonderTex.wrapMode = TextureWrapMode.Clamp;

        // Wonder Colors
        Color cyberBase = new Color(0.06f, 0.08f, 0.15f, 1f); // Deep space dark metal
        Color neonCyan = new Color(0f, 0.85f, 1f, 1f); // Glowing cyber cyan
        Color neonMagenta = new Color(1f, 0.15f, 0.75f, 1f); // Glowing cyber pink
        Color glowCore = new Color(0.3f, 0.95f, 1f, 1f); // Hot white-cyan thruster
        Color crystalBar = new Color(0.1f, 0.12f, 0.2f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsOutsideRoundedCorners(x, y, width, height, cornerRadius))
                {
                    wonderTex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Check if in borders
                bool isBorder = (x < 4 || x >= width - 4 || y < 4 || y >= height - 4);
                bool inLeftPad = (x >= width * 0.12f && x < width * 0.42f && y >= 6 && y < height - 6);
                bool inRightPad = (x >= width * 0.58f && x < width * 0.88f && y >= 6 && y < height - 6);

                // Active glowing bumper lights: Left end and Right end
                float leftBumperDist = Vector2.Distance(new Vector2(x, y), new Vector2(10f, height * 0.5f));
                float rightBumperDist = Vector2.Distance(new Vector2(x, y), new Vector2(width - 10f, height * 0.5f));
                bool isBumper = (leftBumperDist < 3.5f || rightBumperDist < 3.5f);

                // Active glowing thrusters
                bool inLeftThruster = (x >= width * 0.25f - 8 && x < width * 0.25f + 8 && y < 4);
                bool inRightThruster = (x >= width * 0.75f - 8 && x < width * 0.75f + 8 && y < 4);
                bool inThrusterFlame = ((x >= width * 0.25f - 5 && x < width * 0.25f + 5 && y < 2) ||
                                       (x >= width * 0.75f - 5 && x < width * 0.75f + 5 && y < 2));

                // Neon circuits/rune lines running through the board
                bool isCircuitLine = (Mathf.Abs(y - height * 0.5f) <= 0.8f && x >= 12 && x < width - 12);
                // Diagonal rune patterns on the grip pads
                bool isRunePattern = false;
                if (inLeftPad)
                {
                    float lx = x - width * 0.12f;
                    float ly = y - 6;
                    if (Mathf.Abs(lx - ly * 3.5f) < 1f || Mathf.Abs((width * 0.3f - lx) - ly * 3.5f) < 1f)
                    {
                        isRunePattern = true;
                    }
                }
                if (inRightPad)
                {
                    float rx = x - width * 0.58f;
                    float ry = y - 6;
                    if (Mathf.Abs(rx - ry * 3.5f) < 1f || Mathf.Abs((width * 0.3f - rx) - ry * 3.5f) < 1f)
                    {
                        isRunePattern = true;
                    }
                }

                // Paint logic
                if (isBumper)
                {
                    wonderTex.SetPixel(x, y, neonMagenta);
                }
                else if (inThrusterFlame)
                {
                    wonderTex.SetPixel(x, y, glowCore);
                }
                else if (inLeftThruster || inRightThruster)
                {
                    wonderTex.SetPixel(x, y, neonCyan);
                }
                else if (isCircuitLine || isRunePattern)
                {
                    wonderTex.SetPixel(x, y, neonCyan);
                }
                else if (inLeftPad || inRightPad)
                {
                    // Dark cyber crystal texture with custom noise
                    float noise = Random.value * 0.05f;
                    wonderTex.SetPixel(x, y, crystalBar + new Color(0f, noise, noise * 1.5f, 0f));
                }
                else if (isBorder)
                {
                    wonderTex.SetPixel(x, y, Color.Lerp(neonCyan, cyberBase, 0.3f));
                }
                else
                {
                    wonderTex.SetPixel(x, y, cyberBase);
                }
            }
        }
        wonderTex.Apply();
        wonderHoverboardSprite = Sprite.Create(wonderTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
    }

    private bool IsOutsideRoundedCorners(int x, int y, int width, int height, float r)
    {
        if (x < r && y >= height - r)
        {
            if (Vector2.Distance(new Vector2(x, y), new Vector2(r, height - r)) > r) return true;
        }
        if (x < r && y < r)
        {
            if (Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) > r) return true;
        }
        if (x >= width - r && y >= height - r)
        {
            if (Vector2.Distance(new Vector2(x, y), new Vector2(width - r, height - r)) > r) return true;
        }
        if (x >= width - r && y < r)
        {
            if (Vector2.Distance(new Vector2(x, y), new Vector2(width - r, r)) > r) return true;
        }
        return false;
    }
}
