using UnityEngine;

public class NoiseFog : MonoBehaviour
{
    [Header("Fog Appearance")]
    [SerializeField] private Color fogColor = new Color(0.7f, 0.75f, 0.8f, 1f);
    [SerializeField, Range(0f, 1f)] private float density = 0.5f;
    [SerializeField, Range(0.1f, 20f)] private float noiseScale = 3f;

    [Header("Animation")]
    [SerializeField, Range(0f, 2f)] private float scrollSpeed = 0.3f;

    [Header("Fade")]
    [SerializeField, Range(0f, 1f)] private float fadeRadius = 0.8f;
    [SerializeField, Range(0.001f, 2f)] private float maskEdgeSoftness = 0.3f;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int DensityId = Shader.PropertyToID("_Density");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");
    private static readonly int FadeRadiusId = Shader.PropertyToID("_FadeRadius");
    private static readonly int MaskEdgeSoftnessId = Shader.PropertyToID("_MaskEdgeSoftness");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(FogColorId, fogColor);
        _mpb.SetFloat(DensityId, density);
        _mpb.SetFloat(NoiseScaleId, noiseScale);
        _mpb.SetFloat(ScrollSpeedId, scrollSpeed);
        _mpb.SetFloat(FadeRadiusId, fadeRadius);
        _mpb.SetFloat(MaskEdgeSoftnessId, maskEdgeSoftness);
        _renderer.SetPropertyBlock(_mpb);
    }
}
