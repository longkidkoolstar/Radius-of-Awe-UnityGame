using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class WonderPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    [Tooltip("The destination portal linked to this one.")]
    public WonderPortal linkedPortal;
    [Tooltip("Optional exit spawn point. If null, will spawn with an offset in the exit facing direction.")]
    public Transform exitPoint;
    [Tooltip("Velocity scale when exiting this portal.")]
    [SerializeField] private float exitVelocityMultiplier = 1f;
    [Tooltip("Whether to rotate the velocity to match the exit portal's facing direction (local UP).")]
    [SerializeField] private bool preserveMomentum = true;
    [Tooltip("Cooldown in seconds before this portal can teleport the same object again.")]
    [SerializeField] private float cooldownTime = 0.3f;

    [Header("Visual Effects")]
    [Tooltip("Portal glowing effect color in Wonder zone.")]
    [SerializeField] private Color portalColor = new Color(0.1f, 0.8f, 1.0f, 0.8f); // Cyan/blue portal
    [Tooltip("Color when inactive in Mundane.")]
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 0.2f);

    private BoxCollider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    private bool isActiveInWonder = false;
    private Dictionary<Rigidbody2D, float> cooldowns = new Dictionary<Rigidbody2D, float>();
    private AudioSource loopAudioSource;

    private void Start()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Find or create visual styling materials
        Shader wonderShader = Shader.Find("Sprites/WonderMask");
        if (wonderShader != null && spriteRenderer != null)
        {
            spriteRenderer.material = new Material(wonderShader);
        }
    }

    private void Update()
    {
        // Check if inside Wonder Zone
        isActiveInWonder = WonderRadiusController.IsInsideWonderZone(triggerCollider);

        // Update visuals based on status
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActiveInWonder ? portalColor : inactiveColor;
        }

        // Handle loop sound
        if (isActiveInWonder)
        {
            if (loopAudioSource == null && AudioManager.Instance != null && AudioManager.Instance.portalLoopClip != null)
            {
                loopAudioSource = AudioManager.PlayLoopAtPoint(AudioManager.Instance.portalLoopClip, transform.position, 0.4f);
            }
        }
        else
        {
            if (loopAudioSource != null)
            {
                AudioManager.StopLoop(loopAudioSource, 0.2f);
                loopAudioSource = null;
            }
        }

        // Tick down cooldowns
        List<Rigidbody2D> keys = new List<Rigidbody2D>(cooldowns.Keys);
        foreach (var key in keys)
        {
            cooldowns[key] -= Time.deltaTime;
            if (cooldowns[key] <= 0f)
            {
                cooldowns.Remove(key);
            }
        }
    }

    private void OnDestroy()
    {
        if (loopAudioSource != null)
        {
            AudioManager.StopLoop(loopAudioSource, 0f);
        }
    }

    public void AddCooldown(Rigidbody2D rb, float duration)
    {
        cooldowns[rb] = duration;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActiveInWonder || linkedPortal == null) return;

        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (cooldowns.ContainsKey(rb)) return;

            // Teleport the object!
            TeleportObject(rb);
        }
    }

    private void TeleportObject(Rigidbody2D rb)
    {
        // 1. Calculate destination position
        Vector3 targetPosition;
        if (linkedPortal.exitPoint != null)
        {
            targetPosition = linkedPortal.exitPoint.position;
        }
        else
        {
            // Fallback: exit portal center + offset in exit facing direction (local UP)
            targetPosition = linkedPortal.transform.position + linkedPortal.transform.up * 1.5f;
        }

        // 2. Set cooldowns on both portals
        this.AddCooldown(rb, cooldownTime);
        linkedPortal.AddCooldown(rb, cooldownTime);

        // 3. Update velocity
        if (preserveMomentum)
        {
            float speed = rb.velocity.magnitude;
            // Point the velocity in the exit direction of the destination portal
            rb.velocity = linkedPortal.transform.up * speed * exitVelocityMultiplier;
        }
        else if (exitVelocityMultiplier > 0f)
        {
            rb.velocity = linkedPortal.transform.up * exitVelocityMultiplier;
        }

        // 4. Update position
        rb.transform.position = new Vector3(targetPosition.x, targetPosition.y, rb.transform.position.z);

        // Play warp audio at entry and exit
        AudioManager.PlayWonderObjectEnter(transform.position);
        AudioManager.PlaySporeWhoosh(targetPosition);

        Debug.Log($"<b><color=#00ccff>[PORTAL]</color></b>: Teleported {rb.name} from {name} to {linkedPortal.name}. Preserved velocity: {rb.velocity}");
    }
}
