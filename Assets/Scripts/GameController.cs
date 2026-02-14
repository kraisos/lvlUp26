using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Player Management")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    private Map mapReference;

    void Start()
    {
        mapReference = FindFirstObjectByType<Map>();

        // Automatically spawn a player at the start for testing
        if (playerPrefab != null && spawnPoints.Length > 0)
        {
            SpawnPlayer(spawnPoints[0].position);
        }
    }

    void Update()
    {
    }


    public void SpawnPlayer(Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("No player prefab assigned!");
            return;
        }

        GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);

        // Get or create LightSource as child
        LightSource lightSource = newPlayer.GetComponentInChildren<LightSource>();
        if (lightSource == null)
        {
            GameObject lightObject = new GameObject("Light Source");
            lightObject.transform.SetParent(newPlayer.transform);
            lightObject.transform.localPosition = Vector3.up * 0.5f;
            lightSource = lightObject.AddComponent<LightSource>();
        }

        Debug.Log($"Spawned player with light at {position}");
    }
}