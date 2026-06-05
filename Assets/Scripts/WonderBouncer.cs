using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A kinetic launch pad puzzle element. Place a trigger BoxCollider2D in the scene.
/// In the mundane world, it is inert. In the Wonder world, it:
///   1. Spawns glowing lines flowing in the bounce direction.
///   2. Launches the Player or Floaty Crates in the specified direction with high velocity!
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class WonderBouncer : MonoBehaviour
{
    [Header("Bouncer Physics")]
    [Tooltip("Direction to launch the physics objects.")]
    [SerializeField] private Vector2 bounceDirection = Vector2.up;
    [Tooltip("Target velocity magnitude applied to launched objects.")]
    [SerializeField] private float bounceForce = 15f;
    [Tooltip("Cooldown in seconds between bounces for the same object to prevent double launches.")]
    [SerializeField] private float bounceCooldown = 0.3f;

    [Header("Visual Styling")]
    [Tooltip("Number of procedural flow lines showing the launch direction.")]
    [SerializeField] private int flowLineCount = 8;
    [Tooltip("Glowing color of the launch flow lines.")]
    [SerializeField] private Color flowColor = new Color(1.0f, 0.0f, 0.55f, 0.7f); // Neon Magenta/Pink
    [Tooltip("Movement speed of the flow lines.")]
    [SerializeField] private float flowSpeed = 4.5f;

    private BoxCollider2D triggerCollider;
    private bool isActiveInWonder = false;
    private List<Rigidbody2D> rigidbodiesInRange = new List<Rigidbody2D>();
    private Dictionary<Rigidbody2D, float> bounceCooldowns = new Dictionary<Rigidbody2D, float>();

    // Visual pool
    private List<Transform> flowLines = new List<Transform>();
    private Material wonderMaterial;
    private Sprite lineSprite;

    private void Start()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        bounceDirection.Normalize();

        // Initialize runtime wonder mask material
        Shader wonderShader = Shader.Find("Sprites/WonderMask");
        if (wonderShader != null)
        {
            wonderMaterial = new Material(wonderShader);
            wonderMaterial.SetFloat("_Feather", 0.25f);
        }
        else
        {
            wonderMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        }

        GenerateLineSprite();
        SetupFlowLines();
    }

    /// <summary>
    /// Generates a thin line texture in memory.
    /// </summary>
    private void GenerateLineSprite()
    {
        Texture2D tex = new Texture2D(4, 16);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                float dy = 1f - Mathf.Abs(y - 7.5f) / 7.5f;
                float alpha = Mathf.Clamp01(dy);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.8f));
            }
        }
        tex.Apply();
        lineSprite = Sprite.Create(tex, new Rect(0, 0, 4, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    private void SetupFlowLines()
    {
        Vector2 size = triggerCollider.size;
        float angle = Mathf.Atan2(bounceDirection.y, bounceDirection.x) * Mathf.Rad2Deg - 90f; // Align sprite to direction

        for (int i = 0; i < flowLineCount; i++)
        {
            GameObject go = new GameObject("BouncerFlowLine");
            go.transform.SetParent(this.transform, false);

            // Spawn randomly in the trigger bounds
            float rx = Random.Range(-size.x * 0.45f, size.x * 0.45f);
            float ry = Random.Range(-size.y * 0.45f, size.y * 0.45f);
            go.transform.localPosition = new Vector3(rx, ry, 0.1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Scale to thin line shapes
            go.transform.localScale = new Vector3(0.06f, Random.Range(0.4f, 1.0f), 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = lineSprite;
            sr.material = wonderMaterial;
            sr.color = flowColor;
            sr.sortingOrder = -1; // Render behind characters

            flowLines.Add(go.transform);
        }
    }

    private void Update()
    {
        bool wasActive = isActiveInWonder;
        isActiveInWonder = WonderRadiusController.IsInsideWonderZone(triggerCollider);

        // Update cooldowns
        List<Rigidbody2D> keys = new List<Rigidbody2D>(bounceCooldowns.Keys);
        foreach (var key in keys)
        {
            bounceCooldowns[key] -= Time.deltaTime;
            if (bounceCooldowns[key] <= 0f)
            {
                bounceCooldowns.Remove(key);
            }
        }

        // Animate flow lines in bounceDirection
        Vector2 size = triggerCollider.size;
        foreach (var line in flowLines)
        {
            var sr = line.GetComponent<SpriteRenderer>();
            if (isActiveInWonder)
            {
                // Move line in local direction corresponding to bounceDirection
                Vector3 localDir = transform.InverseTransformDirection(bounceDirection);
                line.localPosition += localDir * flowSpeed * Time.deltaTime;

                // Project line onto bounceDirection axis to check bounds
                float dot = Vector3.Dot(line.localPosition, localDir);
                float maxBound = Vector3.Dot(size * 0.5f, Vector3.one); // Approximation of box radius

                if (dot > maxBound * 0.5f)
                {
                    // Recycle to opposite side
                    line.localPosition = -localDir * maxBound * 0.5f + 
                                         transform.InverseTransformDirection(new Vector3(Random.Range(-size.x * 0.4f, size.x * 0.4f), Random.Range(-size.y * 0.4f, size.y * 0.4f), 0f));
                    // Keep alignment
                    float angle = Mathf.Atan2(bounceDirection.y, bounceDirection.x) * Mathf.Rad2Deg - 90f;
                    line.localRotation = Quaternion.Euler(0f, 0f, angle);
                }

                sr.color = Color.Lerp(sr.color, flowColor, Time.deltaTime * 8f);
            }
            else
            {
                sr.color = Color.Lerp(sr.color, Color.clear, Time.deltaTime * 8f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isActiveInWonder) return;

        // Perform launch logic
        for (int i = rigidbodiesInRange.Count - 1; i >= 0; i--)
        {
            var rb = rigidbodiesInRange[i];
            if (rb == null)
            {
                rigidbodiesInRange.RemoveAt(i);
                continue;
            }

            if (!bounceCooldowns.ContainsKey(rb))
            {
                BounceObject(rb);
            }
        }
    }

    private void BounceObject(Rigidbody2D rb)
    {
        // Apply direct velocity override for consistent and clean kinetic launches
        rb.velocity = bounceDirection * bounceForce;
        bounceCooldowns[rb] = bounceCooldown;

        // Play launch effects
        AudioManager.PlaySporeWhoosh(rb.transform.position);
        AudioManager.PlayWonderObjectEnter(rb.transform.position);

        Debug.Log($"<b><color=#ff0088>[BOUNCER]</color></b>: Launched {rb.name} in direction {bounceDirection} with force {bounceForce}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null && !rigidbodiesInRange.Contains(rb))
        {
            rigidbodiesInRange.Add(rb);
            
            // If already in Wonder Zone, launch immediately upon entry
            if (isActiveInWonder && !bounceCooldowns.ContainsKey(rb))
            {
                BounceObject(rb);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null && rigidbodiesInRange.Contains(rb))
        {
            rigidbodiesInRange.Remove(rb);
        }
    }
}
