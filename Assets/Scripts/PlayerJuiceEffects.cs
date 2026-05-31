using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A code-driven procedural particle system that injects visual "juice" into player movement.
/// Emits running dust, circular jump smoke rings, landing sparks, and magical glowing stardust
/// when inside the Wonder Zone. Updates all particles efficiently under a single controller.
/// </summary>
public class PlayerJuiceEffects : MonoBehaviour
{
    private class JuiceParticle
    {
        public GameObject obj;
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float currentAlpha = 1.0f;
        public float fadeSpeed = 3.5f;
        public Vector3 baseScale;
        public float shrinkSpeed = 1.8f;
        public bool isWonderSparkle = false;
        public float gravity = 0f;
    }

    [Header("Run Dust settings")]
    [SerializeField] private float runDustInterval = 0.12f;
    [SerializeField] private float runDustSpeed = 1.5f;

    [Header("Colours")]
    [SerializeField] private Color mundaneColor = new Color(0.85f, 0.85f, 0.85f, 0.65f); // Soft grey-white dust
    [SerializeField] private Color wonderCyan = new Color(0.0f, 0.95f, 1.0f, 0.9f);       // Neon Cyan
    [SerializeField] private Color wonderMagenta = new Color(1.0f, 0.15f, 0.75f, 0.9f);    // Neon Pink/Magenta

    private PlayerController2D playerController;
    private Rigidbody2D playerRb;
    
    private Sprite particleSprite;
    private Material wonderMaterial;
    private Material mundaneMaterial;
    
    private List<JuiceParticle> particles = new List<JuiceParticle>();
    private float runDustTimer = 0f;

    private TrailRenderer playerTrail;
    private UnityEngine.Rendering.Universal.Light2D ambientLight;
    private SpriteRenderer playerGraphicsRenderer;

    private void Start()
    {
        playerController = GetComponent<PlayerController2D>();
        playerRb = GetComponent<Rigidbody2D>();

        // Load or create Wonder Mask material for reality-masking support
        Shader wonderShader = Shader.Find("Sprites/WonderMask");
        if (wonderShader != null)
        {
            wonderMaterial = new Material(wonderShader);
            wonderMaterial.SetFloat("_Feather", 0.35f);
        }
        else
        {
            wonderMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        }

        mundaneMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));

        GenerateParticleSprite();
        UpgradePlayerVisuals();
    }

    private void UpgradePlayerVisuals()
    {
        Transform graphicsObj = transform.Find("Graphics");
        if (graphicsObj != null)
        {
            // 1. Upgrade Sprite Material to Lit
            playerGraphicsRenderer = graphicsObj.GetComponent<SpriteRenderer>();
            if (playerGraphicsRenderer != null)
            {
                playerGraphicsRenderer.material = mundaneMaterial;
                playerGraphicsRenderer.color = mundaneColor;
            }

            // 2. Setup Trail Renderer
            GameObject trailObj = new GameObject("PlayerTrail");
            trailObj.transform.SetParent(graphicsObj, false);
            trailObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            
            playerTrail = trailObj.AddComponent<TrailRenderer>();
            playerTrail.time = 0.25f;
            playerTrail.startWidth = 0.6f;
            playerTrail.endWidth = 0.0f;
            playerTrail.material = wonderMaterial;
            
            Gradient trailGradient = new Gradient();
            trailGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(mundaneColor, 0.0f), new GradientColorKey(mundaneColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            playerTrail.colorGradient = trailGradient;
            playerTrail.sortingOrder = 9;

            // 3. Setup Ambient Glow Light
            var lightObj = new GameObject("AmbientGlow");
            lightObj.transform.SetParent(graphicsObj, false);
            lightObj.transform.localPosition = Vector3.zero;

            ambientLight = lightObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            ambientLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            ambientLight.pointLightInnerRadius = 1.5f;
            ambientLight.pointLightOuterRadius = 4.5f;
            ambientLight.intensity = 0.8f;
            ambientLight.color = mundaneColor;
        }
    }

    /// <summary>
    /// Generates a premium circular glowing starburst particle texture programmatically in memory.
    /// </summary>
    private void GenerateParticleSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) / 2.5f; // Offset for flare look
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - 7.5f) / 7.5f;
                float dy = (y - 7.5f) / 7.5f;
                
                // Classic starburst glow shape (combination of radial and cross flare)
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 2.2f); // Sharp center drop-off
                
                // Add minor cross flare highlights
                float flareX = Mathf.Clamp01(1f - Mathf.Abs(dx) * 4f) * Mathf.Clamp01(1f - Mathf.Abs(dy) * 1.5f) * 0.4f;
                float flareY = Mathf.Clamp01(1f - Mathf.Abs(dy) * 4f) * Mathf.Clamp01(1f - Mathf.Abs(dx) * 1.5f) * 0.4f;
                
                float finalAlpha = Mathf.Clamp01(alpha + flareX + flareY);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, finalAlpha));
            }
        }
        tex.Apply();
        particleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    private void Update()
    {
        HandleRunDustEmission();
        UpdateParticles();
        UpdateDynamicVisuals();
    }

    private void UpdateDynamicVisuals()
    {
        bool insideWonderNow = WonderRadiusController.IsInsideWonderZone(transform.position);
        Color targetColor = insideWonderNow ? wonderCyan : mundaneColor;

        if (playerGraphicsRenderer != null)
        {
            playerGraphicsRenderer.color = Color.Lerp(playerGraphicsRenderer.color, targetColor, Time.deltaTime * 5f);
        }

        if (playerTrail != null)
        {
            Gradient trailGradient = playerTrail.colorGradient;
            var colorKeys = trailGradient.colorKeys;
            colorKeys[0].color = Color.Lerp(colorKeys[0].color, targetColor, Time.deltaTime * 5f);
            colorKeys[1].color = colorKeys[0].color;
            trailGradient.SetKeys(colorKeys, trailGradient.alphaKeys);
            playerTrail.colorGradient = trailGradient;
        }

        if (ambientLight != null)
        {
            ambientLight.color = Color.Lerp(ambientLight.color, targetColor, Time.deltaTime * 5f);
            ambientLight.intensity = Mathf.Lerp(ambientLight.intensity, insideWonderNow ? 1.2f : 0.4f, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Evaluates if the player is running and emits trailing dust puffs from their feet.
    /// </summary>
    private void HandleRunDustEmission()
    {
        if (playerController == null || playerRb == null) return;

        if (playerController.IsGrounded && Mathf.Abs(playerRb.velocity.x) > 1.8f)
        {
            runDustTimer -= Time.deltaTime;
            if (runDustTimer <= 0f)
            {
                // Emit trailing particle
                Vector2 spawnPos = transform.position;
                // Move spawn point to bottom of capsule (feet)
                spawnPos.y -= 0.65f;
                
                // Slide back opposite to movement direction
                float moveDir = -Mathf.Sign(playerRb.velocity.x);
                spawnPos.x += moveDir * 0.15f;

                Vector2 vel = new Vector2(moveDir * runDustSpeed * Random.Range(0.6f, 1.2f), Random.Range(0.1f, 0.5f));
                
                bool insideWonder = WonderRadiusController.IsInsideWonderZone(spawnPos);
                SpawnParticle(spawnPos, vel, insideWonder ? 0.35f : 0.25f, 3.2f, 2.0f, insideWonder);
                
                runDustTimer = runDustInterval * (1f / (Mathf.Abs(playerRb.velocity.x) / playerController.HorizontalVelocity)); // Scaling with speed
                runDustTimer = Mathf.Clamp(runDustTimer, 0.04f, runDustInterval);
            }
        }
    }

    /// <summary>
    /// Spawns a single particle in the managed C# pool.
    /// </summary>
    private void SpawnParticle(Vector2 pos, Vector2 vel, float startScale, float fadeSpd, float shrinkSpd, bool isWonder, float gravity = 0f)
    {
        GameObject go = new GameObject("PlayerJuiceParticle");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * startScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = particleSprite;
        sr.sortingOrder = 15; // In front of player graphics

        // Set materials appropriately
        if (isWonder)
        {
            sr.material = wonderMaterial;
            sr.color = Random.value > 0.5f ? wonderCyan : wonderMagenta;
        }
        else
        {
            sr.color = mundaneColor;
        }

        JuiceParticle p = new JuiceParticle
        {
            obj = go,
            transform = go.transform,
            renderer = sr,
            velocity = vel,
            baseScale = go.transform.localScale,
            fadeSpeed = fadeSpd,
            shrinkSpeed = shrinkSpd,
            isWonderSparkle = isWonder,
            gravity = gravity
        };

        particles.Add(p);
    }

    /// <summary>
    /// Triggers a glorious circular ring puff of sparks/smoke when jumping.
    /// </summary>
    public void EmitJumpBurst()
    {
        Vector2 feetPos = (Vector2)transform.position + Vector2.down * 0.65f;
        int count = 9;
        
        bool inside = WonderRadiusController.IsInsideWonderZone(feetPos);

        for (int i = 0; i < count; i++)
        {
            float angle = (i * (360f / count) + Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.4f); // Flattened ring
            Vector2 vel = dir * Random.Range(1.8f, 3.2f);
            
            SpawnParticle(feetPos, vel, inside ? 0.35f : 0.28f, 2.5f, 1.4f, inside);
        }
    }

    /// <summary>
    /// Triggers a large, flat, ground-hugging impact shockwave of particles upon landing.
    /// </summary>
    public void EmitLandBurst(float fallVelocity)
    {
        Vector2 feetPos = (Vector2)transform.position + Vector2.down * 0.65f;
        
        // Scale burst intensity by landing force
        int count = Mathf.Clamp(Mathf.RoundToInt(fallVelocity * 0.8f), 6, 20);
        float speedMult = Mathf.Clamp(fallVelocity * 0.18f, 1.5f, 5f);
        
        bool inside = WonderRadiusController.IsInsideWonderZone(feetPos);

        for (int i = 0; i < count; i++)
        {
            // Spawn flatly skimming along ground left and right
            float direction = Random.value > 0.5f ? 1f : -1f;
            Vector2 vel = new Vector2(
                direction * Random.Range(0.8f, 2.8f) * speedMult,
                Random.Range(0.1f, 1.2f) * (fallVelocity * 0.05f)
            );

            SpawnParticle(
                feetPos + new Vector2(direction * 0.1f, 0f), 
                vel, 
                inside ? Random.Range(0.28f, 0.42f) : Random.Range(0.2f, 0.3f), 
                Random.Range(2f, 4f), 
                Random.Range(1.2f, 2.5f), 
                inside,
                4.5f // Apply gravity to landing sparks so they fall back heavy!
            );
        }
    }

    /// <summary>
    /// Updates physics, scales, and colors for all active particles.
    /// </summary>
    private void UpdateParticles()
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            if (p == null || p.obj == null)
            {
                particles.RemoveAt(i);
                continue;
            }

            // Apply gravity/drag
            p.velocity.y -= p.gravity * Time.deltaTime;
            p.transform.position += (Vector3)p.velocity * Time.deltaTime;

            // Fade alpha
            p.currentAlpha -= p.fadeSpeed * Time.deltaTime;
            
            // Shrink size
            p.transform.localScale = Vector3.Lerp(p.transform.localScale, Vector3.zero, p.shrinkSpeed * Time.deltaTime);

            if (p.currentAlpha <= 0f || p.transform.localScale.x <= 0.01f)
            {
                Destroy(p.obj);
                particles.RemoveAt(i);
                continue;
            }

            // Reality morphing: dynamically swap particle properties as they cross the active Wonder Radius!
            bool insideWonderNow = WonderRadiusController.IsInsideWonderZone(p.transform.position);
            
            Color col = p.renderer.color;
            if (insideWonderNow)
            {
                p.renderer.material = wonderMaterial;
                
                // If it transitioned from mundane to wonder, light it up!
                if (!p.isWonderSparkle)
                {
                    p.isWonderSparkle = true;
                    col = Random.value > 0.5f ? wonderCyan : wonderMagenta;
                }
                
                col.a = p.currentAlpha;
                p.renderer.color = col;
            }
            else
            {
                p.renderer.material = mundaneMaterial;
                
                if (p.isWonderSparkle)
                {
                    p.isWonderSparkle = false;
                    col = mundaneColor;
                }
                
                col.a = p.currentAlpha * 0.8f; // slightly fainter outside
                p.renderer.color = col;
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up remaining particle objects
        foreach (var p in particles)
        {
            if (p != null && p.obj != null)
            {
                Destroy(p.obj);
            }
        }
        particles.Clear();
    }
}
