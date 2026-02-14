using UnityEngine;
using System.Collections.Generic;

public class MaskVolume : MonoBehaviour
{
    public enum VolumeShape
    {
        Box,
        Sphere,
        Cone
    }

    public VolumeShape shape = VolumeShape.Box;

    [Header("Tile Generation")]
    public TilesGenerator tilesGenerator;
    public Light pointLight;
    public GameObject darknessVolumeMesh;

    [Header("Cone Settings")]
    [Range(0f, 1f)]
    public float coneTopRadius = 0f;

    // Track all active volumes
    private static List<MaskVolume> activeVolumes = new List<MaskVolume>();
    private static Matrix4x4[] matricesArray = new Matrix4x4[16];
    private static Vector4[] shapeDataArray = new Vector4[16]; // x = shape, y = coneTopRadius
    const int MAX_VOLUMES = 16;

    // Initialize shader data on startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitializeShaderData()
    {
        // Initialize arrays with safe defaults
        for (int i = 0; i < MAX_VOLUMES; i++)
        {
            matricesArray[i] = Matrix4x4.identity;
            shapeDataArray[i] = new Vector4(-1, 0, 0, 0); // -1 = inactive
        }

        Shader.SetGlobalMatrixArray("_MaskMatrices", matricesArray);
        Shader.SetGlobalVectorArray("_MaskShapeData", shapeDataArray);
        Shader.SetGlobalInt("_MaskCount", 0);
    }

    void Start()
    {
        updateLinkedRanges();
    }

    void OnEnable()
    {
        if (!activeVolumes.Contains(this))
            activeVolumes.Add(this);
        UpdateShaderData();
    }

    void OnDisable()
    {
        activeVolumes.Remove(this);
        UpdateShaderData();
    }

    void Update()
    {
        updateLinkedRanges();
        UpdateShaderData();
    }

    void updateLinkedRanges()
    {
        // Get the radius from the sphere mesh (Unity sphere has diameter 1, so base radius is 0.5)
        // Use the maximum scale component to get the sphere radius
        float radius = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 0.5f;

        // TODO here don't add "1" but add the tile size
        tilesGenerator.radius = radius + 1; // Ensure tile generation radius matches the sphere
        pointLight.range = radius; // Ensure light range matches the sphere

        if (darknessVolumeMesh != null)
            darknessVolumeMesh.transform.localScale = transform.localScale; // Ensure darkness volume matches the sphere
    }

    static void UpdateShaderData()
    {
        int count = Mathf.Min(activeVolumes.Count, MAX_VOLUMES);

        for (int i = 0; i < count; i++)
        {
            var vol = activeVolumes[i];
            matricesArray[i] = vol.transform.worldToLocalMatrix;
            shapeDataArray[i] = new Vector4(
                (int)vol.shape,
                vol.coneTopRadius,
                0, 0
            );
        }

        // Clear unused slots
        for (int i = count; i < MAX_VOLUMES; i++)
        {
            shapeDataArray[i] = new Vector4(-1, 0, 0, 0); // -1 = inactive
        }

        Shader.SetGlobalMatrixArray("_MaskMatrices", matricesArray);
        Shader.SetGlobalVectorArray("_MaskShapeData", shapeDataArray);
        Shader.SetGlobalInt("_MaskCount", count);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        switch (shape)
        {
            case VolumeShape.Box:
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                break;

            case VolumeShape.Sphere:
                Gizmos.DrawSphere(Vector3.zero, 0.5f);
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
                Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
                break;

            case VolumeShape.Cone:
                DrawConeGizmo();
                break;
        }
    }

    void DrawConeGizmo()
    {
        int segments = 24;
        float bottomRadius = 0.5f;
        float topRadius = 0.5f * coneTopRadius;
        float halfHeight = 0.5f;

        Vector3[] bottomRing = new Vector3[segments];
        Vector3[] topRing = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            bottomRing[i] = new Vector3(cos * bottomRadius, -halfHeight, sin * bottomRadius);
            topRing[i] = new Vector3(cos * topRadius, halfHeight, sin * topRadius);
        }

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            Gizmos.DrawLine(bottomRing[i], bottomRing[next]);
            Gizmos.DrawLine(topRing[i], topRing[next]);
            Gizmos.DrawLine(bottomRing[i], topRing[i]);
        }
    }
}
