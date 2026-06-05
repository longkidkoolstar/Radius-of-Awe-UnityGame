using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

/// <summary>
/// A zero-asset Procedural Main Menu Manager.
/// Creates a stunning cyber-neon UI, procedural button graphics, and background effects entirely from code.
/// Now features a dynamic Level Select panel!
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color bgColor = new Color(0.04f, 0.03f, 0.08f, 1f);
    [SerializeField] private Color titleColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);
    [SerializeField] private Color subtitleColor = new Color(1.0f, 0.08f, 0.65f, 1.0f);
    [SerializeField] private Color buttonNormalColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
    [SerializeField] private Color buttonHoverColor = new Color(0.2f, 0.2f, 0.3f, 1.0f);

    private Canvas canvas;
    private GameObject mainMenuContainer;
    private GameObject levelSelectContainer;
    private bool isTransitioning = false;

    private void Start()
    {
        Camera.main.backgroundColor = bgColor;

        CreateEventSystem();
        CreateCanvas();
        CreateProceduralBackground();
        
        mainMenuContainer = new GameObject("MainMenuContainer");
        mainMenuContainer.transform.SetParent(canvas.transform, false);
        
        levelSelectContainer = new GameObject("LevelSelectContainer");
        levelSelectContainer.transform.SetParent(canvas.transform, false);
        
        CreateMainMenuElements();
        CreateLevelSelectElements();
        
        levelSelectContainer.SetActive(false); // Hide level select initially
    }

    private void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateProceduralBackground()
    {
        int orbCount = 15;
        for (int i = 0; i < orbCount; i++)
        {
            GameObject orbObj = new GameObject("MenuOrb_" + i);
            SpriteRenderer sr = orbObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateGlowSprite(64, 64);
            sr.color = new Color(titleColor.r, titleColor.g, titleColor.b, Random.Range(0.05f, 0.15f));
            
            float x = Random.Range(-10f, 10f);
            float y = Random.Range(-6f, 6f);
            orbObj.transform.position = new Vector3(x, y, 10f);
            float scale = Random.Range(0.5f, 3.0f);
            orbObj.transform.localScale = new Vector3(scale, scale, 1f);

            MenuOrbDrift drift = orbObj.AddComponent<MenuOrbDrift>();
            drift.speed = Random.Range(0.1f, 0.5f);
            drift.direction = Random.insideUnitCircle.normalized;
        }
    }

    private void CreateMainMenuElements()
    {
        Font font = GetSafeBuiltinFont();

        // Title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(mainMenuContainer.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "RADIUS OF AWE";
        titleText.fontSize = 85;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = titleColor;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchoredPosition = new Vector2(0, 300);

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(mainMenuContainer.transform, false);
        Text subText = subObj.AddComponent<Text>();
        subText.font = font;
        subText.text = "A DIMENSIONAL JOURNEY";
        subText.fontSize = 32;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = subtitleColor;
        subText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subText.verticalOverflow = VerticalWrapMode.Overflow;
        subText.text = string.Join("  ", subText.text.ToCharArray());
        RectTransform subRect = subText.rectTransform;
        subRect.anchoredPosition = new Vector2(0, 180);

        // Play Button (Navigates to Level Select)
        CreateButton(mainMenuContainer.transform, "PlayButton", "PLAY", new Vector2(0, -50), new Vector2(250, 60), () => {
            if (!isTransitioning) 
            {
                if (AudioManager.Instance != null) AudioManager.PlayButtonPress(Vector3.zero);
                StartCoroutine(FadeBetweenContainers(mainMenuContainer, levelSelectContainer));
            }
        });

        // Quit Button
        CreateButton(mainMenuContainer.transform, "QuitButton", "QUIT", new Vector2(0, -150), new Vector2(250, 60), () => {
            if (!isTransitioning) 
            {
                if (AudioManager.Instance != null) AudioManager.PlayButtonPress(Vector3.zero);
                Application.Quit();
            }
        });
    }

    private void CreateLevelSelectElements()
    {
        Font font = GetSafeBuiltinFont();

        // Level Select Title
        GameObject titleObj = new GameObject("LevelSelectTitle");
        titleObj.transform.SetParent(levelSelectContainer.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "SELECT LEVEL";
        titleText.fontSize = 55;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = titleColor;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;
        titleText.text = string.Join(" ", titleText.text.ToCharArray());
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchoredPosition = new Vector2(0, 350);

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        
        // Dynamic Grid layout for levels
        float startX = -250f;
        float startY = 150f;
        float spacingX = 500f;
        float spacingY = -120f;
        
        int colCount = 2;
        int currentLevelIndex = 0;

        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevelIndex", 1);

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            
            // Skip the MainMenu scene itself
            if (sceneName == "MainMenu") continue;

            // Format Level Names
            string displayName = sceneName;
            if (displayName == "Level00") displayName = "TUTORIAL";
            else if (displayName.StartsWith("Level0")) displayName = "LEVEL " + displayName.Substring(6);
            else if (displayName.StartsWith("Level")) displayName = "LEVEL " + displayName.Substring(5);

            int captureIndex = i; // capture for closure
            
            int row = currentLevelIndex / colCount;
            int col = currentLevelIndex % colCount;
            Vector2 pos = new Vector2(startX + (col * spacingX), startY + (row * spacingY));
            
            bool isUnlocked = i <= unlockedLevel;
            string buttonLabel = isUnlocked ? displayName.ToUpper() : displayName.ToUpper() + " [LOCKED]";

            CreateButton(levelSelectContainer.transform, "LevelBtn_" + i, buttonLabel, pos, new Vector2(400, 60), () => {
                if (!isTransitioning && isUnlocked) StartCoroutine(TransitionToGame(captureIndex));
            }, isUnlocked);

            currentLevelIndex++;
        }

        // Back Button
        CreateButton(levelSelectContainer.transform, "BackButton", "BACK", new Vector2(0, -380), new Vector2(250, 60), () => {
            if (!isTransitioning) 
            {
                if (AudioManager.Instance != null) AudioManager.PlayButtonPress(Vector3.zero);
                StartCoroutine(FadeBetweenContainers(levelSelectContainer, mainMenuContainer));
            }
        }, true);
    }

    private void CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, bool isInteractable = true)
    {
        Font font = GetSafeBuiltinFont();
        Sprite btnSprite = CreateSolidSprite(2, 2, Color.white);

        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.sprite = btnSprite;
        img.color = buttonNormalColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        ColorBlock colors = btn.colors;
        colors.normalColor = buttonNormalColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = titleColor * 0.8f;
        colors.selectedColor = buttonNormalColor;
        colors.disabledColor = buttonNormalColor * 0.5f;
        btn.colors = colors;
        
        btn.interactable = isInteractable;
        btn.onClick.AddListener(onClick);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        // Button Text
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.font = font;
        txt.text = label;
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = isInteractable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.6f);
        
        txt.text = string.Join(" ", txt.text.ToCharArray());

        RectTransform txtRect = txt.rectTransform;
        txtRect.sizeDelta = size;
        txtRect.anchoredPosition = Vector2.zero;

        // Add Audio Triggers
        if (isInteractable)
        {
            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { if (AudioManager.Instance != null) AudioManager.PlayWonderToggleOn(); });
            trigger.triggers.Add(enterEntry);

            btn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.PlayButtonPress(Vector3.zero);
            });
        }
        else
        {
            btn.onClick.AddListener(() => {
                // Play a locked sound?
                if (AudioManager.Instance != null) AudioManager.PlayGateLock(Vector3.zero);
            });
        }
    }

    private IEnumerator FadeBetweenContainers(GameObject fromContainer, GameObject toContainer)
    {
        isTransitioning = true;
        CanvasGroup fromGroup = GetOrAddComponent<CanvasGroup>(fromContainer);
        CanvasGroup toGroup = GetOrAddComponent<CanvasGroup>(toContainer);

        toContainer.SetActive(true);
        toGroup.alpha = 0f;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fromGroup.alpha = 1f - t;
            toGroup.alpha = t;
            yield return null;
        }

        fromContainer.SetActive(false);
        fromGroup.alpha = 1f;
        toGroup.alpha = 1f;
        isTransitioning = false;
    }

    private IEnumerator TransitionToGame(int buildIndex)
    {
        isTransitioning = true;
        
        if (AudioManager.Instance != null) AudioManager.PlayDriftStart(Vector3.zero);

        // Flash screen white
        GameObject flashObj = new GameObject("FlashOverlay");
        flashObj.transform.SetParent(canvas.transform, false);
        Image flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(1, 1, 1, 0);
        RectTransform flashRect = flashImg.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.sizeDelta = Vector2.zero;

        float duration = 1.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / (duration * 0.8f));
            flashImg.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        SceneManager.LoadScene(buildIndex);
    }

    private T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null) comp = obj.AddComponent<T>();
        return comp;
    }

    private Font GetSafeBuiltinFont()
    {
        Font font = null;
        try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return font;
    }

    private Sprite CreateGlowSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height);
        Vector2 center = new Vector2(width / 2f, height / 2f);
        float maxDist = width / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / maxDist));
                alpha = alpha * alpha; 
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateSolidSprite(int width, int height, Color color)
    {
        Texture2D tex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}

// Simple logic for the drifting background orbs
public class MenuOrbDrift : MonoBehaviour
{
    public float speed;
    public Vector2 direction;

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Wrap around screen
        if (transform.position.x > 12f) transform.position = new Vector3(-12f, transform.position.y, transform.position.z);
        if (transform.position.x < -12f) transform.position = new Vector3(12f, transform.position.y, transform.position.z);
        if (transform.position.y > 8f) transform.position = new Vector3(transform.position.x, -8f, transform.position.z);
        if (transform.position.y < -8f) transform.position = new Vector3(transform.position.x, 8f, transform.position.z);
    }
}
