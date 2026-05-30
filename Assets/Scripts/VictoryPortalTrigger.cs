using UnityEngine;

/// <summary>
/// Triggers victory screenshake and celebration when the player enters the Success Portal.
/// Also handles continuous swirling (rotation) and organic pulsing visual effects.
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

    private void Start()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Cache base local scale
        baseScale = transform.localScale;
    }

    private void Update()
    {
        // 1. Swirl effect: Constant rotation around the Z axis
        transform.Rotate(Vector3.forward, -swirlSpeed * Time.deltaTime);

        // 2. Pulse effect: Whimsical organic breathing scale pulsation
        float pulse = Mathf.Sin(Time.time * pulseFrequency) * pulseAmount;
        transform.localScale = baseScale * (1f + pulse);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !triggered)
        {
            triggered = true;
            
            // Heavy cinematic victory rumble!
            if (CameraController2D.Instance != null)
            {
                CameraController2D.Instance.TriggerShake(0.48f, 0.32f);
            }

            Debug.Log("<b><color=#ffcc00>[VICTORY]</color></b>: Level completed! Step into the portal!");
        }
    }
}
