using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator
{
    private TileInfoMap tileInfoMap;
    private float wallSpawnChance = 0.08f;

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

        bool needsWallNorth = northTile.IsOpenWallSouth;
        bool needsWallEast = eastTile.IsOpenWallWest;
        bool needsWallSouth = southTile.IsOpenWallNorth;
        bool needsWallWest = westTile.IsOpenWallEast;

        if (needsWallNorth || needsWallEast || needsWallSouth || needsWallWest)
        {
            // Need to place a wall tile that connects to existing walls
            List<TileType> validTypes = TileTypeInfo.wallTypes.Where(w =>
                // Must have walls where neighbors require them
                (!needsWallNorth || w.wallNorth) &&
                (!needsWallEast || w.wallEast) &&
                (!needsWallSouth || w.wallSouth) &&
                (!needsWallWest || w.wallWest) &&
                // Walls can only extend into void tiles
                (!w.wallNorth || needsWallNorth || northTile.IsVoid) &&
                (!w.wallEast || needsWallEast || eastTile.IsVoid) &&
                (!w.wallSouth || needsWallSouth || southTile.IsVoid) &&
                (!w.wallWest || needsWallWest || westTile.IsVoid)
            ).Select(w => w.tileType).ToList();

            if (validTypes.Count > 0)
            {
                TileType chosen = validTypes[Random.Range(0, validTypes.Count)];
                return new TileInfo(chosen);
            }
        }

        // Check if at least one neighbor is void — walls can only start where they have room to expand
        bool hasVoidNeighbor = northTile.IsVoid || eastTile.IsVoid || southTile.IsVoid || westTile.IsVoid;

        if (hasVoidNeighbor && Random.value < wallSpawnChance)
        {
            // Try to place a new wall that only extends into void neighbors
            List<TileType> validTypes = TileTypeInfo.wallTypes.Where(w =>
                (!w.wallNorth || northTile.IsVoid) &&
                (!w.wallEast || eastTile.IsVoid) &&
                (!w.wallSouth || southTile.IsVoid) &&
                (!w.wallWest || westTile.IsVoid)
            ).Select(w => w.tileType).ToList();

            if (validTypes.Count > 0)
            {
                TileType chosen = validTypes[Random.Range(0, validTypes.Count)];
                return new TileInfo(chosen);
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
        float lakeWeight = 0.15f;

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

public class WFCGenerator : MonoBehaviour
{
    public enum TileType
    {
        Floor,
        Wood,
        Lake
    }

    [System.Serializable]
    public class TileData
    {
        public TileType type;
        public GameObject prefab;
        [Range(0f, 1f)]
        public float weight = 1f;
    }

    public TileData[] tileDataList;
    public int gridWidth = 5;
    public int gridHeight = 5;
    public float tileSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Building Generation")]
    public GameObject wallPrefab;
    public GameObject buildingFloorPrefab;
    public int minBuildingWidth = 2;
    public int maxBuildingWidth = 4;
    public int minBuildingHeight = 3;
    public int maxBuildingHeight = 5;
    public int maxBuildingAttempts = 10;
    public int numberOfBuildings = 2;

    private GameObject gridContainer;
    private TileType[,] gridResult; // Store the result for building placement

    // Adjacency rules: which tiles can be next to each other
    // Key: TileType, Value: List of allowed neighbors
    private Dictionary<TileType, HashSet<TileType>> adjacencyRules;

    // Clustering boost: how much more likely a tile is to appear next to same type
    [Range(1f, 10f)]
    public float clusteringStrength = 4f;

    private void InitializeAdjacencyRules()
    {
        adjacencyRules = new Dictionary<TileType, HashSet<TileType>>();

        // Floor can be next to anything
        adjacencyRules[TileType.Floor] = new HashSet<TileType>
        {
            TileType.Floor,
            TileType.Wood,
            TileType.Lake
        };

        // Wood prefers to be next to other wood or floor (forests cluster together)
        // Lakes and Woods should NOT be directly adjacent (more natural separation)
        adjacencyRules[TileType.Wood] = new HashSet<TileType>
        {
            TileType.Floor,
            TileType.Wood
        };

        // Lake prefers to be next to other lakes or floor (lakes cluster together)
        // Lakes and Woods should NOT be directly adjacent
        adjacencyRules[TileType.Lake] = new HashSet<TileType>
        {
            TileType.Floor,
            TileType.Lake
        };
    }

    public void GenerateGrid()
    {
        // Clear previous grid
        if (gridContainer != null)
        {
            Destroy(gridContainer);
        }

        gridContainer = new GameObject("WFCGridContainer");
        InitializeAdjacencyRules();

        // Track which cells are used by buildings
        bool[,] usedByBuilding = new bool[gridWidth, gridHeight];
        bool[,] isNearBuilding = new bool[gridWidth, gridHeight]; // Buffer zone around buildings

        // STEP 1: Place buildings FIRST
        List<(int x, int z, int w, int h, int openingSide)> buildings = GenerateBuildings(usedByBuilding, isNearBuilding);

        // STEP 2: Generate WFC terrain around buildings
        List<TileType>[,] wave = new List<TileType>[gridWidth, gridHeight];
        gridResult = new TileType[gridWidth, gridHeight];
        bool[,] collapsed = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (usedByBuilding[x, z])
                {
                    // Building cells are already "collapsed" - they won't have terrain
                    collapsed[x, z] = true;
                    gridResult[x, z] = TileType.Floor;
                    wave[x, z] = new List<TileType> { TileType.Floor };
                }
                else if (isNearBuilding[x, z])
                {
                    // Near buildings: only Floor or Wood allowed (no lakes)
                    wave[x, z] = new List<TileType> { TileType.Floor, TileType.Wood };
                    collapsed[x, z] = false;
                }
                else
                {
                    // Normal cells: all options available
                    wave[x, z] = new List<TileType> { TileType.Floor, TileType.Wood, TileType.Lake };
                    collapsed[x, z] = false;
                }
            }
        }

        // WFC main loop
        int iterations = 0;
        int maxIterations = gridWidth * gridHeight * 10;

        while (iterations < maxIterations)
        {
            iterations++;

            // Find the cell with minimum entropy (least possibilities but not collapsed)
            int minEntropy = int.MaxValue;
            int minX = -1, minZ = -1;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (!collapsed[x, z] && wave[x, z].Count > 0 && wave[x, z].Count < minEntropy)
                    {
                        minEntropy = wave[x, z].Count;
                        minX = x;
                        minZ = z;
                    }
                }
            }

            // If no cell found, we're done
            if (minX == -1)
                break;

            // Collapse the cell with minimum entropy
            TileType chosen = ChooseWeightedRandom(wave[minX, minZ], wave, collapsed, minX, minZ);
            wave[minX, minZ] = new List<TileType> { chosen };
            gridResult[minX, minZ] = chosen;
            collapsed[minX, minZ] = true;

            // Propagate constraints to neighbors
            PropagateConstraints(wave, collapsed, minX, minZ);
        }

        // STEP 3: Instantiate the terrain (skip cells used by buildings)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (usedByBuilding[x, z])
                    continue; // Skip, building will be placed here

                Vector3 position = gridOrigin + new Vector3(x * tileSize, 0f, z * tileSize);
                TileType tileType = collapsed[x, z] ? gridResult[x, z] : TileType.Floor;

                GameObject prefab = GetPrefabForType(tileType);
                if (prefab != null)
                {
                    GameObject tile = Instantiate(prefab, position, Quaternion.identity);
                    tile.transform.SetParent(gridContainer.transform);
                }
            }
        }

        // STEP 4: Instantiate buildings
        foreach (var building in buildings)
        {
            InstantiateBuilding(building.x, building.z, building.w, building.h, building.openingSide);
        }
    }

    private TileType ChooseWeightedRandom(List<TileType> possibilities, List<TileType>[,] wave, bool[,] collapsed, int x, int z)
    {
        if (possibilities.Count == 0)
            return TileType.Floor;

        // Count neighbor types to boost clustering
        Dictionary<TileType, int> neighborCounts = new Dictionary<TileType, int>();
        foreach (TileType t in System.Enum.GetValues(typeof(TileType)))
        {
            neighborCounts[t] = 0;
        }

        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int nz = z + dz[i];

            if (nx >= 0 && nx < gridWidth && nz >= 0 && nz < gridHeight && collapsed[nx, nz])
            {
                TileType neighborType = wave[nx, nz][0];
                neighborCounts[neighborType]++;
            }
        }

        // Calculate weights with clustering boost
        float totalWeight = 0f;
        Dictionary<TileType, float> adjustedWeights = new Dictionary<TileType, float>();

        foreach (var type in possibilities)
        {
            float baseWeight = GetWeightForType(type);
            // Boost weight based on how many neighbors are the same type
            float clusterBoost = 1f + (neighborCounts[type] * clusteringStrength);
            float adjustedWeight = baseWeight * clusterBoost;
            adjustedWeights[type] = adjustedWeight;
            totalWeight += adjustedWeight;
        }

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var type in possibilities)
        {
            cumulative += adjustedWeights[type];
            if (random <= cumulative)
                return type;
        }

        return possibilities[0];
    }

    private float GetWeightForType(TileType type)
    {
        if (tileDataList == null) return 1f;

        foreach (var data in tileDataList)
        {
            if (data.type == type)
                return data.weight;
        }
        return 1f;
    }

    private GameObject GetPrefabForType(TileType type)
    {
        if (tileDataList == null) return null;

        foreach (var data in tileDataList)
        {
            if (data.type == type)
                return data.prefab;
        }
        return null;
    }

    private void PropagateConstraints(List<TileType>[,] wave, bool[,] collapsed, int startX, int startZ)
    {
        Queue<(int x, int z)> toPropagate = new Queue<(int, int)>();
        toPropagate.Enqueue((startX, startZ));

        while (toPropagate.Count > 0)
        {
            var (x, z) = toPropagate.Dequeue();
            TileType currentType = wave[x, z][0]; // Already collapsed to single value

            // Check all 4 neighbors
            int[] dx = { -1, 1, 0, 0 };
            int[] dz = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int nz = z + dz[i];

                // Skip out of bounds
                if (nx < 0 || nx >= gridWidth || nz < 0 || nz >= gridHeight)
                    continue;

                // Skip already collapsed
                if (collapsed[nx, nz])
                    continue;

                // Filter neighbor possibilities based on adjacency rules
                List<TileType> newPossibilities = new List<TileType>();
                foreach (var possibility in wave[nx, nz])
                {
                    // Check if this possibility is allowed next to current type
                    if (adjacencyRules[currentType].Contains(possibility) &&
                        adjacencyRules[possibility].Contains(currentType))
                    {
                        newPossibilities.Add(possibility);
                    }
                }

                // If possibilities changed, update and continue propagation
                if (newPossibilities.Count < wave[nx, nz].Count)
                {
                    wave[nx, nz] = newPossibilities;

                    // If reduced to one possibility, it's effectively collapsed
                    if (newPossibilities.Count == 1)
                    {
                        toPropagate.Enqueue((nx, nz));
                    }
                }
            }
        }
    }

    private List<(int x, int z, int w, int h, int openingSide)> GenerateBuildings(bool[,] usedByBuilding, bool[,] isNearBuilding)
    {
        List<(int x, int z, int w, int h, int openingSide)> buildings = new List<(int, int, int, int, int)>();

        for (int b = 0; b < numberOfBuildings; b++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxBuildingAttempts && !placed; attempt++)
            {
                int buildingW = Random.Range(minBuildingWidth, maxBuildingWidth + 1);
                int buildingH = Random.Range(minBuildingHeight, maxBuildingHeight + 1);

                // Random position (leave room for building + 1 tile buffer)
                int startX = Random.Range(1, gridWidth - buildingW - 1);
                int startZ = Random.Range(1, gridHeight - buildingH - 1);

                if (CanPlaceBuilding(startX, startZ, buildingW, buildingH, usedByBuilding))
                {
                    // Mark cells as used by building
                    for (int x = startX; x < startX + buildingW; x++)
                    {
                        for (int z = startZ; z < startZ + buildingH; z++)
                        {
                            usedByBuilding[x, z] = true;
                        }
                    }

                    // Mark buffer zone around building (no lakes allowed here)
                    for (int x = startX - 1; x <= startX + buildingW; x++)
                    {
                        for (int z = startZ - 1; z <= startZ + buildingH; z++)
                        {
                            if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
                            {
                                if (!usedByBuilding[x, z])
                                {
                                    isNearBuilding[x, z] = true;
                                }
                            }
                        }
                    }

                    // Random opening side: 0=left, 1=right, 2=bottom, 3=top (skip one side for no opening)
                    int openingSide = Random.Range(0, 4);

                    buildings.Add((startX, startZ, buildingW, buildingH, openingSide));
                    placed = true;
                }
            }
        }

        return buildings;
    }

    private bool CanPlaceBuilding(int startX, int startZ, int width, int height, bool[,] usedByBuilding)
    {
        // Check the building area AND a 1-tile buffer around it for other buildings
        for (int x = startX - 1; x <= startX + width; x++)
        {
            for (int z = startZ - 1; z <= startZ + height; z++)
            {
                // Check bounds
                if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight)
                    return false;

                // Check if already used by another building (including buffer to prevent buildings touching)
                if (usedByBuilding[x, z])
                    return false;
            }
        }

        return true;
    }

    private void InstantiateBuilding(int startX, int startZ, int width, int height, int openingSide)
    {
        GameObject buildingContainer = new GameObject($"Building_{startX}_{startZ}");
        buildingContainer.transform.SetParent(gridContainer.transform);

        // Determine which side has NO opening (the 4th side)
        // openingSide indicates which side is completely closed
        // The other 3 sides each have one opening

        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + height; z++)
            {
                Vector3 position = gridOrigin + new Vector3(x * tileSize, 0f, z * tileSize);

                // Place building floor
                if (buildingFloorPrefab != null)
                {
                    GameObject floor = Instantiate(buildingFloorPrefab, position, Quaternion.identity);
                    floor.transform.SetParent(buildingContainer.transform);
                }

                // Check if this is an edge cell and place walls
                bool isLeftEdge = (x == startX);
                bool isRightEdge = (x == startX + width - 1);
                bool isBottomEdge = (z == startZ);
                bool isTopEdge = (z == startZ + height - 1);

                // Calculate opening positions (middle of each side)
                int midX = startX + width / 2;
                int midZ = startZ + height / 2;

                // Left wall
                if (isLeftEdge && wallPrefab != null)
                {
                    // Opening on left side if openingSide != 0, at middle position
                    bool hasOpening = (openingSide != 0) && (z == midZ);
                    if (!hasOpening)
                    {
                        Vector3 wallPos = position + new Vector3(-tileSize * 0.4f, 0f, 0f);
                        GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.Euler(0, 90, 0));
                        wall.transform.SetParent(buildingContainer.transform);
                    }
                }

                // Right wall
                if (isRightEdge && wallPrefab != null)
                {
                    bool hasOpening = (openingSide != 1) && (z == midZ);
                    if (!hasOpening)
                    {
                        Vector3 wallPos = position + new Vector3(tileSize * 0.4f, 0f, 0f);
                        GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.Euler(0, 90, 0));
                        wall.transform.SetParent(buildingContainer.transform);
                    }
                }

                // Bottom wall
                if (isBottomEdge && wallPrefab != null)
                {
                    bool hasOpening = (openingSide != 2) && (x == midX);
                    if (!hasOpening)
                    {
                        Vector3 wallPos = position + new Vector3(0f, 0f, -tileSize * 0.4f);
                        GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity);
                        wall.transform.SetParent(buildingContainer.transform);
                    }
                }

                // Top wall
                if (isTopEdge && wallPrefab != null)
                {
                    bool hasOpening = (openingSide != 3) && (x == midX);
                    if (!hasOpening)
                    {
                        Vector3 wallPos = position + new Vector3(0f, 0f, tileSize * 0.4f);
                        GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity);
                        wall.transform.SetParent(buildingContainer.transform);
                    }
                }
            }
        }
    }
}

