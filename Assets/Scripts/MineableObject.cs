using UnityEngine;

public class MineableObject : MonoBehaviour
{
    [Header("Mineable")]
    [SerializeField] private float miningDuration = 1.5f;

    [Header("Drop")]
    [SerializeField] private GameObject dropPrefab;

    public float MiningDuration => Mathf.Max(0.1f, miningDuration);
    public GameObject DropPrefab => dropPrefab;

    public void OnMined()
    {
        Destroy(gameObject);
    }
}