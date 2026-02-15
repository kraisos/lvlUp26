using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

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

    [Tooltip("Extra padding (world units) around the tile bounds when collecting sources")]
    public float boundsPadding = 5f;

    [Header("Debug")]
    public bool logRebakes = false;

    /// <summary>
    /// Singleton instance so any script can request a rebake easily.
    /// </summary>
    public static NavMeshRebaker Instance { get; private set; }

    private NavMeshSurface surface;
    private NavMeshDataInstance navMeshDataInstance;
    private float timeSinceLastBake;
    private bool rebakeRequested;
    private bool isBaking;
    private bool initialized;

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

    /// <summary>
    /// Lazily creates the NavMeshData and registers it with the navigation system.
    /// </summary>
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        surface.navMeshData = new NavMeshData(surface.agentTypeID);
        navMeshDataInstance = NavMesh.AddNavMeshData(surface.navMeshData);

        if (logRebakes)
            Debug.Log("[NavMeshRebaker] NavMeshData created and registered.");
    }

    /// <summary>
    /// Computes a tight world-space bounding box around the NavMeshSurface's
    /// collected source objects so we only scan the area that actually has tiles.
    /// </summary>
    private Bounds CalculateSourceBounds(List<NavMeshBuildSource> sources)
    {
        if (sources.Count == 0)
            return new Bounds(transform.position, Vector3.one * 10f);

        // Start from the first source's position
        Vector3 min = sources[0].transform.GetColumn(3);
        Vector3 max = min;

        for (int i = 0; i < sources.Count; i++)
        {
            Vector3 pos = sources[i].transform.GetColumn(3);
            Vector3 halfSize = sources[i].size * 0.5f;

            min = Vector3.Min(min, pos - halfSize);
            max = Vector3.Max(max, pos + halfSize);
        }

        // Add padding
        min -= Vector3.one * boundsPadding;
        max += Vector3.one * boundsPadding;

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    private void RebakeAsync()
    {
        EnsureInitialized();

        isBaking = true;
        rebakeRequested = false;
        timeSinceLastBake = 0f;

        // Collect sources with a tight bounds (much cheaper than letting
        // NavMeshSurface.UpdateNavMesh scan everything in the scene)
        var sources = new List<NavMeshBuildSource>();
        var markups = new List<NavMeshBuildMarkup>();
        NavMeshBuildSettings buildSettings = surface.GetBuildSettings();

        // Collect only from the surface's configured layers & geometry mode
        // Use a large initial bounds for collection, then tighten it
        Bounds collectBounds = new Bounds(transform.position, Vector3.one * 10000f);
        NavMeshBuilder.CollectSources(
            collectBounds,
            surface.layerMask,
            surface.useGeometry,
            surface.defaultArea,
            markups,
            sources
        );

        // Now compute the actual tight bounds from the collected sources
        Bounds bakeBounds = CalculateSourceBounds(sources);

        // Kick off the truly async bake — only the triangulation runs on the main thread briefly
        AsyncOperation op = NavMeshBuilder.UpdateNavMeshDataAsync(
            surface.navMeshData,
            buildSettings,
            sources,
            bakeBounds
        );
        op.completed += OnBakeCompleted;

        if (logRebakes)
        {
            Debug.Log($"[NavMeshRebaker] Async rebake started — {sources.Count} sources, bounds: {bakeBounds}");
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
        if (navMeshDataInstance.valid)
            navMeshDataInstance.Remove();

        if (Instance == this)
            Instance = null;
    }
}
