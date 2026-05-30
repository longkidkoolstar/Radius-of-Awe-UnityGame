using UnityEngine;

/// <summary>
/// A modular script to handle slide-open animations for gates when unlocked.
/// Features smooth interpolation and rumbling screen shake.
/// </summary>
public class SlidingGate : MonoBehaviour
{
    [Header("Slide Settings")]
    [SerializeField] private Vector3 slideOffset = new Vector3(0f, 3.5f, 0f);
    [SerializeField] private float duration = 1.4f;
    
    [Header("Screenshake Feedback")]
    [SerializeField] private bool triggerShake = true;
    [SerializeField] private float shakeIntensity = 0.18f;
    [SerializeField] private float shakeDuration = 0.12f;

    private bool isOpening = false;

    /// <summary>
    /// Starts the smooth slide-open animation sequence.
    /// </summary>
    [ContextMenu("Open Gate")]
    public void OpenGate()
    {
        if (!isOpening)
        {
            StartCoroutine(SlideOpenSequence());
        }
    }

    private System.Collections.IEnumerator SlideOpenSequence()
    {
        isOpening = true;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + slideOffset;
        float elapsed = 0f;

        // Trigger rumble screenshake
        if (triggerShake && CameraController2D.Instance != null)
        {
            CameraController2D.Instance.TriggerShake(shakeIntensity, shakeDuration);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smooth-step curve
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.localPosition = endPos;
        
        // Deactivate collider to allow passage completely
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Keep it open
        this.enabled = false;
    }
}
