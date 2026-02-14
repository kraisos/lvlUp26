using UnityEngine;

public class WallHeight : MonoBehaviour
{
    [Min(1)] private int x = 3;

    void Start()
    {
        if (x <= 1)
        {
            return;
        }

        float stepHeight = GetStepHeight();

        for (int i = 1; i < x; i++)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * (stepHeight * i);
            GameObject clone = Instantiate(gameObject, spawnPosition, transform.rotation, transform.parent);

            WallHeight cloneWallHeight = clone.GetComponent<WallHeight>();
            if (cloneWallHeight != null)
            {
                cloneWallHeight.enabled = false;
            }
        }
    }

    float GetStepHeight()
    {
        Renderer rendererComponent = GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            return rendererComponent.bounds.size.y;
        }

        Collider colliderComponent = GetComponent<Collider>();
        if (colliderComponent != null)
        {
            return colliderComponent.bounds.size.y;
        }

        return Mathf.Max(transform.localScale.y, 0.01f);
    }
}
