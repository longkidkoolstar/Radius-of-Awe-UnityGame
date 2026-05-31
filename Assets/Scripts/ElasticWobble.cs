using UnityEngine;

/// <summary>
/// A code-driven procedural aesthetics script that applies elastic spring deformation (squash & stretch)
/// to a GameObject's local scale. Ideal for visual feedback on impact, activation, or landing.
/// Utilizes the classic damped harmonic oscillator equation for premium-feeling spring physics.
/// </summary>
public class ElasticWobble : MonoBehaviour
{
    private Vector3 baseScale = Vector3.one;
    private Vector3 wobbleVector = Vector3.zero;
    
    // Oscillation variables
    private float frequency = 12f;
    private float damping = 4f;
    private float timer = 0f;
    private bool isWobbling = false;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    /// <summary>
    /// Triggers a spring-based squash and stretch wobble effect.
    /// </summary>
    /// <param name="force">Vector3 representing the initial deformation magnitude and direction (e.g. new Vector3(0.2f, -0.2f, 0)).</param>
    /// <param name="customFrequency">The speed of oscillation cycles.</param>
    /// <param name="customDamping">How quickly the oscillation decays back to standard scale.</param>
    public void TriggerWobble(Vector3 force, float customFrequency = 14f, float customDamping = 5.5f)
    {
        // Add cumulative wobble forces for continuous hits
        wobbleVector = force;
        frequency = customFrequency;
        damping = customDamping;
        timer = 0f;
        isWobbling = true;
    }

    private void Update()
    {
        if (!isWobbling) return;

        timer += Time.deltaTime;

        // Damped harmonic oscillation equation: x(t) = A * cos(w * t) * e^(-c * t)
        float decay = Mathf.Exp(-damping * timer);
        
        if (decay < 0.005f)
        {
            // Settle completely to prevent perpetual tiny matrix multiplies
            transform.localScale = baseScale;
            isWobbling = false;
            return;
        }

        float osc = Mathf.Cos(frequency * timer);
        Vector3 currentWobble = wobbleVector * osc * decay;

        // Apply deformation preserving base scale values
        transform.localScale = baseScale + currentWobble;
    }

    /// <summary>
    /// Resets the scale back to the absolute base and stops any active wobbles immediately.
    /// </summary>
    public void ResetToBaseline()
    {
        isWobbling = false;
        transform.localScale = baseScale;
    }
}
