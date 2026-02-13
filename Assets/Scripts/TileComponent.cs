using UnityEngine;

public class TileComponent : MonoBehaviour
{
    [Header("Grid Position")]
    public int gridX;
    public int gridZ;
    
    [Header("Tile Properties")]
    public bool isWalkable = true;
    public bool isOccupied = false;
    
    private Renderer tileRenderer;
    private Color originalColor;
    
    void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.color;
        }
    }
    
    public void HighlightTile(Color highlightColor)
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = highlightColor;
        }
    }
    
    public void ResetTileColor()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = originalColor;
        }
    }
    
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        
        // Visual feedback for occupied tiles
        if (occupied)
        {
            HighlightTile(Color.red);
        }
        else
        {
            ResetTileColor();
        }
    }
    
    void OnMouseEnter()
    {
        if (!isOccupied)
        {
            HighlightTile(Color.yellow);
        }
    }
    
    void OnMouseExit()
    {
        if (!isOccupied)
        {
            ResetTileColor();
        }
    }
    
    void OnMouseDown()
    {
        Debug.Log($"Clicked on tile at position: ({gridX}, {gridZ})");
        
        // You can add tile click functionality here
        // For example: place objects, move characters, etc.
    }
}