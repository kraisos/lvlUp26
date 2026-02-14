using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Player Management")]
    public GameObject playerPrefab;
    public Transform spawnPoint;


    void Start()
    {
        // Automatically spawn a player at the start for testing
        if (playerPrefab != null && spawnPoint != null)
        {
            SpawnPlayer(spawnPoint.position);
        }
    }

    void Update()
    {
    }


    public void SpawnPlayer(Vector3 position)
    {
        GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
        Debug.Log($"Spawned player at {position}");
    }
}