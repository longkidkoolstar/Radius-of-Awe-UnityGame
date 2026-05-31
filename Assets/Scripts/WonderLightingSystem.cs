using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// A dynamic 2D lighting system that leverages URP's real Light2D components.
/// Creates atmospheric ambient lighting, wonder-zone radiance, floating firefly lights,
/// platform edge markers, screen-space vignette, and god-ray beam overlays.
/// Attach to an empty GameObject in the scene root.
/// </summary>
public class WonderLightingSystem : MonoBehaviour
{
    [Header("Player Lantern Light")]
    [SerializeField] private bool enablePlayerLight = true;
    [SerializeField] private float playerLightRadius = 5f;
    [SerializeField] private Color playerLightMundane = new Color(1f, 0.85f, 0.6f, 1f);
    [SerializeField] private Color playerLightWonder  = new Color(0.3f, 0.85f, 1f, 1f);
    [SerializeField] private float playerLightIntensity = 1.2f;
    [SerializeField] private float playerLightPulseSpeed = 2.2f;

    [Header("Wonder Zone Radiance")]
    [SerializeField] private bool enableZoneRadiance = true;
    [SerializeField] private Color zoneColor = new Color(0.5f, 0.25f, 1f, 1f);
    [SerializeField] private float zoneIntensity = 0.8f;

    [Header("Floating Fireflies")]
    [SerializeField] private bool enableFireflies = true;
    [SerializeField] private int fireflyCount = 14;
    [SerializeField] private float fireflySpawnRadius = 10f;
    [SerializeField] private float fireflyDriftSpeed = 0.4f;
    [SerializeField] private float fireflyLightRadius = 1.2f;

    [Header("Platform Edge Lights")]
    [SerializeField] private bool enableEdgeLights = true;
    [SerializeField] private Color edgeLightColor = new Color(0.0f, 0.85f, 1f, 1f);
    [SerializeField] private float edgeLightRadius = 1.5f;
    [SerializeField] private float edgeLightIntensity = 0.6f;

    [Header("Atmospheric Vignette")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private Color vignetteColor = new Color(0.02f, 0.01f, 0.05f, 0.55f);

    [Header("God Rays / Volumetric Beams")]
    [SerializeField] private bool enableGodRays = true;
    [SerializeField] private int godRayCount = 4;
    [SerializeField] private Color godRayColor = new Color(0.5f, 0.3f, 0.8f, 0.04f);

    // Generated sprites (for vignette & god rays — still sprite-based screen effects)
    private Sprite vignetteSprite;
    private Sprite godRaySprite;
    private Material additiveMat;

    // Runtime: Player Light
    private Light2D playerLight2D;
    private GameObject playerLightObj;

    // Runtime: Zone Radiance
    private Light2D zoneLight2D;
    private GameObject zoneLightObj;

    // Firefly state
    private List<Transform> fireflyTransforms = new List<Transform>();
    private List<Light2D> fireflyLights = new List<Light2D>();
    private List<Vector3> fireflyBasePositions = new List<Vector3>();
    private List<float> fireflyPhaseOffsets = new List<float>();
    private List<Color> fireflyColors = new List<Color>();

    // Edge light state
    private List<Light2D> edgeLights = new List<Light2D>();

    // Vignette
    private GameObject vignetteObj;
    private SpriteRenderer vignetteSR;

    // God rays
    private List<Transform> godRayTransforms = new List<Transform>();
    private List<SpriteRenderer> godRaySRs = new List<SpriteRenderer>();
    private List<float> godRayPhases = new List<float>();

    // Cached refs
    private Transform playerTransform;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // Create additive material for screen-space overlays
        Shader additiveShader = Shader.Find("Sprites/AdditiveGlow");
        if (additiveShader != null)
            additiveMat = new Material(additiveShader);
        else
            additiveMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));

        GenerateScreenSprites();

        if (enablePlayerLight)  SetupPlayerLight();
        if (enableZoneRadiance) SetupZoneRadiance();
        if (enableFireflies)    SetupFireflies();
        if (enableEdgeLights)   SetupEdgeLights();
        if (enableVignette)     SetupVignette();
        if (enableGodRays)      SetupGodRays();
    }

    // ──────────────────────────────────────────────────────────────────
    //  SPRITE GENERATION (only for screen-space overlays)
    // ──────────────────────────────────────────────────────────────────
    private void GenerateScreenSprites()
    {
        // Vignette
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1f) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float norm = Mathf.Clamp01(dist / center);
                float alpha = Mathf.Pow(norm, 1.8f);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        tex.Apply();
        vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 0.5f);

        // God ray beam
        int gw = 16, gh = 128;
        Texture2D godTex = new Texture2D(gw, gh);
        godTex.filterMode = FilterMode.Bilinear;
        godTex.wrapMode = TextureWrapMode.Clamp;
        float centerX = (gw - 1f) * 0.5f;
        for (int y = 0; y < gh; y++)
        {
            for (int x = 0; x < gw; x++)
            {
                float hDist = Mathf.Abs(x - centerX) / centerX;
                float hFade = Mathf.Pow(1f - hDist, 3f);
                float vNorm = (float)y / (gh - 1);
                float vFade = Mathf.Sin(vNorm * Mathf.PI);
                godTex.SetPixel(x, y, new Color(1f, 1f, 1f, hFade * vFade));
            }
        }
        godTex.Apply();
        godRaySprite = Sprite.Create(godTex, new Rect(0, 0, gw, gh), new Vector2(0.5f, 0.5f), 16f);
    }

    // ──────────────────────────────────────────────────────────────────
    //  PLAYER LANTERN LIGHT (real URP Light2D)
    // ──────────────────────────────────────────────────────────────────
    private void SetupPlayerLight()
    {
        // Check if the player already has a Light2D child (from scene setup)
        if (playerTransform != null)
        {
            var existingLight = playerTransform.GetComponentInChildren<Light2D>();
            if (existingLight != null)
            {
                playerLight2D = existingLight;
                playerLightObj = existingLight.gameObject;
                return;
            }
        }

        playerLightObj = new GameObject("PlayerLanternLight2D");
        playerLightObj.transform.SetParent(this.transform);
        if (playerTransform != null)
            playerLightObj.transform.position = playerTransform.position;

        playerLight2D = playerLightObj.AddComponent<Light2D>();
        playerLight2D.lightType = Light2D.LightType.Point;
        playerLight2D.color = playerLightMundane;
        playerLight2D.intensity = playerLightIntensity;
        playerLight2D.pointLightOuterRadius = playerLightRadius;
        playerLight2D.pointLightInnerRadius = playerLightRadius * 0.3f;
    }

    // ──────────────────────────────────────────────────────────────────
    //  WONDER ZONE RADIANCE (real URP Light2D)
    // ──────────────────────────────────────────────────────────────────
    private void SetupZoneRadiance()
    {
        zoneLightObj = new GameObject("WonderZoneRadiance2D");
        zoneLightObj.transform.SetParent(this.transform);

        zoneLight2D = zoneLightObj.AddComponent<Light2D>();
        zoneLight2D.lightType = Light2D.LightType.Point;
        zoneLight2D.color = zoneColor;
        zoneLight2D.intensity = 0f; // Start invisible
        zoneLight2D.pointLightOuterRadius = 5f;
        zoneLight2D.pointLightInnerRadius = 1f;
    }

    // ──────────────────────────────────────────────────────────────────
    //  FIREFLIES (real URP Light2D point lights)
    // ──────────────────────────────────────────────────────────────────
    private void SetupFireflies()
    {
        Color[] palette = new Color[]
        {
            new Color(0.2f, 1f, 0.6f, 1f),
            new Color(0.1f, 0.8f, 1f, 1f),
            new Color(1f, 0.7f, 0.1f, 1f),
            new Color(0.9f, 0.3f, 1f, 1f),
            new Color(1f, 0.4f, 0.7f, 1f),
            new Color(0.4f, 0.95f, 0.85f, 1f),
        };

        for (int i = 0; i < fireflyCount; i++)
        {
            GameObject fly = new GameObject($"Firefly2D_{i}");
            fly.transform.SetParent(this.transform);

            Vector3 basePos = new Vector3(
                Random.Range(-fireflySpawnRadius, fireflySpawnRadius),
                Random.Range(-fireflySpawnRadius * 0.6f, fireflySpawnRadius * 0.6f),
                0f
            );
            fly.transform.position = basePos;

            Light2D light = fly.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            Color col = palette[Random.Range(0, palette.Length)];
            light.color = col;
            light.intensity = 0f; // Start off
            light.pointLightOuterRadius = fireflyLightRadius * Random.Range(0.6f, 1.4f);
            light.pointLightInnerRadius = light.pointLightOuterRadius * 0.2f;

            fireflyTransforms.Add(fly.transform);
            fireflyLights.Add(light);
            fireflyBasePositions.Add(basePos);
            fireflyPhaseOffsets.Add(Random.Range(0f, Mathf.PI * 2f));
            fireflyColors.Add(col);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  PLATFORM EDGE LIGHTS (real URP Light2D)
    // ──────────────────────────────────────────────────────────────────
    private void SetupEdgeLights()
    {
        var colliders = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);

        foreach (var col in colliders)
        {
            if (col.gameObject.layer != 8 &&
                !col.gameObject.name.Contains("Platform") &&
                !col.gameObject.name.Contains("Ground"))
                continue;

            if (col.isTrigger || col.size.x < 1.0f)
                continue;

            Vector2 center = col.bounds.center;
            Vector2 size = col.bounds.size;
            float topY = center.y + size.y * 0.5f + 0.1f;
            float leftX = center.x - size.x * 0.5f;
            float rightX = center.x + size.x * 0.5f;

            SpawnEdgeLight2D(new Vector3(leftX + 0.15f, topY, 0f));
            SpawnEdgeLight2D(new Vector3(rightX - 0.15f, topY, 0f));

            if (size.x > 5f)
            {
                int mids = Mathf.FloorToInt(size.x / 3.5f);
                for (int m = 1; m < mids; m++)
                {
                    float t = (float)m / mids;
                    float midX = Mathf.Lerp(leftX + 0.5f, rightX - 0.5f, t);
                    SpawnEdgeLight2D(new Vector3(midX, topY, 0f));
                }
            }
        }
    }

    private void SpawnEdgeLight2D(Vector3 pos)
    {
        GameObject obj = new GameObject("EdgeLight2D");
        obj.transform.SetParent(this.transform);
        obj.transform.position = pos;

        Light2D light = obj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = edgeLightColor;
        light.intensity = edgeLightIntensity;
        light.pointLightOuterRadius = edgeLightRadius;
        light.pointLightInnerRadius = edgeLightRadius * 0.15f;

        edgeLights.Add(light);
    }

    // ──────────────────────────────────────────────────────────────────
    //  VIGNETTE (screen-space sprite overlay)
    // ──────────────────────────────────────────────────────────────────
    private void SetupVignette()
    {
        if (mainCam == null) return;

        vignetteObj = new GameObject("ScreenVignette");
        vignetteObj.transform.SetParent(mainCam.transform, false);
        vignetteObj.transform.localPosition = new Vector3(0f, 0f, 1f);

        vignetteSR = vignetteObj.AddComponent<SpriteRenderer>();
        vignetteSR.sprite = vignetteSprite;
        vignetteSR.sortingOrder = 100;
        vignetteSR.color = vignetteColor;

        float orthoSize = mainCam.orthographicSize;
        float aspect = mainCam.aspect;
        float scaleY = orthoSize * 2f * 1.1f;
        float scaleX = scaleY * aspect;
        vignetteObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    // ──────────────────────────────────────────────────────────────────
    //  GOD RAYS (screen-space sprite overlays)
    // ──────────────────────────────────────────────────────────────────
    private void SetupGodRays()
    {
        if (mainCam == null) return;

        for (int i = 0; i < godRayCount; i++)
        {
            GameObject ray = new GameObject($"GodRay_{i}");
            ray.transform.SetParent(mainCam.transform, false);

            float xPos = Mathf.Lerp(-mainCam.orthographicSize * mainCam.aspect * 0.8f,
                                     mainCam.orthographicSize * mainCam.aspect * 0.8f,
                                     (float)i / Mathf.Max(1, godRayCount - 1));
            ray.transform.localPosition = new Vector3(xPos, mainCam.orthographicSize * 0.2f, 2f);
            ray.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
            ray.transform.localScale = new Vector3(Random.Range(1.5f, 3f), Random.Range(3f, 5f), 1f);

            SpriteRenderer sr = ray.AddComponent<SpriteRenderer>();
            sr.sprite = godRaySprite;
            sr.material = additiveMat;
            sr.sortingOrder = 88;
            sr.color = godRayColor;

            godRayTransforms.Add(ray.transform);
            godRaySRs.Add(sr);
            godRayPhases.Add(Random.Range(0f, Mathf.PI * 2f));
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  UPDATE LOOP
    // ──────────────────────────────────────────────────────────────────
    private void Update()
    {
        UpdatePlayerLight();
        UpdateZoneRadiance();
        UpdateFireflies();
        UpdateEdgeLights();
        UpdateGodRays();
        UpdateVignette();
    }

    private void UpdatePlayerLight()
    {
        if (!enablePlayerLight || playerLight2D == null || playerTransform == null) return;

        // Follow player (if not parented already)
        if (playerLightObj.transform.parent != playerTransform)
        {
            playerLightObj.transform.position = playerTransform.position + Vector3.up * 0.3f;
        }

        // Color shift based on wonder proximity
        bool inWonder = WonderRadiusController.IsInsideWonderZone(playerTransform.position);
        Color targetColor = inWonder ? playerLightWonder : playerLightMundane;
        playerLight2D.color = Color.Lerp(playerLight2D.color, targetColor, Time.deltaTime * 4f);

        // Breathing pulse on intensity and radius
        float pulse = Mathf.Sin(Time.time * playerLightPulseSpeed) * 0.5f + 0.5f;
        playerLight2D.intensity = Mathf.Lerp(playerLightIntensity * 0.85f, playerLightIntensity * 1.15f, pulse);
        playerLight2D.pointLightOuterRadius = Mathf.Lerp(playerLightRadius * 0.95f, playerLightRadius * 1.05f, pulse);
    }

    private void UpdateZoneRadiance()
    {
        if (!enableZoneRadiance || zoneLight2D == null) return;

        var controller = FindFirstObjectByType<WonderRadiusController>();
        if (controller == null) return;

        Vector3 wCenter = controller.Center;
        float wRadius = controller.Radius;
        bool active = controller.IsActive && wRadius > 0.1f;

        zoneLightObj.transform.position = wCenter;

        float targetIntensity = active ? zoneIntensity : 0f;
        zoneLight2D.intensity = Mathf.Lerp(zoneLight2D.intensity, targetIntensity, Time.deltaTime * 5f);
        zoneLight2D.pointLightOuterRadius = wRadius * 1.3f;
        zoneLight2D.pointLightInnerRadius = wRadius * 0.3f;
    }

    private void UpdateFireflies()
    {
        if (!enableFireflies) return;

        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        for (int i = 0; i < fireflyTransforms.Count; i++)
        {
            Transform t = fireflyTransforms[i];
            if (t == null) continue;

            float phase = fireflyPhaseOffsets[i];
            float time = Time.time;

            // Lissajous drifting
            Vector3 drift = new Vector3(
                Mathf.Sin(time * fireflyDriftSpeed * 0.7f + phase) * 1.5f +
                Mathf.Sin(time * fireflyDriftSpeed * 0.3f + phase * 2f) * 0.8f,
                Mathf.Cos(time * fireflyDriftSpeed * 0.5f + phase) * 1.0f +
                Mathf.Sin(time * fireflyDriftSpeed * 0.2f + phase * 3f) * 0.5f,
                0f
            );

            Vector3 targetPos = playerPos + fireflyBasePositions[i] + drift;
            t.position = Vector3.Lerp(t.position, targetPos, Time.deltaTime * 2f);

            // Twinkling intensity
            float twinkle = Mathf.Sin(time * (1.5f + phase * 0.3f) + phase) * 0.5f + 0.5f;
            float slowBreath = Mathf.Sin(time * 0.4f + phase * 1.5f) * 0.5f + 0.5f;
            float alpha = twinkle * slowBreath;

            // Brighter near wonder zone
            float normalDist = WonderRadiusController.GetNormalizedDistance(t.position);
            float wonderBoost = normalDist < 1.2f ? 2.5f :
                               normalDist < 2f ? Mathf.Lerp(2.5f, 0.3f, (normalDist - 1.2f) / 0.8f) : 0.3f;

            fireflyLights[i].intensity = alpha * wonderBoost * 0.7f;
        }
    }

    private void UpdateEdgeLights()
    {
        if (!enableEdgeLights) return;

        for (int i = 0; i < edgeLights.Count; i++)
        {
            if (edgeLights[i] == null) continue;

            // Only visible inside wonder zone
            bool inWonder = WonderRadiusController.IsInsideWonderZone(edgeLights[i].transform.position);
            float targetIntensity = inWonder ? edgeLightIntensity : 0f;

            // Per-light staggered pulse
            float stagger = Mathf.Sin(Time.time * 2.5f + i * 1.3f) * 0.5f + 0.5f;
            targetIntensity *= Mathf.Lerp(0.5f, 1f, stagger);

            edgeLights[i].intensity = Mathf.Lerp(edgeLights[i].intensity, targetIntensity, Time.deltaTime * 8f);
        }
    }

    private void UpdateGodRays()
    {
        if (!enableGodRays) return;

        for (int i = 0; i < godRayTransforms.Count; i++)
        {
            Transform t = godRayTransforms[i];
            if (t == null) continue;

            float phase = godRayPhases[i];
            float angle = Mathf.Sin(Time.time * 0.3f + phase) * 8f;
            t.localRotation = Quaternion.Euler(0, 0, angle + (phase * 10f - 15f));

            float breath = Mathf.Sin(Time.time * 0.5f + phase * 2f) * 0.5f + 0.5f;
            Color c = godRayColor;
            c.a = godRayColor.a * Mathf.Lerp(0.3f, 1f, breath);

            bool playerInWonder = playerTransform != null &&
                                  WonderRadiusController.IsInsideWonderZone(playerTransform.position);
            if (playerInWonder)
            {
                c.a *= 2f;
                c = Color.Lerp(c, new Color(0.3f, 0.6f, 1f, c.a), 0.5f);
            }

            godRaySRs[i].color = c;
        }
    }

    private void UpdateVignette()
    {
        if (!enableVignette || vignetteSR == null) return;

        bool playerInWonder = playerTransform != null &&
                              WonderRadiusController.IsInsideWonderZone(playerTransform.position);

        Color target = vignetteColor;
        if (playerInWonder)
        {
            target.a *= 0.5f;
            target.r = Mathf.Lerp(target.r, 0.08f, 0.3f);
            target.b = Mathf.Lerp(target.b, 0.12f, 0.3f);
        }

        vignetteSR.color = Color.Lerp(vignetteSR.color, target, Time.deltaTime * 3f);

        if (mainCam != null)
        {
            float orthoSize = mainCam.orthographicSize;
            float aspect = mainCam.aspect;
            float scaleY = orthoSize * 2f * 1.1f;
            float scaleX = scaleY * aspect;
            vignetteObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }
}