using UnityEngine;

[RequireComponent(typeof(CableAnchor))]
public class Streetlight : MonoBehaviour
{
    public bool IsEnergySource { get; private set; }

    private void OnEnable()
    {
        EnsureNetwork();
        GasPipeNetwork.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (GasPipeNetwork.Instance != null)
        {
            GasPipeNetwork.Instance.Unregister(this);
        }
    }

    public void MarkAsEnergySource()
    {
        IsEnergySource = true;
    }

    private static void EnsureNetwork()
    {
        if (GasPipeNetwork.Instance != null)
        {
            return;
        }

        var go = new GameObject("GasPipeNetwork");
        go.AddComponent<GasPipeNetwork>();
    }

    void Start()
    {
        // Find the nearest beacon and log the distance
        Beacon nearest = null;
        float nearestDist = float.MaxValue;

        Beacon[] beacons = FindObjectsByType<Beacon>(FindObjectsSortMode.None);
        foreach (Beacon beacon in beacons)
        {
            float dist = Vector3.Distance(transform.position, beacon.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = beacon;
            }
        }

        if (nearest != null)
        {
            Debug.Log($"Streetlight placed — nearest beacon is {nearestDist:F1}m away (activation radius: {nearest.activationRadius}m)");

            if (nearestDist <= nearest.activationRadius && !nearest.IsReached)
            {
                nearest.NotifyReached(this);
            }
        }
        else
        {
            Debug.Log("Streetlight placed — no beacon found in scene");
        }
    }
}
