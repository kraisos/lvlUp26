using UnityEngine;
using System.Collections.Generic;

public class TileInfoMap
{
    private Dictionary<Vector2Int, TileInfo> tileInfoMap = new Dictionary<Vector2Int, TileInfo>();

    public void Set(Vector2Int pos, TileInfo info)
    {
        tileInfoMap[pos] = info;
    }

    public TileInfo Get(Vector2Int pos)
    {
        if (tileInfoMap.TryGetValue(pos, out TileInfo info))
        {
            return info;
        }
        return TileInfo.VOID; // Default to VOID if not set
    }

    public void Remove(Vector2Int pos)
    {
        tileInfoMap.Remove(pos);
    }
}

public class Map : MonoBehaviour
{
    [Header("Grid Settings")]
    public float tileSize = 1.0f;
    public int maxGridSize = 50; // Maximum grid size for dynamic expansion

    [Header("Tile Settings")]
    public GameObject tilePrefab;
    public Material defaultTileMaterial;
    
    [System.Serializable]
    public class TileData
    {
        public TilePrefabType type;
        public GameObject prefab;
        [Range(0f, 1f)]
        public float weight = 1f;
    }
    public TileData[] tileDataList;

    private GameObject[,] tileGrid;
    private HashSet<Vector2Int> activeTiles = new HashSet<Vector2Int>();
    private TileInfoMap tileInfoMap = new TileInfoMap();
    private List<LightSource> lightSources = new List<LightSource>();
    private GameObject gridParent;
    private MapGenerator mapGenerator;

    void Start()
    {
        // Initialize grid for dynamic generation
        tileGrid = new GameObject[maxGridSize, maxGridSize];
        mapGenerator = new MapGenerator(this.tileInfoMap);

        // Create parent object for organization
        gridParent = new GameObject("TileGrid");
        gridParent.transform.parent = transform;
    }

    public void RegisterLightSource(LightSource lightSource)
    {
        if (!lightSources.Contains(lightSource))
        {
            lightSources.Add(lightSource);
            UpdateTilesAroundLightSources();
            Debug.Log($"Light source registered. Total: {lightSources.Count}");
        }
    }

    public void UnregisterLightSource(LightSource lightSource)
    {
        if (lightSources.Contains(lightSource))
        {
            lightSources.Remove(lightSource);
            UpdateTilesAroundLightSources();
            Debug.Log($"Light source unregistered. Total: {lightSources.Count}");
        }
    }

    public void OnLightSourceMoved(LightSource lightSource)
    {
        UpdateTilesAroundLightSources();
    }

    void UpdateTilesAroundLightSources()
    {
        HashSet<Vector2Int> newActiveTiles = new HashSet<Vector2Int>();

        // Calculate which tiles should be active based on light sources
        foreach (LightSource lightSource in lightSources)
        {
            Vector2Int lightPos = lightSource.GetGridPosition();
            int radius = lightSource.GetLightRadiusInTiles();

            for (int x = lightPos.x - radius; x <= lightPos.x + radius; x++)
            {
                for (int z = lightPos.y - radius; z <= lightPos.y + radius; z++)
                {
                    // Check if within circular radius
                    Vector2Int tilePos = new Vector2Int(x, z);
                    float distance = Vector2Int.Distance(lightPos, tilePos);

                    if (distance <= radius)
                    {
                        // Adjust for grid centering
                        Vector2Int adjustedPos = new Vector2Int(
                            x + maxGridSize / 2,
                            z + maxGridSize / 2
                        );

                        if (IsValidGridPosition(adjustedPos))
                        {
                            newActiveTiles.Add(adjustedPos);
                        }
                    }
                }
            }
        }

        // Deactivate tiles that are no longer needed
        HashSet<Vector2Int> tilesToDeactivate = new HashSet<Vector2Int>(activeTiles);
        tilesToDeactivate.ExceptWith(newActiveTiles);

        foreach (Vector2Int pos in tilesToDeactivate)
        {
            DeactivateTileAt(pos);
        }

        // Activate new tiles
        HashSet<Vector2Int> tilesToActivate = new HashSet<Vector2Int>(newActiveTiles);
        tilesToActivate.ExceptWith(activeTiles);

        foreach (Vector2Int pos in tilesToActivate)
        {
            CreateTileAt(pos);
        }

        activeTiles = newActiveTiles;
    }

    void CreateTileAt(Vector2Int pos)
    {
        if (!IsValidGridPosition(pos) || tileGrid[pos.x, pos.y] != null)
            return;

        // Calculate world position (adjusted for centering)
        Vector3 position = new Vector3(
            (pos.x - maxGridSize / 2) * tileSize,
            0,
            (pos.y - maxGridSize / 2) * tileSize
        );

        GameObject tile;

        if (tilePrefab != null)
        {
            tile = Instantiate(tilePrefab, position, Quaternion.identity);
        }
        else
        {
            throw new System.Exception("Tile prefab is not assigned in the Map script!");
        }

        tile.transform.parent = gridParent.transform;
        tile.name = $"Tile_{pos.x - maxGridSize / 2}_{pos.y - maxGridSize / 2}";

        TileComponent tileComponent = tile.GetComponent<TileComponent>();
        if (tileComponent != null)
        {
            tileComponent.gridX = pos.x - maxGridSize / 2;
            tileComponent.gridZ = pos.y - maxGridSize / 2;
        }

        tileGrid[pos.x, pos.y] = tile;
    }

    void DeactivateTileAt(Vector2Int pos)
    {
        if (IsValidGridPosition(pos) && tileGrid[pos.x, pos.y] != null)
        {
            DestroyImmediate(tileGrid[pos.x, pos.y]);
            tileGrid[pos.x, pos.y] = null;
            tileInfoMap.Remove(pos);
        }
    }

    bool IsValidGridPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < maxGridSize &&
               pos.y >= 0 && pos.y < maxGridSize;
    }

    public GameObject GetTile(Vector2Int pos)
    {
        // Adjust for grid centering
        Vector2Int adjusted = new Vector2Int(pos.x + maxGridSize / 2, pos.y + maxGridSize / 2);

        if (IsValidGridPosition(adjusted))
        {
            return tileGrid[adjusted.x, adjusted.y];
        }
        return null;
    }

    public Vector2Int? GetTileCoordinates(GameObject tile)
    {
        TileComponent tileComponent = tile.GetComponent<TileComponent>();
        if (tileComponent != null)
        {
            return new Vector2Int(tileComponent.gridX, tileComponent.gridZ);
        }
        return null;
    }
}
