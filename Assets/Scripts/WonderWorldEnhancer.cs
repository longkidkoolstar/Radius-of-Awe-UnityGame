using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A code-driven visual aesthetics system that creates a literal "window into another universe" portal effect.
/// Implements 5 high-fidelity layers of procedural 2D parallax scrolling (Mundane Sky, Skyline silhouette, Concrete wall, 
/// Wonder space nebula, and floating celestial runes) using a seamless position-wrapping formula.
/// Also scatters beautiful, invisible organic bioluminescent plants (ferns, mushrooms, star-flowers) that sprout and sway under the Wonder Radius!
/// </summary>
public class WonderWorldEnhancer : MonoBehaviour
{
    [Header("Dynamic Background Depth")]
    [SerializeField] private float backgroundZBase = 12f;
    [SerializeField] private Vector2 bgPlaneSize = new Vector2(100f, 60f);

    [Header("Flora Settings")]
    [Tooltip("Probability of a plant spawning at each platform interval.")]
    [SerializeField] private float spawnChance = 0.35f; // Decreased spawn rate for clean layout
    [SerializeField] private float windSwayRange = 12f;

    // Generated assets
    private Sprite mundaneSkySprite;
    private Sprite mundaneSkylineSprite;
    private Sprite mundaneWallSprite;
    private Sprite wonderNebulaSprite;
    private Sprite wonderRunesSprite;
    private Sprite fernSprite;
    private Sprite mushroomSprite;
    private Sprite flowerSprite;

    private Material wonderMaskMaterial;
    
    // Parallax background GameObjects
    private GameObject mundaneSkyObj;
    private GameObject mundaneSkylineObj;
    private GameObject mundaneWallObj;
    private GameObject wonderNebulaObj;
    private GameObject wonderRunesObj;

    private Camera mainCam;

    // Tracker lists for flora animation
    private List<Transform> floraTransforms = new List<Transform>();
    private List<float> floraSwayOffsets = new List<float>();
    private List<float> floraSwaySpeeds = new List<float>();
    private List<float> floraBaseScales = new List<float>();

    private void Start()
    {
        mainCam = Camera.main;

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

        // 2. Generate procedural textures & sprites in memory
        GenerateProceduralSprites();

        // 3. Setup decoupled world-space parallax background layers
        SetupBackgroundPlanes();

        // 4. Scan the environment and spawn hidden plants
        ScanAndSpawnFlora();
    }

    private void GenerateProceduralSprites()
    {
        float ppu = 16f;

        // --- 1. Mundane Far Sky Sprite (16x256 vertical gradient, PPU = 16) ---
        int skyW = 16;
        int skyH = 256;
        Texture2D skyTex = new Texture2D(skyW, skyH);
        skyTex.filterMode = FilterMode.Bilinear;
        skyTex.wrapMode = TextureWrapMode.Clamp;

        Color skyBottom = new Color(0.08f, 0.09f, 0.11f, 1f); // Dark carbon grey
        Color skyTop = new Color(0.04f, 0.05f, 0.06f, 1f);    // Industrial slate black

        for (int y = 0; y < skyH; y++)
        {
            float t = (float)y / skyH;
            Color rowColor = Color.Lerp(skyBottom, skyTop, t);
            for (int x = 0; x < skyW; x++)
            {
                skyTex.SetPixel(x, y, rowColor);
            }
        }
        skyTex.Apply();
        mundaneSkySprite = Sprite.Create(skyTex, new Rect(0, 0, skyW, skyH), new Vector2(0.5f, 0.5f), ppu);

        // --- 2. Mundane Mid Skyline Silhouette (256x64 tiles, PPU = 16) ---
        int lineW = 256;
        int lineH = 64;
        Texture2D lineTex = new Texture2D(lineW, lineH);
        lineTex.filterMode = FilterMode.Bilinear;
        lineTex.wrapMode = TextureWrapMode.Repeat;

        Color skylineColor = new Color(0.07f, 0.08f, 0.1f, 0.8f); // Semi-opaque industrial silhouette
        Color windowLight = new Color(0.85f, 0.7f, 0.2f, 0.6f);     // Weak yellow window lights
        Color redBulb = new Color(0.9f, 0.1f, 0.1f, 1f);

        for (int y = 0; y < lineH; y++)
        {
            for (int x = 0; x < lineW; x++)
            {
                bool isFilled = false;
                bool isWindow = false;
                bool isRedBulb = false;

                // Wide factory tower
                if (x >= 24 && x < 52 && y < 44)
                {
                    isFilled = true;
                    // Windows every 8 pixels
                    if (x % 8 >= 2 && x % 8 <= 5 && y % 10 >= 3 && y % 10 <= 6 && y < 36 && x > 28 && x < 48)
                    {
                        isWindow = true;
                    }
                }
                // Wide cooling tower
                else if (x >= 96 && x < 136 && y < 32)
                {
                    isFilled = true;
                }
                // Tall chimney stack
                else if (x >= 180 && x < 190 && y < 56)
                {
                    isFilled = true;
                    if (y == 55 && x >= 184 && x <= 186)
                    {
                        isRedBulb = true;
                    }
                }
                // Connecting girder frames/beams
                else if ((y == 16 && x >= 52 && x < 96) || (y == 24 && x >= 136 && x < 180))
                {
                    if (x % 16 < 3 || Mathf.Abs((x % 16) - y % 16) < 1.5f)
                    {
                        isFilled = true;
                    }
                }

                if (isRedBulb)
                {
                    lineTex.SetPixel(x, y, redBulb);
                }
                else if (isWindow && Random.value > 0.3f)
                {
                    lineTex.SetPixel(x, y, windowLight);
                }
                else if (isFilled)
                {
                    lineTex.SetPixel(x, y, skylineColor);
                }
                else
                {
                    lineTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        lineTex.Apply();
        mundaneSkylineSprite = Sprite.Create(lineTex, new Rect(0, 0, lineW, lineH), new Vector2(0.5f, 0.0f), ppu); // Pivot at bottom

        // --- 3. Mundane Near Concrete Wall (256x256, PPU = 16) ---
        int bgSize = 256;
        Texture2D mundaneTex = new Texture2D(bgSize, bgSize);
        mundaneTex.filterMode = FilterMode.Bilinear;
        mundaneTex.wrapMode = TextureWrapMode.Repeat;

        Color concreteBase = new Color(0.12f, 0.13f, 0.15f, 1f);
        Color grooveLight = new Color(0.2f, 0.21f, 0.24f, 1f);
        Color grooveDark = new Color(0.08f, 0.08f, 0.1f, 1f);

        for (int y = 0; y < bgSize; y++)
        {
            for (int x = 0; x < bgSize; x++)
            {
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
        mundaneWallSprite = Sprite.Create(mundaneTex, new Rect(0, 0, bgSize, bgSize), new Vector2(0.5f, 0.5f), ppu);

        // --- 4. Wonder Far Background (Space Nebula, 256x256, PPU = 16) ---
        Texture2D wonderTex = new Texture2D(bgSize, bgSize);
        wonderTex.filterMode = FilterMode.Bilinear;
        wonderTex.wrapMode = TextureWrapMode.Repeat;

        Color spaceVoid = new Color(0.03f, 0.02f, 0.06f, 1f);

        for (int y = 0; y < bgSize; y++)
        {
            for (int x = 0; x < bgSize; x++)
            {
                float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(bgSize * 0.35f, bgSize * 0.4f)) / bgSize;
                float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(bgSize * 0.7f, bgSize * 0.75f)) / bgSize;

                float nebulaPurple = Mathf.Clamp01(1f - d1 * 1.8f);
                float nebulaMagenta = Mathf.Clamp01(1f - d2 * 2.2f);

                Color gas = new Color(0.1f * nebulaPurple, 0.04f * nebulaMagenta, 0.18f * nebulaPurple, 0f);
                Color pixelColor = spaceVoid + gas;

                // Tiled Stars
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
        wonderNebulaSprite = Sprite.Create(wonderTex, new Rect(0, 0, bgSize, bgSize), new Vector2(0.5f, 0.5f), ppu);

        // --- 5. Wonder Mid Background (Space Celestial Runes, 256x256, PPU = 16) ---
        Texture2D runesTex = new Texture2D(bgSize, bgSize);
        runesTex.filterMode = FilterMode.Bilinear;
        runesTex.wrapMode = TextureWrapMode.Repeat;
        Color neonCyan = new Color(0f, 0.8f, 1.0f, 0.35f); // Glowing translucent cyan

        for (int y = 0; y < bgSize; y++)
        {
            for (int x = 0; x < bgSize; x++)
            {
                bool isRunePixel = false;

                // Glowing circular star ring at center (64, 64)
                float distCenter = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f));
                if (Mathf.Abs(distCenter - 32f) < 1.2f) isRunePixel = true;

                // Diagonal crosshairs in the ring
                if (distCenter < 32f)
                {
                    if (Mathf.Abs(x - y) <= 1 || Mathf.Abs(x - (128 - y)) <= 1)
                    {
                        if (x % 4 == 0) isRunePixel = true;
                    }
                }

                // Cyber coordinate grid lines
                if (x % 128 == 0 || y % 128 == 0)
                {
                    if ((x + y) % 8 == 0) isRunePixel = true;
                }

                // Tiny coordinate crosshairs at (192, 192)
                float distCross = Vector2.Distance(new Vector2(x, y), new Vector2(192f, 192f));
                if ((distCross < 10f && (x == 192 || y == 192)) || (distCross < 6f && distCross > 4.5f))
                {
                    isRunePixel = true;
                }

                if (isRunePixel)
                {
                    runesTex.SetPixel(x, y, neonCyan);
                }
                else
                {
                    runesTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        runesTex.Apply();
        wonderRunesSprite = Sprite.Create(runesTex, new Rect(0, 0, bgSize, bgSize), new Vector2(0.5f, 0.5f), ppu);

        // --- 6. Bioluminescent Flora Sprites ---
        // A. Curl Fern (32x48)
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
                float spineX = fw * 0.4f + Mathf.Sin(y * 0.08f) * 6f;
                float distToSpine = Mathf.Abs(x - spineX);

                bool isSpine = distToSpine < 1.2f;
                bool isLeaf = false;
                
                if (y > 4 && y % 5 <= 1)
                {
                    float leafWidth = (fh - y) * 0.35f + 1f;
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
        fernSprite = Sprite.Create(fernTex, new Rect(0, 0, fw, fh), new Vector2(0.5f, 0.0f), 32f);

        // B. Bioluminescent Mushroom (32x32)
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
                bool isStalk = (Mathf.Abs(x - mw * 0.5f) < 2.5f && y < 14);

                float cx = mw * 0.5f;
                float cy = 13f;
                float dx = (x - cx) / 10f;
                float dy = (y - cy) / 8f;
                bool isCap = (dx * dx + dy * dy <= 1f && y >= 13);

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

        // C. Nova Star-Flower (32x40)
        int flw = 32, flh = 40;
        Texture2D flowerTex = new Texture2D(flw, flh);
        flowerTex.filterMode = FilterMode.Bilinear;
        flowerTex.wrapMode = TextureWrapMode.Clamp;
        Color petalPink = new Color(1.0f, 0.08f, 0.65f, 1f);
        Color coreGold = new Color(1.0f, 0.85f, 0.1f, 1f);
        Color stemColor = new Color(0.2f, 0.45f, 0.75f, 1f);

        for (int y = 0; y < flh; y++)
        {
            for (int x = 0; x < flw; x++)
            {
                float stalkX = flw * 0.5f + Mathf.Sin(y * 0.15f) * 2f;
                bool isStalk = (Mathf.Abs(x - stalkX) < 1.2f && y < 24);

                Vector2 bloomCenter = new Vector2(flw * 0.5f + Mathf.Sin(24f * 0.15f) * 2f, 26f);
                float distToBloom = Vector2.Distance(new Vector2(x, y), bloomCenter);

                bool isPetal = false;
                if (distToBloom < 9.5f && y >= 20)
                {
                    float angle = Mathf.Atan2(y - bloomCenter.y, x - bloomCenter.x) * Mathf.Rad2Deg;
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
        // Decouple background planes from camera parenting to allow smooth world space parallax!
        // Instantiated at their target sorting orders and depths

        // A. Mundane Far Sky Layer (Parallax = 0.92, Z = 12.0)
        mundaneSkyObj = new GameObject("Parallax_MundaneSky");
        var skySr = mundaneSkyObj.AddComponent<SpriteRenderer>();
        skySr.sprite = mundaneSkySprite;
        skySr.drawMode = SpriteDrawMode.Tiled;
        skySr.size = bgPlaneSize;
        skySr.sortingOrder = -100;

        // B. Wonder Far Nebula Layer (Parallax = 0.85, Z = 11.9, Wonder Masked!)
        wonderNebulaObj = new GameObject("Parallax_WonderNebula");
        var nebulaSr = wonderNebulaObj.AddComponent<SpriteRenderer>();
        nebulaSr.sprite = wonderNebulaSprite;
        nebulaSr.drawMode = SpriteDrawMode.Tiled;
        nebulaSr.size = bgPlaneSize;
        nebulaSr.material = wonderMaskMaterial;
        nebulaSr.sortingOrder = -99;

        // C. Mundane Mid Skyline Layer (Parallax = 0.55, Z = 11.5)
        mundaneSkylineObj = new GameObject("Parallax_MundaneSkyline");
        var lineSr = mundaneSkylineObj.AddComponent<SpriteRenderer>();
        lineSr.sprite = mundaneSkylineSprite;
        lineSr.drawMode = SpriteDrawMode.Tiled;
        // Make skyline flat wide strip (height = 4 units natively)
        lineSr.size = new Vector2(bgPlaneSize.x, 6f);
        lineSr.sortingOrder = -98;

        // D. Wonder Mid Runes Layer (Parallax = 0.40, Z = 11.4, Wonder Masked!)
        wonderRunesObj = new GameObject("Parallax_WonderRunes");
        var runesSr = wonderRunesObj.AddComponent<SpriteRenderer>();
        runesSr.sprite = wonderRunesSprite;
        runesSr.drawMode = SpriteDrawMode.Tiled;
        runesSr.size = bgPlaneSize;
        runesSr.material = wonderMaskMaterial;
        runesSr.sortingOrder = -97;

        // E. Mundane Near Wall Layer (Parallax = 0.22, Z = 10.5)
        mundaneWallObj = new GameObject("Parallax_MundaneWall");
        var wallSr = mundaneWallObj.AddComponent<SpriteRenderer>();
        wallSr.sprite = mundaneWallSprite;
        wallSr.drawMode = SpriteDrawMode.Tiled;
        wallSr.size = bgPlaneSize;
        wallSr.sortingOrder = -96;
    }

    private void ScanAndSpawnFlora()
    {
        var colliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);

        foreach (var col in colliders)
        {
            if (col.gameObject.layer != 8 && !col.gameObject.name.Contains("Platform") && !col.gameObject.name.Contains("Ground"))
                continue;

            if (col.isTrigger || col.size.x < 1.0f)
                continue;

            Vector2 center = col.bounds.center;
            Vector2 size = col.bounds.size;

            float topY = center.y + size.y * 0.5f;
            float leftX = center.x - size.x * 0.5f;
            float rightX = center.x + size.x * 0.5f;

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
        plant.transform.position = new Vector3(x, y, -0.05f);
        plant.transform.localScale = new Vector3(baseScale, baseScale, 1f);

        var sr = plant.AddComponent<SpriteRenderer>();
        sr.sprite = targetSprite;
        sr.material = wonderMaskMaterial;
        sr.sortingOrder = 1;

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

            float swayAngle = Mathf.Sin(Time.time * floraSwaySpeeds[i] + floraSwayOffsets[i]) * windSwayRange;
            trans.localRotation = Quaternion.Euler(0f, 0f, swayAngle);

            float baseScale = floraBaseScales[i];
            float scalePulse = baseScale * (1f + 0.08f * Mathf.Sin(Time.time * 2f + floraSwayOffsets[i]));
            trans.localScale = new Vector3(scalePulse, scalePulse, 1f);
        }
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        float camX = mainCam.transform.position.x;
        float camY = mainCam.transform.position.y;

        // Our procedural tiled textures natively repeat every 16.0 units in world space!
        float tileSize = 16.0f;

        // --- 1. Mundane Far Sky Layer (Parallax: 0.92, Z: Base + 1.0, Follows camera Y) ---
        float skyFactor = 0.92f;
        float skyOffsetX = (camX * (1f - skyFactor)) % tileSize;
        mundaneSkyObj.transform.position = new Vector3(camX - skyOffsetX, camY, backgroundZBase + 1.0f);

        // --- 2. Wonder Far Nebula Layer (Parallax: 0.85, Z: Base + 0.9, Follows camera Y) ---
        float nebulaFactor = 0.85f;
        float nebulaOffsetX = (camX * (1f - nebulaFactor)) % tileSize;
        wonderNebulaObj.transform.position = new Vector3(camX - nebulaOffsetX, camY, backgroundZBase + 0.9f);

        // --- 3. Mundane Mid Skyline Layer (Parallax: 0.55, Z: Base + 0.5, Follows camera Y but lowered slightly) ---
        float lineFactor = 0.55f;
        float lineOffsetX = (camX * (1f - lineFactor)) % tileSize;
        // Keep the skyline placed nicely near the ground floor (Y Offset = -1.2 units)
        mundaneSkylineObj.transform.position = new Vector3(camX - lineOffsetX, camY - 1.2f, backgroundZBase + 0.5f);

        // --- 4. Wonder Mid Runes Layer (Parallax: 0.40, Z: Base + 0.4, Full XY Parallax!) ---
        float runesFactor = 0.40f;
        float runesOffsetX = (camX * (1f - runesFactor)) % tileSize;
        float runesOffsetY = (camY * (1f - runesFactor)) % tileSize;
        wonderRunesObj.transform.position = new Vector3(camX - runesOffsetX, camY - runesOffsetY, backgroundZBase + 0.4f);

        // --- 5. Mundane Near Wall Layer (Parallax: 0.22, Z: Base, Full XY Parallax!) ---
        float wallFactor = 0.22f;
        float wallOffsetX = (camX * (1f - wallFactor)) % tileSize;
        float wallOffsetY = (camY * (1f - wallFactor)) % tileSize;
        mundaneWallObj.transform.position = new Vector3(camX - wallOffsetX, camY - wallOffsetY, backgroundZBase);
    }
}
