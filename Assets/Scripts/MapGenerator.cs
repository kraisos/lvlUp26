using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator
{
    private TileInfoMap tileInfoMap;
    private float wallSpawnChance = 0.02f;

    public MapGenerator(TileInfoMap tileInfoMap)
    {
        this.tileInfoMap = tileInfoMap;
    }

    public TileInfo CreateTile(Vector2Int position)
    {
        // First check the neighbors for any open walls towards that tile
        Neighbors neighbors = new Neighbors(position);
        TileInfo northTile = tileInfoMap.Get(neighbors.north);
        TileInfo eastTile = tileInfoMap.Get(neighbors.east);
        TileInfo southTile = tileInfoMap.Get(neighbors.south);
        TileInfo westTile = tileInfoMap.Get(neighbors.west);

        bool needsWallNorth = northTile.IsLinkSouth;
        bool needsWallEast = eastTile.IsLinkWest;
        bool needsWallSouth = southTile.IsLinkNorth;
        bool needsWallWest = westTile.IsLinkEast;

        if (needsWallNorth || needsWallEast || needsWallSouth || needsWallWest)
        {
            // Need to place a wall tile that connects to existing walls
            List<TileTypeInfo> validTypes = TileTypeInfo.WallTypes.Values.Where(w =>
                // Must have walls where neighbors require them
                (!needsWallNorth || w.LinkNorth) &&
                (!needsWallEast || w.LinkEast) &&
                (!needsWallSouth || w.LinkSouth) &&
                (!needsWallWest || w.LinkWest) &&
                // Walls can only extend into void tiles
                (!w.LinkNorth || needsWallNorth || northTile.IsVoid) &&
                (!w.LinkEast || needsWallEast || eastTile.IsVoid) &&
                (!w.LinkSouth || needsWallSouth || southTile.IsVoid) &&
                (!w.LinkWest || needsWallWest || westTile.IsVoid)
            ).ToList();

            if (validTypes.Count > 0) return new TileInfo(validTypes.PickWeighted().tileType);
        }

        // Check if at least one neighbor is void — walls can only start where they have room to expand
        bool hasVoidNeighbor = northTile.IsVoid || eastTile.IsVoid || southTile.IsVoid || westTile.IsVoid;

        if (hasVoidNeighbor && Random.value < wallSpawnChance)
        {
            // Try to place a new wall that only extends into void neighbors
            List<TileTypeInfo> validTypes = TileTypeInfo.WallTypes.Values.Where(w =>
                (!w.LinkNorth || northTile.IsVoid) &&
                (!w.LinkEast || eastTile.IsVoid) &&
                (!w.LinkSouth || southTile.IsVoid) &&
                (!w.LinkWest || westTile.IsVoid)
            ).ToList();

            if (validTypes.Count > 0)
            {
                return new TileInfo(validTypes.PickWeighted().tileType);
            }
        }

        // No wall constraints - generate terrain with clustering
        TileType terrainType = ChooseTerrainType(northTile, eastTile, southTile, westTile);
        return new TileInfo(terrainType);
    }

    private TileType ChooseTerrainType(TileInfo north, TileInfo east, TileInfo south, TileInfo west)
    {
        // Count neighboring terrain types for clustering
        int groundCount = 0, woodsCount = 0, lakeCount = 0;

        foreach (TileInfo neighbor in new[] { north, east, south, west })
        {
            switch (neighbor.tileType)
            {
                case TileType.Ground: groundCount++; break;
                case TileType.Woods: woodsCount++; break;
                case TileType.Lake: lakeCount++; break;
            }
        }

        // Base weights
        float groundWeight = 0.6f;
        float woodsWeight = 0.25f;
        float lakeWeight = 0.0f;

        // Apply clustering boost (neighbors of same type increase weight)
        float clusterBoost = 2.0f;
        groundWeight += groundCount * clusterBoost;
        woodsWeight += woodsCount * clusterBoost;
        lakeWeight += lakeCount * clusterBoost;

        float totalWeight = groundWeight + woodsWeight + lakeWeight;
        float roll = Random.value * totalWeight;

        if (roll < groundWeight)
            return TileType.Ground;
        else if (roll < groundWeight + woodsWeight)
            return TileType.Woods;
        else
            return TileType.Lake;
    }

    private class Neighbors
    {
        public readonly Vector2Int north;
        public readonly Vector2Int east;
        public readonly Vector2Int south;
        public readonly Vector2Int west;

        public Neighbors(Vector2Int pos)
        {
            north = pos + Vector2Int.up;
            east = pos + Vector2Int.right;
            south = pos + Vector2Int.down;
            west = pos + Vector2Int.left;
        }
    }
}
