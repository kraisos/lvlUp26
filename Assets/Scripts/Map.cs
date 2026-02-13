using UnityEngine;
using System.Collections.Generic;

public class Map : MonoBehaviour
{
    [Header("Grid Settings")]
    public float tileSize = 1.0f;
    public int maxGridSize = 50; // Maximum grid size for dynamic expansion

    [Header("Tile Settings")]
    public GameObject tilePrefab;
    public Material defaultTileMaterial;

    private GameObject[,] tileGrid;
    private HashSet<Vector2Int> activeTiles = new HashSet<Vector2Int>();
    private List<LightSource> lightSources = new List<LightSource>();
    private GameObject gridParent;

    void Start()
    {
        // Initialize grid for dynamic generation
        tileGrid = new GameObject[maxGridSize, maxGridSize];

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
            throw new System.Exception("Tile prefab is not assigned in the Map script!");
        }

        tile.transform.parent = gridParent.transform;
        tile.name = $"Tile_{x - maxGridSize / 2}_{z - maxGridSize / 2}";

        TileComponent tileComponent = tile.GetComponent<TileComponent>();
        if (tileComponent != null)
        {
            tileComponent.gridX = x - maxGridSize / 2;
            tileComponent.gridZ = z - maxGridSize / 2;
        }

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

    bool IsValidGridPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < maxGridSize &&
               pos.y >= 0 && pos.y < maxGridSize;
    }

    public GameObject GetTile(int x, int z)
    {
        // Adjust for grid centering
        x += maxGridSize / 2;
        z += maxGridSize / 2;

        if (x >= 0 && x < maxGridSize && z >= 0 && z < maxGridSize)
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
