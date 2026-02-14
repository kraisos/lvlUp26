using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class StreetlightTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Map map;
    [SerializeField] private GameObject streetlightPrefab;
    public GameObject ghostPrefab;

    [Header("Placement")]
    [SerializeField] private LayerMask placementRaycastMask = ~0;
    [SerializeField] private LayerMask placementBlockingMask = ~0;
    [SerializeField] private float maxPlaceDistance = 20f;
    [SerializeField] private float minStreetlightSpacing = 0.8f;
    [SerializeField] private float ghostYOffset = 0.02f;
    [SerializeField] private float requiredStreetlightDistance = 10f;

    [Header("Ghost Visual")]
    public Material validGhostMaterial;
    public Material invalidGhostMaterial;

    private GameObject ghostInstance;
    private Renderer[] ghostRenderers;
    private Vector3 lastSnappedPosition;
    private bool hasValidPlacement;

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

        CreateGhost();
    }

    private void OnDisable()
    {
        if (ghostInstance != null)
        {
            ghostInstance.SetActive(false);
        }
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
            return;
        }

        if (!TryGetPointedWorldPoint(out var hitPoint))
        {
            hasValidPlacement = false;
            ghostInstance.SetActive(false);
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
        ApplyGhostMaterial(hasValidPlacement ? validGhostMaterial : invalidGhostMaterial);
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
        var gridPos = new Vector2Int(
            Mathf.RoundToInt((position.x - map.transform.position.x) / step),
            Mathf.RoundToInt((position.z - map.transform.position.z) / step)
        );

        if (map.GetTile(gridPos) == null)
        {
            return false;
        }
/*
        var checkPosition = position + Vector3.up * 0.5f;
        var overlaps = Physics.OverlapSphere(checkPosition, placementClearanceRadius, placementBlockingMask, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < overlaps.Length; i++)
        {
            var col = overlaps[i];
            if (col == null)
            {
                continue;
            }

            if (col.GetComponentInParent<TileComponent>() != null)
            {
                continue;
            }

            if (ghostInstance != null && col.transform.IsChildOf(ghostInstance.transform))
            {
                continue;
            }

            return false;
        }
*/
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

        var instance = Instantiate(streetlightPrefab, position, Quaternion.identity);
        if (instance.GetComponent<Streetlight>() == null)
        {
            instance.AddComponent<Streetlight>();
        }
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
