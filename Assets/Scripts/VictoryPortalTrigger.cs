using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Replaces the old portal with a stunning Dimensional Drift Transition!
/// Handles spatial warping effects, time dilation, programmatic letterbox bars,
/// beautiful spaced-out typographic overlays, and bioluminescent stardust particles!
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class VictoryPortalTrigger : MonoBehaviour
{
    [Header("Swirl & Animation")]
    [Tooltip("Constant speed (in degrees per second) at which the portal swirls.")]
    [SerializeField] private float swirlSpeed = 85f;
    [Tooltip("How much the portal pulses in scale.")]
    [SerializeField] private float pulseAmount = 0.08f;
    [Tooltip("Frequency speed of the breathing pulse.")]
    [SerializeField] private float pulseFrequency = 2.5f;

    [Header("Dimensional Drift Settings")]
    [Tooltip("Total duration of the zero-gravity cinematic drift.")]
    [SerializeField] private float driftDuration = 5.2f;
    [Tooltip("Vertical floating speed during the drift.")]
    [SerializeField] private float floatSpeed = 1.4f;
    [Tooltip("Rotation speed of the player while drifting.")]
    [SerializeField] private float rotationSpeed = 65f;
    [Tooltip("The target global radius for the sweeping Wonder Realm wave.")]
    [SerializeField] private float maxWarpRadius = 120f;
    [Tooltip("Expansion speed of the global Wonder Radius wave.")]
    [SerializeField] private float warpExpansionSpeed = 65f;

    [Header("Next Scene Settings")]
    [Tooltip("Specific scene to load upon victory. If empty, loads the next scene in Build Settings.")]
    [SerializeField] private string nextSceneName = "";

    private bool triggered = false;
    private Vector3 baseScale;
    private Vector3 basePosition;

    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Volume warpVolume;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;

    // Procedural assets generated at runtime
    private Sprite radialGlowSprite;
    private List<GameObject> activeParticles = new List<GameObject>();
    private AudioSource portalHumSource;

    private void Start()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Cache base local scale and position
        baseScale = transform.localScale;
        basePosition = transform.position;

        // Find Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }

        SetupWarpVolume();

        // Generate our stardust radial texture and seed the passive glow cloud
        radialGlowSprite = CreateRadialGlowSprite();
        SpawnPassiveStardust(32);
    }

    private void SetupWarpVolume()
    {
        warpVolume = gameObject.AddComponent<Volume>();
        warpVolume.isGlobal = true;
        warpVolume.weight = 0f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        lensDistortion = ScriptableObject.CreateInstance<LensDistortion>();
        lensDistortion.active = true;
        lensDistortion.intensity.Override(0f); // Pulsed during transition
        lensDistortion.scale.Override(1.1f);
        profile.components.Add(lensDistortion);

        chromaticAberration = ScriptableObject.CreateInstance<ChromaticAberration>();
        chromaticAberration.active = true;
        chromaticAberration.intensity.Override(0f); // Pulsed during transition
        profile.components.Add(chromaticAberration);

        warpVolume.profile = profile;
    }

    /// <summary>
    /// Generates a perfectly anti-aliased radial soft glow sprite texture at runtime.
    /// Eliminates the need for external static image assets!
    /// </summary>
    private Sprite CreateRadialGlowSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                // Rubbery exponential falloff for soft bloom core
                alpha = Mathf.Pow(alpha, 2.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>
    /// Seeds a localized nebula cloud of floating, breathing stardust particles.
    /// </summary>
    private void SpawnPassiveStardust(int count)
    {
        Material additiveMat = new Material(Shader.Find("Sprites/AdditiveGlow"));
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("PassiveStardustParticle");
            go.transform.SetParent(transform);
            
            // Spawn dispersed around the rift
            Vector2 circle = Random.insideUnitCircle * 3.8f;
            Vector3 spawnPos = transform.position + new Vector3(circle.x, circle.y, -0.05f);
            
            var particle = go.AddComponent<DriftStardust>();
            
            // Rich curated color palette (electric cyan, hot purple-magenta, bright gold, cosmic white)
            Color[] colors = new Color[] {
                new Color(0.0f, 0.85f, 1.0f, Random.Range(0.25f, 0.55f)), 
                new Color(1.0f, 0.08f, 0.65f, Random.Range(0.25f, 0.55f)), 
                new Color(1.0f, 0.85f, 0.1f, Random.Range(0.15f, 0.35f)), 
                new Color(1.0f, 1.0f, 1.0f, Random.Range(0.3f, 0.65f)) 
            };
            Color color = colors[Random.Range(0, colors.Length)];
            float scale = Random.Range(0.12f, 0.42f);
            
            particle.Initialize(radialGlowSprite, additiveMat, color, scale, spawnPos);
            particle.velocity = new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0.3f, 1.1f));
            particle.swaySpeed = Random.Range(1.4f, 3.2f);
            particle.swayAmount = Random.Range(0.15f, 0.45f);
            particle.lifetime = -1f; // Infinite breathing life
            particle.useUnscaledTime = false;
            
            activeParticles.Add(go);
        }
    }

    private void Update()
    {
        if (triggered) return;

        // 1. Organic spatial drift sway: Compounds multiple sine waves for unpredictable floating
        float time = Time.time;
        float swayX = Mathf.Sin(time * 1.4f) * 0.12f + Mathf.Cos(time * 2.8f) * 0.04f;
        float swayY = Mathf.Cos(time * 1.6f) * 0.12f + Mathf.Sin(time * 2.2f) * 0.04f;
        transform.position = basePosition + new Vector3(swayX, swayY, 0f);

        // 2. Slow swirling rotation
        transform.Rotate(Vector3.forward, -swirlSpeed * 0.2f * Time.deltaTime);

        // 3. Gentle elastic breathing pulse
        float pulse = Mathf.Sin(time * pulseFrequency) * pulseAmount;
        transform.localScale = baseScale * (1f + pulse);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !triggered)
        {
            triggered = true;
            
            // 1. Disable keyboard inputs
            var pController = collision.GetComponent<PlayerController2D>();
            if (pController != null) pController.enabled = false;
            
            // 2. Freeze movement and remove gravity
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
                playerRb.isKinematic = true;
            }

            // 3. Trigger heavy cinematic rumble
            if (CameraController2D.Instance != null)
            {
                CameraController2D.Instance.TriggerShake(0.8f, 0.35f);
            }

            // Play drift entry sound. Use 2D (non-spatialized) for the portal hum to avoid
            // non-finite AudioParam errors if the AudioListener moves/disappears during transition.
            AudioManager.PlayDriftStart(transform.position);
            portalHumSource = AudioManager.PlayLoop2D(AudioManager.Instance.portalLoopClip, 0.8f);

            // 4. Begin the majestic drift coroutine
            StartCoroutine(DimensionalDriftRoutine(collision.transform));

            Debug.Log("<b><color=#ffcc00>[VICTORY]</color></b>: Dimensional Drift initiated! Transitioning to the Realm of Awe...");
        }
    }

    private IEnumerator DimensionalDriftRoutine(Transform playerTrans)
    {
        // --- STEP 1: CREATE CINEMATIC CANVAS AND LETTERBOX BARS ---
        GameObject canvasObj = new GameObject("DriftCinematicCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Top letterbox black bar
        GameObject topBarObj = new GameObject("TopLetterbox");
        topBarObj.transform.SetParent(canvasObj.transform, false);
        var topImage = topBarObj.AddComponent<UnityEngine.UI.Image>();
        topImage.color = new Color(0.04f, 0.03f, 0.08f, 0.85f); // Soft obsidian color
        var topRect = topBarObj.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(0f, 0f);

        // Bottom letterbox black bar
        GameObject bottomBarObj = new GameObject("BottomLetterbox");
        bottomBarObj.transform.SetParent(canvasObj.transform, false);
        var bottomImage = bottomBarObj.AddComponent<UnityEngine.UI.Image>();
        bottomImage.color = new Color(0.04f, 0.03f, 0.08f, 0.85f);
        var bottomRect = bottomBarObj.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.sizeDelta = new Vector2(0f, 0f);

        // Centered Typographic Overlay
        GameObject textObj = new GameObject("DriftText");
        textObj.transform.SetParent(canvasObj.transform, false);
        var uiText = textObj.AddComponent<UnityEngine.UI.Text>();
        uiText.font = GetSafeBuiltinFont();
        uiText.text = "D R I F T I N G   I N T O   W O N D E R";
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontSize = 24;
        uiText.fontStyle = FontStyle.Normal;
        uiText.color = new Color(1f, 1f, 1f, 0f); // Start completely hidden
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.4f);
        textRect.anchorMax = new Vector2(0.9f, 0.6f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // --- STEP 2: CINEMATIC TIME DILATION ---
        float dilElapsed = 0f;
        float dilDuration = 0.8f;
        while (dilElapsed < dilDuration)
        {
            dilElapsed += Time.unscaledDeltaTime; // Use unscaled time while warping timeScale
            float t = dilElapsed / dilDuration;
            Time.timeScale = Mathf.Lerp(1.0f, 0.35f, t);

            // Animate cinematic letterbox heights (12% of screen height)
            float barHeight = Mathf.Lerp(0f, Screen.height * 0.12f, t);
            topRect.sizeDelta = new Vector2(0f, barHeight);
            bottomRect.sizeDelta = new Vector2(0f, barHeight);

            yield return null;
        }
        Time.timeScale = 0.35f;

        // --- STEP 3: DISCONNECT & EXPAND WONDER REALM WAVE ---
        if (WonderRadiusController.Instance != null)
        {
            // Position the Wonder Zone center exactly on the spatial rift
            WonderRadiusController.Instance.EnableDriftMode(transform.position, WonderRadiusController.Instance.Radius);
            
            // Order the radius to grow to 120 units at a high speed
            WonderRadiusController.Instance.SetDriftRadius(maxWarpRadius, warpExpansionSpeed);
        }

        // Start horizontal space spore particle streaming
        StartCoroutine(SpawnActiveDriftSpores(55, driftDuration));

        // --- STEP 4: DRIFT COROUTINE ANIMATION LOOP ---
        float driftElapsed = 0f;
        Vector3 playerStartPos = playerTrans.position;
        Vector3 playerStartScale = playerTrans.localScale;
        float playerStartCamSize = Camera.main != null ? Camera.main.orthographicSize : 6.5f;

        // Snapshot portal center NOW before any scene transition can invalidate this transform.
        // Using transform.position inside the loop risks NaN if the object is destroyed/moved.
        Vector3 portalCenter = transform.position;
        if (!IsFiniteVector(portalCenter)) portalCenter = playerStartPos; // Fallback safety

        // Calculate initial spiral offset vector, distance, and angle relative to the rift center
        Vector3 startOffset = playerStartPos - portalCenter;
        float startDist = startOffset.magnitude;
        float startAngle = Mathf.Atan2(startOffset.y, startOffset.x);

        // Retrieve player renderer for stardust dissolve
        var playerSr = playerTrans.Find("Graphics")?.GetComponent<SpriteRenderer>();
        if (playerSr == null) playerSr = playerTrans.GetComponentInChildren<SpriteRenderer>();
        Color playerStartColor = playerSr != null ? playerSr.color : Color.white;

        while (driftElapsed < driftDuration)
        {
            driftElapsed += Time.unscaledDeltaTime;
            float t = driftElapsed / driftDuration;

            // Spiral Orbit: Orbit radius decreases exponentially toward the center (creating magnetic suction)
            float currentDist = startDist * Mathf.Pow(1f - t, 2.5f);
            
            // Orbit Angle: Spirals around the center 2.8 complete revolutions
            float currentAngle = startAngle + t * (Mathf.PI * 2f * 2.8f);

            // Use the snapshotted portalCenter — never read from transform.position mid-loop
            Vector3 whirlPos = portalCenter + new Vector3(
                Mathf.Cos(currentAngle) * currentDist,
                Mathf.Sin(currentAngle) * currentDist,
                playerStartPos.z
            );

            // Only apply if the computed position is numerically valid
            if (IsFiniteVector(whirlPos))
            {
                playerTrans.position = whirlPos;
            }

            // Fast self-spin on Z-axis as the player is whirled in
            playerTrans.Rotate(Vector3.forward, 520f * Time.unscaledDeltaTime);

            // Rubbery organic shrink into the cosmic rift
            playerTrans.localScale = Vector3.Lerp(playerStartScale, Vector3.zero, t * t);

            // Camera zooms in dynamically to focus on player's scale-down dissolve
            if (Camera.main != null)
            {
                Camera.main.orthographicSize = Mathf.Lerp(playerStartCamSize, playerStartCamSize - 2f, t);
            }

            // Typographic Text fade in (first 30%), static hold, then fade out (last 30%)
            if (t < 0.3f)
            {
                uiText.color = new Color(1f, 1f, 1f, t / 0.3f);
            }
            else if (t > 0.7f)
            {
                uiText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (t - 0.7f) / 0.3f));
            }
            else
            {
                uiText.color = Color.white;
            }

            // Animate URP screen distortion & chromatic aberration
            if (warpVolume != null)
            {
                float warpIntensity = Mathf.Sin(t * Mathf.PI); // Smooth peak at 50%
                warpVolume.weight = warpIntensity;

                if (lensDistortion != null)
                {
                    lensDistortion.intensity.Override(Mathf.Lerp(0f, -0.85f, warpIntensity));
                }
                if (chromaticAberration != null)
                {
                    chromaticAberration.intensity.Override(Mathf.Lerp(0f, 1.0f, warpIntensity));
                }
            }

            // Dissolve player and spatial rift in the final 25%
            if (t > 0.75f)
            {
                float dissolveFactor = (t - 0.75f) / 0.25f;

                if (playerSr != null)
                {
                    // Dissolve player to a neon cyan bioluminescent mist
                    playerSr.color = Color.Lerp(playerStartColor, new Color(0.0f, 0.85f, 1.0f, 0f), dissolveFactor);
                }

                // Dissolve rift sprite
                var riftSr = GetComponent<SpriteRenderer>();
                if (riftSr != null)
                {
                    riftSr.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), dissolveFactor);
                }
            }

            yield return null;
        }

        // --- STEP 5: FINAL FADE TO COSMIC VOID ---
        // Stop looping cosmic portal hum smoothly
        if (portalHumSource != null)
        {
            AudioManager.StopLoop(portalHumSource, 1.2f);
            portalHumSource = null;
        }

        GameObject fadePanelObj = new GameObject("FadePanel");
        fadePanelObj.transform.SetParent(canvasObj.transform, false);
        var fadeImage = fadePanelObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0.04f, 0.03f, 0.08f, 0f); // Space void purple
        var fadeRect = fadePanelObj.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        float fadeElapsed = 0f;
        float fadeDuration = 1.4f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            fadeImage.color = new Color(0.04f, 0.03f, 0.08f, fadeElapsed / fadeDuration);
            yield return null;
        }
        fadeImage.color = new Color(0.04f, 0.03f, 0.08f, 1f);

        // --- STEP 6: ELEGANT END TEXT DISPLAY ---
        // Play the glorious celestial major arpeggio victory chime!
        AudioManager.PlayVictoryChime();

        GameObject resultTextObj = new GameObject("ResultText");
        resultTextObj.transform.SetParent(canvasObj.transform, false);
        var resultText = resultTextObj.AddComponent<UnityEngine.UI.Text>();
        resultText.font = GetSafeBuiltinFont();
        resultText.text = "A W E   A C H I E V E D .\n\n<size=15>The universe is now in perfect harmony.</size>";
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.fontSize = 28;
        resultText.color = new Color(0.0f, 0.85f, 1.0f, 0f); // Cyber neon cyan
        var resultRect = resultTextObj.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.1f, 0.3f);
        resultRect.anchorMax = new Vector2(0.9f, 0.7f);
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;

        float resElapsed = 0f;
        float resDuration = 1.8f;
        while (resElapsed < resDuration)
        {
            resElapsed += Time.unscaledDeltaTime;
            resultText.color = new Color(0.0f, 0.85f, 1.0f, resElapsed / resDuration);
            yield return null;
        }
        resultText.color = new Color(0.0f, 0.85f, 1.0f, 1f);

        // Pause to let the player absorb the "AWE ACHIEVED" message
        yield return new WaitForSecondsRealtime(2.5f);

        // Suspend player object and restore standard timeScale for engine safety
        playerTrans.gameObject.SetActive(false);
        Time.timeScale = 1.0f;

        // Load next level / scene
        LoadNextScene();
    }

    /// <summary>
    /// Spawns horizontal additive drift spores that stream past the viewport at high speed.
    /// Creates a stunning warp-speed sensation during the transition!
    /// </summary>
    private IEnumerator SpawnActiveDriftSpores(int totalCount, float totalDuration)
    {
        Material additiveMat = new Material(Shader.Find("Sprites/AdditiveGlow"));
        float elapsed = 0f;
        float spawnInterval = totalDuration / totalCount;
        
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Spawn just past the left edge of the camera viewport
            Vector3 spawnPos = mainCam.ViewportToWorldPoint(new Vector3(-0.1f, Random.Range(0.15f, 0.85f), 10f));
            spawnPos.z = -0.1f;

            GameObject go = new GameObject("ActiveDriftSpore");
            var spore = go.AddComponent<DriftStardust>();

            // Curated gradient shades
            Color color = new Color(
                Random.Range(0.2f, 1.0f),
                Random.Range(0.1f, 0.85f),
                1.0f,
                Random.Range(0.45f, 0.85f)
            );
            float scale = Random.Range(0.18f, 0.48f);

            spore.Initialize(radialGlowSprite, additiveMat, color, scale, spawnPos);
            spore.velocity = new Vector2(Random.Range(16f, 26f), Random.Range(-1.5f, 1.5f));
            spore.swaySpeed = Random.Range(3f, 6.5f);
            spore.swayAmount = Random.Range(0.6f, 1.4f);
            spore.lifetime = 3.5f; // Automatically cleans up after crossing screen
            spore.useUnscaledTime = true; // Rushes past in real-time, unaffected by slow-mo!

            // Play spatialized wind-whoosh sound for spore rushing past the screen
            AudioManager.PlaySporeWhoosh(spawnPos);

            yield return new WaitForSecondsRealtime(spawnInterval);
        }
    }

    /// <summary>
    /// Returns true only if all vector components are finite (not NaN or Infinity).
    /// Prevents invalid transform.position assignments that crash the WebGL audio backend.
    /// </summary>
    private static bool IsFiniteVector(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
            && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    private void LoadNextScene()
    {
        // Check if a specific next scene name is provided
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Otherwise, load next scene in build index
            int nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
            {
                // Unlock the next level
                int currentUnlocked = PlayerPrefs.GetInt("UnlockedLevelIndex", 1);
                if (nextSceneIndex > currentUnlocked)
                {
                    PlayerPrefs.SetInt("UnlockedLevelIndex", nextSceneIndex);
                    PlayerPrefs.Save();
                }

                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                // Fallback: Reload the current scene if no other scene is available in Build Settings
                Debug.LogWarning("<b><color=orange>[PORTAL]</color></b>: No next scene available in Build Settings! Reloading current scene.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    private Font GetSafeBuiltinFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch {}

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch {}
        }

        if (font == null)
        {
            Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (loadedFonts != null && loadedFonts.Length > 0)
            {
                font = loadedFonts[0];
            }
        }

        return font;
    }
}

/// <summary>
/// A lightweight runtime component that animates custom stardust and spore physics
/// with organic wind sway, radial breath, and support for real-time unscaled motion.
/// </summary>
public class DriftStardust : MonoBehaviour
{
    public Vector2 velocity;
    public float swaySpeed;
    public float swayAmount;
    public float lifetime;
    public bool useUnscaledTime = false;

    private SpriteRenderer sr;
    private float age;
    private float startAlpha;

    public void Initialize(Sprite sprite, Material mat, Color color, float scale, Vector3 initialPos)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.material = mat;
        sr.color = color;
        sr.sortingOrder = 8; // Render on top of normal layer blocks
        
        transform.localScale = Vector3.one * scale;
        transform.position = initialPos;
        
        startAlpha = color.a;
        age = Random.Range(0f, 2f); // Offset to randomize wave cycles
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        age += dt;

        // Apply physical upward/forward drift + horizontal wind sway
        float sway = Mathf.Sin(age * swaySpeed) * swayAmount;
        transform.position += (Vector3)(velocity * dt) + new Vector3(sway * dt, 0f, 0f);

        // Breathing / Lifetime fading
        if (lifetime > 0f)
        {
            float t = age / lifetime;
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
            else
            {
                // Soft ease in and ease out throughout lifecycle
                float alphaMultiplier = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI));
                Color col = sr.color;
                col.a = startAlpha * alphaMultiplier;
                sr.color = col;
            }
        }
        else
        {
            // Infinite passive breathing stardust
            float breath = 0.45f + 0.55f * Mathf.Sin(age * 2.2f);
            Color col = sr.color;
            col.a = startAlpha * breath;
            sr.color = col;
        }
    }
}
