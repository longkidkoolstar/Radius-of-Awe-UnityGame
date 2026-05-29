using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A ceiling pressure plate. Place it on the ceiling of a level.
/// Triggers when a floaty object (or the player) pushes upward against it.
/// Features a smooth physical compression animation, color shifts, and UnityEvent hooks.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CeilingButton : MonoBehaviour
{
    [Header("Activation Rules")]
    [Tooltip("If true, only objects containing a WonderObject script (e.g. Floaty Crates) can activate it.")]
    [SerializeField] private bool requireWonderObject = true;
    [Tooltip("If true, the activating object must be currently inside the Wonder Zone (so weightless).")]
    [SerializeField] private bool requireActiveWonderZoneState = true;

    [Header("Procedural Visuals")]
    [Tooltip("The moving child transform representing the button cap. Auto-finds child named 'Cap' if empty.")]
    [SerializeField] private Transform movingCap;
    [Tooltip("How far upward the cap moves when pressed.")]
    [SerializeField] private float pressDepth = 0.16f;
    [Tooltip("Visual transition speed for pushing and color shifting.")]
    [SerializeField] private float transitionSpeed = 9f;
    [Tooltip("Default color when released.")]
    [SerializeField] private Color normalColor = new Color(0.85f, 0.25f, 0.25f, 1.0f); // Sleek dull red
    [Tooltip("Glowing color when pressed.")]
    [SerializeField] private Color pressedColor = new Color(0.2f, 0.85f, 1.0f, 1.0f);   // Neon Cyan Glow

    [Header("Activation Events")]
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReleased;

    private BoxCollider2D triggerCollider;
    private SpriteRenderer capRenderer;
    private Vector3 releasedLocalPos;
    private Vector3 pressedLocalPos;
    private int overlappingObjectsCount = 0;
    private bool isPressed = false;

    /// <summary>Returns true if the pressure plate is currently pressed.</summary>
    public bool IsPressed => isPressed;

    private void Start()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        // Auto-detect button cap child
        if (movingCap == null) movingCap = transform.Find("Cap");
        if (movingCap == null) movingCap = transform;

        releasedLocalPos = movingCap.localPosition;
        pressedLocalPos = releasedLocalPos + new Vector3(0f, pressDepth, 0f); // Compress upward along local Y

        capRenderer = movingCap.GetComponent<SpriteRenderer>();
        if (capRenderer == null) capRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (capRenderer != null)
        {
            capRenderer.color = normalColor;
        }
    }

    private void Update()
    {
        // Smoothly slide the button cap to pressed/released position
        Vector3 targetPos = isPressed ? pressedLocalPos : releasedLocalPos;
        movingCap.localPosition = Vector3.Lerp(movingCap.localPosition, targetPos, Time.deltaTime * transitionSpeed);

        // Smoothly morph color
        if (capRenderer != null)
        {
            Color targetColor = isPressed ? pressedColor : normalColor;
            capRenderer.color = Color.Lerp(capRenderer.color, targetColor, Time.deltaTime * transitionSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (EvaluateCollision(collision))
        {
            overlappingObjectsCount++;
            EvaluateButtonState();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (EvaluateCollision(collision))
        {
            overlappingObjectsCount = Mathf.Max(0, overlappingObjectsCount - 1);
            EvaluateButtonState();
        }
    }

    /// <summary>
    /// Evaluates if the colliding object meets all activation requirements.
    /// </summary>
    private bool EvaluateCollision(Collider2D collision)
    {
        if (collision.isTrigger) return false;

        var wo = collision.GetComponent<WonderObject>();
        if (requireWonderObject && wo == null) return false;

        if (requireActiveWonderZoneState && wo != null && !wo.IsInWonderZone) return false;

        return true;
    }

    private void EvaluateButtonState()
    {
        bool shouldBePressed = overlappingObjectsCount > 0;
        
        if (shouldBePressed && !isPressed)
        {
            isPressed = true;
            onPressed?.Invoke();

            // Give the button press some weight with a minor screenshake click
            if (CameraController2D.Instance != null)
            {
                CameraController2D.Instance.TriggerShake(0.12f, 0.08f);
            }
        }
        else if (!shouldBePressed && isPressed)
        {
            isPressed = false;
            onReleased?.Invoke();
        }
    }
}
