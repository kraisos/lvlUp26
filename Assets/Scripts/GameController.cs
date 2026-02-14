using UnityEngine;
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

    [Header("Resources")]
    public GameObject resourceCachePrefab;
    public int resourcesCount = 2;
    public int resourceMinDistanceTiles = 10;
    public int resourceMaxDistanceTiles = 20;

    // Runtime references
    private GameObject player;
    private Beacon beacon;
    private Map map;
    private bool gameOver = false;
    private Vector3 originPosition;

    [Header("Death")]
    [SerializeField] private bool restartOnPlayerDeath = true;
    [SerializeField] private float restartDelay = 1.5f;

    void Start()
    {
        Vector3 originPoint = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        originPosition = new Vector3(originPoint.x, 0f, originPoint.z);
        map = FindFirstObjectByType<Map>();

        if (playerPrefab != null && spawnPoint != null)
        {
            SpawnPlayer(spawnPoint.position);
        }

        SpawnBeacon();
        SpawnResources();
    }

    void Update()
    {
        if (gameOver) return;

        // Check win condition: player reached the beacon
        if (beacon != null && beacon.IsReached)
        {
            OnGameWon();
        }
    }

    // ─── Spawning ───

    public void SpawnPlayer(Vector3 position)
    {
        player = Instantiate(playerPrefab, position, Quaternion.identity);
        player.transform.localScale = Vector3.one * 0.75f;
        Debug.Log($"Spawned player at {position}");

        if (StoryAudioManager.Instance != null)
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.FirstSpawn);
    }

    void SpawnBeacon()
    {
        Vector3 beaconPos = GetRandomTilePosition(originPosition, beaconDistanceTiles);

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
            Vector3 cachePos = GetRandomTilePosition(originPosition, distanceTiles);

            GameObject cacheObj;
            if (resourceCachePrefab != null)
            {
                cacheObj = Instantiate(resourceCachePrefab, cachePos, Quaternion.identity);
            }
            else
            {
                cacheObj = CreatePlaceholderCache(cachePos);
            }

            Debug.Log($"Resource cache {i + 1} spawned at {cachePos} ({distanceTiles} tiles from player)");
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

    void OnGameWon()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("=== GAME WON — You reached the beacon! ===");
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
            StartCoroutine(RestartSceneAfterDelay());
        }
    }

    IEnumerator RestartSceneAfterDelay()
    {
        float delay = Mathf.Max(0f, restartDelay);
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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