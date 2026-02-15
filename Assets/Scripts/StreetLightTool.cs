using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StreetlightTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Map map;
    [SerializeField] private Inventory inventory;
    [SerializeField] private GameObject streetlightPrefab;
    public GameObject ghostPrefab;

    [Header("Placement")]
    [SerializeField] private LayerMask placementRaycastMask = ~0;
    [SerializeField] private LayerMask placementBlockingMask = ~0;
    [SerializeField] private float maxPlaceDistance = 20f;
    [SerializeField] private float minStreetlightSpacing = 0.8f;
    [SerializeField] private float ghostYOffset = 0.02f;
    [SerializeField] private float requiredStreetlightDistance = 10f;
    [SerializeField] private string requiredItemId = "streetlight";
    [SerializeField] private int requiredItemAmount = 1;

    [Header("Ghost Visual")]
    public Material validGhostMaterial;
    public Material invalidGhostMaterial;

    private GameObject ghostInstance;
    private Renderer[] ghostRenderers;
    private Vector3 lastSnappedPosition;
    private bool hasValidPlacement;

    private readonly List<LineRenderer> ghostPipePool = new List<LineRenderer>();
    private int activeGhostPipeCount;
    private const int GhostPipeSegments = 16;
    private const float GhostPipeWidth = 0.15f;
    private const float GhostPipeAnchorY = 0.05f;
    private const float GhostPipeAnchorOffset = 0.3f;
    private const float GhostPipeTangentStrength = 0.4f;
    private const float GhostPipeSCurveOffset = 0.5f;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (map == null)
        {
            map = FindFirstObjectByType<Map>();
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        CreateGhost();
    }

    private void OnDisable()
    {
        if (ghostInstance != null)
        {
            ghostInstance.SetActive(false);
        }

        HideAllGhostPipes();
    }

    private void Update()
    {
        UpdateGhostPlacement();

        if (!hasValidPlacement)
        {
            return;
        }

        if (IsPrimaryPressedThisFrame())
        {
            PlaceStreetlight(lastSnappedPosition);
        }
    }

    private void CreateGhost()
    {
        if (ghostPrefab == null)
        {
            return;
        }

        ghostInstance = Instantiate(ghostPrefab, Vector3.zero, Quaternion.identity);
        ghostInstance.name = $"{ghostPrefab.name}_Ghost";

        foreach (var collider in ghostInstance.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        foreach (var rigidbody in ghostInstance.GetComponentsInChildren<Rigidbody>())
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        ghostRenderers = ghostInstance.GetComponentsInChildren<Renderer>(true);
        ghostInstance.SetActive(false);
    }

    private void UpdateGhostPlacement()
    {
        if (ghostInstance == null || playerCamera == null || map == null)
        {
            hasValidPlacement = false;
            if (ghostInstance != null)
            {
                ghostInstance.SetActive(false);
            }
            HideAllGhostPipes();
            return;
        }

        if (!TryGetPointedWorldPoint(out var hitPoint))
        {
            hasValidPlacement = false;
            ghostInstance.SetActive(false);
            HideAllGhostPipes();
            return;
        }

        var snappedPosition = SnapToMapGrid(hitPoint);
        lastSnappedPosition = snappedPosition;

        hasValidPlacement = IsPlacementValid(snappedPosition);

        if (!ghostInstance.activeSelf)
        {
            ghostInstance.SetActive(true);
        }

        ghostInstance.transform.position = snappedPosition + new Vector3(0f, ghostYOffset, 0f);
        ghostInstance.transform.rotation = Quaternion.identity;

        var ghostMat = hasValidPlacement ? validGhostMaterial : invalidGhostMaterial;
        ApplyGhostMaterial(ghostMat);
        UpdateGhostPipes(snappedPosition, ghostMat);
    }

    private void UpdateGhostPipes(Vector3 ghostPosition, Material ghostMaterial)
    {
        var ghostBase = ghostPosition + Vector3.up * GhostPipeAnchorY;
        var existingStreetlights = FindObjectsByType<Streetlight>(FindObjectsSortMode.None);

        activeGhostPipeCount = 0;

        for (var i = 0; i < existingStreetlights.Length; i++)
        {
            var sl = existingStreetlights[i];
            if (sl == null)
            {
                continue;
            }

            var distance = Vector3.Distance(ghostPosition, sl.transform.position);
            if (distance > requiredStreetlightDistance || distance < minStreetlightSpacing)
            {
                continue;
            }

            // Ghost start anchor: at the foot of the ghost lamppost
            var dir = sl.transform.position - ghostPosition;
            dir.y = 0f;
            var start = ghostBase + GetCardinalOffset(dir) * GhostPipeAnchorOffset;

            // End anchor: at the foot of the existing lamppost
            var anchorComp = sl.GetComponent<CableAnchor>();
            Vector3 end;
            if (anchorComp != null)
            {
                end = anchorComp.GetAnchorToward(ghostPosition).position;
            }
            else
            {
                end = sl.transform.position + Vector3.up * GhostPipeAnchorY;
            }

            var lr = GetOrCreateGhostPipe(activeGhostPipeCount);
            SetGhostPipeCurve(lr, start, end, ghostMaterial);
            lr.gameObject.SetActive(true);
            activeGhostPipeCount++;
        }

        // Also show ghost pipes to nearby beacons (using beacon's activationRadius)
        var existingBeacons = FindObjectsByType<Beacon>(FindObjectsSortMode.None);
        for (var i = 0; i < existingBeacons.Length; i++)
        {
            var beacon = existingBeacons[i];
            if (beacon == null) continue;

            var distance = Vector3.Distance(ghostPosition, beacon.transform.position);
            if (distance > beacon.activationRadius || distance < minStreetlightSpacing) continue;

            var dir = beacon.transform.position - ghostPosition;
            dir.y = 0f;
            var start = ghostBase + GetCardinalOffset(dir) * GhostPipeAnchorOffset;

            var anchorComp = beacon.GetComponent<CableAnchor>();
            Vector3 end;
            if (anchorComp != null)
            {
                end = anchorComp.GetAnchorToward(ghostPosition).position;
            }
            else
            {
                end = beacon.transform.position + Vector3.up * GhostPipeAnchorY;
            }

            var lr = GetOrCreateGhostPipe(activeGhostPipeCount);
            SetGhostPipeCurve(lr, start, end, ghostMaterial);
            lr.gameObject.SetActive(true);
            activeGhostPipeCount++;
        }

        // Hide unused ghost pipes
        for (var i = activeGhostPipeCount; i < ghostPipePool.Count; i++)
        {
            ghostPipePool[i].gameObject.SetActive(false);
        }
    }

    private Vector3 GetCardinalOffset(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return Vector3.forward;
        }

        var absX = Mathf.Abs(direction.x);
        var absZ = Mathf.Abs(direction.z);

        if (absZ >= absX)
        {
            return direction.z >= 0f ? Vector3.forward : Vector3.back;
        }

        return direction.x >= 0f ? Vector3.right : Vector3.left;
    }

    private LineRenderer GetOrCreateGhostPipe(int index)
    {
        if (index < ghostPipePool.Count)
        {
            return ghostPipePool[index];
        }

        var go = new GameObject($"GhostPipe_{index}");
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = GhostPipeSegments + 1;
        lr.startWidth = GhostPipeWidth;
        lr.endWidth = GhostPipeWidth;
        lr.useWorldSpace = true;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        ghostPipePool.Add(lr);
        return lr;
    }

    private void SetGhostPipeCurve(LineRenderer lr, Vector3 start, Vector3 end, Material material)
    {
        lr.positionCount = GhostPipeSegments + 1;
        lr.material = material;

        var dist = Vector3.Distance(start, end);
        var tangentLen = dist * GhostPipeTangentStrength;

        var dirXZ = end - start;
        dirXZ.y = 0f;
        if (dirXZ.sqrMagnitude > 0.001f)
        {
            dirXZ.Normalize();
        }
        else
        {
            dirXZ = Vector3.forward;
        }

        var perp = new Vector3(-dirXZ.z, 0f, dirXZ.x);
        var groundY = Mathf.Min(start.y, end.y);

        var p0 = start;
        var p1 = start + dirXZ * tangentLen + perp * GhostPipeSCurveOffset;
        p1.y = groundY;
        var p2 = end - dirXZ * tangentLen - perp * GhostPipeSCurveOffset;
        p2.y = groundY;
        var p3 = end;

        for (var i = 0; i <= GhostPipeSegments; i++)
        {
            var t = (float)i / GhostPipeSegments;
            var u = 1f - t;
            var position = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            lr.SetPosition(i, position);
        }
    }

    private void HideAllGhostPipes()
    {
        for (var i = 0; i < ghostPipePool.Count; i++)
        {
            if (ghostPipePool[i] != null)
            {
                ghostPipePool[i].gameObject.SetActive(false);
            }
        }

        activeGhostPipeCount = 0;
    }

    private bool TryGetPointedWorldPoint(out Vector3 point)
    {
        point = default;
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, maxPlaceDistance, placementRaycastMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        point = hit.point;
        return true;
    }

    private Vector3 SnapToMapGrid(Vector3 worldPosition)
    {
        var step = Mathf.Max(0.001f, map.tileSize * map.tileScale);
        var local = worldPosition - map.transform.position;
        var gridX = Mathf.RoundToInt(local.x / step);
        var gridZ = Mathf.RoundToInt(local.z / step);

        return map.transform.position + new Vector3(gridX * step, map.transform.position.y, gridZ * step);
    }

    private bool IsPlacementValid(Vector3 position)
    {
        var step = Mathf.Max(0.001f, map.tileSize * map.tileScale);
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt((position.x - map.transform.position.x) / step),
            Mathf.RoundToInt((position.z - map.transform.position.z) / step)
        );

        if (map.GetTile(gridPos).IsVoid)
        {
            return false;
        }

        if (!HasRequiredItem())
        {
            return false;
        }

        if (IsTooCloseToStreetlight(position))
        {
            return false;
        }

        return HasStreetlightInRange(position);
    }

    private bool IsTooCloseToStreetlight(Vector3 candidatePosition)
    {
        var existingStreetlights = FindObjectsByType<Streetlight>(FindObjectsSortMode.None);
        for (var i = 0; i < existingStreetlights.Length; i++)
        {
            var light = existingStreetlights[i];
            if (light == null)
            {
                continue;
            }

            var distance = Vector3.Distance(candidatePosition, light.transform.position);
            if (distance < minStreetlightSpacing)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasStreetlightInRange(Vector3 candidatePosition)
    {
        var existingStreetlights = FindObjectsByType<Streetlight>(FindObjectsSortMode.None);
        for (var i = 0; i < existingStreetlights.Length; i++)
        {
            var light = existingStreetlights[i];
            if (light == null)
            {
                continue;
            }

            var distance = Vector3.Distance(candidatePosition, light.transform.position);
            if (distance < requiredStreetlightDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void PlaceStreetlight(Vector3 position)
    {
        if (streetlightPrefab == null)
        {
            return;
        }

        if (!TryConsumeRequiredItem())
        {
            return;
        }

        var instance = Instantiate(streetlightPrefab, position, Quaternion.identity);
        if (instance.GetComponent<Streetlight>() == null)
        {
            instance.AddComponent<Streetlight>();
        }
    }

    private bool HasRequiredItem()
    {
        if (inventory == null)
        {
            return false;
        }

        return inventory.HasItem(requiredItemId, requiredItemAmount);
    }

    private bool TryConsumeRequiredItem()
    {
        if (inventory == null)
        {
            return false;
        }

        return inventory.TryConsumeItem(requiredItemId, requiredItemAmount);
    }

    private void ApplyGhostMaterial(Material material)
    {
        if (ghostRenderers == null || material == null)
        {
            return;
        }

        foreach (var renderer in ghostRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static bool IsPrimaryPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
