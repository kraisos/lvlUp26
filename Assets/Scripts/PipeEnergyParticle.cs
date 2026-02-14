using UnityEngine;

public class PipeEnergyParticle : MonoBehaviour
{
    private const float GlowRange = 3f;
    private const float GlowIntensity = 2f;

    private Vector3[] path;
    private int segmentCount;
    private float speed;
    private float progress;
    private Light glowLight;

    public void Init(Vector3[] curvePath, float travelSpeed, Color color, float size)
    {
        path = curvePath;
        segmentCount = path.Length - 1;
        speed = travelSpeed;
        progress = 0f;

        // Use a Unity sphere primitive for the visual (visible from all angles)
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * size;
        sphere.name = "Glow_Sphere";

        // Remove collider from the sphere
        var col = sphere.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        // Emissive material
        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetColor("_EmissionColor", color * 3f);
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 1f);
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Point light for glow effect
        glowLight = gameObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = color;
        glowLight.intensity = GlowIntensity;
        glowLight.range = GlowRange;
        glowLight.shadows = LightShadows.None;
        glowLight.renderMode = LightRenderMode.Auto;

        if (path.Length > 0)
        {
            transform.position = path[0];
        }
    }

    private void Update()
    {
        if (path == null || segmentCount <= 0)
        {
            return;
        }

        progress += speed * Time.deltaTime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        var totalT = progress * segmentCount;
        var segIndex = Mathf.FloorToInt(totalT);
        if (segIndex >= segmentCount)
        {
            segIndex = segmentCount - 1;
        }

        var localT = totalT - segIndex;
        transform.position = Vector3.Lerp(path[segIndex], path[segIndex + 1], localT);

        // Pulse
        if (glowLight != null)
        {
            var pulse = Mathf.Sin(Time.time * 8f) * 0.4f + 0.6f;
            glowLight.intensity = GlowIntensity * pulse;
        }
    }
}
