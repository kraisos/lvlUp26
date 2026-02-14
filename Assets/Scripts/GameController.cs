using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameController : MonoBehaviour
{
    [Header("Player Management")]
    public GameObject playerPrefab;
    public Transform spawnPoint;

    [Header("Beacon")]
    public GameObject beaconPrefab;
    public int beaconDistanceTiles = 30;
    public float beaconClearRadius = 5f;

    [FormerlySerializedAs("resourceCachePrefab")] [Header("Resources")]
    public GameObject resourcePrefab;
    public int resourcesCount = 2;
    public int resourceMinDistanceTiles = 10;
    public int resourceMaxDistanceTiles = 20;

    // Runtime references
    private GameObject player;
    private Beacon beacon;
    private Map map;
    private bool gameOver = false;
    private Vector3 originPosition;
    private Inventory playerInventory;
    private int lastStreetlightCount = 0;
    private int coppermineSpawnCount = 0;

    [Header("Death")]
    [SerializeField] private bool restartOnPlayerDeath = true;
    [SerializeField] private string deathSceneName = "GameOverScene";

    void Start()
    {
        map = FindFirstObjectByType<Map>();

        if (map == null)
        {
            Debug.LogError("GameController: No Map found in scene. Aborting startup.");
            enabled = false;
            return;
        }

        Vector3 originPoint = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        originPosition = new Vector3(originPoint.x, 0f, originPoint.z);

        SpawnPlayer(spawnPoint.position);
        SpawnBeacon();
        SpawnResources();
    }

    void Update()
    {
        if (gameOver) return;
    }

    // ─── Spawning ───

    public void SpawnPlayer(Vector3 position)
    {
        player = Instantiate(playerPrefab, position, Quaternion.identity);
        player.transform.localScale = Vector3.one * 0.75f;
        Debug.Log($"Spawned player at {position}");

        // Track streetlight pickups via inventory
        playerInventory = player.GetComponentInChildren<Inventory>();
        if (playerInventory != null)
        {
            lastStreetlightCount = 0;
            playerInventory.Changed += OnInventoryChanged;
        }

        if (StoryAudioManager.Instance != null)
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.FirstSpawn);
    }

    void SpawnBeacon()
    {
        Vector3 beaconPos = map.ReserveClearTile(originPosition, beaconDistanceTiles, beaconClearRadius);

        GameObject beaconObj;
        if (beaconPrefab != null)
        {
            beaconObj = Instantiate(beaconPrefab, beaconPos, Quaternion.identity);
        }
        else
        {
            beaconObj = CreatePlaceholderBeacon(beaconPos);
        }

        beacon = beaconObj.GetComponent<Beacon>();
        if (beacon == null)
        {
            beacon = beaconObj.AddComponent<Beacon>();
        }

        beacon.OnBeaconReached += () => OnGameWon();

        Debug.Log($"Beacon spawned at {beaconPos} ({beaconDistanceTiles} tiles from player)");
    }

    void SpawnResources()
    {
        for (int i = 0; i < resourcesCount; i++)
        {
            int distanceTiles = Random.Range(resourceMinDistanceTiles, resourceMaxDistanceTiles + 1);
            Vector3 cachePos = map.ReserveClearTile(originPosition, distanceTiles);

            if (resourcePrefab != null)
            {
                Instantiate(resourcePrefab, cachePos, Quaternion.identity);
                Debug.Log($"Resource cache {i + 1} spawned at {cachePos} ({distanceTiles} tiles from player)");
            }
            else
            {
                Debug.LogWarning("No resource prefab found");
            }

        }
    }

    // ─── Positioning ───

    /// <summary>
    /// Returns a world position at the given tile distance from origin in a random direction.
    /// Uses the Map's tileSize and tileScale to convert tile units to world units.
    /// </summary>
    Vector3 GetRandomTilePosition(Vector3 origin, int tileDistance)
    {
        float worldStep = 3f; // default: tileSize(1) * tileScale(3)
        if (map != null)
        {
            worldStep = map.tileSize * map.tileScale;
        }

        float angle = Random.Range(0f, 360f);
        float worldDistance = tileDistance * worldStep;

        float x = origin.x + Mathf.Cos(angle * Mathf.Deg2Rad) * worldDistance;
        float z = origin.z + Mathf.Sin(angle * Mathf.Deg2Rad) * worldDistance;

        return new Vector3(x, origin.y, z);
    }

    // ─── Events ───

    void OnInventoryChanged()
    {
        if (playerInventory == null || player == null) return;

        // Count current streetlight items
        int currentCount = 0;
        foreach (var stack in playerInventory.Items)
        {
            if (stack.itemId == "streetlight")
            {
                currentCount = stack.quantity;
                break;
            }
        }

        // If the player gained streetlights, spawn a coppermine for each new one
        if (currentCount > lastStreetlightCount)
        {
            int gained = currentCount - lastStreetlightCount;
            for (int i = 0; i < gained; i++)
            {
                SpawnCopperBehindPlayer();
            }
        }

        lastStreetlightCount = currentCount;
    }

    void SpawnCopperBehindPlayer()
    {
        coppermineSpawnCount++;
        int distance = Random.Range(resourceMinDistanceTiles + coppermineSpawnCount, resourceMaxDistanceTiles + coppermineSpawnCount + 1);


        Vector3 playerPos = new Vector3(player.transform.position.x, 0f, player.transform.position.z);
        Vector3 pos = map.ReserveClearTileBehind(playerPos, player.transform.forward, distance);

        if (resourcePrefab != null)
        {
            Instantiate(resourcePrefab, pos, Quaternion.identity);
        }

        Debug.Log($"Coppermine #{coppermineSpawnCount} spawned at {pos} ({distance} tiles behind player)");
    }

    void OnGameWon()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("=== GAME WON — Streetlight placed near the beacon! ===");
    }

    public void OnPlayerCaught(Transform caughtBy = null)
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log($"=== GAME OVER — Player was caught by {(caughtBy != null ? caughtBy.name : "an enemy")} ===");

        if (StoryAudioManager.Instance != null)
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.FirstDeath);

        if (player != null)
        {
            Destroy(player);
            player = null;
        }

        if (restartOnPlayerDeath)
        {
            ShowGameOverScene();
        }
    }

    void ShowGameOverScene()
    {
        SceneManager.LoadScene(deathSceneName);
    }

    // ─── Placeholder visuals (used when no prefab is assigned) ───

    GameObject CreatePlaceholderBeacon(Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = "Beacon";
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(1.5f, 5f, 1.5f);

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(0.2f, 0.8f, 1f);
        }

        // Remove the default non-trigger collider so we rely on the Beacon script's trigger
        Collider col = obj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        return obj;
    }

    GameObject CreatePlaceholderCache(Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "ResourceCache";
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(1f, 1f, 1f);

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = new Color(1f, 0.8f, 0.2f);
        }

        // Remove the default non-trigger collider so we rely on the ResourceCache script's trigger
        Collider col = obj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        return obj;
    }
}