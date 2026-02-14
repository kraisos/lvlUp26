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
    public float tileScale = 3f;
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
    private List<TilesGenerator> lightSources = new List<TilesGenerator>();
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

    public void RegisterLightSource(TilesGenerator lightSource)
    {
        if (!lightSources.Contains(lightSource))
        {
            lightSources.Add(lightSource);

            // Pre-seed the 3x3 tiles around the light source as Ground
            Vector2Int lightPos = lightSource.GetGridPosition();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    Vector2Int adjustedPos = new Vector2Int(
                        lightPos.x + dx + maxGridSize / 2,
                        lightPos.y + dz + maxGridSize / 2
                    );
                    if (IsValidGridPosition(adjustedPos) && tileInfoMap.Get(adjustedPos).IsVoid)
                    {
                        tileInfoMap.Set(adjustedPos, new TileInfo(TileType.Ground));
                    }
                }
            }

            UpdateTilesAroundLightSources();
            Debug.Log($"Light source registered. Total: {lightSources.Count}");
        }
    }

    public void UnregisterLightSource(TilesGenerator lightSource)
    {
        if (lightSources.Contains(lightSource))
        {
            lightSources.Remove(lightSource);
            UpdateTilesAroundLightSources();
            Debug.Log($"Light source unregistered. Total: {lightSources.Count}");
        }
    }

    public void OnLightSourceMoved(TilesGenerator lightSource)
    {
        UpdateTilesAroundLightSources();
    }

    void UpdateTilesAroundLightSources()
    {
        HashSet<Vector2Int> newActiveTiles = new HashSet<Vector2Int>();

        // Calculate which tiles should be active based on light sources
        foreach (TilesGenerator lightSource in lightSources)
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

        // Use pre-seeded tile info if available, otherwise generate
        TileInfo tileInfo = tileInfoMap.Get(pos);
        if (tileInfo.IsVoid)
        {
            tileInfo = mapGenerator.CreateTile(pos);
        }
        tileInfoMap.Set(pos, tileInfo);

        GameObject selectedPrefab = GetPrefabForTileInfo(tileInfo);
        if (selectedPrefab == null)
        {
            selectedPrefab = tilePrefab;
        }

        // Calculate world position (adjusted for centering and scale)
        Vector3 position = new Vector3(
            (pos.x - maxGridSize / 2) * tileSize * tileScale,
            0,
            (pos.y - maxGridSize / 2) * tileSize * tileScale
        );

        Quaternion rotation = Quaternion.Euler(0, GetTileRotation(tileInfo.tileType), 0);

        GameObject tile;

        if (selectedPrefab != null)
        {
            tile = Instantiate(selectedPrefab, position, rotation);
        }
        else
        {
            throw new System.Exception("No tile prefab resolved. Assign either tilePrefab or tileDataList prefabs in the Map script.");
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

    private GameObject GetPrefabForTileInfo(TileInfo tileInfo)
    {
        if (tileInfo == null || tileDataList == null)
            return null;

        TilePrefabType prefabType = ToPrefabType(tileInfo.tileType);

        for (int i = 0; i < tileDataList.Length; i++)
        {
            if (tileDataList[i] != null && tileDataList[i].type == prefabType)
                return tileDataList[i].prefab;
        }

        return null;
    }

    private TilePrefabType ToPrefabType(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Ground:
                return TilePrefabType.Ground;
            case TileType.Woods:
                return TilePrefabType.Woods;
            case TileType.Lake:
                return TilePrefabType.Lake;
            case TileType.DoorVertical:
            case TileType.DoorHorizontal:
                return TilePrefabType.Door;
            case TileType.WallVertical:
            case TileType.WallHorizontal:
                return TilePrefabType.WallStraight;
            case TileType.WallNW:
            case TileType.WallNE:
            case TileType.WallSE:
            case TileType.WallSW:
                return TilePrefabType.WallCorner;
            case TileType.WallTN:
            case TileType.WallTE:
            case TileType.WallTS:
            case TileType.WallTW:
                return TilePrefabType.WallT;
            case TileType.WallCross:
                return TilePrefabType.WallCross;
            case TileType.Void:
            default:
                return TilePrefabType.Ground;
        }
    }

    // Returns the Y-axis rotation in degrees for each TileType.
    // Each prefab group has a base orientation:
    //   WallStraight prefab: vertical (│) by default
    //   WallCorner prefab:   NW (┌) by default
    //   WallT prefab:        TN (┬) by default
    //   Door prefab:         vertical (║) by default
    private float GetTileRotation(TileType tileType)
    {
        switch (tileType)
        {
            // Straight walls
            case TileType.WallHorizontal:   return 0f;
            case TileType.WallVertical:     return 90f;

            // Corners (base: NW ┌)
            case TileType.WallSW:           return 0f;
            case TileType.WallNW:           return 90f;
            case TileType.WallNE:           return 180f;
            case TileType.WallSE:           return 270f;

            // T-junctions (base: TN ┬)
            case TileType.WallTS:           return 0f;
            case TileType.WallTW:           return 90f;
            case TileType.WallTN:           return 180f;
            case TileType.WallTE:           return 270f;

            // Doors
            case TileType.DoorHorizontal:   return 0f;
            case TileType.DoorVertical:     return 90f;

            // Cross, terrain, void — no rotation
            default:                        return 0f;
        }
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
