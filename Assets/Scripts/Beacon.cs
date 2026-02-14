using UnityEngine;

/// <summary>
/// End-goal beacon. The player wins by placing a streetlight close enough to it.
/// </summary>
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
    /// Fired when a streetlight is placed close enough to the beacon.
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
    /// Called by a Streetlight when it's placed close enough to this beacon.
    /// </summary>
    public void NotifyReached(Streetlight streetlight)
    {
        if (reached) return;

        float dist = Vector3.Distance(transform.position, streetlight.transform.position);
        reached = true;
        Debug.Log($"Beacon activated! Streetlight placed at distance {dist:F1}m (radius {activationRadius}m).");
        OnBeaconReached?.Invoke();
    }

    void OnDrawGizmos()
    {
        // Always show the beacon position in the editor
        Gizmos.color = beaconColor;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 10f);
    }
}
