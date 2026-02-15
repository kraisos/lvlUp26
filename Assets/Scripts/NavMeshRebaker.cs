using UnityEngine;
using Unity.AI.Navigation;

/// <summary>
/// Manages runtime NavMesh baking for dynamically generated environments.
/// Attach this to the same GameObject that has the NavMeshSurface component.
///
/// Setup:
///   1. Add a NavMeshSurface component to this GameObject
///   2. Configure the surface: Use Geometry = Physics Colliders, include your ground/obstacle layers
///   3. Call RequestRebake() from your Map script whenever tiles are created/destroyed
///   4. The rebake is debounced — rapid calls are batched into a single rebuild
/// </summary>
[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshRebaker : MonoBehaviour
{
    [Header("Rebake Settings")]
    [Tooltip("Minimum time (seconds) between NavMesh rebakes to avoid rebuilding every frame")]
    public float rebakeDebounceTime = 0.5f;

    [Tooltip("Perform an initial bake after this many seconds (gives tiles time to spawn)")]
    public float initialBakeDelay = 1f;

    [Header("Debug")]
    public bool logRebakes = false;

    /// <summary>
    /// Singleton instance so any script can request a rebake easily.
    /// </summary>
    public static NavMeshRebaker Instance { get; private set; }

    private NavMeshSurface surface;
    private float timeSinceLastBake;
    private bool rebakeRequested;
    private bool isBaking;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple NavMeshRebaker instances found. Destroying duplicate.");
            Destroy(this);
            return;
        }
        Instance = this;

        surface = GetComponent<NavMeshSurface>();

        // Force Physics Colliders mode to avoid "mesh does not allow read access" errors
        if (surface != null)
        {
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        }
    }

    void Start()
    {
        // Schedule the initial bake after a short delay so the first batch of tiles exists
        // StartCoroutine(InitialBakeCoroutine());
    }

    void Update()
    {
        if (!rebakeRequested || isBaking) return;

        timeSinceLastBake += Time.deltaTime;
        if (timeSinceLastBake >= rebakeDebounceTime)
        {
            RebakeAsync();
        }
    }

    /// <summary>
    /// Call this whenever the environment changes (tiles created/destroyed, obstacles moved).
    /// The actual rebake is debounced so calling this many times per frame is fine.
    /// </summary>
    public static void RequestRebake()
    {
        if (Instance != null)
        {
            Instance.rebakeRequested = true;
        }
    }

    private System.Collections.IEnumerator InitialBakeCoroutine()
    {
        // Wait for tiles to spawn
        yield return new WaitForSeconds(initialBakeDelay);

        if (surface == null) yield break;

        // Build initial NavMeshData (synchronous but on a small initial set of tiles)
        // We need this once to create the NavMeshData asset that UpdateNavMesh can work with
        RebakeAsync();
        if (logRebakes)
        {
            Debug.Log("[NavMeshRebaker] Initial NavMesh bake completed.");
        }
    }

    private void RebakeAsync()
    {
        if (surface == null || surface.navMeshData == null) return;

        isBaking = true;
        rebakeRequested = false;
        timeSinceLastBake = 0f;

        // UpdateNavMesh runs the heavy bake on a background thread — does NOT freeze the game
        AsyncOperation op = surface.UpdateNavMesh(surface.navMeshData);
        op.completed += OnBakeCompleted;

        if (logRebakes)
        {
            Debug.Log("[NavMeshRebaker] Async NavMesh rebake started...");
        }
    }

    private void OnBakeCompleted(AsyncOperation op)
    {
        isBaking = false;

        if (logRebakes)
        {
            Debug.Log("[NavMeshRebaker] Async NavMesh rebake completed.");
        }

        // If another rebake was requested while we were baking, start the timer again
        if (rebakeRequested)
        {
            timeSinceLastBake = 0f;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
