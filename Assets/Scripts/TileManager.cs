using UnityEngine;
using System.Collections.Generic;

public class TileManager : MonoBehaviour
{
    public Map map;

    [Header("Tile Interaction")]
    public LayerMask tileLayerMask = -1;
    public Camera playerCamera;

    private List<GameObject> highlightedTiles = new List<GameObject>();

    void Start()
    {
        if (map == null)
        {
            map = FindObjectOfType<Map>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        HandleTileSelection();
    }

    void HandleTileSelection()
    {
        if (playerCamera == null) return;

        // Cast ray from camera to mouse position
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, tileLayerMask))
        {
            TileComponent tileComponent = hit.collider.GetComponent<TileComponent>();
            if (tileComponent != null)
            {
                // Handle tile hover or selection here
                if (Input.GetMouseButtonDown(0))
                {
                    SelectTile(hit.collider.gameObject);
                }
            }
        }
    }

    public void SelectTile(GameObject tile)
    {
        TileComponent tileComponent = tile.GetComponent<TileComponent>();
        if (tileComponent != null)
        {
            Debug.Log($"Selected tile at: ({tileComponent.gridX}, {tileComponent.gridZ})");

            // Clear previous highlights
            ClearHighlightedTiles();

            // Highlight selected tile
            tileComponent.HighlightTile(Color.green);
            highlightedTiles.Add(tile);
        }
    }

    public void HighlightTilesInRange(Vector2Int center, int range)
    {
        ClearHighlightedTiles();

        for (int x = center.x - range; x <= center.x + range; x++)
        {
            for (int z = center.y - range; z <= center.y + range; z++)
            {
                GameObject tile = map.GetTile(x, z);
                if (tile != null)
                {
                    TileComponent tileComponent = tile.GetComponent<TileComponent>();
                    if (tileComponent != null && tileComponent.isWalkable)
                    {
                        tileComponent.HighlightTile(Color.cyan);
                        highlightedTiles.Add(tile);
                    }
                }
            }
        }
    }

    public void ClearHighlightedTiles()
    {
        foreach (GameObject tile in highlightedTiles)
        {
            if (tile != null)
            {
                TileComponent tileComponent = tile.GetComponent<TileComponent>();
                if (tileComponent != null)
                {
                    tileComponent.ResetTileColor();
                }
            }
        }
        highlightedTiles.Clear();
    }

    public List<GameObject> FindPath(Vector2Int start, Vector2Int end)
    {
        // Basic pathfinding placeholder - you can implement A* or other algorithms here
        List<GameObject> path = new List<GameObject>();

        // Simple direct line for demonstration
        Vector2Int current = start;
        while (current != end)
        {
            GameObject tile = map.GetTile(current.x, current.y);
            if (tile != null)
            {
                path.Add(tile);
            }

            // Move towards target (simplified)
            if (current.x < end.x) current.x++;
            else if (current.x > end.x) current.x--;

            if (current.y < end.y) current.y++;
            else if (current.y > end.y) current.y--;
        }

        // Add final tile
        GameObject finalTile = map.GetTile(end.x, end.y);
        if (finalTile != null)
        {
            path.Add(finalTile);
        }

        return path;
    }
}