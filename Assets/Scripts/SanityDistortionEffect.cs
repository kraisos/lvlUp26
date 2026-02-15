using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SanityDistortionEffect : MonoBehaviour
{
    [Header("Distortion Settings")]
    [SerializeField] private float sanityThreshold = 0.4f;
    [SerializeField] private float maxIntensity = 1f;

    private Material distortionMat;
    private SanitySystem sanitySystem;

    private void Start()
    {
        var shader = Shader.Find("Custom/SanityDistortion");
        if (shader == null)
        {
            Debug.LogError("[SanityDistortion] Shader 'Custom/SanityDistortion' not found!");
            enabled = false;
            return;
        }

        distortionMat = new Material(shader);

        sanitySystem = GetComponentInParent<SanitySystem>();
        if (sanitySystem == null)
        {
            sanitySystem = FindFirstObjectByType<SanitySystem>();
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (distortionMat == null || sanitySystem == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        float sanity = sanitySystem.SanityNormalized;

        if (sanity <= sanityThreshold)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // Remap sanity from [threshold..1] to [0..maxIntensity]
        float t = (sanity - sanityThreshold) / (1f - sanityThreshold);
        // Ease in so it ramps up more aggressively toward max
        float intensity = t * t * maxIntensity;

        distortionMat.SetFloat("_Intensity", intensity);
        Graphics.Blit(src, dest, distortionMat);
    }

    private void OnDestroy()
    {
        if (distortionMat != null)
        {
            Destroy(distortionMat);
        }
    }
}
