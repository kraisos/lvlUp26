using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SanitySystem : MonoBehaviour
{
    [Header("Sanity")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float lightDrainRate = 5f;
    [SerializeField] private float darkRiseRate = 0.667f;
    [SerializeField] private float sprintRiseRate = 1.5f;
    [SerializeField] private float mobDetectionRadius = 15f;

    [Header("UI")]
    [SerializeField] private float barWidth = 600f;
    [SerializeField] private float barHeight = 48f;
    [SerializeField] private float iconSize = 80f;
    [SerializeField] private Vector2 barOffset = new Vector2(40f, -20f);
    [SerializeField] private Sprite sanityIcon;
    [SerializeField] private float breathingSpeed = 2f;
    [SerializeField] private float breathingThreshold = 0.4f;

    private float currentSanity;
    private bool maxReached;
    private bool isSprinting;
    private readonly Collider[] mobCheckBuffer = new Collider[20];
    private readonly HashSet<MobAI> nearbyMobsSet = new HashSet<MobAI>();

    private Image barFill;
    private CanvasGroup barCanvasGroup;
    private RectTransform barBgRect;
    private RectTransform iconRect;
    private Vector2 barBgBasePos;

    // Sprint bar
    private Image sprintBarFill;
    private CanvasGroup sprintBarCanvasGroup;
    private bool sprintCooldownBreathing;
    private static readonly Color sprintBarBaseColor = new Color(0.25f, 0.3f, 0.35f, 1f);
    private static readonly Color sprintBarCooldownColor = new Color(0.6f, 0.15f, 0.1f, 1f);

    public float CurrentSanity => currentSanity;
    public float MaxSanity => maxSanity;
    public float SanityNormalized => currentSanity / maxSanity;

    public void SetSprinting(bool sprinting) => isSprinting = sprinting;

    private void Start()
    {
        BuildUI();
    }

    private void Update()
    {
        float rate = 0f;

        if (LightedZone.IsPlayerInAnyLightZone)
        {
            rate -= lightDrainRate;
        }
        else
        {
            rate += darkRiseRate;
        }

        rate += GetNearbyMobsProximityRate();

        if (isSprinting)
        {
            rate += sprintRiseRate;
        }

        currentSanity += rate * Time.deltaTime;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        if (currentSanity >= maxSanity && !maxReached)
        {
            maxReached = true;
            TriggerGameOver();
            return;
        }
        else if (currentSanity < maxSanity)
        {
            maxReached = false;
        }

        UpdateUI();
    }

    private float GetNearbyMobsProximityRate()
    {
        nearbyMobsSet.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, mobDetectionRadius, mobCheckBuffer);

        for (int i = 0; i < hitCount && i < mobCheckBuffer.Length; i++)
        {
            var mob = mobCheckBuffer[i].GetComponentInParent<MobAI>();
            if (mob != null)
            {
                nearbyMobsSet.Add(mob);
            }
        }

        float totalRate = 0f;
        foreach (var mob in nearbyMobsSet)
        {
            totalRate += mob.sanityProximityRate;
        }

        return totalRate;
    }

    private void TriggerGameOver()
    {
        var fpc = GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            fpc.HandlePlayerDeath(transform);
        }
    }

    public void OnMobContact(float damage)
    {
        currentSanity = Mathf.Min(currentSanity + damage, maxSanity);
        Debug.Log($"[SanitySystem] Mob contact! Sanity: {currentSanity:F1}/{maxSanity}");
    }

    public void SetSprintBar(float normalizedValue)
    {
        if (sprintBarFill != null)
        {
            sprintBarFill.rectTransform.localScale = new Vector3(normalizedValue, 1f, 1f);
        }
    }

    public void SetSprintBarAlpha(float alpha)
    {
        if (sprintBarCanvasGroup != null)
        {
            sprintBarCanvasGroup.alpha = alpha;
        }
    }

    public float GetSprintBarAlpha()
    {
        return sprintBarCanvasGroup != null ? sprintBarCanvasGroup.alpha : 0f;
    }

    public void SetSprintBarCooldown(bool cooling)
    {
        sprintCooldownBreathing = cooling;
        if (!cooling && sprintBarFill != null)
        {
            sprintBarFill.color = sprintBarBaseColor;
        }
    }

    private void UpdateUI()
    {
        if (barFill == null)
        {
            return;
        }

        float t = SanityNormalized;

        // Wiggle effect: the fill bar squirms like it's alive
        float wiggle = t * t; // intensity scales with sanity squared
        float wiggleRotation = Mathf.Sin(Time.time * 6f) * 1.5f * wiggle;
        float wiggleScaleY = 1f + Mathf.Sin(Time.time * 9f) * 0.06f * wiggle;
        float wiggleScaleX = t + Mathf.Sin(Time.time * 7.3f) * 0.015f * wiggle;
        barFill.rectTransform.localScale = new Vector3(wiggleScaleX, wiggleScaleY, 1f);
        barFill.rectTransform.localRotation = Quaternion.Euler(0f, 0f, wiggleRotation);

        // Horror color gradient: dark teal -> bruised purple -> deep crimson
        Color calmColor = new Color(0.1f, 0.35f, 0.3f, 1f);
        Color midColor = new Color(0.35f, 0.1f, 0.4f, 1f);
        Color panicColor = new Color(0.6f, 0.05f, 0.05f, 1f);
        barFill.color = t < 0.5f
            ? Color.Lerp(calmColor, midColor, t * 2f)
            : Color.Lerp(midColor, panicColor, (t - 0.5f) * 2f);

        // Bar jitter at high sanity
        if (barBgRect != null && t > 0.6f)
        {
            float jitterStrength = (t - 0.6f) / 0.4f * 3f;
            float jx = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * jitterStrength;
            float jy = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * jitterStrength;
            barBgRect.anchoredPosition = barBgBasePos + new Vector2(jx, jy);
        }
        else if (barBgRect != null)
        {
            barBgRect.anchoredPosition = barBgBasePos;
        }

        // Icon wobble at critical sanity
        if (iconRect != null && t > 0.7f)
        {
            float wobbleStrength = (t - 0.7f) / 0.3f;
            float angle = Mathf.Sin(Time.time * 8f) * 15f * wobbleStrength;
            iconRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (iconRect != null)
        {
            iconRect.localRotation = Quaternion.identity;
        }

        // Sprint bar cooldown breathing
        if (sprintCooldownBreathing && sprintBarFill != null)
        {
            float pulse = Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f;
            sprintBarFill.color = Color.Lerp(sprintBarBaseColor, sprintBarCooldownColor, pulse);
        }

        // More aggressive pulsing
        if (barCanvasGroup != null && t > breathingThreshold)
        {
            float intensity = (t - breathingThreshold) / (1f - breathingThreshold);
            float speed = breathingSpeed * Mathf.Lerp(1f, 5f, intensity);
            float pulse = Mathf.Sin(Time.time * speed) * 0.5f + 0.5f;
            float minAlpha = Mathf.Lerp(1f, 0.2f, intensity);
            barCanvasGroup.alpha = Mathf.Lerp(minAlpha, 1f, pulse);
        }
        else if (barCanvasGroup != null)
        {
            barCanvasGroup.alpha = 1f;
        }
    }

    private void BuildUI()
    {
        var canvasObj = new GameObject("SanityBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Icon placeholder (left side)
        var iconObj = new GameObject("SanityIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(canvasObj.transform, false);

        iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = barOffset;
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);

        var iconImage = iconObj.GetComponent<Image>();
        if (sanityIcon != null)
        {
            iconImage.sprite = sanityIcon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = new Color(1f, 1f, 1f, 0.3f);
        }

        // Bar background (right of icon)
        var bgObj = new GameObject("SanityBarBG", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        bgObj.transform.SetParent(canvasObj.transform, false);
        barCanvasGroup = bgObj.GetComponent<CanvasGroup>();

        barBgRect = bgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 1f);
        barBgRect.anchorMax = new Vector2(0f, 1f);
        barBgRect.pivot = new Vector2(0f, 0.5f);
        barBgBasePos = new Vector2(barOffset.x + iconSize + 6f, barOffset.y - iconSize * 0.5f);
        barBgRect.anchoredPosition = barBgBasePos;
        barBgRect.sizeDelta = new Vector2(barWidth, barHeight);

        var bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(0.15f, 0.05f, 0.2f, 0.7f);

        // Inner background (dark fill area)
        var innerObj = new GameObject("SanityBarInner", typeof(RectTransform), typeof(Image));
        innerObj.transform.SetParent(bgObj.transform, false);

        var innerRect = innerObj.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);

        var innerImage = innerObj.GetComponent<Image>();
        innerImage.color = new Color(0.02f, 0f, 0.05f, 0.8f);

        // Bar fill
        var fillObj = new GameObject("SanityBarFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(innerObj.transform, false);

        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0f, 0.5f);

        barFill = fillObj.GetComponent<Image>();
        barFill.color = new Color(0.1f, 0.35f, 0.3f, 1f);
        barFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);

        // "Madness level" label inside the bar
        var textObj = new GameObject("SanityBarLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(innerObj.transform, false);

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var label = textObj.GetComponent<TextMeshProUGUI>();
        label.text = "Madness level";
        label.fontSize = barHeight * 0.45f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.85f, 0.75f, 0.9f, 0.9f);

        // Sprint bar (below the madness bar)
        float sprintBarHeight = barHeight * 0.5f;
        float sprintBarY = barBgBasePos.y - barHeight * 0.5f - 6f - sprintBarHeight * 0.5f;

        var sprintBgObj = new GameObject("SprintBarBG", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        sprintBgObj.transform.SetParent(canvasObj.transform, false);
        sprintBarCanvasGroup = sprintBgObj.GetComponent<CanvasGroup>();
        sprintBarCanvasGroup.alpha = 0f;

        var sprintBgRect = sprintBgObj.GetComponent<RectTransform>();
        sprintBgRect.anchorMin = new Vector2(0f, 1f);
        sprintBgRect.anchorMax = new Vector2(0f, 1f);
        sprintBgRect.pivot = new Vector2(0f, 0.5f);
        sprintBgRect.anchoredPosition = new Vector2(barBgBasePos.x, sprintBarY);
        sprintBgRect.sizeDelta = new Vector2(barWidth, sprintBarHeight);

        var sprintBgImage = sprintBgObj.GetComponent<Image>();
        sprintBgImage.color = new Color(0.08f, 0.06f, 0.1f, 0.7f);

        var sprintInnerObj = new GameObject("SprintBarInner", typeof(RectTransform), typeof(Image));
        sprintInnerObj.transform.SetParent(sprintBgObj.transform, false);

        var sprintInnerRect = sprintInnerObj.GetComponent<RectTransform>();
        sprintInnerRect.anchorMin = Vector2.zero;
        sprintInnerRect.anchorMax = Vector2.one;
        sprintInnerRect.offsetMin = new Vector2(2f, 2f);
        sprintInnerRect.offsetMax = new Vector2(-2f, -2f);

        var sprintInnerImage = sprintInnerObj.GetComponent<Image>();
        sprintInnerImage.color = new Color(0.03f, 0.02f, 0.04f, 0.85f);

        var sprintFillObj = new GameObject("SprintBarFill", typeof(RectTransform), typeof(Image));
        sprintFillObj.transform.SetParent(sprintInnerObj.transform, false);

        var sprintFillRect = sprintFillObj.GetComponent<RectTransform>();
        sprintFillRect.anchorMin = Vector2.zero;
        sprintFillRect.anchorMax = Vector2.one;
        sprintFillRect.offsetMin = Vector2.zero;
        sprintFillRect.offsetMax = Vector2.zero;
        sprintFillRect.pivot = new Vector2(0f, 0.5f);

        sprintBarFill = sprintFillObj.GetComponent<Image>();
        sprintBarFill.color = new Color(0.25f, 0.3f, 0.35f, 1f);
        sprintBarFill.rectTransform.localScale = new Vector3(1f, 1f, 1f);
    }
}
