using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A global self-contained manager that monitors key puzzle objects.
/// If a key falls out of the world bounds (y < -7), it pauses physics,
/// and draws a beautiful obsidian/neon warning banner prompting the player
/// to restart the level with platform-aware key prompts (R on Keyboard, Y on Gamepad).
/// </summary>
public class KeyFallDetectorManager : MonoBehaviour
{
    private List<Rigidbody2D> keyRigidbodies = new List<Rigidbody2D>();
    private Dictionary<Rigidbody2D, SlidingGate> keyToGateMap = new Dictionary<Rigidbody2D, SlidingGate>();
    private bool isKeyLost = false;
    private bool isGamepadMode = false;

    // UI elements
    private GameObject canvasObj;
    private RectTransform panelRect;
    private CanvasGroup panelCanvasGroup;
    private Text promptText;
    private RectTransform promptTextRect;
    private Outline warningOutline;
    private Coroutine activeAnimCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeDetector()
    {
        // Don't spawn if there's already a detector active
        if (FindObjectOfType<KeyFallDetectorManager>() != null) return;

        // Don't spawn if the Player is not present (means we are not in a gameplay level)
        if (GameObject.FindGameObjectWithTag("Player") == null) return;

        GameObject go = new GameObject("KeyFallDetectorManager");
        go.AddComponent<KeyFallDetectorManager>();
    }

    private void Start()
    {
        FindKeyObjects();
        // Match the initial gamepad state with the TutorialPromptCard if available
        isGamepadMode = TutorialPromptCard.IsGamepadMode;

        AssociateKeysWithGates();
    }

    private void AssociateKeysWithGates()
    {
        keyToGateMap.Clear();
        var buttons = FindObjectsOfType<CeilingButton>();
        if (buttons.Length == 0) return;

        foreach (var rb in keyRigidbodies)
        {
            if (rb == null) continue;

            // Find closest button on the X-axis
            CeilingButton closestButton = null;
            float minDistance = float.MaxValue;
            foreach (var btn in buttons)
            {
                float dist = Mathf.Abs(rb.transform.position.x - btn.transform.position.x);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestButton = btn;
                }
            }

            if (closestButton != null)
            {
                // Find associated sliding gate via button onPressed event target
                SlidingGate gate = null;
                var onPressed = closestButton.OnPressed;
                int count = onPressed.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    var target = onPressed.GetPersistentTarget(i);
                    if (target != null)
                    {
                        if (target is GameObject go)
                        {
                            gate = go.GetComponent<SlidingGate>();
                        }
                        else if (target is Component comp)
                        {
                            gate = comp.GetComponent<SlidingGate>();
                        }
                    }
                    if (gate != null) break;
                }

                if (gate != null)
                {
                    keyToGateMap[rb] = gate;
                }
            }
        }
    }

    private void FindKeyObjects()
    {
        keyRigidbodies.Clear();
        foreach (var rb in FindObjectsOfType<Rigidbody2D>())
        {
            if (rb.gameObject.name.Contains("Key") || rb.gameObject.name.Contains("Hoverboard"))
            {
                keyRigidbodies.Add(rb);
            }
        }
    }

    private void Update()
    {
        // Continuously update gamepad vs. KBM state
        DetectInputMode();

        // 1. Manage key physics (whether lost or not, so they can float up if inside Wonder Zone)
        bool anyKeyBelowLimit = false;
        for (int i = keyRigidbodies.Count - 1; i >= 0; i--)
        {
            var rb = keyRigidbodies[i];
            if (rb == null)
            {
                keyRigidbodies.RemoveAt(i);
                continue;
            }

            if (rb.transform.position.y < -7f)
            {
                // If the gate associated with this key is already open, we don't trigger "anyKeyBelowLimit"
                bool isKeyDone = keyToGateMap.TryGetValue(rb, out var gate) && gate != null && gate.IsOpen;
                if (!isKeyDone)
                {
                    anyKeyBelowLimit = true;
                }

                // Check if the key is inside the active Wonder Zone
                bool inWonderZone = WonderRadiusController.IsInsideWonderZone(rb.transform.position);

                if (inWonderZone)
                {
                    // Unfreeze it so it can float back up!
                    if (rb.bodyType == RigidbodyType2D.Kinematic)
                    {
                        rb.bodyType = RigidbodyType2D.Dynamic;
                    }
                }
                else
                {
                    // Freeze it so it doesn't drop forever in mundane world
                    if (rb.bodyType == RigidbodyType2D.Dynamic)
                    {
                        rb.velocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Kinematic;
                    }
                }
            }
            else
            {
                // Key is above -7f. Restore dynamic if it was frozen kinematic
                if (rb.bodyType == RigidbodyType2D.Kinematic)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
            }
        }

        // 2. Handle state transitions
        if (anyKeyBelowLimit)
        {
            if (!isKeyLost)
            {
                TriggerKeyLost();
            }
        }
        else
        {
            if (isKeyLost)
            {
                DismissKeyLost();
            }
        }

        // 3. Listen for restart inputs and animate if lost
        if (isKeyLost)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton3))
            {
                Time.timeScale = 1.0f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            AnimateWarningUI();
        }
    }

    private void TriggerKeyLost()
    {
        isKeyLost = true;

        // Play heavy land / impact sound to signify the loss
        AudioManager.PlayLand(18f);

        // Shake the camera slightly to call attention
        if (CameraController2D.Instance != null)
        {
            CameraController2D.Instance.TriggerShake(0.2f, 0.15f);
        }

        // Spawn warning UI or restore if existing
        if (canvasObj == null)
        {
            CreateWarningUI();
        }
        else
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(AnimatePanelIn());
        }
    }

    private void DismissKeyLost()
    {
        isKeyLost = false;

        if (canvasObj != null)
        {
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(AnimatePanelOut());
        }
    }

    private void CreateWarningUI()
    {
        canvasObj = new GameObject("KeyLostCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Keep on top of all other elements

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create sleek modern glass panel
        GameObject panelObj = new GameObject("WarningPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        var panelImage = panelObj.AddComponent<Image>();
        // Very translucent dark smoked glass
        panelImage.color = new Color(0.06f, 0.07f, 0.12f, 0.45f);

        panelCanvasGroup = panelObj.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;

        panelRect = panelObj.GetComponent<RectTransform>();
        // Anchor to Top Center
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        
        // Sleek wide and short banner
        panelRect.sizeDelta = new Vector2(500f, 48f);
        panelRect.anchoredPosition = new Vector2(0f, 60f); // Start off-screen

        // Glassmorphism edge glow highlight (very thin, semi-transparent white)
        warningOutline = panelObj.AddComponent<Outline>();
        warningOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);
        warningOutline.effectDistance = new Vector2(1f, -1f);

        // Soft drop shadow for panel depth
        var panelShadow = panelObj.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        panelShadow.effectDistance = new Vector2(3f, -3f);

        // Subtitle: PRESS [R] TO RESTART
        GameObject subObj = new GameObject("PromptText");
        subObj.transform.SetParent(panelObj.transform, false);

        promptText = subObj.AddComponent<Text>();
        promptText.font = GetSafeBuiltinFont();
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.fontSize = 17;
        promptText.color = new Color(0.95f, 0.98f, 1f, 0.95f); // Crisp white/blue

        // Subtle shadow behind the text for readability
        var textShadow = subObj.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        textShadow.effectDistance = new Vector2(1.5f, -1.5f);

        promptTextRect = subObj.GetComponent<RectTransform>();
        promptTextRect.anchorMin = Vector2.zero;
        promptTextRect.anchorMax = Vector2.one;
        promptTextRect.offsetMin = Vector2.zero;
        promptTextRect.offsetMax = Vector2.zero;

        UpdatePromptText();

        if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
        activeAnimCoroutine = StartCoroutine(AnimatePanelIn());
    }

    private IEnumerator AnimatePanelIn()
    {
        float duration = 0.8f; // Smooth and calm
        float elapsed = 0f;
        Vector2 startPos = new Vector2(0f, 60f); // Off-screen above
        Vector2 endPos = new Vector2(0f, -30f); // Sleek margin from top edge

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth ease out
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            panelCanvasGroup.alpha = Mathf.Lerp(panelCanvasGroup.alpha, 1f, smoothT);
            
            yield return null;
        }
        
        panelRect.anchoredPosition = endPos;
        panelCanvasGroup.alpha = 1f;
        activeAnimCoroutine = null;
    }

    private IEnumerator AnimatePanelOut()
    {
        float duration = 0.8f;
        float elapsed = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 endPos = new Vector2(0f, 60f); // Slide back up off-screen

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth ease in
            float smoothT = t * t * (3f - 2f * t);

            panelRect.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            panelCanvasGroup.alpha = Mathf.Lerp(panelCanvasGroup.alpha, 0f, smoothT);
            
            yield return null;
        }

        Destroy(canvasObj);
        canvasObj = null;
        panelRect = null;
        panelCanvasGroup = null;
        activeAnimCoroutine = null;
    }

    private void UpdatePromptText()
    {
        if (promptText == null) return;

        if (isGamepadMode)
        {
            promptText.text = "PRESS   [ Y ]   TO   RESTART   LEVEL";
        }
        else
        {
            promptText.text = "PRESS   [ R ]   TO   RESTART   LEVEL";
        }
    }

    private void AnimateWarningUI()
    {
        // 1. Gentle pulse on the refraction border outline for a dynamic light effect
        if (warningOutline != null)
        {
            float pulse = 0.2f + Mathf.PingPong(Time.unscaledTime * 1.5f, 0.2f);
            warningOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, pulse);
        }

        // 2. Pulse subtitle prompt scale very gently
        if (promptTextRect != null)
        {
            float scalePulse = 0.98f + Mathf.PingPong(Time.unscaledTime * 1.2f, 0.04f);
            promptTextRect.localScale = new Vector3(scalePulse, scalePulse, 1f);
        }
    }

    private void DetectInputMode()
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

        // Detect Gamepad analog stick activity
        float rx = 0f, ry = 0f, lt = 0f, rt = 0f;
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

        // Detect active mouse movement
        if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.05f || 
            Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.05f || 
            Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
        {
            SetGamepadMode(false);
        }
    }

    private void SetGamepadMode(bool gamepad)
    {
        if (isGamepadMode == gamepad) return;
        isGamepadMode = gamepad;
        UpdatePromptText();
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
