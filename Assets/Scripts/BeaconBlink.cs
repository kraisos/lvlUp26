using System;
using System.Reflection;
using UnityEngine;

public class BeaconBlink : MonoBehaviour
{
    [SerializeField] private string visualChildName = "Visual";
    [SerializeField] private float breathingSpeed = 1.5f;
    [SerializeField] private float breathingAmount = 0.2f;
    [SerializeField] private float minLightMultiplier = 0.35f;
    [SerializeField] private float maxLightMultiplier = 1f;
    [SerializeField] private float minHaloMultiplier = 0.35f;
    [SerializeField] private float maxHaloMultiplier = 1f;

    private Transform visualTransform;
    private Vector3 baseScale;
    private Light[] spotLights;
    private float[] baseLightIntensities;
    private Behaviour haloBehaviour;
    private PropertyInfo haloSizeProperty;
    private PropertyInfo haloColorProperty;
    private float baseHaloSize = 1f;
    private Color baseHaloColor = Color.white;

    private void Awake()
    {
        Transform foundVisual = transform.Find(visualChildName);
        visualTransform = foundVisual != null ? foundVisual : transform;
        baseScale = visualTransform.localScale;

        Light[] childLights = GetComponentsInChildren<Light>(true);
        int spotCount = 0;

        for (int i = 0; i < childLights.Length; i++)
        {
            if (childLights[i].type == LightType.Spot)
            {
                spotCount++;
            }
        }

        spotLights = new Light[spotCount];
        baseLightIntensities = new float[spotCount];

        int index = 0;
        for (int i = 0; i < childLights.Length; i++)
        {
            if (childLights[i].type != LightType.Spot)
            {
                continue;
            }

            spotLights[index] = childLights[i];
            baseLightIntensities[index] = childLights[i].intensity;
            index++;
        }

        haloBehaviour = GetComponent("Halo") as Behaviour;
        if (haloBehaviour != null)
        {
            Type haloType = haloBehaviour.GetType();
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

            haloSizeProperty = haloType.GetProperty("size", bindingFlags);
            if (haloSizeProperty != null && haloSizeProperty.PropertyType == typeof(float))
            {
                baseHaloSize = (float)haloSizeProperty.GetValue(haloBehaviour, null);
            }

            haloColorProperty = haloType.GetProperty("color", bindingFlags);
            if (haloColorProperty != null && haloColorProperty.PropertyType == typeof(Color))
            {
                baseHaloColor = (Color)haloColorProperty.GetValue(haloBehaviour, null);
            }
        }
    }

    private void Update()
    {
        float pulse = (Mathf.Sin(Time.time * breathingSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scaleMultiplier = 1f + Mathf.Lerp(-breathingAmount, breathingAmount, pulse);
        float lightMultiplier = Mathf.Lerp(minLightMultiplier, maxLightMultiplier, pulse);
        float haloMultiplier = Mathf.Lerp(minHaloMultiplier, maxHaloMultiplier, pulse);

        visualTransform.localScale = baseScale * scaleMultiplier;

        for (int i = 0; i < spotLights.Length; i++)
        {
            if (spotLights[i] == null)
            {
                continue;
            }

            spotLights[i].intensity = baseLightIntensities[i] * lightMultiplier;
        }

        if (haloBehaviour != null)
        {
            if (haloSizeProperty != null)
            {
                haloSizeProperty.SetValue(haloBehaviour, baseHaloSize * haloMultiplier, null);
            }

            if (haloColorProperty != null)
            {
                Color haloColor = baseHaloColor;
                haloColor.a = Mathf.Clamp01(baseHaloColor.a * haloMultiplier);
                haloColorProperty.SetValue(haloBehaviour, haloColor, null);
            }
        }
    }
}
