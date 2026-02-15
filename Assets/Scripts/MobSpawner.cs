using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MobSpawner : MonoBehaviour
{
    [Header("What To Spawn")]
    [SerializeField] private List<MobAI> mobPrefabs = new List<MobAI>();

    [Header("Timing")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float firstSpawnDelay = 5f;

    [Header("Limits")]
    [SerializeField] private int maxAliveMobs = 10;

    [Header("Spawn Area")]
    [SerializeField] private float minDistanceFromPlayer = 30f;
    [SerializeField] private float maxDistanceFromPlayer = 50f;

    private readonly List<MobAI> aliveMobs = new List<MobAI>();

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        if (firstSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(firstSpawnDelay);
        }

        while (true)
        {
            CleanupDeadReferences();

            if (LightedZone.IsPlayerInAnyLightZone)
            {
                yield return new WaitForSeconds(spawnInterval);
                continue;
            }

            MobAI prefabToSpawn = GetRandomMobPrefab();
            if (prefabToSpawn != null && aliveMobs.Count < maxAliveMobs)
            {
                Vector3 spawnPosition = GetSpawnPosition();
                MobAI spawnedMob = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                aliveMobs.Add(spawnedMob);
                Debug.Log($"Spawned mob: {spawnedMob.name} at {spawnPosition}. Total alive mobs: {aliveMobs.Count}");
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Transform player = GetPlayerTransform();

        // Try several random directions to find a point on the NavMesh
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 direction2D = Random.insideUnitCircle.normalized;
            if (direction2D == Vector2.zero)
            {
                direction2D = Vector2.right;
            }

            float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
            Vector3 offset = new Vector3(direction2D.x, 0f, direction2D.y) * distance;
            Vector3 candidate = new Vector3(player.position.x, transform.position.y, player.position.z) + offset;

            // Snap to the closest point on the NavMesh
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // Fallback: return raw position (MobAI will try to warp on its own)
        Vector2 fallbackDir = Random.insideUnitCircle.normalized;
        float fallbackDist = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
        Vector3 fallbackOffset = new Vector3(fallbackDir.x, 0f, fallbackDir.y) * fallbackDist;
        return new Vector3(player.position.x, transform.position.y, player.position.z) + fallbackOffset;
    }

    private MobAI GetRandomMobPrefab()
    {
        if (mobPrefabs == null || mobPrefabs.Count == 0)
        {
            return null;
        }

        List<MobAI> validPrefabs = new List<MobAI>();
        for (int i = 0; i < mobPrefabs.Count; i++)
        {
            if (mobPrefabs[i] != null)
            {
                validPrefabs.Add(mobPrefabs[i]);
            }
        }

        if (validPrefabs.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, validPrefabs.Count);
        return validPrefabs[index];
    }

    private Transform GetPlayerTransform()
    {
        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        return taggedPlayer.transform;
    }

    private void CleanupDeadReferences()
    {
        for (int i = aliveMobs.Count - 1; i >= 0; i--)
        {
            if (aliveMobs[i] == null)
            {
                aliveMobs.RemoveAt(i);
            }
        }
    }
}
