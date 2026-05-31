using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A purely code-driven generative graphics system that spawns floating magical particles
/// (glows and star sparkles) in a bounding box centered on the player.
/// All spawned objects use the custom "Sprites/WonderMask" shader, making them
/// 100% invisible in the gray mundane world, but gorgeous, colorful, and active
/// when inside the player's Wonder Radius!
/// </summary>
public class WonderProceduralVisuals : MonoBehaviour
{
    [System.Serializable]
    public class FloatParticle
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 driftVelocity;
        public float rotationSpeed;
        public float sinOffset;
        public float sinFrequency;
        public float baseScale;
        public float bounceOffset;
    }

    [Header("Pool Settings")]
    [Tooltip("Total number of background visual particles to spawn.")]
    [SerializeField] private int particleCount = 75;
    [Tooltip("The viewport bounding box around the player where particles exist.")]
    [SerializeField] private Vector2 wrapBounds = new Vector2(30f, 18f);

    [Header("Particle Drift Behavior")]
    [SerializeField] private float minDriftSpeed = 0.15f;
    [SerializeField] private float maxDriftSpeed = 0.6f;
    [SerializeField] private float minRotationSpeed = -30f;
    [SerializeField] private float maxRotationSpeed = 30f;

    [Header("Sizing & Scaling")]
    [SerializeField] private float minScale = 0.15f;
    [SerializeField] private float maxScale = 0.45f;

    [Header("Bioluminescent Colors")]
    [Tooltip("A collection of vibrant, magical colors to assign to the particles.")]
    [SerializeField] private Color[] magicColors = new Color[]
    {
        new Color(0.2f, 0.85f, 1.0f, 0.8f),  // Electric Cyan
        new Color(0.9f, 0.2f, 1.0f, 0.8f),  // Hot Pink/Magenta
        new Color(1.0f, 0.85f, 0.2f, 0.8f),  // Radiant Gold
        new Color(0.3f, 1.0f, 0.5f, 0.8f),  // Bioluminescent Emerald
        new Color(0.6f, 0.3f, 1.0f, 0.8f)   // Cosmic Violet
    };

    private Transform playerTransform;
    private List<FloatParticle> particles = new List<FloatParticle>();
    private Material wonderMaterial;
    
    // Procedural sprite assets generated at runtime
    private Sprite radialGlowSprite;
    private Sprite diamondSparkleSprite;

    private void Start()
    {
        // Automatically spawn WonderWorldEnhancer at runtime if not present
        if (FindFirstObjectByType<WonderWorldEnhancer>() == null)
        {
            GameObject enhancerObj = new GameObject("WonderWorldEnhancer");
            enhancerObj.AddComponent<WonderWorldEnhancer>();
        }
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Initialize runtime wonder mask material
        Shader wonderShader = Shader.Find("Sprites/WonderMask");
        if (wonderShader != null)
        {
            wonderMaterial = new Material(wonderShader);
            // Set a soft default edge feather on materials
            wonderMaterial.SetFloat("_Feather", 0.4f);
        }
        else
        {
            Debug.LogWarning("Sprites/WonderMask shader not found! Particles will use fallback.");
            wonderMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        }

        // Generate procedural sprites
        GenerateProceduralSprites();

        // Spawn particles
        SpawnVisualsPool();
    }

    /// <summary>
    /// Programmatically generates two high-quality textures and sprites in memory
    /// to guarantee the game has assets out of the box with zero external file dependencies!
    /// </summary>
    private void GenerateProceduralSprites()
    {
        // 1. Generate a radial glow (soft circle)
        Texture2D glowTex = new Texture2D(32, 32);
        glowTex.filterMode = FilterMode.Bilinear;
        glowTex.wrapMode = TextureWrapMode.Clamp;
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                alpha = Mathf.Pow(alpha, 2.2f); // Exponential fade for soft look
                glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        glowTex.Apply();
        radialGlowSprite = Sprite.Create(glowTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);

        // 2. Generate a star sparkle (Manhattan-distance diamond)
        Texture2D sparkleTex = new Texture2D(16, 16);
        sparkleTex.filterMode = FilterMode.Bilinear;
        sparkleTex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = Mathf.Abs(x - 7.5f) / 7.5f;
                float dy = Mathf.Abs(y - 7.5f) / 7.5f;
                float val = 1f - (dx + dy); // Manhattan shape
                float alpha = Mathf.Clamp01(val);
                alpha = Mathf.Pow(alpha, 1.8f); // Pinched star look
                
                // Add center intensity
                if (distToCenter(x, y, 7.5f, 7.5f) < 2f) alpha += 0.2f;

                sparkleTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }
        sparkleTex.Apply();
        diamondSparkleSprite = Sprite.Create(sparkleTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 8f);
    }

    private float distToCenter(float x, float y, float cx, float cy)
    {
        return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
    }

    /// <summary>
    /// Spawns the entire particle container pool in random starting positions.
    /// </summary>
    private void SpawnVisualsPool()
    {
        Vector2 center = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;

        for (int i = 0; i < particleCount; i++)
        {
            GameObject go = new GameObject($"WonderDriftParticle_{i}");
            go.transform.SetParent(this.transform);
            
            // Random position in bounding box
            float rx = Random.Range(-wrapBounds.x * 0.5f, wrapBounds.x * 0.5f);
            float ry = Random.Range(-wrapBounds.y * 0.5f, wrapBounds.y * 0.5f);
            go.transform.position = new Vector3(center.x + rx, center.y + ry, 1f); // Place slightly behind characters

            var sr = go.AddComponent<SpriteRenderer>();
            sr.material = wonderMaterial;
            
            // Randomize between circle and diamond sparkle
            sr.sprite = (Random.value > 0.45f) ? radialGlowSprite : diamondSparkleSprite;
            
            // Assign magical color and transparency
            Color colorBase = magicColors[Random.Range(0, magicColors.Length)];
            colorBase.a = Random.Range(0.4f, 0.85f);
            sr.color = colorBase;
            
            // Random sorting layer offset so they stack nicely in background
            sr.sortingOrder = -2;

            // Save state object
            FloatParticle p = new FloatParticle();
            p.transform = go.transform;
            p.renderer = sr;
            p.driftVelocity = new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(minDriftSpeed, maxDriftSpeed) // Whimsical drift upward
            ).normalized * Random.Range(minDriftSpeed, maxDriftSpeed);
            
            p.rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            p.sinOffset = Random.Range(0f, 100f);
            p.sinFrequency = Random.Range(0.5f, 2.0f);
            p.baseScale = Random.Range(minScale, maxScale);
            p.bounceOffset = Random.Range(0f, Mathf.PI * 2f);

            // Apply base scale
            p.transform.localScale = new Vector3(p.baseScale, p.baseScale, 1f);

            particles.Add(p);
        }
    }

    private void Update()
    {
        Vector2 refPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;

        foreach (var p in particles)
        {
            // --- 1. Drift and Sinusoidal Sway Animation ---
            Vector3 position = p.transform.position;
            position.x += p.driftVelocity.x * Time.deltaTime;
            // Add custom horizontal wave sway
            position.x += Mathf.Sin(Time.time * p.sinFrequency + p.sinOffset) * 0.015f;
            position.y += p.driftVelocity.y * Time.deltaTime;

            p.transform.position = position;

            // Rotate sparkle stars
            p.transform.Rotate(Vector3.forward, p.rotationSpeed * Time.deltaTime);

            // Gentle breathing pulse animation for scale
            float scalePulse = p.baseScale * (1f + 0.15f * Mathf.Sin(Time.time * 2f + p.bounceOffset));
            p.transform.localScale = new Vector3(scalePulse, scalePulse, 1f);

            // --- 2. Screen-Wrap Recycler (Keeps particles centered on player) ---
            Vector3 delta = p.transform.position - (Vector3)refPos;

            // Horizontal wrapping
            if (delta.x > wrapBounds.x * 0.5f)
            {
                p.transform.position = new Vector3(refPos.x - wrapBounds.x * 0.5f, p.transform.position.y, 1f);
            }
            else if (delta.x < -wrapBounds.x * 0.5f)
            {
                p.transform.position = new Vector3(refPos.x + wrapBounds.x * 0.5f, p.transform.position.y, 1f);
            }

            // Vertical wrapping
            if (delta.y > wrapBounds.y * 0.5f)
            {
                p.transform.position = new Vector3(p.transform.position.x, refPos.y - wrapBounds.y * 0.5f, 1f);
            }
            else if (delta.y < -wrapBounds.y * 0.5f)
            {
                p.transform.position = new Vector3(p.transform.position.x, refPos.y + wrapBounds.y * 0.5f, 1f);
            }
        }
    }
}
