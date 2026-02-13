using UnityEngine;
using System.Collections.Generic;

public class Map : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float tileSize = 1.0f;

    [Header("Tile Settings")]
    public GameObject tilePrefab;
    public Material defaultTileMaterial;
    public Material illuminatedTileMaterial;

    [Header("Dynamic Tile Generation")]
    public bool useDynamicGeneration = true;
    public int maxGridSize = 50; // Maximum grid size for dynamic expansion

    private GameObject[,] tileGrid;
    private HashSet<Vector2Int> activeTiles = new HashSet<Vector2Int>();
    private List<LightSource> lightSources = new List<LightSource>();
    private GameObject gridParent;

    void Start()
    {
        // Initialize grid with larger potential size for dynamic generation
        int actualGridSize = useDynamicGeneration ? maxGridSize : Mathf.Max(gridWidth, gridHeight);
        tileGrid = new GameObject[actualGridSize, actualGridSize];

        // Create parent object for organization
        gridParent = new GameObject("TileGrid");
        gridParent.transform.parent = transform;

        if (!useDynamicGeneration)
        {
            GenerateStaticGrid();
        }
    }

    void GenerateStaticGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                CreateTileAt(x, z);
                activeTiles.Add(new Vector2Int(x, z));
            }
        }

        // Center the grid
        gridParent.transform.position = new Vector3(-gridWidth * tileSize * 0.5f, 0, -gridHeight * tileSize * 0.5f);
    }

    public void RegisterLightSource(LightSource lightSource)
    {
        if (!lightSources.Contains(lightSource))
        {
            lightSources.Add(lightSource);
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
        if (!useDynamicGeneration) return;

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
            DeactivateTileAt(pos.x, pos.y);
        }

        // Activate new tiles
        HashSet<Vector2Int> tilesToActivate = new HashSet<Vector2Int>(newActiveTiles);
        tilesToActivate.ExceptWith(activeTiles);

        foreach (Vector2Int pos in tilesToActivate)
        {
            CreateTileAt(pos.x, pos.y);
        }

        activeTiles = newActiveTiles;
        UpdateTileVisuals();
    }

    void CreateTileAt(int x, int z)
    {
        if (!IsValidGridPosition(new Vector2Int(x, z)) || tileGrid[x, z] != null)
            return;

        // Calculate world position (adjusted for centering)
        Vector3 position = new Vector3(
            (x - maxGridSize / 2) * tileSize,
            0,
            (z - maxGridSize / 2) * tileSize
        );

        GameObject tile;

        if (tilePrefab != null)
        {
            tile = Instantiate(tilePrefab, position, Quaternion.identity);
        }
        else
        {
            tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.transform.position = position;
            tile.transform.localScale = new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f);

            if (defaultTileMaterial != null)
            {
                Renderer renderer = tile.GetComponent<Renderer>();
                renderer.material = defaultTileMaterial;
            }
        }

        tile.transform.parent = gridParent.transform;
        tile.name = $"Tile_{x - maxGridSize / 2}_{z - maxGridSize / 2}";

        TileComponent tileComponent = tile.AddComponent<TileComponent>();
        tileComponent.gridX = x - maxGridSize / 2;
        tileComponent.gridZ = z - maxGridSize / 2;

        tileGrid[x, z] = tile;
    }

    void DeactivateTileAt(int x, int z)
    {
        if (IsValidGridPosition(new Vector2Int(x, z)) && tileGrid[x, z] != null)
        {
            DestroyImmediate(tileGrid[x, z]);
            tileGrid[x, z] = null;
        }
    }

    void UpdateTileVisuals()
    {
        foreach (Vector2Int pos in activeTiles)
        {
            GameObject tile = tileGrid[pos.x, pos.y];
            if (tile != null)
            {
                TileComponent tileComponent = tile.GetComponent<TileComponent>();
                if (tileComponent != null)
                {
                    bool isIlluminated = IsTileIlluminated(tileComponent.gridX, tileComponent.gridZ);
                    UpdateTileAppearance(tile, isIlluminated);
                }
            }
        }
    }

    bool IsTileIlluminated(int tileX, int tileZ)
    {
        foreach (LightSource lightSource in lightSources)
        {
            Vector2Int lightPos = lightSource.GetGridPosition();
            float distance = Vector2Int.Distance(new Vector2Int(tileX, tileZ), lightPos);

            if (distance <= lightSource.GetLightRadiusInTiles())
            {
                return true;
            }
        }
        return false;
    }

    void UpdateTileAppearance(GameObject tile, bool isIlluminated)
    {
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (isIlluminated && illuminatedTileMaterial != null)
            {
                renderer.material = illuminatedTileMaterial;
            }
            else if (defaultTileMaterial != null)
            {
                renderer.material = defaultTileMaterial;
            }
        }
    }

    bool IsValidGridPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < (useDynamicGeneration ? maxGridSize : gridWidth) &&
               pos.y >= 0 && pos.y < (useDynamicGeneration ? maxGridSize : gridHeight);
    }

    public GameObject GetTile(int x, int z)
    {
        // Adjust for grid centering in dynamic mode
        if (useDynamicGeneration)
        {
            x += maxGridSize / 2;
            z += maxGridSize / 2;
        }

        if (x >= 0 && x < (useDynamicGeneration ? maxGridSize : gridWidth) &&
            z >= 0 && z < (useDynamicGeneration ? maxGridSize : gridHeight))
        {
            return tileGrid[x, z];
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
