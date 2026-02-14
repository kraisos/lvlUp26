using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PickaxeTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform visual;

    [Header("Mining")]
    [SerializeField] private float maxMineDistance = 6f;
    [SerializeField] private LayerMask mineableLayers = ~0;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.4f, 0f);
    [SerializeField] private float dropForce = 1.8f;

    [Header("Swing")]
    [SerializeField] private float swingAngle = 35f;
    [SerializeField] private float swingDuration = 0.16f;

    [Header("Mining UI")]
    [SerializeField] private Vector2 barSize = new Vector2(240f, 18f);
    [SerializeField] private Vector2 barOffset = new Vector2(0f, -130f);
    [SerializeField] private Color barBackgroundColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color barFillColor = new Color(0.2f, 0.85f, 0.3f, 1f);

    private MineableObject activeMineable;
    private float activeMineProgress;
    private bool isSwinging;
    private bool requireReleaseBeforeMining;

    private Canvas miningCanvas;
    private Slider miningSlider;
    private Transform visualTransform;
    private Quaternion visualBaseRotation;
    private Coroutine swingCoroutine;

    private void Awake()
    {
        ResolveReferences();
        BuildMiningUI();
        HideMiningUI();
    }

    private void Update()
    {
        var primaryPressedThisFrame = IsPrimaryPressedThisFrame();

        if (IsPrimaryHeld())
        {
            if (requireReleaseBeforeMining)
            {
                CancelMining();
                return;
            }

            if (TryGetMineableUnderMouse(out var mineable, out var hit))
            {
                TryStartSwing();
                ContinueMining(mineable, hit);
                return;
            }

            if (activeMineable != null)
            {
                InterruptMiningUntilRelease();
                return;
            }

            CancelMining();
            if (primaryPressedThisFrame)
            {
                TryStartSwing();
            }
            return;
        }

        requireReleaseBeforeMining = false;
        CancelMining();
    }

    private void ResolveReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (visual == null)
        {
            var visualChild = transform.Find("Visual");
            if (visualChild != null)
            {
                visual = visualChild;
            }
        }

        visualTransform = visual != null ? visual : transform;
        visualBaseRotation = visualTransform.localRotation;
    }

    private void BuildMiningUI()
    {
        var canvasObject = new GameObject("PickaxeMiningCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        miningCanvas = canvasObject.GetComponent<Canvas>();
        miningCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("MiningBar", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = barOffset;
        panelRect.sizeDelta = barSize;

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = barBackgroundColor;

        var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(panel.transform, false);

        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.offsetMin = new Vector2(4f, 4f);
        sliderRect.offsetMax = new Vector2(-4f, -4f);

        var fillAreaObject = new GameObject("FillArea", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);

        var fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillAreaObject.transform, false);

        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fillImage = fillObject.GetComponent<Image>();
        fillImage.color = barFillColor;
        fillImage.type = Image.Type.Simple;

        miningSlider = sliderObject.GetComponent<Slider>();
        miningSlider.minValue = 0f;
        miningSlider.maxValue = 1f;
        miningSlider.value = 0f;
        miningSlider.transition = Selectable.Transition.None;
        miningSlider.targetGraphic = fillImage;
        miningSlider.fillRect = fillRect;
        miningSlider.handleRect = null;
        miningSlider.direction = Slider.Direction.LeftToRight;
    }

    private void ContinueMining(MineableObject mineable, RaycastHit hit)
    {
        if (activeMineable != mineable)
        {
            activeMineable = mineable;
            activeMineProgress = 0f;
        }

        ShowMiningUI();
        activeMineProgress += Time.deltaTime / activeMineable.MiningDuration;
        miningSlider.value = Mathf.Clamp01(activeMineProgress);

        if (activeMineProgress < 1f)
        {
            return;
        }

        CompleteMining(hit.point, activeMineable);
        CancelMining();
    }

    private void CompleteMining(Vector3 hitPoint, MineableObject mineable)
    {
        var dropPrefab = mineable.DropPrefab;
        if (dropPrefab == null)
        {
            mineable.OnMined();
            return;
        }

        var dropPosition = hitPoint + dropOffset;
        var spawned = Instantiate(dropPrefab, dropPosition, Quaternion.identity);

        if (spawned.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            rigidbody.AddForce((Vector3.up + transform.forward * 0.15f) * dropForce, ForceMode.Impulse);
        }

        mineable.OnMined();
    }

    private bool TryGetMineableUnderMouse(out MineableObject mineable, out RaycastHit hit)
    {
        mineable = null;
        hit = default;

        if (playerCamera == null)
        {
            return false;
        }

        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out hit, maxMineDistance, mineableLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        mineable = hit.collider.GetComponentInParent<MineableObject>();
        return mineable != null;
    }

    private void TryStartSwing()
    {
        if (isSwinging)
        {
            return;
        }

        swingCoroutine = StartCoroutine(SwingRoutine());
    }

    private System.Collections.IEnumerator SwingRoutine()
    {
        isSwinging = true;
        var elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            var normalized = Mathf.Clamp01(elapsed / swingDuration);
            var angle = Mathf.Sin(normalized * Mathf.PI) * swingAngle;
            visualTransform.localRotation = Quaternion.Euler(-angle, 0f, 0f) * visualBaseRotation;
            yield return null;
        }

        visualTransform.localRotation = visualBaseRotation;
        isSwinging = false;
        swingCoroutine = null;
    }

    private void InterruptMiningUntilRelease()
    {
        requireReleaseBeforeMining = true;
        CancelMining();
        StopSwing();
    }

    private void StopSwing()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        isSwinging = false;
        if (visualTransform != null)
        {
            visualTransform.localRotation = visualBaseRotation;
        }
    }

    private void CancelMining()
    {
        activeMineable = null;
        activeMineProgress = 0f;
        HideMiningUI();
    }

    private void ShowMiningUI()
    {
        if (miningCanvas != null)
        {
            miningCanvas.enabled = true;
        }
    }

    private void HideMiningUI()
    {
        if (miningSlider != null)
        {
            miningSlider.value = 0f;
        }

        if (miningCanvas != null)
        {
            miningCanvas.enabled = false;
        }
    }

    private static bool IsPrimaryPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static bool IsPrimaryHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }
}
