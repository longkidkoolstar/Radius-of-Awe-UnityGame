using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A state-of-the-art, fully self-contained Procedural Synthesized Audio Manager.
/// Automatically initializes itself before the first scene loads, mathematically compiles
/// retro-premium synthesized AudioClips in memory, and handles 2D/3D spatialized audio playback
/// and looping fade-outs for an immersive, zero-asset gameplay experience.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance => instance;

    // Samplerate for high fidelity
    private const int SAMPLERATE = 44100;

    // Cached procedural AudioClips
    public AudioClip jumpClip { get; private set; }
    public AudioClip landClip { get; private set; }
    public AudioClip footstepClip { get; private set; }
    public AudioClip toggleOnClip { get; private set; }
    public AudioClip toggleOffClip { get; private set; }
    public AudioClip buttonPressClip { get; private set; }
    public AudioClip buttonReleaseClip { get; private set; }
    public AudioClip gateSlideClip { get; private set; }
    public AudioClip gateLockClip { get; private set; }
    public AudioClip updraftClip { get; private set; }
    public AudioClip portalLoopClip { get; private set; }
    public AudioClip driftStartClip { get; private set; }
    public AudioClip sporeWhooshClip { get; private set; }
    public AudioClip victoryChimeClip { get; private set; }
    public AudioClip wonderObjectEnterClip { get; private set; }

    /// <summary>
    /// Automatically instantiates the AudioManager before any scene is loaded.
    /// Eliminates any manual editor drag-and-drop or setup work!
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("AudioManager");
            instance = go.AddComponent<AudioManager>();
            DontDestroyOnLoad(go);
            
            Debug.Log("<b><color=#00ff88>[AUDIO]</color></b>: Procedural AudioManager auto-initialized successfully!");
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        CompileProceduralSFX();
    }

    /// <summary>
    /// Generates all AudioClips programmatically on start.
    /// </summary>
    private void CompileProceduralSFX()
    {
        // 1. Jump
        jumpClip = CreateClip("Jump", GenerateJumpSamples());
        // 2. Land
        landClip = CreateClip("Land", GenerateLandSamples());
        // 3. Footstep
        footstepClip = CreateClip("Footstep", GenerateFootstepSamples());
        // 4. Wonder Toggle On
        toggleOnClip = CreateClip("ToggleOn", GenerateToggleOnSamples());
        // 5. Wonder Toggle Off
        toggleOffClip = CreateClip("ToggleOff", GenerateToggleOffSamples());
        // 6. Button Press
        buttonPressClip = CreateClip("ButtonPress", GenerateButtonPressSamples());
        // 7. Button Release
        buttonReleaseClip = CreateClip("ButtonRelease", GenerateButtonReleaseSamples());
        // 8. Gate Slide (Loopable)
        gateSlideClip = CreateClip("GateSlide", GenerateGateSlideSamples());
        // 9. Gate Lock
        gateLockClip = CreateClip("GateLock", GenerateGateLockSamples());
        // 10. Updraft wind (Loopable)
        updraftClip = CreateClip("Updraft", GenerateUpdraftSamples());
        // 11. Portal Loop (Loopable)
        portalLoopClip = CreateClip("PortalLoop", GeneratePortalLoopSamples());
        // 12. Drift Start
        driftStartClip = CreateClip("DriftStart", GenerateDriftStartSamples());
        // 13. Spore Whoosh
        sporeWhooshClip = CreateClip("SporeWhoosh", GenerateSporeWhooshSamples());
        // 14. Victory Chime
        victoryChimeClip = CreateClip("VictoryChime", GenerateVictorySamples());
        // 15. Wonder Object Enter
        wonderObjectEnterClip = CreateClip("WonderObjectEnter", GenerateWonderObjectEnterSamples());

        Debug.Log("<b><color=#00ccff>[AUDIO]</color></b>: All 15 procedural audio clips synthesized successfully!");
    }

    private AudioClip CreateClip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SAMPLERATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ==========================================
    // SYNTHESIS WAVEFORM ALGORITHMS
    // ==========================================

    private float[] GenerateJumpSamples()
    {
        float duration = 0.16f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            // Fast quadratic frequency sweep upward for a snappy jump sound
            float freq = Mathf.Lerp(260f, 850f, progress * progress);
            float phase = 2f * Mathf.PI * freq * t;
            
            // Fundamental sine wave + subtle 1st harmonic for warmth
            float wave = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f;
            
            // Decaying envelope
            float env = Mathf.Exp(-progress * 5.5f) * (1f - progress);
            samples[i] = wave * env * 0.28f;
        }
        return samples;
    }

    private float[] GenerateLandSamples()
    {
        float duration = 0.25f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        float noiseVal = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            // Deep sub sweep downward
            float freq = Mathf.Lerp(130f, 40f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            float sineWave = Mathf.Sin(phase);
            
            // Low pass noise to simulate dust thud
            noiseVal = noiseVal * 0.91f + UnityEngine.Random.Range(-1f, 1f) * 0.09f;
            
            float wave = sineWave * 0.55f + noiseVal * 0.45f;
            float env = Mathf.Exp(-progress * 7.5f) * (1f - progress);
            samples[i] = wave * env * 0.38f;
        }
        return samples;
    }

    private float[] GenerateFootstepSamples()
    {
        float duration = 0.04f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        float lastNoise = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            // High-pass filter noise via 1st order derivative
            float rawNoise = UnityEngine.Random.Range(-1f, 1f);
            float hpNoise = rawNoise - lastNoise;
            lastNoise = rawNoise;
            
            float env = Mathf.Exp(-progress * 16f) * (1f - progress);
            samples[i] = hpNoise * env * 0.08f;
        }
        return samples;
    }

    private float[] GenerateToggleOnSamples()
    {
        float duration = 0.45f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            // Exponential pitch sweep up
            float freq = Mathf.Lerp(190f, 980f, progress * progress);
            float phase1 = 2f * Mathf.PI * freq * t;
            float phase2 = 2f * Mathf.PI * (freq * 1.5f) * t; // Perfect fifth chord
            float phase3 = 2f * Mathf.PI * (freq * 2.0f) * t; // Octave
            
            float wave = Mathf.Sin(phase1) * 0.5f + Mathf.Sin(phase2) * 0.3f + Mathf.Sin(phase3) * 0.2f;
            
            // Ambient tremolo LFO
            float tremolo = Mathf.Sin(2f * Mathf.PI * 12f * t) * 0.2f + 0.8f;
            
            // Swell envelope
            float env = 0f;
            if (progress < 0.15f) env = progress / 0.15f; // attack
            else env = Mathf.Exp(-(progress - 0.15f) * 4.5f) * (1f - progress); // decay
            
            samples[i] = wave * tremolo * env * 0.26f;
        }
        return samples;
    }

    private float[] GenerateToggleOffSamples()
    {
        float duration = 0.45f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            // Pitch sweep down
            float freq = Mathf.Lerp(880f, 130f, progress);
            float phase1 = 2f * Mathf.PI * freq * t;
            float phase2 = 2f * Mathf.PI * (freq * 1.2f) * t; // Minor third chord
            float phase3 = 2f * Mathf.PI * (freq * 1.5f) * t; // Fifth
            
            float wave = Mathf.Sin(phase1) * 0.5f + Mathf.Sin(phase2) * 0.3f + Mathf.Sin(phase3) * 0.2f;
            float env = Mathf.Exp(-progress * 3.5f) * (1f - progress);
            
            samples[i] = wave * env * 0.24f;
        }
        return samples;
    }

    private float[] GenerateButtonPressSamples()
    {
        float duration = 0.06f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            float freq = Mathf.Lerp(1600f, 950f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            float rawNoise = UnityEngine.Random.Range(-1f, 1f);
            
            // Combine sharp click chirp with mechanical noise burst
            float wave = Mathf.Sin(phase) * 0.65f + rawNoise * 0.35f;
            float env = Mathf.Exp(-progress * 16f) * (1f - progress);
            samples[i] = wave * env * 0.18f;
        }
        return samples;
    }

    private float[] GenerateButtonReleaseSamples()
    {
        float duration = 0.06f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            float freq = Mathf.Lerp(1100f, 650f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            float rawNoise = UnityEngine.Random.Range(-1f, 1f);
            
            float wave = Mathf.Sin(phase) * 0.65f + rawNoise * 0.35f;
            float env = Mathf.Exp(-progress * 13f) * (1f - progress);
            samples[i] = wave * env * 0.14f;
        }
        return samples;
    }

    private float[] GenerateGateSlideSamples()
    {
        float duration = 1.0f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        float noiseVal = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            
            // Grinding 60Hz mechanical hum
            float phase1 = 2f * Mathf.PI * 60f * t;
            float phase2 = 2f * Mathf.PI * 120f * t;
            float hum = Mathf.Sin(phase1) * 0.55f + Mathf.Sin(phase2) * 0.45f;
            
            // Mechanical rustle noise
            noiseVal = noiseVal * 0.95f + UnityEngine.Random.Range(-1f, 1f) * 0.05f;
            
            float wave = hum * 0.35f + noiseVal * 0.65f;
            samples[i] = wave * 0.16f; // hum level
        }
        return samples;
    }

    private float[] GenerateGateLockSamples()
    {
        float duration = 0.35f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        float noiseVal = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            float freq = Mathf.Lerp(85f, 35f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            float sineWave = Mathf.Sin(phase);
            
            noiseVal = noiseVal * 0.94f + UnityEngine.Random.Range(-1f, 1f) * 0.06f;
            
            float wave = sineWave * 0.5f + noiseVal * 0.5f;
            float env = Mathf.Exp(-progress * 6.5f) * (1f - progress);
            samples[i] = wave * env * 0.45f;
        }
        return samples;
    }

    private float[] GenerateUpdraftSamples()
    {
        float duration = 1.5f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            
            // Whistling wind pitch LFOs
            float lfo1 = Mathf.Sin(2f * Mathf.PI * 0.6f * t);
            float lfo2 = Mathf.Cos(2f * Mathf.PI * 0.4f * t);
            
            float f1 = 290f + lfo1 * 35f;
            float f2 = 390f + lfo2 * 50f;
            float f3 = 510f + Mathf.Sin(2f * Mathf.PI * 1.3f * t) * 25f;
            
            float whistle = Mathf.Sin(2f * Mathf.PI * f1 * t) * (0.35f + 0.25f * lfo2) +
                            Mathf.Sin(2f * Mathf.PI * f2 * t) * (0.2f + 0.15f * lfo1) +
                            Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.1f;
                            
            // Soft white noise
            float noise = UnityEngine.Random.Range(-1f, 1f) * 0.12f;
            
            samples[i] = (whistle + noise) * 0.1f;
        }
        return samples;
    }

    private float[] GeneratePortalLoopSamples()
    {
        float duration = 2.0f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            
            // Slow orbital LFO hum pulse
            float pulseLfo = Mathf.Sin(2f * Mathf.PI * 1.3f * t) * 0.2f + 0.8f;
            
            // Complex deep cosmic chord drone
            float chord = Mathf.Sin(2f * Mathf.PI * 75f * t) * 0.5f +
                          Mathf.Sin(2f * Mathf.PI * 112.5f * t) * 0.3f + // fifth chord component
                          Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.15f +
                          Mathf.Sin(2f * Mathf.PI * 225f * t) * 0.05f;
                          
            // High frequency orbital ring shimmer
            float shimmerFreq = 1150f + Mathf.Sin(2f * Mathf.PI * 5f * t) * 180f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * shimmerFreq * t) * 0.04f;
            
            samples[i] = (chord * pulseLfo + shimmer) * 0.14f;
        }
        return samples;
    }

    private float[] GenerateDriftStartSamples()
    {
        float duration = 4.0f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            // Decelerating engine hum
            float baseFreq = Mathf.Lerp(280f, 55f, progress * progress);
            float phase1 = 2f * Mathf.PI * baseFreq * t;
            float phase2 = 2f * Mathf.PI * (baseFreq * 1.5f) * t;
            float engine = Mathf.Sin(phase1) * 0.55f + Mathf.Sin(phase2) * 0.45f;
            
            // Sparkling rising stardust trail
            float riseFreq = Mathf.Lerp(180f, 1500f, progress);
            float rise = Mathf.Sin(2f * Mathf.PI * riseFreq * t) * 0.15f * progress;
            
            // Sine swell
            float env = Mathf.Sin(progress * Mathf.PI);
            samples[i] = (engine + rise) * env * 0.22f;
        }
        return samples;
    }

    private float[] GenerateSporeWhooshSamples()
    {
        float duration = 1.0f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        float lastFilter = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            // Frequency modulation envelope (Whoosh)
            float filterFactor = Mathf.Sin(progress * Mathf.PI);
            float rawNoise = UnityEngine.Random.Range(-1f, 1f);
            
            float hpNoise = rawNoise - lastFilter;
            lastFilter = rawNoise;
            
            float env = Mathf.Sin(progress * Mathf.PI);
            samples[i] = hpNoise * env * filterFactor * 0.1f;
        }
        return samples;
    }

    private float[] GenerateVictorySamples()
    {
        float duration = 5.0f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        
        // Roland Synth Arpeggio: C4, E4, G4, C5, E5, G5, C6
        float[] chordFreqs = new float[] { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f, 783.99f, 1046.50f };
        float arpeggioInterval = 0.22f;
        
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float compositeSample = 0f;
            
            for (int note = 0; note < chordFreqs.Length; note++)
            {
                float noteStartTime = note * arpeggioInterval;
                if (t >= noteStartTime)
                {
                    float noteAge = t - noteStartTime;
                    
                    // Decaying physical bell pluck harmonics
                    float phase = 2f * Mathf.PI * chordFreqs[note] * noteAge;
                    float pluck = Mathf.Sin(phase) * 0.55f + 
                                  Mathf.Sin(phase * 2f) * 0.25f + 
                                  Mathf.Sin(phase * 3f) * 0.12f +
                                  Mathf.Sin(phase * 4f) * 0.08f;
                                  
                    float pluckEnv = Mathf.Exp(-noteAge * 1.9f); // long resonance decay
                    compositeSample += pluck * pluckEnv * 0.14f;
                }
            }
            
            // Smooth final fade
            float globalFade = 1f;
            if (t > 4.0f) globalFade = 1f - (t - 4.0f) / 1.0f;
            
            samples[i] = Mathf.Clamp(compositeSample * globalFade, -1f, 1f) * 0.32f;
        }
        return samples;
    }

    private float[] GenerateWonderObjectEnterSamples()
    {
        float duration = 0.35f;
        int sampleCount = (int)(SAMPLERATE * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLERATE;
            float progress = t / duration;
            
            float freq = Mathf.Lerp(550f, 1550f, progress);
            float phase = 2f * Mathf.PI * freq * t;
            
            // Rapid celestial sparkling tremolo (16Hz vibrato LFO)
            float vibrato = Mathf.Sin(2f * Mathf.PI * 16f * t) * 0.25f + 0.75f;
            
            float wave = (Mathf.Sin(phase) + Mathf.Sin(phase * 1.5f) * 0.35f) * vibrato;
            float env = Mathf.Exp(-progress * 6.5f) * (1f - progress);
            samples[i] = wave * env * 0.16f;
        }
        return samples;
    }

    // ==========================================
    // AUDIO COMPONENT PLAYBACK HANDLERS
    // ==========================================

    /// <summary>
    /// Plays a non-spatialized, direct 2D Audio Clip (ambient or UI SFX).
    /// </summary>
    public static AudioSource Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || instance == null) return null;
        
        GameObject go = new GameObject("Temp2DSound_" + clip.name);
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0f; // 2D Stereo
        source.Play();
        
        Destroy(go, clip.length + 0.2f);
        return source;
    }

    /// <summary>
    /// Plays an audio clip spatialized at a specific position in the 2D/3D game world.
    /// </summary>
    public static AudioSource PlayAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
    {
        if (clip == null || instance == null) return null;
        
        GameObject go = new GameObject("Temp3DSound_" + clip.name);
        go.transform.position = position;
        
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend; // 1 = fully 3D, 0 = fully 2D
        source.minDistance = 4f;
        source.maxDistance = 25f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.Play();
        
        Destroy(go, clip.length + 0.2f);
        return source;
    }

    /// <summary>
    /// Starts a looping spatialized sound at a world position.
    /// </summary>
    public static AudioSource PlayLoopAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || instance == null) return null;
        
        GameObject go = new GameObject("Looping3DSound_" + clip.name);
        go.transform.position = position;
        
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 1f; // fully 3D
        source.loop = true;
        source.minDistance = 4f;
        source.maxDistance = 22f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.Play();
        
        return source;
    }

    /// <summary>
    /// Fades out a looping audio source smoothly and cleans up its GameObject.
    /// Prevents clicking, popping, or harsh structural cuts.
    /// </summary>
    public static void StopLoop(AudioSource source, float fadeTime = 0.18f)
    {
        if (source != null && instance != null)
        {
            instance.StartCoroutine(instance.FadeOutAndDestroyRoutine(source, fadeTime));
        }
    }

    private IEnumerator FadeOutAndDestroyRoutine(AudioSource source, float fadeTime)
    {
        if (source == null) yield break;
        
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeTime && source != null)
        {
            elapsed += Time.unscaledDeltaTime; // support time dilation safety
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }
        
        if (source != null && source.gameObject != null)
        {
            Destroy(source.gameObject);
        }
    }

    // ==========================================
    // MODULE-SPECIFIC QUICK STATIC APIS
    // ==========================================

    public static void PlayJump()
    {
        if (instance != null) Play(instance.jumpClip, 0.7f, UnityEngine.Random.Range(0.95f, 1.05f));
    }

    public static void PlayLand(float fallVelocity)
    {
        if (instance == null) return;
        
        // Dynamically scale pitch and volume by severity of physical impact
        float volume = Mathf.Clamp(fallVelocity * 0.05f, 0.2f, 1.0f);
        float pitch = Mathf.Clamp(1.05f - fallVelocity * 0.015f, 0.7f, 1.1f); // harder fall is deeper
        
        Play(instance.landClip, volume, pitch);
    }

    public static void PlayFootstep(float runRatio)
    {
        if (instance == null) return;
        
        // Slightly quieter/louder and higher/lower pitch based on current horizontal velocity ratio
        float volume = Mathf.Lerp(0.08f, 0.28f, runRatio);
        float pitch = Mathf.Lerp(0.92f, 1.08f, runRatio);
        
        Play(instance.footstepClip, volume, pitch);
    }

    public static void PlayWonderToggleOn()
    {
        if (instance != null) Play(instance.toggleOnClip, 0.85f);
    }

    public static void PlayWonderToggleOff()
    {
        if (instance != null) Play(instance.toggleOffClip, 0.85f);
    }

    public static void PlayButtonPress(Vector3 pos)
    {
        if (instance != null) PlayAtPoint(instance.buttonPressClip, pos, 0.9f, UnityEngine.Random.Range(0.96f, 1.04f));
    }

    public static void PlayButtonRelease(Vector3 pos)
    {
        if (instance != null) PlayAtPoint(instance.buttonReleaseClip, pos, 0.8f, UnityEngine.Random.Range(0.96f, 1.04f));
    }

    public static void PlayGateLock(Vector3 pos)
    {
        if (instance != null) PlayAtPoint(instance.gateLockClip, pos, 1.0f);
    }

    public static void PlayWonderObjectEnter(Vector3 pos)
    {
        if (instance != null) PlayAtPoint(instance.wonderObjectEnterClip, pos, 0.75f, UnityEngine.Random.Range(0.95f, 1.05f));
    }

    public static void PlayDriftStart(Vector3 pos)
    {
        if (instance != null) PlayAtPoint(instance.driftStartClip, pos, 0.9f);
    }

    public static void PlaySporeWhoosh(Vector3 pos)
    {
        if (instance != null)
        {
            // Set panning: map position relative to camera viewport
            float volume = UnityEngine.Random.Range(0.45f, 0.75f);
            float pitch = UnityEngine.Random.Range(0.85f, 1.15f);
            PlayAtPoint(instance.sporeWhooshClip, pos, volume, pitch);
        }
    }

    public static void PlayVictoryChime()
    {
        if (instance != null) Play(instance.victoryChimeClip, 0.9f);
    }
}
