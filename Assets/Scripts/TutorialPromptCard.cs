using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A code-driven procedural World-Space Tutorial Prompt Card.
/// Programmatically generates a semi-transparent obsidian card, neon trim outlines,
/// and glowing vector typographical instructions, placing them directly in the 2D world.
/// Automatically detects gamepad vs. keyboard and mouse input to show relevant controls!
/// </summary>
public class TutorialPromptCard : MonoBehaviour
{
    [Header("Prompt Details (Keyboard & Mouse)")]
    [TextArea(2, 5)]
    [SerializeField] private string instructionText = "A / D   TO   MOVE\nSPACE   TO   JUMP";

    [Header("Prompt Details (Gamepad / Controller)")]
    [TextArea(2, 5)]
    [SerializeField] private string instructionTextGamepad = "";

    [SerializeField] private Vector2 cardSize = new Vector2(3.5f, 1.2f);

    [Header("Aesthetics")]
    [SerializeField] private Color cardColor = new Color(0.04f, 0.03f, 0.08f, 0.75f); // Obsidian
    [SerializeField] private Color textColor = new Color(0.0f, 0.9f, 1.0f, 1.0f);      // Neon Cyan
    [SerializeField] private int fontSize = 16;

    private Canvas canvas;
    private Image cardBg;
    private Text textComp;

    // Static input states shared across all tutorial prompts
    private static List<TutorialPromptCard> activeCards = new List<TutorialPromptCard>();
    public static bool IsGamepadMode { get; private set; } = false;

    private void OnEnable()
    {
        activeCards.Add(this);
    }

    private void OnDisable()
    {
        activeCards.Remove(this);
    }

    private void Start()
    {
        // 1. Automatically populate Gamepad instructions if they are empty
        if (string.IsNullOrEmpty(instructionTextGamepad))
        {
            string lowerText = instructionText.ToLower();
            if (lowerText.Contains("a / d") || lowerText.Contains("space") || gameObject.name.Contains("Movement"))
            {
                instructionTextGamepad = "L-STICK   TO   MOVE\n(A)   TO   JUMP";
            }
            else if (lowerText.Contains("e ") || lowerText.Contains("mouse") || lowerText.Contains("aim") || gameObject.name.Contains("Wonder"))
            {
                instructionTextGamepad = "X   TO   PROJECT   WONDER\nR-STICK   TO   AIM   RADIUS";
            }
            else if (lowerText.Contains("scroll") || lowerText.Contains("resize") || lowerText.Contains("lift") || gameObject.name.Contains("Puzzle"))
            {
                instructionTextGamepad = "LT / RT   TO   RESIZE   RADIUS\nLIFT   CRATE   TO   CEILING";
            }
            else
            {
                // Fallback smart parser replacement
                instructionTextGamepad = instructionText
                    .Replace("A / D", "L-STICK")
                    .Replace("a / d", "l-stick")
                    .Replace("SPACE", "(A)")
                    .Replace("Space", "(A)")
                    .Replace("space", "(a)")
                    .Replace("E KEY", "X")
                    .Replace("E key", "X")
                    .Replace("E", "X")
                    .Replace("e", "x")
                    .Replace("MOUSE", "R-STICK")
                    .Replace("Mouse", "R-STICK")
                    .Replace("mouse", "r-stick")
                    .Replace("SCROLL", "LT / RT")
                    .Replace("Scroll", "LT / RT")
                    .Replace("scroll", "lt / rt");
            }
        }

        // 2. Create World Space Canvas
        GameObject canvasObj = new GameObject("TutorialCanvas_" + gameObject.name);
        canvasObj.transform.SetParent(this.transform, false);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 1f); // Scale down for world space

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5; // In front of standard blocks but behind characters/visuals

        canvasObj.AddComponent<CanvasScaler>();

        // 3. Create Card Background Image (fits cardSize converted to pixels, e.g. 100 pixels per unit)
        GameObject bgObj = new GameObject("CardBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        
        cardBg = bgObj.AddComponent<Image>();
        cardBg.color = cardColor;

        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = cardSize * 100f; // Scale units to pixels
        bgRect.anchoredPosition = Vector2.zero;

        // 4. Generate soft border trim by adding a procedural Outline component
        var outline = bgObj.AddComponent<Outline>();
        outline.effectColor = new Color(textColor.r, textColor.g, textColor.b, 0.35f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);

        // 5. Create Text Component
        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(bgObj.transform, false);

        textComp = textObj.AddComponent<Text>();
        textComp.font = GetSafeBuiltinFont();
        textComp.text = IsGamepadMode ? instructionTextGamepad : instructionText;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.fontSize = fontSize;
        textComp.color = textColor;

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 8f);
        textRect.offsetMax = new Vector2(-8f, -8f);
    }

    private void Update()
    {
        // Detect Gamepad buttons
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i)))
            {
                SetGamepadMode(true);
                return;
            }
        }

        // Detect Gamepad analog activity
        float rx = 0f;
        float ry = 0f;
        float lt = 0f;
        float rt = 0f;
        try
        {
            rx = Input.GetAxis("RightStickX");
            ry = Input.GetAxis("RightStickY");
            lt = Input.GetAxis("LeftTrigger");
            rt = Input.GetAxis("RightTrigger");
        }
        catch {}

        if (Mathf.Abs(rx) > 0.15f || Mathf.Abs(ry) > 0.15f || Mathf.Abs(lt) > 0.15f || Mathf.Abs(rt) > 0.15f)
        {
            SetGamepadMode(true);
            return;
        }

        // Detect Keyboard & Mouse activity
        if (Input.anyKeyDown)
        {
            bool joystickPressed = false;
            for (int i = 0; i < 20; i++)
            {
                if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    joystickPressed = true;
                    break;
                }
            }
            if (!joystickPressed)
            {
                SetGamepadMode(false);
            }
        }

        // Detect active mouse or scroll wheel movement
        if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.05f || 
            Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.05f || 
            Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
        {
            SetGamepadMode(false);
        }
    }

    private static void SetGamepadMode(bool gamepad)
    {
        if (IsGamepadMode == gamepad) return;
        IsGamepadMode = gamepad;
        foreach (var card in activeCards)
        {
            card.RefreshText();
        }
    }

    private void RefreshText()
    {
        if (textComp != null)
        {
            textComp.text = IsGamepadMode ? instructionTextGamepad : instructionText;
        }
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
