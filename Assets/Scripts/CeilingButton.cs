using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// A ceiling pressure plate. Place it on the ceiling of a level.
/// Triggers when a floaty object (or the player) pushes upward against it.
/// Features a smooth physical compression animation, color shifts, and UnityEvent hooks.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CeilingButton : MonoBehaviour
{
    [Header("Activation Rules")]
    [Tooltip("If true, only objects containing a WonderObject script (e.g. Floaty Crates) can activate it.")]
    [SerializeField] private bool requireWonderObject = true;
    [Tooltip("If true, the activating object must be currently inside the Wonder Zone (so weightless).")]
    [SerializeField] private bool requireActiveWonderZoneState = true;

    [Header("Trigger Leeway")]
    [Tooltip("Extra width added to the trigger collider at runtime to give the player more leeway.")]
    [SerializeField] private float extraTriggerWidth = 0.4f;
    [Tooltip("Extra height added to the trigger collider extending downwards at runtime.")]
    [SerializeField] private float extraTriggerHeightDown = 0.3f;

    [Header("Procedural Visuals")]
    [Tooltip("The moving child transform representing the button cap. Auto-finds child named 'Cap' if empty.")]
    [SerializeField] private Transform movingCap;
    [Tooltip("How far upward the cap moves when pressed.")]
    [SerializeField] private float pressDepth = 0.16f;
    [Tooltip("Visual transition speed for pushing and color shifting.")]
    [SerializeField] private float transitionSpeed = 9f;
    [Tooltip("Default color when released.")]
    [SerializeField] private Color normalColor = new Color(0.85f, 0.25f, 0.25f, 1.0f); // Sleek dull red
    [Tooltip("Glowing color when pressed.")]
    [SerializeField] private Color pressedColor = new Color(0.2f, 0.85f, 1.0f, 1.0f);   // Neon Cyan Glow

    [Header("Activation Events")]
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReleased;

    private BoxCollider2D triggerCollider;
    private SpriteRenderer capRenderer;
    private Vector3 releasedLocalPos;
    private Vector3 pressedLocalPos;
    private int overlappingObjectsCount = 0;
    private bool isPressed = false;
    private ElasticWobble capWobbler;
    private List<Collider2D> overlappingColliders = new List<Collider2D>();

    private Sprite baseSprite;
    private Sprite capSprite;

    /// <summary>Returns true if the pressure plate is currently pressed.</summary>
    public bool IsPressed => isPressed;

    public UnityEvent OnPressed => onPressed;
    public UnityEvent OnReleased => onReleased;

    private void Start()
    {
        requireActiveWonderZoneState = false; // Disable Wonder Zone requirement so plates trigger on physical contact
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        // Apply runtime leeway trigger size expansion
        if (triggerCollider != null)
        {
            Vector2 size = triggerCollider.size;
            Vector2 offset = triggerCollider.offset;
            triggerCollider.size = new Vector2(size.x + extraTriggerWidth, size.y + extraTriggerHeightDown);
            triggerCollider.offset = new Vector2(offset.x, offset.y - extraTriggerHeightDown * 0.5f);
        }

        // Auto-detect button cap child
        if (movingCap == null) movingCap = transform.Find("Cap");
        if (movingCap == null) movingCap = transform;

        // Auto-detect or attach ElasticWobble component to movingCap
        if (movingCap != null)
        {
            capWobbler = movingCap.GetComponent<ElasticWobble>();
            if (capWobbler == null)
            {
                capWobbler = movingCap.gameObject.AddComponent<ElasticWobble>();
            }
            
            // Reset scale of moving cap to ensure non-distorted pixel-perfect ratio (1.6 units by 0.3 units)
            movingCap.localScale = Vector3.one;
        }

        releasedLocalPos = movingCap.localPosition;
        pressedLocalPos = releasedLocalPos + new Vector3(0f, pressDepth, 0f); // Compress upward along local Y

        capRenderer = movingCap.GetComponent<SpriteRenderer>();
        if (capRenderer == null) capRenderer = GetComponentInChildren<SpriteRenderer>();

        // Generate procedural textures and sprites
        GenerateProceduralSprites();

        if (capRenderer != null)
        {
            capRenderer.sprite = capSprite;
            capRenderer.color = normalColor;
        }

        // Spawn/configure static Base mount if it doesn't exist
        Transform baseTrans = transform.Find("Base");
        if (baseTrans == null)
        {
            GameObject baseObj = new GameObject("Base");
            baseObj.transform.SetParent(this.transform, false);
            // Place slightly behind the cap and flat against the ceiling
            baseObj.transform.localPosition = new Vector3(0f, 0.1f, 0.1f);
            baseObj.transform.localScale = Vector3.one;

            var baseSr = baseObj.AddComponent<SpriteRenderer>();
            baseSr.sprite = baseSprite;
            baseSr.sortingOrder = (capRenderer != null) ? capRenderer.sortingOrder - 1 : 1;
        }
    }

    private void Update()
    {
        // Smoothly slide the button cap to pressed/released position
        Vector3 targetPos = isPressed ? pressedLocalPos : releasedLocalPos;
        movingCap.localPosition = Vector3.Lerp(movingCap.localPosition, targetPos, Time.deltaTime * transitionSpeed);

        // Smoothly morph color
        if (capRenderer != null)
        {
            Color targetColor = isPressed ? pressedColor : normalColor;
            capRenderer.color = Color.Lerp(capRenderer.color, targetColor, Time.deltaTime * transitionSpeed);
        }

        // Clean up any null, disabled, or inactive colliders from the overlapping list
        overlappingColliders.RemoveAll(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        // Dynamically evaluate if any overlapping object satisfies the collision rules
        bool shouldBePressed = false;
        int activeOverlapCount = 0;
        foreach (var col in overlappingColliders)
        {
            if (EvaluateCollision(col))
            {
                shouldBePressed = true;
                activeOverlapCount++;
            }
        }
        overlappingObjectsCount = activeOverlapCount;

        EvaluateButtonState(shouldBePressed);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.isTrigger) return;
        if (!overlappingColliders.Contains(collision))
        {
            overlappingColliders.Add(collision);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (overlappingColliders.Contains(collision))
        {
            overlappingColliders.Remove(collision);
        }
    }

    /// <summary>
    /// Evaluates if the colliding object meets all activation requirements.
    /// </summary>
    private bool EvaluateCollision(Collider2D collision)
    {
        if (collision == null || collision.isTrigger) return false;

        var wo = collision.GetComponent<WonderObject>();
        if (requireWonderObject && wo == null) return false;

        if (requireActiveWonderZoneState && wo != null && !wo.IsInWonderZone) return false;

        return true;
    }

    private void EvaluateButtonState(bool shouldBePressed)
    {
        if (shouldBePressed && !isPressed)
        {
            isPressed = true;
            onPressed?.Invoke();

            // Trigger visual squash wobble
            if (capWobbler != null)
            {
                capWobbler.TriggerWobble(new Vector3(0.25f, -0.32f, 0f), 18f, 5.0f);
            }

            // Give the button press some weight with a minor screenshake click
            if (CameraController2D.Instance != null)
            {
                CameraController2D.Instance.TriggerShake(0.12f, 0.08f);
            }

            // Play procedural spatialized button press sound
            AudioManager.PlayButtonPress(transform.position);
        }
        else if (!shouldBePressed && isPressed)
        {
            isPressed = false;
            onReleased?.Invoke();

            // Trigger visual release recoil wobble
            if (capWobbler != null)
            {
                capWobbler.TriggerWobble(new Vector3(-0.18f, 0.22f, 0f), 15f, 4.5f);
            }

            // Play procedural spatialized button release sound
            AudioManager.PlayButtonRelease(transform.position);
        }
    }

    /// <summary>
    /// Programmatically generates a gorgeous mechanical casing Base mount
    /// and a hazard-striped heavy pressure plate Cap with a central light bar.
    /// </summary>
    private void GenerateProceduralSprites()
    {
        // 1. Generate static Base Mount Sprite (fits wider 2.4 units)
        int baseWidth = 192;
        int baseHeight = 16;
        float ppu = 80f;
        Texture2D baseTex = new Texture2D(baseWidth, baseHeight);
        baseTex.filterMode = FilterMode.Bilinear;
        baseTex.wrapMode = TextureWrapMode.Clamp;

        Color baseMetal = new Color(0.18f, 0.2f, 0.23f, 1f);
        Color baseBezel = new Color(0.12f, 0.13f, 0.15f, 1f);
        Color baseShadow = new Color(0.08f, 0.09f, 0.1f, 1f);
        Color baseRivet = new Color(0.6f, 0.65f, 0.7f, 1f);

        for (int y = 0; y < baseHeight; y++)
        {
            for (int x = 0; x < baseWidth; x++)
            {
                // Socket slot in the middle (recessed groove)
                bool inSocket = (x >= 32 && x < 160 && y < 12);
                bool isBorder = (x < 3 || x >= baseWidth - 3 || y >= baseHeight - 3);

                // Rivets on the edges
                bool isLeftRivet = (Mathf.Abs(x - 12) < 2f && Mathf.Abs(y - 8) < 2f);
                bool isRightRivet = (Mathf.Abs(x - 180) < 2f && Mathf.Abs(y - 8) < 2f);

                if (isLeftRivet || isRightRivet)
                {
                    baseTex.SetPixel(x, y, baseRivet);
                }
                else if (inSocket)
                {
                    baseTex.SetPixel(x, y, baseShadow);
                }
                else if (isBorder)
                {
                    baseTex.SetPixel(x, y, baseBezel);
                }
                else
                {
                    baseTex.SetPixel(x, y, baseMetal);
                }
            }
        }
        baseTex.Apply();
        baseSprite = Sprite.Create(baseTex, new Rect(0, 0, baseWidth, baseHeight), new Vector2(0.5f, 0.5f), ppu);

        // 2. Generate button Cap Sprite with hazard stripes and crystal bar
        int capWidth = 128;
        int capHeight = 24;
        Texture2D capTex = new Texture2D(capWidth, capHeight);
        capTex.filterMode = FilterMode.Bilinear;
        capTex.wrapMode = TextureWrapMode.Clamp;

        Color capMetal = new Color(0.35f, 0.38f, 0.42f, 1f);
        Color capBezel = new Color(0.2f, 0.22f, 0.24f, 1f);
        Color capHighlight = new Color(0.55f, 0.58f, 0.62f, 1f);
        Color hazardYellow = new Color(0.85f, 0.68f, 0.08f, 1f);
        Color hazardBlack = new Color(0.08f, 0.09f, 0.1f, 1f);
        Color glassBackground = new Color(0.12f, 0.13f, 0.15f, 1f);

        for (int y = 0; y < capHeight; y++)
        {
            for (int x = 0; x < capWidth; x++)
            {
                // Bevel check
                bool isBorder = (x < 3 || x >= capWidth - 3 || y < 3 || y >= capHeight - 3);
                bool isTopHighlight = (y >= capHeight - 2 && x >= 3 && x < capWidth - 3);

                // Hazard Slopes (left and right edges)
                bool inLeftHazard = (x >= 4 && x < 28 && y >= 3 && y < capHeight - 3);
                bool inRightHazard = (x >= 100 && x < 124 && y >= 3 && y < capHeight - 3);

                // Center Emissive Glass Bar
                bool inGlassSocket = (x >= 36 && x < 92 && y >= 4 && y < capHeight - 4);
                bool inGlassBar = (x >= 40 && x < 88 && y >= 6 && y < capHeight - 6);

                if (inGlassBar)
                {
                    // Frosted white glass with a glint highlight at top
                    if (y == capHeight - 7)
                    {
                        capTex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.95f));
                    }
                    else
                    {
                        capTex.SetPixel(x, y, new Color(0.85f, 0.9f, 0.95f, 0.8f));
                    }
                }
                else if (inGlassSocket)
                {
                    capTex.SetPixel(x, y, glassBackground);
                }
                else if (inLeftHazard || inRightHazard)
                {
                    // Yellow and black warning stripes at 45 degrees
                    if ((x + y) % 10 < 5)
                    {
                        capTex.SetPixel(x, y, hazardYellow);
                    }
                    else
                    {
                        capTex.SetPixel(x, y, hazardBlack);
                    }
                }
                else if (isTopHighlight)
                {
                    capTex.SetPixel(x, y, capHighlight);
                }
                else if (isBorder)
                {
                    capTex.SetPixel(x, y, capBezel);
                }
                else
                {
                    capTex.SetPixel(x, y, capMetal);
                }
            }
        }
        capTex.Apply();
        capSprite = Sprite.Create(capTex, new Rect(0, 0, capWidth, capHeight), new Vector2(0.5f, 0.5f), ppu);
    }
}
