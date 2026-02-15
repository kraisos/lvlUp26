using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SanitySystem : MonoBehaviour
{
    [Header("Sanity")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float lightDrainRate = 5f;
    [SerializeField] private float darkRiseRate = 2f;
    [SerializeField] private float mobDetectionRadius = 15f;

    [Header("UI")]
    [SerializeField] private float barWidth = 300f;
    [SerializeField] private float barHeight = 24f;
    [SerializeField] private float iconSize = 40f;
    [SerializeField] private Vector2 barOffset = new Vector2(40f, -20f);
    [SerializeField] private Sprite sanityIcon;
    [SerializeField] private float breathingSpeed = 2f;
    [SerializeField] private float breathingThreshold = 0.4f;

    private float currentSanity;
    private bool maxReached;
    private readonly Collider[] mobCheckBuffer = new Collider[20];
    private readonly HashSet<MobAI> nearbyMobsSet = new HashSet<MobAI>();

    private Image barFill;
    private CanvasGroup barCanvasGroup;

    public float CurrentSanity => currentSanity;
    public float MaxSanity => maxSanity;
    public float SanityNormalized => currentSanity / maxSanity;

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

    private void UpdateUI()
    {
        if (barFill == null)
        {
            return;
        }

        float t = SanityNormalized;
        barFill.rectTransform.localScale = new Vector3(t, 1f, 1f);
        barFill.color = Color.Lerp(new Color(0.2f, 0.55f, 0.2f, 1f), new Color(0.7f, 0.15f, 0.15f, 1f), t);

        if (barCanvasGroup != null && t > breathingThreshold)
        {
            float intensity = (t - breathingThreshold) / (1f - breathingThreshold);
            float pulse = Mathf.Sin(Time.time * breathingSpeed * Mathf.Lerp(1f, 3f, intensity)) * 0.5f + 0.5f;
            float minAlpha = Mathf.Lerp(1f, 0.35f, intensity);
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

        var iconRect = iconObj.GetComponent<RectTransform>();
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

        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 1f);
        bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.pivot = new Vector2(0f, 0.5f);
        bgRect.anchoredPosition = new Vector2(barOffset.x + iconSize + 6f, barOffset.y - iconSize * 0.5f);
        bgRect.sizeDelta = new Vector2(barWidth, barHeight);

        var bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.2f);

        // Inner background (dark fill area)
        var innerObj = new GameObject("SanityBarInner", typeof(RectTransform), typeof(Image));
        innerObj.transform.SetParent(bgObj.transform, false);

        var innerRect = innerObj.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);

        var innerImage = innerObj.GetComponent<Image>();
        innerImage.color = new Color(0f, 0f, 0f, 0.5f);

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
        barFill.color = new Color(0.2f, 0.55f, 0.2f, 1f);
        barFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
    }
}
