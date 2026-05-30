using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A code-driven aesthetics system that creates a literal "window into another universe" portal effect.
/// Spawns a dynamic dual-background (concrete wall vs cosmic space nebula) and scatters beautiful,
/// invisible bioluminescent plants (ferns, mushrooms, star-flowers) that sprout and sway under the Wonder Radius!
/// </summary>
public class WonderWorldEnhancer : MonoBehaviour
{
    [Header("Dynamic Background Settings")]
    [SerializeField] private float backgroundZOffset = 10f;
    [SerializeField] private Vector2 backgroundScaleMultiplier = new Vector2(40f, 25f);

    [Header("Flora Spawn Density")]
    [Tooltip("Probability of a plant spawning at each platform interval.")]
    [SerializeField] private float spawnChance = 0.7f;
    [SerializeField] private float windSwayRange = 12f;

    // Generated assets
    private Sprite mundaneBgSprite;
    private Sprite wonderBgSprite;
    private Sprite fernSprite;
    private Sprite mushroomSprite;
    private Sprite flowerSprite;

    private Material wonderMaskMaterial;
    
    // Background objects (attached to main camera)
    private GameObject mundaneBgObj;
    private GameObject wonderBgObj;

    // Tracker lists for animation
    private List<Transform> floraTransforms = new List<Transform>();
    private List<float> floraSwayOffsets = new List<float>();
    private List<float> floraSwaySpeeds = new List<float>();
    private List<float> floraBaseScales = new List<float>();

    private void Start()
    {
        // 1. Initialize custom WonderMask shader material
        Shader wonderShader = Shader.Find("Sprites/WonderMask");
        if (wonderShader != null)
        {
            wonderMaskMaterial = new Material(wonderShader);
            wonderMaskMaterial.SetFloat("_Feather", 0.45f);
        }
        else
        {
            Debug.LogWarning("Sprites/WonderMask shader not found for WonderWorldEnhancer! Using fallback.");
            wonderMaskMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        // 2. Generate procedural textures & sprites
        GenerateProceduralSprites();

        // 3. Setup Camera-attached background planes
        SetupBackgroundPlanes();

        // 4. Scan the environment and spawn hidden plants
        ScanAndSpawnFlora();
    }

    private void GenerateProceduralSprites()
    {
        float ppu = 16f;

        // --- A. Generate Mundane Background Sprite (Tiled Concrete grid) ---
        int bgSize = 256;
        Texture2D mundaneTex = new Texture2D(bgSize, bgSize);
        mundaneTex.filterMode = FilterMode.Bilinear;
        mundaneTex.wrapMode = TextureWrapMode.Repeat;

        Color concreteBase = new Color(0.15f, 0.16f, 0.18f, 1f);
        Color grooveLight = new Color(0.24f, 0.25f, 0.28f, 1f);
        Color grooveDark = new Color(0.1f, 0.1f, 0.12f, 1f);

        for (int y = 0; y < bgSize; y++)
        {
            for (int x = 0; x < bgSize; x++)
            {
                // Sub-grid blocks of 64x64
                bool isBevelX = (x % 64 == 0 || x % 64 == 63);
                bool isBevelY = (y % 64 == 0 || y % 64 == 63);
                bool isGrooveX = (x % 64 == 1 || x % 64 == 62);
                bool isGrooveY = (y % 64 == 1 || y % 64 == 62);

                float noise = Random.value * 0.04f - 0.02f;
                Color pixelColor = concreteBase + new Color(noise, noise, noise, 0f);

                if (isBevelX || isBevelY)
                {
                    pixelColor = grooveDark;
                }
                else if (isGrooveX || isGrooveY)
                {
                    pixelColor = grooveLight;
                }
                mundaneTex.SetPixel(x, y, pixelColor);
            }
        }
        mundaneTex.Apply();
        mundaneBgSprite = Sprite.Create(mundaneTex, new Rect(0, 0, bgSize, bgSize), new Vector2(0.5f, 0.5f), ppu);

        // --- B. Generate Wonder Background Sprite (Cosmic Starfield & Purple Dust) ---
        Texture2D wonderTex = new Texture2D(bgSize, bgSize);
        wonderTex.filterMode = FilterMode.Bilinear;
        wonderTex.wrapMode = TextureWrapMode.Repeat;

        Color spaceVoid = new Color(0.04f, 0.03f, 0.08f, 1f);

        for (int y = 0; y < bgSize; y++)
        {
            for (int x = 0; x < bgSize; x++)
            {
                // Procedural Nebula Gas Cloud
                float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(bgSize * 0.35f, bgSize * 0.4f)) / bgSize;
                float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(bgSize * 0.7f, bgSize * 0.75f)) / bgSize;

                float nebulaPurple = Mathf.Clamp01(1f - d1 * 1.8f);
                float nebulaMagenta = Mathf.Clamp01(1f - d2 * 2.2f);

                Color gas = new Color(0.12f * nebulaPurple, 0.05f * nebulaMagenta, 0.22f * nebulaPurple, 0f);
                Color pixelColor = spaceVoid + gas;

                // Tiled Stars (White and Cyan)
                float starChance = Random.value;
                if (starChance > 0.996f)
                {
                    pixelColor = new Color(1f, 1f, 1f, Random.Range(0.6f, 1.0f));
                }
                else if (starChance > 0.993f)
                {
                    pixelColor = new Color(0.1f, 0.8f, 1.0f, Random.Range(0.4f, 0.8f));
                }

                wonderTex.SetPixel(x, y, pixelColor);
            }
        }
        wonderTex.Apply();
        wonderBgSprite = Sprite.Create(wonderTex, new Rect(0, 0, bgSize, bgSize), new Vector2(0.5f, 0.5f), ppu);

        // --- C. Generate Bioluminescent Flora Sprites ---
        // 1. Curl Fern (32x48)
        int fw = 32, fh = 48;
        Texture2D fernTex = new Texture2D(fw, fh);
        fernTex.filterMode = FilterMode.Bilinear;
        fernTex.wrapMode = TextureWrapMode.Clamp;
        Color neonGreen = new Color(0.0f, 1.0f, 0.45f, 1f);
        Color stemGreen = new Color(0.0f, 0.6f, 0.25f, 1f);

        for (int y = 0; y < fh; y++)
        {
            for (int x = 0; x < fw; x++)
            {
                // Curved main spine
                float spineX = fw * 0.4f + Mathf.Sin(y * 0.08f) * 6f;
                float distToSpine = Mathf.Abs(x - spineX);

                // Leaf growth: multiple horizontal ribs jutting out
                bool isSpine = distToSpine < 1.2f;
                bool isLeaf = false;
                
                // Frequency of leaves along height
                if (y > 4 && y % 5 <= 1)
                {
                    float leafWidth = (fh - y) * 0.35f + 1f; // Taper off at top
                    if (Mathf.Abs(x - spineX) < leafWidth)
                    {
                        isLeaf = true;
                    }
                }

                if (isSpine && y < fh - 4)
                {
                    fernTex.SetPixel(x, y, stemGreen);
                }
                else if (isLeaf && y < fh - 3)
                {
                    // Gradient neon green tips
                    float grad = (float)y / fh;
                    fernTex.SetPixel(x, y, Color.Lerp(neonGreen, stemGreen, 1f - grad));
                }
                else
                {
                    fernTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        fernTex.Apply();
        fernSprite = Sprite.Create(fernTex, new Rect(0, 0, fw, fh), new Vector2(0.5f, 0.0f), 32f); // Pivot at bottom

        // 2. Bioluminescent Mushroom (32x32)
        int mw = 32, mh = 32;
        Texture2D mushTex = new Texture2D(mw, mh);
        mushTex.filterMode = FilterMode.Bilinear;
        mushTex.wrapMode = TextureWrapMode.Clamp;
        Color capCyan = new Color(0.0f, 0.85f, 1.0f, 1f);
        Color capSpot = new Color(0.7f, 0.1f, 1.0f, 1f);
        Color stalkWhite = new Color(0.8f, 0.95f, 1.0f, 0.8f);

        for (int y = 0; y < mh; y++)
        {
            for (int x = 0; x < mw; x++)
            {
                // Stalk: centered vertically at bottom half
                bool isStalk = (Mathf.Abs(x - mw * 0.5f) < 2.5f && y < 14);

                // Cap: semi-ellipse at top half
                float cx = mw * 0.5f;
                float cy = 13f;
                float dx = (x - cx) / 10f;
                float dy = (y - cy) / 8f;
                bool isCap = (dx * dx + dy * dy <= 1f && y >= 13);

                // Glowing spots on cap
                bool isSpot = false;
                if (isCap)
                {
                    float distSpot1 = Vector2.Distance(new Vector2(x, y), new Vector2(mw * 0.35f, 19f));
                    float distSpot2 = Vector2.Distance(new Vector2(x, y), new Vector2(mw * 0.65f, 18f));
                    float distSpot3 = Vector2.Distance(new Vector2(x, y), new Vector2(mw * 0.5f, 22f));
                    isSpot = (distSpot1 < 2.2f || distSpot2 < 2.2f || distSpot3 < 2.0f);
                }

                if (isSpot)
                {
                    mushTex.SetPixel(x, y, capSpot);
                }
                else if (isCap)
                {
                    // Glowing cyan cap
                    mushTex.SetPixel(x, y, capCyan);
                }
                else if (isStalk)
                {
                    mushTex.SetPixel(x, y, stalkWhite);
                }
                else
                {
                    mushTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        mushTex.Apply();
        mushroomSprite = Sprite.Create(mushTex, new Rect(0, 0, mw, mh), new Vector2(0.5f, 0.0f), 32f);

        // 3. Nova Star-Flower (32x40)
        int flw = 32, flh = 40;
        Texture2D flowerTex = new Texture2D(flw, flh);
        flowerTex.filterMode = FilterMode.Bilinear;
        flowerTex.wrapMode = TextureWrapMode.Clamp;
        Color petalPink = new Color(1.0f, 0.08f, 0.65f, 1f);
        Color coreGold = new Color(1.0f, 0.85f, 0.1f, 1f);
        Color stemColor = new Color(0.2f, 0.45f, 0.75f, 1f); // Glowing cyber-blue stem

        for (int y = 0; y < flh; y++)
        {
            for (int x = 0; x < flw; x++)
            {
                // Thin organic stalk
                float stalkX = flw * 0.5f + Mathf.Sin(y * 0.15f) * 2f;
                bool isStalk = (Mathf.Abs(x - stalkX) < 1.2f && y < 24);

                // Bloom core at top
                Vector2 bloomCenter = new Vector2(flw * 0.5f + Mathf.Sin(24f * 0.15f) * 2f, 26f);
                float distToBloom = Vector2.Distance(new Vector2(x, y), bloomCenter);

                // Five pointed star-petals
                bool isPetal = false;
                if (distToBloom < 9.5f && y >= 20)
                {
                    float angle = Mathf.Atan2(y - bloomCenter.y, x - bloomCenter.x) * Mathf.Rad2Deg;
                    // Add petal spike ripples
                    float petalStrength = Mathf.Cos(angle * 5f * Mathf.Deg2Rad) * 4f + 5.5f;
                    if (distToBloom < petalStrength)
                    {
                        isPetal = true;
                    }
                }

                bool isCore = (distToBloom < 2.5f && y >= 20);

                if (isCore)
                {
                    flowerTex.SetPixel(x, y, coreGold);
                }
                else if (isPetal)
                {
                    flowerTex.SetPixel(x, y, Color.Lerp(petalPink, coreGold, 0.2f));
                }
                else if (isStalk)
                {
                    flowerTex.SetPixel(x, y, stemColor);
                }
                else
                {
                    flowerTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        flowerTex.Apply();
        flowerSprite = Sprite.Create(flowerTex, new Rect(0, 0, flw, flh), new Vector2(0.5f, 0.0f), 32f);
    }

    private void SetupBackgroundPlanes()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // We mount background planes directly as children of the main camera
        // This ensures they stay perfectly locked and aligned to the player's viewport!
        Transform camTrans = mainCam.transform;

        // A. Mundane Background (Always visible)
        mundaneBgObj = new GameObject("MundaneBackground_Plane");
        mundaneBgObj.transform.SetParent(camTrans, false);
        // Slightly behind the wonder background
        mundaneBgObj.transform.localPosition = new Vector3(0f, 0f, backgroundZOffset + 0.1f);
        mundaneBgObj.transform.localScale = new Vector3(backgroundScaleMultiplier.x, backgroundScaleMultiplier.y, 1f);

        var mundaneSr = mundaneBgObj.AddComponent<SpriteRenderer>();
        mundaneSr.sprite = mundaneBgSprite;
        mundaneSr.drawMode = SpriteDrawMode.Tiled; // Tile it infinitely
        mundaneSr.size = new Vector2(4f, 4f);      // Grid pattern tiling repeat
        mundaneSr.sortingOrder = -99;              // Render at the absolute bottom depth

        // B. Wonder Background (Only visible inside Wonder Radius!)
        wonderBgObj = new GameObject("WonderBackground_Plane");
        wonderBgObj.transform.SetParent(camTrans, false);
        wonderBgObj.transform.localPosition = new Vector3(0f, 0f, backgroundZOffset);
        wonderBgObj.transform.localScale = new Vector3(backgroundScaleMultiplier.x, backgroundScaleMultiplier.y, 1f);

        var wonderSr = wonderBgObj.AddComponent<SpriteRenderer>();
        wonderSr.sprite = wonderBgSprite;
        wonderSr.drawMode = SpriteDrawMode.Tiled;
        wonderSr.size = new Vector2(4f, 4f);
        wonderSr.material = wonderMaskMaterial; // Reveal ONLY inside active Wonder Radius!
        wonderSr.sortingOrder = -98;
    }

    private void ScanAndSpawnFlora()
    {
        // Find all Ground solid colliders in the scene to decorate
        var colliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);

        foreach (var col in colliders)
        {
            // Only decorate platforms that belong to layer 8 "Ground" or have static platforms
            if (col.gameObject.layer != 8 && !col.gameObject.name.Contains("Platform") && !col.gameObject.name.Contains("Ground"))
                continue;

            // Ensure it is a solid (non-trigger) horizontal platform
            if (col.isTrigger || col.size.x < 1.0f)
                continue;

            Vector2 center = col.bounds.center;
            Vector2 size = col.bounds.size;

            float topY = center.y + size.y * 0.5f;
            float leftX = center.x - size.x * 0.5f;
            float rightX = center.x + size.x * 0.5f;

            // Determine spacing: spawn plant every 0.6 to 1.2 units along the platform width
            float currentX = leftX + 0.5f;
            while (currentX < rightX - 0.5f)
            {
                if (Random.value < spawnChance)
                {
                    SpawnPlantItem(currentX, topY);
                }
                currentX += Random.Range(0.7f, 1.3f);
            }
        }
    }

    private void SpawnPlantItem(float x, float y)
    {
        int type = Random.Range(0, 3);
        Sprite targetSprite = fernSprite;
        string plantName = "GlowFern";
        float baseScale = Random.Range(0.7f, 1.1f);

        if (type == 1)
        {
            targetSprite = mushroomSprite;
            plantName = "ShroomFlora";
            baseScale = Random.Range(0.6f, 0.9f);
        }
        else if (type == 2)
        {
            targetSprite = flowerSprite;
            plantName = "NovaFlower";
            baseScale = Random.Range(0.65f, 0.95f);
        }

        GameObject plant = new GameObject(plantName);
        plant.transform.SetParent(this.transform);
        plant.transform.position = new Vector3(x, y, -0.05f); // Render slightly in front of platforms
        plant.transform.localScale = new Vector3(baseScale, baseScale, 1f);

        var sr = plant.AddComponent<SpriteRenderer>();
        sr.sprite = targetSprite;
        sr.material = wonderMaskMaterial; // Mask it so it only appears inside Wonder Lens!
        sr.sortingOrder = 1;              // Render in front of ground blocks

        // Track state for wave wind-sway & breathing animations
        floraTransforms.Add(plant.transform);
        floraSwayOffsets.Add(Random.Range(0f, 100f));
        floraSwaySpeeds.Add(Random.Range(2.2f, 3.8f));
        floraBaseScales.Add(baseScale);
    }

    private void Update()
    {
        // Animate all spawned foliage in the wind!
        int count = floraTransforms.Count;
        for (int i = 0; i < count; i++)
        {
            Transform trans = floraTransforms[i];
            if (trans == null) continue;

            // 1. Organic wind sway (rotates around bottom pivot)
            float swayAngle = Mathf.Sin(Time.time * floraSwaySpeeds[i] + floraSwayOffsets[i]) * windSwayRange;
            trans.localRotation = Quaternion.Euler(0f, 0f, swayAngle);

            // 2. Bioluminescent scale breathing pulse
            float baseScale = floraBaseScales[i];
            float scalePulse = baseScale * (1f + 0.08f * Mathf.Sin(Time.time * 2f + floraSwayOffsets[i]));
            trans.localScale = new Vector3(scalePulse, scalePulse, 1f);
        }
    }
}
