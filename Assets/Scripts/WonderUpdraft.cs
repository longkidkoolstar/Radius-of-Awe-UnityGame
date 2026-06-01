using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A puzzle elevator element. Place a trigger BoxCollider2D in the scene.
/// In the mundane world, it is inert. In the Wonder world, it:
///   1. Spawns rising glowing flow particles in memory.
///   2. Applies a steady upward wind lift force to the Player or Floaty Crates!
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class WonderUpdraft : MonoBehaviour
{
    [Header("Updraft Lift Physics")]
    [Tooltip("Amount of upward force to apply to Rigidbody2D objects inside the tunnel.")]
    [SerializeField] private float upwardForce = 65f;
    [Tooltip("Maximum velocity a Rigidbody2D can reach while being lifted (keeps it stable).")]
    [SerializeField] private float maxLiftVelocity = 6.5f;

    [Header("Bioluminescent Flow Visuals")]
    [Tooltip("Number of procedural wind lines inside the draft.")]
    [SerializeField] private int flowLineCount = 14;
    [Tooltip("Vibrant color of the upward glowing wind streams.")]
    [SerializeField] private Color flowColor = new Color(0.2f, 1.0f, 0.5f, 0.45f); // Neon Emerald Green
    [Tooltip("Rising speed of the flow visual lines.")]
    [SerializeField] private float flowSpeed = 2.8f;

    private BoxCollider2D triggerCollider;
    private bool isActiveInWonder = false;
    private List<Rigidbody2D> rigidbodiesInRange = new List<Rigidbody2D>();
    private AudioSource windLoopSource;

    // Visual pool
    private List<Transform> flowLines = new List<Transform>();
    private Material wonderMaterial;
    private Sprite lineSprite;

    private void Start()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        // Initialize runtime wonder mask material
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

        GenerateLineSprite();
        SetupFlowLines();
    }

    /// <summary>
    /// Generates a thin vertical vector line texture programmatically in memory.
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
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.75f));
            }
        }
        tex.Apply();
        lineSprite = Sprite.Create(tex, new Rect(0, 0, 4, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    private void SetupFlowLines()
    {
        Vector2 size = triggerCollider.size;
        for (int i = 0; i < flowLineCount; i++)
        {
            GameObject go = new GameObject("UpdraftFlowLine");
            go.transform.SetParent(this.transform, false);

            // Spawn at random coordinates inside the trigger volume
            float rx = Random.Range(-size.x * 0.5f, size.x * 0.5f);
            float ry = Random.Range(-size.y * 0.5f, size.y * 0.5f);
            go.transform.localPosition = new Vector3(rx, ry, 0.1f);

            // Scale to thin line shapes
            go.transform.localScale = new Vector3(0.08f, Random.Range(0.5f, 1.4f), 1f);

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
        // Query the static API using the updraft's trigger collider bounds
        bool wasActive = isActiveInWonder;
        isActiveInWonder = WonderRadiusController.IsInsideWonderZone(triggerCollider);

        // Wind loop transition
        if (isActiveInWonder && !wasActive)
        {
            if (windLoopSource == null)
            {
                windLoopSource = AudioManager.PlayLoopAtPoint(AudioManager.Instance.updraftClip, transform.position, 0.4f);
            }
        }
        else if (!isActiveInWonder && wasActive)
        {
            if (windLoopSource != null)
            {
                AudioManager.StopLoop(windLoopSource, 0.25f);
                windLoopSource = null;
            }
        }

        // Animate wind lines
        Vector2 size = triggerCollider.size;
        foreach (var line in flowLines)
        {
            var sr = line.GetComponent<SpriteRenderer>();
            if (isActiveInWonder)
            {
                // Rise
                Vector3 pos = line.localPosition;
                pos.y += flowSpeed * Time.deltaTime;

                // Recycle back to bottom when leaving
                if (pos.y > size.y * 0.5f)
                {
                    pos.y = -size.y * 0.5f;
                    pos.x = Random.Range(-size.x * 0.5f, size.x * 0.5f);
                }
                line.localPosition = pos;

                // Fade in
                sr.color = Color.Lerp(sr.color, flowColor, Time.deltaTime * 6f);
            }
            else
            {
                // Fade out when inactive
                sr.color = Color.Lerp(sr.color, Color.clear, Time.deltaTime * 6f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isActiveInWonder) return;

        // Apply physical upward lift forces to Rigidbody2Ds
        for (int i = rigidbodiesInRange.Count - 1; i >= 0; i--)
        {
            var rb = rigidbodiesInRange[i];
            if (rb == null)
            {
                rigidbodiesInRange.RemoveAt(i);
                continue;
            }

            // Apply direct lift force up to a terminal draft speed
            if (rb.velocity.y < maxLiftVelocity)
            {
                rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Force);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null && !rigidbodiesInRange.Contains(rb))
        {
            rigidbodiesInRange.Add(rb);
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

    private void OnDisable()
    {
        if (windLoopSource != null)
        {
            AudioManager.StopLoop(windLoopSource, 0.15f);
            windLoopSource = null;
        }
    }
}
