using UnityEngine;

[RequireComponent(typeof(CableAnchor))]
public class Streetlight : MonoBehaviour
{
    [Header("Lamp Light")]
    [SerializeField] private Color lightColor = new Color(1f, 0.9f, 0.7f); // warm white
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightRange = 12f;
    [SerializeField] private Vector3 lightOffset = new Vector3(0f, 4f, 0f); // near the lamp head

    public bool IsEnergySource { get; private set; }

    private void OnEnable()
    {
        EnsureNetwork();
        GasPipeNetwork.Instance.Register(this);
        SetupLight();
    }

    private void OnDisable()
    {
        if (GasPipeNetwork.Instance != null)
        {
            GasPipeNetwork.Instance.Unregister(this);
        }
    }

    public void MarkAsEnergySource()
    {
        IsEnergySource = true;
    }

    private static void EnsureNetwork()
    {
        if (GasPipeNetwork.Instance != null)
        {
            return;
        }

        var go = new GameObject("GasPipeNetwork");
        go.AddComponent<GasPipeNetwork>();
    }

    private void SetupLight()
    {
        // Only add if not already present
        if (GetComponentInChildren<Light>() != null) return;

        var lightObj = new GameObject("LampLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = lightOffset;

        var pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = lightColor;
        pointLight.intensity = lightIntensity;
        pointLight.range = lightRange;
        pointLight.shadows = LightShadows.Soft;

        // Apply emissive glow to the mesh renderer if present
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            foreach (var mat in meshRenderer.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", lightColor * lightIntensity);
                }
            }
        }
    }
}
