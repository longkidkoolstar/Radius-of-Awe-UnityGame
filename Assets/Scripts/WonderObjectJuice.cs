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
    private bool wasInside = false;

    // Generated assets
    private Sprite glowSprite;
    private Sprite proceduralRuneSprite;
    private Sprite mundaneHoverboardSprite;
    private Sprite wonderHoverboardSprite;
    private Sprite mundanePassableWallSprite;
    private Sprite wonderPassableWallSprite;

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
                
                // Stretches tiled/sliced elements via scale to bypass Unity runtime tiling mesh bugs
                if (rootSr.drawMode == SpriteDrawMode.Tiled || rootSr.drawMode == SpriteDrawMode.Sliced)
                {
                    go.transform.localScale = new Vector3(rootSr.size.x, rootSr.size.y, 1f);
                    graphicsRenderer.drawMode = SpriteDrawMode.Simple;
                }
                else
                {
                    graphicsRenderer.drawMode = rootSr.drawMode;
                    graphicsRenderer.size = rootSr.size;
                    graphicsRenderer.tileMode = rootSr.tileMode;
                }
                
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
            if (graphicsChild != null)
            {
                graphicsChild.localScale = Vector3.one;
            }
            if (graphicsRenderer != null)
            {
                graphicsRenderer.sprite = mundaneHoverboardSprite;
                graphicsRenderer.color = Color.white;
            }
            customWonderSprite = wonderHoverboardSprite;
        }

        // Check if this object is a Passable Wall Barrier
        bool isPassableWall = gameObject.name.Contains("PassableWall");
        if (isPassableWall)
        {
            GeneratePassableWallSprites();
            if (graphicsChild != null)
            {
                graphicsChild.localScale = Vector3.one;
            }
            if (graphicsRenderer != null)
            {
                graphicsRenderer.sprite = mundanePassableWallSprite;
                graphicsRenderer.color = Color.white;
            }
            customWonderSprite = wonderPassableWallSprite;
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

        if (wonderObject != null)
        {
            wasInside = wonderObject.IsInWonderZone;
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

        // Match local scale based on parent bounds (inherited from parent transform scale if tiled/sliced)
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

        // Transition SFX triggers
        if (inside && !wasInside)
        {
            AudioManager.PlayWonderObjectEnter(transform.position);
        }
        else if (!inside && wasInside)
        {
            // Play a soft thud/deactivate sound
            AudioManager.PlayButtonRelease(transform.position);
        }
        wasInside = inside;

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

    /// <summary>
    /// Generates high-fidelity industrial gate and glowing cyber phase-gate sprites for Passable Walls.
    /// Matches the actual BoxCollider2D bounds.
    /// </summary>
    private void GeneratePassableWallSprites()
    {
        Vector2 colSize = new Vector2(0.6f, 2.0f);
        var boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            colSize = boxCol.size;
        }

        float ppu = 100f;
        int width = Mathf.Max(32, Mathf.RoundToInt(colSize.x * ppu));
        int height = Mathf.Max(16, Mathf.RoundToInt(colSize.y * ppu));

        // 1. Generate Mundane PassableWall Sprite (Industrial Heavy Chainlink Gate)
        Texture2D mundaneTex = new Texture2D(width, height);
        mundaneTex.filterMode = FilterMode.Bilinear;
        mundaneTex.wrapMode = TextureWrapMode.Clamp;

        // Slate/Industrial Colors
        Color borderDark = new Color(0.12f, 0.14f, 0.16f, 1f);
        Color borderLight = new Color(0.38f, 0.42f, 0.46f, 1f);
        Color baseGrey = new Color(0.24f, 0.26f, 0.28f, 1f);
        Color wireMeshColor = new Color(0.16f, 0.18f, 0.2f, 1f);
        Color wireMeshHighlight = new Color(0.35f, 0.38f, 0.4f, 1f);
        Color rivetColor = new Color(0.7f, 0.72f, 0.75f, 1f);
        Color hazardYellow = new Color(0.85f, 0.68f, 0.08f, 1f);
        Color hazardBlack = new Color(0.08f, 0.09f, 0.1f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Solid border frames on left, right, top, and bottom
                bool isLeftFrame = (x < 6);
                bool isRightFrame = (x >= width - 6);
                bool isTopFrame = (y >= height - 8);
                bool isBottomFrame = (y < 8);
                bool isFrame = isLeftFrame || isRightFrame || isTopFrame || isBottomFrame;

                bool isInner = !isFrame;

                // Diagonal cautionary stripes on top and bottom frame bezels
                bool isHazardTop = isTopFrame && (x >= 6 && x < width - 6);
                bool isHazardBottom = isBottomFrame && (x >= 6 && x < width - 6);

                // CRISP DIAGONAL METAL CHAINLINK MESH
                bool isChainlink = false;
                bool isChainlinkHighlight = false;
                if (isInner)
                {
                    // Draw repeating diagonal diamond grid lines
                    int gridSpacing = 16;
                    int thickness = 2;
                    
                    bool line1 = Mathf.Abs((x - y) % gridSpacing) < thickness;
                    bool line2 = Mathf.Abs((x + y) % gridSpacing) < thickness;
                    
                    isChainlink = line1 || line2;
                    
                    // Add light reflections at diagonal edges to make wire 3D
                    if (isChainlink)
                    {
                        isChainlinkHighlight = Mathf.Abs((x - y) % gridSpacing) == 0 || Mathf.Abs((x + y) % gridSpacing) == 0;
                    }
                }

                // Support braces at 1/3 and 2/3 heights
                bool inHorizontalBrace = isInner && (Mathf.Abs(y - height * 0.33f) < 2 || Mathf.Abs(y - height * 0.66f) < 2);

                // Frame assembly rivets
                bool isRivet = false;
                if (isFrame)
                {
                    if ((x == 3 || x == width - 4) && (y % 30 == 15))
                    {
                        isRivet = true;
                    }
                }

                // Paint logic
                if (isRivet)
                {
                    mundaneTex.SetPixel(x, y, rivetColor);
                }
                else if (isHazardTop || isHazardBottom)
                {
                    // Diagonal stripes pattern matching
                    if ((x + y) % 14 < 7)
                    {
                        mundaneTex.SetPixel(x, y, hazardYellow);
                    }
                    else
                    {
                        mundaneTex.SetPixel(x, y, hazardBlack);
                    }
                }
                else if (inHorizontalBrace)
                {
                    mundaneTex.SetPixel(x, y, borderLight);
                }
                else if (isChainlinkHighlight)
                {
                    mundaneTex.SetPixel(x, y, wireMeshHighlight);
                }
                else if (isChainlink)
                {
                    mundaneTex.SetPixel(x, y, wireMeshColor);
                }
                else if (isInner)
                {
                    // Translucent dark opening between bars for gritted visibility
                    float shadowNoise = Random.value * 0.03f;
                    mundaneTex.SetPixel(x, y, new Color(0.06f + shadowNoise, 0.06f + shadowNoise, 0.08f, 0.85f));
                }
                else if (isLeftFrame || isRightFrame)
                {
                    if (isLeftFrame && x < 2)
                        mundaneTex.SetPixel(x, y, borderLight);
                    else if (isRightFrame && x >= width - 2)
                        mundaneTex.SetPixel(x, y, borderDark);
                    else
                        mundaneTex.SetPixel(x, y, baseGrey);
                }
                else
                {
                    mundaneTex.SetPixel(x, y, baseGrey);
                }
            }
        }
        mundaneTex.Apply();
        mundanePassableWallSprite = Sprite.Create(mundaneTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);

        // 2. Generate Wonder PassableWall Sprite (Glowing Holographic energy Lattice)
        Texture2D wonderTex = new Texture2D(width, height);
        wonderTex.filterMode = FilterMode.Bilinear;
        wonderTex.wrapMode = TextureWrapMode.Clamp;

        Color cyberBorder = new Color(0.9f, 0.15f, 0.85f, 1f); // Hot pink border
        Color energyCyan = new Color(0f, 0.85f, 1.0f, 1f); // Glowing cyber cyan
        Color energyCyanSoft = new Color(0f, 0.65f, 0.9f, 0.25f); // Translucent energy mist
        Color energyNodeCore = new Color(0.8f, 0.98f, 1.0f, 1f); // Glowing white energy node

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isLeftFrame = (x < 6);
                bool isRightFrame = (x >= width - 6);
                bool isTopFrame = (y >= height - 8);
                bool isBottomFrame = (y < 8);
                bool isFrame = isLeftFrame || isRightFrame || isTopFrame || isBottomFrame;

                bool isInner = !isFrame;

                // Glowing energy nozzles/nodes inside the frame
                bool isEnergyNode = false;
                if (isFrame)
                {
                    if ((x >= 2 && x < width - 2) && (y % 40 == 20))
                    {
                        isEnergyNode = true;
                    }
                }

                // GLOWING NEON ENERGY LATTICE
                bool isLatticeLine = false;
                bool isLatticeNode = false;
                if (isInner)
                {
                    int latticeSpacing = 16;
                    int thickness = 2;

                    // Calculate grid line intersections
                    int dist1 = (x - y) % latticeSpacing;
                    int dist2 = (x + y) % latticeSpacing;

                    // Handle wrapping coordinates for modular arithmetic in negative domains safely
                    if (dist1 < 0) dist1 += latticeSpacing;
                    if (dist2 < 0) dist2 += latticeSpacing;

                    bool line1 = dist1 < thickness || dist1 > latticeSpacing - thickness;
                    bool line2 = dist2 < thickness || dist2 > latticeSpacing - thickness;

                    isLatticeLine = line1 || line2;

                    // Spawn circular nodes at the crossings of the energy diagonal lines
                    if (line1 && line2)
                    {
                        isLatticeNode = true;
                    }
                }

                // Paint logic
                if (isEnergyNode)
                {
                    wonderTex.SetPixel(x, y, energyNodeCore);
                }
                else if (isLatticeNode)
                {
                    wonderTex.SetPixel(x, y, energyNodeCore);
                }
                else if (isLatticeLine)
                {
                    wonderTex.SetPixel(x, y, energyCyan);
                }
                else if (isInner)
                {
                    // Shimmering horizontal wave phase scanlines
                    float scanline = Mathf.Sin((float)y * 0.9f) * 0.18f + 0.32f;
                    
                    // Transparent phase-shield energy field
                    wonderTex.SetPixel(x, y, new Color(energyCyanSoft.r, energyCyanSoft.g, energyCyanSoft.b, scanline * 0.45f));
                }
                else
                {
                    // Holographic border pulsing slightly with vertical index coordinates
                    float borderPulse = Mathf.Sin((float)y * 0.08f) * 0.25f + 0.75f;
                    wonderTex.SetPixel(x, y, Color.Lerp(cyberBorder, energyCyan, borderPulse * 0.35f));
                }
            }
        }
        wonderTex.Apply();
        wonderPassableWallSprite = Sprite.Create(wonderTex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
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
