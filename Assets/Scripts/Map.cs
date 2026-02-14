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
    [Range(0f, 5f)]
    public float treePositionJitter = 0.2f;

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

        ApplyTreeRandomRotation(tile, tileInfo);
    }

    private void ApplyTreeRandomRotation(GameObject tile, TileInfo tileInfo)
    {
        if (tileInfo == null || tileInfo.tileType != TileType.Woods || tile == null)
            return;

        Transform modelTransform = FindChildByNameContains(tile.transform, "tree");
        if (modelTransform == null)
            return;

        Vector3 localPosition = modelTransform.localPosition;
        localPosition.x += Random.Range(-treePositionJitter, treePositionJitter);
        localPosition.z += Random.Range(-treePositionJitter, treePositionJitter);
        modelTransform.localPosition = localPosition;

        Vector3 localEuler = modelTransform.localEulerAngles;
        localEuler.y = Random.Range(0f, 360f);
        modelTransform.localEulerAngles = localEuler;
    }

    private Transform FindChildByNameContains(Transform root, string namePartLower)
    {
        if (root == null || string.IsNullOrEmpty(namePartLower))
            return null;

        if (root.name.ToLowerInvariant().Contains(namePartLower))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByNameContains(root.GetChild(i), namePartLower);
            if (found != null)
                return found;
        }

        return null;
    }

    private GameObject GetPrefabForTileInfo(TileInfo tileInfo)
    {
        if (tileInfo == null || tileDataList == null)
            return null;

        TilePrefabType prefabType = ToPrefabType(tileInfo.tileType);

        List<GameObject> matchingPrefabs = new List<GameObject>();
        for (int i = 0; i < tileDataList.Length; i++)
        {
            if (tileDataList[i] != null && tileDataList[i].type == prefabType)
            matchingPrefabs.Add(tileDataList[i].prefab);
        }
        
        if (matchingPrefabs.Count > 0)
            return matchingPrefabs[Random.Range(0, matchingPrefabs.Count)];

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
    //   WallStraight prefab: horizontal (─) by default
    //   WallCorner prefab:   SW (└) by default
    //   WallT prefab:        TS (┴) by default
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

    /// <summary>
    /// Finds a tile at the given distance (in tiles) from originWorld where the
    /// circular area of the given radius (in world units) is entirely void, seeds
    /// all tiles within that radius with Ground, and returns the world-space
    /// position of the centre tile.
    /// </summary>
    /// <param name="clearRadiusWorld">Radius in world units of the area to clear.
    /// Defaults to one tile step (i.e. a 3×3 area).</param>
    public Vector3 ReserveClearTile(Vector3 originWorld, int tileDistance, float clearRadiusWorld = -1f)
    {
        float worldStep = tileSize * tileScale;

        // Default clear radius: 1 tile step → covers the 3×3 neighbourhood
        if (clearRadiusWorld < 0f)
            clearRadiusWorld = worldStep;

        int clearRadiusTiles = Mathf.CeilToInt(clearRadiusWorld / worldStep);

        // Convert the world-space origin to a grid coordinate (un-adjusted, centred)
        Vector2Int originGrid = new Vector2Int(
            Mathf.RoundToInt(originWorld.x / worldStep),
            Mathf.RoundToInt(originWorld.z / worldStep)
        );

        const int maxAttempts = 36;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f);
            int gx = originGrid.x + Mathf.RoundToInt(Mathf.Cos(angle * Mathf.Deg2Rad) * tileDistance);
            int gz = originGrid.y + Mathf.RoundToInt(Mathf.Sin(angle * Mathf.Deg2Rad) * tileDistance);

            Vector2Int candidate = new Vector2Int(gx, gz);

            if (IsAreaClear(candidate, clearRadiusTiles))
            {
                SeedGroundArea(candidate, clearRadiusTiles);
                return GridToWorld(candidate, originWorld.y);
            }
        }

        // Fallback: scan a ring at the exact distance and pick the first clear spot
        for (int angle = 0; angle < 360; angle += 5)
        {
            int gx = originGrid.x + Mathf.RoundToInt(Mathf.Cos(angle * Mathf.Deg2Rad) * tileDistance);
            int gz = originGrid.y + Mathf.RoundToInt(Mathf.Sin(angle * Mathf.Deg2Rad) * tileDistance);

            Vector2Int candidate = new Vector2Int(gx, gz);

            if (IsAreaClear(candidate, clearRadiusTiles))
            {
                SeedGroundArea(candidate, clearRadiusTiles);
                return GridToWorld(candidate, originWorld.y);
            }
        }

        // Last resort: force-clear even if not void
        float fallbackAngle = Random.Range(0f, 360f);
        int fx = originGrid.x + Mathf.RoundToInt(Mathf.Cos(fallbackAngle * Mathf.Deg2Rad) * tileDistance);
        int fz = originGrid.y + Mathf.RoundToInt(Mathf.Sin(fallbackAngle * Mathf.Deg2Rad) * tileDistance);
        Vector2Int fallback = new Vector2Int(fx, fz);
        SeedGroundArea(fallback, clearRadiusTiles);
        Debug.LogWarning($"ReserveClearTile: no fully clear area (r={clearRadiusTiles}) found at distance {tileDistance}, force-cleared at {fallback}");
        return GridToWorld(fallback, originWorld.y);
    }

    /// <summary>
    /// Returns true when every tile within the given tile radius (circular) of
    /// gridPos is void and within valid grid bounds.
    /// </summary>
    private bool IsAreaClear(Vector2Int gridPos, int radiusTiles)
    {
        for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
        {
            for (int dz = -radiusTiles; dz <= radiusTiles; dz++)
            {
                if (dx * dx + dz * dz > radiusTiles * radiusTiles)
                    continue;

                Vector2Int adjusted = new Vector2Int(
                    gridPos.x + dx + maxGridSize / 2,
                    gridPos.y + dz + maxGridSize / 2
                );

                if (!IsValidGridPosition(adjusted))
                    return false;

                if (!tileInfoMap.Get(adjusted).IsVoid)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Seeds all tiles within the given tile radius (circular) of gridPos
    /// with Ground tiles in the tile info map.
    /// </summary>
    private void SeedGroundArea(Vector2Int gridPos, int radiusTiles)
    {
        for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
        {
            for (int dz = -radiusTiles; dz <= radiusTiles; dz++)
            {
                if (dx * dx + dz * dz > radiusTiles * radiusTiles)
                    continue;

                Vector2Int adjusted = new Vector2Int(
                    gridPos.x + dx + maxGridSize / 2,
                    gridPos.y + dz + maxGridSize / 2
                );

                if (IsValidGridPosition(adjusted))
                {
                    tileInfoMap.Set(adjusted, new TileInfo(TileType.Ground));
                }
            }
        }
    }

    /// <summary>
    /// Converts a centred grid coordinate to a world-space position.
    /// </summary>
    private Vector3 GridToWorld(Vector2Int gridPos, float y)
    {
        float worldStep = tileSize * tileScale;
        return new Vector3(gridPos.x * worldStep, y, gridPos.y * worldStep);
    }

    /// <summary>
    /// Reserves and clears a tile behind the given position (opposite to the given
    /// forward direction) at the specified tile distance, within a half-circle arc.
    /// The cleared area is a circle of clearRadiusWorld (world units).
    /// </summary>
    public Vector3 ReserveClearTileBehind(Vector3 origin, Vector3 forward, int tileDistance, float clearRadiusWorld = -1f)
    {
        float worldStep = tileSize * tileScale;

        if (clearRadiusWorld < 0f)
            clearRadiusWorld = worldStep;

        int clearRadiusTiles = Mathf.CeilToInt(clearRadiusWorld / worldStep);

        Vector2Int originGrid = new Vector2Int(
            Mathf.RoundToInt(origin.x / worldStep),
            Mathf.RoundToInt(origin.z / worldStep)
        );

        // "Behind" = opposite of the forward direction projected on XZ
        float behindAngle = Mathf.Atan2(-forward.z, -forward.x) * Mathf.Rad2Deg;

        const int maxAttempts = 36;
        const float halfArc = 90f; // search within a 180° arc behind the player

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = behindAngle + Random.Range(-halfArc, halfArc);
            int gx = originGrid.x + Mathf.RoundToInt(Mathf.Cos(angle * Mathf.Deg2Rad) * tileDistance);
            int gz = originGrid.y + Mathf.RoundToInt(Mathf.Sin(angle * Mathf.Deg2Rad) * tileDistance);

            Vector2Int candidate = new Vector2Int(gx, gz);

            if (IsAreaClear(candidate, clearRadiusTiles))
            {
                SeedGroundArea(candidate, clearRadiusTiles);
                return GridToWorld(candidate, origin.y);
            }
        }

        // Fallback: sweep the full behind arc in 5° steps
        for (float a = -halfArc; a <= halfArc; a += 5f)
        {
            float angle = behindAngle + a;
            int gx = originGrid.x + Mathf.RoundToInt(Mathf.Cos(angle * Mathf.Deg2Rad) * tileDistance);
            int gz = originGrid.y + Mathf.RoundToInt(Mathf.Sin(angle * Mathf.Deg2Rad) * tileDistance);

            Vector2Int candidate = new Vector2Int(gx, gz);

            if (IsAreaClear(candidate, clearRadiusTiles))
            {
                SeedGroundArea(candidate, clearRadiusTiles);
                return GridToWorld(candidate, origin.y);
            }
        }

        // Last resort: force-clear
        float fallbackAngle = behindAngle + Random.Range(-halfArc, halfArc);
        int fx = originGrid.x + Mathf.RoundToInt(Mathf.Cos(fallbackAngle * Mathf.Deg2Rad) * tileDistance);
        int fz = originGrid.y + Mathf.RoundToInt(Mathf.Sin(fallbackAngle * Mathf.Deg2Rad) * tileDistance);
        Vector2Int fallback = new Vector2Int(fx, fz);
        SeedGroundArea(fallback, clearRadiusTiles);
        Debug.LogWarning($"ReserveClearTileBehind: no clear area found, force-cleared at {fallback}");
        return GridToWorld(fallback, origin.y);
    }
}
