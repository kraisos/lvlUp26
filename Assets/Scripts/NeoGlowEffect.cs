using UnityEngine;

public class NeoGlowEffect : MonoBehaviour
{
    [Header("Glow Material")]
    public Material glowMaterial;

    [Header("Override Settings")]
    [Tooltip("Override glow color from material")]
    public bool overrideColor = false;
    [ColorUsage(true, true)]
    public Color glowColor = new Color(0f, 0.85f, 1f, 1f);

    [Tooltip("Override glow intensity from material")]
    public bool overrideIntensity = false;
    [Range(0f, 10f)]
    public float glowIntensity = 2.5f;

    private MaterialPropertyBlock _propBlock;

    void Start()
    {
        if (glowMaterial == null) return;

        _propBlock = new MaterialPropertyBlock();
        ApplyGlowToChildren();
    }

    void ApplyGlowToChildren()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;

            // Create a child GameObject for the glow overlay
            GameObject glowObj = new GameObject("_GlowOverlay");
            glowObj.transform.SetParent(renderer.transform, false);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localRotation = Quaternion.identity;
            glowObj.transform.localScale = Vector3.one;

            MeshFilter glowFilter = glowObj.AddComponent<MeshFilter>();
            glowFilter.sharedMesh = filter.sharedMesh;

            MeshRenderer glowRenderer = glowObj.AddComponent<MeshRenderer>();
            glowRenderer.sharedMaterial = glowMaterial;
            glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;

            // Apply property overrides
            if (overrideColor || overrideIntensity)
            {
                glowRenderer.GetPropertyBlock(_propBlock);
                if (overrideColor)
                    _propBlock.SetColor("_GlowColor", glowColor);
                if (overrideIntensity)
                    _propBlock.SetFloat("_GlowIntensity", glowIntensity);
                glowRenderer.SetPropertyBlock(_propBlock);
            }
        }
    }
}
