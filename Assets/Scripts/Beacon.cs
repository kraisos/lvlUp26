using UnityEngine;

/// <summary>
/// End-goal beacon. The player wins when energy from the pipe network reaches this node.
/// The Beacon acts as a connectable node in the GasPipeNetwork (like a Streetlight).
/// </summary>
[RequireComponent(typeof(CableAnchor))]
public class Beacon : MonoBehaviour
{
    [Header("Detection")]
    public float activationRadius = 20f;

    [Header("Visuals")]
    public Color beaconColor = new Color(0.2f, 0.8f, 1f, 1f);
    public float pulseSpeed = 2f;
    public float lightIntensity = 2f;

    private Light beaconLight;
    private bool reached = false;

    public bool IsReached => reached;

    /// <summary>
    /// Fired when energy reaches the beacon through the pipe network.
    /// </summary>
    public event System.Action OnBeaconReached;

    void Start()
    {
        // Add a point light as a visual beacon
        beaconLight = gameObject.GetComponentInChildren<Light>();
        if (beaconLight == null)
        {
            GameObject lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 5f;
            beaconLight = lightObj.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = beaconColor;
            beaconLight.intensity = lightIntensity;
            beaconLight.range = 20f;
        }

        // Register with the pipe network as a connectable endpoint
        EnsureNetwork();
        GasPipeNetwork.Instance.RegisterBeacon(this);
    }

    private void OnDisable()
    {
        if (GasPipeNetwork.Instance != null)
        {
            GasPipeNetwork.Instance.UnregisterBeacon(this);
        }
    }

    void Update()
    {
        if (reached) return;

        // Pulse the light
        if (beaconLight != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            beaconLight.intensity = Mathf.Lerp(lightIntensity * 0.5f, lightIntensity * 1.5f, pulse);
        }
    }

    /// <summary>
    /// Called by GasPipeNetwork when BFS energy reaches this beacon.
    /// </summary>
    public void NotifyEnergyArrived()
    {
        if (reached) return;

        reached = true;
        Debug.Log("Beacon activated! Energy has reached the beacon through the pipe network.");
        OnBeaconReached?.Invoke();
    }

    /// <summary>
    /// Legacy method kept for compatibility — now redirects to energy-based activation.
    /// </summary>
    public void NotifyReached(Streetlight streetlight)
    {
        // No longer triggers win by proximity alone.
        // Win is triggered only when energy flows through pipes to the beacon.
        float dist = Vector3.Distance(transform.position, streetlight.transform.position);
        Debug.Log($"Streetlight placed at distance {dist:F1}m from beacon — waiting for energy connection.");
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

    void OnDrawGizmos()
    {
        // Always show the beacon position in the editor
        Gizmos.color = beaconColor;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 10f);
    }
}
