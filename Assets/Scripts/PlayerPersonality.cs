using UnityEngine;

/// <summary>
/// A code-driven procedural aesthetics script attached to the Player Graphics transform.
/// Spawns a cute dark faceplate and two glowing neon eyes at runtime.
/// Animates blinking, looking ahead in the direction of running, and reacting to jumps/falls
/// and landing squash impacts, giving the player capsule an adorable robot personality!
/// </summary>
public class PlayerPersonality : MonoBehaviour
{
    [Header("Eye Settings")]
    [SerializeField] private Color eyeColor = new Color(0.15f, 0.9f, 1f, 1.0f); // Neon Cyan Glow
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

    // Animations state
    private float blinkTimer;
    private float blinkProgress = 0f;
    private float currentLookShiftX = 0f;
    private Vector3 defaultEyeScale = new Vector3(0.08f, 0.08f, 1f);

    private void Start()
    {
        controller = GetComponentInParent<PlayerController2D>();
        if (controller != null)
        {
            parentRb = controller.GetComponent<Rigidbody2D>();
        }

        // Initialize blink timer
        blinkTimer = Random.Range(minBlinkInterval, maxBlinkInterval);

        // Spawn face
        SetupProceduralFace();
    }

    /// <summary>
    /// Spawns the faceplate and small circular eyes programmatically.
    /// </summary>
    private void SetupProceduralFace()
    {
        Sprite backgroundSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite knobSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // 1. Spawn Faceplate (Dark Translucent Grey Plate)
        faceplateObj = new GameObject("Faceplate");
        faceplateObj.transform.SetParent(this.transform, false);
        // Position slightly above center, and forward
        faceplateObj.transform.localPosition = new Vector3(0f, eyeHeightOffset, -0.02f);
        faceplateObj.transform.localScale = new Vector3(0.55f, 0.24f, 1f);

        var faceSr = faceplateObj.AddComponent<SpriteRenderer>();
        faceSr.sprite = backgroundSprite;
        faceSr.color = new Color(0.12f, 0.12f, 0.15f, 0.88f); // Dark translucent glass faceplate
        faceSr.sortingOrder = 11; // In front of capsule body (10)

        // 2. Spawn Left Eye
        leftEyeObj = new GameObject("LeftEye");
        leftEyeObj.transform.SetParent(this.transform, false);
        leftEyeObj.transform.localPosition = new Vector3(-eyeSpacing, eyeHeightOffset, -0.04f);
        leftEyeObj.transform.localScale = defaultEyeScale;

        leftEyeRenderer = leftEyeObj.AddComponent<SpriteRenderer>();
        leftEyeRenderer.sprite = knobSprite;
        leftEyeRenderer.color = eyeColor;
        leftEyeRenderer.sortingOrder = 12; // In front of faceplate

        // 3. Spawn Right Eye
        rightEyeObj = new GameObject("RightEye");
        rightEyeObj.transform.SetParent(this.transform, false);
        rightEyeObj.transform.localPosition = new Vector3(eyeSpacing, eyeHeightOffset, -0.04f);
        rightEyeObj.transform.localScale = defaultEyeScale;

        rightEyeRenderer = rightEyeObj.AddComponent<SpriteRenderer>();
        rightEyeRenderer.sprite = knobSprite;
        rightEyeRenderer.color = eyeColor;
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
            targetShift = Mathf.Clamp(hVel * lookShiftFactor, -maxLookShift, maxLookShift);
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

        // --- 3. Airborne & Impact Expressions ---
        float eyeScaleY = defaultEyeScale.y;
        float eyeScaleX = defaultEyeScale.x;

        if (isBlinking)
        {
            // Squeeze eyes to 0 height for blink
            eyeScaleY = 0f;
        }
        else if (!controller.IsGrounded)
        {
            // Mid-air: widen eyes in anticipation/excitement
            float vVel = parentRb.velocity.y;
            float widenFactor = Mathf.Clamp(vVel * 0.015f, -0.2f, 0.45f);
            
            // Widen height, squeeze width slightly
            eyeScaleY = defaultEyeScale.y * (1f + widenFactor);
            eyeScaleX = defaultEyeScale.x * (1f - widenFactor * 0.2f);
        }
        else
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

        // --- 4. Apply Position & Scale Updates ---
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
}
