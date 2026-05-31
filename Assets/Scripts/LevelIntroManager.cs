using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// A state-of-the-art, fully self-contained Level Intro Manager.
/// Automatically instantiates itself upon level load, overlays gorgeous obsidian letterbox bars,
/// displays a spaced-out space-neon title and subtitle, plays spatial procedural chime sweeps,
/// and temporarily disables player controls for an incredibly cinematic widescreen intro!
/// </summary>
public class LevelIntroManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float waitBeforeFade = 0.5f;
    [SerializeField] private float textFadeDuration = 1.0f;
    [SerializeField] private float barSlideDuration = 0.7f;
    [SerializeField] private float showDuration = 1.8f;

    [Header("Colors")]
    [SerializeField] private Color letterboxColor = new Color(0.04f, 0.03f, 0.08f, 0.88f); // Obsidian purple-black
    [SerializeField] private Color titleColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);       // Cyber neon cyan
    [SerializeField] private Color subtitleColor = new Color(1.0f, 0.08f, 0.65f, 1.0f);   // Neon magenta

    private Canvas canvas;
    private RectTransform topBar;
    private RectTransform bottomBar;
    private Text titleText;
    private Text subtitleText;
    private PlayerController2D playerController;

    /// <summary>
    /// Programmatically auto-instantiates and triggers the level intro at the start of every loaded level!
    /// Eliminates manual editor setup or dragging components into scenes.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStartIntro()
    {
        // Don't spawn if the AudioManager or Player is not present (e.g. core setups aren't loaded)
        if (GameObject.FindGameObjectWithTag("Player") == null) return;

        GameObject go = new GameObject("LevelIntroManager");
        go.AddComponent<LevelIntroManager>();
    }

    private void Start()
    {
        // Find Player and temporarily disable controls
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController2D>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }

        // Setup the UI canvas and cinematic elements programmatically
        SetupIntroCanvas();

        // Start the cinematic intro sequence
        StartCoroutine(IntroSequence());
    }

    private void SetupIntroCanvas()
    {
        GameObject canvasObj = new GameObject("LevelIntroCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998; // Just below the Victory Cinematic sorting order (999)

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        float barHeight = Screen.height * 0.14f; // 14% widescreen bar height

        // Top letterbox black bar
        GameObject topBarObj = new GameObject("TopLetterbox");
        topBarObj.transform.SetParent(canvasObj.transform, false);
        var topImage = topBarObj.AddComponent<Image>();
        topImage.color = letterboxColor;
        topBar = topBarObj.GetComponent<RectTransform>();
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, barHeight);
        // Start closed (out of screen)
        topBar.anchoredPosition = new Vector2(0f, barHeight);

        // Bottom letterbox black bar
        GameObject bottomBarObj = new GameObject("BottomLetterbox");
        bottomBarObj.transform.SetParent(canvasObj.transform, false);
        var bottomImage = bottomBarObj.AddComponent<Image>();
        bottomImage.color = letterboxColor;
        bottomBar = bottomBarObj.GetComponent<RectTransform>();
        bottomBar.anchorMin = new Vector2(0f, 0f);
        bottomBar.anchorMax = new Vector2(1f, 0f);
        bottomBar.pivot = new Vector2(0.5f, 0f);
        bottomBar.sizeDelta = new Vector2(0f, barHeight);
        // Start closed (out of screen)
        bottomBar.anchoredPosition = new Vector2(0f, -barHeight);

        // Text Group Container
        GameObject textGroup = new GameObject("TextGroup");
        textGroup.transform.SetParent(canvasObj.transform, false);
        var textGroupRect = textGroup.AddComponent<RectTransform>();
        textGroupRect.anchorMin = new Vector2(0.1f, 0.35f);
        textGroupRect.anchorMax = new Vector2(0.9f, 0.65f);
        textGroupRect.offsetMin = Vector2.zero;
        textGroupRect.offsetMax = Vector2.zero;

        // Title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(textGroup.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.font = GetSafeBuiltinFont();
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 32;
        titleText.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0f); // invisible on start
        
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Subtitle text
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(textGroup.transform, false);
        subtitleText = subObj.AddComponent<Text>();
        subtitleText.font = GetSafeBuiltinFont();
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.fontSize = 18;
        subtitleText.fontStyle = FontStyle.Italic;
        subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, 0f); // invisible on start

        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.45f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;

        // Determine scene and display texts
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "SampleScene")
        {
            titleText.text = "L E V E L   O N E";
            subtitleText.text = "T h e   P o r t a l   A s c e n t";
        }
        else if (sceneName == "Level2")
        {
            titleText.text = "L E V E L   T W O";
            subtitleText.text = "T h e   U p d r a f t   O d y s s e y";
        }
        else
        {
            titleText.text = sceneName.ToUpper();
            subtitleText.text = "E n t e r   T h e   R e a l m   O f   A w e";
        }
    }

    private IEnumerator IntroSequence()
    {
        float elapsed = 0f;
        float barHeight = Screen.height * 0.14f;

        // --- STEP 1: SLIDE IN LETTERBOX BARS ---
        // Play spatial toggle sound as the bars sweep in
        AudioManager.PlayWonderToggleOn();
        
        while (elapsed < barSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / barSlideDuration;
            t = t * t * (3f - 2f * t); // smoothstep

            topBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(barHeight, 0f, t));
            bottomBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(-barHeight, 0f, t));
            yield return null;
        }
        topBar.anchoredPosition = new Vector2(0f, 0f);
        bottomBar.anchoredPosition = new Vector2(0f, 0f);

        yield return new WaitForSeconds(waitBeforeFade);

        // --- STEP 2: FADE IN TEXTS ---
        elapsed = 0f;
        while (elapsed < textFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / textFadeDuration;
            titleText.color = new Color(titleColor.r, titleColor.g, titleColor.b, t);
            subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, t);
            yield return null;
        }
        titleText.color = titleColor;
        subtitleText.color = subtitleColor;

        // Play the glorious major victory arpeggio chime as the title becomes full bright
        AudioManager.PlayVictoryChime();

        // Hold display
        yield return new WaitForSeconds(showDuration);

        // --- STEP 3: FADE OUT TEXTS ---
        elapsed = 0f;
        while (elapsed < textFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / textFadeDuration;
            titleText.color = new Color(titleColor.r, titleColor.g, titleColor.b, 1f - t);
            subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, 1f - t);
            yield return null;
        }
        titleText.color = Color.clear;
        subtitleText.color = Color.clear;

        // --- STEP 4: SLIDE OUT LETTERBOX BARS ---
        elapsed = 0f;
        while (elapsed < barSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / barSlideDuration;
            t = t * t * (3f - 2f * t);

            topBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, barHeight, t));
            bottomBar.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -barHeight, t));
            yield return null;
        }
        topBar.anchoredPosition = new Vector2(0f, barHeight);
        bottomBar.anchoredPosition = new Vector2(0f, -barHeight);

        // --- STEP 5: RESTORE PLAYER CONTROLS ---
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Clean up intro canvas
        Destroy(canvas.gameObject);
        Destroy(this.gameObject);
    }

    private Font GetSafeBuiltinFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch {}

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch {}
        }

        return font;
    }
}
