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

    private readonly HashSet<Streetlight> streetlights = new HashSet<Streetlight>();
    private readonly Dictionary<long, PipeConnection> activePipes = new Dictionary<long, PipeConnection>();
    private Streetlight energySource;
    private bool isDirty;
    private float particleTimer;

    private struct PipeConnection
    {
        public GasPipeRenderer renderer;
        public Streetlight lightA;
        public Streetlight lightB;
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

    public void Register(Streetlight streetlight)
    {
        if (streetlight == null)
        {
            return;
        }

        if (streetlights.Add(streetlight))
        {
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
        if (streetlight == null)
        {
            return;
        }

        if (streetlights.Remove(streetlight))
        {
            if (energySource == streetlight)
            {
                energySource = null;
                // Promote the next one if any
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

    private void UpdateParticles()
    {
        if (energySource == null || activePipes.Count == 0)
        {
            return;
        }

        particleTimer += Time.deltaTime;
        if (particleTimer < ParticleSpawnInterval)
        {
            return;
        }

        particleTimer = 0f;

        // BFS from energy source along connected pipes
        var visited = new HashSet<int>();
        var queue = new Queue<Streetlight>();
        visited.Add(energySource.GetInstanceID());
        queue.Enqueue(energySource);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var kvp in activePipes)
            {
                var conn = kvp.Value;
                if (conn.renderer == null)
                {
                    continue;
                }

                Streetlight neighbor = null;
                bool isA = false;

                if (conn.lightA == current && !visited.Contains(conn.lightB.GetInstanceID()))
                {
                    neighbor = conn.lightB;
                    isA = true;
                }
                else if (conn.lightB == current && !visited.Contains(conn.lightA.GetInstanceID()))
                {
                    neighbor = conn.lightA;
                    isA = false;
                }

                if (neighbor == null)
                {
                    continue;
                }

                visited.Add(neighbor.GetInstanceID());
                queue.Enqueue(neighbor);

                // Spawn particle along this pipe (from current toward neighbor)
                var path = isA ? conn.renderer.GetPath() : conn.renderer.GetReversedPath();
                if (path != null && path.Length > 1)
                {
                    SpawnParticle(path);
                }
            }
        }
    }

    private void SpawnParticle(Vector3[] path)
    {
        var go = new GameObject("EnergyParticle");
        var particle = go.AddComponent<PipeEnergyParticle>();
        particle.Init(path, ParticleSpeed, new Color(1f, 0.7f, 0.2f, 1f), 0.25f);
    }

    private void RebuildConnections()
    {
        var validLights = new List<Streetlight>();
        foreach (var sl in streetlights)
        {
            if (sl != null)
            {
                validLights.Add(sl);
            }
        }

        var desiredPairs = new HashSet<long>();

        for (var i = 0; i < validLights.Count; i++)
        {
            for (var j = i + 1; j < validLights.Count; j++)
            {
                var a = validLights[i];
                var b = validLights[j];
                var distance = Vector3.Distance(a.transform.position, b.transform.position);

                if (distance <= maxConnectionDistance)
                {
                    var key = MakePairKey(a.GetInstanceID(), b.GetInstanceID());
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
        for (var i = 0; i < validLights.Count; i++)
        {
            for (var j = i + 1; j < validLights.Count; j++)
            {
                var a = validLights[i];
                var b = validLights[j];
                var key = MakePairKey(a.GetInstanceID(), b.GetInstanceID());

                if (!desiredPairs.Contains(key))
                {
                    continue;
                }

                if (activePipes.ContainsKey(key))
                {
                    var existing = activePipes[key];
                    UpdatePipeEndpoints(existing.renderer, a, b);
                    continue;
                }

                var pipeGO = new GameObject($"GasPipe_{a.GetInstanceID()}_{b.GetInstanceID()}");
                pipeGO.transform.SetParent(transform, false);

                var pipe = pipeGO.AddComponent<GasPipeRenderer>();
                pipe.Init(copperMaterial);
                UpdatePipeEndpoints(pipe, a, b);

                activePipes[key] = new PipeConnection
                {
                    renderer = pipe,
                    lightA = a,
                    lightB = b
                };
            }
        }
    }

    private void UpdatePipeEndpoints(GasPipeRenderer pipe, Streetlight a, Streetlight b)
    {
        var anchorA = a.GetComponent<CableAnchor>();
        var anchorB = b.GetComponent<CableAnchor>();

        Vector3 start;
        Vector3 end;

        if (anchorA != null && anchorB != null)
        {
            start = anchorA.GetAnchorToward(b.transform.position).position;
            end = anchorB.GetAnchorToward(a.transform.position).position;
        }
        else
        {
            start = a.transform.position + Vector3.up * 0.05f;
            end = b.transform.position + Vector3.up * 0.05f;
        }

        pipe.SetEndpoints(start, end);
    }

    private static long MakePairKey(int idA, int idB)
    {
        var lo = idA < idB ? idA : idB;
        var hi = idA < idB ? idB : idA;
        return ((long)lo << 32) | (uint)hi;
    }
}
