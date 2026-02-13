using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Player Management")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Testing Controls")]
    [Space]
    [Header("Press these keys to test:")]
    [Header("1 - Spawn Player with Light")]
    [Header("2 - Remove Last Player")]
    [Header("3 - Move Random Player")]
    [Header("R - Reset All Players")]

    private Map mapReference;

    void Start()
    {
        mapReference = FindFirstObjectByType<Map>();

        // Automatically spawn a player at the start for testing
        if (playerPrefab != null)
        {
            SpawnPlayer(Vector3.zero);
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Spawn new player
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            SpawnPlayer(spawnPos);
        }

        // Remove last player
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            RemoveLastPlayer();
        }

        // Move random player to random position
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            MoveRandomPlayer();
        }

        // Reset all players
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllPlayers();
        }
    }

    public void SpawnPlayer(Vector3 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("No player prefab assigned!");
            return;
        }

        GameObject newPlayer = Instantiate(playerPrefab, position, Quaternion.identity);

        // Get or add PlayerController
        PlayerController playerController = newPlayer.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = newPlayer.AddComponent<PlayerController>();
        }

        // Get or create LightSource as child
        LightSource lightSource = newPlayer.GetComponentInChildren<LightSource>();
        if (lightSource == null)
        {
            GameObject lightObject = new GameObject("Light Source");
            lightObject.transform.SetParent(newPlayer.transform);
            lightObject.transform.localPosition = Vector3.up * 0.5f;
            lightSource = lightObject.AddComponent<LightSource>();
        }

        // Link the light source to the player controller
        playerController.attachedLight = lightSource;

        // Randomize light properties for variety
        lightSource.lightRadius = Random.Range(3f, 8f);

        Debug.Log($"Spawned player with light at {position}");
    }

    Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return randomPoint.position;
        }

        // Generate random position around map center
        Vector3 mapCenter = Vector3.zero;
        if (mapReference != null)
        {
            mapCenter = mapReference.transform.position;
        }

        Vector3 randomOffset = new Vector3(
            Random.Range(-10f, 10f),
            1f, // Keep light sources above ground
            Random.Range(-10f, 10f)
        );

        return mapCenter + randomOffset;
    }

    void RemoveLastPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        if (players.Length > 0)
        {
            PlayerController lastPlayer = players[players.Length - 1];
            Debug.Log($"Removing player: {lastPlayer.name}");
            DestroyImmediate(lastPlayer.gameObject);
        }
        else
        {
            Debug.Log("No players to remove");
        }
    }

    void MoveRandomPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        if (players.Length > 0)
        {
            PlayerController randomPlayer = players[Random.Range(0, players.Length)];
            Vector3 newPosition = GetRandomSpawnPosition();

            randomPlayer.SetPosition(newPosition);
            Debug.Log($"Moved {randomPlayer.name} to {newPosition}");
        }
        else
        {
            Debug.Log("No players to move");
        }
    }

    void ResetAllPlayers()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            DestroyImmediate(player.gameObject);
        }

        Debug.Log("Removed all players");

        // Spawn one default player
        if (playerPrefab != null)
        {
            SpawnPlayer(Vector3.zero);
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 150));
        GUILayout.Label("Player Controls:");
        GUILayout.Label("1 - Spawn Player with Light");
        GUILayout.Label("2 - Remove Last Player");
        GUILayout.Label("3 - Move Random Player");
        GUILayout.Label("R - Reset All Players");
        GUILayout.Label("");
        GUILayout.Label("WASD - Move active player");
        GUILayout.Label("Space - Jump");
        GUILayout.Label("Shift - Sprint");

        int playerCount = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length;
        int lightCount = FindObjectsByType<LightSource>(FindObjectsSortMode.None).Length;
        GUILayout.Label($"Active Players: {playerCount}");
        GUILayout.Label($"Active Light Sources: {lightCount}");
        GUILayout.EndArea();
    }
}