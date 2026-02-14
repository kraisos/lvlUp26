using UnityEngine;

public class SpawnRessource : MonoBehaviour
{
    public GameObject[] ressources;
    public float spawnChance = 0.1f; // 10% chance

    void Start()
    {
        if (Random.value < spawnChance)
        {
            SpawnRandomRessource();
        }
    }

    void SpawnRandomRessource()
    {
        if (ressources.Length == 0) return;
        
        GameObject randomRessource = ressources[Random.Range(0, ressources.Length)];
        Instantiate(randomRessource, transform.position, Quaternion.identity);
    }
}
