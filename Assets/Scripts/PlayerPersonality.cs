using UnityEngine;

/// <summary>
/// A code-driven procedural aesthetics script attached to the Player Graphics transform.
/// Spawns a gorgeous large cybernetic visor and two big cartoony glowing eyes.
/// Dynamically animates blinking, shifting focus direction, and swaps expressions in real-time
/// (happy arches when jumping, surprised pupils when falling, flat blinks when squished)
/// giving the player capsule an adorable, expressive cartoon robot personality!
/// </summary>
public class PlayerPersonality : MonoBehaviour
{
    [Header("Eye Settings")]
    [SerializeField] private float eyeHeightOffset = 0.28f;
    [SerializeField] private float eyeSpacing = 0.12f;
    
    [Header("Blinking Settings")]
    [SerializeField] private float minBlinkInterval = 2.5f;
    [SerializeField] private float maxBlinkInterval = 5f;

    [Header("Look Shifting")]
    [Tooltip("How much the eyes slide left/right when running.")]
    [SerializeField] private float lookShiftFactor = 0.015f;
    [SerializeField] private float maxLookShift = 0.08f;

    // References
    private PlayerController2D controller;
    private Rigidbody2D parentRb;

    // Spawned parts
    private GameObject faceplateObj;
    private GameObject leftEyeObj;
    private GameObject rightEyeObj;
    private SpriteRenderer leftEyeRenderer;
    private SpriteRenderer rightEyeRenderer;

    // Generated assets
    private Sprite visorSprite;
    private Sprite eyeSpriteNormal;
    private Sprite eyeSpriteHappy;
    private Sprite eyeSpriteSurprised;
    private Sprite eyeSpriteBlink;

    // Animations state
    private float blinkTimer;
    private float blinkProgress = 0f;
    private float currentLookShiftX = 0f;
    private Vector3 defaultEyeScale = new Vector3(0.18f, 0.18f, 1f); // Large cartoony eyes!

    private void Start()
    {
        controller = GetComponentInParent<PlayerController2D>();
        if (controller != null)
        {
            parentRb = controller.GetComponent<Rigidbody2D>();
        }

        // Initialize blink timer
        blinkTimer = Random.Range(minBlinkInterval, maxBlinkInterval);

        // Generate procedural visor and cartoony expression sprites in memory
        GenerateProceduralFaceSprites();

        // Spawn face (static visor base and large animated eyes)
        SetupProceduralFace();
    }

    /// <summary>
    /// Spawns the beautiful glowing robotic eyes, aligned perfectly within the visor painted on the body.
    /// </summary>
    private void SetupProceduralFace()
    {
        // 1. Spawn Faceplate (Dark Visor Plate)
        faceplateObj = new GameObject("Faceplate");
        faceplateObj.transform.SetParent(this.transform, false);
        faceplateObj.transform.localPosition = new Vector3(0f, eyeHeightOffset, -0.02f);
        faceplateObj.transform.localScale = new Vector3(0.65f, 0.32f, 1f); // Visor is large and cartoony!

        var faceSr = faceplateObj.AddComponent<SpriteRenderer>();
        faceSr.sprite = visorSprite;
        faceSr.sortingOrder = 11; // In front of capsule body (10)

        // 2. Spawn Left Eye
        leftEyeObj = new GameObject("LeftEye");
        leftEyeObj.transform.SetParent(this.transform, false);
        leftEyeObj.transform.localPosition = new Vector3(-eyeSpacing, eyeHeightOffset, -0.04f);
        leftEyeObj.transform.localScale = defaultEyeScale;

        leftEyeRenderer = leftEyeObj.AddComponent<SpriteRenderer>();
        leftEyeRenderer.sprite = eyeSpriteNormal;
        leftEyeRenderer.color = Color.white;
        leftEyeRenderer.sortingOrder = 12; // In front of faceplate

        // 3. Spawn Right Eye
        rightEyeObj = new GameObject("RightEye");
        rightEyeObj.transform.SetParent(this.transform, false);
        rightEyeObj.transform.localPosition = new Vector3(eyeSpacing, eyeHeightOffset, -0.04f);
        rightEyeObj.transform.localScale = defaultEyeScale;

        rightEyeRenderer = rightEyeObj.AddComponent<SpriteRenderer>();
        rightEyeRenderer.sprite = eyeSpriteNormal;
        rightEyeRenderer.color = Color.white;
        rightEyeRenderer.sortingOrder = 12; // In front of faceplate
    }

    private void Update()
    {
        if (controller == null || parentRb == null) return;

        // --- 1. Procedural Look-Ahead Shifting ---
        float targetShift = 0f;
        float hVel = parentRb.velocity.x;
        
        if (Mathf.Abs(hVel) > 0.15f)
        {
            // Shift eyes forward in direction of travel
            // Multiply by the parent's facing sign to compensate for scale.x mirroring when flipped left
            float facingSign = Mathf.Sign(transform.lossyScale.x);
            targetShift = Mathf.Clamp(hVel * lookShiftFactor * facingSign, -maxLookShift, maxLookShift);
        }
        
        currentLookShiftX = Mathf.Lerp(currentLookShiftX, targetShift, Time.deltaTime * 10f);

        // --- 2. Dynamic Blinking Solver ---
        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f && blinkProgress <= 0f)
        {
            blinkProgress = 1.0f; // Trigger blink cycle
        }

        bool isBlinking = false;
        if (blinkProgress > 0f)
        {
            blinkProgress -= Time.deltaTime * 10f; // Takes 0.1 seconds
            isBlinking = true;

            if (blinkProgress <= 0f)
            {
                blinkTimer = Random.Range(minBlinkInterval, maxBlinkInterval);
            }
        }

        // --- 3. Dynamic Sprite Expression Swapping ---
        Sprite targetEyeSprite = eyeSpriteNormal;

        if (isBlinking)
        {
            targetEyeSprite = eyeSpriteBlink;
        }
        else if (!controller.IsGrounded)
        {
            float vVel = parentRb.velocity.y;
            if (vVel > 1.5f)
            {
                // Jumping upward: happy arches!
                targetEyeSprite = eyeSpriteHappy;
            }
            else if (vVel < -2.2f)
            {
                // Falling fast: surprised open eyes!
                targetEyeSprite = eyeSpriteSurprised;
            }
        }
        else
        {
            // Grounded: check squash
            float parentScaleY = transform.localScale.y;
            if (parentScaleY < 0.45f) // Squished down heavily on landing impact
            {
                targetEyeSprite = eyeSpriteBlink;
            }
        }

        // Apply sprite to renderers
        if (leftEyeRenderer != null) leftEyeRenderer.sprite = targetEyeSprite;
        if (rightEyeRenderer != null) rightEyeRenderer.sprite = targetEyeSprite;

        // --- 4. Airborne & Impact Squash/Stretch Scaling ---
        float eyeScaleY = defaultEyeScale.y;
        float eyeScaleX = defaultEyeScale.x;

        if (!controller.IsGrounded && !isBlinking)
        {
            // Mid-air: widen eyes in anticipation/excitement
            float vVel = parentRb.velocity.y;
            float widenFactor = Mathf.Clamp(vVel * 0.015f, -0.2f, 0.45f);
            
            // Widen height, squeeze width slightly
            eyeScaleY = defaultEyeScale.y * (1f + widenFactor);
            eyeScaleX = defaultEyeScale.x * (1f - widenFactor * 0.2f);
        }
        else if (controller.IsGrounded)
        {
            // Grounded: match player squash/stretch from parent Graphics localScale
            // On impact landing, eyes squeeze flat along with player capsule
            float parentScaleY = transform.localScale.y;
            if (parentScaleY < 0.5f) // Squished down
            {
                float squishFactor = (0.5f - parentScaleY) / 0.5f;
                // Squeeze eyes vertically
                eyeScaleY = defaultEyeScale.y * (1f - squishFactor * 0.5f);
                eyeScaleX = defaultEyeScale.x * (1f + squishFactor * 0.25f);
            }
        }

        // --- 5. Apply Position & Scale Updates ---
        Vector3 leftPos = new Vector3(-eyeSpacing + currentLookShiftX, eyeHeightOffset, -0.04f);
        Vector3 rightPos = new Vector3(eyeSpacing + currentLookShiftX, eyeHeightOffset, -0.04f);

        if (leftEyeObj != null)
        {
            leftEyeObj.transform.localPosition = leftPos;
            leftEyeObj.transform.localScale = new Vector3(eyeScaleX, eyeScaleY, 1f);
        }

        if (rightEyeObj != null)
        {
            rightEyeObj.transform.localPosition = rightPos;
            rightEyeObj.transform.localScale = new Vector3(eyeScaleX, eyeScaleY, 1f);
        }
    }

    /// <summary>
    /// Generates the static faceplate visor and 4 animated eye sprites dynamically in memory.
    /// </summary>
    private void GenerateProceduralFaceSprites()
    {
        // 1. Visor Screen Sprite (64x32, PPU = 32)
        int vw = 64;
        int vh = 32;
        Texture2D visorTex = new Texture2D(vw, vh);
        visorTex.filterMode = FilterMode.Bilinear;
        visorTex.wrapMode = TextureWrapMode.Clamp;

        Color visorBg = new Color(0.08f, 0.09f, 0.13f, 0.88f); // Semi-transparent dark glass
        Color visorCyan = new Color(0f, 0.85f, 1f, 1f); // Sleek cyber cyan outline

        float vr = 8f; // Corner radius for rounded visor rectangle
        for (int y = 0; y < vh; y++)
        {
            for (int x = 0; x < vw; x++)
            {
                // Rounded corner check
                bool outside = false;
                if (x < vr && y < vr && Vector2.Distance(new Vector2(x, y), new Vector2(vr, vr)) > vr) outside = true;
                if (x < vr && y >= vh - vr && Vector2.Distance(new Vector2(x, y), new Vector2(vr, vh - vr)) > vr) outside = true;
                if (x >= vw - vr && y < vr && Vector2.Distance(new Vector2(x, y), new Vector2(vw - vr, vr)) > vr) outside = true;
                if (x >= vw - vr && y >= vh - vr && Vector2.Distance(new Vector2(x, y), new Vector2(vw - vr, vh - vr)) > vr) outside = true;

                if (outside)
                {
                    visorTex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Check outline for glowing trim
                bool isBorder = false;
                if (x == 0 || x == vw - 1 || y == 0 || y == vh - 1) isBorder = true;
                if (x < vr && y < vr && Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(vr, vr)) - vr) < 1.2f) isBorder = true;
                if (x < vr && y >= vh - vr && Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(vr, vh - vr)) - vr) < 1.2f) isBorder = true;
                if (x >= vw - vr && y < vr && Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(vw - vr, vr)) - vr) < 1.2f) isBorder = true;
                if (x >= vw - vr && y >= vh - vr && Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(vw - vr, vh - vr)) - vr) < 1.2f) isBorder = true;

                if (isBorder)
                {
                    visorTex.SetPixel(x, y, visorCyan);
                }
                else
                {
                    visorTex.SetPixel(x, y, visorBg);
                }
            }
        }
        visorTex.Apply();
        visorSprite = Sprite.Create(visorTex, new Rect(0, 0, vw, vh), new Vector2(0.5f, 0.5f), 32f);

        // 2. Normal Eye Sprite (32x32, PPU = 80)
        int ew = 32, eh = 32;
        Texture2D normalTex = new Texture2D(ew, eh);
        normalTex.filterMode = FilterMode.Bilinear;
        normalTex.wrapMode = TextureWrapMode.Clamp;
        Color neonCyan = new Color(0f, 0.9f, 1f, 1f);

        for (int y = 0; y < eh; y++)
        {
            for (int x = 0; x < ew; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                bool isGlint = (Vector2.Distance(new Vector2(x, y), new Vector2(21f, 21f)) < 2f);

                if (dist > 14f)
                {
                    normalTex.SetPixel(x, y, Color.clear);
                }
                else if (isGlint)
                {
                    normalTex.SetPixel(x, y, Color.white);
                }
                else if (dist <= 5f)
                {
                    // Large cartoony dark pupil
                    normalTex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.08f, 1f));
                }
                else
                {
                    // Glowing cyan iris
                    normalTex.SetPixel(x, y, neonCyan);
                }
            }
        }
        normalTex.Apply();
        eyeSpriteNormal = Sprite.Create(normalTex, new Rect(0, 0, ew, eh), new Vector2(0.5f, 0.5f), 80f);

        // 3. Happy Eye Sprite (32x32, PPU = 80)
        Texture2D happyTex = new Texture2D(ew, eh);
        happyTex.filterMode = FilterMode.Bilinear;
        happyTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < eh; y++)
        {
            for (int x = 0; x < ew; x++)
            {
                // Arch pointing upward: y = center_y + cos(x)
                float dx = (x - 15.5f);
                float arcY = 14f + Mathf.Cos(dx * 0.12f) * 7f;
                float distToArc = Mathf.Abs(y - arcY);

                if (distToArc < 3.2f && x >= 4 && x < 28)
                {
                    happyTex.SetPixel(x, y, neonCyan);
                }
                else
                {
                    happyTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        happyTex.Apply();
        eyeSpriteHappy = Sprite.Create(happyTex, new Rect(0, 0, ew, eh), new Vector2(0.5f, 0.5f), 80f);

        // 4. Surprised Eye Sprite (32x32, PPU = 80)
        Texture2D surprisedTex = new Texture2D(ew, eh);
        surprisedTex.filterMode = FilterMode.Bilinear;
        surprisedTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < eh; y++)
        {
            for (int x = 0; x < ew; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                bool isGlint = (Vector2.Distance(new Vector2(x, y), new Vector2(21f, 21f)) < 1.5f);

                if (dist > 14f)
                {
                    surprisedTex.SetPixel(x, y, Color.clear);
                }
                else if (isGlint)
                {
                    surprisedTex.SetPixel(x, y, Color.white);
                }
                else if (dist <= 3.2f)
                {
                    // Tiny surprised black pupil
                    surprisedTex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.08f, 1f));
                }
                else if (dist > 10.5f)
                {
                    // Cyan outer ring
                    surprisedTex.SetPixel(x, y, neonCyan);
                }
                else
                {
                    surprisedTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        surprisedTex.Apply();
        eyeSpriteSurprised = Sprite.Create(surprisedTex, new Rect(0, 0, ew, eh), new Vector2(0.5f, 0.5f), 80f);

        // 5. Blink/Squint Eye Sprite (32x32, PPU = 80)
        Texture2D blinkTex = new Texture2D(ew, eh);
        blinkTex.filterMode = FilterMode.Bilinear;
        blinkTex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < eh; y++)
        {
            for (int x = 0; x < ew; x++)
            {
                bool inBlink = (y >= 13 && y < 19 && x >= 4 && x < 28);
                if (inBlink)
                {
                    blinkTex.SetPixel(x, y, neonCyan);
                }
                else
                {
                    blinkTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        blinkTex.Apply();
        eyeSpriteBlink = Sprite.Create(blinkTex, new Rect(0, 0, ew, eh), new Vector2(0.5f, 0.5f), 80f);
    }
}
