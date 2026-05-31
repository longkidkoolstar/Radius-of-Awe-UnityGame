using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Triggers victory screenshake and celebration when the player enters the Success Portal.
/// Handles spatial warping effects, gravitational pull, and spaghettification animations!
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

    private bool triggered = false;
    private Vector3 baseScale;

    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private Volume warpVolume;

    private void Start()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Cache base local scale
        baseScale = transform.localScale;

        // Find Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }

        SetupWarpVolume();
    }

    private void SetupWarpVolume()
    {
        warpVolume = gameObject.AddComponent<Volume>();
        warpVolume.isGlobal = true;
        warpVolume.weight = 0f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        LensDistortion lens = ScriptableObject.CreateInstance<LensDistortion>();
        lens.active = true;
        lens.intensity.Override(-0.65f); // Massive pinch effect
        lens.scale.Override(1.1f);
        profile.components.Add(lens);

        ChromaticAberration chrom = ScriptableObject.CreateInstance<ChromaticAberration>();
        chrom.active = true;
        chrom.intensity.Override(1.0f); // High color separation at edges
        profile.components.Add(chrom);

        warpVolume.profile = profile;
    }

    private void Update()
    {
        // 1. Swirl effect: Constant rotation around the Z axis
        transform.Rotate(Vector3.forward, -swirlSpeed * Time.deltaTime);

        // 2. Pulse effect: Whimsical organic breathing scale pulsation
        float pulse = Mathf.Sin(Time.time * pulseFrequency) * pulseAmount;
        transform.localScale = baseScale * (1f + pulse);

        // 3. Black Hole physics and screen warping!
        if (playerTransform != null && !triggered)
        {
            float dist = Vector2.Distance(playerTransform.position, transform.position);
            
            // Effect starts when player is within 12 units
            float maxEffectDist = 12f;
            float warpWeight = Mathf.Clamp01(1f - (dist / maxEffectDist));
            
            // Ease in the visual effect exponentially so it's smooth then intense near the center
            warpVolume.weight = Mathf.Pow(warpWeight, 2.5f);

            // Gravitational physical pull
            if (playerRb != null && dist < maxEffectDist)
            {
                Vector2 pullDir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                // Pull gets stronger the closer you are
                float pullStrength = Mathf.Lerp(0f, 18f, warpWeight);
                playerRb.AddForce(pullDir * pullStrength * Time.deltaTime * 60f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !triggered)
        {
            triggered = true;
            
            // Disable Player Controller so they can't escape the singularity
            var pController = collision.GetComponent<PlayerController2D>();
            if (pController != null) pController.enabled = false;
            
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
                playerRb.isKinematic = true; // Stop gravity so they don't fall while being sucked in
            }

            // Start spaghettification animation
            StartCoroutine(SpaghettifyRoutine(collision.transform));

            // Heavy cinematic victory rumble!
            if (CameraController2D.Instance != null)
            {
                CameraController2D.Instance.TriggerShake(0.65f, 0.4f);
            }

            Debug.Log("<b><color=#ffcc00>[VICTORY]</color></b>: Level completed! Step into the portal!");
        }
    }

    private IEnumerator SpaghettifyRoutine(Transform target)
    {
        float duration = 1.4f;
        float elapsed = 0f;
        Vector3 startScale = target.localScale;
        Vector3 startPos = target.position;
        
        // Push warp volume to absolute maximum intensity
        warpVolume.weight = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Lerp position to EXACT center of portal (ease in quadratic)
            target.position = Vector3.Lerp(startPos, transform.position, t * t);
            
            // Spin incredibly fast
            target.Rotate(Vector3.forward, 1500f * Time.deltaTime);
            
            // Shrink down into the singularity
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
            
            // Make swirl effect of portal spin exponentially faster!
            swirlSpeed += 800f * Time.deltaTime;

            yield return null;
        }

        target.gameObject.SetActive(false);

        // Fade out screen distortion after absorbed
        while (warpVolume.weight > 0f)
        {
            warpVolume.weight -= Time.deltaTime * 1.5f;
            yield return null;
        }
    }
}
