using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GasPipeRenderer : MonoBehaviour
{
    private const int SegmentCount = 20;
    private const float PipeWidth = 0.15f;
    private const float TangentStrength = 0.4f;
    private const float SCurveOffset = 0.5f;

    private LineRenderer lineRenderer;
    private Material normalMaterial;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3[] cachedPath;

    public void Init(Material copperMaterial)
    {
        normalMaterial = copperMaterial;
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = SegmentCount + 1;
        lineRenderer.startWidth = PipeWidth;
        lineRenderer.endWidth = PipeWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        if (normalMaterial != null)
        {
            lineRenderer.material = normalMaterial;
        }
    }

    public void SetEndpoints(Vector3 start, Vector3 end)
    {
        startPoint = start;
        endPoint = end;
        UpdateCurve();
    }

    public void SetGhostMode(bool ghost, Material ghostMaterial)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.material = ghost ? ghostMaterial : normalMaterial;
    }

    private void UpdateCurve()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.positionCount = SegmentCount + 1;

        var dist = Vector3.Distance(startPoint, endPoint);
        var tangentLen = dist * TangentStrength;

        var dirXZ = endPoint - startPoint;
        dirXZ.y = 0f;
        if (dirXZ.sqrMagnitude > 0.001f)
        {
            dirXZ.Normalize();
        }
        else
        {
            dirXZ = Vector3.forward;
        }

        // Perpendicular on the XZ plane for the S shape
        var perp = new Vector3(-dirXZ.z, 0f, dirXZ.x);

        var groundY = Mathf.Min(startPoint.y, endPoint.y);

        // S curve: P1 offset to the left, P2 offset to the right
        var p0 = startPoint;
        var p1 = startPoint + dirXZ * tangentLen + perp * SCurveOffset;
        p1.y = groundY;
        var p2 = endPoint - dirXZ * tangentLen - perp * SCurveOffset;
        p2.y = groundY;
        var p3 = endPoint;

        cachedPath = new Vector3[SegmentCount + 1];
        for (var i = 0; i <= SegmentCount; i++)
        {
            var t = (float)i / SegmentCount;
            var position = CubicBezier(p0, p1, p2, p3, t);
            cachedPath[i] = position;
            lineRenderer.SetPosition(i, position);
        }
    }

    public Vector3[] GetPath()
    {
        return cachedPath;
    }

    public Vector3[] GetReversedPath()
    {
        if (cachedPath == null)
        {
            return null;
        }

        var reversed = new Vector3[cachedPath.Length];
        for (var i = 0; i < cachedPath.Length; i++)
        {
            reversed[i] = cachedPath[cachedPath.Length - 1 - i];
        }

        return reversed;
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var u = 1f - t;
        var uu = u * u;
        var tt = t * t;
        return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
    }
}
