using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class EscapeMenu : MonoBehaviour
{
    public static bool IsMenuOpen { get; private set; }

    private static EscapeMenu instance;

    private GameObject menuRoot;
    private bool isMenuOpen;
    private bool useTimeScalePause;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        useTimeScalePause = ShouldUseTimeScalePause();
        BuildMenuUI();
        SetMenuOpen(false);
    }

    private static bool ShouldUseTimeScalePause()
    {
#if ENABLE_INPUT_SYSTEM
        return InputSystem.settings.updateMode != InputSettings.UpdateMode.ProcessEventsInFixedUpdate;
#else
        return true;
#endif
    }

    private void Update()
    {
        if (WasEscapePressedThisFrame())
        {
            SetMenuOpen(!isMenuOpen);
        }
    }

    private static bool WasEscapePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void BuildMenuUI()
    {
        var canvasObject = new GameObject("EscapeMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        menuRoot = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
        menuRoot.transform.SetParent(canvasObject.transform, false);

        var panelRect = menuRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(320f, 180f);

        var panelImage = menuRoot.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f);

        var restartButtonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        restartButtonObject.transform.SetParent(menuRoot.transform, false);

        var buttonRect = restartButtonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(180f, 52f);

        var buttonImage = restartButtonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var button = restartButtonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(RestartCurrentScene);

        var buttonTextObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        buttonTextObject.transform.SetParent(restartButtonObject.transform, false);

        var textRect = buttonTextObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var buttonText = buttonTextObject.GetComponent<Text>();
        buttonText.text = "Restart";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        buttonText.fontSize = 24;

        EnsureEventSystemExists();
    }

    private static void EnsureEventSystemExists()
    {
        var existingEventSystem = FindFirstObjectByType<EventSystem>();
        if (existingEventSystem == null)
        {
#if ENABLE_INPUT_SYSTEM
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
            return;
        }

#if ENABLE_INPUT_SYSTEM
        var legacyModule = existingEventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
        {
            Destroy(legacyModule);
        }

        if (existingEventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#endif
    }

    private void SetMenuOpen(bool open)
    {
        isMenuOpen = open;
        IsMenuOpen = open;

        if (menuRoot != null)
        {
            menuRoot.SetActive(open);
        }

        if (useTimeScalePause)
        {
            if (open)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            }
        }

        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void RestartCurrentScene()
    {
        SetMenuOpen(false);

        if (useTimeScalePause)
        {
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        IsMenuOpen = false;

        if (isMenuOpen)
        {
            if (useTimeScalePause)
            {
                Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
