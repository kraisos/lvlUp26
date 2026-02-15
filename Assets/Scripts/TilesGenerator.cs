using UnityEngine;

public class TilesGenerator : MonoBehaviour
{
    [Header("Tile Generation Properties")]
    public float radius = 10.0f; // radius in world units

    private Map mapReference;
    private Vector2Int gridPosition;
    private Vector2Int lastGridPosition;

    void Start()
    {
        // Find the map reference
        mapReference = FindFirstObjectByType<Map>();

        // Initialize position tracking
        UpdateGridPosition();
        lastGridPosition = gridPosition;

        // Register with the map
        mapReference.RegisterLightSource(this);

        // Update tiles around this light source
        NotifyMapOfPositionChange();
    }

    void Update()
    {
        CheckForPositionChange();
    }

    void CheckForPositionChange()
    {
        UpdateGridPosition();

        // If position changed, notify the map
        if (gridPosition != lastGridPosition)
        {
            NotifyMapOfPositionChange();
            lastGridPosition = gridPosition;
        }
    }

    void UpdateGridPosition()
    {
        if (mapReference != null)
        {
            // Convert world position to grid coordinates
            // Tiles are spaced at tileSize * tileScale in world units
            float worldStep = mapReference.tileSize * mapReference.tileScale;
            Vector3 localPos = transform.position - mapReference.transform.position;
            gridPosition = new Vector2Int(
                Mathf.RoundToInt(localPos.x / worldStep),
                Mathf.RoundToInt(localPos.z / worldStep)
            );
        }
    }

    void NotifyMapOfPositionChange()
    {
        if (mapReference != null)
        {
            mapReference.OnLightSourceMoved(this);
        }
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public int GetLightRadiusInTiles()
    {
        if (mapReference != null)
        {
            float worldStep = mapReference.tileSize * mapReference.tileScale;
            return Mathf.CeilToInt(radius / worldStep);
        }
        return Mathf.CeilToInt(radius);
    }

    void OnDestroy()
    {
        // Unregister from map when destroyed
        if (mapReference != null)
        {
            mapReference.UnregisterLightSource(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize light radius in editor
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow with transparency
        Gizmos.DrawSphere(transform.position, radius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
