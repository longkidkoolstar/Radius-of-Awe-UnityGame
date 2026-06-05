using UnityEngine;

/// <summary>
/// Controls dynamic background music crossfading between the Mundane and Wonderous layers.
/// Automatically keeps tracks synchronized in time to prevent drift.
/// </summary>
public class DynamicMusicPlayer : MonoBehaviour
{
    [Header("Audio Tracks")]
    [SerializeField] private AudioClip mundaneTrack;
    [SerializeField] private AudioClip wonderousTrack;

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float maxMundaneVolume = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float maxWonderousVolume = 0.25f;
    [Tooltip("How fast the music crossfades between worlds.")]
    [SerializeField] private float fadeSpeed = 1.8f;

    private AudioSource mundaneSource;
    private AudioSource wonderousSource;

    private static DynamicMusicPlayer instance;

    private void Awake()
    {
        if (instance != null)
        {
            // If the clips match, reload of same level occurred. Keep old playing, destroy duplicate.
            if (instance.mundaneTrack == this.mundaneTrack && instance.wonderousTrack == this.wonderousTrack)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                // Different level, destroy old player so the new one can take over.
                Destroy(instance.gameObject);
            }
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 1. Set up Mundane Audio Source
        mundaneSource = gameObject.AddComponent<AudioSource>();
        mundaneSource.clip = mundaneTrack;
        mundaneSource.loop = true;
        mundaneSource.volume = maxMundaneVolume;
        mundaneSource.spatialBlend = 0f; // 2D Stereo
        mundaneSource.playOnAwake = false;

        // 2. Set up Wonderous Audio Source
        wonderousSource = gameObject.AddComponent<AudioSource>();
        wonderousSource.clip = wonderousTrack;
        wonderousSource.loop = true;
        wonderousSource.volume = 0f;
        wonderousSource.spatialBlend = 0f; // 2D Stereo
        wonderousSource.playOnAwake = false;

        // 3. Play both simultaneously
        if (mundaneTrack != null) mundaneSource.Play();
        if (wonderousTrack != null) wonderousSource.Play();

        // 4. Align start times
        if (mundaneSource.isPlaying) mundaneSource.time = 0f;
        if (wonderousSource.isPlaying) wonderousSource.time = 0f;
    }

    private void Update()
    {
        bool isWonderActive = false;
        if (WonderRadiusController.Instance != null)
        {
            isWonderActive = WonderRadiusController.Instance.IsActive;
        }

        // Determine targets
        float targetMundaneVolume = isWonderActive ? 0f : maxMundaneVolume;
        float targetWonderousVolume = isWonderActive ? maxWonderousVolume : 0f;

        // Sync playback time of the silent track to the active track to prevent drift
        if (mundaneSource != null && wonderousSource != null && 
            mundaneSource.isPlaying && wonderousSource.isPlaying)
        {
            if (isWonderActive)
            {
                // If mundane is silent, sync it to wonderous
                if (mundaneSource.volume < 0.02f && Mathf.Abs(mundaneSource.time - wonderousSource.time) > 0.05f)
                {
                    mundaneSource.time = wonderousSource.time;
                }
            }
            else
            {
                // If wonderous is silent, sync it to mundane
                if (wonderousSource.volume < 0.02f && Mathf.Abs(wonderousSource.time - mundaneSource.time) > 0.05f)
                {
                    wonderousSource.time = mundaneSource.time;
                }
            }
        }

        // Smoothly fade volumes
        if (mundaneSource != null)
        {
            mundaneSource.volume = Mathf.MoveTowards(mundaneSource.volume, targetMundaneVolume, Time.deltaTime * fadeSpeed);
        }
        if (wonderousSource != null)
        {
            wonderousSource.volume = Mathf.MoveTowards(wonderousSource.volume, targetWonderousVolume, Time.deltaTime * fadeSpeed);
        }
    }
}
