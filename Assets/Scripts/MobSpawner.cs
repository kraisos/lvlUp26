using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

            MobAI prefabToSpawn = GetRandomMobPrefab();
            if (aliveMobs.Count < maxAliveMobs)
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

        Vector2 direction2D = Random.insideUnitCircle.normalized;
        if (direction2D == Vector2.zero)
        {
            direction2D = Vector2.right;
        }

        float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
        Vector3 offset = new Vector3(direction2D.x, 0f, direction2D.y) * distance;
        return new Vector3(player.position.x, transform.position.y, player.position.z) + offset;
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
