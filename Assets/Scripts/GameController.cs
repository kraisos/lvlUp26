using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("Player Management")]
    public GameObject playerPrefab;
    public Transform spawnPoint;

    [Header("Beacon")]
    public GameObject beaconPrefab;
    public int beaconDistanceTiles = 30;
    public float beaconClearRadius = 5f;

    [Header("Streetlights")]
    public GameObject streetlightPrefab;

    [Header("Airship")]
    public GameObject airshipPrefab;
    public int airshipDistanceTiles = 15;

    [FormerlySerializedAs("resourceCachePrefab")] [Header("Resources")]
    public GameObject resourcePrefab;
    public int resourcesCount = 2;
    public int resourceMinDistanceTiles = 2;
    public int resourceMaxDistanceTiles = 4;

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

    [Header("Win")]
    [SerializeField] private string winSceneName = "WinScene";

    void Start()
    {
        map = FindFirstObjectByType<Map>();

        if (map == null)
        {
            Debug.LogError("GameController: No Map found in scene. Aborting startup.");
            enabled = false;
            return;
        }

        // Wait for the map to be ready before spawning
        if (map.IsReady) OnMapReady();
        else map.OnMapReady += OnMapReady;
    }

    void OnMapReady()
    {
        Debug.Log("GameController: OnMapReady");
        Vector3 originPoint = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        originPosition = new Vector3(originPoint.x, 0f, originPoint.z);

        SpawnPlayer(spawnPoint.position);
        SpwanInitialStreetlight();
        SpawnBeacon();
        SpawnResources();
        SpawnAirship();
    }

    void Update()
    {
        if (gameOver) return;
    }

    void OnDestroy()
    {
        if (map != null)
        {
            map.OnMapReady -= OnMapReady;
        }

        if (playerInventory != null)
        {
            playerInventory.Changed -= OnInventoryChanged;
        }
    }

    // ─── Spawning ───

    public void SpawnPlayer(Vector3 position)
    {
        player = Instantiate(playerPrefab, position, Quaternion.identity);
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

    void SpwanInitialStreetlight()
    {
        Vector3 streetlightPos = map.ReserveClearTile(originPosition, 2);
        if(streetlightPrefab != null)
        {
            Instantiate(streetlightPrefab, streetlightPos, Quaternion.identity);
            Debug.Log($"Initial streetlight spawned at {streetlightPos}");
        }
        else
        {
            Debug.LogWarning("No streetlight prefab found for initial spawn");
        }
    }

    void SpawnAirship()
    {
        if (airshipPrefab == null) return;

        Vector3 airshipPos = map.ReserveClearTile(originPosition, airshipDistanceTiles);
        airshipPos.y = 18f;

        Instantiate(airshipPrefab, airshipPos, Quaternion.identity);
        Debug.Log($"Airship spawned at {airshipPos} ({airshipDistanceTiles} tiles from origin)");
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
        // if (currentCount > lastStreetlightCount)
        // {
        //     int gained = currentCount - lastStreetlightCount;
        //     for (int i = 0; i < gained; i++)
        //     {
        //         SpawnCopperBehindPlayer();
        //     }
        // }

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
        SceneManager.LoadScene(winSceneName);
    }

    public void OnPlayerCaught(Transform caughtBy = null)
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log($"=== GAME OVER — Player was caught by {(caughtBy != null ? caughtBy.name : "an enemy")} ===");

        if (StoryAudioManager.Instance != null)
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.FirstDeath);

        if (restartOnPlayerDeath)
        {
            ShowDeathOverlay();
        }
    }

    void ShowDeathOverlay()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Full-screen canvas
        var canvasObj = new GameObject("DeathOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark vignette overlay
        var overlayObj = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
        overlayObj.transform.SetParent(canvasObj.transform, false);
        var overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImage = overlayObj.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.5f);

        // Death message
        var titleObj = new GameObject("DeathTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(1200f, 200f);

        var title = titleObj.GetComponent<TextMeshProUGUI>();
        title.text = "VOUS AVEZ SUCCOMB\u00C9 \u00C0 LA FOLIE";
        title.fontSize = 52f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.85f, 0.75f, 0.9f, 1f);

        // Restart button
        var buttonObj = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(canvasObj.transform, false);
        var btnRect = buttonObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.3f);
        btnRect.anchorMax = new Vector2(0.5f, 0.3f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(300f, 70f);

        var btnImage = buttonObj.GetComponent<Image>();
        btnImage.color = new Color(0.15f, 0.05f, 0.2f, 0.85f);

        // Button label
        var labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(buttonObj.transform, false);
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObj.GetComponent<TextMeshProUGUI>();
        label.text = "R\u00C9ESSAYER";
        label.fontSize = 32f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.85f, 0.75f, 0.9f, 1f);

        var button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
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