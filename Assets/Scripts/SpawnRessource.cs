using UnityEngine;

public class SpawnRessource : MonoBehaviour
{
    public GameObject[] ressources;
    public float spawnChance = 0.1f; // 10% chance

    void Start()
    {
        if (Random.value < spawnChance && !HasNearbyItem(10f))
        {
            SpawnRandomRessource();
        }
    }

    bool HasNearbyItem(float radius)
    {
        Item[] items = FindObjectsByType<Item>(FindObjectsSortMode.None);
        foreach (Item item in items)
        {
            if (Vector3.Distance(transform.position, item.transform.position) < radius)
                return true;
        }

        MineableObject[] mineables = FindObjectsByType<MineableObject>(FindObjectsSortMode.None);
        foreach (MineableObject mineable in mineables)
        {
            if (Vector3.Distance(transform.position, mineable.transform.position) < radius)
                return true;
        }

        return false;
    }

    void SpawnRandomRessource()
    {
        if (ressources.Length == 0) return;
        
        GameObject randomRessource = ressources[Random.Range(0, ressources.Length)];
        Instantiate(randomRessource, transform.position, Quaternion.identity);
    }
}
