using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlareLightPattern : MonoBehaviour
{
    [Header("Flicker")]
    [SerializeField] private float minIntensity = 0.9f;
    [SerializeField] private float maxIntensity = 2.0f;
    [SerializeField] private float flickerSpeed = 10f;
    [SerializeField] private float jitterAmount = 0.2f;

    [Header("Lifetime")]
    [SerializeField] private float timeBeforeFade = 20f;
    [SerializeField] private float fadeDuration = 5f;

    private Light flareLight;
    private float elapsed;
    private float noiseSeed;

    private void Awake()
    {
        flareLight = GetComponent<Light>();
        noiseSeed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float noiseTime = Time.time * flickerSpeed;
        float smoothNoise = Mathf.PerlinNoise(noiseSeed, noiseTime);
        float baseIntensity = Mathf.Lerp(minIntensity, maxIntensity, smoothNoise);

        float jitter = Random.Range(-jitterAmount, jitterAmount);
        float targetIntensity = Mathf.Max(0f, baseIntensity + jitter);

        float fadeMultiplier = 1f;
        if (elapsed > timeBeforeFade)
        {
            float fadeElapsed = elapsed - timeBeforeFade;
            fadeMultiplier = 1f - Mathf.Clamp01(fadeElapsed / Mathf.Max(0.01f, fadeDuration));
        }

        flareLight.intensity = targetIntensity * fadeMultiplier;

        if (fadeMultiplier <= 0f)
        {
            flareLight.enabled = false;
            enabled = false;
        }
    }
}
