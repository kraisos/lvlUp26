using UnityEngine;
using System.Collections.Generic;

public class GasPipeNetwork : MonoBehaviour
{
    public static GasPipeNetwork Instance { get; private set; }

    [Header("Connection Settings")]
    [SerializeField] private float maxConnectionDistance = 10f;
    [SerializeField] private Material copperMaterial;

    [Header("Energy Particles")]
    private const float ParticleSpawnInterval = 0.8f;
    private const float ParticleSpeed = 0.5f;

    // All connectable nodes (streetlights + beacons) identified by instance ID
    private readonly HashSet<Streetlight> streetlights = new HashSet<Streetlight>();
    private readonly HashSet<Beacon> beacons = new HashSet<Beacon>();

    // Unified node list: every connectable Transform (streetlights and beacons)
    private readonly Dictionary<int, Transform> allNodes = new Dictionary<int, Transform>();
    private readonly Dictionary<int, CableAnchor> allAnchors = new Dictionary<int, CableAnchor>();
    private readonly HashSet<int> beaconIds = new HashSet<int>();

    private readonly Dictionary<long, PipeConnection> activePipes = new Dictionary<long, PipeConnection>();
    private Streetlight energySource;
    private bool isDirty;
    private float particleTimer;

    private struct PipeConnection
    {
        public GasPipeRenderer renderer;
        public int nodeIdA;
        public int nodeIdB;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (copperMaterial == null)
        {
            copperMaterial = new Material(Shader.Find("Standard"));
            copperMaterial.name = "CopperPipe_Auto";
            copperMaterial.color = new Color(0.72f, 0.45f, 0.20f);
            copperMaterial.SetFloat("_Metallic", 0.8f);
            copperMaterial.SetFloat("_Glossiness", 0.6f);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ─── Streetlight registration ───

    public void Register(Streetlight streetlight)
    {
        if (streetlight == null) return;

        if (streetlights.Add(streetlight))
        {
            int id = streetlight.GetInstanceID();
            allNodes[id] = streetlight.transform;
            allAnchors[id] = streetlight.GetComponent<CableAnchor>();

            // First streetlight registered becomes the energy source
            if (energySource == null)
            {
                energySource = streetlight;
                streetlight.MarkAsEnergySource();
            }

            isDirty = true;
        }
    }

    public void Unregister(Streetlight streetlight)
    {
        if (streetlight == null) return;

        if (streetlights.Remove(streetlight))
        {
            int id = streetlight.GetInstanceID();
            allNodes.Remove(id);
            allAnchors.Remove(id);

            if (energySource == streetlight)
            {
                energySource = null;
                foreach (var sl in streetlights)
                {
                    if (sl != null)
                    {
                        energySource = sl;
                        sl.MarkAsEnergySource();
                        break;
                    }
                }
            }

            isDirty = true;
        }
    }

    // ─── Beacon registration ───

    public void RegisterBeacon(Beacon beacon)
    {
        if (beacon == null) return;

        if (beacons.Add(beacon))
        {
            int id = beacon.GetInstanceID();
            allNodes[id] = beacon.transform;
            allAnchors[id] = beacon.GetComponent<CableAnchor>();
            beaconIds.Add(id);
            isDirty = true;
        }
    }

    public void UnregisterBeacon(Beacon beacon)
    {
        if (beacon == null) return;

        if (beacons.Remove(beacon))
        {
            int id = beacon.GetInstanceID();
            allNodes.Remove(id);
            allAnchors.Remove(id);
            beaconIds.Remove(id);
            isDirty = true;
        }
    }

    // ─── Update loop ───

    private void LateUpdate()
    {
        if (isDirty)
        {
            RebuildConnections();
            isDirty = false;
        }

        UpdateParticles();
    }

    public void MarkDirty()
    {
        isDirty = true;
    }

    // ─── Particle spawning via BFS from energy source ───

    private void UpdateParticles()
    {
        if (energySource == null || activePipes.Count == 0) return;

        particleTimer += Time.deltaTime;
        if (particleTimer < ParticleSpawnInterval) return;
        particleTimer = 0f;

        // BFS from energy source along connected pipes
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        int sourceId = energySource.GetInstanceID();
        visited.Add(sourceId);
        queue.Enqueue(sourceId);

        while (queue.Count > 0)
        {
            int currentId = queue.Dequeue();

            foreach (var kvp in activePipes)
            {
                var conn = kvp.Value;
                if (conn.renderer == null) continue;

                int neighborId = -1;
                bool fromA = false;

                if (conn.nodeIdA == currentId && !visited.Contains(conn.nodeIdB))
                {
                    neighborId = conn.nodeIdB;
                    fromA = true;
                }
                else if (conn.nodeIdB == currentId && !visited.Contains(conn.nodeIdA))
                {
                    neighborId = conn.nodeIdA;
                    fromA = false;
                }

                if (neighborId == -1) continue;

                visited.Add(neighborId);
                queue.Enqueue(neighborId);

                // Spawn particle along this pipe (from current toward neighbor)
                var path = fromA ? conn.renderer.GetPath() : conn.renderer.GetReversedPath();
                if (path != null && path.Length > 1)
                {
                    SpawnParticle(path);
                }

                // Check if this neighbor is a beacon — notify energy arrived
                NotifyBeaconIfReached(neighborId);
            }
        }
    }

    private void NotifyBeaconIfReached(int nodeId)
    {
        foreach (var beacon in beacons)
        {
            if (beacon != null && beacon.GetInstanceID() == nodeId && !beacon.IsReached)
            {
                beacon.NotifyEnergyArrived();
            }
        }
    }

    private void SpawnParticle(Vector3[] path)
    {
        var go = new GameObject("EnergyParticle");
        var particle = go.AddComponent<PipeEnergyParticle>();
        particle.Init(path, ParticleSpeed, new Color(1f, 0.7f, 0.2f, 1f), 0.25f);
    }

    // ─── Rebuild pipe connections between all nodes ───

    private void RebuildConnections()
    {
        // Collect all valid node IDs and their positions
        var nodeIds = new List<int>();
        foreach (var kvp in allNodes)
        {
            if (kvp.Value != null)
            {
                nodeIds.Add(kvp.Key);
            }
        }

        var desiredPairs = new HashSet<long>();

        for (var i = 0; i < nodeIds.Count; i++)
        {
            for (var j = i + 1; j < nodeIds.Count; j++)
            {
                int idA = nodeIds[i];
                int idB = nodeIds[j];
                var posA = allNodes[idA].position;
                var posB = allNodes[idB].position;
                var distance = Vector3.Distance(posA, posB);

                // Use the beacon's activationRadius when one node is a beacon
                float maxDist = GetConnectionDistance(idA, idB);

                if (distance <= maxDist)
                {
                    var key = MakePairKey(idA, idB);
                    desiredPairs.Add(key);
                }
            }
        }

        // Remove pipes that are no longer needed
        var keysToRemove = new List<long>();
        foreach (var kvp in activePipes)
        {
            if (!desiredPairs.Contains(kvp.Key))
            {
                if (kvp.Value.renderer != null)
                {
                    Destroy(kvp.Value.renderer.gameObject);
                }
                keysToRemove.Add(kvp.Key);
            }
        }

        for (var i = 0; i < keysToRemove.Count; i++)
        {
            activePipes.Remove(keysToRemove[i]);
        }

        // Create missing pipes
        for (var i = 0; i < nodeIds.Count; i++)
        {
            for (var j = i + 1; j < nodeIds.Count; j++)
            {
                int idA = nodeIds[i];
                int idB = nodeIds[j];
                var key = MakePairKey(idA, idB);

                if (!desiredPairs.Contains(key)) continue;

                if (activePipes.ContainsKey(key))
                {
                    var existing = activePipes[key];
                    UpdatePipeEndpoints(existing.renderer, idA, idB);
                    continue;
                }

                var pipeGO = new GameObject($"GasPipe_{idA}_{idB}");
                pipeGO.transform.SetParent(transform, false);

                var pipe = pipeGO.AddComponent<GasPipeRenderer>();
                pipe.Init(copperMaterial);
                UpdatePipeEndpoints(pipe, idA, idB);

                activePipes[key] = new PipeConnection
                {
                    renderer = pipe,
                    nodeIdA = idA,
                    nodeIdB = idB
                };
            }
        }
    }

    private void UpdatePipeEndpoints(GasPipeRenderer pipe, int idA, int idB)
    {
        var transformA = allNodes.ContainsKey(idA) ? allNodes[idA] : null;
        var transformB = allNodes.ContainsKey(idB) ? allNodes[idB] : null;

        if (transformA == null || transformB == null) return;

        var anchorA = allAnchors.ContainsKey(idA) ? allAnchors[idA] : null;
        var anchorB = allAnchors.ContainsKey(idB) ? allAnchors[idB] : null;

        Vector3 start;
        Vector3 end;

        if (anchorA != null && anchorB != null)
        {
            start = anchorA.GetAnchorToward(transformB.position).position;
            end = anchorB.GetAnchorToward(transformA.position).position;
        }
        else
        {
            start = transformA.position + Vector3.up * 0.05f;
            end = transformB.position + Vector3.up * 0.05f;
        }

        pipe.SetEndpoints(start, end);
    }

    /// <summary>
    /// Returns the max connection distance for a pair of nodes.
    /// If one of the nodes is a Beacon, uses the beacon's activationRadius.
    /// Otherwise uses the default maxConnectionDistance.
    /// </summary>
    private float GetConnectionDistance(int idA, int idB)
    {
        // Check if either node is a beacon and use its larger activation radius
        foreach (var beacon in beacons)
        {
            if (beacon == null) continue;
            int bId = beacon.GetInstanceID();
            if (bId == idA || bId == idB)
            {
                return beacon.activationRadius;
            }
        }

        return maxConnectionDistance;
    }

    private static long MakePairKey(int idA, int idB)
    {
        var lo = idA < idB ? idA : idB;
        var hi = idA < idB ? idB : idA;
        return ((long)lo << 32) | (uint)hi;
    }
}
