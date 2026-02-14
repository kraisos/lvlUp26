using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryBarUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private bool autoFindInventory = true;

    [Header("Layout")]
    [SerializeField] private int slotCount = 4;
    [SerializeField] private float slotSize = 64f;
    [SerializeField] private float quantityLabelHeight = 18f;
    [SerializeField] private float slotSpacing = 10f;
    [SerializeField] private Vector2 barPadding = new Vector2(12f, 12f);
    [SerializeField] private Vector2 barOffset = new Vector2(0f, 28f);

    [Header("Colors")]
    [SerializeField] private Color barColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color slotNormalColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color slotSelectedColor = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField] private Color slotTextColor = Color.white;
    [SerializeField] private Color slotIconTint = Color.white;

    private readonly List<Image> slotImages = new List<Image>();
    private readonly List<Image> slotIcons = new List<Image>();
    private readonly List<Text> slotTexts = new List<Text>();
    private Inventory subscribedInventory;
    private int selectedIndex;

    private void Awake()
    {
        BuildUI();
        TryBindInventory();
        RefreshSlotContent();
        SetSelected(0);
    }

    private void OnEnable()
    {
        TryBindInventory();
    }

    private void OnDisable()
    {
        UnbindInventory();
    }

    private void Update()
    {
        if (inventory == null && autoFindInventory)
        {
            TryBindInventory();
        }

        if (IsSlotKeyPressed(1))
        {
            SetSelected(0);
        }
        else if (IsSlotKeyPressed(2))
        {
            SetSelected(1);
        }
        else if (IsSlotKeyPressed(3))
        {
            SetSelected(2);
        }
        else if (IsSlotKeyPressed(4))
        {
            SetSelected(3);
        }
    }

    private static bool IsSlotKeyPressed(int slotNumber)
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return slotNumber switch
        {
            1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
            2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
            3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
            4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
            _ => false
        };
#else
        return slotNumber switch
        {
            1 => Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1),
            2 => Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2),
            3 => Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3),
            4 => Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4),
            _ => false
        };
#endif
    }

    private void BuildUI()
    {
        slotImages.Clear();
        slotIcons.Clear();
        slotTexts.Clear();

        var canvasObject = new GameObject("InventoryBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var barObject = new GameObject("InventoryBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        barObject.transform.SetParent(canvasObject.transform, false);

        var barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = barOffset;

        var width = (slotCount * slotSize) + ((slotCount - 1) * slotSpacing) + (barPadding.x * 2f);
        var height = slotSize + quantityLabelHeight + (barPadding.y * 2f);
        barRect.sizeDelta = new Vector2(width, height);

        var barImage = barObject.GetComponent<Image>();
        barImage.color = barColor;

        var layout = barObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset((int)barPadding.x, (int)barPadding.x, (int)barPadding.y, (int)barPadding.y);
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        for (var i = 0; i < slotCount; i++)
        {
            var slotContainer = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            slotContainer.transform.SetParent(barObject.transform, false);

            var layoutElement = slotContainer.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = slotSize;
            layoutElement.preferredHeight = slotSize + quantityLabelHeight;

            var slotContainerLayout = slotContainer.GetComponent<VerticalLayoutGroup>();
            slotContainerLayout.childAlignment = TextAnchor.UpperCenter;
            slotContainerLayout.childControlWidth = true;
            slotContainerLayout.childControlHeight = false;
            slotContainerLayout.childForceExpandWidth = false;
            slotContainerLayout.childForceExpandHeight = false;
            slotContainerLayout.spacing = 2f;
            slotContainerLayout.padding = new RectOffset(0, 0, 0, 0);

            var slotObject = new GameObject("SlotFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotObject.transform.SetParent(slotContainer.transform, false);

            var slotLayoutElement = slotObject.GetComponent<LayoutElement>();
            slotLayoutElement.preferredWidth = slotSize;
            slotLayoutElement.preferredHeight = slotSize;

            var slotImage = slotObject.GetComponent<Image>();
            slotImage.color = slotNormalColor;

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slotObject.transform, false);

            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.15f, 0.15f);
            iconRect.anchorMax = new Vector2(0.85f, 0.85f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.color = new Color(slotIconTint.r, slotIconTint.g, slotIconTint.b, 0f);

            var labelObject = new GameObject("QuantityLabel", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            labelObject.transform.SetParent(slotContainer.transform, false);

            var labelLayoutElement = labelObject.GetComponent<LayoutElement>();
            labelLayoutElement.preferredWidth = slotSize;
            labelLayoutElement.preferredHeight = quantityLabelHeight;

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.fontSize = 12;
            label.color = slotTextColor;
            label.text = string.Empty;

            slotImages.Add(slotImage);
            slotIcons.Add(icon);
            slotTexts.Add(label);
        }
    }

    private void TryBindInventory()
    {
        var targetInventory = inventory;
        if (targetInventory == null && autoFindInventory)
        {
            targetInventory = FindFirstObjectByType<Inventory>();
        }

        inventory = targetInventory;

        if (targetInventory == subscribedInventory)
        {
            return;
        }

        UnbindInventory();

        if (targetInventory != null)
        {
            targetInventory.Changed += OnInventoryChanged;
            subscribedInventory = targetInventory;
            RefreshSlotContent();
        }
    }

    private void UnbindInventory()
    {
        if (subscribedInventory == null)
        {
            return;
        }

        subscribedInventory.Changed -= OnInventoryChanged;
        subscribedInventory = null;
    }

    private void OnInventoryChanged()
    {
        RefreshSlotContent();
    }

    private void RefreshSlotContent()
    {
        for (var i = 0; i < slotTexts.Count; i++)
        {
            if (inventory != null && i < inventory.Items.Count)
            {
                var stack = inventory.Items[i];

                if (i < slotIcons.Count)
                {
                    slotIcons[i].sprite = stack.sprite;
                    slotIcons[i].color = stack.sprite != null
                        ? slotIconTint
                        : new Color(slotIconTint.r, slotIconTint.g, slotIconTint.b, 0f);
                }

                slotTexts[i].text = IsInfiniteQuantity(stack.quantity) ? string.Empty : stack.quantity.ToString();
            }
            else
            {
                if (i < slotIcons.Count)
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].color = new Color(slotIconTint.r, slotIconTint.g, slotIconTint.b, 0f);
                }

                slotTexts[i].text = string.Empty;
            }
        }
    }

    private static bool IsInfiniteQuantity(int quantity)
    {
        return quantity == int.MaxValue;
    }

    private void SetSelected(int index)
    {
        if (slotImages.Count == 0)
        {
            return;
        }

        selectedIndex = Mathf.Clamp(index, 0, slotImages.Count - 1);

        for (var i = 0; i < slotImages.Count; i++)
        {
            slotImages[i].color = i == selectedIndex ? slotSelectedColor : slotNormalColor;
        }
    }
}
